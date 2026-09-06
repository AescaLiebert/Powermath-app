using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;

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
    }
}
