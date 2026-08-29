using System;
using System.Collections.Generic;

namespace PowerMath.Session
{
    public static class PlayerResetPayloadBuilder
    {
        public static FirestorePatchPlan BuildGameDataResetPlan(string username, string newPublicPlayerId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be empty.", nameof(username));
            }

            string publicId = string.IsNullOrWhiteSpace(newPublicPlayerId)
                ? Guid.NewGuid().ToString("N")
                : newPublicPlayerId.Trim();

            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { username, "gamedata" };

            builder.AddInteger(Join(root, "revision"), 0);

            // Profile
            builder.AddString(Join(root, "profile", "displayName"), username);
            builder.AddString(Join(root, "profile", "iconId"), "avatar-default");
            builder.AddString(Join(root, "profile", "publicPlayerId"), publicId);
            builder.AddInteger(Join(root, "profile", "displayNameChangedAtUnixSeconds"), 0);

            // Progression
            builder.AddInteger(Join(root, "progression", "currentStage"), 1);
            builder.AddInteger(Join(root, "progression", "highestStage"), 1);
            builder.AddString(Join(root, "progression", "activeRank"), "Silver");
            builder.AddInteger(Join(root, "progression", "rankProgress"), 0);
            builder.AddInteger(Join(root, "progression", "prestige"), 0);
            builder.AddBoolean(Join(root, "progression", "firstStage200Reached"), false);
            builder.AddInteger(Join(root, "progression", "firstStage200ReachedAtUnixSeconds"), 0);
            builder.AddInteger(Join(root, "progression", "totalDamage"), 0);
            builder.AddInteger(Join(root, "progression", "legacyAtkBonusBasisPoints"), 0);

            // Wallet
            builder.AddInteger(Join(root, "wallet", "silver"), 0);
            builder.AddInteger(Join(root, "wallet", "gold"), 0);
            builder.AddInteger(Join(root, "wallet", "diamond"), 0);
            builder.AddInteger(Join(root, "wallet", "powerCoins"), 0);

            // Inventory & Loadout
            builder.AddEmptyArray(Join(root, "inventory"));
            builder.AddString(Join(root, "loadout", "petId"), string.Empty);
            builder.AddString(Join(root, "loadout", "weaponId"), string.Empty);
            builder.AddString(Join(root, "loadout", "avatarId"), string.Empty);

            // Active Run
            builder.AddString(Join(root, "activeRun", "runId"), Guid.NewGuid().ToString("N"));
            builder.AddInteger(Join(root, "activeRun", "currentStage"), 1);
            builder.AddString(Join(root, "activeRun", "committedAttemptId"), string.Empty);
            builder.AddString(Join(root, "activeRun", "biomeId"), string.Empty);
            builder.AddString(Join(root, "activeRun", "biomeTitle"), string.Empty);
            builder.AddString(Join(root, "activeRun", "encounterKind"), "NormalMonster");
            builder.AddString(Join(root, "activeRun", "encounterId"), string.Empty);
            builder.AddString(Join(root, "activeRun", "questionContentKind"), string.Empty);
            builder.AddString(Join(root, "activeRun", "questionDocumentId"), string.Empty);
            builder.AddInteger(Join(root, "activeRun", "questionId"), 0);
            builder.AddInteger(Join(root, "activeRun", "eventAttemptOrdinal"), 0);
            builder.AddString(Join(root, "activeRun", "enemyId"), string.Empty);
            builder.AddInteger(Join(root, "activeRun", "enemyCurrentHp"), 0);
            builder.AddInteger(Join(root, "activeRun", "enemyMaximumHp"), 0);
            builder.AddInteger(Join(root, "activeRun", "enemyRemainingCooldown"), 0);
            builder.AddInteger(Join(root, "activeRun", "enemyMaximumCooldown"), 0);
            builder.AddInteger(Join(root, "activeRun", "playerCurrentHearts"), 0);
            builder.AddInteger(Join(root, "activeRun", "playerMaximumHearts"), 0);
            builder.AddString(Join(root, "activeRun", "phase"), "EnemyReady");
            builder.AddInteger(Join(root, "activeRun", "silverEarned"), 0);
            builder.AddInteger(Join(root, "activeRun", "goldEarned"), 0);
            builder.AddInteger(Join(root, "activeRun", "diamondEarned"), 0);
            builder.AddInteger(Join(root, "activeRun", "bonusMultiplierBasisPoints"), 10000);

