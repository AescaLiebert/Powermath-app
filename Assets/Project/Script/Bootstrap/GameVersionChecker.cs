using System;
using System.Collections;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerMath.Bootstrap
{
    public static class GameVersionChecker
    {
        public static IEnumerator FetchManifest(
            string manifestUrl,
            int timeoutSeconds,
            Action<GameVersionManifest> onSuccess,
            Action<string> onFailure)
        {
            if (string.IsNullOrWhiteSpace(manifestUrl))
            {
                onFailure?.Invoke("Version manifest URL is not configured.");
                yield break;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            // In Unity Editor or Standalone builds, relative URLs (like "version.json") cannot be fetched over HTTP.
            // Check local file system (Cloudflare/public or StreamingAssets) first.
            bool isAbsoluteHttp = manifestUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                 manifestUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (!isAbsoluteHttp)
            {
                string localCloudflarePath = System.IO.Path.Combine(Application.dataPath, "..", "Cloudflare", "public", manifestUrl);
                string localStreamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, manifestUrl);
                string chosenPath = System.IO.File.Exists(localCloudflarePath)
                    ? localCloudflarePath
                    : (System.IO.File.Exists(localStreamingPath) ? localStreamingPath : null);

                if (chosenPath != null)
                {
                    try
                    {
                        string localJson = System.IO.File.ReadAllText(chosenPath);
                        GameVersionManifest localManifest = JsonUtility.FromJson<GameVersionManifest>(localJson);
                        if (localManifest != null)
                        {
                            onSuccess?.Invoke(localManifest);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[GameVersionChecker] Failed to read local version manifest ({chosenPath}): {ex.Message}");
                    }
                }

                // If not found locally in Editor, bypass cleanly with default compatible manifest
                onSuccess?.Invoke(new GameVersionManifest
                {
                    clientVersion = Application.version,
                    minSupportedVersion = Application.version,
                    schemaVersion = PlayerSessionStore.SupportedSchemaVersion
                });
                yield break;
            }
#endif

            // Append timestamp query parameter to bypass intermediate caches in browser
            string separator = manifestUrl.Contains("?") ? "&" : "?";
            string cacheBustedUrl = manifestUrl + separator + "t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (UnityWebRequest request = UnityWebRequest.Get(cacheBustedUrl))
            {
                request.timeout = Mathf.Max(3, timeoutSeconds);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onFailure?.Invoke($"Failed to fetch version manifest: {request.error}");
                    yield break;
                }

                try
                {
                    string json = request.downloadHandler.text;
                    GameVersionManifest manifest = JsonUtility.FromJson<GameVersionManifest>(json);
                    if (manifest == null)
                    {
                        onFailure?.Invoke("Version manifest was empty or invalid JSON.");
                        yield break;
                    }

                    onSuccess?.Invoke(manifest);
                }
                catch (Exception ex)
                {
                    onFailure?.Invoke($"Error parsing version manifest: {ex.Message}");
                }
            }
        }

        public static VersionCompatibilityResult EvaluateCompatibility(
            GameVersionManifest manifest,
            string currentAppVersion,
            int supportedSchemaVersion,
            out string statusMessage)
        {
            statusMessage = string.Empty;
            if (manifest == null)
            {
                return VersionCompatibilityResult.Compatible;
            }

            if (manifest.maintenance != null && manifest.maintenance.isActive)
            {
                statusMessage = string.IsNullOrWhiteSpace(manifest.maintenance.message)
                    ? "Game is currently undergoing maintenance. Please try again later."
                    : manifest.maintenance.message;
                return VersionCompatibilityResult.MaintenanceActive;
            }

            if (manifest.schemaVersion > supportedSchemaVersion)
            {
                statusMessage = "A game update is required to support the new player data format.";
                return VersionCompatibilityResult.IncompatibleSchema;
            }

            if (!string.IsNullOrWhiteSpace(manifest.minSupportedVersion))
            {
                if (IsVersionOlder(currentAppVersion, manifest.minSupportedVersion))
                {
                    statusMessage = $"Your game version ({currentAppVersion}) is no longer supported. Minimum version required is {manifest.minSupportedVersion}.";
                    return VersionCompatibilityResult.HardUpdateRequired;
                }
            }

            if (!string.IsNullOrWhiteSpace(manifest.clientVersion))
            {
                if (IsVersionOlder(currentAppVersion, manifest.clientVersion))
                {
                    statusMessage = $"A newer version of the game ({manifest.clientVersion}) is available.";
                    return VersionCompatibilityResult.UpdateRecommended;
                }
            }

            return VersionCompatibilityResult.Compatible;
        }

        public static bool IsVersionOlder(string currentVersion, string targetVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(targetVersion))
            {
                return false;
            }

            if (Version.TryParse(NormalizeVersion(currentVersion), out Version current) &&
                Version.TryParse(NormalizeVersion(targetVersion), out Version target))
            {
                return current < target;
            }

            return string.Compare(currentVersion.Trim(), targetVersion.Trim(), StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.0.0.0";
            string[] parts = version.Trim().Split('.');
            if (parts.Length == 1) return parts[0] + ".0.0.0";
            if (parts.Length == 2) return parts[0] + "." + parts[1] + ".0.0";
            if (parts.Length == 3) return parts[0] + "." + parts[1] + "." + parts[2] + ".0";
            return version.Trim();
        }
    }
}
