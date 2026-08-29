using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PowerMath.PlayerData;
using UnityEngine.Networking;

namespace PowerMath.Session
{
    public sealed class FirestoreRestClient
    {
        public enum FailureKind
        {
            Configuration,
            Network,
            AuthenticationRequired,
            InvalidCredentials,
            InvalidResponse,
            PermissionDenied,
            ServiceUnavailable
        }

        public readonly struct Failure
        {
            public Failure(FailureKind kind, string playerMessage)
            {
                Kind = kind;
                PlayerMessage = playerMessage;
            }

            public FailureKind Kind { get; }
            public string PlayerMessage { get; }
        }

        public readonly struct AuthenticatedAccount
        {
            public AuthenticatedAccount(string username, string levelDocumentId, PlayerSnapshot player)
            {
                Username = username;
                LevelDocumentId = levelDocumentId;
                Player = player;
            }

            public string Username { get; }
            public string LevelDocumentId { get; }
            public PlayerSnapshot Player { get; }
        }

        private readonly GameApiSettings _settings;

        public FirestoreRestClient(GameApiSettings settings) => _settings = settings;

        public IEnumerator Authenticate(
            string username,
            string password,
            Action<AuthenticatedAccount> onSuccess,
            Action<Failure> onFailure)
        {
            string normalizedUsername = DirectFirestoreCredentialStore.NormalizeUsername(username);
            if (!DirectFirestoreCredentialStore.IsValidUsername(normalizedUsername) ||
                !DirectFirestoreCredentialStore.IsSixDigitPassword(password))
            {
                onFailure?.Invoke(InvalidCredentialsFailure());
                yield break;
            }

            if (_settings == null || _settings.LevelDocumentCount == 0)
            {
                onFailure?.Invoke(ConfigurationFailure());
                yield break;
            }

            bool foundAnyLevelDocument = false;
            for (int index = 0; index < _settings.LevelDocumentCount; index++)
            {
                if (!_settings.TryGetLevelDocument(
                    index,
                    out string levelDocumentId,
                    out string gradeBand,
                    out string url))
                {
                    onFailure?.Invoke(ConfigurationFailure());
                    yield break;
                }

                JsonValue document = null;
                using (UnityWebRequest request = CreateGetRequest(url))
                {
                    yield return request.SendWebRequest();
                    if (request.responseCode == 404) continue;
                    if (!TryHandleTransport(request, onFailure)) yield break;
                    foundAnyLevelDocument = true;
                    if (!TryParseDocument(request.downloadHandler.text, out document))
                    {
                        onFailure?.Invoke(InvalidResponseFailure());
                        yield break;
                    }
                }

                if (!TryGetStudent(document, normalizedUsername, out JsonValue student))
                    continue;

                if (!CredentialsMatch(student, normalizedUsername, password))
                {
                    onFailure?.Invoke(InvalidCredentialsFailure());
                    yield break;
                }

                // Existing users are repaired with update masks. No missing student is created.
                for (int patchAttempt = 0; patchAttempt < 2; patchAttempt++)
                {
                    FirestorePatchPlan plan = PlayerDefaultsPlanner.Plan(student, normalizedUsername);
                    if (plan.IsEmpty) break;

                    string updateTime = ReadRawStringProperty(document, "updateTime");
                    if (string.IsNullOrWhiteSpace(updateTime))
                    {
                        onFailure?.Invoke(InvalidResponseFailure());
                        yield break;
                    }
                    using (UnityWebRequest patch = CreatePatchRequest(url, plan, updateTime))
                    {
                        yield return patch.SendWebRequest();
                        bool conflict = patch.responseCode == 409 || patch.responseCode == 412;
                        if (!conflict && !TryHandleTransport(patch, onFailure)) yield break;
                    }

                    using (UnityWebRequest reload = CreateGetRequest(url))
                    {
                        yield return reload.SendWebRequest();
                        if (!TryHandleTransport(reload, onFailure))
                            yield break;
                        if (!TryParseDocument(reload.downloadHandler.text, out document) ||
                            !TryGetStudent(document, normalizedUsername, out student))
                        {
                            onFailure?.Invoke(InvalidResponseFailure());
                            yield break;
                        }
                    }

                    if (!CredentialsMatch(student, normalizedUsername, password))
                    {
                        onFailure?.Invoke(InvalidCredentialsFailure());
                        yield break;
                    }

                    if (patchAttempt == 1 && !PlayerDefaultsPlanner.Plan(student, normalizedUsername).IsEmpty)
                    {
                        onFailure?.Invoke(new Failure(
                            FailureKind.ServiceUnavailable,
                            "Your player data changed while loading. Please try again."
                        ));
                        yield break;
                    }
                }

                if (!TryMapPlayer(
                    normalizedUsername,
                    levelDocumentId,
                    gradeBand,
                    student,
                    out PlayerSnapshot player))
                {
                    onFailure?.Invoke(InvalidResponseFailure());
                    yield break;
                }

                onSuccess?.Invoke(new AuthenticatedAccount(
                    normalizedUsername,
                    levelDocumentId,
                    player
                ));
                yield break;
            }

            onFailure?.Invoke(foundAnyLevelDocument
                ? InvalidCredentialsFailure()
                : new Failure(
                    FailureKind.InvalidResponse,
                    "The Grade 4-6 competition data could not be found."
                ));
        }

