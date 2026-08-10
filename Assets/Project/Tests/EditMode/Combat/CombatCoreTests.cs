using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;

namespace PowerMath.Gameplay.Combat.Tests
{
    public sealed class CombatCoreTests
    {
        [TestCase(1, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(196, 40)]
        [TestCase(200, 40)]
        public void StageId_ReturnsExpectedWorldLevel(int stage, int expected)
        {
            Assert.That(new StageId(stage).WorldLevel, Is.EqualTo(expected));
        }

        [Test]
        public void AnswerBuffer_NormalizesLeadingZeros()
        {
            AnswerBuffer buffer = new AnswerBuffer(4);
            buffer.TryAppend(0);
            buffer.TryAppend(0);
            buffer.TryAppend(7);

            Assert.That(buffer.TryGetNormalized(out string answer), Is.True);
            Assert.That(answer, Is.EqualTo("7"));
        }

        [Test]
        public void DamageCalculator_UsesAwayFromZeroMidpointRounding()
        {
            DamageCalculator calculator = new DamageCalculator();
            DamageResult result = calculator.Calculate(
                new DamageInput(5, 1d, 1d, 50d, true)
            );

            Assert.That(result.UnroundedDamage, Is.EqualTo(7.5d));
            Assert.That(result.FinalDamage, Is.EqualTo(8));
        }

        [Test]
        public void ContentFailure_RestoresConsumedCooldown()
        {
            LocalCombatEngine engine = CreateCombatEngine(maximumCooldown: 3);

            engine.CommitAttempt();
            CombatSnapshot restored = engine.VoidContentFailure();

            Assert.That(restored.EnemyRemainingCooldown, Is.EqualTo(3));
            Assert.That(restored.Phase, Is.EqualTo(CombatPhase.EnemyReady));
        }

        [Test]
        public void LethalDamage_CancelsEnemyCounterattackAndAdvancesStage()
        {
            LocalCombatEngine engine = new LocalCombatEngine(
                new StageId(1),
                new EnemyDefinitionData("test", "Test Enemy", 1, 1),
                new MaximumRandomSource(),
                3,
                0d,
                50d
            );

            engine.CommitAttempt();
            CombatResolution result = engine.ResolveCorrect(10, 1d);

            Assert.That(result.EnemyDefeated, Is.True);
            Assert.That(result.EnemyAttacked, Is.False);
            Assert.That(result.StageAdvanced, Is.True);
            Assert.That(result.Snapshot.Stage.Value, Is.EqualTo(2));
            Assert.That(result.Snapshot.PlayerCurrentHearts, Is.EqualTo(3));
        }

        [Test]
        public void Timeout_DealsZeroAndAllowsEnemyCounterattack()
        {
            LocalCombatEngine engine = CreateCombatEngine(maximumCooldown: 1);

            engine.CommitAttempt();
            CombatResolution result = engine.ResolveTimeout();

            Assert.That(result.FinalDamage, Is.Zero);
            Assert.That(result.TimedOut, Is.True);
            Assert.That(result.EnemyAttacked, Is.True);
            Assert.That(result.Snapshot.PlayerCurrentHearts, Is.EqualTo(2));
            Assert.That(result.Snapshot.EnemyRemainingCooldown, Is.EqualTo(1));
        }

        [Test]
        public void Coordinator_SubmitAtDeadline_ResolvesTimeoutExactlyOnce()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 2);
            var coordinator = new CombatAttemptCoordinator(fixture.Gateway);
            int resolutions = 0;
            AttemptResolution captured = null;
            coordinator.AttemptResolved += result =>
            {
                resolutions++;
                captured = result;
            };

            fixture.Clock.NowSeconds = 100d;
            Assert.That(coordinator.TryBeginAttempt(out _), Is.True);
            Assert.That(coordinator.BeginAnswerWindow(), Is.True);
            coordinator.TryAppendDigit(7);
            fixture.Clock.NowSeconds = 111d;
            coordinator.TrySubmit(111d);
            coordinator.Tick(111d);

