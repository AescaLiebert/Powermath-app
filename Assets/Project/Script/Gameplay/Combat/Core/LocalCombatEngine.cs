using System;

namespace PowerMath.Gameplay.Combat
{
    public sealed class LocalCombatEngine : ILocalEncounterEngine
    {
        private readonly EnemyDefinitionData _enemyDefinition;
        private readonly IRandomSource _random;
        private readonly StageProgressionCalculator _stageCalculator;
        private readonly DamageCalculator _damageCalculator;
        private readonly int _maximumHearts;
        private readonly double _criticalRate;
        private readonly double _criticalDamagePercent;
        private readonly int _effectiveAttack;

        private StageId _stage;
        private EnemyState _enemy;
        private int _currentHearts;
        private bool _attemptCommitted;
        private CombatPhase _phase;

        public LocalCombatEngine(
            StageId startingStage,
            EnemyDefinitionData enemyDefinition,
            IRandomSource random,
            int maximumHearts,
            double criticalRate,
            double criticalDamagePercent)
        {
            _stage = startingStage;
            _enemyDefinition = enemyDefinition ??
                throw new ArgumentNullException(nameof(enemyDefinition));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (maximumHearts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHearts));
            }

            if (criticalRate < 0d || criticalRate > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(criticalRate));
            }

            if (criticalDamagePercent < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(criticalDamagePercent));
            }

            _maximumHearts = maximumHearts;
            _currentHearts = maximumHearts;
            _criticalRate = criticalRate;
            _criticalDamagePercent = criticalDamagePercent;
            _effectiveAttack = 5;
            _stageCalculator = new StageProgressionCalculator();
            _damageCalculator = new DamageCalculator();
            _phase = CombatPhase.EnemyReady;
            _enemy = SpawnEnemy();
        }

        public LocalCombatEngine(
            StageId startingStage,
            EnemyDefinitionData enemyDefinition,
            IRandomSource random,
            int maximumHearts,
            PlayerCombatStats stats)
            : this(startingStage, enemyDefinition, random, maximumHearts,
                stats.CriticalRate, stats.CriticalDamagePercent)
        {
            _effectiveAttack = stats.EffectiveAttack;
        }

        public LocalCombatEngine(
            CombatSnapshot restored,
            EnemyDefinitionData enemyDefinition,
            IRandomSource random,
            double criticalRate,
            double criticalDamagePercent)
        {
            if (restored == null) throw new ArgumentNullException(nameof(restored));
            _stage = restored.Stage;
            _enemyDefinition = enemyDefinition ?? throw new ArgumentNullException(nameof(enemyDefinition));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (restored.PlayerMaximumHearts <= 0 || restored.PlayerCurrentHearts < 0 ||
                restored.PlayerCurrentHearts > restored.PlayerMaximumHearts)
                throw new ArgumentOutOfRangeException(nameof(restored));
            if (criticalRate < 0d || criticalRate > 1d)
                throw new ArgumentOutOfRangeException(nameof(criticalRate));
            if (criticalDamagePercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(criticalDamagePercent));

            _maximumHearts = restored.PlayerMaximumHearts;
            _currentHearts = restored.PlayerCurrentHearts;
            _criticalRate = criticalRate;
            _criticalDamagePercent = criticalDamagePercent;
            _effectiveAttack = 5;
            _stageCalculator = new StageProgressionCalculator();
            _damageCalculator = new DamageCalculator();
            _phase = restored.Phase == CombatPhase.PresentingResult
                ? CombatPhase.EnemyReady
                : restored.Phase;
            _attemptCommitted = _phase == CombatPhase.Committed;
            _enemy = new EnemyState(
                enemyDefinition,
                restored.EnemyMaximumHp,
                restored.EnemyCurrentHp,
                restored.EnemyRemainingCooldown);
        }

        public LocalCombatEngine(
            CombatSnapshot restored,
            EnemyDefinitionData enemyDefinition,
            IRandomSource random,
            PlayerCombatStats stats)
            : this(restored, enemyDefinition, random,
                stats.CriticalRate, stats.CriticalDamagePercent)
        {
            _effectiveAttack = stats.EffectiveAttack;
        }

        public CombatSnapshot Snapshot => CreateSnapshot();

        public CombatSnapshot CommitAttempt()
        {
            if (_phase != CombatPhase.EnemyReady || _attemptCommitted)
            {
                throw new InvalidOperationException("Combat is not ready for another attempt.");
            }

            _enemy.ConsumeCooldown();
            _attemptCommitted = true;
            _phase = CombatPhase.Committed;
            return CreateSnapshot();
        }

        public CombatSnapshot VoidContentFailure()
        {
            EnsureCommittedAttempt();
            _enemy.RestoreCommittedCooldown();
            _attemptCommitted = false;
            _phase = CombatPhase.EnemyReady;
            return CreateSnapshot();
        }

        public CombatResolution ResolveCorrect(
            int responseScore,
            double rankMultiplier)
        {
            EnsureCommittedAttempt();
            if (responseScore < 1 || responseScore > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(responseScore));
            }

            if (rankMultiplier <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(rankMultiplier));
            }

            bool isCritical = _random.NextUnit() < _criticalRate;
            DamageResult damage = _damageCalculator.Calculate(
                new DamageInput(
                    _effectiveAttack,
                    rankMultiplier,
                    1d,
                    _criticalDamagePercent,
                    isCritical,
                    responseScore
                )
            );

            return Resolve(
                responseScore,
                damage.FinalDamage,
                true,
                isCritical,
                false
            );
        }

        public CombatResolution ResolveIncorrect(bool timedOut)
        {
            EnsureCommittedAttempt();
            return Resolve(0, 0, false, false, timedOut);
        }

        public CombatResolution ResolveTimeout()
        {
            return ResolveIncorrect(true);
        }

        private CombatResolution Resolve(
            int responseScore,
            int finalDamage,
            bool isCorrect,
            bool isCritical,
            bool timedOut)
        {
            StageId resolvedStage = _stage;
            int enemyHpBefore = _enemy.CurrentHp;
            int enemyHpAfter = _enemy.ApplyDamage(finalDamage);
            bool enemyDefeated = _enemy.IsDefeated;
            bool enemyAttacked = false;
            bool playerDefeated = false;
            bool stageAdvanced = false;

            _phase = CombatPhase.Resolving;

            if (enemyDefeated)
            {
                if (_stage.TryNext(out StageId nextStage))
                {
                    _stage = nextStage;
                    stageAdvanced = true;
                    _enemy = SpawnEnemy();
                    _phase = CombatPhase.PresentingResult;
                }
                else
                {
                    _phase = CombatPhase.RunComplete;
                }
            }
            else if (_enemy.RemainingCooldown == 0)
            {
                enemyAttacked = true;
                _currentHearts = Math.Max(0, _currentHearts - 1);
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

            _attemptCommitted = false;

            return new CombatResolution(
                responseScore,
                finalDamage,
                isCorrect,
                isCritical,
                timedOut,
                enemyHpBefore,
                enemyHpAfter,
                enemyDefeated,
                enemyAttacked,
                playerDefeated,
                resolvedStage,
                stageAdvanced,
                CreateSnapshot()
            );
        }

        public CombatSnapshot CompletePresentation()
        {
            if (_phase == CombatPhase.PresentingResult)
            {
                _phase = CombatPhase.EnemyReady;
            }

            return CreateSnapshot();
        }

        private EnemyState SpawnEnemy()
        {
            int spawnHp = _stageCalculator.CalculateSpawnHp(
                _stage,
                _enemyDefinition.BaseHp,
                _random.NextUnit()
            );
            return new EnemyState(_enemyDefinition, spawnHp);
        }

        private void EnsureCommittedAttempt()
        {
            if (!_attemptCommitted || _phase != CombatPhase.Committed)
            {
                throw new InvalidOperationException("No committed attempt can be resolved.");
            }
        }

        private CombatSnapshot CreateSnapshot()
        {
            return new CombatSnapshot(
                _stage,
                _enemyDefinition.EnemyId,
                _enemyDefinition.DisplayName,
                _enemy.CurrentHp,
                _enemy.MaximumHp,
                _enemy.RemainingCooldown,
                _enemyDefinition.MaximumCooldown,
                _currentHearts,
                _maximumHearts,
                _phase,
                true
            );
        }
    }
}
