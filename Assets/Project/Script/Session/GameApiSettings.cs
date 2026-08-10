using System;
using UnityEngine;

namespace PowerMath.Session
{
    [CreateAssetMenu(
        fileName = "GameApiSettings",
        menuName = "PowerMath/Session/Game API Settings")]
    public sealed class GameApiSettings : ScriptableObject
    {
        [Header("Firebase Firestore REST")]
        [Tooltip("Firebase project ID, for example powermath-school.")]
        [SerializeField] private string projectId = string.Empty;

        [Tooltip("Usually (default).")]
        [SerializeField] private string databaseId = "(default)";

        [Tooltip(
            "Firebase Web API key. This identifies the Firebase project and is " +
            "not a secret or an authorization mechanism."
        )]
        [SerializeField] private string apiKey = string.Empty;

        [Header("Competition Data Layout")]
        [SerializeField] private string competitionCollection = "competition";
        [SerializeField] private string[] levelDocumentIds =
        {
            "level1",
            "level2",
            "level3"
        };

        [Header("Shared Question Firebase Firestore REST")]
        [Tooltip("Firebase project ID that owns the shared question catalog.")]
        [SerializeField] private string questionProjectId = string.Empty;
        [Tooltip("Usually (default).")]
        [SerializeField] private string questionDatabaseId = "(default)";
        [Tooltip("Firebase Web API key for the shared question project.")]
        [SerializeField] private string questionApiKey = string.Empty;

        [Header("Shared Question Content")]
        [SerializeField] private string questionCollection = "question";
        [SerializeField] private string silverQuestionDocument = "silver";
        [SerializeField] private string goldQuestionDocument = "gold";
        [SerializeField] private string diamondQuestionDocument = "diamond";
        [SerializeField] private string[] gradeBands =
        {
            "Grade 4",
            "Grade 5",
            "Grade 6"
        };

        [Tooltip("Starting timeout. Adjust after human E2E on the school network.")]
        [Min(1)]
        [SerializeField] private int requestTimeoutSeconds = 30;

        [Header("Scene Flow")]
        [SerializeField] private string bootstrapSceneName = "BootstrapScene";
        [SerializeField] private string authenticationSceneName = "AuthenticationScene";
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

#if UNITY_EDITOR
        [Header("Editor Play Mode")]
        [Tooltip(
            "Use the sample-student login and player data in Play Mode. " +
            "This setting and implementation are compiled out of player builds."
        )]
        [SerializeField] private bool useEditorSampleStudent = true;
#endif

        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public string BootstrapSceneName => bootstrapSceneName;
        public string AuthenticationSceneName => authenticationSceneName;
        public string MainMenuSceneName => mainMenuSceneName;

#if UNITY_EDITOR
        public bool UseEditorSampleStudent => useEditorSampleStudent;
#endif

        public int LevelDocumentCount => levelDocumentIds == null
            ? 0
            : levelDocumentIds.Length;

        public bool TryGetLevelDocument(
            int index,
            out string documentId,
            out string gradeBand,
            out string url)
        {
            documentId = string.Empty;
            gradeBand = string.Empty;
            url = string.Empty;
            if (index < 0 || levelDocumentIds == null || gradeBands == null ||
                index >= levelDocumentIds.Length || index >= gradeBands.Length)
            {
                return false;
            }

            documentId = levelDocumentIds[index]?.Trim();
            gradeBand = gradeBands[index]?.Trim();
            if (!TryGetRoot(out string root) ||
                string.IsNullOrWhiteSpace(competitionCollection) ||
                string.IsNullOrWhiteSpace(documentId) ||
                string.IsNullOrWhiteSpace(gradeBand))
            {
                return false;
            }

            url = AppendApiKey(
                root + "/" + Encode(competitionCollection) + "/" + Encode(documentId),
                apiKey
            );
            return true;
        }

        public bool TryGetQuestionDocument(
            string rankName,
            out string documentId,
            out string url)
        {
            documentId = string.Empty;
            url = string.Empty;
            if (string.Equals(rankName, "Silver", StringComparison.OrdinalIgnoreCase))
                documentId = silverQuestionDocument;
            else if (string.Equals(rankName, "Gold", StringComparison.OrdinalIgnoreCase))
                documentId = goldQuestionDocument;
            else if (string.Equals(rankName, "Diamond", StringComparison.OrdinalIgnoreCase))
                documentId = diamondQuestionDocument;
            else
                return false;

            documentId = documentId?.Trim();
            if (!TryGetQuestionRoot(out string root) ||
                string.IsNullOrWhiteSpace(questionCollection) ||
                string.IsNullOrWhiteSpace(documentId))
                return false;

            url = AppendApiKey(
                root + "/" + Encode(questionCollection) + "/" + Encode(documentId),
                questionApiKey);
            return true;
        }

        public bool TryGetLevelDocumentById(string expectedDocumentId, out string url)
        {
            url = string.Empty;
            for (int index = 0; index < LevelDocumentCount; index++)
            {
                if (TryGetLevelDocument(index, out string documentId, out _, out string candidate) &&
                    string.Equals(documentId, expectedDocumentId, StringComparison.Ordinal))
                {
                    url = candidate;
                    return true;
                }
            }
            return false;
        }

        private bool TryGetRoot(out string root)
        {
            root = string.Empty;
            if (string.IsNullOrWhiteSpace(projectId) ||
                string.IsNullOrWhiteSpace(databaseId) ||
                string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            root = "https://firestore.googleapis.com/v1/projects/" + Encode(projectId) +
                "/databases/" + Encode(databaseId) + "/documents";
            return true;
        }

        private bool TryGetQuestionRoot(out string root)
        {
            root = string.Empty;
            if (string.IsNullOrWhiteSpace(questionProjectId) ||
                string.IsNullOrWhiteSpace(questionDatabaseId) ||
                string.IsNullOrWhiteSpace(questionApiKey))
                return false;

            root = "https://firestore.googleapis.com/v1/projects/" +
                Encode(questionProjectId) + "/databases/" +
                Encode(questionDatabaseId) + "/documents";
            return true;
        }

        private static string AppendApiKey(string url, string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? url
                : url + "?key=" + Encode(key.Trim());
        }

        private static string Encode(string value)
        {
            return Uri.EscapeDataString(value.Trim());
        }

        private void OnValidate()
        {
            requestTimeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);

            if (levelDocumentIds == null || gradeBands == null ||
                levelDocumentIds.Length != gradeBands.Length)
            {
                Debug.LogWarning(
                    "GameApiSettings needs one grade band for each level document."
                );
            }
        }
    }
}
