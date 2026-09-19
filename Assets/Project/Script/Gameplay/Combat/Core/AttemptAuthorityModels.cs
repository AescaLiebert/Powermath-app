using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public sealed class GameplaySnapshot
    {
        public GameplaySnapshot(
            CombatSnapshot combat,
            AcademicProgressionProjection academic,
            long powerCoins = 0)
        {
            if (powerCoins < 0) throw new ArgumentOutOfRangeException(nameof(powerCoins));
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Academic = academic;
            PowerCoins = powerCoins;
        }

        public CombatSnapshot Combat { get; }
        public AcademicProgressionProjection Academic { get; }
        public long PowerCoins { get; }
    }

    public readonly struct AnswerInputPolicy
    {
        public AnswerInputPolicy(int maximumLength)
        {
            if (maximumLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLength));
            }

            MaximumLength = maximumLength;
        }

        public int MaximumLength { get; }
    }

    public sealed class AttemptCommit
    {
        public AttemptCommit(
            QuestionPresentationDescriptor question,
            AnswerInputPolicy answerPolicy,
            GameplaySnapshot snapshot,
            string presentationId = "")
        {
            Question = question ?? throw new ArgumentNullException(nameof(question));
            AnswerPolicy = answerPolicy;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            PresentationId = presentationId ?? string.Empty;
        }

        public QuestionPresentationDescriptor Question { get; }
        public AnswerInputPolicy AnswerPolicy { get; }
        public GameplaySnapshot Snapshot { get; }
        public string PresentationId { get; }
    }

    public readonly struct AnswerWindowReceipt
    {
        public AnswerWindowReceipt(
            double preparationEndsAt,
            double answerEndsAt)
        {
            if (answerEndsAt <= preparationEndsAt)
            {
                throw new ArgumentOutOfRangeException(nameof(answerEndsAt));
            }

            PreparationEndsAt = preparationEndsAt;
            AnswerEndsAt = answerEndsAt;
        }

        public double PreparationEndsAt { get; }
        public double AnswerEndsAt { get; }
    }

    public sealed class AttemptResolution
    {
        public AttemptResolution(
            AcademicAttemptResult academic,
            CombatResolution combat,
            GameplaySnapshot snapshot,
            int responseDurationMilliseconds = 0,
            AttemptPresentationReceipt presentation = null)
        {
            Academic = academic ?? throw new ArgumentNullException(nameof(academic));
            ContentKind = QuestionContentKind.RankQuestion;
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            ResponseDurationMilliseconds = Math.Max(0, responseDurationMilliseconds);
            Presentation = presentation;
        }

        public AcademicAttemptResult Academic { get; }
        public EventAttemptResult Event { get; }
        public QuestionContentKind ContentKind { get; }
        public bool IsAcademic => ContentKind == QuestionContentKind.RankQuestion;
        public CombatResolution Combat { get; }
        public GameplaySnapshot Snapshot { get; }
        public int ResponseDurationMilliseconds { get; }
        public AttemptPresentationReceipt Presentation { get; }

        public AttemptResolution(
            EventAttemptResult eventResult,
            CombatResolution combat,
            GameplaySnapshot snapshot,
            int responseDurationMilliseconds = 0,
            AttemptPresentationReceipt presentation = null)
        {
            Event = eventResult ?? throw new ArgumentNullException(nameof(eventResult));
            ContentKind = QuestionContentKind.EventQuestion;
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            ResponseDurationMilliseconds = Math.Max(0, responseDurationMilliseconds);
            Presentation = presentation;
        }
    }

    public sealed class EventAttemptResult
    {
        public EventAttemptResult(string eventId, string questionDocumentId,
            ChallengeQuestionId questionId, QuestionOutcome outcome, int responseScore, int biomeIndex = 1)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("Event ID is required.", nameof(eventId));
            if (string.IsNullOrWhiteSpace(questionDocumentId))
                throw new ArgumentException("Question document ID is required.", nameof(questionDocumentId));
            EventId = eventId;
            QuestionDocumentId = questionDocumentId;
            QuestionId = questionId;
            Outcome = outcome;
            ResponseScore = Math.Max(0, Math.Min(10, responseScore));
            PowerCoinsGranted = ChallengeRewardPolicy.Calculate(outcome, responseScore, biomeIndex);
        }

        public string EventId { get; }
        public string QuestionDocumentId { get; }
        public ChallengeQuestionId QuestionId { get; }
        public QuestionOutcome Outcome { get; }
        public int ResponseScore { get; }
        public bool IsCorrect => Outcome == QuestionOutcome.Correct;
        public int PowerCoinsGranted { get; }
    }
}
