using System;
using System.Collections;
using System.Collections.Generic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.Networking;

namespace PowerMath.Gameplay.Academic
{
    /// <summary>Reads shared question content. It never reads or writes a student document.</summary>
    public sealed class FirestoreQuestionCatalogRepository : IQuestionCatalogRepository
    {
        private readonly MonoBehaviour _host;
        private readonly GameApiSettings _settings;
        private UnityWebRequest _activeRequest;
        private int _generation;

        public FirestoreQuestionCatalogRepository(MonoBehaviour host, GameApiSettings settings)
        {
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public void Load(Action<QuestionCatalogLoadResult> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            int generation = ++_generation;
            _host.StartCoroutine(LoadRoutine(generation, completed));
        }

        public void Cancel()
        {
            _generation++;
            _activeRequest?.Abort();
            _activeRequest = null;
        }

        private IEnumerator LoadRoutine(int generation, Action<QuestionCatalogLoadResult> completed)
        {
            var documents = new List<RankedQuestionDocument>();
            foreach (AcademicRank rank in new[]
            {
                AcademicRank.Silver,
                AcademicRank.Gold,
                AcademicRank.Diamond
            })
            {
                if (!_settings.TryGetQuestionDocument(rank.ToString(), out _, out string url))
                {
                    Complete(generation, completed, QuestionCatalogLoadResult.Failure(
                        $"Question document for {rank} is not configured."));
                    yield break;
                }

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    _activeRequest = request;
                    request.timeout = Mathf.Min(3, _settings.RequestTimeoutSeconds > 0 ? _settings.RequestTimeoutSeconds : 3);
                    request.SetRequestHeader("Accept", "application/json");
                    yield return request.SendWebRequest();
                    _activeRequest = null;
                    if (generation != _generation) yield break;

                    if (request.result != UnityWebRequest.Result.Success ||
                        request.responseCode < 200 || request.responseCode >= 300)
                    {
                        Complete(generation, completed, QuestionCatalogLoadResult.Failure(
                            $"Could not load the shared {rank} question document."));
                        yield break;
                    }

                    if (!TryReadItems(request.downloadHandler.text, rank, documents, out string error))
                    {
                        Complete(generation, completed, QuestionCatalogLoadResult.Failure(error));
                        yield break;
                    }
                }
            }

            Complete(
                generation,
                completed,
                new QuestionDocumentMapper().MapCatalog(documents)
            );
        }

        private static readonly string[] IdFieldCandidates = { "id", "Id", "question_id", "questionId", "ID" };
        private static readonly string[] VideoLinkFieldCandidates = { "video_link", "videoLink", "video-url", "url", "link", "video_url", "videoUrl" };
        private static readonly string[] AnswerFieldCandidates = { "answer", "Answer", "correct_answer", "correctAnswer", "value" };

        private static bool TryReadItems(
            string json,
            AcademicRank rank,
            ICollection<RankedQuestionDocument> destination,
            out string error)
        {
            error = string.Empty;
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue root, out string parseError) ||
                !FirestoreJsonNavigator.TryGetDocumentFields(root, out JsonValue fields))
            {
                error = $"The shared {rank} question document has an invalid document structure. {parseError}".Trim();
                return false;
            }

            JsonValue items = null;
            if (fields.TryGet("items", out items) ||
                fields.TryGet("Items", out items) ||
                fields.TryGet("questions", out items) ||
                fields.TryGet("Questions", out items))
            {
                if (!FirestoreJsonNavigator.TryGetArrayValues(items, out IReadOnlyList<JsonValue> values))
                {
                    error = $"The shared {rank} question document has an invalid items array.";
                    return false;
                }

                foreach (JsonValue value in values)
                {
                    if (!TryReadQuestion(value, null, rank, destination, out error))
                        return false;
                }
                return true;
            }

            var keyedQuestions = new List<KeyValuePair<long, JsonValue>>();
            foreach (KeyValuePair<string, JsonValue> pair in
                     fields.Object ?? new Dictionary<string, JsonValue>())
            {
                if (string.Equals(pair.Key, "_meta", StringComparison.Ordinal))
                    continue;
                if (!TryReadQuestionFieldId(pair.Key, out long questionId))
                {
                    error = $"The shared {rank} question document contains an unsupported field '{pair.Key}'.";
                    return false;
                }
                keyedQuestions.Add(new KeyValuePair<long, JsonValue>(questionId, pair.Value));
            }

            if (keyedQuestions.Count == 0)
            {
                error = $"The shared {rank} question document contains no questions.";
                return false;
            }

            keyedQuestions.Sort((left, right) => left.Key.CompareTo(right.Key));
            foreach (KeyValuePair<long, JsonValue> question in keyedQuestions)
            {
                if (!TryReadQuestion(question.Value, question.Key, rank, destination, out error))
                    return false;
            }
            return true;
        }

        private static bool TryReadQuestion(
            JsonValue value,
            long? documentFieldId,
            AcademicRank rank,
            ICollection<RankedQuestionDocument> destination,
            out string error)
        {
            error = string.Empty;
            if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue itemFields) ||
                !TryReadAnyString(itemFields, VideoLinkFieldCandidates, out string videoLink) ||
                !TryReadAnyInteger(itemFields, AnswerFieldCandidates, out long answer))
            {
                error = $"The shared {rank} question document contains a malformed item.";
                return false;
            }

            long id;
            if (documentFieldId.HasValue)
            {
                // The deployed catalog uses q1/q2/... as the stable numeric identity
                // while its display/content id is rank-prefixed (s1/g1/d1).
                id = documentFieldId.Value;
            }
            else if (!TryReadAnyInteger(itemFields, IdFieldCandidates, out id))
            {
                error = $"The shared {rank} question document contains a malformed item.";
                return false;
            }

            destination.Add(new RankedQuestionDocument(
                rank,
                new QuestionDocumentDto
                {
                    id = id,
                    video_link = videoLink,
                    answer = answer
                }
            ));
            return true;
        }

        private static bool TryReadQuestionFieldId(string fieldName, out long id)
        {
            id = 0;
            return !string.IsNullOrEmpty(fieldName) &&
                fieldName.Length > 1 &&
                (fieldName[0] == 'q' || fieldName[0] == 'Q') &&
                long.TryParse(
                    fieldName.Substring(1),
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out id) &&
                id >= 0;
        }

        private static bool TryReadString(JsonValue fields, string name, out string value)
        {
            value = string.Empty;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadString(leaf, out value);
        }

        private static bool TryReadInteger(JsonValue fields, string name, out long value)
        {
            value = 0;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadInteger(leaf, out value);
        }

        private static bool TryReadAnyString(JsonValue fields, string[] candidateNames, out string value)
        {
            value = string.Empty;
            if (fields == null) return false;
            for (int i = 0; i < candidateNames.Length; i++)
            {
                if (TryReadString(fields, candidateNames[i], out value) && !string.IsNullOrEmpty(value))
                    return true;
            }
            return false;
        }

        private static bool TryReadAnyInteger(JsonValue fields, string[] candidateNames, out long value)
        {
            value = 0;
            if (fields == null) return false;
            for (int i = 0; i < candidateNames.Length; i++)
            {
                if (TryReadInteger(fields, candidateNames[i], out value))
                    return true;
            }
            return false;
        }

        private void Complete(
            int generation,
            Action<QuestionCatalogLoadResult> completed,
            QuestionCatalogLoadResult result)
        {
            if (generation == _generation) completed(result);
        }
    }
}
