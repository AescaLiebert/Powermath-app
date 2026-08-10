using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Academic
{
    public interface IQuestionCatalogRepository
    {
        void Load(Action<QuestionCatalogLoadResult> completed);
        void Cancel();
    }

    public sealed class QuestionCatalogLoadResult
    {
        private QuestionCatalogLoadResult(QuestionCatalog catalog, string[] errors)
        {
            Catalog = catalog;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool IsSuccess => Catalog != null;
        public QuestionCatalog Catalog { get; }
        public IReadOnlyList<string> Errors { get; }

        public static QuestionCatalogLoadResult Success(QuestionCatalog catalog)
        {
            return new QuestionCatalogLoadResult(
                catalog ?? throw new ArgumentNullException(nameof(catalog)),
                Array.Empty<string>()
            );
        }

        public static QuestionCatalogLoadResult Failure(params string[] errors)
        {
            return new QuestionCatalogLoadResult(null, errors);
        }
    }
}
