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

        [Serializable]
        public sealed class ProfileData
        {
            public string displayName;
            public string gradeBand;
            public string iconId;
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
            public string enemyId;
            public int enemyCurrentHp;
            public int enemyMaximumHp;
            public int enemyRemainingCooldown;
            public int enemyMaximumCooldown;
            public int playerCurrentHearts;
            public int playerMaximumHearts;
            public string phase;
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
    }
}
