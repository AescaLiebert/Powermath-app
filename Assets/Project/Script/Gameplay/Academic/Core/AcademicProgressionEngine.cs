using System;

namespace PowerMath.Gameplay.Academic
{
    public sealed class AcademicProgressionEngine
    {
        private readonly QuestionCatalog _catalog;
        private readonly long _correctCurrencyAward;

        public AcademicProgressionEngine(
            QuestionCatalog catalog,
            long correctCurrencyAward = 1)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (correctCurrencyAward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(correctCurrencyAward));
            }

            _correctCurrencyAward = correctCurrencyAward;
        }

        public AcademicProgressionState CreateInitialState(
            AcademicRank activeRank,
            RankCurrencyBalances balances)
        {
            return new AcademicProgressionState(
                activeRank,
                new AuditWindow(),
                balances,
                new RankQuestionInventorySet(_catalog)
            );
        }

        public AcademicProgressionState Rehydrate(AcademicPersistenceSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return new AcademicProgressionState(
                snapshot.ActiveRank,
                new AuditWindow(snapshot.AuditResolvedCount, snapshot.AuditScore),
                snapshot.Balances,
                new RankQuestionInventorySet(_catalog, snapshot)
            );
        }

        public QuestionReservationResult TryReserve(
            AcademicProgressionState source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            AcademicProgressionState next = source.Clone();
            RankQuestionInventory inventory = next.Inventories.Get(next.ActiveRank);
            if (!inventory.TryReserve(out QuestionId id) ||
                !_catalog.TryGet(next.ActiveRank, id, out QuestionDefinition question))
            {
                return new QuestionReservationResult(
                    false,
                    source,
                    default,
                    "No distinct question is available for the active audit."
                );
            }

            return new QuestionReservationResult(
                true,
                next,
                new QuestionReservation(question, next.ActiveRank),
                string.Empty
            );
        }

        public AcademicProgressionState VoidReservation(
            AcademicProgressionState source,
            QuestionReservation reservation)
        {
            AcademicProgressionState next = source?.Clone() ??
                throw new ArgumentNullException(nameof(source));
            RankQuestionInventory inventory = next.Inventories.Get(
                reservation.RankAtCommit
            );
            EnsureReservationMatches(inventory, reservation.Question.Id);
            inventory.VoidReserved();
            return next;
        }

        public AcademicMutationResult Resolve(
            AcademicProgressionState source,
            QuestionReservation reservation,
            QuestionOutcome outcome,
            int responseScore)
        {
            AcademicProgressionState next = source?.Clone() ??
                throw new ArgumentNullException(nameof(source));
            RankQuestionInventory inventory = next.Inventories.Get(
                reservation.RankAtCommit
            );
            EnsureReservationMatches(inventory, reservation.Question.Id);

            bool correct = outcome == QuestionOutcome.Correct;
            int appliedScore = correct
                ? Math.Max(1, Math.Min(AuditWindow.MaximumResultScore, responseScore))
                : 0;
            long currencyDelta = correct ? _correctCurrencyAward : 0;

            inventory.ResolveReserved(correct);
            if (currencyDelta > 0)
            {
                next.Balances = next.Balances.Add(
                    reservation.RankAtCommit,
                    currencyDelta
                );
            }

            AuditRecordResult audit = next.Audit.Record(appliedScore);
            next.Audit = audit.NextWindow;
            RankTransition transition = new RankTransition(
                next.ActiveRank,
                next.ActiveRank
            );
            if (audit.Completed)
            {
                inventory.CompleteAudit();
                transition = RankProgressionPolicy.Evaluate(
                    next.ActiveRank,
                    audit.CompletedScore
                );
                next.ActiveRank = transition.Current;
            }

            var result = new AcademicAttemptResult(
                reservation.Question.Id,
                reservation.RankAtCommit,
                outcome,
                appliedScore,
                currencyDelta,
                transition,
                next.ToProjection(true)
            );
            return new AcademicMutationResult(next, result);
        }

        private static void EnsureReservationMatches(
            RankQuestionInventory inventory,
            QuestionId expected)
        {
            if (!inventory.HasReservation ||
                inventory.ReservedQuestionId != expected)
            {
                throw new InvalidOperationException(
                    "The academic question reservation does not match the active attempt."
                );
            }
        }
    }
}
