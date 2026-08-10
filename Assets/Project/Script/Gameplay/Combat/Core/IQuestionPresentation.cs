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
            string playerMessage)
        {
            Status = status;
            PlayerMessage = playerMessage ?? string.Empty;
        }

        public QuestionPresentationStatus Status { get; }
        public string PlayerMessage { get; }
        public bool IsReady => Status == QuestionPresentationStatus.Ready;
    }

    public interface IQuestionPresentation
    {
        void Begin(
            QuestionPresentationDescriptor question,
            Action<QuestionPresentationResult> completed
        );
        void Cancel();
    }
}
