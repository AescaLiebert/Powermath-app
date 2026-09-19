using System;
using System.Collections;
using System.Collections.Generic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Combat;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerMath.Gameplay.Academic
{
    public sealed class FirestoreEventQuestionCatalogRepository
    {
        private readonly MonoBehaviour _host;
        private readonly GameApiSettings _settings;
        private UnityWebRequest _activeRequest;
        private int _generation;

        public FirestoreEventQuestionCatalogRepository(MonoBehaviour host, GameApiSettings settings)
        {
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public void Load(IEnumerable<string> documentIds,
            Action<EventQuestionCatalog, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            _host.StartCoroutine(LoadRoutine(++_generation, documentIds, completed));
        }

        public void Cancel()
        {
            _generation++;
            _activeRequest?.Abort();
            _activeRequest = null;
        }

        private IEnumerator LoadRoutine(int generation, IEnumerable<string> documentIds,
            Action<EventQuestionCatalog, string> completed)
        {
            var documents = new Dictionary<string, IReadOnlyList<ChallengeQuestionDefinition>>(StringComparer.Ordinal);
            var catalogIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string rawId in documentIds ?? Array.Empty<string>())
            {
                string catalogId = rawId?.Trim();
                if (!string.IsNullOrEmpty(catalogId)) catalogIds.Add(catalogId);
            }

            var questions = new List<ChallengeQuestionDefinition>();
            foreach (AcademicRank rank in new[]
            {
                AcademicRank.Silver,
                AcademicRank.Gold,
                AcademicRank.Diamond
            })
            {
                if (!_settings.TryGetChallengeQuestionDocument(
                        rank.ToString(), out string documentId, out string url))
                {
                    Complete(generation, completed, null,
                        $"Challenge question document for {rank} is not configured.");
                    yield break;
                }

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    _activeRequest = request;
                    request.timeout = Mathf.Min(3,
                        _settings.RequestTimeoutSeconds > 0
                            ? _settings.RequestTimeoutSeconds
                            : 3);
                    request.SetRequestHeader("Accept", "application/json");
                    yield return request.SendWebRequest();
                    _activeRequest = null;
                    if (generation != _generation) yield break;
                    string error = string.Empty;
                    if (request.result != UnityWebRequest.Result.Success ||
                        request.responseCode < 200 || request.responseCode >= 300 ||
                        !TryMapItems(request.downloadHandler.text, rank,
                            out IReadOnlyList<ChallengeQuestionDefinition> rankQuestions,
                            out error))
                    {
                        Complete(generation, completed, null,
                            string.IsNullOrEmpty(error)
                                ? $"Could not load Challenge question document '{documentId}'."
                                : error);
                        yield break;
                    }
                    questions.AddRange(rankQuestions);
                }
            }

            foreach (string catalogId in catalogIds)
                documents.Add(catalogId, questions);
            try { Complete(generation, completed, new EventQuestionCatalog(documents), string.Empty); }
            catch (Exception exception) { Complete(generation, completed, null, exception.Message); }
        }

        private static readonly string[] IdFieldCandidates =
            { "id", "Id", "question_id", "questionId", "ID" };
        private static readonly string[] VideoLinkFieldCandidates =
            { "video_link", "videoLink", "video-url", "url", "link", "video_url", "videoUrl" };
        private static readonly string[] AnswerFieldCandidates =
            { "answer", "Answer", "correct_answer", "correctAnswer", "value" };

        private static bool TryMapItems(string json,
            AcademicRank expectedRank,
            out IReadOnlyList<ChallengeQuestionDefinition> questions, out string error)
        {
            var result = new List<ChallengeQuestionDefinition>();
            questions = result; error = string.Empty;
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue root, out string parseError) ||
                !FirestoreJsonNavigator.TryGetDocumentFields(root, out JsonValue fields))
            {
                error = $"The shared {expectedRank} Challenge document has an invalid document structure. {parseError}".Trim();
                return false;
            }

            if (fields.TryGet("items", out JsonValue items) ||
                fields.TryGet("Items", out items) ||
                fields.TryGet("questions", out items) ||
                fields.TryGet("Questions", out items))
            {
                if (!FirestoreJsonNavigator.TryGetArrayValues(items,
                        out IReadOnlyList<JsonValue> values))
                {
                    error = $"The shared {expectedRank} Challenge document has an invalid items array.";
                    return false;
                }
                foreach (JsonValue value in values)
                    if (!TryMapQuestion(value, null, expectedRank, result, out error))
                        return false;
            }
            else
            {
                var keyedQuestions = new List<KeyValuePair<int, JsonValue>>();
                foreach (KeyValuePair<string, JsonValue> pair in
                         fields.Object ?? new Dictionary<string, JsonValue>())
                {
                    if (string.Equals(pair.Key, "_meta", StringComparison.Ordinal))
                        continue;
                    if (!TryReadQuestionFieldOrdinal(pair.Key, out int ordinal))
                    {
                        error = $"The shared {expectedRank} Challenge document contains an unsupported field '{pair.Key}'.";
                        return false;
                    }
                    keyedQuestions.Add(new KeyValuePair<int, JsonValue>(ordinal, pair.Value));
                }
                keyedQuestions.Sort((left, right) => left.Key.CompareTo(right.Key));
                foreach (KeyValuePair<int, JsonValue> question in keyedQuestions)
                    if (!TryMapQuestion(question.Value, question.Key, expectedRank,
                            result, out error))
                        return false;
            }

            if (result.Count == 0)
            {
                error = $"The shared {expectedRank} Challenge document is empty.";
                return false;
            }
            return true;
        }

        private static bool TryMapQuestion(JsonValue value, int? fieldOrdinal,
            AcademicRank expectedRank, ICollection<ChallengeQuestionDefinition> destination,
            out string error)
        {
            error = string.Empty;
            string rawId = "<unknown>";
            if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue item) ||
                !TryReadAnyString(item, IdFieldCandidates, out rawId) ||
                !TryReadAnyString(item, VideoLinkFieldCandidates, out string link) ||
                !TryReadAnyInteger(item, AnswerFieldCandidates, out long answer) ||
                !ChallengeQuestionId.TryParse(rawId, out ChallengeQuestionId id) ||
                id.Rank != expectedRank ||
                (fieldOrdinal.HasValue && id.Ordinal != fieldOrdinal.Value) ||
                answer < 0 || answer > int.MaxValue ||
                !YouTubeVideoAddress.TryParse(link, out Uri uri, out string videoId))
            {
                error = $"Challenge question '{rawId}' is malformed or does not match Rank {expectedRank}.";
                return false;
            }
            destination.Add(new ChallengeQuestionDefinition(id, uri, (int)answer, videoId));
            return true;
        }

        private static bool TryReadQuestionFieldOrdinal(string fieldName, out int ordinal)
        {
            ordinal = 0;
            return !string.IsNullOrEmpty(fieldName) && fieldName.Length > 1 &&
                (fieldName[0] == 'q' || fieldName[0] == 'Q') &&
                int.TryParse(fieldName.Substring(1),
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out ordinal) && ordinal > 0;
        }

        private static bool TryReadString(JsonValue fields, string name, out string value)
        {
            value = string.Empty;
            return fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadString(leaf, out value);
        }

        private static bool TryReadInteger(JsonValue fields, string name, out long value)
        {
            value = 0;
            return fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadInteger(leaf, out value);
        }

        private static bool TryReadAnyString(JsonValue fields, string[] names,
            out string value)
        {
            value = string.Empty;
            for (int index = 0; index < names.Length; index++)
                if (TryReadString(fields, names[index], out value) &&
                    !string.IsNullOrEmpty(value)) return true;
            return false;
        }

        private static bool TryReadAnyInteger(JsonValue fields, string[] names,
            out long value)
        {
            value = 0;
            for (int index = 0; index < names.Length; index++)
                if (TryReadInteger(fields, names[index], out value)) return true;
            return false;
        }

        private void Complete(int generation,
            Action<EventQuestionCatalog, string> completed,
            EventQuestionCatalog catalog, string error)
        {
            if (generation == _generation) completed(catalog, error);
        }
    }
}
