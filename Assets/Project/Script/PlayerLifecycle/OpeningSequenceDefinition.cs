using UnityEngine;
using System;

namespace PowerMath.PlayerLifecycle
{
    [CreateAssetMenu(menuName = "PowerMath/Player/Opening Sequence")]
    public sealed class OpeningSequenceDefinition : ScriptableObject
    {
        [TextArea] public string englishText;
        [TextArea] public string thaiText;
        [Tooltip("Hosted video URL. WebGL does not support embedded VideoClip assets.")]
        public string videoUrl;
        public bool HasVideo => Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}

