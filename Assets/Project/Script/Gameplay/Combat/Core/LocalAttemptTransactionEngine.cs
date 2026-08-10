using System;
using System.Globalization;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public sealed class LocalAttemptTransactionEngine : IGameplaySaveRequestFactory
    {
        private readonly LocalCombatEngine _combat;
        private readonly AcademicProgressionEngine _academic;
        private readonly IMonotonicClock _clock;
        private readonly double _preparationSeconds;
        private readonly double _answerSeconds;

        private AcademicProgressionState _academicState;
        private ActiveAttempt _activeAttempt;

        public LocalAttemptTransactionEngine(
            LocalCombatEngine combat,
            AcademicProgressionEngine academic,
            AcademicProgressionState academicState,
            IMonotonicClock clock,
            double preparationSeconds,
            double answerSeconds)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _academic = academic ?? throw new ArgumentNullException(nameof(academic));
            _academicState = academicState ??
                throw new ArgumentNullException(nameof(academicState));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (preparationSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(preparationSeconds));
            }

            if (answerSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(answerSeconds));
            }

            _preparationSeconds = preparationSeconds;
            _answerSeconds = answerSeconds;
        }

        public GameplaySnapshot Snapshot => CreateSnapshot();

        public AcademicPersistenceSnapshot ExportAcademicPersistence()
        {
            return _academicState.ExportPersistence();
        }

        public GameplaySaveRequest CreateSaveRequest(
            GameplaySavePoint savePoint,
            QuestionPresentationDescriptor activeQuestion = null,
            AnswerWindowReceipt? answerWindow = null)
        {
            return new GameplaySaveRequest(
                savePoint,
                CreateSnapshot(),
                _academicState.ExportPersistence(),
                activeQuestion,
                answerWindow
            );
        }

        public AttemptCommit CommitAttempt()
        {
            if (_activeAttempt != null)
            {
                throw new InvalidOperationException("An attempt is already committed.");
            }

            QuestionReservationResult reservation = _academic.TryReserve(_academicState);
            if (!reservation.Success)
            {
                throw new InvalidOperationException(reservation.Error);
            }

            try
            {
                _combat.CommitAttempt();
            }
            catch
            {
                _academic.VoidReservation(
                    reservation.State,
                    reservation.Reservation
                );
                throw;
            }

            _academicState = reservation.State;
            _activeAttempt = new ActiveAttempt(reservation.Reservation);
            QuestionDefinition question = reservation.Reservation.Question;
            var descriptor = new QuestionPresentationDescriptor(
                question.Id,
                reservation.Reservation.RankAtCommit,
                question.VideoUri,
                $"QA target answer: {question.CorrectAnswer}",
                question.YouTubeVideoId
            );
            return new AttemptCommit(
                descriptor,
                new AnswerInputPolicy(question.AnswerLength),
                CreateSnapshot()
            );
        }

        public AnswerWindowReceipt OpenAnswerWindow(QuestionId questionId)
        {
            EnsureActiveAttempt();
            if (_activeAttempt.Reservation.Question.Id != questionId)
            {
                throw new InvalidOperationException(
                    "The presented question does not match the committed question."
                );
            }

            if (_activeAttempt.WindowOpen)
            {
                throw new InvalidOperationException("The answer window is already open.");
            }

            double preparationEndsAt = _clock.NowSeconds + _preparationSeconds;
            double answerEndsAt = preparationEndsAt + _answerSeconds;
            _activeAttempt.OpenWindow(preparationEndsAt, answerEndsAt);
            return new AnswerWindowReceipt(preparationEndsAt, answerEndsAt);
        }

        public AttemptResolution SubmitAnswer(string normalizedAnswer)
        {
            EnsureOpenWindow();
            if (_clock.NowSeconds >= _activeAttempt.AnswerEndsAt)
            {
                return Resolve(QuestionOutcome.Timeout, 0);
            }

            bool parsed = int.TryParse(
                normalizedAnswer,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int submittedAnswer
            );
            bool correct = parsed &&
                submittedAnswer == _activeAttempt.Reservation.Question.CorrectAnswer;
            if (!correct)
            {
                return Resolve(QuestionOutcome.Incorrect, 0);
            }

            int responseScore = _clock.NowSeconds < _activeAttempt.PreparationEndsAt
                ? 10
                : Math.Max(
                    1,
                    Math.Min(
                        10,
                        (int)Math.Ceiling(
                            _activeAttempt.AnswerEndsAt - _clock.NowSeconds
                        )
                    )
                );
            return Resolve(QuestionOutcome.Correct, responseScore);
        }

        public AttemptResolution ResolveTimeout()
        {
            EnsureOpenWindow();
            return Resolve(QuestionOutcome.Timeout, 0);
        }

        public GameplaySnapshot VoidContentFailure()
        {
            EnsureActiveAttempt();
            _academicState = _academic.VoidReservation(
                _academicState,
                _activeAttempt.Reservation
            );
            _combat.VoidContentFailure();
            _activeAttempt = null;
            return CreateSnapshot();
        }

        public GameplaySnapshot CompletePresentation()
        {
            _combat.CompletePresentation();
            return CreateSnapshot();
        }

        private AttemptResolution Resolve(
            QuestionOutcome outcome,
            int responseScore)
        {
            QuestionReservation reservation = _activeAttempt.Reservation;
            bool correct = outcome == QuestionOutcome.Correct;
            CombatResolution combat = correct
                ? _combat.ResolveCorrect(
                    responseScore,
                    reservation.RankAtCommit.DamageMultiplier
                )
                : _combat.ResolveIncorrect(outcome == QuestionOutcome.Timeout);
            AcademicMutationResult academic = _academic.Resolve(
                _academicState,
                reservation,
                outcome,
                responseScore
            );
            _academicState = academic.State;
            _activeAttempt = null;
            GameplaySnapshot snapshot = CreateSnapshot();
            return new AttemptResolution(academic.Attempt, combat, snapshot);
        }

        private GameplaySnapshot CreateSnapshot()
        {
            return new GameplaySnapshot(
                _combat.Snapshot,
                _academicState.ToProjection(true)
            );
        }

        private void EnsureActiveAttempt()
        {
            if (_activeAttempt == null)
            {
                throw new InvalidOperationException("No attempt is committed.");
            }
        }

        private void EnsureOpenWindow()
        {
            EnsureActiveAttempt();
            if (!_activeAttempt.WindowOpen)
            {
                throw new InvalidOperationException("The answer window is not open.");
            }
        }

        private sealed class ActiveAttempt
        {
            public ActiveAttempt(QuestionReservation reservation)
            {
                Reservation = reservation;
            }

            public QuestionReservation Reservation { get; }
            public bool WindowOpen { get; private set; }
            public double PreparationEndsAt { get; private set; }
            public double AnswerEndsAt { get; private set; }

            public void OpenWindow(
                double preparationEndsAt,
                double answerEndsAt)
            {
                PreparationEndsAt = preparationEndsAt;
                AnswerEndsAt = answerEndsAt;
                WindowOpen = true;
            }
        }
    }
}