        public IEnumerator CreateBootstrap(
            AuthenticatedAccount account,
            bool remembered,
            Action<BootstrapResponse> onSuccess,
            Action<Failure> onFailure)
        {
            if (account.Player == null)
            {
                onFailure?.Invoke(InvalidResponseFailure());
                yield break;
            }

            onSuccess?.Invoke(new BootstrapResponse
            {
                schemaVersion = PlayerSessionStore.SupportedSchemaVersion,
                remembered = remembered,
                player = account.Player,
                serverTimeUtc = DateTime.UtcNow.ToString("O")
            });
        }

        private UnityWebRequest CreateGetRequest(string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = _settings.RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private UnityWebRequest CreatePatchRequest(
            string url,
            FirestorePatchPlan plan,
            string updateTime)
        {
            var builder = new StringBuilder(url);
            string separator = url.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string fieldPath in plan.FieldPaths)
            {
                builder.Append(separator)
                    .Append("updateMask.fieldPaths=")
                    .Append(Uri.EscapeDataString(fieldPath));
                separator = "&";
            }
            if (!string.IsNullOrWhiteSpace(updateTime))
            {
                builder.Append(separator)
                    .Append("currentDocument.updateTime=")
                    .Append(Uri.EscapeDataString(updateTime));
            }

            var request = new UnityWebRequest(builder.ToString(), "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(plan.ToJson())),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _settings.RequestTimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            return request;
        }

        private static bool TryParseDocument(string json, out JsonValue document)
        {
            return FirestoreJsonNavigator.TryParse(json, out document, out _) &&
                document != null &&
                document.Kind == JsonValueKind.Object;
        }

        private static bool TryGetStudent(JsonValue document, string username, out JsonValue student)
        {
            student = null;
            return FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) &&
                fields.TryGet(username, out student) &&
                FirestoreJsonNavigator.TryGetMapFields(student, out _);
        }

        private static bool CredentialsMatch(JsonValue student, string username, string password)
        {
            return TryGetMap(student, "userdata", out JsonValue userData) &&
                TryReadString(userData, "username", out string storedUsername) &&
                TryReadString(userData, "password", out string storedPassword) &&
                string.Equals(
                    DirectFirestoreCredentialStore.NormalizeUsername(storedUsername),
                    username,
                    StringComparison.Ordinal) &&
                string.Equals(storedPassword, password, StringComparison.Ordinal);
        }

