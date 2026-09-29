using System;
using System.Collections;
using System.Text;
using PowerMath.PlayerData;
using PowerMath.UI.Settings;
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
            if (!AdminAccountAccessPolicy.IsAuthorized(_player))
            {
                onFailure?.Invoke("This account is not authorized to reset player data.");
                yield break;
            }

            if (_player == null)
            {
                onFailure?.Invoke("No active player data to reset.");
                yield break;
            }

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

            if (!_settings.TryGetPlayerDocument(levelId, username, out string levelUrl))
            {
                onFailure?.Invoke("Level document is not configured for this player.");
                yield break;
            }

            string oldPublicId = _player.profile?.publicPlayerId?.Trim();
            // Validate the player revision before changing any projection.
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
                if (!TryReadRevision(root, username, out long remoteRevision) ||
                    remoteRevision != _player.revision)
                {
                    onFailure?.Invoke("Player data changed on another client. Refresh and try again.");
                    yield break;
                }
            }

            // Replace the complete game data map while leaving sibling userdata intact.
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

            // Only remove the public projection after the authoritative reset commits.
            if (IsValidPublicPlayerId(oldPublicId) &&
                _settings.TryGetLeaderboardDocument(levelId, out string leaderboardUrl))
            {
                yield return DeleteLeaderboardEntry(leaderboardUrl, oldPublicId);
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

        private static bool TryReadRevision(JsonValue document, string username, out long revision)
        {
            revision = 0;
            return FirestoreRestClient.TryGetStudent(document, username, out JsonValue student) &&
                FirestoreJsonNavigator.TryGetMapFields(student, out JsonValue studentFields) &&
                studentFields.TryGet("gamedata", out JsonValue gameDataValue) &&
                FirestoreJsonNavigator.TryGetMapFields(gameDataValue, out JsonValue gameData) &&
                gameData.TryGet("revision", out JsonValue revisionValue) &&
                FirestoreJsonNavigator.TryReadInteger(revisionValue, out revision) &&
                revision >= 0;
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

    }
}
