using UnityEngine;
using System;
using UnityEngine.Video;
using PowerMath.UI.Shared;

namespace PowerMath.PlayerLifecycle
{
    [CreateAssetMenu(menuName = "PowerMath/Player/Opening Sequence")]
    public sealed class OpeningSequenceDefinition : ScriptableObject
    {
        [Header("Narrative")]
        [TextArea] public string[] englishNarrativeLines;
        [TextArea] public string[] thaiNarrativeLines;
        [TextArea] public string englishText;
        [TextArea] public string thaiText;

        [Header("Video")]
        [Tooltip("Local opening clip for Editor and native builds.")]
        public VideoClip videoClip;
        [Tooltip("Hosted video URL. WebGL does not support embedded VideoClip assets.")]
        public string videoUrl;

        [Header("Starting Timing Values")]
        [Min(0.005f)] public float characterRevealSeconds = 0.025f;
        [Min(0f)] public float lineHoldSeconds = 1.15f;
        [Min(0f)] public float lineFadeSeconds = 0.42f;

        public bool HasVideo => videoClip != null || HasHostedVideo;
        public bool HasHostedVideo =>
            StreamingVideoPath.TryResolve(videoUrl, out _);

        public string[] GetNarrativeLines(string locale)
        {
            string[] localized = string.Equals(locale, "th", StringComparison.OrdinalIgnoreCase)
                ? thaiNarrativeLines
                : englishNarrativeLines;
            if (localized != null && localized.Length > 0) return localized;

            string fallback = string.Equals(locale, "th", StringComparison.OrdinalIgnoreCase)
                ? thaiText
                : englishText;
            return string.IsNullOrWhiteSpace(fallback)
                ? Array.Empty<string>()
                : fallback.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

