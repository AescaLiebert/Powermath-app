using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public interface IAttemptAuthorityGateway
    {
        GameplaySnapshot Snapshot { get; }
        AttemptPresentationReceipt PendingPresentation { get; }

        AttemptCommit CommitAttempt(CombatCommandId commandId);

        AnswerWindowReceipt OpenAnswerWindow(
            CombatCommandId commandId,
            QuestionId questionId
        );

        AttemptResolution SubmitAnswer(
            CombatCommandId commandId,
            string normalizedAnswer
        );

        AttemptResolution ResolveTimeout(CombatCommandId commandId);

        GameplaySnapshot VoidContentFailure(CombatCommandId commandId);

        GameplaySnapshot CompletePresentation(
            CombatCommandId commandId,
            string presentationId);
    }

    public sealed class LocalDevelopmentAttemptGateway : IAttemptAuthorityGateway
    {
        private readonly LocalAttemptTransactionEngine _engine;
        private readonly CommandReceiptCache _receipts = new CommandReceiptCache();

        public LocalDevelopmentAttemptGateway(LocalAttemptTransactionEngine engine)
        {
            _engine = engine ?? throw new System.ArgumentNullException(nameof(engine));
        }

        public GameplaySnapshot Snapshot => _engine.Snapshot;
        public AttemptPresentationReceipt PendingPresentation =>
            _engine.PendingPresentation;

        public AttemptCommit CommitAttempt(CombatCommandId commandId)
        {
            return _receipts.GetOrAdd(commandId, "commit", _engine.CommitAttempt);
        }

        public AnswerWindowReceipt OpenAnswerWindow(
            CombatCommandId commandId,
            QuestionId questionId)
        {
            return _receipts.GetOrAdd(
                commandId,
                "open-answer-window",
                () => _engine.OpenAnswerWindow(questionId)
            );
        }

        public AttemptResolution SubmitAnswer(
            CombatCommandId commandId,
            string normalizedAnswer)
        {
            if (string.IsNullOrWhiteSpace(normalizedAnswer))
            {
                throw new System.ArgumentException(
                    "A normalized answer is required.",
                    nameof(normalizedAnswer)
                );
            }

            return _receipts.GetOrAdd(
                commandId,
                "submit",
                () => _engine.SubmitAnswer(normalizedAnswer)
            );
        }

        public AttemptResolution ResolveTimeout(CombatCommandId commandId)
        {
            return _receipts.GetOrAdd(
                commandId,
                "timeout",
                _engine.ResolveTimeout
            );
        }

        public GameplaySnapshot VoidContentFailure(CombatCommandId commandId)
        {
            return _receipts.GetOrAdd(
                commandId,
                "void-content",
                _engine.VoidContentFailure
            );
        }

        public GameplaySnapshot CompletePresentation(
            CombatCommandId commandId,
            string presentationId)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
                throw new System.ArgumentException(
                    "Presentation ID is required.", nameof(presentationId));
            return _receipts.GetOrAdd(
                commandId,
                "complete-presentation:" + presentationId,
                () => _engine.CompletePresentation(presentationId)
            );
        }
    }
}