        private static bool TryMapPlayer(
            string username,
            string levelDocumentId,
            string gradeBand,
            JsonValue student,
            out PlayerSnapshot player)
        {
            player = null;
            if (!TryGetMap(student, "gamedata", out JsonValue gameData)) return false;
            TryGetMapFromFields(gameData, "profile", out JsonValue profile);
            TryGetMapFromFields(gameData, "progression", out JsonValue progression);
            TryGetMapFromFields(gameData, "wallet", out JsonValue wallet);
            TryGetMapFromFields(gameData, "loadout", out JsonValue loadout);
            TryGetMapFromFields(gameData, "activeRun", out JsonValue activeRun);
            TryGetMapFromFields(gameData, "economy", out JsonValue economy);
            TryGetMapFromFields(gameData, "lastRunSettlement", out JsonValue lastRunSettlement);

            string displayName = ReadString(profile, "displayName", username);
            string iconId = ReadString(profile, "iconId", "avatar-default");
            player = new PlayerSnapshot
            {
                playerId = levelDocumentId + ":" + username,
                revision = ReadLong(gameData, "revision"),
                profile = new PlayerSnapshot.ProfileData
                {
                    displayName = displayName,
                    gradeBand = gradeBand,
                    iconId = iconId,
                    publicPlayerId = ReadString(profile, "publicPlayerId"),
                    displayNameChangedAtUnixSeconds = ReadLong(profile, "displayNameChangedAtUnixSeconds")
                },
                progression = new PlayerSnapshot.ProgressionData
                {
                    currentStage = ReadInt(progression, "currentStage"),
                    highestStage = ReadInt(progression, "highestStage"),
                    activeRank = ReadString(progression, "activeRank", "Silver"),
                    rankProgress = ReadInt(progression, "rankProgress"),
                    prestige = ReadInt(progression, "prestige"),
                    firstStage200Reached = ReadBool(progression, "firstStage200Reached"),
                    firstStage200ReachedAtUnixSeconds = ReadLong(progression, "firstStage200ReachedAtUnixSeconds"),
                    totalDamage = ReadLong(progression, "totalDamage"),
                    legacyAtkBonusBasisPoints = ReadLong(progression, "legacyAtkBonusBasisPoints")
                },
                wallet = new PlayerSnapshot.WalletData
                {
                    silver = ReadLong(wallet, "silver"),
                    gold = ReadLong(wallet, "gold"),
                    diamond = ReadLong(wallet, "diamond"),
                    powerCoins = ReadLong(wallet, "powerCoins")
                },
                inventory = MapInventory(gameData),
                loadout = new PlayerSnapshot.LoadoutData
                {
                    petId = ReadString(loadout, "petId"),
                    weaponId = ReadString(loadout, "weaponId"),
                    avatarId = ReadString(loadout, "avatarId")
                },
                activeRun = new PlayerSnapshot.ActiveRunData
                {
                    runId = ReadString(activeRun, "runId"),
                    currentStage = ReadInt(activeRun, "currentStage"),
                    committedAttemptId = ReadString(activeRun, "committedAttemptId"),
                    biomeId = ReadString(activeRun, "biomeId"),
                    biomeTitle = ReadString(activeRun, "biomeTitle"),
                    encounterKind = ReadString(activeRun, "encounterKind", "NormalMonster"),
                    encounterId = ReadString(activeRun, "encounterId"),
                    questionContentKind = ReadString(activeRun, "questionContentKind"),
                    questionDocumentId = ReadString(activeRun, "questionDocumentId"),
                    questionId = ReadLong(activeRun, "questionId"),
                    eventAttemptOrdinal = ReadInt(activeRun, "eventAttemptOrdinal"),
                    enemyId = ReadString(activeRun, "enemyId"),
                    enemyCurrentHp = ReadInt(activeRun, "enemyCurrentHp"),
                    enemyMaximumHp = ReadInt(activeRun, "enemyMaximumHp"),
                    enemyRemainingCooldown = ReadInt(activeRun, "enemyRemainingCooldown"),
                    enemyMaximumCooldown = ReadInt(activeRun, "enemyMaximumCooldown"),
                    playerCurrentHearts = ReadInt(activeRun, "playerCurrentHearts"),
                    playerMaximumHearts = ReadInt(activeRun, "playerMaximumHearts"),
                    phase = ReadString(activeRun, "phase", "EnemyReady"),
                    silverEarned = ReadLong(activeRun, "silverEarned"),
                    goldEarned = ReadLong(activeRun, "goldEarned"),
                    diamondEarned = ReadLong(activeRun, "diamondEarned"),
                    bonusMultiplierBasisPoints = Math.Max(10000, ReadInt(activeRun, "bonusMultiplierBasisPoints")),
                    pendingPresentation = MapAttemptPresentation(activeRun)
                },
                academic = MapAcademic(gameData),
                analytics = MapAnalytics(gameData),
                economy = new PlayerSnapshot.EconomyData
                {
                    lastWeaponAscendTransactionId = ReadString(economy, "lastWeaponAscendTransactionId"),
                    lastWeaponAscendLevel = ReadInt(economy, "lastWeaponAscendLevel"),
                    lastWeaponAscendCost = ReadLong(economy, "lastWeaponAscendCost"),
                    lastPetGachaTransactionId = ReadString(economy, "lastPetGachaTransactionId"),
                    lastPetGachaCatalogVersion = ReadString(economy, "lastPetGachaCatalogVersion"),
                    lastPetGachaPetId = ReadString(economy, "lastPetGachaPetId"),
                    lastPetGachaWasNew = ReadBool(economy, "lastPetGachaWasNew"),
                    lastPetGachaCost = ReadLong(economy, "lastPetGachaCost"),
                    lastPetGachaResultingPowerCoins = ReadLong(economy, "lastPetGachaResultingPowerCoins"),
                    lastPetEquipTransactionId = ReadString(economy, "lastPetEquipTransactionId"),
                    lastPetEquipPetId = ReadString(economy, "lastPetEquipPetId")
                },
                lastRunSettlement = new PlayerSnapshot.RunSettlementData
                {
                    runId = ReadString(lastRunSettlement, "runId"),
                    type = ReadString(lastRunSettlement, "type"),
                    stageReached = ReadInt(lastRunSettlement, "stageReached"),
                    powerCoinsGranted = ReadLong(lastRunSettlement, "powerCoinsGranted"),
                    legacyAtkBasisPointsGranted = ReadLong(lastRunSettlement, "legacyAtkBasisPointsGranted"),
                    prestigeGranted = ReadInt(lastRunSettlement, "prestigeGranted"),
                    resultingPowerCoins = ReadLong(lastRunSettlement, "resultingPowerCoins"),
                    presentationVersion = ReadInt(lastRunSettlement, "presentationVersion"),
                    presentationId = ReadString(lastRunSettlement, "presentationId"),
                    presentationStatus = ReadString(lastRunSettlement, "presentationStatus", "None"),
                    presentationCause = ReadString(lastRunSettlement, "presentationCause"),
                    sourceBiomeId = ReadString(lastRunSettlement, "sourceBiomeId"),
                    sourceEncounterId = ReadString(lastRunSettlement, "sourceEncounterId"),
                    sourceEncounterKind = ReadString(lastRunSettlement, "sourceEncounterKind"),
                    sourcePowerCoins = ReadLong(lastRunSettlement, "sourcePowerCoins"),
                    sourceLegacyAtkBasisPoints = ReadLong(lastRunSettlement, "sourceLegacyAtkBasisPoints"),
                    sourcePrestige = ReadInt(lastRunSettlement, "sourcePrestige"),
                    sourceEffectiveAttack = ReadLong(lastRunSettlement, "sourceEffectiveAttack"),
                    resultingEffectiveAttack = ReadLong(lastRunSettlement, "resultingEffectiveAttack"),
                    acknowledgedAtUnixSeconds = ReadLong(lastRunSettlement, "acknowledgedAtUnixSeconds")
                }
            };
            return true;
        }

