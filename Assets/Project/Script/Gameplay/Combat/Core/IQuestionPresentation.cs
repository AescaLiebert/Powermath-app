using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public enum QuestionPresentationStatus
    {
        Ready,
        ContentUnavailable
    }

    public readonly struct QuestionPresentationResult
    {
        public QuestionPresentationResult(
            QuestionPresentationStatus status,
            string playerMessage,
            bool retainsPresentationSurface = false)
        {
            Status = status;
            PlayerMessage = playerMessage ?? string.Empty;
            RetainsPresentationSurface = retainsPresentationSurface;
        }

        public QuestionPresentationStatus Status { get; }
        public string PlayerMessage { get; }
        public bool RetainsPresentationSurface { get; }
        public bool IsReady => Status == QuestionPresentationStatus.Ready;
    }

    public interface IQuestionPresentation
    {
        void Begin(
            QuestionPresentationDescriptor question,
            Action<QuestionPresentationResult> completed
        );
        void Dismiss();
        void Cancel();
    }
}
