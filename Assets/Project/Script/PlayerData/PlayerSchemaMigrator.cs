using System;
using UnityEngine;

namespace PowerMath.PlayerData
{
    public static class PlayerSchemaMigrator
    {
        public const int CurrentSchemaVersion = 3;

        public static PlayerSnapshot Migrate(PlayerSnapshot snapshot, int fromVersion, int targetVersion = CurrentSchemaVersion)
        {
            if (snapshot == null) return null;
            if (fromVersion < 0 || targetVersion > CurrentSchemaVersion || fromVersion > targetVersion)
                throw new NotSupportedException("Unsupported schema migration.");
            if (fromVersion == targetVersion)
            {
                EnsureBaselineDefaults(snapshot);
                return snapshot;
            }

            if (fromVersion < 0 || fromVersion > targetVersion || targetVersion > CurrentSchemaVersion)
            {
                throw new NotSupportedException($"Unsupported schema migration {fromVersion} -> {targetVersion}.");
            }

            PlayerSnapshot current = snapshot;
            int version = fromVersion;

            while (version < targetVersion)
            {
                switch (version)
                {
                    case 0:
                        current = MigrateV0ToV1(current);
                        version = 1;
                        break;
                    case 1:
                        current = MigrateV1ToV2(current);
                        version = 2;
                        break;
                    case 2:
                        EnsureLifecycleDefaults(current);
                        version = 3;
                        break;
                    default:
                        throw new NotSupportedException("Missing schema migration step.");
                }
            }

            EnsureBaselineDefaults(current);
            current.schemaVersion = targetVersion;
            return current;
        }

        private static PlayerSnapshot MigrateV0ToV1(PlayerSnapshot s)
        {
            EnsureBaselineDefaults(s);
            return s;
        }

        private static PlayerSnapshot MigrateV1ToV2(PlayerSnapshot s)
        {
            EnsureBaselineDefaults(s);
            // Existing receipts can be present in unversioned/older prototype saves. Preserve them.
            s.lastRunSettlement.presentationStatus = string.IsNullOrWhiteSpace(
                s.lastRunSettlement.presentationStatus)
                ? "None"
                : s.lastRunSettlement.presentationStatus;
            return s;
        }

        private static void EnsureLifecycleDefaults(PlayerSnapshot s)
        {
            s.preferences ??= new PlayerSnapshot.PreferencesData();
            s.onboarding ??= new PlayerSnapshot.OnboardingData
            {
                version = 1, phase = "character", legacyPlayer = true,
                openingCheckpointId = "complete"
            };
            s.tutorial ??= new PlayerSnapshot.TutorialData { version = 1 };
        }

        public static void EnsureBaselineDefaults(PlayerSnapshot s)
        {
            if (s == null) return;
            EnsureLifecycleDefaults(s);

            if (s.profile == null)
            {
                s.profile = new PlayerSnapshot.ProfileData
                {
                    displayName = s.playerId ?? "Student",
                    iconId = "avatar-default",
                    publicPlayerId = Guid.NewGuid().ToString("N"),
                    gradeBand = "Grade 4"
                };
            }

            if (s.progression == null)
            {
                s.progression = new PlayerSnapshot.ProgressionData
                {
                    currentStage = 1,
                    highestStage = 1,
                    activeRank = "Silver",
                    firstStage200Reached = false
                };
            }

            if (s.wallet == null)
            {
                s.wallet = new PlayerSnapshot.WalletData
                {
                    silver = 0,
                    gold = 0,
                    diamond = 0,
                    powerCoins = 0
                };
            }

            if (s.loadout == null)
            {
                s.loadout = new PlayerSnapshot.LoadoutData
                {
                    petId = string.Empty,
                    weaponId = string.Empty,
                    avatarId = string.Empty
                };
            }

            if (s.activeRun == null)
            {
                s.activeRun = new PlayerSnapshot.ActiveRunData
                {
                    runId = Guid.NewGuid().ToString("N"),
                    currentStage = 1,
                    encounterKind = "NormalMonster",
                    phase = "EnemyReady",
                    bonusMultiplierBasisPoints = 10000
                };
            }

            if (s.inventory == null)
            {
                s.inventory = Array.Empty<PlayerSnapshot.InventoryItemData>();
            }

            if (s.academic == null)
            {
                s.academic = new PlayerSnapshot.AcademicData
                {
                    silver = CreateEmptyRankInventory(),
                    gold = CreateEmptyRankInventory(),
                    diamond = CreateEmptyRankInventory()
                };
            }

            if (s.economy == null)
            {
                s.economy = new PlayerSnapshot.EconomyData();
            }

            if (s.lastRunSettlement == null)
            {
                s.lastRunSettlement = new PlayerSnapshot.RunSettlementData();
            }
            if (string.IsNullOrWhiteSpace(s.lastRunSettlement.presentationStatus))
                s.lastRunSettlement.presentationStatus = "None";

            if (s.analytics == null)
            {
                s.analytics = new PlayerSnapshot.AnalyticsData
                {
                    silver = new PlayerSnapshot.RankAnalyticsData(),
                    gold = new PlayerSnapshot.RankAnalyticsData(),
                    diamond = new PlayerSnapshot.RankAnalyticsData(),
                    byQuestion = Array.Empty<PlayerSnapshot.QuestionAnalyticsData>()
                };
            }
        }

        private static PlayerSnapshot.RankInventoryData CreateEmptyRankInventory()
        {
            return new PlayerSnapshot.RankInventoryData
            {
                cycle = 0,
                pendingIds = Array.Empty<long>(),
                failedIds = Array.Empty<long>(),
                attemptedInAuditIds = Array.Empty<long>(),
                clearedInCycleIds = Array.Empty<long>()
            };
        }
    }
}
