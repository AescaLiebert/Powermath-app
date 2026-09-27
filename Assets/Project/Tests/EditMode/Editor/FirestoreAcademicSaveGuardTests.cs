using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Combat;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;

namespace PowerMath.Tests.EditMode
{
    public sealed class FirestoreAcademicSaveGuardTests
    {
        [Test]
        public void MatchingRevisionPreservesPendingReceiptWalletAndUnknownFields()
        {
            JsonValue document = Document(
                Integer(PlayerSchemaMigrator.CurrentSchemaVersion), Integer(10),
                ",\"wallet\":{\"mapValue\":{\"fields\":{\"silver\":{\"integerValue\":\"90\"}}}}" +
                ",\"activeRun\":{\"mapValue\":{\"fields\":{\"pendingPresentation\":{\"mapValue\":{\"fields\":{\"attemptId\":{\"stringValue\":\"committed-attempt\"}}}}}}}" +
                ",\"futureFeature\":{\"stringValue\":\"keep\"}");
            JsonValue game = Game(document);
            Assert.That(game.TryGet("wallet", out JsonValue wallet), Is.True);
            Assert.That(game.TryGet("activeRun", out JsonValue activeRun), Is.True);
            Assert.That(game.TryGet("futureFeature", out JsonValue unknown), Is.True);

            Assert.That(Validate(document, 10, out string error), Is.True, error);

            Assert.That(Game(document), Is.SameAs(game));
            Assert.That(game.Object["wallet"], Is.SameAs(wallet));
            Assert.That(game.Object["activeRun"], Is.SameAs(activeRun));
            Assert.That(game.Object["futureFeature"], Is.SameAs(unknown));
            Assert.That(unknown.Object["stringValue"].Text, Is.EqualTo("keep"));
            Assert.That(game.Object["revision"].Object["integerValue"].Text, Is.EqualTo("10"));
        }