            Assert.That(resolutions, Is.EqualTo(1));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured.Combat.TimedOut, Is.True);
            Assert.That(captured.Academic.Outcome, Is.EqualTo(QuestionOutcome.Timeout));
        }

        [Test]
        public void Gateway_DuplicateCommandReturnsReceiptWithoutSecondMutation()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            var commandId = new CombatCommandId("commit-1");

            AttemptCommit first = fixture.Gateway.CommitAttempt(commandId);
            AttemptCommit duplicate = fixture.Gateway.CommitAttempt(commandId);

            Assert.That(first, Is.SameAs(duplicate));
            Assert.That(
                fixture.Gateway.Snapshot.Combat.EnemyRemainingCooldown,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void Transaction_CorrectAnswerAwardsCurrencyAndRankDamage()
        {
            GatewayFixture fixture = CreateGateway(
                maximumCooldown: 3,
                activeRank: AcademicRank.Gold
            );
            fixture.Clock.NowSeconds = 10d;
            AttemptCommit commit = fixture.Gateway.CommitAttempt(
                new CombatCommandId("commit")
            );
            fixture.Gateway.OpenAnswerWindow(
                new CombatCommandId("window"),
                commit.Question.Id
            );

            AttemptResolution result = fixture.Gateway.SubmitAnswer(
                new CombatCommandId("submit"),
                "12"
            );

            Assert.That(result.Academic.IsCorrect, Is.True);
            Assert.That(result.Academic.CurrencyDelta, Is.EqualTo(1));
            Assert.That(result.Snapshot.Academic.Balances.Gold, Is.EqualTo(1));
            Assert.That(result.Combat.FinalDamage, Is.EqualTo(15));
        }

        [Test]
        public void Transaction_IncorrectAnswerDealsZeroAndAwardsNothing()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            fixture.Clock.NowSeconds = 10d;
            AttemptCommit commit = fixture.Gateway.CommitAttempt(
                new CombatCommandId("commit")
            );
            fixture.Gateway.OpenAnswerWindow(
                new CombatCommandId("window"),
                commit.Question.Id
            );

            AttemptResolution result = fixture.Gateway.SubmitAnswer(
                new CombatCommandId("submit"),
                "3"
            );

            Assert.That(result.Academic.Outcome, Is.EqualTo(QuestionOutcome.Incorrect));
            Assert.That(result.Academic.CurrencyDelta, Is.Zero);
            Assert.That(result.Combat.FinalDamage, Is.Zero);
            Assert.That(result.Combat.IsCorrect, Is.False);
        }

        [Test]
        public void Coordinator_ContentFailureRestoresQuestionCooldownAndReadyState()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            var coordinator = new CombatAttemptCoordinator(fixture.Gateway);

            Assert.That(coordinator.TryBeginAttempt(out AttemptCommit first), Is.True);
            GameplaySnapshot restored = coordinator.VoidContentFailure();
            Assert.That(restored.Combat.EnemyRemainingCooldown, Is.EqualTo(3));
            Assert.That(coordinator.Phase, Is.EqualTo(CombatPhase.EnemyReady));

            Assert.That(coordinator.TryBeginAttempt(out AttemptCommit retry), Is.True);
            Assert.That(retry.Question.Id, Is.EqualTo(first.Question.Id));
        }

        [Test]
        public void PersistenceRequest_AfterCommitContainsReservedQuestionAndCooldown()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            AttemptCommit commit = fixture.Gateway.CommitAttempt(
                new CombatCommandId("commit-save"));

            GameplaySaveRequest request = fixture.Transaction.CreateSaveRequest(
                GameplaySavePoint.AttemptCommitted,
                commit.Question);

            Assert.That(request.Snapshot.Combat.EnemyRemainingCooldown, Is.EqualTo(2));
            Assert.That(request.ActiveQuestion.Id, Is.EqualTo(commit.Question.Id));
            Assert.That(request.Academic.Silver.Reserved, Is.EqualTo(commit.Question.Id));
        }

        [Test]
        public void CombatRestore_PreservesEnemyAndPlayerRunState()
        {
            var definition = new EnemyDefinitionData("test", "Test Enemy", 50, 3);
            var saved = new CombatSnapshot(
                new StageId(7),
                "test",
                "Test Enemy",
                31,
                55,
                1,
                3,
                2,
                3,
                CombatPhase.EnemyReady,
                false);

            var restored = new LocalCombatEngine(
                saved,
                definition,
                new MinimumRandomSource(),
                0d,
                50d);

            Assert.That(restored.Snapshot.Stage.Value, Is.EqualTo(7));
            Assert.That(restored.Snapshot.EnemyCurrentHp, Is.EqualTo(31));
            Assert.That(restored.Snapshot.EnemyRemainingCooldown, Is.EqualTo(1));
            Assert.That(restored.Snapshot.PlayerCurrentHearts, Is.EqualTo(2));
        }

        private static LocalCombatEngine CreateCombatEngine(int maximumCooldown)
        {
            return new LocalCombatEngine(
                new StageId(1),
                new EnemyDefinitionData("test", "Test Enemy", 50, maximumCooldown),
                new MinimumRandomSource(),
                3,
                0d,
                50d
            );
        }

        private static GatewayFixture CreateGateway(
            int maximumCooldown,
            AcademicRank? activeRank = null)
        {
            QuestionCatalogLoadResult catalogResult = new QuestionDocumentMapper()
                .MapCatalog(InMemoryQuestionCatalogRepository.CreateDefaultDocuments());
            Assert.That(catalogResult.IsSuccess, Is.True);
            var academic = new AcademicProgressionEngine(catalogResult.Catalog);
            AcademicProgressionState state = academic.CreateInitialState(
                activeRank ?? AcademicRank.Silver,
                new RankCurrencyBalances(0, 0, 0)
            );
            var clock = new ManualClock();
            var transaction = new LocalAttemptTransactionEngine(
                CreateCombatEngine(maximumCooldown),
                academic,
                state,
                clock,
                1d,
                10d
            );
            return new GatewayFixture(
                new LocalDevelopmentAttemptGateway(transaction),
                clock,
                transaction
            );
        }

        private sealed class GatewayFixture
        {
            public GatewayFixture(
                LocalDevelopmentAttemptGateway gateway,
                ManualClock clock,
                LocalAttemptTransactionEngine transaction)
            {
                Gateway = gateway;
                Clock = clock;
                Transaction = transaction;
            }

            public LocalDevelopmentAttemptGateway Gateway { get; }
            public ManualClock Clock { get; }
            public LocalAttemptTransactionEngine Transaction { get; }
        }

        private sealed class ManualClock : IMonotonicClock
        {
            public double NowSeconds { get; set; }
        }

        private sealed class MinimumRandomSource : IRandomSource
        {
            public int NextInclusive(int minimum, int maximum) => minimum;
            public double NextUnit() => 0d;
        }

        private sealed class MaximumRandomSource : IRandomSource
        {
            public int NextInclusive(int minimum, int maximum) => maximum;
            public double NextUnit() => 1d;
        }
    }
}
