using System;

namespace PowerMath.PlayerData
{
    [Serializable]
    public sealed class PlayerSnapshot
    {
        public string playerId;
        public long revision;
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
        public sealed class ProfileData
        {
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
            public int eventAttemptOrdinal;
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
        }

        [Serializable]
        public sealed class AcademicData
        {
            public int auditScore;
            public int auditResolvedCount;
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
