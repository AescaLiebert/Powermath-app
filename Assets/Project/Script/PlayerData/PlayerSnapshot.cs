using System;

namespace PowerMath.PlayerData
{
    [Serializable]
    public sealed class PlayerSnapshot
    {
        public int schemaVersion;
        public PreferencesData preferences;
        public OnboardingData onboarding;
        public TutorialData tutorial;
        public TutorialEntryData[] tutorialEntries;
        public string playerId;
        public bool isAdmin;
        public long revision;
        public AdminTuningData adminTuning;
        public ProfileData profile;
        public ProgressionData progression;
        public WalletData wallet;
        public InventoryItemData[] inventory;
        public LoadoutData loadout;
        public ActiveRunData activeRun;
        public AcademicData academic;
        public AnalyticsData analytics;
        public EconomyData economy;
        public RunSettlementData lastRunSettlement;

        [Serializable]
        public sealed class PreferencesData
        {
            public string locale;
        }

        [Serializable]
        public sealed class AdminTuningData
        {
            public bool combatOverrideEnabled;
            public int attack;
            public int criticalRateBasisPoints;
            public int criticalDamageBasisPoints;
            public bool invincible;
            public bool bypassVideoQuestion;
        }

        [Serializable]
        public sealed class OnboardingData
        {
            public int version;
            public string phase;
            public string openingCheckpointId;
            public string selectedCharacterId;
            public string completionOperationId;
            public bool legacyPlayer;
        }

        [Serializable]
        public sealed class TutorialData
        {
            public int version;
            public string checkpointId;
        }

        [Serializable]
        public sealed class TutorialEntryData
        {
            public string tutorialId;
            public int version;
            public string status;
            public string currentStepId;
            public long triggerRecordedAtUnixSeconds;
            public long completedAtUnixSeconds;
            public bool rewardClaimed;
            public string lastTransactionId;
            public string lastOperationId;
            public bool legacyPlayer;
            public string guidedEncounterId;
            public string firstAttemptOutcome;
            public string variant;
        }

        [Serializable]
        public sealed class ProfileData
        {
            public string characterId;
            public string displayName;
            public string gradeBand;
            public string iconId;
            public string publicPlayerId;
            public long displayNameChangedAtUnixSeconds;
        }

        [Serializable]
        public sealed class ProgressionData
        {
            public int currentStage;
            public int highestStage;
            public string activeRank;
            public int rankProgress;
            public int prestige;
            public bool firstStage200Reached;
            public long firstStage200ReachedAtUnixSeconds;
            public long totalDamage;
            public long legacyAtkBonusBasisPoints;
            public long leaderboardSnapshotAtUnixSeconds;
            public long lastSnapshotHighestStage;
            public long lastSnapshotWeightedScore;
        }

        [Serializable]
        public sealed class WalletData
        {
            public long silver;
            public long gold;
            public long diamond;
            public long powerCoins;
        }

        [Serializable]
        public sealed class InventoryItemData
        {
            public string itemId;
            public int upgradeLevel;
            public bool owned;
            public int count;
        }

        [Serializable]
        public sealed class LoadoutData
        {
            public string petId;
            public string weaponId;
            public string avatarId;
        }

        [Serializable]
        public sealed class ActiveRunData
        {
            public string runId;
            public int currentStage;
            public string committedAttemptId;
            public string biomeId;
            public string biomeTitle;
            public string encounterKind;
            public string encounterId;
            public string questionContentKind;
            public string questionDocumentId;
            public long questionId;
            public string questionContentId;
            public int eventAttemptOrdinal;
            public int eventScheduleVersion;
            public string eventScheduleCatalogVersion;
            public string eventScheduleEventId;
            public int[] eventScheduleStages;
            public int eventChanceBasisPoints;
            public int petEventMultiplierBasisPoints;
            public ChallengeQuestionSequenceData challengeQuestions;
            public string lastChallengeRewardAttemptId;
            public int lastChallengeRewardPowerCoins;
            public long lastChallengeRewardResultingPowerCoins;
            public string enemyId;
            public int enemyCurrentHp;
            public int enemyMaximumHp;
            public int enemyRemainingCooldown;
            public int enemyMaximumCooldown;
            public int playerCurrentHearts;
            public int playerMaximumHearts;
            public string phase;
            public long silverEarned;
            public long goldEarned;
            public long diamondEarned;
            public int bonusMultiplierBasisPoints;
            public AttemptPresentationData pendingPresentation;
            public int stageAttackCount;
            public int bigBossesDefeated;
            public int pendingPetFollowUpDamage;
            public bool wasTeleported;
        }

