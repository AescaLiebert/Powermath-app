using System;

namespace PowerMath.Gameplay.Combat
{
    public interface ILocalEncounterEngine
    {
        CombatSnapshot Snapshot { get; }
        CombatSnapshot CommitAttempt();
        CombatSnapshot VoidContentFailure();
        CombatResolution ResolveCorrect(int responseScore, double rankMultiplier);
        CombatResolution ResolveIncorrect(bool timedOut);
        CombatSnapshot CompletePresentation();
    }

    public sealed class LocalRunEncounterEngine : ILocalEncounterEngine
    {
        private readonly StageEncounterResolver _resolver;
        private readonly string _runId;
        private readonly IRandomSource _random;
        private readonly DamageCalculator _damageCalculator = new DamageCalculator();
        private readonly PlayerCombatStats _stats;
        private StageId _stage;
        private EncounterSelection _selection;
        private EnemyState _enemy;
        private int _currentHearts;
        private readonly int _maximumHearts;
        private int _eventAttemptOrdinal;
        private bool _attemptCommitted;
        private CombatPhase _phase;

        public LocalRunEncounterEngine(StageId stage, string runId,
            StageEncounterResolver resolver, IRandomSource random,
            int maximumHearts, PlayerCombatStats stats)
        {
            if (maximumHearts <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHearts));
            _stage = stage; _runId = runId ?? throw new ArgumentNullException(nameof(runId));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _maximumHearts = maximumHearts; _currentHearts = maximumHearts; _stats = stats;
            LoadSelection(_resolver.Resolve(_runId, _stage), true);
        }

        public LocalRunEncounterEngine(CombatSnapshot restored, string runId,
            StageEncounterResolver resolver, IRandomSource random, PlayerCombatStats stats)
        {
            if (restored == null) throw new ArgumentNullException(nameof(restored));
            _stage = restored.Stage; _runId = runId ?? throw new ArgumentNullException(nameof(runId));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _random = random ?? throw new ArgumentNullException(nameof(random)); _stats = stats;
            _maximumHearts = restored.PlayerMaximumHearts; _currentHearts = restored.PlayerCurrentHearts;
            _selection = _resolver.Resolve(_runId, _stage);
            if (!string.Equals(_selection.EncounterId, restored.EnemyId, StringComparison.Ordinal) ||
                _selection.Kind != restored.EncounterKind)
                throw new InvalidOperationException("Saved encounter does not match the Stage Map catalog.");
            _eventAttemptOrdinal = Math.Max(0, restored.EventAttemptOrdinal);
            _phase = restored.Phase == CombatPhase.PresentingResult
                ? (_selection.IsEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady)
                : restored.Phase;
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
            if (!_selection.IsEvent) _enemy.ConsumeCooldown();
            _attemptCommitted = true; _phase = CombatPhase.Committed;
            return CreateSnapshot();
        }

        public CombatSnapshot VoidContentFailure()
        {
            EnsureCommitted();
            if (!_selection.IsEvent) _enemy.RestoreCommittedCooldown();
            _attemptCommitted = false;
            _phase = _selection.IsEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady;
            return CreateSnapshot();
        }

        public CombatResolution ResolveCorrect(int responseScore, double rankMultiplier)
        {
            EnsureCommitted();
            ResponseDamagePolicy.GetMultiplier(responseScore);
            if (_selection.IsEvent) return Resolve(responseScore, 1, true, false, false);
            bool critical = _random.NextUnit() < _stats.CriticalRate;
            DamageResult damage = _damageCalculator.Calculate(new DamageInput(
                _stats.EffectiveAttack, rankMultiplier, 1d,
                _stats.CriticalDamagePercent, critical, responseScore));
            return Resolve(responseScore, damage.FinalDamage, true, critical, false);
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
            return CreateSnapshot();
        }

        private CombatResolution Resolve(int score, int damage, bool correct, bool critical, bool timedOut)
        {
            StageId resolvedStage = _stage;
            string previousBiome = _selection.BiomeId;
            int hpBefore = _selection.IsEvent ? 1 : _enemy.CurrentHp;
            int hpAfter = _selection.IsEvent ? (correct ? 0 : 1) : _enemy.ApplyDamage(damage);
            bool defeated = hpAfter == 0;
            bool attacked = false;
            bool playerDefeated = false;
            bool advanced = false;
            _phase = CombatPhase.Resolving;

            if (defeated)
            {
                if (_stage.TryNext(out StageId next))
                {
                    _stage = next; advanced = true;
                    LoadSelection(_resolver.Resolve(_runId, _stage), false);
                    _phase = CombatPhase.PresentingResult;
                }
                else _phase = CombatPhase.RunComplete;
            }
            else if (_selection.IsEvent)
            {
                attacked = true; _eventAttemptOrdinal++;
                _currentHearts = Math.Max(0, _currentHearts - 1);
                playerDefeated = _currentHearts == 0;
                _phase = playerDefeated ? CombatPhase.RunDefeat : CombatPhase.PresentingResult;
            }
            else if (_enemy.RemainingCooldown == 0)
            {
                attacked = true; _currentHearts = Math.Max(0, _currentHearts - 1);
                playerDefeated = _currentHearts == 0; _enemy.ResetCooldown();
                _phase = playerDefeated ? CombatPhase.RunDefeat : CombatPhase.PresentingResult;
            }
            else _phase = CombatPhase.PresentingResult;

            _attemptCommitted = false;
            return new CombatResolution(score, damage, correct, critical, timedOut,
                hpBefore, hpAfter, defeated, attacked, playerDefeated, resolvedStage,
                advanced, CreateSnapshot(), advanced && previousBiome != _selection.BiomeId);
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
                _selection.Kind, _selection.QuestionDocumentId, _eventAttemptOrdinal);
        }
    }
}