        [TestCase(11)] // Another tab, or this attempt's successful PATCH with a lost response.
        [TestCase(9)]
        public void ChangedRevisionRequiresReloadInsteadOfReplayingSnapshot(long persistedRevision)
        {
            JsonValue document = Document(Integer(PlayerSchemaMigrator.CurrentSchemaVersion), Integer(persistedRevision));
            Assert.That(Validate(document, 10, out string error), Is.False);
            StringAssert.Contains("reload", error);
            Assert.That(Game(document).Object["revision"].Object["integerValue"].Text,
                Is.EqualTo(persistedRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        [Test]
        public void FutureSchemaRequiresUpdateEvenWhenRevisionMatches()
        {
            Assert.That(Validate(Document(Integer(PlayerSchemaMigrator.CurrentSchemaVersion + 1), Integer(10)),
                10, out string error), Is.False);
            StringAssert.Contains("newer game version", error);
        }

        [TestCase(null)]
        [TestCase("{\"integerValue\":\"2\"}")]
        [TestCase("{\"stringValue\":\"3\"}")]
        [TestCase("{\"doubleValue\":3.5}")]
        public void MissingLegacyOrMalformedSchemaCannotBeWritten(string schema)
        {
            Assert.That(Validate(Document(schema, Integer(10)), 10, out _), Is.False);
        }

        [TestCase(null)]
        [TestCase("{\"integerValue\":\"-1\"}")]
        [TestCase("{\"integerValue\":\"9223372036854775808\"}")]
        [TestCase("{\"stringValue\":\"10\"}")]
        [TestCase("{\"doubleValue\":10.5}")]
        public void MissingOrMalformedRevisionCannotBeCoercedToExpectedValue(string revision)
        {
            Assert.That(Validate(Document(Integer(PlayerSchemaMigrator.CurrentSchemaVersion), revision),
                10, out _), Is.False);
        }

        [TestCase("{\"fields\":{}}")]
        [TestCase("{\"fields\":{\"student\":{\"mapValue\":{\"fields\":{}}}}}")]
        [TestCase("{\"fields\":{\"student\":{\"mapValue\":{\"fields\":{\"gamedata\":{\"nullValue\":null}}}}}}")]
        public void MissingPlayerOrGameDataCannotTriggerGameplayInitialization(string json)
        {
            Assert.That(FirestoreJsonNavigator.TryParse(json, out JsonValue document, out _), Is.True);
            Assert.That(Validate(document, 0, out _), Is.False);
        }

        [Test]
        public void InterruptedChallengeRewardMustBeSavedBeforePresentationAcknowledgement()
        {
            var player = new PlayerSnapshot
            {
                wallet = new PlayerSnapshot.WalletData { powerCoins = 5 },
                activeRun = new PlayerSnapshot.ActiveRunData
                {
                    runId = "run-1", committedAttemptId = "attempt-1",
                    currentStage = 7, encounterId = "event-1"
                }
            };
            var settings = ScriptableObject.CreateInstance<GameApiSettings>();
            try
            {
                var store = new FirestoreAcademicProgressionStore(
                    settings, "level-3", "test", player, null);
                var source = new CombatPresentationSnapshot(
                    new StageId(7), "biome-a", "event-1",
                    StageEncounterKind.ChallengeEvent, 1, 1, 0, 0, 3, 3,
                    CombatPhase.Committed);
                var destination = new CombatPresentationSnapshot(
                    new StageId(8), "biome-a", "enemy-2",
                    StageEncounterKind.NormalMonster, 10, 10, 2, 2, 3, 3,
                    CombatPhase.PresentingResult);
                var receipt = new AttemptPresentationReceipt(
                    "interrupted-attempt-attempt-1", "attempt-1",
                    AttemptOutcomeKind.Timeout, 0, 0, false, source, destination,
                    1, false, false, false, true, false, default,
                    enemyFled: true, powerCoinsGranted: 10,
                    resultingPowerCoins: 15);
                var combat = new CombatSnapshot(
                    new StageId(8), "enemy-2", "Enemy 2", 10, 10, 2, 2,
                    3, 3, CombatPhase.PresentingResult, false, "biome-a", "Biome A",
                    StageEncounterKind.NormalMonster, string.Empty, 0);
                var snapshot = new GameplaySnapshot(combat, default, 15);
                var emptyInventory = new RankQuestionInventorySnapshot(
                    0, null, null, null, null);
                var academic = new AcademicPersistenceSnapshot(
                    AcademicRank.Silver, 0, 0, new RankCurrencyBalances(0, 0, 0),
                    emptyInventory, emptyInventory, emptyInventory);
                var resolved = new GameplaySaveRequest(
                    GameplaySavePoint.InterruptedAttemptResolved, snapshot, academic,
                    transactionId: "attempt-1", recoveryPresentation: receipt);
                var acknowledged = new GameplaySaveRequest(
                    GameplaySavePoint.PresentationCompleted, snapshot, academic,
                    transactionId: "attempt-1", presentationId: receipt.PresentationId);

                Assert.That(ValidatePowerCoins(store, resolved), Is.True);
                Assert.That(ValidatePowerCoins(store, acknowledged), Is.False);
                player.wallet.powerCoins = 15;
                player.activeRun.lastChallengeRewardAttemptId = "attempt-1";
                player.activeRun.lastChallengeRewardResultingPowerCoins = 15;
                Assert.That(ValidatePowerCoins(store, acknowledged), Is.True);
                Assert.That(ValidatePowerCoins(store, resolved), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void InterruptedRankTimeoutUpdatesAnalyticsOnlyOnce()
        {
            var player = new PlayerSnapshot();
            var rank = AcademicRank.Silver;
            var result = new AcademicAttemptResult(
                new QuestionId(1), rank, QuestionOutcome.Timeout, 0, 0,
                new RankTransition(rank, rank),
                new AcademicProgressionProjection(rank,
                    new RankCurrencyBalances(0, 0, 0), true));
            var combat = new CombatSnapshot(
                new StageId(1), "enemy-1", "Enemy 1", 10, 10,
                2, 2, 3, 3, CombatPhase.PresentingResult, false);
            var inventory = new RankQuestionInventorySnapshot(
                0, null, null, null, null);
            var request = new GameplaySaveRequest(
                GameplaySavePoint.InterruptedAttemptResolved,
                new GameplaySnapshot(combat, default),
                new AcademicPersistenceSnapshot(rank, 1, 0,
                    new RankCurrencyBalances(0, 0, 0),
                    inventory, inventory, inventory),
                transactionId: "attempt-1", recoveryAcademicResult: result);

            PlayerAnalyticsUpdater.Apply(player, request);
            PlayerAnalyticsUpdater.Apply(player, request);

            Assert.That(player.analytics.totalQuestionsResolved, Is.EqualTo(1));
            Assert.That(player.analytics.totalTimeout, Is.EqualTo(1));
            Assert.That(player.analytics.silver.resolved, Is.EqualTo(1));
            Assert.That(player.analytics.byQuestion[0].timeout, Is.EqualTo(1));
        }

        private static bool ValidatePowerCoins(
            FirestoreAcademicProgressionStore store, GameplaySaveRequest request)
        {
            MethodInfo method = typeof(FirestoreAcademicProgressionStore).GetMethod(
                "TryValidatePowerCoinChange", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { request, null };
            return (bool)method.Invoke(store, arguments);
        }

        private static bool Validate(JsonValue document, long expectedRevision, out string error)
        {
            MethodInfo method = typeof(FirestoreAcademicProgressionStore).GetMethod(
                "TryValidateSaveDocument", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { document, "student", expectedRevision, null };
            bool result = (bool)method.Invoke(null, arguments);
            error = arguments[3] as string;
            return result;
        }

        private static string Integer(long value) => "{\"integerValue\":\"" +
            value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\"}";

        private static JsonValue Document(string schema, string revision, string extraFields = "")
        {
            string game = "\"marker\":{\"stringValue\":\"keep\"}";
            if (schema != null) game += ",\"schemaVersion\":" + schema;
            if (revision != null) game += ",\"revision\":" + revision;
            string json = "{\"fields\":{\"gamedata\":{\"mapValue\":{\"fields\":{" +
                game + extraFields + "}}}}}";
            Assert.That(FirestoreJsonNavigator.TryParse(json, out JsonValue document, out string error), Is.True, error);
            return document;
        }

        private static JsonValue Game(JsonValue document)
        {
            if (document.Object["fields"].Object.TryGetValue("gamedata", out JsonValue directGame))
                return directGame.Object["mapValue"].Object["fields"];

            return document.Object["fields"].Object["student"]
                .Object["mapValue"].Object["fields"].Object["gamedata"].Object["mapValue"].Object["fields"];
        }
    }
}
