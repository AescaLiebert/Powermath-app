using System;
using System.Runtime.InteropServices;
using PowerMath.Gameplay.Academic;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    /// <summary>
    /// WebGL adapter for YouTube's IFrame Player API. The iframe is positioned over
    /// the Unity canvas, so playback stays in the game page without opening a tab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WebGlYouTubeQuestionPresentation : MonoBehaviour, IQuestionPresentation
    {
        private Action<QuestionPresentationResult> _completed;
        private int _generation;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PowerMathYouTubeShow(
            string receiver,
            string videoId,
            int generation);

        [DllImport("__Internal")]
        private static extern void PowerMathYouTubeHide();
#endif

        public void Begin(
            QuestionPresentationDescriptor question,
            Action<QuestionPresentationResult> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            Cancel();
            _completed = completed;
            int generation = ++_generation;
            if (question == null || string.IsNullOrWhiteSpace(question.YouTubeVideoId))
            {
                CompleteUnavailable(generation, "This question video is unavailable.");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathYouTubeShow(gameObject.name, question.YouTubeVideoId, generation);
#else
            CompleteUnavailable(generation, "Embedded YouTube playback requires a WebGL build.");
#endif
        }

        public void Cancel()
        {
            _generation++;
            _completed = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathYouTubeHide();
#endif
        }

        // Invoked by Assets/Plugins/WebGL/PowerMathYouTube.jslib.
        public void OnYouTubeEnded(string generationText)
        {
            if (!TryMatchGeneration(generationText, out int generation)) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathYouTubeHide();
#endif
            Action<QuestionPresentationResult> completed = _completed;
            _completed = null;
            completed?.Invoke(new QuestionPresentationResult(
                QuestionPresentationStatus.Ready,
                "Enter your answer."
            ));
        }

        public void OnYouTubeError(string payload)
        {
            string[] parts = (payload ?? string.Empty).Split('|');
            if (parts.Length == 0 || !TryMatchGeneration(parts[0], out int generation)) return;
            CompleteUnavailable(generation, "This YouTube video could not be played.");
        }

        private bool TryMatchGeneration(string text, out int generation)
        {
            return int.TryParse(text, out generation) && generation == _generation;
        }

        private void CompleteUnavailable(int generation, string message)
        {
            if (generation != _generation) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathYouTubeHide();
#endif
            Action<QuestionPresentationResult> completed = _completed;
            _completed = null;
            completed?.Invoke(new QuestionPresentationResult(
                QuestionPresentationStatus.ContentUnavailable,
                message
            ));
        }
    }
}
