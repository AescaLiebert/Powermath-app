namespace PowerMath.Gameplay.Combat
{
    public sealed class PetFollowUpResolution
    {
        public PetFollowUpResolution(
            int damage,
            bool isCritical,
            CombatSnapshot target,
            int enemyHpAfter,
            bool enemyDefeated,
            bool stageAdvanced,
            bool carried)
        {
            if (damage <= 0) throw new System.ArgumentOutOfRangeException(nameof(damage));
            Target = target ?? throw new System.ArgumentNullException(nameof(target));
            if (enemyHpAfter < 0 || enemyHpAfter > target.EnemyMaximumHp)
                throw new System.ArgumentOutOfRangeException(nameof(enemyHpAfter));
            Damage = damage;
            IsCritical = isCritical;
            EnemyHpAfter = enemyHpAfter;
            EnemyDefeated = enemyDefeated;
            StageAdvanced = stageAdvanced;
            Carried = carried;
        }

        public int Damage { get; }
        public bool IsCritical { get; }
        public CombatSnapshot Target { get; }
        public int EnemyHpAfter { get; }
        public bool EnemyDefeated { get; }
        public bool StageAdvanced { get; }
        public bool Carried { get; }
    }

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
            int eventAttemptOrdinal,
            EventScheduleSnapshot eventSchedule = null,
            int stageAttackCount = 0,
            int bigBossesDefeated = 0,
            int pendingPetFollowUpDamage = 0)
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
            EventSchedule = eventSchedule;
            StageAttackCount = stageAttackCount;
            BigBossesDefeated = bigBossesDefeated;
            PendingPetFollowUpDamage = pendingPetFollowUpDamage;
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
        public EventScheduleSnapshot EventSchedule { get; }
        public int StageAttackCount { get; }
        public int BigBossesDefeated { get; }
        public int PendingPetFollowUpDamage { get; }
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
            DamageBreakdown damageBreakdown = default,
            bool enemyFled = false,
            int playerDamage = -1,
            int playerEnemyHpAfter = -1,
            PetFollowUpResolution petFollowUp = null)
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
            EnemyFled = enemyFled;
            PlayerDamage = playerDamage < 0 ? finalDamage : playerDamage;
            PlayerEnemyHpAfter = playerEnemyHpAfter < 0
                ? enemyHpAfter
                : playerEnemyHpAfter;
            PetFollowUp = petFollowUp;
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
        public bool EnemyFled { get; }
        public int PlayerDamage { get; }
        public int PlayerEnemyHpAfter { get; }
        public PetFollowUpResolution PetFollowUp { get; }
        public bool HasPetFollowUp => PetFollowUp != null;
    }
}
