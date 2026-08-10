using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public sealed class GameplaySnapshot
    {
        public GameplaySnapshot(
            CombatSnapshot combat,
            AcademicProgressionProjection academic)
        {
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Academic = academic;
        }

        public CombatSnapshot Combat { get; }
        public AcademicProgressionProjection Academic { get; }
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
            GameplaySnapshot snapshot)
        {
            Question = question ?? throw new ArgumentNullException(nameof(question));
            AnswerPolicy = answerPolicy;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public QuestionPresentationDescriptor Question { get; }
        public AnswerInputPolicy AnswerPolicy { get; }
        public GameplaySnapshot Snapshot { get; }
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
            GameplaySnapshot snapshot)
        {
            Academic = academic ?? throw new ArgumentNullException(nameof(academic));
            Combat = combat ?? throw new ArgumentNullException(nameof(combat));
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        public AcademicAttemptResult Academic { get; }
        public CombatResolution Combat { get; }
        public GameplaySnapshot Snapshot { get; }
    }
}
