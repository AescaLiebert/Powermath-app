namespace PowerMath.Gameplay.Combat
{
    public enum CombatPhase
    {
        EnemyReady,
        Committed,
        Preparation,
        Answering,
        Resolving,
        PresentingResult,
        RunDefeat,
        RunComplete,
        Unavailable
    }

    public sealed class CombatSnapshot
    {
        public CombatSnapshot(
            StageId stage,
            string enemyId,
            string enemyName,
            int enemyCurrentHp,
            int enemyMaximumHp,
            int enemyRemainingCooldown,
            int enemyMaximumCooldown,
            int playerCurrentHearts,
            int playerMaximumHearts,
            CombatPhase phase,
            bool isSimulation)
        {
            Stage = stage;
            EnemyId = enemyId;
            EnemyName = enemyName;
            EnemyCurrentHp = enemyCurrentHp;
            EnemyMaximumHp = enemyMaximumHp;
            EnemyRemainingCooldown = enemyRemainingCooldown;
            EnemyMaximumCooldown = enemyMaximumCooldown;
            PlayerCurrentHearts = playerCurrentHearts;
            PlayerMaximumHearts = playerMaximumHearts;
            Phase = phase;
            IsSimulation = isSimulation;
        }

        public StageId Stage { get; }
        public string EnemyId { get; }
        public string EnemyName { get; }
        public int EnemyCurrentHp { get; }
        public int EnemyMaximumHp { get; }
        public int EnemyRemainingCooldown { get; }
        public int EnemyMaximumCooldown { get; }
        public int PlayerCurrentHearts { get; }
        public int PlayerMaximumHearts { get; }
        public CombatPhase Phase { get; }
        public bool IsSimulation { get; }
    }

    public sealed class CombatResolution
    {
        public CombatResolution(
            int responseScore,
            int finalDamage,
            bool isCorrect,
            bool isCritical,
            bool timedOut,
            int enemyHpBefore,
            int enemyHpAfter,
            bool enemyDefeated,
            bool enemyAttacked,
            bool playerDefeated,
            StageId resolvedStage,
            bool stageAdvanced,
            CombatSnapshot snapshot)
        {
            ResponseScore = responseScore;
            FinalDamage = finalDamage;
            IsCorrect = isCorrect;
            IsCritical = isCritical;
            TimedOut = timedOut;
            EnemyHpBefore = enemyHpBefore;
            EnemyHpAfter = enemyHpAfter;
            EnemyDefeated = enemyDefeated;
            EnemyAttacked = enemyAttacked;
            PlayerDefeated = playerDefeated;
            ResolvedStage = resolvedStage;
            StageAdvanced = stageAdvanced;
            Snapshot = snapshot;
        }

        public int ResponseScore { get; }
        public int FinalDamage { get; }
        public bool IsCorrect { get; }
        public bool IsCritical { get; }
        public bool TimedOut { get; }
        public int EnemyHpBefore { get; }
        public int EnemyHpAfter { get; }
        public bool EnemyDefeated { get; }
        public bool EnemyAttacked { get; }
        public bool PlayerDefeated { get; }
        public StageId ResolvedStage { get; }
        public bool StageAdvanced { get; }
        public CombatSnapshot Snapshot { get; }
    }
}
