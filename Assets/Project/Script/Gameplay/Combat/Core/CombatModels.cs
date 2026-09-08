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
        Unavailable,
        EventReady
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
            : this(stage, enemyId, enemyName, enemyCurrentHp, enemyMaximumHp,
                enemyRemainingCooldown, enemyMaximumCooldown, playerCurrentHearts,
                playerMaximumHearts, phase, isSimulation, string.Empty, string.Empty,
                StageEncounterKind.NormalMonster, string.Empty, 0)
        {
        }

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
            bool isSimulation,
            string biomeId,
            string biomeTitle,
            StageEncounterKind encounterKind,
            string questionDocumentId,
            int eventAttemptOrdinal)
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
            BiomeId = biomeId ?? string.Empty;
            BiomeTitle = biomeTitle ?? string.Empty;
            EncounterKind = encounterKind;
            QuestionDocumentId = questionDocumentId ?? string.Empty;
            EventAttemptOrdinal = eventAttemptOrdinal;
        }

        public StageId Stage { get; }
        public string EnemyId { get; }
        public string EncounterId => EnemyId;
        public string EnemyName { get; }
        public int EnemyCurrentHp { get; }
        public int EnemyMaximumHp { get; }
        public int EnemyRemainingCooldown { get; }
        public int EnemyMaximumCooldown { get; }
        public int PlayerCurrentHearts { get; }
        public int PlayerMaximumHearts { get; }
        public CombatPhase Phase { get; }
        public bool IsSimulation { get; }
        public string BiomeId { get; }
        public string BiomeTitle { get; }
        public StageEncounterKind EncounterKind { get; }
        public string QuestionDocumentId { get; }
        public int EventAttemptOrdinal { get; }
        public bool IsEvent => EncounterKind == StageEncounterKind.ChallengeEvent;
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
            : this(responseScore, finalDamage, isCorrect, isCritical, timedOut,
                enemyHpBefore, enemyHpAfter, enemyDefeated, enemyAttacked,
                playerDefeated, resolvedStage, stageAdvanced, snapshot, false)
        {
        }

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
            CombatSnapshot snapshot,
            bool biomeChanged,
            DamageBreakdown damageBreakdown = default)
        {
            ResponseScore = responseScore;
            ResponseDamageMultiplier = isCorrect
                ? ResponseDamagePolicy.GetMultiplier(responseScore)
                : 0d;
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
            BiomeChanged = biomeChanged;
            DamageBreakdown = damageBreakdown;
        }

        public int ResponseScore { get; }
        public double ResponseDamageMultiplier { get; }
        public int ResponseDamagePercent => IsCorrect
            ? ResponseDamagePolicy.GetPercent(ResponseScore)
            : 0;
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
        public bool BiomeChanged { get; }
        public DamageBreakdown DamageBreakdown { get; }
    }
}