        private static PlayerSnapshot.AttemptPresentationData MapAttemptPresentation(
            JsonValue activeRun)
        {
            if (!TryGetMapFromFields(activeRun, "pendingPresentation", out JsonValue value))
                return null;
            string presentationId = ReadString(value, "presentationId");
            if (string.IsNullOrWhiteSpace(presentationId)) return null;
            TryGetMapFromFields(value, "source", out JsonValue source);
            TryGetMapFromFields(value, "destination", out JsonValue destination);
            return new PlayerSnapshot.AttemptPresentationData
            {
                version = ReadInt(value, "version"),
                presentationId = presentationId,
                attemptId = ReadString(value, "attemptId"),
                outcome = ReadString(value, "outcome"),
                responseScore = ReadInt(value, "responseScore"),
                finalDamage = ReadInt(value, "finalDamage"),
                isCritical = ReadBool(value, "isCritical"),
                resolvedEnemyHpAfter = ReadInt(value, "resolvedEnemyHpAfter"),
                enemyDefeated = ReadBool(value, "enemyDefeated"),
                enemyAttacked = ReadBool(value, "enemyAttacked"),
                playerDefeated = ReadBool(value, "playerDefeated"),
                stageAdvanced = ReadBool(value, "stageAdvanced"),
                biomeChanged = ReadBool(value, "biomeChanged"),
                previousRank = ReadString(value, "previousRank", "Silver"),
                currentRank = ReadString(value, "currentRank", "Silver"),
                source = MapPresentationSnapshot(source),
                destination = MapPresentationSnapshot(destination)
            };
        }

