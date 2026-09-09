using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.PlayerData;
using PowerMath.Session;

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
            string json = "{\"fields\":{\"student\":{\"mapValue\":{\"fields\":{\"gamedata\":{\"mapValue\":{\"fields\":{" +
                game + extraFields + "}}}}}}}}";
            Assert.That(FirestoreJsonNavigator.TryParse(json, out JsonValue document, out string error), Is.True, error);
            return document;
        }

        private static JsonValue Game(JsonValue document) => document.Object["fields"].Object["student"]
            .Object["mapValue"].Object["fields"].Object["gamedata"].Object["mapValue"].Object["fields"];
    }
}
