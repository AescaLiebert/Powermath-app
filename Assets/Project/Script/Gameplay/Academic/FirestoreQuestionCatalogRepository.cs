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
                    request.timeout = _settings.RequestTimeoutSeconds;
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

        private static bool TryReadItems(
            string json,
            AcademicRank rank,
            ICollection<RankedQuestionDocument> destination,
            out string error)
        {
            error = string.Empty;
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue root, out string parseError) ||
                !FirestoreJsonNavigator.TryGetDocumentFields(root, out JsonValue fields) ||
                !fields.TryGet("items", out JsonValue items) ||
                !FirestoreJsonNavigator.TryGetArrayValues(items, out IReadOnlyList<JsonValue> values))
            {
                error = $"The shared {rank} question document has an invalid items array. {parseError}".Trim();
                return false;
            }

            foreach (JsonValue value in values)
            {
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue itemFields) ||
                    !TryReadInteger(itemFields, "id", out long id) ||
                    !TryReadString(itemFields, "video_link", out string videoLink) ||
                    !TryReadInteger(itemFields, "answer", out long answer))
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
            }
            return true;
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

        private void Complete(
            int generation,
            Action<QuestionCatalogLoadResult> completed,
            QuestionCatalogLoadResult result)
        {
            if (generation == _generation) completed(result);
        }
    }
}