        private static PlayerSnapshot.CombatPresentationData MapPresentationSnapshot(
            JsonValue value)
        {
            return new PlayerSnapshot.CombatPresentationData
            {
                stage = ReadInt(value, "stage"),
                biomeId = ReadString(value, "biomeId"),
                encounterId = ReadString(value, "encounterId"),
                encounterKind = ReadString(value, "encounterKind", "NormalMonster"),
                enemyCurrentHp = ReadInt(value, "enemyCurrentHp"),
                enemyMaximumHp = ReadInt(value, "enemyMaximumHp"),
                enemyRemainingCooldown = ReadInt(value, "enemyRemainingCooldown"),
                enemyMaximumCooldown = ReadInt(value, "enemyMaximumCooldown"),
                playerCurrentHearts = ReadInt(value, "playerCurrentHearts"),
                playerMaximumHearts = ReadInt(value, "playerMaximumHearts"),
                phase = ReadString(value, "phase")
            };
        }

        private static PlayerSnapshot.AcademicData MapAcademic(JsonValue gameData)
        {
            TryGetMapFromFields(gameData, "academic", out JsonValue academic);
            TryGetMapFromFields(academic, "inventories", out JsonValue inventories);
            return new PlayerSnapshot.AcademicData
            {
                auditScore = ReadInt(academic, "auditScore"),
                auditResolvedCount = ReadInt(academic, "auditResolvedCount"),
                silver = MapRankInventory(inventories, "silver"),
                gold = MapRankInventory(inventories, "gold"),
                diamond = MapRankInventory(inventories, "diamond")
            };
        }

