using System;
using System.Collections.Generic;
using PowerMath.Gameplay.Pets;

namespace PowerMath.Gameplay.Combat
{
    public enum HeartChangeReason
    {
        DamageTaken,
        PetPassiveRestore,
        Heal,
        MaxHeartsExpanded
    }

    public readonly struct HeartChangeArgs
    {
        public HeartChangeArgs(
            int previousHearts,
            int currentHearts,
            int maximumHearts,
            int changeAmount,
            HeartChangeReason reason)
        {
            PreviousHearts = previousHearts;
            CurrentHearts = currentHearts;
            MaximumHearts = maximumHearts;
            ChangeAmount = changeAmount;
            Reason = reason;
        }

        public int PreviousHearts { get; }
        public int CurrentHearts { get; }
        public int MaximumHearts { get; }
        public int ChangeAmount { get; }
        public HeartChangeReason Reason { get; }
    }

    public interface ILocalEncounterEngine
    {
        CombatSnapshot Snapshot { get; }
        event Action<HeartChangeArgs> HeartChanged;
        CombatSnapshot CommitAttempt();
        CombatSnapshot VoidContentFailure();
        CombatResolution ResolveCorrect(int responseScore, double rankMultiplier);
        CombatResolution ResolveIncorrect(bool timedOut);
        CombatSnapshot CompletePresentation();
        void RefreshPlayerStats(PlayerCombatStats stats);
        void RefreshEventSchedule(EventScheduleSnapshot schedule);
    }

    public sealed class LocalRunEncounterEngine : ILocalEncounterEngine
    {
        private readonly StageEncounterResolver _resolver;
        private readonly string _runId;
        private readonly IRandomSource _random;
        private readonly DamageCalculator _damageCalculator = new DamageCalculator();
        private PlayerCombatStats _stats;
        private PlayerCombatStats? _pendingStats;
        private readonly bool _invincible;
        private StageId _stage;
        private EncounterSelection _selection;
        private EnemyState _enemy;
        private int _currentHearts;
        private readonly int _baseMaximumHearts;
        private int _maximumHearts;
        private int _eventAttemptOrdinal;
        private bool _attemptCommitted;
        private CombatPhase _phase;
        private int _stageAttackCount;
        private int _bigBossesDefeated;
        private int _pendingPetFollowUpDamage;

        public event Action<HeartChangeArgs> HeartChanged;

        public LocalRunEncounterEngine(StageId stage, string runId,
            StageEncounterResolver resolver, IRandomSource random,
            int maximumHearts, PlayerCombatStats stats, bool invincible = false)
        {
            if (maximumHearts <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHearts));
            _stage = stage; _runId = runId ?? throw new ArgumentNullException(nameof(runId));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _stats = stats;
            _baseMaximumHearts = maximumHearts;
            _maximumHearts = checked(_baseMaximumHearts + stats.BonusMaxHearts);
            _currentHearts = _maximumHearts;
            _invincible = invincible;
            LoadSelection(_resolver.Resolve(_runId, _stage), true);
        }

