using System;
using System.Collections.Generic;

namespace PowerMath.Session
{
    public static class PlayerDefaultsPlanner
    {
        public static FirestorePatchPlan Plan(JsonValue studentValue, string username)
        {
            if (!FirestoreJsonNavigator.TryGetMapFields(studentValue, out JsonValue studentFields))
            {
                throw new ArgumentException("Student must be a Firestore map value.", nameof(studentValue));
            }

            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { username, "gamedata" };
            studentFields.TryGet("gamedata", out JsonValue gameDataValue);
            FirestoreJsonNavigator.TryGetMapFields(gameDataValue, out JsonValue gameData);

            AddInteger(builder, root, gameData, "revision", 0);
            AddString(builder, root, gameData, new[] { "profile", "displayName" }, username);
            AddString(builder, root, gameData, new[] { "profile", "iconId" }, "avatar-default");
            AddInteger(builder, root, gameData, new[] { "progression", "currentStage" }, 1);
            AddInteger(builder, root, gameData, new[] { "progression", "highestStage" }, 1);
            AddString(builder, root, gameData, new[] { "progression", "activeRank" }, "Silver");
            AddInteger(builder, root, gameData, new[] { "progression", "prestige" }, 0);
            AddBoolean(builder, root, gameData, new[] { "progression", "firstStage200Reached" }, false);
            AddInteger(builder, root, gameData, new[] { "wallet", "silver" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "gold" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "diamond" }, 0);
            AddInteger(builder, root, gameData, new[] { "wallet", "powerCoins" }, 0);
            AddEmptyArray(builder, root, gameData, "inventory");
            AddString(builder, root, gameData, new[] { "loadout", "petId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "loadout", "weaponId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "loadout", "avatarId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "runId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "activeRun", "currentStage" }, 1);
            AddString(builder, root, gameData, new[] { "activeRun", "committedAttemptId" }, string.Empty);
            AddString(builder, root, gameData, new[] { "activeRun", "enemyId" }, string.Empty);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyCurrentHp" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyMaximumHp" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyRemainingCooldown" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "enemyMaximumCooldown" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "playerCurrentHearts" }, 0);
            AddInteger(builder, root, gameData, new[] { "activeRun", "playerMaximumHearts" }, 0);
            AddString(builder, root, gameData, new[] { "activeRun", "phase" }, "EnemyReady");
            AddInteger(builder, root, gameData, new[] { "academic", "auditScore" }, 0);
            AddInteger(builder, root, gameData, new[] { "academic", "auditResolvedCount" }, 0);
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
            return builder.Build();
        }

        private static void AddString(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name, string value) =>
            AddString(builder, root, fields, new[] { name }, value);
        private static void AddString(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path, string value)
        {
            if (!HasPath(fields, path)) builder.AddString(Combine(root, path), value);
        }
        private static void AddInteger(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name, long value) =>
            AddInteger(builder, root, fields, new[] { name }, value);
        private static void AddInteger(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path, long value)
        {
            if (!HasPath(fields, path)) builder.AddInteger(Combine(root, path), value);
        }
        private static void AddBoolean(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path, bool value)
        {
            if (!HasPath(fields, path)) builder.AddBoolean(Combine(root, path), value);
        }
        private static void AddEmptyArray(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string name) =>
            AddEmptyArray(builder, root, fields, new[] { name });
        private static void AddEmptyArray(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path)
        {
            if (!HasPath(fields, path)) builder.AddEmptyArray(Combine(root, path));
        }
        private static void AddNull(FirestorePatchDocumentBuilder builder, string[] root, JsonValue fields, string[] path)
        {
            if (!HasPath(fields, path)) builder.AddNull(Combine(root, path));
        }

        private static bool HasPath(JsonValue fields, IReadOnlyList<string> path)
        {
            JsonValue current = fields;
            for (int index = 0; index < path.Count; index++)
            {
                if (current == null || !current.TryGet(path[index], out JsonValue value)) return false;
                if (index == path.Count - 1) return true;
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out current)) return false;
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