        private static PlayerSnapshot.AnalyticsData MapAnalytics(JsonValue gameData)
        {
            TryGetMapFromFields(gameData, "analytics", out JsonValue analytics);
            TryGetMapFromFields(analytics, "byRank", out JsonValue byRank);
            TryGetMapFromFields(analytics, "byQuestion", out JsonValue byQuestion);
            return new PlayerSnapshot.AnalyticsData
            {
                totalQuestionsResolved = ReadLong(analytics, "totalQuestionsResolved"),
                totalCorrect = ReadLong(analytics, "totalCorrect"),
                totalIncorrect = ReadLong(analytics, "totalIncorrect"),
                totalTimeout = ReadLong(analytics, "totalTimeout"),
                totalAbandoned = ReadLong(analytics, "totalAbandoned"),
                responseScoreSum = ReadLong(analytics, "responseScoreSum"),
                responseEfficiencySum = ReadLong(analytics, "responseEfficiencySum"),
                responseDurationMillisecondsSum = ReadLong(analytics, "responseDurationMillisecondsSum"),
                responseScoreHistogram = ReadIntegerArray(analytics, "responseScoreHistogram"),
                responseEfficiencyHistogram = ReadIntegerArray(analytics, "responseEfficiencyHistogram"),
                responseDuration100msHistogram = ReadIntegerArray(analytics, "responseDuration100msHistogram"),
                totalPlaySeconds = ReadLong(analytics, "totalPlaySeconds"),
                lastAppliedAttemptId = ReadString(analytics, "lastAppliedAttemptId"),
                silver = MapRankAnalytics(byRank, "silver"),
                gold = MapRankAnalytics(byRank, "gold"),
                diamond = MapRankAnalytics(byRank, "diamond"),
                byQuestion = MapQuestionAnalytics(byQuestion)
            };
        }

        private static PlayerSnapshot.QuestionAnalyticsData[] MapQuestionAnalytics(JsonValue values)
        {
            var result = new List<PlayerSnapshot.QuestionAnalyticsData>();
            if (values?.Object == null) return result.ToArray();
            foreach (KeyValuePair<string, JsonValue> pair in values.Object)
            {
                if (!FirestoreJsonNavigator.TryGetMapFields(pair.Value, out JsonValue fields)) continue;
                long questionId = ReadLong(fields, "questionId");
                if (questionId <= 0) continue;
                result.Add(new PlayerSnapshot.QuestionAnalyticsData
                {
                    questionId = questionId,
                    resolved = ReadLong(fields, "resolved"),
                    correct = ReadLong(fields, "correct"),
                    incorrect = ReadLong(fields, "incorrect"),
                    timeout = ReadLong(fields, "timeout"),
                    abandoned = ReadLong(fields, "abandoned"),
                    responseScoreSum = ReadLong(fields, "responseScoreSum"),
                    responseDurationMillisecondsSum = ReadLong(fields, "responseDurationMillisecondsSum"),
                    responseEfficiencySum = ReadLong(fields, "responseEfficiencySum")
                });
            }
            result.Sort((left, right) => left.questionId.CompareTo(right.questionId));
            return result.ToArray();
        }

        private static PlayerSnapshot.RankAnalyticsData MapRankAnalytics(
            JsonValue byRank,
            string rank)
        {
            TryGetMapFromFields(byRank, rank, out JsonValue fields);
            return new PlayerSnapshot.RankAnalyticsData
            {
                resolved = ReadLong(fields, "resolved"),
                correct = ReadLong(fields, "correct"),
                responseScoreSum = ReadLong(fields, "responseScoreSum"),
                responseEfficiencySum = ReadLong(fields, "responseEfficiencySum")
            };
        }

        private static PlayerSnapshot.RankInventoryData MapRankInventory(
            JsonValue inventories,
            string rank)
        {
            TryGetMapFromFields(inventories, rank, out JsonValue fields);
            return new PlayerSnapshot.RankInventoryData
            {
                cycle = ReadInt(fields, "cycle"),
                pendingIds = ReadIntegerArray(fields, "pendingIds"),
                failedIds = ReadIntegerArray(fields, "failedIds"),
                attemptedInAuditIds = ReadIntegerArray(fields, "attemptedInAuditIds"),
                clearedInCycleIds = ReadIntegerArray(fields, "clearedInCycleIds")
            };
        }

        private static long[] ReadIntegerArray(JsonValue fields, string name)
        {
            if (fields == null || !fields.TryGet(name, out JsonValue value) ||
                !FirestoreJsonNavigator.TryGetArrayValues(value, out IReadOnlyList<JsonValue> values))
                return Array.Empty<long>();
            var result = new List<long>();
            foreach (JsonValue item in values)
            {
                if (FirestoreJsonNavigator.TryReadInteger(item, out long id)) result.Add(id);
            }
            return result.ToArray();
        }

