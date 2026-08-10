using System;

namespace PowerMath.Gameplay.Academic
{
    public enum QuestionOutcome
    {
        Correct,
        Incorrect,
        Timeout,
        Abandoned
    }

    public sealed class AcademicProgressionState
    {
        public AcademicProgressionState(
            AcademicRank activeRank,
            AuditWindow audit,
            RankCurrencyBalances balances,
            RankQuestionInventorySet inventories)
        {
            ActiveRank = activeRank;
            Audit = audit ?? throw new ArgumentNullException(nameof(audit));
            Balances = balances;
            Inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        }

        public AcademicRank ActiveRank { get; internal set; }
        public AuditWindow Audit { get; internal set; }
        public RankCurrencyBalances Balances { get; internal set; }
        public RankQuestionInventorySet Inventories { get; }

        public AcademicProgressionState Clone()
        {
            return new AcademicProgressionState(
                ActiveRank,
                new AuditWindow(Audit.ResolvedCount, Audit.Score),
                Balances,
                Inventories.Clone()
            );
        }

        public AcademicPersistenceSnapshot ExportPersistence()
        {
            return new AcademicPersistenceSnapshot(
                ActiveRank,
                Audit.ResolvedCount,
                Audit.Score,
                Balances,
                Inventories.Get(AcademicRank.Silver).Export(),
                Inventories.Get(AcademicRank.Gold).Export(),
                Inventories.Get(AcademicRank.Diamond).Export()
            );
        }

        public AcademicProgressionProjection ToProjection(bool isLocal)
        {
            return new AcademicProgressionProjection(
                ActiveRank,
                Balances,
                isLocal
            );
        }
    }

    public readonly struct AcademicProgressionProjection
    {
        public AcademicProgressionProjection(
            AcademicRank activeRank,
            RankCurrencyBalances balances,
            bool isLocal)
        {
            ActiveRank = activeRank;
            Balances = balances;
            IsLocal = isLocal;
        }

        public AcademicRank ActiveRank { get; }
        public RankCurrencyBalances Balances { get; }
        public bool IsLocal { get; }
    }

    public readonly struct QuestionReservation
    {
        public QuestionReservation(QuestionDefinition question, AcademicRank rankAtCommit)
        {
            Question = question ?? throw new ArgumentNullException(nameof(question));
            RankAtCommit = rankAtCommit;
        }

        public QuestionDefinition Question { get; }
        public AcademicRank RankAtCommit { get; }
    }

    public sealed class AcademicAttemptResult
    {
        public AcademicAttemptResult(
            QuestionId questionId,
            AcademicRank rankAtCommit,
            QuestionOutcome outcome,
            int responseScore,
            long currencyDelta,
            RankTransition rankTransition,
            AcademicProgressionProjection projection)
        {
            QuestionId = questionId;
            RankAtCommit = rankAtCommit;
            Outcome = outcome;
            ResponseScore = responseScore;
            CurrencyDelta = currencyDelta;
            RankTransition = rankTransition;
            Projection = projection;
        }

        public QuestionId QuestionId { get; }
        public AcademicRank RankAtCommit { get; }
        public QuestionOutcome Outcome { get; }
        public int ResponseScore { get; }
        public long CurrencyDelta { get; }
        public RankTransition RankTransition { get; }
        public AcademicProgressionProjection Projection { get; }
        public bool IsCorrect => Outcome == QuestionOutcome.Correct;
    }

    public readonly struct QuestionReservationResult
    {
        public QuestionReservationResult(
            bool success,
            AcademicProgressionState state,
            QuestionReservation reservation,
            string error)
        {
            Success = success;
            State = state;
            Reservation = reservation;
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public AcademicProgressionState State { get; }
        public QuestionReservation Reservation { get; }
        public string Error { get; }
    }

    public readonly struct AcademicMutationResult
    {
        public AcademicMutationResult(
            AcademicProgressionState state,
            AcademicAttemptResult attempt)
        {
            State = state;
            Attempt = attempt;
        }

        public AcademicProgressionState State { get; }
        public AcademicAttemptResult Attempt { get; }
    }
}
