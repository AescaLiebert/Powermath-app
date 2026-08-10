using System;

namespace PowerMath.Gameplay.Academic
{
    public sealed class AuditWindow
    {
        public const int RequiredResults = 5;
        public const int MaximumResultScore = 10;
        public const int MaximumScore = RequiredResults * MaximumResultScore;

        public AuditWindow(int resolvedCount = 0, int score = 0)
        {
            if (resolvedCount < 0 || resolvedCount >= RequiredResults)
            {
                throw new ArgumentOutOfRangeException(nameof(resolvedCount));
            }

            if (score < 0 || score > resolvedCount * MaximumResultScore)
            {
                throw new ArgumentOutOfRangeException(nameof(score));
            }

            ResolvedCount = resolvedCount;
            Score = score;
        }

        public int ResolvedCount { get; }
        public int Score { get; }

        public AuditRecordResult Record(int responseScore)
        {
            int clampedScore = Math.Max(0, Math.Min(MaximumResultScore, responseScore));
            int completedScore = Math.Max(
                0,
                Math.Min(MaximumScore, Score + clampedScore)
            );
            int nextCount = ResolvedCount + 1;
            bool completed = nextCount == RequiredResults;

            return new AuditRecordResult(
                completed ? new AuditWindow() : new AuditWindow(nextCount, completedScore),
                completed,
                completed ? completedScore : 0
            );
        }
    }

    public readonly struct AuditRecordResult
    {
        public AuditRecordResult(
            AuditWindow nextWindow,
            bool completed,
            int completedScore)
        {
            NextWindow = nextWindow;
            Completed = completed;
            CompletedScore = completedScore;
        }

        public AuditWindow NextWindow { get; }
        public bool Completed { get; }
        public int CompletedScore { get; }
    }
}