            // Economy
            builder.AddString(Join(root, "economy", "lastWeaponAscendTransactionId"), string.Empty);
            builder.AddInteger(Join(root, "economy", "lastWeaponAscendLevel"), 0);
            builder.AddInteger(Join(root, "economy", "lastWeaponAscendCost"), 0);
            builder.AddString(Join(root, "economy", "lastPetGachaTransactionId"), string.Empty);
            builder.AddString(Join(root, "economy", "lastPetGachaCatalogVersion"), string.Empty);
            builder.AddString(Join(root, "economy", "lastPetGachaPetId"), string.Empty);
            builder.AddBoolean(Join(root, "economy", "lastPetGachaWasNew"), false);
            builder.AddInteger(Join(root, "economy", "lastPetGachaCost"), 0);
            builder.AddInteger(Join(root, "economy", "lastPetGachaResultingPowerCoins"), 0);
            builder.AddString(Join(root, "economy", "lastPetEquipTransactionId"), string.Empty);
            builder.AddString(Join(root, "economy", "lastPetEquipPetId"), string.Empty);

            // Run Settlement
            builder.AddString(Join(root, "lastRunSettlement", "runId"), string.Empty);
            builder.AddString(Join(root, "lastRunSettlement", "type"), string.Empty);
            builder.AddInteger(Join(root, "lastRunSettlement", "stageReached"), 0);
            builder.AddInteger(Join(root, "lastRunSettlement", "powerCoinsGranted"), 0);
            builder.AddInteger(Join(root, "lastRunSettlement", "legacyAtkBasisPointsGranted"), 0);
            builder.AddInteger(Join(root, "lastRunSettlement", "prestigeGranted"), 0);
            builder.AddInteger(Join(root, "lastRunSettlement", "resultingPowerCoins"), 0);

            // Academic
            builder.AddInteger(Join(root, "academic", "auditScore"), 0);
            builder.AddInteger(Join(root, "academic", "auditResolvedCount"), 0);
            builder.AddNull(Join(root, "academic", "activeAttempt"));
            foreach (string rank in new[] { "silver", "gold", "diamond" })
            {
                string[] rankRoot = { "academic", "inventories", rank };
                builder.AddInteger(Join(root, Combine(rankRoot, "cycle")), 0);
                builder.AddEmptyArray(Join(root, Combine(rankRoot, "pendingIds")));
                builder.AddEmptyArray(Join(root, Combine(rankRoot, "failedIds")));
                builder.AddEmptyArray(Join(root, Combine(rankRoot, "attemptedInAuditIds")));
                builder.AddEmptyArray(Join(root, Combine(rankRoot, "clearedInCycleIds")));
            }

            // Analytics
            foreach (string field in new[]
            {
                "totalQuestionsResolved", "totalCorrect", "totalIncorrect",
                "totalTimeout", "totalAbandoned", "responseScoreSum",
                "responseEfficiencySum", "responseDurationMillisecondsSum", "totalPlaySeconds"
            })
            {
                builder.AddInteger(Join(root, "analytics", field), 0);
            }
            builder.AddString(Join(root, "analytics", "lastAppliedAttemptId"), string.Empty);
            builder.AddEmptyArray(Join(root, "analytics", "responseScoreHistogram"));
            builder.AddEmptyArray(Join(root, "analytics", "responseEfficiencyHistogram"));
            builder.AddEmptyArray(Join(root, "analytics", "responseDuration100msHistogram"));
            foreach (string rank in new[] { "silver", "gold", "diamond" })
            {
                foreach (string field in new[] { "resolved", "correct", "responseScoreSum", "responseEfficiencySum" })
                {
                    builder.AddInteger(Join(root, "analytics", "byRank", rank, field), 0);
                }
            }

            return builder.Build();
        }

        public static FirestorePatchPlan BuildLeaderboardDeletionPlan(string oldPublicPlayerId)
        {
            if (string.IsNullOrWhiteSpace(oldPublicPlayerId))
            {
                throw new ArgumentException("Public Player ID cannot be empty.", nameof(oldPublicPlayerId));
            }

            var builder = new FirestorePatchDocumentBuilder();
            builder.AddNull(new[] { oldPublicPlayerId.Trim() });
            return builder.Build();
        }

        private static string[] Join(string[] root, params string[] values)
        {
            var result = new string[root.Length + values.Length];
            Array.Copy(root, result, root.Length);
            Array.Copy(values, 0, result, root.Length, values.Length);
            return result;
        }

        private static string[] Combine(IReadOnlyList<string> left, params string[] right)
        {
            var result = new string[left.Count + right.Length];
            for (int index = 0; index < left.Count; index++) result[index] = left[index];
            for (int index = 0; index < right.Length; index++) result[left.Count + index] = right[index];
            return result;
        }
    }
}
