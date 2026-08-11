using System;
using System.Collections.Generic;
using System.Linq;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public sealed class EventQuestionCatalog
    {
        private readonly Dictionary<string, QuestionDefinition[]> _documents;

        public EventQuestionCatalog(
            IReadOnlyDictionary<string, IReadOnlyList<QuestionDefinition>> documents)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            _documents = new Dictionary<string, QuestionDefinition[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyList<QuestionDefinition>> pair in documents)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null || pair.Value.Count == 0)
                    throw new ArgumentException("Each event question document needs an ID and questions.");
                QuestionDefinition[] questions = pair.Value.Where(value => value != null)
                    .OrderBy(value => value.Id.Value).ToArray();
                if (questions.Length == 0 || questions.Select(value => value.Id).Distinct().Count() != questions.Length)
                    throw new ArgumentException($"Event question document '{pair.Key}' contains invalid or duplicate questions.");
                _documents.Add(pair.Key.Trim(), questions);
            }
        }

        public QuestionDefinition Select(string documentId, string runId,
            StageId stage, int attemptOrdinal)
        {
            if (!_documents.TryGetValue(documentId ?? string.Empty, out QuestionDefinition[] questions))
                throw new InvalidOperationException($"Event question document '{documentId}' is not loaded.");
            ulong hash = StableHash64.Compute(runId, stage.Value.ToString(),
                Math.Max(0, attemptOrdinal).ToString(), documentId, "event-question");
            return questions[(int)(hash % (ulong)questions.Length)];
        }
    }
}
