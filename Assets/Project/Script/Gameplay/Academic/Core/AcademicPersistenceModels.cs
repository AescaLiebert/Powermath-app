using System;

namespace PowerMath.Gameplay.Academic
{
    public sealed class AcademicPersistenceSnapshot
    {
        public AcademicPersistenceSnapshot(
            AcademicRank activeRank,
            int auditResolvedCount,
            int auditScore,
            RankCurrencyBalances balances,
            RankQuestionInventorySnapshot silver,
            RankQuestionInventorySnapshot gold,
            RankQuestionInventorySnapshot diamond)
        {
            ActiveRank = activeRank;
            AuditResolvedCount = auditResolvedCount;
            AuditScore = auditScore;
            Balances = balances;
            Silver = silver ?? throw new ArgumentNullException(nameof(silver));
            Gold = gold ?? throw new ArgumentNullException(nameof(gold));
            Diamond = diamond ?? throw new ArgumentNullException(nameof(diamond));
        }

        public AcademicRank ActiveRank { get; }
        public int AuditResolvedCount { get; }
        public int AuditScore { get; }
        public RankCurrencyBalances Balances { get; }
        public RankQuestionInventorySnapshot Silver { get; }
        public RankQuestionInventorySnapshot Gold { get; }
        public RankQuestionInventorySnapshot Diamond { get; }

        public RankQuestionInventorySnapshot Get(AcademicRank rank)
        {
            switch (rank.Tier)
            {
                case AcademicRankTier.Silver: return Silver;
                case AcademicRankTier.Gold: return Gold;
                case AcademicRankTier.Diamond: return Diamond;
                default: throw new ArgumentOutOfRangeException(nameof(rank));
            }
        }
    }

    public sealed class RankQuestionInventorySnapshot
    {
        public RankQuestionInventorySnapshot(
            int cycle,
            QuestionId[] pending,
            QuestionId[] failed,
            QuestionId[] attempted,
            QuestionId[] cleared,
            QuestionId? reserved = null)
        {
            if (cycle < 0) throw new ArgumentOutOfRangeException(nameof(cycle));
            Cycle = cycle;
            Pending = pending ?? Array.Empty<QuestionId>();
            Failed = failed ?? Array.Empty<QuestionId>();
            Attempted = attempted ?? Array.Empty<QuestionId>();
            Cleared = cleared ?? Array.Empty<QuestionId>();
            Reserved = reserved;
        }

        public int Cycle { get; }
        public QuestionId[] Pending { get; }
        public QuestionId[] Failed { get; }
        public QuestionId[] Attempted { get; }
        public QuestionId[] Cleared { get; }
        public QuestionId? Reserved { get; }
    }
}
