using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Combat;

namespace PowerMath.Tests.EditMode
{
    public sealed class FirestoreQuestionCatalogRepositoryTests
    {
        [Test]
        public void Parser_AcceptsDeployedTopLevelQuestionMaps()
        {
            const string json =
                "{\"fields\":{" +
                "\"q2\":{\"mapValue\":{\"fields\":{" +
                "\"id\":{\"stringValue\":\"s2\"}," +
                "\"answer\":{\"integerValue\":\"12\"}," +
                "\"video-url\":{\"stringValue\":\"https://youtu.be/M7lc1UVf-VE\"}}}}," +
                "\"q1\":{\"mapValue\":{\"fields\":{" +
                "\"id\":{\"stringValue\":\"s1\"}," +
                "\"answer\":{\"integerValue\":\"7\"}," +
                "\"video-url\":{\"stringValue\":\"https://youtu.be/QuvCVSoeTZ4\"}}}}}}";

            List<RankedQuestionDocument> documents = Parse(json);

            Assert.That(documents, Has.Count.EqualTo(2));
            Assert.That(documents[0].Document.id, Is.EqualTo(1));
            Assert.That(documents[1].Document.id, Is.EqualTo(2));
            Assert.That(documents[0].Document.video_link,
                Is.EqualTo("https://youtu.be/QuvCVSoeTZ4"));
        }

        [Test]
        public void Parser_PreservesItemsArrayContract()
        {
            const string json =
                "{\"fields\":{\"items\":{\"arrayValue\":{\"values\":[" +
                "{\"mapValue\":{\"fields\":{" +
                "\"id\":{\"integerValue\":\"5\"}," +
                "\"answer\":{\"integerValue\":\"42\"}," +
                "\"video_link\":{\"stringValue\":\"https://youtu.be/M7lc1UVf-VE\"}}}}]}}}}";

            List<RankedQuestionDocument> documents = Parse(json);

            Assert.That(documents, Has.Count.EqualTo(1));
            Assert.That(documents[0].Document.id, Is.EqualTo(5));
            Assert.That(documents[0].Document.answer, Is.EqualTo(42));
        }

        [Test]
        public void EventParser_AcceptsCanonicalRankedStringIdsWithQuestionSchema()
        {
            IReadOnlyList<ChallengeQuestionDefinition> silver = ParseEvent(
                ChallengeDocument("cs1"), AcademicRank.Silver);
            IReadOnlyList<ChallengeQuestionDefinition> gold = ParseEvent(
                ChallengeDocument("cg1"), AcademicRank.Gold);
            IReadOnlyList<ChallengeQuestionDefinition> diamond = ParseEvent(
                ChallengeDocument("cd1"), AcademicRank.Diamond);

            Assert.That(silver.Single().Id.Value, Is.EqualTo("cs1"));
            Assert.That(gold.Single().Id.Value, Is.EqualTo("cg1"));
            Assert.That(diamond.Single().Id.Value, Is.EqualTo("cd1"));
        }

        [TestCase("1")]
        [TestCase("CS1")]
        [TestCase("cs01")]
        [TestCase(" cs1 ")]
        [TestCase("cg1")]
        [TestCase("cs2")]
        public void EventParser_RejectsNonCanonicalChallengeIds(string id)
        {
            Assert.That(TryParseEvent(ChallengeDocument(id), AcademicRank.Silver,
                out _, out _), Is.False);
        }

        private static List<RankedQuestionDocument> Parse(string json)
        {
            MethodInfo method = typeof(FirestoreQuestionCatalogRepository).GetMethod(
                "TryReadItems",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var documents = new List<RankedQuestionDocument>();
            object[] arguments = { json, AcademicRank.Silver, documents, null };

            bool succeeded = (bool)method.Invoke(null, arguments);

            Assert.That(succeeded, Is.True, arguments[3] as string);
            return documents;
        }

        private static IReadOnlyList<ChallengeQuestionDefinition> ParseEvent(
            string json, AcademicRank rank)
        {
            Assert.That(TryParseEvent(json, rank,
                out IReadOnlyList<ChallengeQuestionDefinition> questions,
                out string error), Is.True, error);
            return questions;
        }

        private static bool TryParseEvent(string json, AcademicRank rank,
            out IReadOnlyList<ChallengeQuestionDefinition> questions, out string error)
        {
            MethodInfo method = typeof(FirestoreEventQuestionCatalogRepository).GetMethod(
                "TryMapItems",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { json, rank, null, null };
            bool succeeded = (bool)method.Invoke(null, arguments);
            questions = arguments[2] as IReadOnlyList<ChallengeQuestionDefinition>;
            error = arguments[3] as string;
            return succeeded;
        }

        private static string ChallengeDocument(string id) =>
            "{\"fields\":{\"q1\":{\"mapValue\":{\"fields\":{" +
            $"\"id\":{{\"stringValue\":\"{id}\"}}," +
            "\"answer\":{\"integerValue\":\"39\"}," +
            "\"video-url\":{\"stringValue\":\"https://youtu.be/M7lc1UVf-VE\"}}}}}}";
    }
}