        private static PlayerSnapshot.InventoryItemData[] MapInventory(JsonValue gameData)
        {
            if (gameData == null || !gameData.TryGet("inventory", out JsonValue inventory) ||
                !FirestoreJsonNavigator.TryGetArrayValues(inventory, out IReadOnlyList<JsonValue> values))
                return Array.Empty<PlayerSnapshot.InventoryItemData>();

            var result = new List<PlayerSnapshot.InventoryItemData>();
            foreach (JsonValue value in values)
            {
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue fields)) continue;
                result.Add(new PlayerSnapshot.InventoryItemData
                {
                    itemId = ReadString(fields, "itemId"),
                    owned = ReadBool(fields, "owned"),
                    upgradeLevel = ReadInt(fields, "upgradeLevel")
                });
            }
            return result.ToArray();
        }

        private static bool TryGetMap(JsonValue mapValue, string name, out JsonValue fields)
        {
            fields = null;
            return FirestoreJsonNavigator.TryGetMapFields(mapValue, out JsonValue source) &&
                TryGetMapFromFields(source, name, out fields);
        }

        private static bool TryGetMapFromFields(JsonValue source, string name, out JsonValue fields)
        {
            fields = null;
            return source != null && source.TryGet(name, out JsonValue value) &&
                FirestoreJsonNavigator.TryGetMapFields(value, out fields);
        }

        private static bool TryReadString(JsonValue fields, string name, out string value)
        {
            value = string.Empty;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadString(leaf, out value);
        }

        private static string ReadString(JsonValue fields, string name, string fallback = "") =>
            TryReadString(fields, name, out string value) ? value : fallback;

        private static long ReadLong(JsonValue fields, string name)
        {
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadInteger(leaf, out long value) ? value : 0L;
        }

        private static int ReadInt(JsonValue fields, string name)
        {
            long value = ReadLong(fields, name);
            return value > int.MaxValue ? int.MaxValue : value < int.MinValue ? int.MinValue : (int)value;
        }

        private static bool ReadBool(JsonValue fields, string name)
        {
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadBoolean(leaf, out bool value) && value;
        }

        private static string ReadRawStringProperty(JsonValue value, string name)
        {
            return value != null && value.TryGet(name, out JsonValue leaf) &&
                leaf.Kind == JsonValueKind.String ? leaf.Text : string.Empty;
        }

        private static bool TryHandleTransport(UnityWebRequest request, Action<Failure> onFailure)
        {
            if (request.result == UnityWebRequest.Result.Success &&
                request.responseCode >= 200 && request.responseCode < 300)
                return true;

            if (request.responseCode == 400)
                onFailure?.Invoke(ConfigurationFailure());
            else if (request.responseCode == 401 || request.responseCode == 403)
                onFailure?.Invoke(new Failure(
                    FailureKind.PermissionDenied,
                    "Firestore rules do not allow this game data request."));
            else if (request.responseCode == 429 || request.responseCode >= 500)
                onFailure?.Invoke(new Failure(
                    FailureKind.ServiceUnavailable,
                    "Firebase is temporarily unavailable. Please try again."));
            else
                onFailure?.Invoke(new Failure(
                    FailureKind.Network,
                    "We could not reach Firebase. Check your connection and try again."));
            return false;
        }

        private static Failure ConfigurationFailure() => new Failure(
            FailureKind.Configuration,
            "PowerMath Firebase settings are incomplete.");
        private static Failure InvalidCredentialsFailure() => new Failure(
            FailureKind.InvalidCredentials,
            "Student name or 6-digit password is incorrect.");
        private static Failure InvalidResponseFailure() => new Failure(
            FailureKind.InvalidResponse,
            "Your PowerMath player data could not be read safely.");
    }
}