        [Serializable]
        public sealed class AttemptPresentationData
        {
            public int version;
            public string presentationId;
            public string attemptId;
            public string outcome;
            public int responseScore;
            public int finalDamage;
            public int playerDamage;
            public int playerEnemyHpAfter;
            public bool isCritical;
            public int resolvedEnemyHpAfter;
            public bool enemyDefeated;
            public bool enemyAttacked;
            public bool playerDefeated;
            public bool stageAdvanced;
            public bool biomeChanged;
            public bool enemyFled;
            public int powerCoinsGranted;
            public long resultingPowerCoins;
            public CombatPresentationData source;
            public CombatPresentationData destination;
            public PetFollowUpPresentationData petFollowUp;
            public string previousRank;
            public string currentRank;
        }

        [Serializable]
        public sealed class PetFollowUpPresentationData
        {
            public int damage;
            public bool isCritical;
            public int enemyHpAfter;
            public bool enemyDefeated;
            public bool stageAdvanced;
            public bool carried;
            public CombatPresentationData target;
        }

        [Serializable]
        public sealed class ChallengeQuestionSequenceData
        {
            public int silverCursor;
            public int goldCursor;
            public int diamondCursor;
            public string reservedDocumentId;
            public string reservedQuestionId;
        }

        [Serializable]
        public sealed class CombatPresentationData
        {
            public int stage;
            public string biomeId;
            public string encounterId;
            public string encounterKind;
            public int enemyCurrentHp;
            public int enemyMaximumHp;
            public int enemyRemainingCooldown;
            public int enemyMaximumCooldown;
            public int playerCurrentHearts;
            public int playerMaximumHearts;
            public string phase;
        }

        [Serializable]
        public sealed class EconomyData
        {
            public string lastWeaponAscendTransactionId;
            public int lastWeaponAscendLevel;
            public long lastWeaponAscendCost;
            public string lastPetGachaTransactionId;
            public string lastPetGachaCatalogVersion;
            public string lastPetGachaPetId;
            public bool lastPetGachaWasNew;
            public long lastPetGachaCost;
            public long lastPetGachaResultingPowerCoins;
            public int petGachaPullsSinceSsr;
            public int lastPetGachaPreviousPityCount;
            public int lastPetGachaResultingPityCount;
            public PetGachaResultData[] lastPetGachaResults;
            public string lastPetEquipTransactionId;
            public string lastPetEquipPetId;
            public bool firstGachaPullCompleted;
        }

        [Serializable]
        public sealed class PetGachaResultData
        {
            public string petId;
            public string rarityId;
            public bool wasNew;
            public int previousCount;
            public int resultingCount;
        }

        [Serializable]
        public sealed class RunSettlementData
        {
            public string runId;
            public string type;
            public int stageReached;
            public long powerCoinsGranted;
            public long legacyAtkBasisPointsGranted;
            public int prestigeGranted;
            public long resultingPowerCoins;
            public int presentationVersion;
            public string presentationId;
            public string presentationStatus;
            public string presentationCause;
            public string sourceBiomeId;
            public string sourceEncounterId;
            public string sourceEncounterKind;
            public long sourcePowerCoins;
            public long sourceLegacyAtkBasisPoints;
            public int sourcePrestige;
            public long sourceEffectiveAttack;
            public long resultingEffectiveAttack;
            public long acknowledgedAtUnixSeconds;
        }

        [Serializable]
        public sealed class AcademicData
        {
            public int auditScore;
            public int auditResolvedCount;
            public int auditCorrectCount;
            public RankInventoryData silver;
            public RankInventoryData gold;
            public RankInventoryData diamond;
        }

        [Serializable]
        public sealed class RankInventoryData
        {
            public int cycle;
            public long[] pendingIds;
            public long[] failedIds;
            public long[] attemptedInAuditIds;
            public long[] clearedInCycleIds;
        }

        [Serializable]
        public sealed class AnalyticsData
        {
            public long totalQuestionsResolved;
            public long totalCorrect;
            public long totalIncorrect;
            public long totalTimeout;
            public long totalAbandoned;
            public long responseScoreSum;
            public long responseEfficiencySum;
            public long responseDurationMillisecondsSum;
            public long[] responseScoreHistogram;
            public long[] responseEfficiencyHistogram;
            public long[] responseDuration100msHistogram;
            public long totalPlaySeconds;
            public string lastAppliedAttemptId;
            public RankAnalyticsData silver;
            public RankAnalyticsData gold;
            public RankAnalyticsData diamond;
            public QuestionAnalyticsData[] byQuestion;
        }

        [Serializable]
        public sealed class RankAnalyticsData
        {
            public long resolved;
            public long correct;
            public long responseScoreSum;
            public long responseEfficiencySum;
        }

        [Serializable]
        public sealed class QuestionAnalyticsData
        {
            public long questionId;
            public long resolved;
            public long correct;
            public long incorrect;
            public long timeout;
            public long abandoned;
            public long responseScoreSum;
            public long responseDurationMillisecondsSum;
            public long responseEfficiencySum;
        }
    }
}
