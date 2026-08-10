using System.Linq;
using NUnit.Framework;
using PowerMath.Gameplay.Academic.Infrastructure;

namespace PowerMath.Gameplay.Academic.Tests
{
    public sealed class AcademicCoreTests
    {
        [TestCase("Silver", AcademicRankTier.Silver, 1d)]
        [TestCase("Gold", AcademicRankTier.Gold, 1.5d)]
        [TestCase("Diamond", AcademicRankTier.Diamond, 2d)]
        public void AcademicRank_ParsesExactAndReturnsMultiplier(
            string value,
            AcademicRankTier expectedTier,
            double expectedMultiplier)
        {
            Assert.That(AcademicRank.TryParseExact(value, out AcademicRank rank), Is.True);
            Assert.That(rank.Tier, Is.EqualTo(expectedTier));
            Assert.That(rank.DamageMultiplier, Is.EqualTo(expectedMultiplier));
        }

        [TestCase("silver")]
        [TestCase("Platinum")]
        [TestCase("")]
        public void AcademicRank_RejectsUnknownWireValues(string value)
        {
            Assert.That(AcademicRank.TryParseExact(value, out _), Is.False);
        }

        [TestCase(AcademicRankTier.Gold, 25, AcademicRankTier.Silver)]
        [TestCase(AcademicRankTier.Gold, 26, AcademicRankTier.Gold)]
        [TestCase(AcademicRankTier.Gold, 39, AcademicRankTier.Gold)]
        [TestCase(AcademicRankTier.Gold, 40, AcademicRankTier.Diamond)]
        [TestCase(AcademicRankTier.Silver, 0, AcademicRankTier.Silver)]
        [TestCase(AcademicRankTier.Diamond, 50, AcademicRankTier.Diamond)]
        public void RankPolicy_UsesGddThresholdsAndBounds(
            AcademicRankTier current,
            int score,
            AcademicRankTier expected)
        {
            RankTransition transition = RankProgressionPolicy.Evaluate(
                new AcademicRank(current),
                score
            );
            Assert.That(transition.Current.Tier, Is.EqualTo(expected));
        }

        [Test]
        public void AuditWindow_FifthResultCompletesAndResets()
        {
            var audit = new AuditWindow();
            AuditRecordResult result = default;
            for (int index = 0; index < 5; index++)
            {
                result = audit.Record(20);
                audit = result.NextWindow;
            }

            Assert.That(result.Completed, Is.True);
            Assert.That(result.CompletedScore, Is.EqualTo(50));
            Assert.That(audit.ResolvedCount, Is.Zero);
            Assert.That(audit.Score, Is.Zero);
        }

        [Test]
        public void AuditWindow_ClampsEveryResultToZeroThroughTen()
        {
            var audit = new AuditWindow();
            audit = audit.Record(-4).NextWindow;
            audit = audit.Record(18).NextWindow;

            Assert.That(audit.ResolvedCount, Is.EqualTo(2));
            Assert.That(audit.Score, Is.EqualTo(10));
        }

        [Test]
        public void DefaultQuestionDocuments_BuildValidThreeRankCatalog()
        {
            QuestionCatalogLoadResult result = new QuestionDocumentMapper()
                .MapCatalog(InMemoryQuestionCatalogRepository.CreateDefaultDocuments());

            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            Assert.That(result.Catalog.GetRankQuestions(AcademicRank.Silver).Count, Is.EqualTo(5));
            Assert.That(result.Catalog.GetRankQuestions(AcademicRank.Gold).Count, Is.EqualTo(5));
            Assert.That(result.Catalog.GetRankQuestions(AcademicRank.Diamond).Count, Is.EqualTo(5));
        }

