using System;
using System.Collections;
using System.Text;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerMath.Session
{
    public sealed class FirestorePlayerResetService
    {
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;

        public FirestorePlayerResetService(GameApiSettings settings, PlayerSnapshot player)
        {
            _settings = settings;
            _player = player;
        }

        public IEnumerator ResetUserData(Action onSuccess, Action<string> onFailure)
        {
            if (_player == null)
            {
                onFailure?.Invoke("No active player data to reset.");
                yield break;
            }

#if UNITY_EDITOR
            if (_settings != null && _settings.UseEditorSampleStudent)
            {
                ResetEditorMockData();
                onSuccess?.Invoke();
                yield break;
            }
#endif

            if (_settings == null)
            {
                onFailure?.Invoke("Firebase settings are not configured.");
                yield break;
            }

            if (!TryResolvePlayer(out string levelId, out string username))
            {
                onFailure?.Invoke("Unable to resolve student and level identifier.");
                yield break;
            }

            if (!_settings.TryGetLevelDocumentById(levelId, out string levelUrl))
            {
                onFailure?.Invoke("Level document is not configured for this player.");
                yield break;
            }

            // Step 1: Remove old leaderboard projection if publicPlayerId exists
            string oldPublicId = _player.profile?.publicPlayerId?.Trim();
            if (IsValidPublicPlayerId(oldPublicId) && _settings.TryGetLeaderboardDocument(levelId, out string leaderboardUrl))
            {
                yield return DeleteLeaderboardEntry(leaderboardUrl, oldPublicId);
            }

            // Step 2: Fetch level document for updateTime concurrency token
            string updateTime;
            using (UnityWebRequest get = UnityWebRequest.Get(levelUrl))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                get.SetRequestHeader("Accept", "application/json");
                yield return get.SendWebRequest();

                if (get.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(get.downloadHandler.text, out JsonValue root, out _) ||
                    !root.TryGet("updateTime", out JsonValue updateValue) ||
                    updateValue.Kind != JsonValueKind.String)
                {
                    onFailure?.Invoke("Could not read student document from Firebase before resetting.");
                    yield break;
                }

                updateTime = updateValue.Text;
            }

            // Step 3: Send Game Data reset PATCH
            string newPublicId = Guid.NewGuid().ToString("N");
            FirestorePatchPlan resetPlan = PlayerResetPayloadBuilder.BuildGameDataResetPlan(username, newPublicId);

            var address = new StringBuilder(levelUrl);
            string separator = levelUrl.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in resetPlan.FieldPaths)
            {
                address.Append(separator).Append("updateMask.fieldPaths=")
                    .Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            if (!string.IsNullOrWhiteSpace(updateTime))
            {
                address.Append(separator).Append("currentDocument.updateTime=")
                    .Append(Uri.EscapeDataString(updateTime));
            }

            using (var patch = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                patch.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(resetPlan.ToJson()));
                patch.downloadHandler = new DownloadHandlerBuffer();
                patch.timeout = _settings.RequestTimeoutSeconds;
                patch.SetRequestHeader("Content-Type", "application/json");
                patch.SetRequestHeader("Accept", "application/json");

                yield return patch.SendWebRequest();

                if (patch.result != UnityWebRequest.Result.Success)
                {
                    if (patch.responseCode == 409 || patch.responseCode == 412)
                    {
                        onFailure?.Invoke("Player data changed on another client. Please retry.");
                    }
                    else
                    {
                        onFailure?.Invoke("Failed to reset player data in Firebase (" + patch.responseCode + ").");
                    }
                    yield break;
                }
            }

            onSuccess?.Invoke();
        }

        private IEnumerator DeleteLeaderboardEntry(string leaderboardUrl, string publicPlayerId)
        {
            var address = new StringBuilder(leaderboardUrl);
            string separator = leaderboardUrl.IndexOf('?') >= 0 ? "&" : "?";
            address.Append(separator).Append("updateMask.fieldPaths=")
                .Append(Uri.EscapeDataString(publicPlayerId));

            // Passing {"fields":{}} with updateMask deletes the target field in Firestore REST API
            string emptyBody = "{\"fields\":{}}";

            using (var patch = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                patch.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(emptyBody));
                patch.downloadHandler = new DownloadHandlerBuffer();
                patch.timeout = _settings.RequestTimeoutSeconds;
                patch.SetRequestHeader("Content-Type", "application/json");
                patch.SetRequestHeader("Accept", "application/json");

                yield return patch.SendWebRequest();
                // Leaderboard deletion failure does not abort the main game data reset; it is logged.
                if (patch.result != UnityWebRequest.Result.Success)
                {
                    PowerMath.Diagnostics.AppLog.Warning("Session", "Leaderboard entry deletion returned " + patch.responseCode + ": " + patch.error);
                }
            }
        }

        private bool TryResolvePlayer(out string levelId, out string username)
        {
            levelId = string.Empty;
            username = string.Empty;
            string playerId = _player?.playerId ?? string.Empty;
            int separator = playerId.IndexOf(':');
            if (separator <= 0 || separator >= playerId.Length - 1)
            {
                return false;
            }

            levelId = playerId.Substring(0, separator);
            username = playerId.Substring(separator + 1);
            return true;
        }

        private static bool IsValidPublicPlayerId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32) return false;
            foreach (char character in value)
            {
                if (!Uri.IsHexDigit(character)) return false;
            }
            return true;
        }

#if UNITY_EDITOR
        private void ResetEditorMockData()
        {
            if (_player == null) return;
            _player.revision = 0;
            if (_player.profile != null)
            {
                _player.profile.publicPlayerId = Guid.NewGuid().ToString("N");
                _player.profile.displayNameChangedAtUnixSeconds = 0;
            }
            if (_player.progression != null)
            {
                _player.progression.currentStage = 1;
                _player.progression.highestStage = 1;
                _player.progression.activeRank = "Silver";
                _player.progression.rankProgress = 0;
                _player.progression.prestige = 0;
                _player.progression.firstStage200Reached = false;
                _player.progression.firstStage200ReachedAtUnixSeconds = 0;
                _player.progression.totalDamage = 0;
                _player.progression.legacyAtkBonusBasisPoints = 0;
            }
            if (_player.wallet != null)
            {
                _player.wallet.silver = 0;
                _player.wallet.gold = 0;
                _player.wallet.diamond = 0;
                _player.wallet.powerCoins = 0;
            }
            _player.inventory = Array.Empty<PlayerSnapshot.InventoryItemData>();
            if (_player.loadout != null)
            {
                _player.loadout.petId = string.Empty;
                _player.loadout.weaponId = string.Empty;
                _player.loadout.avatarId = string.Empty;
            }
            if (_player.activeRun != null)
            {
                _player.activeRun.runId = Guid.NewGuid().ToString("N");
                _player.activeRun.currentStage = 1;
                _player.activeRun.committedAttemptId = string.Empty;
                _player.activeRun.phase = "EnemyReady";
                _player.activeRun.silverEarned = 0;
                _player.activeRun.goldEarned = 0;
                _player.activeRun.diamondEarned = 0;
            }
            if (_player.academic != null)
            {
                _player.academic.auditScore = 0;
                _player.academic.auditResolvedCount = 0;
            }
            if (_player.analytics != null)
            {
                _player.analytics.totalQuestionsResolved = 0;
                _player.analytics.totalCorrect = 0;
                _player.analytics.totalIncorrect = 0;
                _player.analytics.totalTimeout = 0;
                _player.analytics.totalAbandoned = 0;
                _player.analytics.totalPlaySeconds = 0;
            }
        }
#endif
    }
}
