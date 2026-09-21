using System;
using System.Collections.Generic;

namespace PowerMath.Session
{
    public static class PlayerDefaultsPlanner
    {
        public static FirestorePatchPlan Plan(JsonValue studentValue, string username) =>
            Plan(studentValue, username, customRoot: null);

        public static FirestorePatchPlan Plan(JsonValue studentValue, string username, string[] customRoot)
        {
            if (!FirestoreJsonNavigator.TryGetMapFields(studentValue, out JsonValue studentFields))
            {
                throw new ArgumentException("Student must be a Firestore map value.", nameof(studentValue));
            }

            int storedVersion = PlayerSaveContract.Inspect(studentValue, out bool isNewPlayer);
            var builder = new FirestorePatchDocumentBuilder();
            string[] root = customRoot ?? new[] { "gamedata" };
            studentFields.TryGet("gamedata", out JsonValue gameDataValue);
            FirestoreJsonNavigator.TryGetMapFields(gameDataValue, out JsonValue gameData);

            if (storedVersion < PowerMath.PlayerData.PlayerSchemaMigrator.CurrentSchemaVersion)
            {
                ApplyDurableMigrations(builder, root, gameData, storedVersion);
                builder.AddInteger(Combine(root, "schemaVersion"), PowerMath.PlayerData.PlayerSchemaMigrator.CurrentSchemaVersion);
                if (gameData != null && gameData.TryGet("revision", out var revisionValue))
                {
                    if (!FirestoreJsonNavigator.TryReadInteger(revisionValue, out long revision) || revision < 0 || revision == long.MaxValue)
                        throw new FormatException("Invalid player revision.");
                    builder.AddInteger(Combine(root, "revision"), revision + 1);
                }
            }
            AddString(builder, root, gameData, new[] { "preferences", "locale" }, string.Empty);
            AddString(builder, root, gameData, new[] { "profile", "characterId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "onboarding", "version" }, 1);
            AddString(builder, root, gameData, new[] { "onboarding", "phase" }, isNewPlayer ? "opening" : "character");
            AddBoolean(builder, root, gameData, new[] { "onboarding", "legacyPlayer" }, !isNewPlayer);
            AddString(builder, root, gameData, new[] { "onboarding", "openingCheckpointId" }, isNewPlayer ? string.Empty : "complete");
            AddString(builder, root, gameData, new[] { "onboarding", "selectedCharacterId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "onboarding", "completionOperationId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "tutorial", "version" }, 1);
            AddString(builder, root, gameData, new[] { "tutorial", "checkpointId" }, string.Empty);
            AddEmptyMap(builder, root, gameData, "tutorialMap");
            AddInteger(builder, root, gameData, "revision", 0);
            AddString(builder, root, gameData, new[] { "profile", "displayName" }, username);
            AddString(builder, root, gameData, new[] { "profile", "iconId" }, "avatar-default");
            AddString(builder, root, gameData, new[] { "profile", "publicPlayerId" }, Guid.NewGuid().ToString("N"));
            AddInteger(builder, root, gameData, new[] { "profile", "displayNameChangedAtUnixSeconds" }, 0);
            AddInteger(builder, root, gameData, new[] { "progression", "currentStage" }, 1);
            AddInteger(builder, root, gameData, new[] { "progression", "highestStage" }, 1);
            AddString(builder, root, gameData, new[] { "progression", "activeRank" }, "Silver");
            AddInteger(builder, root, gameData, new[] { "progression", "prestige" }, 0);
            AddBoolean(builder, root, gameData, new[] { "progression", "firstStage200Reached" }, false);
            AddInteger(builder, root, gameData, new[] { "progression", "firstStage200ReachedAtUnixSeconds" }, 0);
            AddInteger(builder, root, gameData, new[] { "progression", "totalDamage" }, 0);
            AddInteger(builder, root, gameData, new[] { "progression", "legacyAtkBonusBasisPoints" }, 0);
            AddInteger(builder, root, gameData, new[] { "progression", "leaderboardSnapshotAtUnixSeconds" }, 0);
            AddInteger(builder, root, gameData, new[] { "progression", "lastSnapshotHighestStage" }, 0);
            AddInteger(builder, root, gameData, new[] { "progression", "lastSnapshotWeightedScore" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "silver" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "gold" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "diamond" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "powerCoins" }, 0);
            AddEmptyArray(builder, root, gameData, "inventory");
            AddString(builder, root, gameData, new[] { "loadout", "petId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "loadout", "weaponId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "loadout", "avatarId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "runId" }, Guid.NewGuid().ToString("N"));
            AddInteger(builder, root, gameData, new[] { "activeRun", "currentStage" }, 1);
            AddString(builder, root, gameData, new[] { "activeRun", "committedAttemptId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "biomeId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "biomeTitle" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "encounterKind" }, "NormalMonster");
            AddString(builder, root, gameData, new[] { "activeRun", "encounterId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "questionContentKind" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "questionDocumentId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "activeRun", "questionId" }, 0);
            AddString(builder, root, gameData, new[] { "activeRun", "questionContentId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "activeRun", "eventAttemptOrdinal" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "eventScheduleVersion" }, 0);
            AddString(builder, root, gameData, new[] { "activeRun", "eventScheduleCatalogVersion" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "eventScheduleEventId" }, string.Empty);
            AddEmptyArray(builder, root, gameData, new[] { "activeRun", "eventScheduleStages" });
            AddInteger(builder, root, gameData, new[] { "activeRun", "eventChanceBasisPoints" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "petEventMultiplierBasisPoints" }, 10000);
            AddInteger(builder, root, gameData, new[] { "activeRun", "challengeQuestions", "silverCursor" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "challengeQuestions", "goldCursor" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "challengeQuestions", "diamondCursor" }, 0);
            AddString(builder, root, gameData, new[] { "activeRun", "challengeQuestions", "reservedDocumentId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "challengeQuestions", "reservedQuestionId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "lastChallengeRewardAttemptId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "activeRun", "lastChallengeRewardPowerCoins" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "lastChallengeRewardResultingPowerCoins" }, 0);
            AddString(builder, root, gameData, new[] { "activeRun", "enemyId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyCurrentHp" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyMaximumHp" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyRemainingCooldown" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyMaximumCooldown" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "playerCurrentHearts" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "playerMaximumHearts" }, 0);
            AddString(builder, root, gameData, new[] { "activeRun", "phase" }, "EnemyReady");
            AddInteger(builder, root, gameData, new[] { "activeRun", "silverEarned" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "goldEarned" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "diamondEarned" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "bonusMultiplierBasisPoints" }, 10000);
            AddInteger(builder, root, gameData, new[] { "activeRun", "stageAttackCount" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "bigBossesDefeated" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "pendingPetFollowUpDamage" }, 0);
            AddBoolean(builder, root, gameData, new[] { "activeRun", "wasTeleported" }, false);
            AddString(builder, root, gameData, new[] { "economy", "lastWeaponAscendTransactionId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "economy", "lastWeaponAscendLevel" }, 0);
            AddInteger(builder, root, gameData, new[] { "economy", "lastWeaponAscendCost" }, 0);
            AddString(builder, root, gameData, new[] { "economy", "lastPetGachaTransactionId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "economy", "lastPetGachaCatalogVersion" }, string.Empty);
            AddString(builder, root, gameData, new[] { "economy", "lastPetGachaPetId" }, string.Empty);
            AddBoolean(builder, root, gameData, new[] { "economy", "lastPetGachaWasNew" }, false);
            AddInteger(builder, root, gameData, new[] { "economy", "lastPetGachaCost" }, 0);
            AddInteger(builder, root, gameData, new[] { "economy", "lastPetGachaResultingPowerCoins" }, 0);
            AddInteger(builder, root, gameData, new[] { "economy", "petGachaPullsSinceSsr" }, 0);
            AddInteger(builder, root, gameData, new[] { "economy", "lastPetGachaPreviousPityCount" }, 0);
            AddInteger(builder, root, gameData, new[] { "economy", "lastPetGachaResultingPityCount" }, 0);
            AddEmptyArray(builder, root, gameData, new[] { "economy", "lastPetGachaResults" });
            AddString(builder, root, gameData, new[] { "economy", "lastPetEquipTransactionId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "economy", "lastPetEquipPetId" }, string.Empty);
            AddBoolean(builder, root, gameData, new[] { "economy", "firstGachaPullCompleted" }, false);
            AddString(builder, root, gameData, new[] { "lastRunSettlement", "runId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "lastRunSettlement", "type" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "lastRunSettlement", "stageReached" }, 0);
            AddInteger(builder, root, gameData, new[] { "lastRunSettlement", "powerCoinsGranted" }, 0);
            AddInteger(builder, root, gameData, new[] { "lastRunSettlement", "legacyAtkBasisPointsGranted" }, 0);
            AddInteger(builder, root, gameData, new[] { "lastRunSettlement", "prestigeGranted" }, 0);
            AddInteger(builder, root, gameData, new[] { "lastRunSettlement", "resultingPowerCoins" }, 0);
            AddInteger(builder, root, gameData, new[] { "academic", "auditScore" }, 0);
            AddInteger(builder, root, gameData, new[] { "academic", "auditResolvedCount" }, 0);
            AddInteger(builder, root, gameData, new[] { "academic", "auditCorrectCount" }, 0);
            AddNull(builder, root, gameData, new[] { "academic", "activeAttempt" });
            foreach (string rank in new[] { "silver", "gold", "diamond" })
            {
                string[] rankRoot = { "academic", "inventories", rank };
                AddInteger(builder, root, gameData, Combine(rankRoot, "cycle"), 0);
                AddEmptyArray(builder, root, gameData, Combine(rankRoot, "pendingIds"));
                AddEmptyArray(builder, root, gameData, Combine(rankRoot, "failedIds"));
                AddEmptyArray(builder, root, gameData, Combine(rankRoot, "attemptedInAuditIds"));
                AddEmptyArray(builder, root, gameData, Combine(rankRoot, "clearedInCycleIds"));
            }
            foreach (string field in new[]
            {
                "totalQuestionsResolved", "totalCorrect", "totalIncorrect",
                "totalTimeout", "totalAbandoned", "responseScoreSum",
                "responseEfficiencySum", "responseDurationMillisecondsSum", "totalPlaySeconds"
            })
            {
                AddInteger(builder, root, gameData, new[] { "analytics", field }, 0);
            }
            AddString(builder, root, gameData, new[] { "analytics", "lastAppliedAttemptId" }, string.Empty);
            AddEmptyArray(builder, root, gameData, new[] { "analytics", "responseScoreHistogram" });
            AddEmptyArray(builder, root, gameData, new[] { "analytics", "responseEfficiencyHistogram" });
            AddEmptyArray(builder, root, gameData, new[] { "analytics", "responseDuration100msHistogram" });
            foreach (string rank in new[] { "silver", "gold", "diamond" })
            {
                foreach (string field in new[] { "resolved", "correct", "responseScoreSum", "responseEfficiencySum" })
                    AddInteger(builder, root, gameData, new[] { "analytics", "byRank", rank, field }, 0);
            }
            return builder.Build();
        }

        private static void ApplyDurableMigrations(
            FirestorePatchDocumentBuilder builder,
            string[] root,
            JsonValue gameData,
            int storedVersion)
        {
            if (storedVersion >= 6 || gameData == null ||
                !gameData.TryGet("activeRun", out JsonValue activeRunValue) ||
                !FirestoreJsonNavigator.TryGetMapFields(
                    activeRunValue,
                    out JsonValue activeRun))
            {
                return;
            }

            string encounterKind = ReadMigrationString(activeRun, "encounterKind");
            string phase = ReadMigrationString(activeRun, "phase");
            if (!string.Equals(encounterKind, "NormalMonster", StringComparison.Ordinal) ||
                !string.Equals(phase, "Committed", StringComparison.Ordinal) ||
                !activeRun.TryGet("enemyRemainingCooldown", out JsonValue remainingValue) ||
                !activeRun.TryGet("enemyMaximumCooldown", out JsonValue maximumValue))
            {
                return;
            }

            if (!FirestoreJsonNavigator.TryReadInteger(remainingValue, out long remaining) ||
                !FirestoreJsonNavigator.TryReadInteger(maximumValue, out long maximum) ||
                remaining < 0 || maximum < 0 ||
                remaining > int.MaxValue || maximum > int.MaxValue)
            {
                throw new FormatException("Invalid cooldown in player migration data.");
            }

            long corrected = Math.Min(maximum, remaining + 1L);
            if (corrected != remaining)
            {
                builder.AddInteger(
                    Combine(root, "activeRun", "enemyRemainingCooldown"),
                    corrected);
            }
        }

        private static string ReadMigrationString(JsonValue fields, string name)
        {
            if (!fields.TryGet(name, out JsonValue value)) return string.Empty;
            if (!value.TryGet("stringValue", out JsonValue text) ||
                text.Kind != JsonValueKind.String)
            {
                throw new FormatException("Invalid string in player migration data: " + name);
            }
            return text.Text ?? string.Empty;
        }

        private static void AddString(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name, string value) =>
            AddString(builder, root, fields, new[] { name }, value);
        private static void AddString(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path, string value)
        {
            if (!HasPath(fields, path)) builder.AddString(Combine(root, path), value);
            else if (FirestoreJsonNavigator.IsNull(ReadLeaf(fields, path)))
                builder.AddString(Combine(root, path), value);
            else if (!ReadLeaf(fields, path).TryGet("stringValue", out var text) || text.Kind != JsonValueKind.String)
                throw new FormatException("Invalid string in player data: " + string.Join(".", path));
        }
        private static void AddInteger(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name, long value) =>
            AddInteger(builder, root, fields, new[] { name }, value);
        private static void AddInteger(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path, long value)
        {
            if (!HasPath(fields, path)) builder.AddInteger(Combine(root, path), value);
            else if (!FirestoreJsonNavigator.TryReadInteger(ReadLeaf(fields, path), out _))
                throw new FormatException("Invalid integer in player data: " + string.Join(".", path));
        }
        private static void AddBoolean(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path, bool value)
        {
            if (!HasPath(fields, path)) builder.AddBoolean(Combine(root, path), value);
            else if (!FirestoreJsonNavigator.TryReadBoolean(ReadLeaf(fields, path), out _))
                throw new FormatException("Invalid boolean in player data: " + string.Join(".", path));
        }
        private static void AddEmptyArray(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name) =>
            AddEmptyArray(builder, root, fields, new[] { name });
        private static void AddEmptyArray(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path)
        {
            if (!HasPath(fields, path)) builder.AddEmptyArray(Combine(root, path));
            else if (!FirestoreJsonNavigator.TryGetArrayValues(ReadLeaf(fields, path), out _))
                throw new FormatException("Invalid array in player data: " + string.Join(".", path));
        }
        private static void AddEmptyMap(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name) =>
            AddEmptyMap(builder, root, fields, new[] { name });
        private static void AddEmptyMap(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path)
        {
            if (!HasPath(fields, path)) builder.AddEmptyMap(Combine(root, path));
            else if (!FirestoreJsonNavigator.TryGetMapFields(ReadLeaf(fields, path), out _))
                throw new FormatException("Invalid map in player data: " + string.Join(".", path));
        }
        private static void AddNull(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path)
        {
            if (!HasPath(fields, path)) builder.AddNull(Combine(root, path));
        }

        private static JsonValue ReadLeaf(JsonValue fields, IReadOnlyList<string> path)
        {
            JsonValue current = fields;
            for (int index = 0; index < path.Count; index++)
            {
                current.TryGet(path[index], out var value);
                if (index == path.Count - 1) return value;
                FirestoreJsonNavigator.TryGetMapFields(value, out current);
            }
            return null;
        }

        private static bool HasPath(JsonValue fields, IReadOnlyList<string> path)
        {
            JsonValue current = fields;
            for (int index = 0; index < path.Count; index++)
            {
                if (current == null || !current.TryGet(path[index], out JsonValue value)) return false;
                if (index == path.Count - 1) return true;
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out current))
                    throw new FormatException("Invalid map in player data: " + path[index]);
            }
            return false;
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
