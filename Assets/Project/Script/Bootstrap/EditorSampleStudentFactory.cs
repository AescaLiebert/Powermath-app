#if UNITY_EDITOR
using System;
using PowerMath.PlayerData;
using PowerMath.Session;

namespace PowerMath.Bootstrap
{
    internal static class EditorSampleStudentFactory
    {
        public static BootstrapResponse CreateResponse()
        {
            DateTime now = DateTime.UtcNow;

            return new BootstrapResponse
            {
                schemaVersion = PlayerSessionStore.SupportedSchemaVersion,
                remembered = false,
                player = new PlayerSnapshot
                {
                    playerId = "editor-sample-student",
                    revision = 1,
                    profile = new PlayerSnapshot.ProfileData
                    {
                        displayName = "Developer Sample Student",
                        gradeBand = "Grade 6",
                        iconId = "avatar-default"
                    },
                    progression = new PlayerSnapshot.ProgressionData
                    {
                        currentStage = 24,
                        highestStage = 27,
                        activeRank = "Silver",
                        rankProgress = 60,
                        prestige = 0,
                        firstStage200Reached = false
                    },
                    wallet = new PlayerSnapshot.WalletData
                    {
                        silver = 2500,
                        gold = 50,
                        diamond = 5,
                        powerCoins = 1200
                    },
                    inventory = new[]
                    {
                        new PlayerSnapshot.InventoryItemData
                        {
                            itemId = "starter-sword",
                            upgradeLevel = 2,
                            owned = true
                        },
                        new PlayerSnapshot.InventoryItemData
                        {
                            itemId = "starter-pet",
                            upgradeLevel = 1,
                            owned = true
                        }
                    },
                    loadout = new PlayerSnapshot.LoadoutData
                    {
                        petId = "starter-pet",
                        weaponId = "starter-sword",
                        avatarId = "avatar-default"
                    },
                    activeRun = new PlayerSnapshot.ActiveRunData
                    {
                        runId = "editor-sample-run",
                        currentStage = 24,
                        committedAttemptId = string.Empty
                    }
                },
                serverTimeUtc = now.ToString("O")
            };
        }
    }
}
#endif