        [Test]
        public void QuestionMapper_AllowsSameIdAcrossRanksButRejectsWithinRank()
        {
            RankedQuestionDocument[] documents =
                InMemoryQuestionCatalogRepository.CreateDefaultDocuments();

            QuestionCatalogLoadResult valid = new QuestionDocumentMapper()
                .MapCatalog(documents);
            Assert.That(valid.IsSuccess, Is.True, string.Join("; ", valid.Errors));

            documents[1].Document.id = documents[0].Document.id;

            QuestionCatalogLoadResult result = new QuestionDocumentMapper()
                .MapCatalog(documents);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("Duplicate")), Is.True);
        }

        [TestCase("http://www.youtube.com/watch?v=M7lc1UVf-VE")]
        [TestCase("https://example.com/watch?v=M7lc1UVf-VE")]
        [TestCase("")]
        public void QuestionMapper_RejectsUnsupportedVideoLink(string link)
        {
            var document = new QuestionDocumentDto
            {
                id = 1,
                answer = 1,
                video_link = link
            };

            Assert.That(
                new QuestionDocumentMapper().TryMap(
                    document,
                    AcademicRank.Silver,
                    out _,
                    out _),
                Is.False
            );
        }

        [TestCase(-1L, 1L)]
        [TestCase(1L, -1L)]
        [TestCase(1L, 2147483648L)]
        public void QuestionMapper_RejectsInvalidIdentityOrAnswer(long id, long answer)
        {
            var document = new QuestionDocumentDto
            {
                id = id,
                answer = answer,
                video_link = "https://www.youtube.com/watch?v=M7lc1UVf-VE"
            };

            Assert.That(
                new QuestionDocumentMapper().TryMap(
                    document,
                    AcademicRank.Silver,
                    out _,
                    out _),
                Is.False
            );
        }

        [TestCase("https://youtu.be/M7lc1UVf-VE")]
        [TestCase("https://www.youtube.com/watch?v=M7lc1UVf-VE")]
        [TestCase("https://www.youtube.com/embed/M7lc1UVf-VE")]
        [TestCase("https://www.youtube.com/shorts/M7lc1UVf-VE")]
        public void QuestionMapper_AcceptsSupportedYouTubeLinks(string link)
        {
            var document = new QuestionDocumentDto
            {
                id = 1,
                answer = 7,
                video_link = link
            };

            Assert.That(
                new QuestionDocumentMapper().TryMap(
                    document,
                    AcademicRank.Silver,
                    out QuestionDefinition definition,
                    out string error),
                Is.True,
                error
            );
            Assert.That(definition.YouTubeVideoId, Is.EqualTo("M7lc1UVf-VE"));
        }

        [Test]
        public void QuestionCatalog_RejectsRankWithFewerThanFiveQuestions()
        {
            RankedQuestionDocument[] documents =
                InMemoryQuestionCatalogRepository.CreateDefaultDocuments();

            QuestionCatalogLoadResult result = new QuestionDocumentMapper()
                .MapCatalog(documents.Take(documents.Length - 1));

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(
                result.Errors.Any(error => error.Contains("at least 5")),
                Is.True
            );
        }

        [Test]
        public void Inventory_RequeuesFailuresAtFrontInFailureOrder()
        {
            QuestionCatalog catalog = LoadCatalog();
            var inventory = new RankQuestionInventory(
                AcademicRank.Silver,
                catalog.GetRankQuestions(AcademicRank.Silver).Select(item => item.Id)
            );

            inventory.TryReserve(out QuestionId first);
            inventory.ResolveReserved(false);
            inventory.TryReserve(out QuestionId second);
            inventory.ResolveReserved(false);
            inventory.CompleteAudit();

            Assert.That(inventory.PendingQuestions[0], Is.EqualTo(first));
            Assert.That(inventory.PendingQuestions[1], Is.EqualTo(second));
        }

        [Test]
        public void Inventory_NewCyclePreservesNoRepeatWithinAudit()
        {
            QuestionCatalog catalog = LoadCatalog();
            var inventory = new RankQuestionInventory(
                AcademicRank.Silver,
                catalog.GetRankQuestions(AcademicRank.Silver).Select(item => item.Id)
            );

            for (int index = 0; index < 5; index++)
            {
                Assert.That(inventory.TryReserve(out _), Is.True);
                inventory.ResolveReserved(true);
            }

            Assert.That(inventory.TryReserve(out _), Is.False);
            inventory.CompleteAudit();
            Assert.That(inventory.TryReserve(out _), Is.True);
            Assert.That(inventory.CycleNumber, Is.EqualTo(1));
        }

        [Test]
        public void Progression_RankInventoriesAdvanceIndependently()
        {
            QuestionCatalog catalog = LoadCatalog();
            var engine = new AcademicProgressionEngine(catalog);
            AcademicProgressionState state = engine.CreateInitialState(
                AcademicRank.Silver,
                new RankCurrencyBalances(0, 0, 0)
            );
            QuestionId goldHead = state.Inventories
                .Get(AcademicRank.Gold)
                .PendingQuestions[0];

            QuestionReservationResult reservation = engine.TryReserve(state);
            AcademicMutationResult mutation = engine.Resolve(
                reservation.State,
                reservation.Reservation,
                QuestionOutcome.Correct,
                10
            );

            Assert.That(
                mutation.State.Inventories.Get(AcademicRank.Gold).PendingQuestions[0],
                Is.EqualTo(goldHead)
            );
            Assert.That(
                mutation.State.Inventories.Get(AcademicRank.Silver).PendingQuestions.Count,
                Is.EqualTo(4)
            );
        }

        [Test]
        public void Progression_FifthFastCorrectUsesOldRankThenPromotes()
        {
            QuestionCatalog catalog = LoadCatalog();
            var engine = new AcademicProgressionEngine(catalog);
            AcademicProgressionState state = engine.CreateInitialState(
                AcademicRank.Silver,
                new RankCurrencyBalances(0, 0, 0)
            );
            AcademicAttemptResult fifth = null;

            for (int index = 0; index < 5; index++)
            {
                QuestionReservationResult reservation = engine.TryReserve(state);
                Assert.That(reservation.Success, Is.True);
                AcademicMutationResult mutation = engine.Resolve(
                    reservation.State,
                    reservation.Reservation,
                    QuestionOutcome.Correct,
                    10
                );
                state = mutation.State;
                fifth = mutation.Attempt;
            }

            Assert.That(fifth.RankAtCommit, Is.EqualTo(AcademicRank.Silver));
            Assert.That(fifth.RankTransition.IsPromotion, Is.True);
            Assert.That(state.ActiveRank, Is.EqualTo(AcademicRank.Gold));
            Assert.That(state.Balances.Silver, Is.EqualTo(5));
            Assert.That(state.Balances.Gold, Is.Zero);
            Assert.That(state.Audit.ResolvedCount, Is.Zero);
        }

        [Test]
        public void Progression_VoidRestoresReservationWithoutAuditMutation()
        {
            QuestionCatalog catalog = LoadCatalog();
            var engine = new AcademicProgressionEngine(catalog);
            AcademicProgressionState state = engine.CreateInitialState(
                AcademicRank.Silver,
                new RankCurrencyBalances(0, 0, 0)
            );
            QuestionReservationResult reservation = engine.TryReserve(state);

            AcademicProgressionState restored = engine.VoidReservation(
                reservation.State,
                reservation.Reservation
            );

            Assert.That(restored.Audit.ResolvedCount, Is.Zero);
            Assert.That(restored.Balances.Silver, Is.Zero);
            Assert.That(
                restored.Inventories.Get(AcademicRank.Silver).HasReservation,
                Is.False
            );
            Assert.That(
                restored.Inventories.Get(AcademicRank.Silver).PendingQuestions[0],
                Is.EqualTo(reservation.Reservation.Question.Id)
            );
        }

        [Test]
        public void Progression_PersistenceRoundTripPreservesAuditAndRankQueues()
        {
            QuestionCatalog catalog = LoadCatalog();
            var engine = new AcademicProgressionEngine(catalog);
            AcademicProgressionState state = engine.CreateInitialState(
                AcademicRank.Silver,
                new RankCurrencyBalances(0, 0, 0)
            );
            QuestionReservationResult reservation = engine.TryReserve(state);
            state = engine.Resolve(
                reservation.State,
                reservation.Reservation,
                QuestionOutcome.Incorrect,
                0
            ).State;

            AcademicProgressionState restored = engine.Rehydrate(
                state.ExportPersistence()
            );

            Assert.That(restored.Audit.ResolvedCount, Is.EqualTo(1));
            Assert.That(restored.Audit.Score, Is.Zero);
            Assert.That(
                restored.Inventories.Get(AcademicRank.Silver).FailedCount,
                Is.EqualTo(1)
            );
            Assert.That(
                restored.Inventories.Get(AcademicRank.Silver).PendingQuestions,
                Is.EqualTo(state.Inventories.Get(AcademicRank.Silver).PendingQuestions)
            );
        }

        private static QuestionCatalog LoadCatalog()
        {
            QuestionCatalogLoadResult result = new QuestionDocumentMapper()
                .MapCatalog(InMemoryQuestionCatalogRepository.CreateDefaultDocuments());
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            return result.Catalog;
        }
    }
}
