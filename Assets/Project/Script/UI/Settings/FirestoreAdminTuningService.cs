using System;
using System.Collections;
using System.Text;
using PowerMath.Gameplay.Combat;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine.Networking;

namespace PowerMath.UI.Settings
{
    public sealed class FirestoreAdminTuningService
    {
        private readonly GameApiSettings _settings;
        private PlayerSnapshot _player;

        public FirestoreAdminTuningService(GameApiSettings settings, PlayerSnapshot player)
        {
            _settings = settings;
            _player = player;
        }

        public IEnumerator SetRank(string rank, Action succeeded, Action<string> failed)
        {
            if (rank != "Silver" && rank != "Gold" && rank != "Diamond")
            {
                failed?.Invoke("Choose Silver, Gold, or Diamond.");
                yield break;
            }

            yield return Patch(builder =>
            {
                string[] root = PlayerRoot();
                builder.AddString(Join(root, "progression", "activeRank"), rank);
                builder.AddInteger(Join(root, "progression", "rankProgress"), 0);
                builder.AddInteger(Join(root, "academic", "auditScore"), 0);
                builder.AddInteger(Join(root, "academic", "auditResolvedCount"), 0);
                builder.AddNull(Join(root, "academic", "activeAttempt"));
                string inventory = rank.ToLowerInvariant();
                builder.AddInteger(Join(root, "academic", "inventories", inventory, "cycle"), 0);
                builder.AddEmptyArray(Join(root, "academic", "inventories", inventory, "pendingIds"));
                builder.AddEmptyArray(Join(root, "academic", "inventories", inventory, "failedIds"));
                builder.AddEmptyArray(Join(root, "academic", "inventories", inventory, "attemptedInAuditIds"));
                builder.AddEmptyArray(Join(root, "academic", "inventories", inventory, "clearedInCycleIds"));
                builder.AddInteger(Join(root, "revision"), _player.revision + 1);
            }, succeeded, failed);
        }

        public IEnumerator SetCombat(
            int attack,
            int criticalRateBasisPoints,
            int criticalDamageBasisPoints,
            bool invincible,
            bool bypassVideoQuestion,
            Action succeeded,
            Action<string> failed)
        {
            if (attack < 1 || attack > 1000000 ||
                criticalRateBasisPoints < 0 || criticalRateBasisPoints > 10000 ||
                criticalDamageBasisPoints < 0 || criticalDamageBasisPoints > 1000000)
            {
                failed?.Invoke("Combat settings are outside the supported range.");
                yield break;
            }

            yield return Patch(builder =>
            {
                string[] root = PlayerRoot();
                builder.AddBoolean(Join(root, "adminTuning", "combatOverrideEnabled"), true);
                builder.AddInteger(Join(root, "adminTuning", "attack"), attack);
                builder.AddInteger(Join(root, "adminTuning", "criticalRateBasisPoints"), criticalRateBasisPoints);
                builder.AddInteger(Join(root, "adminTuning", "criticalDamageBasisPoints"), criticalDamageBasisPoints);
                builder.AddBoolean(Join(root, "adminTuning", "invincible"), invincible);
                builder.AddBoolean(Join(root, "adminTuning", "bypassVideoQuestion"), bypassVideoQuestion);
                builder.AddInteger(Join(root, "revision"), _player.revision + 1);
            }, succeeded, failed);
        }

        public IEnumerator SetBypassVideoQuestion(bool bypassVideoQuestion, Action succeeded, Action<string> failed)
        {
            yield return Patch(builder =>
            {
                string[] root = PlayerRoot();
                builder.AddBoolean(Join(root, "adminTuning", "bypassVideoQuestion"), bypassVideoQuestion);
                builder.AddInteger(Join(root, "revision"), _player.revision + 1);
            }, succeeded, failed);
        }

