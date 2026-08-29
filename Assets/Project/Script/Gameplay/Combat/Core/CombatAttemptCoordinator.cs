using System;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct AnswerTiming
    {
        public AnswerTiming(
            bool isPreparation,
            double remainingSeconds,
            int displayedSeconds)
        {
            IsPreparation = isPreparation;
            RemainingSeconds = remainingSeconds;
            DisplayedSeconds = displayedSeconds;
        }

        public bool IsPreparation { get; }
        public double RemainingSeconds { get; }
        public int DisplayedSeconds { get; }
    }

    public sealed class CombatAttemptCoordinator
    {
        private readonly IAttemptAuthorityGateway _gateway;
        private AnswerBuffer _answerBuffer = new AnswerBuffer(1);
        private AttemptCommit _commit;
        private AnswerWindowReceipt _window;
        private bool _resolutionStarted;
        private string _pendingPresentationId = string.Empty;

        public CombatAttemptCoordinator(IAttemptAuthorityGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            Phase = gateway.Snapshot.Combat.Phase;
            _pendingPresentationId = gateway.PendingPresentation?.PresentationId ??
                string.Empty;
        }

        public event Action<GameplaySnapshot> SnapshotChanged;
        public event Action<string, bool> AnswerChanged;
        public event Action<AttemptResolution> AttemptResolved;

        public CombatPhase Phase { get; private set; }
        public GameplaySnapshot Snapshot => _gateway.Snapshot;
        public AttemptPresentationReceipt PendingPresentation =>
            _gateway.PendingPresentation;

        public bool TryBeginAttempt(out AttemptCommit commit)
        {
            commit = null;
            if (Phase != CombatPhase.EnemyReady && Phase != CombatPhase.EventReady)
            {
                return false;
            }

            commit = _gateway.CommitAttempt(CombatCommandId.New());
            _commit = commit;
            _answerBuffer = new AnswerBuffer(commit.AnswerPolicy.MaximumLength);
            _resolutionStarted = false;
            Phase = CombatPhase.Committed;
            SnapshotChanged?.Invoke(commit.Snapshot);
            AnswerChanged?.Invoke(string.Empty, false);
            return true;
        }

        public bool BeginAnswerWindow()
        {
            return BeginAnswerWindow(out _);
        }

        public bool BeginAnswerWindow(out AnswerWindowReceipt receipt)
        {
            receipt = default;
            if (Phase != CombatPhase.Committed || _commit == null)
            {
                return false;
            }

            _window = _gateway.OpenAnswerWindow(
                CombatCommandId.New(),
                _commit.Question.Id
            );
            receipt = _window;
            Phase = CombatPhase.Preparation;
            return true;
        }

        public GameplaySnapshot VoidContentFailure()
        {
            if (Phase != CombatPhase.Committed)
            {
                throw new InvalidOperationException(
                    "Only a committed attempt can be voided for content failure."
                );
            }

            GameplaySnapshot snapshot = _gateway.VoidContentFailure(
                CombatCommandId.New()
            );
            ResetAttemptState();
            Phase = snapshot.Combat.Phase;
            SnapshotChanged?.Invoke(snapshot);
            AnswerChanged?.Invoke(string.Empty, false);
            return snapshot;
        }

        public AnswerTiming Tick(double nowSeconds)
        {
            if (Phase != CombatPhase.Preparation && Phase != CombatPhase.Answering)
            {
                return new AnswerTiming(false, 0d, 0);
            }

            if (nowSeconds >= _window.AnswerEndsAt)
            {
                ResolveTimeout();
                return new AnswerTiming(false, 0d, 0);
            }

            bool isPreparation = nowSeconds < _window.PreparationEndsAt;
            if (!isPreparation && Phase == CombatPhase.Preparation)
            {
                Phase = CombatPhase.Answering;
            }

            double remaining = Math.Max(0d, _window.AnswerEndsAt - nowSeconds);
            int displayed = (int)Math.Ceiling(remaining);
            return new AnswerTiming(isPreparation, remaining, displayed);
        }

        public bool TryAppendDigit(int digit)
        {
            if (!CanAcceptAnswerInput() || !_answerBuffer.TryAppend(digit))
            {
                return false;
            }

            PublishAnswer();
            return true;
        }

        public bool TryBackspace()
        {
            if (!CanAcceptAnswerInput() || !_answerBuffer.Backspace())
            {
                return false;
            }

            PublishAnswer();
            return true;
        }

        public bool TryClear()
        {
            if (!CanAcceptAnswerInput() || _answerBuffer.IsEmpty)
            {
                return false;
            }

            _answerBuffer.Clear();
            PublishAnswer();
            return true;
        }

        public bool TrySubmit(double nowSeconds)
        {
            if (!CanAcceptAnswerInput() || _resolutionStarted)
            {
                return false;
            }

            if (nowSeconds >= _window.AnswerEndsAt)
            {
                ResolveTimeout();
                return true;
            }

            if (!_answerBuffer.TryGetNormalized(out string normalized))
            {
                return false;
            }

            _resolutionStarted = true;
            Phase = CombatPhase.Resolving;
            AttemptResolution resolution = _gateway.SubmitAnswer(
                CombatCommandId.New(),
                normalized
            );
            _pendingPresentationId = resolution.Presentation?.PresentationId ?? string.Empty;
            Phase = resolution.Snapshot.Combat.Phase;
            AttemptResolved?.Invoke(resolution);
            return true;
        }

        public GameplaySnapshot CompletePresentation()
        {
            if (string.IsNullOrWhiteSpace(_pendingPresentationId))
                throw new InvalidOperationException("No accepted presentation is pending completion.");
            GameplaySnapshot snapshot = _gateway.CompletePresentation(
                CombatCommandId.New(),
                _pendingPresentationId
            );
            ResetAttemptState();
            Phase = snapshot.Combat.Phase;
            SnapshotChanged?.Invoke(snapshot);
            return snapshot;
        }

        private void ResolveTimeout()
        {
            if (_resolutionStarted)
            {
                return;
            }

            _resolutionStarted = true;
            Phase = CombatPhase.Resolving;
            AttemptResolution resolution = _gateway.ResolveTimeout(
                CombatCommandId.New()
            );
            _pendingPresentationId = resolution.Presentation?.PresentationId ?? string.Empty;
            Phase = resolution.Snapshot.Combat.Phase;
            AttemptResolved?.Invoke(resolution);
        }

        private bool CanAcceptAnswerInput()
        {
            return Phase == CombatPhase.Preparation || Phase == CombatPhase.Answering;
        }

        private void PublishAnswer()
        {
            AnswerChanged?.Invoke(
                _answerBuffer.GetDisplayValue(),
                !_answerBuffer.IsEmpty
            );
        }

        private void ResetAttemptState()
        {
            _commit = null;
            _answerBuffer = new AnswerBuffer(1);
            _resolutionStarted = false;
            _pendingPresentationId = string.Empty;
        }
    }
}
