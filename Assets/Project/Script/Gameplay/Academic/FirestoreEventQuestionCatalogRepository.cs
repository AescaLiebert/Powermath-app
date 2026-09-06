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
            var documents = new Dictionary<string, IReadOnlyList<QuestionDefinition>>(StringComparer.Ordinal);
            foreach (string rawId in documentIds ?? Array.Empty<string>())
            {
                string id = rawId?.Trim();
                if (string.IsNullOrEmpty(id) || documents.ContainsKey(id)) continue;
                if (!_settings.TryGetQuestionDocumentById(id, out string url))
                {
                    Complete(generation, completed, null, $"Event question document '{id}' is not configured.");
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
                    string error = string.Empty;
                    if (request.result != UnityWebRequest.Result.Success ||
                        !TryMapItems(request.downloadHandler.text, out IReadOnlyList<QuestionDefinition> questions,
                            out error))
                    {
                        Complete(generation, completed, null,
                            string.IsNullOrEmpty(error) ? $"Could not load event question document '{id}'." : error);
                        yield break;
                    }
                    documents.Add(id, questions);
                }
            }
            try { Complete(generation, completed, new EventQuestionCatalog(documents), string.Empty); }
            catch (Exception exception) { Complete(generation, completed, null, exception.Message); }
        }

        private static bool TryMapItems(string json,
            out IReadOnlyList<QuestionDefinition> questions, out string error)
        {
            var result = new List<QuestionDefinition>();
            questions = result; error = string.Empty;
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue root, out string parseError) ||
                !FirestoreJsonNavigator.TryGetDocumentFields(root, out JsonValue fields) ||
                !fields.TryGet("items", out JsonValue items) ||
                !FirestoreJsonNavigator.TryGetArrayValues(items, out IReadOnlyList<JsonValue> values))
            {
                error = $"Event question document has an invalid items array. {parseError}".Trim();
                return false;
            }
            var mapper = new QuestionDocumentMapper();
            foreach (JsonValue value in values)
            {
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue item) ||
                    !TryReadInteger(item, "id", out long id) ||
                    !TryReadString(item, "video_link", out string link) ||
                    !TryReadInteger(item, "answer", out long answer) ||
                    !mapper.TryMap(new QuestionDocumentDto { id = id, video_link = link, answer = answer },
                        AcademicRank.Diamond, out QuestionDefinition definition, out error))
                    return false;
                result.Add(definition);
            }
            if (result.Count == 0) { error = "Event question document is empty."; return false; }
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

        private void Complete(int generation,
            Action<EventQuestionCatalog, string> completed,
            EventQuestionCatalog catalog, string error)
        {
            if (generation == _generation) completed(catalog, error);
        }
    }
}
