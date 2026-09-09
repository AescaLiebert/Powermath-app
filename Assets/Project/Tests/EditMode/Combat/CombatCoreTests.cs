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
                new DamageInput(5, 1d, 1d, 50d, true, 5)
            );

            Assert.That(result.UnroundedDamage, Is.EqualTo(7.5d));
            Assert.That(result.FinalDamage, Is.EqualTo(8));
        }

        [TestCase(1, 20, 10)]
        [TestCase(2, 40, 20)]
        [TestCase(3, 60, 30)]
        [TestCase(4, 80, 40)]
        [TestCase(5, 100, 50)]
        [TestCase(6, 120, 60)]
        [TestCase(7, 140, 70)]
        [TestCase(8, 160, 80)]
        [TestCase(9, 180, 90)]
        [TestCase(10, 200, 100)]
        public void DamageCalculator_AppliesResponseScoreMultiplier(
            int responseScore,
            int expectedPercent,
            int expectedDamage)
        {
            DamageResult result = new DamageCalculator().Calculate(
                new DamageInput(50, 1d, 1d, 0d, false, responseScore));

            Assert.That(
                ResponseDamagePolicy.GetPercent(responseScore),
                Is.EqualTo(expectedPercent));
            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
            Assert.That(
                result.ResponseDamageMultiplier,
                Is.EqualTo(expectedPercent / 100d));
            Assert.That(result.Breakdown.IsAvailable, Is.True);
            Assert.That(result.Breakdown.BaseAttack, Is.EqualTo(50));
            Assert.That(result.Breakdown.RankMultiplier, Is.EqualTo(1d));
            Assert.That(result.Breakdown.BuffMultiplier, Is.EqualTo(1d));
            Assert.That(result.Breakdown.ResponseScore, Is.EqualTo(responseScore));
            Assert.That(result.Breakdown.FinalDamage, Is.EqualTo(expectedDamage));
        }

        [Test]
        public void QuestionPresentationResult_ReportsRetainedSurfaceExplicitly()
        {
            var result = new QuestionPresentationResult(
                QuestionPresentationStatus.Ready,
                "Enter your answer.",
                true);

            Assert.That(result.IsReady, Is.True);
            Assert.That(result.RetainsPresentationSurface, Is.True);
        }

        [TestCase(0)]
        [TestCase(11)]
        public void DamageCalculator_RejectsInvalidCorrectResponseScore(int responseScore)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new DamageInput(50, 1d, 1d, 0d, false, responseScore));
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
            Assert.That(result.Combat.ResponseScore, Is.EqualTo(10));
            Assert.That(result.Combat.ResponseDamagePercent, Is.EqualTo(200));
            Assert.That(result.Combat.FinalDamage, Is.EqualTo(15));
        }

        [Test]
        public void Transaction_ResolutionCreatesMatchingPendingPresentationReceipt()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            fixture.Clock.NowSeconds = 10d;
            AttemptCommit commit = fixture.Gateway.CommitAttempt(
                new CombatCommandId("commit-receipt"));
            fixture.Gateway.OpenAnswerWindow(
                new CombatCommandId("window-receipt"),
                commit.Question.Id);

            AttemptResolution result = fixture.Gateway.SubmitAnswer(
                new CombatCommandId("submit-receipt"), "12");

            Assert.That(result.Presentation, Is.Not.Null);
            Assert.That(fixture.Gateway.PendingPresentation,
                Is.SameAs(result.Presentation));
            Assert.That(result.Presentation.AttemptId, Is.Not.Empty);
            Assert.That(result.Presentation.PresentationId,
                Is.EqualTo(commit.PresentationId));
            Assert.That(result.Presentation.FinalDamage,
                Is.EqualTo(result.Combat.FinalDamage));
            Assert.That(result.Presentation.Source.EnemyCurrentHp, Is.EqualTo(48));
            Assert.That(result.Presentation.Destination.Phase,
                Is.EqualTo(CombatPhase.PresentingResult));
        }

        [Test]
        public void Transaction_CompletionRejectsMismatchedPresentationId()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            fixture.Clock.NowSeconds = 10d;
            AttemptCommit commit = fixture.Gateway.CommitAttempt(
                new CombatCommandId("commit-mismatch"));
            fixture.Gateway.OpenAnswerWindow(
                new CombatCommandId("window-mismatch"),
                commit.Question.Id);
            AttemptResolution result = fixture.Gateway.SubmitAnswer(
                new CombatCommandId("submit-mismatch"), "12");

            Assert.That(
                () => fixture.Gateway.CompletePresentation(
                    new CombatCommandId("complete-wrong"), "wrong-id"),
                Throws.InvalidOperationException);
            Assert.That(fixture.Gateway.PendingPresentation,
                Is.SameAs(result.Presentation));

            GameplaySnapshot completed = fixture.Gateway.CompletePresentation(
                new CombatCommandId("complete-right"),
                result.Presentation.PresentationId);

            Assert.That(fixture.Gateway.PendingPresentation, Is.Null);
            Assert.That(completed.Combat.Phase, Is.EqualTo(CombatPhase.EnemyReady));
        }

        [Test]
        public void Transaction_ConstructorWithPendingPresentation_RejectsReadyCombatPhase()
        {
            QuestionCatalogLoadResult catalogResult = new QuestionDocumentMapper()
                .MapCatalog(InMemoryQuestionCatalogRepository.CreateDefaultDocuments());
            var academic = new AcademicProgressionEngine(catalogResult.Catalog);
            AcademicProgressionState state = academic.CreateInitialState(
                AcademicRank.Silver,
                new RankCurrencyBalances(0, 0, 0)
            );
            var clock = new ManualClock();
            LocalCombatEngine readyCombat = CreateCombatEngine(maximumCooldown: 3);
            var dummyReceipt = new AttemptPresentationReceipt(
                "p-1", "a-1", AttemptOutcomeKind.Correct, 10, 5, false,
                CombatPresentationSnapshot.From(readyCombat.Snapshot),
                CombatPresentationSnapshot.From(readyCombat.Snapshot),
                10, false, false, false, false, false, default);

            Assert.Throws<System.InvalidOperationException>(() =>
                new LocalAttemptTransactionEngine(
                    readyCombat, academic, state, clock, 1d, 10d, null, "run-1", dummyReceipt));
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

        [Test]
        public void StageHpPolicy_ScalesFromMonsterBaseHpWithWorldLevelGrowth()
        {
            var map = DevelopmentStageMapFactory.Create();
            string runId = "test-run";

            // Stage 1: World Level 1 -> Growth = 1.0x
            var normalMonster = new MonsterData("test-normal", "Test Normal", StageEncounterKind.NormalMonster, "biome-1", 3, 30);
            int stage1Hp = StageHpPolicy.Calculate(map, normalMonster, runId, new StageId(1));
            // Expect 30 +/- 7% variation -> between 27 and 33
            Assert.That(stage1Hp, Is.InRange(27, 33));

            // Stage 30: World Level 6 -> Growth = 1.0 + 5 * 0.12 = 1.60x
            var bigBoss = new MonsterData("test-boss", "Test Big Boss", StageEncounterKind.BigBoss, "biome-1", 2, 100);
            int stage30Hp = StageHpPolicy.Calculate(map, bigBoss, runId, new StageId(30));
            // 100 * 1.60 = 160 +/- 7% variation -> between 148 and 172
            Assert.That(stage30Hp, Is.InRange(148, 172));

            // Stage 200: World Level 40 -> Growth = 1.0 + 39 * 0.12 = 5.68x
            var finalBoss = new MonsterData("test-final", "Test Final Boss", StageEncounterKind.FinalBoss, "biome-7", 2, 5000);
            int stage200Hp = StageHpPolicy.Calculate(map, finalBoss, runId, new StageId(200));
            // 5000 * 5.68 = 28400 +/- 7% variation -> between 26412 and 30388
            Assert.That(stage200Hp, Is.InRange(26412, 30388));
        }

        [Test]
        public void StageClassificationPolicy_CorrectlyClassifiesFlexibleBiomeBossCadence()
        {
            // Biome 5: 121 - 160 (Non-final biome with 40 stages)
            // Stage 150: multiple of 30, but intermediate -> MiniBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(150), biomeLastStage: 160, isFinalBiome: false),
                Is.EqualTo(StageEncounterKind.MiniBoss)
            );
            // Stage 155: intermediate multiple of 5 -> MiniBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(155), biomeLastStage: 160, isFinalBiome: false),
                Is.EqualTo(StageEncounterKind.MiniBoss)
            );
            // Stage 160: last stage of Biome 5 -> BigBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(160), biomeLastStage: 160, isFinalBiome: false),
                Is.EqualTo(StageEncounterKind.BigBoss)
            );

            // Biome 6: 161 - 190
            // Stage 180: multiple of 30, but intermediate -> MiniBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(180), biomeLastStage: 190, isFinalBiome: false),
                Is.EqualTo(StageEncounterKind.MiniBoss)
            );
            // Stage 190: last stage of Biome 6 -> BigBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(190), biomeLastStage: 190, isFinalBiome: false),
                Is.EqualTo(StageEncounterKind.BigBoss)
            );

            // Biome 7: 191 - 215 (Final biome)
            // Stage 200: intermediate multiple of 5 -> MiniBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(200), biomeLastStage: 215, isFinalBiome: true),
                Is.EqualTo(StageEncounterKind.MiniBoss)
            );
            // Stage 215: last stage of Final Biome -> FinalBoss
            Assert.That(
                StageClassificationPolicy.Classify(new StageId(215), biomeLastStage: 215, isFinalBiome: true),
                Is.EqualTo(StageEncounterKind.FinalBoss)
            );

            // Protected stages check
            Assert.That(StageClassificationPolicy.IsProtected(new StageId(150)), Is.True);
            Assert.That(StageClassificationPolicy.IsProtected(new StageId(160)), Is.True);
            Assert.That(StageClassificationPolicy.IsProtected(new StageId(161)), Is.False);
            Assert.That(StageClassificationPolicy.IsProtected(new StageId(215)), Is.True);
        }

        [Test]
        public void DevelopmentStageMapFactory_BuildsValid215StageJourney()
        {
            var map = DevelopmentStageMapFactory.Create();
            Assert.That(map.Biomes.Count, Is.EqualTo(7));
            Assert.That(map.Biomes[0].FirstStage, Is.EqualTo(1));
            Assert.That(map.Biomes[0].LastStage, Is.EqualTo(30));
            Assert.That(map.Biomes[4].FirstStage, Is.EqualTo(121));
            Assert.That(map.Biomes[4].LastStage, Is.EqualTo(160));
            Assert.That(map.Biomes[5].FirstStage, Is.EqualTo(161));
            Assert.That(map.Biomes[5].LastStage, Is.EqualTo(190));
            Assert.That(map.Biomes[6].FirstStage, Is.EqualTo(191));
            Assert.That(map.Biomes[6].LastStage, Is.EqualTo(215));

            var resolver = new StageEncounterResolver(map);
            var encounter150 = resolver.Resolve("test-run", new StageId(150));
            Assert.That(encounter150.Kind, Is.EqualTo(StageEncounterKind.MiniBoss));

            var encounter160 = resolver.Resolve("test-run", new StageId(160));
            Assert.That(encounter160.Kind, Is.EqualTo(StageEncounterKind.BigBoss));

            var encounter215 = resolver.Resolve("test-run", new StageId(215));
            Assert.That(encounter215.Kind, Is.EqualTo(StageEncounterKind.FinalBoss));
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
