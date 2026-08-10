using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Academic.Infrastructure
{
    public sealed class InMemoryQuestionCatalogRepository : IQuestionCatalogRepository
    {
        private readonly RankedQuestionDocument[] _documents;
        private bool _cancelled;

        public InMemoryQuestionCatalogRepository(IEnumerable<RankedQuestionDocument> documents = null)
        {
            _documents = documents == null
                ? CreateDefaultDocuments()
                : new List<RankedQuestionDocument>(documents).ToArray();
        }

        public void Load(Action<QuestionCatalogLoadResult> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            _cancelled = false;
            QuestionCatalogLoadResult result = new QuestionDocumentMapper().MapCatalog(_documents);
            if (!_cancelled) completed(result);
        }

        public void Cancel() => _cancelled = true;

        public static RankedQuestionDocument[] CreateDefaultDocuments()
        {
            var documents = new List<RankedQuestionDocument>();
            AddRank(documents, AcademicRank.Silver, new[] { 7, 8, 9, 10, 11 });
            AddRank(documents, AcademicRank.Gold, new[] { 12, 13, 14, 15, 16 });
            AddRank(documents, AcademicRank.Diamond, new[] { 17, 18, 19, 20, 21 });
            return documents.ToArray();
        }

        private static void AddRank(
            ICollection<RankedQuestionDocument> documents,
            AcademicRank rank,
            IReadOnlyList<int> answers)
        {
            for (int index = 0; index < answers.Count; index++)
            {
                documents.Add(new RankedQuestionDocument(
                    rank,
                    new QuestionDocumentDto
                    {
                        id = index + 1,
                        answer = answers[index],
                        video_link = "https://www.youtube.com/watch?v=M7lc1UVf-VE"
                    }
                ));
            }
        }
    }

    public sealed class UnavailableQuestionCatalogRepository : IQuestionCatalogRepository
    {
        public void Load(Action<QuestionCatalogLoadResult> completed) =>
            completed?.Invoke(QuestionCatalogLoadResult.Failure("Question service is unavailable."));
        public void Cancel() { }
    }
}