        public IEnumerator SetCurrency(long silver, long gold, long diamond, long powerCoins, Action succeeded, Action<string> failed)
        {
            if (silver < 0 || gold < 0 || diamond < 0 || powerCoins < 0)
            {
                failed?.Invoke("Currency cannot be negative.");
                yield break;
            }

            yield return Patch(builder =>
            {
                string[] root = PlayerRoot();
                builder.AddInteger(Join(root, "wallet", "silver"), silver);
                builder.AddInteger(Join(root, "wallet", "gold"), gold);
                builder.AddInteger(Join(root, "wallet", "diamond"), diamond);
                builder.AddInteger(Join(root, "wallet", "powerCoins"), powerCoins);
                builder.AddInteger(Join(root, "revision"), _player.revision + 1);
            }, succeeded, failed);
        }

        public IEnumerator RestoreHearts(Action succeeded, Action<string> failed)
        {
            int maximum = _player?.activeRun?.playerMaximumHearts ?? 0;
            if (maximum <= 0)
            {
                failed?.Invoke("Start a run before restoring hearts.");
                yield break;
            }

            yield return Patch(builder =>
            {
                string[] root = PlayerRoot();
                builder.AddInteger(Join(root, "activeRun", "playerCurrentHearts"), maximum);
                builder.AddInteger(Join(root, "revision"), _player.revision + 1);
            }, succeeded, failed);
        }

        public IEnumerator SetStage(int targetStage, Action succeeded, Action<string> failed)
        {
            if (targetStage < StageId.First || targetStage > StageId.Final)
            {
                failed?.Invoke($"Stage must be between {StageId.First} and {StageId.Final}.");
                yield break;
            }

            yield return Patch(builder =>
            {
                string[] root = PlayerRoot();
                builder.AddInteger(Join(root, "progression", "currentStage"), targetStage);
                int currentHighest = _player?.progression?.highestStage ?? 1;
                if (targetStage > currentHighest)
                {
                    builder.AddInteger(Join(root, "progression", "highestStage"), targetStage);
                }
                if (targetStage >= 200 && (_player?.progression == null || !_player.progression.firstStage200Reached))
                {
                    builder.AddBoolean(Join(root, "progression", "firstStage200Reached"), true);
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    builder.AddInteger(Join(root, "progression", "firstStage200ReachedAtUnixSeconds"), now);
                }
                if (_player?.activeRun != null)
                {
                    builder.AddInteger(Join(root, "activeRun", "currentStage"), targetStage);
                    builder.AddNull(Join(root, "activeRun", "committedAttemptId"));
                    builder.AddNull(Join(root, "activeRun", "pendingPresentation"));
                    builder.AddInteger(Join(root, "activeRun", "enemyMaximumHp"), 0);
                    builder.AddInteger(Join(root, "activeRun", "enemyCurrentHp"), 0);
                    builder.AddString(Join(root, "activeRun", "phase"), "EnemyReady");
                }
                builder.AddInteger(Join(root, "revision"), _player.revision + 1);
            }, succeeded, failed);
        }

        private IEnumerator Patch(Action<FirestorePatchDocumentBuilder> compose, Action succeeded, Action<string> failed)
        {
            if (!AdminAccountAccessPolicy.IsAuthorized(_player))
            {
                failed?.Invoke("This account is not authorized for admin tools.");
                yield break;
            }
            if (_settings == null || !TryResolve(out string levelId, out string username))
            {
                failed?.Invoke("Player service is not configured.");
                yield break;
            }
            if (!_settings.TryGetLevelDocumentById(levelId, out string levelUrl))
            {
                failed?.Invoke("Level document is not configured.");
                yield break;
            }

            string updateTime;
            using (UnityWebRequest get = UnityWebRequest.Get(levelUrl))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                get.SetRequestHeader("Accept", "application/json");
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(get.downloadHandler.text, out JsonValue root, out _) ||
                    !root.TryGet("updateTime", out JsonValue update) || update.Kind != JsonValueKind.String ||
                    !FirestoreJsonNavigator.TryGetDocumentFields(root, out JsonValue documentFields) ||
                    !documentFields.TryGet(username, out JsonValue student) ||
                    !FirestoreRestClient.TryMapPlayer(
                        username,
                        levelId,
                        _player?.profile?.gradeBand ?? string.Empty,
                        student,
                        out PlayerSnapshot remotePlayer))
                {
                    failed?.Invoke("Could not refresh the player before applying the command.");
                    yield break;
                }
                updateTime = update.Text;
                if (!AdminAccountAccessPolicy.IsAuthorized(remotePlayer))
                {
                    failed?.Invoke("Firebase no longer authorizes this account for admin tools.");
                    yield break;
                }
                if (remotePlayer.revision != _player.revision)
                {
                    failed?.Invoke("Player data changed on another client. Refresh and try again.");
                    yield break;
                }
                _player = remotePlayer;
            }

