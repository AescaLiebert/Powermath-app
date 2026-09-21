using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Pets;

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
                new DamageInput(5, 1d, 1d, 50d, true, 1)
            );

            Assert.That(result.UnroundedDamage, Is.EqualTo(7.5d));
            Assert.That(result.FinalDamage, Is.EqualTo(8));
        }

        [TestCase(0d, 0.90d, 90)]
        [TestCase(0.5d, 1.00d, 100)]
        [TestCase(1d, 1.10d, 110)]
        public void DamageCalculator_AppliesAttackVariance(
            double randomUnit,
            double expectedMultiplier,
            int expectedDamage)
        {
            double multiplier = AttackDamageVariancePolicy.GetMultiplier(randomUnit);
            DamageResult result = new DamageCalculator().Calculate(
                new DamageInput(100, 1d, 1d, 0d, false, 1, multiplier));

            Assert.That(multiplier, Is.EqualTo(expectedMultiplier).Within(0.000001d));
            Assert.That(result.FinalDamage, Is.EqualTo(expectedDamage));
            Assert.That(result.Breakdown.VarianceMultiplier,
                Is.EqualTo(expectedMultiplier).Within(0.000001d));
        }

        [TestCase(-0.000001d)]
        [TestCase(1.000001d)]
        public void AttackDamageVariancePolicy_RejectsOutOfRangeRoll(double randomUnit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AttackDamageVariancePolicy.GetMultiplier(randomUnit));
        }

        [TestCase(1, 100, 50)]
        [TestCase(2, 105, 53)]
        [TestCase(3, 110, 55)]
        [TestCase(4, 115, 58)]
        [TestCase(5, 120, 60)]
        [TestCase(6, 125, 63)]
        [TestCase(7, 130, 65)]
        [TestCase(8, 135, 68)]
        [TestCase(9, 140, 70)]
        [TestCase(10, 150, 75)]
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
            Assert.That(result.Breakdown.EffectiveAttack, Is.EqualTo(50));
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
        public void ContentFailure_DoesNotConsumeEnemyCooldown()
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
            Assert.That(result.Snapshot.EnemyRemainingCooldown, Is.EqualTo(1));
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
        public void SurvivingEnemy_ConsumesCooldownOnlyAfterPlayerResult()
        {
            LocalCombatEngine engine = CreateCombatEngine(maximumCooldown: 3);

            CombatSnapshot committed = engine.CommitAttempt();
            CombatResolution result = engine.ResolveCorrect(1, 1d);

            Assert.That(committed.EnemyRemainingCooldown, Is.EqualTo(3));
            Assert.That(result.EnemyDefeated, Is.False);
            Assert.That(result.EnemyAttacked, Is.False);
            Assert.That(result.Snapshot.EnemyRemainingCooldown, Is.EqualTo(2));
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
                Is.EqualTo(3)
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
            CombatPresentationSnapshot source =
                CombatPresentationSnapshot.From(readyCombat.Snapshot);
            var destination = new CombatPresentationSnapshot(
                source.Stage,
                source.BiomeId,
                source.EncounterId,
                source.EncounterKind,
                source.EnemyCurrentHp - 5,
                source.EnemyMaximumHp,
                source.EnemyRemainingCooldown,
                source.EnemyMaximumCooldown,
                source.PlayerCurrentHearts,
                source.PlayerMaximumHearts,
                CombatPhase.PresentingResult);
            var dummyReceipt = new AttemptPresentationReceipt(
                "p-1", "a-1", AttemptOutcomeKind.Correct, 10, 5, false,
                source, destination,
                source.EnemyCurrentHp - 5,
                false, false, false, false, false, default);

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
        public void PersistenceRequest_AfterCommitPreservesEnemyCooldownUntilEnemyTurn()
        {
            GatewayFixture fixture = CreateGateway(maximumCooldown: 3);
            AttemptCommit commit = fixture.Gateway.CommitAttempt(
                new CombatCommandId("commit-save"));

            GameplaySaveRequest request = fixture.Transaction.CreateSaveRequest(
                GameplaySavePoint.AttemptCommitted,
                commit.Question);

            Assert.That(request.Snapshot.Combat.EnemyRemainingCooldown, Is.EqualTo(3));
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
        public void RunEncounter_InvincibilityPreventsHeartLoss()
        {
            var map = DevelopmentStageMapFactory.Create();
            var engine = new LocalRunEncounterEngine(
                new StageId(1),
                "invincible-test",
                new StageEncounterResolver(map),
                new MinimumRandomSource(),
                3,
                new PlayerCombatStats(5, 0d, 50d),
                invincible: true);

            int attempts = engine.Snapshot.EnemyMaximumCooldown;
            CombatResolution resolution = null;
            for (int index = 0; index < attempts; index++)
            {
                engine.CommitAttempt();
                resolution = engine.ResolveIncorrect(timedOut: false);
                if (index < attempts - 1) engine.CompletePresentation();
            }

            Assert.That(resolution, Is.Not.Null);
            Assert.That(resolution.EnemyAttacked, Is.True);
            Assert.That(resolution.Snapshot.PlayerCurrentHearts, Is.EqualTo(3));
            Assert.That(resolution.PlayerDefeated, Is.False);
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

            // Stage 200: World Level 40 -> Phase 3 Growth = 30.02x
            var finalBoss = new MonsterData("test-final", "Test Final Boss", StageEncounterKind.FinalBoss, "biome-7", 2, 5000);
            int stage200Hp = StageHpPolicy.Calculate(map, finalBoss, runId, new StageId(200));
            // 5000 * 30.02 = 150100 +/- 5% variation -> between 142000 and 158000
            Assert.That(stage200Hp, Is.InRange(142000, 158000));
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

        [TestCase(0, 1)]
        [TestCase(10000, 2)]
        public void EventSchedule_GuaranteesOneAndCapsEachBlockAtTwo(
            int chanceBasisPoints,
            int expectedPerBlock)
        {
            StageMapData source = DevelopmentStageMapFactory.Create();
            var map = new StageMapData(
                source.CatalogVersion,
                source.NormalHpBaseline,
                source.GrowthBasisPoints,
                source.VariationBasisPoints,
                source.Biomes,
                source.FixedEventStages.ToDictionary(
                    value => value,
                    value =>
                    {
                        source.TryGetFixedEvent(new StageId(value), out EventData fixedEvent);
                        return fixedEvent;
                    }),
                source.ChanceEvents,
                chanceBasisPoints);

            EventScheduleSnapshot schedule = EventScheduleGenerator.Create(
                "event-schedule-test", map, 10000);

            Assert.That(schedule.GeneratedStages.All(value =>
                !StageClassificationPolicy.IsProtected(new StageId(value))), Is.True);
            for (int start = StageId.First;
                 start <= StageId.Final;
                 start += EventScheduleGenerator.StagesPerBlock)
            {
                int end = Math.Min(StageId.Final,
                    start + EventScheduleGenerator.StagesPerBlock - 1);
                int fixedCount = map.FixedEventStages.Count(value =>
                    value >= start && value <= end &&
                    map.TryGetFixedEvent(new StageId(value), out EventData eventData) &&
                    eventData.Type == EventStageType.ChallengeMonster);
                int generatedCount = schedule.GeneratedStages.Count(value =>
                    value >= start && value <= end);
                Assert.That(fixedCount + generatedCount,
                    Is.EqualTo(expectedPerBlock), $"Block {start}-{end}");
            }
        }

        [Test]
        public void EventSchedule_SelectsDeterministicallyFromMultipleChallengeEvents()
        {
            StageMapData source = DevelopmentStageMapFactory.Create();
            EventData[] eventPool =
            {
                new EventData("event-c1", "Challenge One", EventStageType.ChallengeMonster),
                new EventData("event-c2", "Challenge Two", EventStageType.ChallengeMonster)
            };
            var map = new StageMapData(
                source.CatalogVersion,
                source.NormalHpBaseline,
                source.GrowthBasisPoints,
                source.VariationBasisPoints,
                source.Biomes,
                new Dictionary<int, EventData>(),
                eventPool,
                0);

            EventScheduleSnapshot schedule = EventScheduleGenerator.Create(
                "multi-event-schedule", map);
            var firstResolver = new StageEncounterResolver(map, schedule);
            var secondResolver = new StageEncounterResolver(map, schedule);

            foreach (int scheduledStage in schedule.GeneratedStages)
            {
                EncounterSelection first = firstResolver.Resolve(
                    "multi-event-schedule", new StageId(scheduledStage));
                EncounterSelection second = secondResolver.Resolve(
                    "multi-event-schedule", new StageId(scheduledStage));

                Assert.That(first.IsEvent, Is.True);
                Assert.That(eventPool.Select(value => value.Id), Does.Contain(first.EncounterId));
                Assert.That(second.EncounterId, Is.EqualTo(first.EncounterId));
            }
        }

        [Test]
        public void ChallengeQuestionId_RequiresCanonicalRankPrefix()
        {
            Assert.That(ChallengeQuestionId.TryParse("cs1", out ChallengeQuestionId silver), Is.True);
            Assert.That(silver.Rank, Is.EqualTo(AcademicRank.Silver));
            Assert.That(ChallengeQuestionId.TryParse("cg12", out ChallengeQuestionId gold), Is.True);
            Assert.That(gold.Rank, Is.EqualTo(AcademicRank.Gold));
            Assert.That(ChallengeQuestionId.TryParse("cd3", out ChallengeQuestionId diamond), Is.True);
            Assert.That(diamond.Rank, Is.EqualTo(AcademicRank.Diamond));
            Assert.That(ChallengeQuestionId.TryParse("CS1", out _), Is.False);
            Assert.That(ChallengeQuestionId.TryParse("cs01", out _), Is.False);
            Assert.That(ChallengeQuestionId.TryParse("s1", out _), Is.False);
            Assert.That(ChallengeQuestionId.TryParse(" cs1 ", out _), Is.False);
        }

        [Test]
        public void ChallengeSequence_AdvancesEachRankIndependently()
        {
            EventQuestionCatalog catalog = CreateChallengeCatalog();
            var sequence = new ChallengeQuestionSequence(catalog);

            ChallengeQuestionDefinition firstSilver = sequence.Reserve("challenge", AcademicRank.Silver);
            sequence.Resolve(firstSilver.Id);
            ChallengeQuestionDefinition firstGold = sequence.Reserve("challenge", AcademicRank.Gold);
            sequence.Resolve(firstGold.Id);
            ChallengeQuestionDefinition secondSilver = sequence.Reserve("challenge", AcademicRank.Silver);

            Assert.That(firstSilver.Id.Value, Is.EqualTo("cs1"));
            Assert.That(firstGold.Id.Value, Is.EqualTo("cg1"));
            Assert.That(secondSilver.Id.Value, Is.EqualTo("cs2"));
            Assert.That(sequence.Export().GoldCursor, Is.EqualTo(1));
        }

        [Test]
        public void ChallengeSequence_SharesRankCursorAcrossEventCatalogKeys()
        {
            IReadOnlyList<ChallengeQuestionDefinition> questions =
                CreateChallengeQuestions();
            var catalog = new EventQuestionCatalog(
                new Dictionary<string, IReadOnlyList<ChallengeQuestionDefinition>>
                {
                    { "challenge-a", questions },
                    { "challenge-b", questions }
                });
            var sequence = new ChallengeQuestionSequence(catalog);

            ChallengeQuestionDefinition first = sequence.Reserve(
                "challenge-a", AcademicRank.Silver);
            sequence.Resolve(first.Id);
            ChallengeQuestionDefinition second = sequence.Reserve(
                "challenge-b", AcademicRank.Silver);

            Assert.That(first.Id.Value, Is.EqualTo("cs1"));
            Assert.That(second.Id.Value, Is.EqualTo("cs2"));
        }

        [TestCase(QuestionOutcome.Correct, 10, 20)]
        [TestCase(QuestionOutcome.Correct, 5, 15)]
        [TestCase(QuestionOutcome.Correct, 0, 10)]
        [TestCase(QuestionOutcome.Incorrect, 10, 10)]
        [TestCase(QuestionOutcome.Timeout, 10, 10)]
        public void ChallengeReward_UsesEfficiencyAndHalfRewardFloor(
            QuestionOutcome outcome,
            int score,
            int expected)
        {
            Assert.That(ChallengeRewardPolicy.Calculate(outcome, score), Is.EqualTo(expected));
        }

        [TestCase(QuestionOutcome.Correct, 10, 2, 30)]
        [TestCase(QuestionOutcome.Correct, 0, 2, 15)]
        [TestCase(QuestionOutcome.Correct, 10, 6, 150)]
        [TestCase(QuestionOutcome.Correct, 5, 6, 112)]
        [TestCase(QuestionOutcome.Incorrect, 10, 6, 75)]
        [TestCase(QuestionOutcome.Correct, 10, 7, 200)]
        [TestCase(QuestionOutcome.Incorrect, 10, 7, 100)]
        public void ChallengeReward_ScalesByBiome(
            QuestionOutcome outcome,
            int score,
            int biomeIndex,
            int expected)
        {
            Assert.That(ChallengeRewardPolicy.Calculate(outcome, score, biomeIndex), Is.EqualTo(expected));
        }

        [Test]
        public void ChallengeFailure_FleesWithoutHeartLossAndAdvancesStage()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var engine = new LocalRunEncounterEngine(
                new StageId(7),
                "challenge-flee-test",
                new StageEncounterResolver(map),
                new MinimumRandomSource(),
                3,
                new PlayerCombatStats(5, 0d, 50d));

            engine.CommitAttempt();
            CombatResolution result = engine.ResolveIncorrect(timedOut: false);

            Assert.That(result.EnemyFled, Is.True);
            Assert.That(result.EnemyAttacked, Is.False);
            Assert.That(result.PlayerDefeated, Is.False);
            Assert.That(result.StageAdvanced, Is.True);
            Assert.That(result.Snapshot.Stage.Value, Is.EqualTo(8));
            Assert.That(result.Snapshot.PlayerCurrentHearts, Is.EqualTo(3));
        }

        [Test]
        public void ChallengeTransaction_GrantsCoinsWithoutChangingAudit()
        {
            QuestionCatalogLoadResult rankCatalog = new QuestionDocumentMapper()
                .MapCatalog(InMemoryQuestionCatalogRepository.CreateDefaultDocuments());
            var academic = new AcademicProgressionEngine(rankCatalog.Catalog);
            AcademicProgressionState academicState = academic.CreateInitialState(
                AcademicRank.Silver, new RankCurrencyBalances(0, 0, 0));
            var clock = new ManualClock { NowSeconds = 10d };
            StageMapData map = DevelopmentStageMapFactory.Create();
            var combat = new LocalRunEncounterEngine(
                new StageId(7), "challenge-transaction-test",
                new StageEncounterResolver(map), new MinimumRandomSource(), 3,
                new PlayerCombatStats(5, 0d, 50d));
            var transaction = new LocalAttemptTransactionEngine(
                combat, academic, academicState, clock, 1d, 10d,
                CreateChallengeCatalog(), "challenge-transaction-test",
                powerCoins: 5);

            AttemptCommit commit = transaction.CommitAttempt();
            transaction.OpenAnswerWindow(commit.Question.Id);
            AttemptResolution result = transaction.SubmitAnswer("999");
            GameplaySaveRequest save = transaction.CreateSaveRequest(
                GameplaySavePoint.AttemptResolved, resolution: result);

            Assert.That(commit.Question.ContentId, Is.EqualTo("cs1"));
            Assert.That(result.IsAcademic, Is.False);
            Assert.That(result.Event.PowerCoinsGranted, Is.EqualTo(10));
            Assert.That(result.Snapshot.PowerCoins, Is.EqualTo(15));
            Assert.That(save.Academic.AuditResolvedCount, Is.Zero);
            Assert.That(result.Combat.EnemyFled, Is.True);
        }

        [Test]
        public void StageHpPolicy_SmoothGrowthAcrossWorldLevels()
        {
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(1, 1500), Is.EqualTo(10000L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(6, 1500), Is.EqualTo(17500L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(12, 1500), Is.EqualTo(26500L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(18, 1500), Is.EqualTo(47500L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(24, 1500), Is.EqualTo(68500L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(30, 1500), Is.EqualTo(116500L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(36, 1500), Is.EqualTo(206500L));
            Assert.That(StageHpPolicy.CalculateGrowthBasisPoints(40, 1500), Is.EqualTo(266500L));
        }

        [Test]
        public void LocalRunEncounterEngine_AuregriffPassive_IncreasesThirdAttackDamageAndResetsOnStageProgression()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            // EffectiveAttack = 10, Auregriff passive enabled
            var stats = new PlayerCombatStats(
                effectiveAttack: 10,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:nth-attack",
                    PetPassiveEffectType.ModifyEveryNthPlayerAttack,
                    0.25d,
                    triggerCount: 3,
                    resetScope: PetPassiveResetScope.Stage)));

            EncounterSelection selection = resolver.Resolve(
                "auregriff-test-run",
                new StageId(1));
            var snapshot = new CombatSnapshot(
                new StageId(1), selection.EncounterId, selection.DisplayName,
                enemyCurrentHp: 100,
                enemyMaximumHp: 100,
                selection.MaximumCooldown,
                selection.MaximumCooldown,
                playerCurrentHearts: 3,
                playerMaximumHearts: 3,
                phase: CombatPhase.EnemyReady,
                isSimulation: false,
                biomeId: selection.BiomeId,
                biomeTitle: selection.BiomeTitle,
                encounterKind: selection.Kind,
                questionDocumentId: selection.QuestionDocumentId,
                eventAttemptOrdinal: 0);
            var engine = new LocalRunEncounterEngine(
                snapshot,
                "auregriff-test-run",
                resolver,
                new MidpointRandomSource(),
                stats);

            // Hit 1: buffMultiplier 1.0 -> 10 * 1.0 * 1.5 (score 10) = 15 damage
            engine.CommitAttempt();
            CombatResolution res1 = engine.ResolveCorrect(10, 1d);
            Assert.That(res1.FinalDamage, Is.EqualTo(15));
            Assert.That(engine.StageAttackCount, Is.EqualTo(1));

            // Hit 2: buffMultiplier 1.0 -> 15 damage
            engine.CompletePresentation();
            engine.CommitAttempt();
            CombatResolution res2 = engine.ResolveCorrect(10, 1d);
            Assert.That(res2.FinalDamage, Is.EqualTo(15));
            Assert.That(engine.StageAttackCount, Is.EqualTo(2));

            // Hit 3: 3rd hit in same stage! buffMultiplier 1.25 -> 10 * 1.25 * 1.5 = 18.75 -> 19 damage!
            engine.CompletePresentation();
            engine.CommitAttempt();
            CombatResolution res3 = engine.ResolveCorrect(10, 1d);
            Assert.That(res3.FinalDamage, Is.EqualTo(19));
            Assert.That(engine.StageAttackCount, Is.EqualTo(3));
        }

        [Test]
        public void LocalRunEncounterEngine_LumirinPassive_RegeneratesHeartOnBigBossDefeat()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            // Stage 30 is BigBoss
            var stats = new PlayerCombatStats(
                effectiveAttack: 10000,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                bonusMaxHearts: 1,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:heart-restore",
                    PetPassiveEffectType.RestoreHeartsOnEncounterDefeat,
                    1d,
                    encounterFilter: PetEncounterFilter.BigBoss)));

            // Snapshot with 2 / 4 hearts at stage 30
            var selection = resolver.Resolve("lumirin-test-run", new StageId(30));
            Assert.That(selection.Kind, Is.EqualTo(StageEncounterKind.BigBoss));

            var snapshot = new CombatSnapshot(
                new StageId(30), selection.EncounterId, selection.DisplayName,
                selection.MaximumHp, selection.MaximumHp,
                selection.MaximumCooldown, selection.MaximumCooldown,
                playerCurrentHearts: 2,
                playerMaximumHearts: 4,
                phase: CombatPhase.EnemyReady,
                isSimulation: false,
                biomeId: selection.BiomeId,
                biomeTitle: selection.BiomeTitle,
                encounterKind: selection.Kind,
                questionDocumentId: selection.QuestionDocumentId,
                eventAttemptOrdinal: 0);

            var engine = new LocalRunEncounterEngine(
                snapshot, "lumirin-test-run",
                resolver, new MinimumRandomSource(), stats);

            HeartChangeArgs? receivedHeartChange = null;
            engine.HeartChanged += args => receivedHeartChange = args;

            engine.CommitAttempt();
            CombatResolution res = engine.ResolveCorrect(10, 1d);

            Assert.That(res.EnemyDefeated, Is.True);
            Assert.That(receivedHeartChange.HasValue, Is.True);
            Assert.That(receivedHeartChange.Value.Reason, Is.EqualTo(HeartChangeReason.PetPassiveRestore));
            Assert.That(receivedHeartChange.Value.PreviousHearts, Is.EqualTo(2));
            Assert.That(receivedHeartChange.Value.CurrentHearts, Is.EqualTo(3));
            Assert.That(receivedHeartChange.Value.ChangeAmount, Is.EqualTo(1));
            Assert.That(res.Snapshot.PlayerCurrentHearts, Is.EqualTo(3));
            Assert.That(res.Snapshot.BigBossesDefeated, Is.EqualTo(1));
            Assert.That(engine.BigBossesDefeated, Is.EqualTo(1));
        }

        [Test]
        public void LocalRunEncounterEngine_DamageTaken_FiresHeartChangedEvent()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            var stats = new PlayerCombatStats(10, 0d, 50d);

            EncounterSelection selection = resolver.Resolve(
                "damage-event-test",
                new StageId(1));
            Assert.That(selection.IsEvent, Is.False);
            var snapshot = new CombatSnapshot(
                new StageId(1), selection.EncounterId, selection.DisplayName,
                selection.MaximumHp, selection.MaximumHp,
                enemyRemainingCooldown: 1,
                enemyMaximumCooldown: selection.MaximumCooldown,
                playerCurrentHearts: 3,
                playerMaximumHearts: 3,
                phase: CombatPhase.EnemyReady,
                isSimulation: false,
                biomeId: selection.BiomeId,
                biomeTitle: selection.BiomeTitle,
                encounterKind: selection.Kind,
                questionDocumentId: selection.QuestionDocumentId,
                eventAttemptOrdinal: 0);
            var engine = new LocalRunEncounterEngine(
                snapshot,
                "damage-event-test",
                resolver,
                new MinimumRandomSource(),
                stats);

            HeartChangeArgs? received = null;
            engine.HeartChanged += args => received = args;

            engine.CommitAttempt();
            engine.ResolveIncorrect(false);

            Assert.That(received.HasValue, Is.True);
            Assert.That(received.Value.Reason, Is.EqualTo(HeartChangeReason.DamageTaken));
            Assert.That(received.Value.ChangeAmount, Is.EqualTo(-1));
            Assert.That(received.Value.PreviousHearts, Is.EqualTo(3));
            Assert.That(received.Value.CurrentHearts, Is.EqualTo(2));
        }

        [Test]
        public void LocalRunEncounterEngine_SapphireFollowUp_AddsPetAttackToTotalDamage()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            // Player ATK: 10, Pet ATK: 30, SapphireFollowUp active
            var stats = new PlayerCombatStats(
                effectiveAttack: 10,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                effectivePetAttack: 30,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:follow-up",
                    PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack,
                    1d)));

            var engine = new LocalRunEncounterEngine(
                new StageId(1), "sapphire-test",
                resolver, new MidpointRandomSource(), 3, stats);

            engine.CommitAttempt();
            // Player damage = 10 * 1.0 * 1.5 (score 10) = 15. Pet damage = 30 * 1.0 = 30. Total = 45.
            CombatResolution res = engine.ResolveCorrect(10, 1d);
            Assert.That(res.FinalDamage, Is.EqualTo(45));
            Assert.That(res.PlayerDamage, Is.EqualTo(15));
            Assert.That(res.PetFollowUp, Is.Not.Null);
            Assert.That(res.PetFollowUp.Damage, Is.EqualTo(30));
            Assert.That(res.PetFollowUp.Carried, Is.False);
        }

        [Test]
        public void LocalRunEncounterEngine_CarriedFollowUp_KillsNextEnemyAndAdvancesAgain()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            const string runId = "carried-follow-up-test";
            var resolver = new StageEncounterResolver(map);
            var stats = new PlayerCombatStats(
                effectiveAttack: 10000,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                effectivePetAttack: 10000,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:follow-up",
                    PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack,
                    1d)));
            var engine = new LocalRunEncounterEngine(
                new StageId(1),
                runId,
                resolver,
                new MidpointRandomSource(),
                3,
                stats);

            engine.CommitAttempt();
            CombatResolution result = engine.ResolveCorrect(10, 1d);

            Assert.That(result.PlayerEnemyHpAfter, Is.Zero);
            Assert.That(result.PetFollowUp, Is.Not.Null);
            Assert.That(result.PetFollowUp.Carried, Is.True);
            Assert.That(result.PetFollowUp.Target.Stage.Value, Is.EqualTo(2));
            Assert.That(result.PetFollowUp.EnemyDefeated, Is.True);
            Assert.That(result.Snapshot.Stage.Value, Is.EqualTo(3));
            Assert.That(result.Snapshot.PendingPetFollowUpDamage, Is.Zero);
        }

        [Test]
        public void LocalRunEncounterEngine_RefreshPlayerStats_AppliesNewPetImmediately()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var engine = new LocalRunEncounterEngine(
                new StageId(1),
                "pet-hot-reload",
                new StageEncounterResolver(map),
                new MidpointRandomSource(),
                3,
                new PlayerCombatStats(10, 0d, 50d));
            var refreshed = new PlayerCombatStats(
                effectiveAttack: 20,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                effectivePetAttack: 30,
                bonusMaxHearts: 1,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:follow-up",
                    PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack,
                    1d)));

            engine.RefreshPlayerStats(refreshed);
            engine.CommitAttempt();
            CombatResolution result = engine.ResolveCorrect(1, 1d);

            Assert.That(result.PlayerDamage, Is.EqualTo(20));
            Assert.That(result.PetFollowUp, Is.Not.Null);
            Assert.That(result.PetFollowUp.Damage, Is.EqualTo(30));
            Assert.That(result.Snapshot.PlayerMaximumHearts, Is.EqualTo(4));
            Assert.That(result.Snapshot.PlayerCurrentHearts, Is.EqualTo(4));
        }

        [Test]
        public void EventScheduleRefresh_PreservesReachedStagesAndUsesNewMultiplier()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var current = new EventScheduleSnapshot(
                map.CatalogVersion,
                "challenge-monster",
                new[] { 3, 18, 25 },
                1000,
                10000);
            var refreshed = new EventScheduleSnapshot(
                map.CatalogVersion,
                "challenge-monster",
                new[] { 4, 19, 26, 35 },
                1000,
                12500);

            EventScheduleSnapshot merged =
                EventScheduleRefreshPolicy.PreserveReachedStages(
                    map,
                    current,
                    refreshed,
                    new StageId(10));

            Assert.That(merged.GeneratedStages, Does.Contain(3));
            Assert.That(merged.GeneratedStages, Has.No.Member(18));
            Assert.That(merged.GeneratedStages, Does.Contain(26));
            Assert.That(merged.PetMultiplierBasisPoints, Is.EqualTo(12500));
        }

        [Test]
        public void LocalRunEncounterEngine_RestoresPetStateFromSnapshot_PreservesTrackingMetrics()
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            var stats = new PlayerCombatStats(10, 0d, 50d);
            var selection = resolver.Resolve("restore-test-run", new StageId(5));

            var snapshot = new CombatSnapshot(
                new StageId(5), selection.EncounterId, selection.DisplayName,
                selection.MaximumHp, selection.MaximumHp,
                selection.MaximumCooldown, selection.MaximumCooldown,
                playerCurrentHearts: 3,
                playerMaximumHearts: 3,
                phase: CombatPhase.EnemyReady,
                isSimulation: false,
                biomeId: selection.BiomeId,
                biomeTitle: selection.BiomeTitle,
                encounterKind: selection.Kind,
                questionDocumentId: selection.QuestionDocumentId,
                eventAttemptOrdinal: 0,
                eventSchedule: null,
                stageAttackCount: 2,
                bigBossesDefeated: 1,
                pendingPetFollowUpDamage: 15);

            var engine = new LocalRunEncounterEngine(
                snapshot, "restore-test-run",
                resolver, new MinimumRandomSource(), stats);

            Assert.That(engine.StageAttackCount, Is.EqualTo(2));
            Assert.That(engine.BigBossesDefeated, Is.EqualTo(1));
            Assert.That(engine.PendingPetFollowUpDamage, Is.EqualTo(15));
            Assert.That(engine.Snapshot.StageAttackCount, Is.EqualTo(2));
            Assert.That(engine.Snapshot.BigBossesDefeated, Is.EqualTo(1));
            Assert.That(engine.Snapshot.PendingPetFollowUpDamage, Is.EqualTo(15));
        }

        private static ActivePetPassiveSet Passives(
            params PetPassiveDefinition[] definitions)
        {
            var passives = new ActivePetPassive[definitions.Length];
            for (int index = 0; index < definitions.Length; index++)
                passives[index] = new ActivePetPassive(definitions[index], 1);
            return new ActivePetPassiveSet(passives);
        }

        private static EventQuestionCatalog CreateChallengeCatalog()
        {
            IReadOnlyList<ChallengeQuestionDefinition> questions =
                CreateChallengeQuestions();
            return new EventQuestionCatalog(
                new Dictionary<string, IReadOnlyList<ChallengeQuestionDefinition>>
                {
                    { "challenge", questions }
                });
        }

        private static IReadOnlyList<ChallengeQuestionDefinition>
            CreateChallengeQuestions() => new List<ChallengeQuestionDefinition>
            {
                Challenge("cs1", 11), Challenge("cs2", 12),
                Challenge("cg1", 21), Challenge("cg2", 22),
                Challenge("cd1", 31), Challenge("cd2", 32)
            };

        private static ChallengeQuestionDefinition Challenge(string id, int answer) =>
            new ChallengeQuestionDefinition(
                new ChallengeQuestionId(id),
                new Uri("https://www.youtube.com/watch?v=dQw4w9WgXcQ"),
                answer,
                "dQw4w9WgXcQ");

        [TestCase(30, 1.0d, 1.0d, false, 0d, 30)]
        [TestCase(30, 1.0d, 1.25d, false, 0d, 38)]
        [TestCase(30, 1.0d, 1.5d, false, 0d, 45)]
        [TestCase(30, 1.0d, 1.5d, true, 50d, 68)]
        public void PetCombatPolicy_CalculatesDamageWithRankMultiplier(
            int effectivePetAttack,
            double passiveMagnitude,
            double rankMultiplier,
            bool isCritical,
            double criticalDamagePercent,
            int expectedDamage)
        {
            int damage = PetCombatPolicy.CalculateDamage(
                effectivePetAttack,
                passiveMagnitude,
                rankMultiplier,
                isCritical,
                criticalDamagePercent);

            Assert.That(damage, Is.EqualTo(expectedDamage));
        }

        [TestCase(0.90d, 27)]
        [TestCase(1.00d, 30)]
        [TestCase(1.10d, 33)]
        public void PetCombatPolicy_AppliesAttackVariance(
            double varianceMultiplier,
            int expectedDamage)
        {
            int damage = PetCombatPolicy.CalculateDamage(
                30,
                1d,
                1d,
                false,
                0d,
                varianceMultiplier);

            Assert.That(damage, Is.EqualTo(expectedDamage));
        }

        [TestCase(1.0d, 30)]
        [TestCase(1.25d, 38)]
        [TestCase(1.5d, 45)]
        public void LocalRunEncounterEngine_PetFollowUp_ScalesWithRankMultiplier(
            double rankMultiplier,
            int expectedPetDamage)
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            var stats = new PlayerCombatStats(
                effectiveAttack: 10,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                effectivePetAttack: 30,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:follow-up",
                    PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack,
                    1d)));

            var engine = new LocalRunEncounterEngine(
                new StageId(1), "rank-followup-test",
                resolver, new MidpointRandomSource(), 3, stats);

            engine.CommitAttempt();
            CombatResolution res = engine.ResolveCorrect(1, rankMultiplier);

            Assert.That(res.PetFollowUp, Is.Not.Null);
            Assert.That(res.PetFollowUp.Damage, Is.EqualTo(expectedPetDamage));
        }

        [TestCase(1.0d, 30)]
        [TestCase(1.25d, 38)]
        [TestCase(1.5d, 45)]
        public void LocalRunEncounterEngine_PetCounterAttack_TriggersOnHeartLossWithRankMultiplier(
            double rankMultiplier,
            int expectedPetDamage)
        {
            StageMapData map = DevelopmentStageMapFactory.Create();
            var resolver = new StageEncounterResolver(map);
            var stats = new PlayerCombatStats(
                effectiveAttack: 10,
                criticalRate: 0d,
                criticalDamagePercent: 50d,
                effectivePetAttack: 30,
                petPassives: Passives(new PetPassiveDefinition(
                    "test:counter-attack",
                    PetPassiveEffectType.CounterAttackAfterHeartLoss,
                    1d)));

            // Spawn enemy with cooldown 1 so the next attempt will trigger enemy attack
            var selection = resolver.Resolve("counter-test", new StageId(1));
            var snapshot = new CombatSnapshot(
                new StageId(1), selection.EncounterId, selection.DisplayName,
                100, 100,
                enemyRemainingCooldown: 1,
                enemyMaximumCooldown: 1,
                playerCurrentHearts: 3,
                playerMaximumHearts: 3,
                phase: CombatPhase.EnemyReady,
                isSimulation: false,
                biomeId: selection.BiomeId,
                biomeTitle: selection.BiomeTitle,
                encounterKind: selection.Kind,
                questionDocumentId: selection.QuestionDocumentId,
                eventAttemptOrdinal: 0);

            var engine = new LocalRunEncounterEngine(
                snapshot, "counter-test",
                resolver, new MidpointRandomSource(), stats);

            engine.CommitAttempt();
            // Resolve incorrect with timeout to let enemy attack
            CombatResolution res = engine.ResolveIncorrect(true, rankMultiplier);

            Assert.That(res.EnemyAttacked, Is.True);
            Assert.That(res.Snapshot.PlayerCurrentHearts, Is.EqualTo(2));
            Assert.That(res.PetFollowUp, Is.Not.Null);
            Assert.That(res.PetFollowUp.Damage, Is.EqualTo(expectedPetDamage));
            Assert.That(res.FinalDamage, Is.EqualTo(expectedPetDamage));
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

        private sealed class MidpointRandomSource : IRandomSource
        {
            public int NextInclusive(int minimum, int maximum) =>
                minimum + (maximum - minimum) / 2;

            public double NextUnit() => 0.5d;
        }
    }
}