        public LocalRunEncounterEngine(CombatSnapshot restored, string runId,
            StageEncounterResolver resolver, IRandomSource random, PlayerCombatStats stats,
            bool invincible = false,
            int baseMaximumHearts = 3)
        {
            if (restored == null) throw new ArgumentNullException(nameof(restored));
            _stage = restored.Stage; _runId = runId ?? throw new ArgumentNullException(nameof(runId));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _random = random ?? throw new ArgumentNullException(nameof(random)); _stats = stats;
            _invincible = invincible;
            if (baseMaximumHearts <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseMaximumHearts));
            _baseMaximumHearts = Math.Max(
                baseMaximumHearts,
                restored.PlayerMaximumHearts - stats.BonusMaxHearts);
            _maximumHearts = Math.Max(
                restored.PlayerMaximumHearts,
                checked(_baseMaximumHearts + stats.BonusMaxHearts));
            _currentHearts = restored.PlayerCurrentHearts;
            _stageAttackCount = Math.Max(0, restored.StageAttackCount);
            _bigBossesDefeated = Math.Max(0, restored.BigBossesDefeated);
            _pendingPetFollowUpDamage = Math.Max(0, restored.PendingPetFollowUpDamage);
            _selection = _resolver.Resolve(_runId, _stage);
            if (!string.Equals(_selection.EncounterId, restored.EnemyId, StringComparison.Ordinal) ||
                _selection.Kind != restored.EncounterKind)
                throw new InvalidOperationException("Saved encounter does not match the Stage Map catalog.");
            _eventAttemptOrdinal = Math.Max(0, restored.EventAttemptOrdinal);
            _phase = restored.Phase;
            _attemptCommitted = _phase == CombatPhase.Committed;
            if (!_selection.IsEvent)
            {
                var definition = ToEnemyDefinition(_selection);
                _enemy = new EnemyState(definition, restored.EnemyMaximumHp,
                    restored.EnemyCurrentHp, restored.EnemyRemainingCooldown);
            }
        }

        public CombatSnapshot Snapshot => CreateSnapshot();

        public CombatSnapshot CommitAttempt()
        {
            bool ready = _phase == CombatPhase.EnemyReady || _phase == CombatPhase.EventReady;
            if (!ready || _attemptCommitted) throw new InvalidOperationException("Encounter is not ready.");
            _attemptCommitted = true; _phase = CombatPhase.Committed;
            return CreateSnapshot();
        }

        public CombatSnapshot VoidContentFailure()
        {
            EnsureCommitted();
            _attemptCommitted = false;
            _phase = _selection.IsEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady;
            return CreateSnapshot();
        }

        public CombatResolution ResolveCorrect(int responseScore, double rankMultiplier)
        {
            EnsureCommitted();
            ResponseDamagePolicy.GetMultiplier(responseScore);
            if (_selection.IsEvent) return Resolve(responseScore, 1, true, false, false);

            _stageAttackCount++;
            double buffMultiplier = ResolvePlayerAttackPassiveMultiplier();

            bool critical = _random.NextUnit() < _stats.CriticalRate;
            DamageResult damage = _damageCalculator.Calculate(new DamageInput(
                _stats.EffectiveAttack, rankMultiplier, buffMultiplier,
                _stats.CriticalDamagePercent, critical, responseScore));

            bool petCritical = false;
            int petDamage = CalculatePetFollowUpDamage(ref petCritical);
            return ResolveSuccessfulAttack(
                responseScore,
                damage.FinalDamage,
                critical,
                damage.Breakdown,
                petDamage,
                petCritical);
        }

        private CombatResolution ResolveSuccessfulAttack(
            int score,
            int playerDamage,
            bool critical,
            DamageBreakdown damageBreakdown,
            int petDamage,
            bool petCritical)
        {
            StageId resolvedStage = _stage;
            string previousBiome = _selection.BiomeId;
            int hpBefore = _enemy.CurrentHp;
            _phase = CombatPhase.Resolving;
            int playerHpAfter = _enemy.ApplyDamage(playerDamage);
            int hpAfter = playerHpAfter;
            int totalSourceDamage = playerDamage;
            bool defeated = playerHpAfter == 0;
            bool advanced = false;
            bool attacked = false;
            bool playerDefeated = false;
            PetFollowUpResolution petFollowUp = null;

            if (!defeated && petDamage > 0)
            {
                CombatSnapshot target = CreateSnapshot();
                hpAfter = _enemy.ApplyDamage(petDamage);
                totalSourceDamage = checked(totalSourceDamage + petDamage);
                defeated = hpAfter == 0;
                petFollowUp = new PetFollowUpResolution(
                    petDamage,
                    petCritical,
                    target,
                    hpAfter,
                    defeated,
                    defeated,
                    false);
            }
            else if (defeated && petDamage > 0)
            {
                _pendingPetFollowUpDamage = checked(
                    _pendingPetFollowUpDamage + petDamage);
            }

            if (defeated)
            {
                AdvanceAfterDefeat(ref advanced);
                if (petFollowUp == null)
                {
                    petFollowUp = ResolvePendingPetFollowUp(
                        petCritical,
                        true,
                        ref advanced);
                }
            }
            else
            {
                ResolveEnemyTurn(out attacked, out playerDefeated);
            }

            _attemptCommitted = false;
            return new CombatResolution(
                score,
                totalSourceDamage,
                true,
                critical,
                false,
                hpBefore,
                hpAfter,
                defeated,
                attacked,
                playerDefeated,
                resolvedStage,
                advanced,
                CreateSnapshot(),
                advanced && previousBiome != _selection.BiomeId,
                damageBreakdown,
                false,
                playerDamage,
                playerHpAfter,
                petFollowUp);
        }

        public CombatResolution ResolveIncorrect(bool timedOut)
        {
            EnsureCommitted();
            return Resolve(0, 0, false, false, timedOut);
        }

        public CombatSnapshot CompletePresentation()
        {
            if (_phase == CombatPhase.PresentingResult)
                _phase = _selection.IsEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady;
            ApplyPendingStatsIfSafe();
            return CreateSnapshot();
        }

        public void RefreshPlayerStats(PlayerCombatStats stats)
        {
            if (_attemptCommitted || _phase == CombatPhase.Committed ||
                _phase == CombatPhase.Resolving ||
                _phase == CombatPhase.PresentingResult)
            {
                _pendingStats = stats;
                return;
            }

            ApplyPlayerStats(stats);
        }

        public void RefreshEventSchedule(EventScheduleSnapshot schedule)
        {
            _resolver.ReplaceSchedule(schedule);
        }

        private void ApplyPendingStatsIfSafe()
        {
            if (!_pendingStats.HasValue || _attemptCommitted ||
                _phase == CombatPhase.Committed ||
                _phase == CombatPhase.Resolving ||
                _phase == CombatPhase.PresentingResult)
                return;

            PlayerCombatStats stats = _pendingStats.Value;
            _pendingStats = null;
            ApplyPlayerStats(stats);
        }

        private void ApplyPlayerStats(PlayerCombatStats stats)
        {
            int previousMaximum = _maximumHearts;
            int previousCurrent = _currentHearts;
            _stats = stats;
            _maximumHearts = checked(_baseMaximumHearts + stats.BonusMaxHearts);
            int maximumDelta = _maximumHearts - previousMaximum;
            _currentHearts = maximumDelta > 0
                ? Math.Min(_maximumHearts, checked(_currentHearts + maximumDelta))
                : Math.Min(_currentHearts, _maximumHearts);
            if (_maximumHearts != previousMaximum || _currentHearts != previousCurrent)
            {
                HeartChanged?.Invoke(new HeartChangeArgs(
                    previousCurrent,
                    _currentHearts,
                    _maximumHearts,
                    _currentHearts - previousCurrent,
                    HeartChangeReason.MaxHeartsExpanded));
            }
        }

        private CombatResolution Resolve(int score, int damage, bool correct,
            bool critical, bool timedOut,
            DamageBreakdown damageBreakdown = default)
        {
            StageId resolvedStage = _stage;
            string previousBiome = _selection.BiomeId;
            int hpBefore = _selection.IsEvent ? 1 : _enemy.CurrentHp;
            int hpAfter = _selection.IsEvent ? (correct ? 0 : 1) : _enemy.ApplyDamage(damage);
            bool defeated = hpAfter == 0;
            bool attacked = false;
            bool playerDefeated = false;
            bool advanced = false;
            bool fled = false;
            _phase = CombatPhase.Resolving;

            PetFollowUpResolution petFollowUp = null;
            if (defeated)
            {
                AdvanceAfterDefeat(ref advanced);
                petFollowUp = ResolvePendingPetFollowUp(false, true, ref advanced);
            }
            else if (_selection.IsEvent)
            {
                fled = true;
                _eventAttemptOrdinal++;
                if (_stage.TryNext(out StageId next))
                {
                    _stage = next;
                    advanced = true;
                    _stageAttackCount = 0;
                    LoadSelection(_resolver.Resolve(_runId, _stage), false);
                    _phase = CombatPhase.PresentingResult;
                }
                else _phase = CombatPhase.RunComplete;
                petFollowUp = ResolvePendingPetFollowUp(false, true, ref advanced);
            }
            else
            {
                ResolveEnemyTurn(out attacked, out playerDefeated);
            }

            _attemptCommitted = false;
            return new CombatResolution(score, damage, correct, critical, timedOut,
                hpBefore, hpAfter, defeated, attacked, playerDefeated, resolvedStage,
                advanced, CreateSnapshot(), advanced && previousBiome != _selection.BiomeId,
                damageBreakdown, fled, damage, hpAfter, petFollowUp);
        }

        private int CalculatePetFollowUpDamage(ref bool critical)
        {
            double multiplier = _stats.PetPassives.SumMagnitude(
                PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack);
            if (multiplier <= 0d || _stats.EffectivePetAttack <= 0) return 0;

            double rawDamage = _stats.EffectivePetAttack * multiplier;
            if (rawDamage > int.MaxValue)
                throw new OverflowException("Pet follow-up damage exceeds the supported range.");
            int damage = Math.Max(1, (int)Math.Round(
                rawDamage,
                MidpointRounding.AwayFromZero));
            critical = _stats.PetPassives.HasEffect(
                    PetPassiveEffectType.EnablePetFollowUpCritical) &&
                _random.NextUnit() < _stats.CriticalRate;
            if (critical)
            {
                double criticalDamage = damage *
                    (1d + _stats.CriticalDamagePercent / 100d);
                if (criticalDamage > int.MaxValue)
                    throw new OverflowException("Critical pet follow-up damage exceeds the supported range.");
                damage = Math.Max(1, (int)Math.Round(
                    criticalDamage,
                    MidpointRounding.AwayFromZero));
            }
            return damage;
        }

        private PetFollowUpResolution ResolvePendingPetFollowUp(
            bool critical,
            bool carried,
            ref bool advanced)
        {
            if (_pendingPetFollowUpDamage <= 0 || _enemy == null ||
                _phase == CombatPhase.RunComplete)
                return null;

            int damage = _pendingPetFollowUpDamage;
            _pendingPetFollowUpDamage = 0;
            CombatSnapshot target = CreateSnapshot();
            int hpAfter = _enemy.ApplyDamage(damage);
            bool defeated = hpAfter == 0;
            bool petAdvanced = false;
            if (defeated)
            {
                AdvanceAfterDefeat(ref advanced);
                petAdvanced = true;
            }
            return new PetFollowUpResolution(
                damage,
                critical,
                target,
                hpAfter,
                defeated,
                petAdvanced,
                carried);
        }

        private void AdvanceAfterDefeat(ref bool advanced)
        {
            if (_selection.Kind == StageEncounterKind.BigBoss)
                _bigBossesDefeated++;

            int passiveHeartRestore = ResolvePassiveHeartRestore();
            if (passiveHeartRestore > 0 && _currentHearts < _maximumHearts)
            {
                int previous = _currentHearts;
                _currentHearts = Math.Min(
                    _maximumHearts,
                    checked(_currentHearts + passiveHeartRestore));
                if (_currentHearts != previous)
                {
                    HeartChanged?.Invoke(new HeartChangeArgs(
                        previous,
                        _currentHearts,
                        _maximumHearts,
                        _currentHearts - previous,
                        HeartChangeReason.PetPassiveRestore));
                }
            }

            if (_stage.TryNext(out StageId next))
            {
                _stage = next;
                advanced = true;
                _stageAttackCount = 0;
                LoadSelection(_resolver.Resolve(_runId, _stage), false);
                _phase = CombatPhase.PresentingResult;
            }
            else
            {
                _phase = CombatPhase.RunComplete;
            }
        }

        private void ResolveEnemyTurn(out bool attacked, out bool playerDefeated)
        {
            attacked = false;
            playerDefeated = false;
            _enemy.ConsumeCooldown();
            if (_enemy.RemainingCooldown == 0)
            {
                attacked = true;
                if (!_invincible)
                {
                    int previous = _currentHearts;
                    _currentHearts = Math.Max(0, _currentHearts - 1);
                    if (_currentHearts != previous)
                    {
                        HeartChanged?.Invoke(new HeartChangeArgs(
                            previous,
                            _currentHearts,
                            _maximumHearts,
                            -1,
                            HeartChangeReason.DamageTaken));
                    }
                }
                playerDefeated = _currentHearts == 0;
                _enemy.ResetCooldown();
                _phase = playerDefeated
                    ? CombatPhase.RunDefeat
                    : CombatPhase.PresentingResult;
            }
            else
            {
                _phase = CombatPhase.PresentingResult;
            }
        }

        public int StageAttackCount => _stageAttackCount;
        public int BigBossesDefeated => _bigBossesDefeated;
        public int PendingPetFollowUpDamage => _pendingPetFollowUpDamage;

        private double ResolvePlayerAttackPassiveMultiplier()
        {
            if (!_stats.PetPassives.TryGetFirst(
                    PetPassiveEffectType.ModifyEveryNthPlayerAttack,
                    out ActivePetPassive passive))
                return 1d;

            int triggerCount = passive.Definition.TriggerCount;
            if (triggerCount <= 0 || _stageAttackCount % triggerCount != 0)
                return 1d;
            return 1d + passive.EffectiveMagnitude;
        }

        private int ResolvePassiveHeartRestore()
        {
            double total = 0d;
            IReadOnlyList<ActivePetPassive> passives = _stats.PetPassives.Items;
            for (int index = 0; index < passives.Count; index++)
            {
                ActivePetPassive passive = passives[index];
                if (passive.Definition.EffectType !=
                    PetPassiveEffectType.RestoreHeartsOnEncounterDefeat)
                    continue;
                if (!MatchesEncounterFilter(passive.Definition.EncounterFilter))
                    continue;
                total += passive.EffectiveMagnitude;
            }

            if (total <= 0d) return 0;
            if (total > int.MaxValue)
                throw new OverflowException("Pet heart restoration exceeds the supported range.");
            return Math.Max(0, (int)Math.Round(total, MidpointRounding.AwayFromZero));
        }

        private bool MatchesEncounterFilter(PetEncounterFilter filter)
        {
            switch (filter)
            {
                case PetEncounterFilter.Any:
                    return true;
                case PetEncounterFilter.BigBoss:
                    return _selection.Kind == StageEncounterKind.BigBoss;
                default:
                    return false;
            }
        }

        private void LoadSelection(EncounterSelection selection, bool initial)
        {
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _eventAttemptOrdinal = 0;
            _enemy = selection.IsEvent ? null : new EnemyState(ToEnemyDefinition(selection), selection.MaximumHp);
            _phase = selection.IsEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady;
        }

        private static EnemyDefinitionData ToEnemyDefinition(EncounterSelection value) =>
            new EnemyDefinitionData(value.EncounterId, value.DisplayName,
                Math.Max(1, value.MaximumHp), Math.Max(1, value.MaximumCooldown));

        private void EnsureCommitted()
        {
            if (!_attemptCommitted || _phase != CombatPhase.Committed)
                throw new InvalidOperationException("No committed attempt can be resolved.");
        }

        private CombatSnapshot CreateSnapshot()
        {
            int hp = _selection.IsEvent ? 1 : _enemy.CurrentHp;
            int maxHp = _selection.IsEvent ? 1 : _enemy.MaximumHp;
            int cooldown = _selection.IsEvent ? 0 : _enemy.RemainingCooldown;
            return new CombatSnapshot(_stage, _selection.EncounterId, _selection.DisplayName,
                hp, maxHp, cooldown, _selection.MaximumCooldown, _currentHearts,
                _maximumHearts, _phase, false, _selection.BiomeId, _selection.BiomeTitle,
                _selection.Kind, _selection.QuestionDocumentId, _eventAttemptOrdinal,
                _resolver.Schedule, _stageAttackCount, _bigBossesDefeated, _pendingPetFollowUpDamage);
        }
    }
}
