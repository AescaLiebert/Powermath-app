using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private string leaderboardPublicCollection = "leaderboard-public";
        [Tooltip("The collection name for each level (e.g. level-1, level-2, level-3).")]
        [FormerlySerializedAs("levelDocumentIds")]
        [SerializeField] private string[] levelCollections =
        {
            "level-1",
            "level-2",
            "level-3"
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
        [Header("Shared Challenge Content")]
        [SerializeField] private string silverChallengeDocument = "challenge-silver";
        [SerializeField] private string goldChallengeDocument = "challenge-gold";
        [SerializeField] private string diamondChallengeDocument = "challenge-diamond";
        [SerializeField] private string[] gradeBands =
        {
            "Grade 4",
            "Grade 5",
            "Grade 6"
        };

        [Tooltip("Starting timeout. Adjust after human E2E on the school network.")]
        [Min(1)]
        [SerializeField] private int requestTimeoutSeconds = 30;

        [Header("Version & Updates")]
        [Tooltip("Relative or absolute URL to version.json manifest. Defaults to version.json for WebGL root.")]
        [SerializeField] private string versionManifestUrl = "version.json";
        [SerializeField] private bool enableVersionCheck = true;

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
        public string VersionManifestUrl => versionManifestUrl;
        public bool EnableVersionCheck => enableVersionCheck;
        public string BootstrapSceneName => bootstrapSceneName;
        public string AuthenticationSceneName => authenticationSceneName;
        public string MainMenuSceneName => mainMenuSceneName;

#if UNITY_EDITOR
        public bool UseEditorSampleStudent => useEditorSampleStudent;
#endif

        public int LevelCollectionCount => levelCollections == null
            ? 0
            : levelCollections.Length;

        public int LevelDocumentCount => LevelCollectionCount;

        public bool TryGetLevel(
            int index,
            out string levelId,
            out string gradeBand)
        {
            levelId = string.Empty;
            gradeBand = string.Empty;
            if (index < 0 || levelCollections == null || gradeBands == null ||
                index >= levelCollections.Length || index >= gradeBands.Length)
            {
                return false;
            }

            levelId = levelCollections[index]?.Trim();
            gradeBand = gradeBands[index]?.Trim();
            return !string.IsNullOrWhiteSpace(levelId) && !string.IsNullOrWhiteSpace(gradeBand);
        }

        public bool TryGetPlayerDocument(
            string levelId,
            string username,
            out string url)
        {
            url = string.Empty;
            levelId = levelId?.Trim();
            username = username?.Trim();
            if (!TryGetRoot(out string root) ||
                string.IsNullOrWhiteSpace(levelId) ||
                string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            url = AppendApiKey(
                root + "/" + Encode(levelId) + "/" + Encode(username),
                apiKey
            );
            return true;
        }

        public bool TryGetPlayerDocument(
            int index,
            string username,
            out string levelId,
            out string gradeBand,
            out string url)
        {
            url = string.Empty;
            if (!TryGetLevel(index, out levelId, out gradeBand))
            {
                return false;
            }

            return TryGetPlayerDocument(levelId, username, out url);
        }

        public bool TryGetPlayerDocumentByPlayerId(
            string playerId,
            out string url)
        {
            url = string.Empty;
            if (string.IsNullOrWhiteSpace(playerId)) return false;
            int separator = playerId.IndexOf(':');
            if (separator <= 0 || separator >= playerId.Length - 1) return false;
            return TryGetPlayerDocument(
                playerId.Substring(0, separator),
                playerId.Substring(separator + 1),
                out url
            );
        }

        public bool TryGetLevelDocument(
            int index,
            out string documentId,
            out string gradeBand,
            out string url)
        {
            url = string.Empty;
            if (!TryGetLevel(index, out documentId, out gradeBand))
            {
                return false;
            }

            if (!TryGetRoot(out string root))
            {
                return false;
            }

            url = AppendApiKey(
                root + "/" + Encode(documentId),
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

        public bool TryGetQuestionDocumentById(string documentId, out string url)
        {
            url = string.Empty;
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

        public bool TryGetChallengeQuestionDocument(
            string rankName,
            out string documentId,
            out string url)
        {
            documentId = string.Empty;
            url = string.Empty;
            if (string.Equals(rankName, "Silver", StringComparison.OrdinalIgnoreCase))
                documentId = silverChallengeDocument;
            else if (string.Equals(rankName, "Gold", StringComparison.OrdinalIgnoreCase))
                documentId = goldChallengeDocument;
            else if (string.Equals(rankName, "Diamond", StringComparison.OrdinalIgnoreCase))
                documentId = diamondChallengeDocument;
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

        public bool TryGetLeaderboardDocument(string levelDocumentId, out string url)
        {
            url = string.Empty;
            string publicCollection = string.IsNullOrWhiteSpace(leaderboardPublicCollection)
                ? "leaderboard-public"
                : leaderboardPublicCollection.Trim();
            if (!TryGetRoot(out string root) ||
                string.IsNullOrWhiteSpace(levelDocumentId))
                return false;
            int cohortIndex = -1;
            for (int index = 0; index < LevelDocumentCount; index++)
            {
                if (TryGetLevelDocument(index, out string candidate, out _, out _) &&
                    string.Equals(candidate, levelDocumentId, StringComparison.Ordinal))
                {
                    cohortIndex = index;
                    break;
                }
            }
            if (cohortIndex < 0) return false;
            string publicDocumentId = "level" + (cohortIndex + 1);
            url = AppendApiKey(
                root + "/" + Encode(publicCollection) + "/" + Encode(publicDocumentId),
                apiKey);
            return true;
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

            if (levelCollections == null || gradeBands == null ||
                levelCollections.Length != gradeBands.Length)
            {
                PowerMath.Diagnostics.AppLog.Warning(
                    "Session",
                    "GameApiSettings needs one grade band for each level collection."
                );
            }
        }
    }
}
