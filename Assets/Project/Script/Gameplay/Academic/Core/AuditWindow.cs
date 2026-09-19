using System;

namespace PowerMath.Gameplay.Academic
{
    public sealed class AuditWindow
    {
        public const int RequiredResults = 5;
        public const int MaximumResultScore = 10;
        public const int MaximumScore = RequiredResults * MaximumResultScore;

        public AuditWindow(int resolvedCount = 0, int score = 0, int correctCount = 0)
        {
            if (resolvedCount < 0 || resolvedCount >= RequiredResults)
            {
                throw new ArgumentOutOfRangeException(nameof(resolvedCount));
            }

            if (score < 0 || score > resolvedCount * MaximumResultScore)
            {
                throw new ArgumentOutOfRangeException(nameof(score));
            }

            if (correctCount < 0 || correctCount > resolvedCount)
            {
                throw new ArgumentOutOfRangeException(nameof(correctCount));
            }

            ResolvedCount = resolvedCount;
            Score = score;
            CorrectCount = correctCount;
        }

        public int ResolvedCount { get; }
        public int Score { get; }
        public int CorrectCount { get; }

        public AuditRecordResult Record(bool isCorrect, int responseScore)
        {
            int clampedScore = Math.Max(0, Math.Min(MaximumResultScore, responseScore));
            int completedScore = Math.Max(
                0,
                Math.Min(MaximumScore, Score + clampedScore)
            );
            int nextCount = ResolvedCount + 1;
            int nextCorrectCount = CorrectCount + (isCorrect ? 1 : 0);
            bool completed = nextCount == RequiredResults;

            return new AuditRecordResult(
                completed ? new AuditWindow() : new AuditWindow(nextCount, completedScore, nextCorrectCount),
                completed,
                completed ? completedScore : 0,
                completed ? nextCorrectCount : 0
            );
        }

        public AuditRecordResult Record(int responseScore)
        {
            return Record(responseScore > 0, responseScore);
        }
    }

    public readonly struct AuditRecordResult
    {
        public AuditRecordResult(
            AuditWindow nextWindow,
            bool completed,
            int completedScore,
            int completedCorrectCount = 0)
        {
            NextWindow = nextWindow;
            Completed = completed;
            CompletedScore = completedScore;
            CompletedCorrectCount = completedCorrectCount;
        }

        public AuditWindow NextWindow { get; }
        public bool Completed { get; }
        public int CompletedScore { get; }
        public int CompletedCorrectCount { get; }
    }
}
