using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerMath.Gameplay.Academic
{
    public sealed class QuestionCatalog
    {
        private readonly Dictionary<QuestionKey, QuestionDefinition> _byKey;
        private readonly Dictionary<AcademicRankTier, QuestionDefinition[]> _byRank;

        private QuestionCatalog(
            Dictionary<QuestionKey, QuestionDefinition> byKey,
            Dictionary<AcademicRankTier, QuestionDefinition[]> byRank)
        {
            _byKey = byKey;
            _byRank = byRank;
        }

        public static QuestionCatalogBuildResult TryCreate(
            IEnumerable<QuestionDefinition> definitions)
        {
            if (definitions == null)
            {
                return QuestionCatalogBuildResult.Failure("Question definitions are required.");
            }

            var errors = new List<string>();
            var byKey = new Dictionary<QuestionKey, QuestionDefinition>();
            var ordered = new List<QuestionDefinition>();
            foreach (QuestionDefinition definition in definitions)
            {
                if (definition == null)
                {
                    errors.Add("Question definition cannot be null.");
                    continue;
                }

                if (!byKey.TryAdd(definition.Key, definition))
                {
                    errors.Add($"Duplicate question key '{definition.Key}'.");
                    continue;
                }

                ordered.Add(definition);
            }

            var byRank = new Dictionary<AcademicRankTier, QuestionDefinition[]>();
            foreach (AcademicRankTier tier in Enum.GetValues(typeof(AcademicRankTier)))
            {
                QuestionDefinition[] rankQuestions = ordered
                    .Where(item => item.Rank.Tier == tier)
                    .OrderBy(item => item.Id.Value)
                    .ToArray();
                if (rankQuestions.Length < AuditWindow.RequiredResults)
                {
                    errors.Add(
                        $"Rank {tier} requires at least {AuditWindow.RequiredResults} distinct questions."
                    );
                }

                byRank[tier] = rankQuestions;
            }

            return errors.Count > 0
                ? QuestionCatalogBuildResult.Failure(errors.ToArray())
                : QuestionCatalogBuildResult.Success(new QuestionCatalog(byKey, byRank));
        }

        public bool TryGet(AcademicRank rank, QuestionId id, out QuestionDefinition definition)
        {
            return _byKey.TryGetValue(new QuestionKey(rank, id), out definition);
        }

        public IReadOnlyList<QuestionDefinition> GetRankQuestions(AcademicRank rank)
        {
            return _byRank[rank.Tier];
        }
    }

    public sealed class QuestionCatalogBuildResult
    {
        private QuestionCatalogBuildResult(QuestionCatalog catalog, string[] errors)
        {
            Catalog = catalog;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool IsSuccess => Catalog != null;
        public QuestionCatalog Catalog { get; }
        public IReadOnlyList<string> Errors { get; }

        public static QuestionCatalogBuildResult Success(QuestionCatalog catalog)
        {
            return new QuestionCatalogBuildResult(
                catalog ?? throw new ArgumentNullException(nameof(catalog)),
                Array.Empty<string>()
            );
        }

        public static QuestionCatalogBuildResult Failure(params string[] errors)
        {
            return new QuestionCatalogBuildResult(null, errors);
        }
    }
}
