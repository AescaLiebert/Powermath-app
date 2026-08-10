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
            AnswerWindowReceipt? answerWindow = null)
        {
            TransactionId = Guid.NewGuid().ToString("N");
            SavePoint = savePoint;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Academic = academic ?? throw new ArgumentNullException(nameof(academic));
            ActiveQuestion = activeQuestion;
            AnswerWindow = answerWindow;
        }

        public GameplaySavePoint SavePoint { get; }
        public string TransactionId { get; }
        public GameplaySnapshot Snapshot { get; }
        public AcademicPersistenceSnapshot Academic { get; }
        public QuestionPresentationDescriptor ActiveQuestion { get; }
        public AnswerWindowReceipt? AnswerWindow { get; }
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
            AnswerWindowReceipt? answerWindow = null);
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
