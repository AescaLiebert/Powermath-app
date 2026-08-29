using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public enum GameplaySavePoint
    {
        AttemptCommitted,
        AnswerWindowOpened,
        AttemptResolved,
        ContentFailureVoided,
        PresentationCompleted
    }

    public readonly struct GameplaySaveRequest
    {
        public GameplaySaveRequest(
            GameplaySavePoint savePoint,
            GameplaySnapshot snapshot,
            AcademicPersistenceSnapshot academic,
            QuestionPresentationDescriptor activeQuestion = null,
            AnswerWindowReceipt? answerWindow = null,
            AttemptResolution resolution = null,
            string transactionId = "",
            string presentationId = "")
        {
            TransactionId = string.IsNullOrWhiteSpace(transactionId)
                ? Guid.NewGuid().ToString("N")
                : transactionId;
            SavePoint = savePoint;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Academic = academic ?? throw new ArgumentNullException(nameof(academic));
            ActiveQuestion = activeQuestion;
            AnswerWindow = answerWindow;
            Resolution = resolution;
            PresentationId = presentationId ?? string.Empty;
        }

        public GameplaySavePoint SavePoint { get; }
        public string TransactionId { get; }
        public GameplaySnapshot Snapshot { get; }
        public AcademicPersistenceSnapshot Academic { get; }
        public QuestionPresentationDescriptor ActiveQuestion { get; }
        public AnswerWindowReceipt? AnswerWindow { get; }
        public AttemptResolution Resolution { get; }
        public string PresentationId { get; }
    }

    public interface IGameplayPersistence
    {
        void Save(
            GameplaySaveRequest request,
            Action completed,
            Action<string> failed);
        void Cancel();
    }

    public interface IGameplaySaveRequestFactory
    {
        GameplaySaveRequest CreateSaveRequest(
            GameplaySavePoint savePoint,
            QuestionPresentationDescriptor activeQuestion = null,
            AnswerWindowReceipt? answerWindow = null,
            AttemptResolution resolution = null,
            string presentationId = "");
    }

    public sealed class ImmediateGameplayPersistence : IGameplayPersistence
    {
        public void Save(
            GameplaySaveRequest request,
            Action completed,
            Action<string> failed) => completed?.Invoke();
        public void Cancel() { }
    }
}
