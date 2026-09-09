using System;
using UnityEngine;

namespace PowerMath.UI.Shared
{
    public static class StreamingVideoPath
    {
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

            url = Application.streamingAssetsPath.TrimEnd('/', '\\') + "/" +
                value.TrimStart('/');
            return true;
        }
    }
}
