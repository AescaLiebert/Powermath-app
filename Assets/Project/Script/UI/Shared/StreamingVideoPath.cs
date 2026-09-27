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
            // Prioritize same-origin StreamingAssets path so browser requests have NO CORS restrictions
            // and resolve directly against the deployed WebGL host where videos are uploaded.
            string streamingPath = Application.streamingAssetsPath;
            if (!string.IsNullOrWhiteSpace(streamingPath))
            {
                url = streamingPath.TrimEnd('/') + "/" + relativePath;
                return true;
            }

            url = "StreamingAssets/" + relativePath;
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

            // Fallback to repository root folder (e.g. /Videos/ junction) for offline Editor playback
            string rootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", relativePath));
            if (System.IO.File.Exists(rootPath))
            {
                url = rootPath;
                return true;
            }

            url = RemoteStreamingAssetsBaseUrl.TrimEnd('/') + "/" + relativePath;
            return true;
#endif
        }
    }
}
