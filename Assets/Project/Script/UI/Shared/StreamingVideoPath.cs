using System;
using UnityEngine;

namespace PowerMath.UI.Shared
{
    public static class StreamingVideoPath
    {
        public const string RemoteStreamingAssetsBaseUrl = "https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/StreamingAssets/";

        public static bool TryResolve(string configuredPath, out string url)
        {
            url = string.Empty;
            if (string.IsNullOrWhiteSpace(configuredPath)) return false;

            string value = configuredPath.Trim().Replace('\\', '/');
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttp ||
                 absolute.Scheme == Uri.UriSchemeHttps ||
                 absolute.Scheme == Uri.UriSchemeFile))
            {
                url = absolute.AbsoluteUri;
                return true;
            }

            string relativePath = value.TrimStart('/');

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL builds do not package large video files locally to keep initial payload small.
            // Always stream videos directly from the remote Cloudflare R2 bucket.
            url = RemoteStreamingAssetsBaseUrl.TrimEnd('/') + "/" + relativePath;
            return true;
#else
            string localPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
            if (System.IO.File.Exists(localPath))
            {
                url = localPath;
                return true;
            }

            // Fallback to local backup folder if present in repository root for offline Editor playback
            string backupPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "StreamingAssets_LocalBackup", relativePath));
            if (System.IO.File.Exists(backupPath))
            {
                url = backupPath;
                return true;
            }

            url = RemoteStreamingAssetsBaseUrl.TrimEnd('/') + "/" + relativePath;
            return true;
#endif
        }
    }
}
