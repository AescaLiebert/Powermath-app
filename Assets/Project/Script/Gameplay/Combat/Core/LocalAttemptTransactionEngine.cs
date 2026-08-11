using System;
using System.Globalization;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public sealed class LocalAttemptTransactionEngine : IGameplaySaveRequestFactory
    {
        private readonly ILocalEncounterEngine _combat;
        private readonly AcademicProgressionEngine _academic;
        private readonly IMonotonicClock _clock;
        private readonly double _preparationSeconds;
        private readonly double _answerSeconds;
        private readonly EventQuestionCatalog _eventQuestions;
        private readonly string _runId;

        private AcademicProgressionState _academicState;
        private ActiveAttempt _activeAttempt;
        private string _lastAttemptId = string.Empty;

        public LocalAttemptTransactionEngine(
            ILocalEncounterEngine combat,
            AcademicProgressionEngine academic,
            AcademicProgressionState academicState,
            IMonotonicClock clock,
            double preparationSeconds,
            double answerSeconds)
            : this(combat, academic, academicState, clock, preparationSeconds,
                answerSeconds, null, "legacy-run")
        {
        }

        public LocalAttemptTransactionEngine(
            ILocalEncounterEngine combat,
            AcademicProgressionEngine academic,
            AcademicProgressionState academicState,
            IMonotonicClock clock,
            double preparationSeconds,
            double answerSeconds,
            EventQuestionCatalog eventQuestions,
            string runId)
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
            _eventQuestions = eventQuestions;
            _runId = string.IsNullOrWhiteSpace(runId) ? "local-run" : runId;
        }

        public GameplaySnapshot Snapshot => CreateSnapshot();

        public AcademicPersistenceSnapshot ExportAcademicPersistence()
        {
            return _academicState.ExportPersistence();
        }

        public GameplaySaveRequest CreateSaveRequest(
            GameplaySavePoint savePoint,
            QuestionPresentationDescriptor activeQuestion = null,
            AnswerWindowReceipt? answerWindow = null,
            AttemptResolution resolution = null)
        {
            return new GameplaySaveRequest(
                savePoint,
                CreateSnapshot(),
                _academicState.ExportPersistence(),
                activeQuestion,
                answerWindow,
                resolution,
                ResolveTransactionId()
            );
        }

        public AttemptCommit CommitAttempt()
        {
            if (_activeAttempt != null)
            {
                throw new InvalidOperationException("An attempt is already committed.");
            }

            CombatSnapshot combatBefore = _combat.Snapshot;
            bool isEvent = combatBefore.IsEvent;
            QuestionReservationResult reservation = isEvent
                ? default
                : _academic.TryReserve(_academicState);
            if (!isEvent && !reservation.Success)
                throw new InvalidOperationException(reservation.Error);

            try
            {
                _combat.CommitAttempt();
            }
            catch
            {
                if (!isEvent)
                    _academic.VoidReservation(reservation.State, reservation.Reservation);
                throw;
            }

            QuestionDefinition question;
            if (isEvent)
            {
                if (_eventQuestions == null)
                {
                    _combat.VoidContentFailure();
                    throw new InvalidOperationException("Event question catalog is unavailable.");
                }
                question = _eventQuestions.Select(combatBefore.QuestionDocumentId,
                    _runId, combatBefore.Stage, combatBefore.EventAttemptOrdinal);
                _activeAttempt = ActiveAttempt.ForEvent(question, combatBefore.EnemyId,
                    combatBefore.QuestionDocumentId);
            }
            else
            {
                _academicState = reservation.State;
                question = reservation.Reservation.Question;
                _activeAttempt = ActiveAttempt.ForAcademic(reservation.Reservation);
            }
            var descriptor = new QuestionPresentationDescriptor(
                question.Id,
                isEvent ? question.Rank : reservation.Reservation.RankAtCommit,
                question.VideoUri,
                $"QA target answer: {question.CorrectAnswer}",
                question.YouTubeVideoId,
                isEvent ? QuestionContentKind.EventQuestion : QuestionContentKind.RankQuestion,
                isEvent ? combatBefore.QuestionDocumentId : string.Empty
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
            if (_activeAttempt.Question.Id != questionId)
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
                submittedAnswer == _activeAttempt.Question.CorrectAnswer;
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
            if (_activeAttempt.IsAcademic)
                _academicState = _academic.VoidReservation(
                    _academicState, _activeAttempt.Reservation);
            _combat.VoidContentFailure();
            _lastAttemptId = _activeAttempt.AttemptId;
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
            int responseDurationMilliseconds = (int)Math.Max(0d, Math.Min(
                int.MaxValue,
                Math.Round((_clock.NowSeconds -
                    (_activeAttempt.PreparationEndsAt - _preparationSeconds)) * 1000d)));
            ActiveAttempt active = _activeAttempt;
            bool correct = outcome == QuestionOutcome.Correct;
            CombatResolution combat = correct
                ? _combat.ResolveCorrect(
                    responseScore,
                    active.IsAcademic
                        ? active.Reservation.RankAtCommit.DamageMultiplier
                        : 1d
                )
                : _combat.ResolveIncorrect(outcome == QuestionOutcome.Timeout);
            AcademicAttemptResult academicResult = null;
            EventAttemptResult eventResult = null;
            if (active.IsAcademic)
            {
                AcademicMutationResult academic = _academic.Resolve(
                    _academicState, active.Reservation, outcome, responseScore);
                _academicState = academic.State;
                academicResult = academic.Attempt;
            }
            else
            {
                eventResult = new EventAttemptResult(active.EventId,
                    active.QuestionDocumentId, active.Question.Id, outcome, responseScore);
            }
            _lastAttemptId = active.AttemptId;
            _activeAttempt = null;
            GameplaySnapshot snapshot = CreateSnapshot();
            return active.IsAcademic
                ? new AttemptResolution(academicResult, combat, snapshot,
                    responseDurationMilliseconds)
                : new AttemptResolution(eventResult, combat, snapshot,
                    responseDurationMilliseconds);
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
            private ActiveAttempt(QuestionDefinition question,
                QuestionReservation? reservation, string eventId,
                string questionDocumentId)
            {
                Question = question ?? throw new ArgumentNullException(nameof(question));
                _reservation = reservation;
                EventId = eventId ?? string.Empty;
                QuestionDocumentId = questionDocumentId ?? string.Empty;
                AttemptId = Guid.NewGuid().ToString("N");
            }

            public static ActiveAttempt ForAcademic(QuestionReservation reservation) =>
                new ActiveAttempt(reservation.Question, reservation, string.Empty, string.Empty);
            public static ActiveAttempt ForEvent(QuestionDefinition question,
                string eventId, string documentId) =>
                new ActiveAttempt(question, null, eventId, documentId);

            public QuestionDefinition Question { get; }
            public QuestionReservation Reservation => _reservation.Value;
            public bool IsAcademic => _reservation.HasValue;
            public string EventId { get; }
            public string QuestionDocumentId { get; }
            public string AttemptId { get; }
            private readonly QuestionReservation? _reservation;
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

        private string ResolveTransactionId()
        {
            if (_activeAttempt != null) return _activeAttempt.AttemptId;
            if (!string.IsNullOrEmpty(_lastAttemptId)) return _lastAttemptId;
            return Guid.NewGuid().ToString("N");
        }
    }
}