            var builder = new FirestorePatchDocumentBuilder();
            compose(builder);
            FirestorePatchPlan plan = builder.Build();
            var address = new StringBuilder(levelUrl);
            string separator = levelUrl.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in plan.FieldPaths)
            {
                address.Append(separator).Append("updateMask.fieldPaths=").Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            address.Append(separator).Append("currentDocument.updateTime=").Append(Uri.EscapeDataString(updateTime));

            using (var patch = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                patch.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(plan.ToJson()));
                patch.downloadHandler = new DownloadHandlerBuffer();
                patch.timeout = _settings.RequestTimeoutSeconds;
                patch.SetRequestHeader("Content-Type", "application/json");
                patch.SetRequestHeader("Accept", "application/json");
                yield return patch.SendWebRequest();
                if (patch.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke(patch.responseCode == 409 || patch.responseCode == 412
                        ? "Player data changed on another client. Try again."
                        : "Firebase rejected the admin command (" + patch.responseCode + ").");
                    yield break;
                }
            }

            PlayerSnapshot refreshed = null;
            using (UnityWebRequest reload = UnityWebRequest.Get(levelUrl))
            {
                reload.timeout = _settings.RequestTimeoutSeconds;
                reload.SetRequestHeader("Accept", "application/json");
                yield return reload.SendWebRequest();
                if (reload.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(reload.downloadHandler.text, out JsonValue document, out _) ||
                    !FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue documentFields) ||
                    !documentFields.TryGet(username, out JsonValue student) ||
                    !FirestoreRestClient.TryMapPlayer(
                        username,
                        levelId,
                        _player?.profile?.gradeBand ?? string.Empty,
                        student,
                        out refreshed))
                {
                    failed?.Invoke("The command was saved, but the player could not be refreshed from Firebase.");
                    yield break;
                }
            }

            PlayerSessionStore store = PlayerSessionStore.Instance;
            if (store == null || store.TryHydrate(new BootstrapResponse
                {
                    schemaVersion = refreshed.schemaVersion,
                    remembered = store.IsRemembered,
                    player = refreshed,
                    serverTimeUtc = string.Empty
                }) != PlayerSessionStore.HydrationResult.Success)
            {
                failed?.Invoke("The command was saved, but the refreshed player session was invalid.");
                yield break;
            }
            _player = refreshed;
            succeeded?.Invoke();
        }

        private bool TryResolve(out string levelId, out string username)
        {
            levelId = string.Empty;
            username = string.Empty;
            string id = _player?.playerId ?? string.Empty;
            int separator = id.IndexOf(':');
            if (separator <= 0 || separator >= id.Length - 1) return false;
            levelId = id.Substring(0, separator);
            username = id.Substring(separator + 1);
            return true;
        }

        private string[] PlayerRoot()
        {
            TryResolve(out _, out string username);
            return new[] { username, "gamedata" };
        }

        private static string[] Join(string[] root, params string[] values)
        {
            var result = new string[root.Length + values.Length];
            Array.Copy(root, result, root.Length);
            Array.Copy(values, 0, result, root.Length, values.Length);
            return result;
        }
    }
}
