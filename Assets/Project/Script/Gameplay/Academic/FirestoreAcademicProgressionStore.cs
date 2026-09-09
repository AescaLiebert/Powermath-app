using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using PowerMath.Gameplay.Combat;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine.Networking;

namespace PowerMath.Gameplay.Academic
{
    /// <summary>Persists only per-student academic state; shared question content is never written.</summary>
    public sealed class FirestoreAcademicProgressionStore
    {
        private readonly GameApiSettings _settings;
        private readonly string _levelDocumentId;
        private readonly string _username;
        private readonly PlayerSnapshot _player;
        private readonly ProfileActivityTracker _activity;
        private int _highestStage;
        public long LastFirstStage200ReachedAtUnixSeconds { get; private set; }

        public FirestoreAcademicProgressionStore(
            GameApiSettings settings,
            string levelDocumentId,
            string username,
            PlayerSnapshot player,
            ProfileActivityTracker activity)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _levelDocumentId = levelDocumentId ?? string.Empty;
            _username = username ?? string.Empty;
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _activity = activity;
            _highestStage = Math.Max(1, player.progression == null ? 1 : player.progression.highestStage);
            LastFirstStage200ReachedAtUnixSeconds = player.progression == null
                ? 0
                : player.progression.firstStage200ReachedAtUnixSeconds;
            player.activeRun = player.activeRun ?? new PlayerSnapshot.ActiveRunData();
            if (string.IsNullOrWhiteSpace(player.activeRun.runId))
                player.activeRun.runId = Guid.NewGuid().ToString("N");
            if (player.activeRun.bonusMultiplierBasisPoints <= 0)
                player.activeRun.bonusMultiplierBasisPoints = 10000;
        }

        public IEnumerator Save(
            GameplaySaveRequest request,
            long nextRevision,
            Action completed,
            Action<string> failed)
        {
            if (request.Academic == null) throw new ArgumentException("Academic state is required.", nameof(request));
            long expectedRevision = _player.revision;
            if (expectedRevision < 0 || nextRevision <= 0 || expectedRevision != nextRevision - 1)
            {
                failed?.Invoke("Player progression changed before saving; reload before continuing.");
                yield break;
            }
            if (!_settings.TryGetLevelDocumentById(_levelDocumentId, out string url))
            {
                failed?.Invoke("Player progression document is not configured.");
                yield break;
            }

            string updateTime;
            long serverSeconds = 0;
            using (UnityWebRequest get = UnityWebRequest.Get(url))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(get.downloadHandler.text, out JsonValue root, out _) ||
                    !root.TryGet("updateTime", out JsonValue updateValue) ||
                    updateValue.Kind != JsonValueKind.String || string.IsNullOrWhiteSpace(updateValue.Text))
                {
                    failed?.Invoke("Could not refresh player progression before saving.");
                    yield break;
                }
                if (!TryValidateSaveDocument(root, _username, expectedRevision, out string validationError))
                {
                    failed?.Invoke(validationError);
                    yield break;
                }
                updateTime = updateValue.Text;
                DateTimeOffset serverTime;
                string date = get.GetResponseHeader("Date");
                if (DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out serverTime))
                    serverSeconds = serverTime.ToUnixTimeSeconds();
            }

            if (_player.revision != expectedRevision)
            {
                failed?.Invoke("Player progression changed while saving; reload before continuing.");
                yield break;
            }
            if (request.Snapshot.Combat.Stage.Value >= 200 &&
                LastFirstStage200ReachedAtUnixSeconds <= 0 && serverSeconds <= 0)
            {
                failed?.Invoke("Could not verify the Stage 200 milestone time.");
                yield break;
            }
            FirestorePatchPlan plan = BuildPlan(request, nextRevision, serverSeconds);
            var address = new StringBuilder(url);
            string separator = url.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in plan.FieldPaths)
            {
                address.Append(separator).Append("updateMask.fieldPaths=")
                    .Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            address.Append(separator).Append("currentDocument.updateTime=")
                .Append(Uri.EscapeDataString(updateTime));

            using (var patch = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                patch.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(plan.ToJson()));
                patch.downloadHandler = new DownloadHandlerBuffer();
                patch.timeout = _settings.RequestTimeoutSeconds;
                patch.SetRequestHeader("Content-Type", "application/json");
                yield return patch.SendWebRequest();
                if (patch.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke(patch.responseCode == 409 || patch.responseCode == 412
                        ? "Player progression changed on another client; reload before continuing."
                        : "Player progression could not be saved.");
                    yield break;
                }
            }
            _highestStage = Math.Max(
                _highestStage,
                request.Snapshot.Combat.Stage.Value);
            if (_highestStage >= 200 && LastFirstStage200ReachedAtUnixSeconds <= 0)
                LastFirstStage200ReachedAtUnixSeconds = serverSeconds;
            long committedPlaySeconds = _activity == null ? 0 : _activity.PendingWholeSeconds;
            LastTotalPlaySeconds = checked(
                (_player.analytics == null ? 0 : _player.analytics.totalPlaySeconds) + committedPlaySeconds);
            _activity?.Commit(committedPlaySeconds);
            completed?.Invoke();
        }

        public long LastTotalPlaySeconds { get; private set; }

        // A fresh document updateTime only protects writes after this GET. Check the
        // snapshot revision too, so a stale tab cannot overwrite an earlier commit.
        private static bool TryValidateSaveDocument(
            JsonValue document,
            string username,
            long expectedRevision,
            out string error)
        {
            error = "Player data could not be read safely. Reload before continuing.";
            if (expectedRevision < 0 ||
                !FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) ||
                !fields.TryGet(username, out JsonValue student)) return false;
            try
            {
                int schema = PlayerSaveContract.Inspect(student, out bool isNewPlayer);
                if (isNewPlayer ||
                    !FirestoreJsonNavigator.TryGetMapFields(student, out JsonValue studentFields) ||
                    !studentFields.TryGet("gamedata", out JsonValue gameValue) ||
                    !FirestoreJsonNavigator.TryGetMapFields(gameValue, out JsonValue game) ||
                    !TryReadStoredInteger(game, "schemaVersion", out long storedSchema)) return false;
                if (schema != PlayerSchemaMigrator.CurrentSchemaVersion || storedSchema != schema)
                {
                    error = "Player data requires migration. Reload before continuing.";
                    return false;
                }
                if (!TryReadStoredInteger(game, "revision", out long revision) || revision < 0) return false;
                if (revision != expectedRevision)
                {
                    // This also handles a lost PATCH response: recover by loading the
                    // committed receipt, never by replaying an older reward snapshot.
                    error = "Player progression changed on another client; reload before continuing.";
                    return false;
                }
                error = string.Empty;
                return true;
            }
            catch (NotSupportedException)
            {
                error = "This save requires a newer game version. Refresh before continuing.";
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool TryReadStoredInteger(JsonValue fields, string name, out long value)
        {
            value = 0;
            return fields.TryGet(name, out JsonValue field) &&
                field.TryGet("integerValue", out JsonValue integer) &&
                (integer.Kind == JsonValueKind.String || integer.Kind == JsonValueKind.Number) &&
                long.TryParse(integer.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private FirestorePatchPlan BuildPlan(
            GameplaySaveRequest request,
            long revision,
            long serverSeconds)
        {
            AcademicPersistenceSnapshot snapshot = request.Academic;
            CombatSnapshot combat = request.Snapshot.Combat;
            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { _username, "gamedata" };
            builder.AddInteger(Join(root, "revision"), revision);
            builder.AddString(Join(root, "progression", "activeRank"), snapshot.ActiveRank.ToString());
            builder.AddInteger(Join(root, "progression", "currentStage"), combat.Stage.Value);
            builder.AddInteger(
                Join(root, "progression", "highestStage"),
                Math.Max(_highestStage, combat.Stage.Value));
            long stage200At = LastFirstStage200ReachedAtUnixSeconds;
            if (stage200At <= 0 && combat.Stage.Value >= 200) stage200At = serverSeconds;
            builder.AddBoolean(Join(root, "progression", "firstStage200Reached"), stage200At > 0);
            builder.AddInteger(Join(root, "progression", "firstStage200ReachedAtUnixSeconds"), stage200At);
            long nextTotalDamage = _player.progression == null ? 0 : _player.progression.totalDamage;
            if (request.SavePoint == GameplaySavePoint.AttemptResolved && request.Resolution != null)
                nextTotalDamage = checked(nextTotalDamage + Math.Max(0, request.Resolution.Combat.FinalDamage));
            builder.AddInteger(Join(root, "progression", "totalDamage"), nextTotalDamage);
            builder.AddInteger(Join(root, "wallet", "silver"), snapshot.Balances.Silver);
            builder.AddInteger(Join(root, "wallet", "gold"), snapshot.Balances.Gold);
            builder.AddInteger(Join(root, "wallet", "diamond"), snapshot.Balances.Diamond);
            builder.AddInteger(Join(root, "academic", "auditScore"), snapshot.AuditScore);
            builder.AddInteger(Join(root, "academic", "auditResolvedCount"), snapshot.AuditResolvedCount);
            AddInventory(builder, root, "silver", snapshot.Silver);
            AddInventory(builder, root, "gold", snapshot.Gold);
            AddInventory(builder, root, "diamond", snapshot.Diamond);
            builder.AddInteger(Join(root, "activeRun", "currentStage"), combat.Stage.Value);
            builder.AddString(Join(root, "activeRun", "runId"), _player.activeRun.runId);
            builder.AddString(Join(root, "activeRun", "biomeId"), combat.BiomeId);
            builder.AddString(Join(root, "activeRun", "biomeTitle"), combat.BiomeTitle);
            builder.AddString(Join(root, "activeRun", "encounterKind"), combat.EncounterKind.ToString());
            builder.AddString(Join(root, "activeRun", "encounterId"), combat.EnemyId);
            builder.AddInteger(Join(root, "activeRun", "eventAttemptOrdinal"), combat.EventAttemptOrdinal);
            builder.AddString(Join(root, "activeRun", "enemyId"), combat.EnemyId);
            builder.AddInteger(Join(root, "activeRun", "enemyCurrentHp"), combat.EnemyCurrentHp);
            builder.AddInteger(Join(root, "activeRun", "enemyMaximumHp"), combat.EnemyMaximumHp);
            builder.AddInteger(Join(root, "activeRun", "enemyRemainingCooldown"), combat.EnemyRemainingCooldown);
            builder.AddInteger(Join(root, "activeRun", "enemyMaximumCooldown"), combat.EnemyMaximumCooldown);
            builder.AddInteger(Join(root, "activeRun", "playerCurrentHearts"), combat.PlayerCurrentHearts);
            builder.AddInteger(Join(root, "activeRun", "playerMaximumHearts"), combat.PlayerMaximumHearts);
            builder.AddString(Join(root, "activeRun", "phase"), combat.Phase.ToString());
            long silverEarned = _player.activeRun.silverEarned;
            long goldEarned = _player.activeRun.goldEarned;
            long diamondEarned = _player.activeRun.diamondEarned;
            if (request.SavePoint == GameplaySavePoint.AttemptResolved &&
                request.Resolution != null && request.Resolution.IsAcademic)
            {
                long delta = Math.Max(0, request.Resolution.Academic.CurrencyDelta);
                switch (request.Resolution.Academic.RankAtCommit.Tier)
                {
                    case AcademicRankTier.Gold: goldEarned = checked(goldEarned + delta); break;
                    case AcademicRankTier.Diamond: diamondEarned = checked(diamondEarned + delta); break;
                    default: silverEarned = checked(silverEarned + delta); break;
                }
            }
            builder.AddInteger(Join(root, "activeRun", "silverEarned"), silverEarned);
            builder.AddInteger(Join(root, "activeRun", "goldEarned"), goldEarned);
            builder.AddInteger(Join(root, "activeRun", "diamondEarned"), diamondEarned);
            builder.AddInteger(Join(root, "activeRun", "bonusMultiplierBasisPoints"),
                Math.Max(10000, _player.activeRun.bonusMultiplierBasisPoints));

            bool hasActiveAttempt = request.ActiveQuestion != null &&
                (request.SavePoint == GameplaySavePoint.AttemptCommitted ||
                 request.SavePoint == GameplaySavePoint.AnswerWindowOpened);
            if (hasActiveAttempt)
            {
                builder.AddString(Join(root, "activeRun", "committedAttemptId"), request.TransactionId);
                builder.AddString(Join(root, "activeRun", "questionContentKind"),
                    request.ActiveQuestion.ContentKind.ToString());
                builder.AddString(Join(root, "activeRun", "questionDocumentId"),
                    request.ActiveQuestion.SourceId);
                builder.AddInteger(Join(root, "activeRun", "questionId"), request.ActiveQuestion.Id.Value);
                if (request.ActiveQuestion.ContentKind == QuestionContentKind.RankQuestion)
                {
                    builder.AddString(Join(root, "academic", "activeAttempt", "transactionId"), request.TransactionId);
                    builder.AddString(Join(root, "academic", "activeAttempt", "rank"), request.ActiveQuestion.Rank.ToString());
                    builder.AddInteger(Join(root, "academic", "activeAttempt", "questionId"), request.ActiveQuestion.Id.Value);
                    builder.AddString(Join(root, "academic", "activeAttempt", "state"), request.SavePoint.ToString());
                }
                else builder.AddNull(Join(root, "academic", "activeAttempt"));
            }
            else
            {
                builder.AddString(Join(root, "activeRun", "committedAttemptId"), string.Empty);
                builder.AddString(Join(root, "activeRun", "questionContentKind"), string.Empty);
                builder.AddString(Join(root, "activeRun", "questionDocumentId"), string.Empty);
                builder.AddInteger(Join(root, "activeRun", "questionId"), 0);
                builder.AddNull(Join(root, "academic", "activeAttempt"));
            }
            if (request.SavePoint == GameplaySavePoint.AttemptResolved &&
                request.Resolution?.Presentation != null)
            {
                AddPendingPresentation(
                    builder, root, request.Resolution.Presentation);
            }
            else if (request.SavePoint == GameplaySavePoint.PresentationCompleted ||
                     request.SavePoint == GameplaySavePoint.AttemptCommitted)
            {
                builder.AddNull(Join(root, "activeRun", "pendingPresentation"));
            }
            AddAnalytics(builder, root, request);
            return builder.Build();
        }

        private static void AddPendingPresentation(
            FirestorePatchDocumentBuilder builder,
            string[] root,
            AttemptPresentationReceipt receipt)
        {
            string[] receiptRoot = Join(root, "activeRun", "pendingPresentation");
            builder.AddInteger(Join(receiptRoot, "version"), receipt.Version);
            builder.AddString(Join(receiptRoot, "presentationId"), receipt.PresentationId);
            builder.AddString(Join(receiptRoot, "attemptId"), receipt.AttemptId);
            builder.AddString(Join(receiptRoot, "outcome"), receipt.Outcome.ToString());
            builder.AddInteger(Join(receiptRoot, "responseScore"), receipt.ResponseScore);
            builder.AddInteger(Join(receiptRoot, "finalDamage"), receipt.FinalDamage);
            builder.AddBoolean(Join(receiptRoot, "isCritical"), receipt.IsCritical);
            builder.AddInteger(Join(receiptRoot, "resolvedEnemyHpAfter"),
                receipt.ResolvedEnemyHpAfter);
            builder.AddBoolean(Join(receiptRoot, "enemyDefeated"), receipt.EnemyDefeated);
            builder.AddBoolean(Join(receiptRoot, "enemyAttacked"), receipt.EnemyAttacked);
            builder.AddBoolean(Join(receiptRoot, "playerDefeated"), receipt.PlayerDefeated);
            builder.AddBoolean(Join(receiptRoot, "stageAdvanced"), receipt.StageAdvanced);
            builder.AddBoolean(Join(receiptRoot, "biomeChanged"), receipt.BiomeChanged);
            builder.AddString(Join(receiptRoot, "previousRank"),
                receipt.RankTransition.Previous.ToString());
            builder.AddString(Join(receiptRoot, "currentRank"),
                receipt.RankTransition.Current.ToString());
            AddPresentationSnapshot(builder, Join(receiptRoot, "source"), receipt.Source);
            AddPresentationSnapshot(builder, Join(receiptRoot, "destination"),
                receipt.Destination);
        }

        private static void AddPresentationSnapshot(
            FirestorePatchDocumentBuilder builder,
            string[] root,
            CombatPresentationSnapshot snapshot)
        {
            builder.AddInteger(Join(root, "stage"), snapshot.Stage.Value);
            builder.AddString(Join(root, "biomeId"), snapshot.BiomeId);
            builder.AddString(Join(root, "encounterId"), snapshot.EncounterId);
            builder.AddString(Join(root, "encounterKind"), snapshot.EncounterKind.ToString());
            builder.AddInteger(Join(root, "enemyCurrentHp"), snapshot.EnemyCurrentHp);
            builder.AddInteger(Join(root, "enemyMaximumHp"), snapshot.EnemyMaximumHp);
            builder.AddInteger(Join(root, "enemyRemainingCooldown"),
                snapshot.EnemyRemainingCooldown);
            builder.AddInteger(Join(root, "enemyMaximumCooldown"),
                snapshot.EnemyMaximumCooldown);
            builder.AddInteger(Join(root, "playerCurrentHearts"),
                snapshot.PlayerCurrentHearts);
            builder.AddInteger(Join(root, "playerMaximumHearts"),
                snapshot.PlayerMaximumHearts);
            builder.AddString(Join(root, "phase"), snapshot.Phase.ToString());
        }

        private void AddAnalytics(
            FirestorePatchDocumentBuilder builder,
            string[] root,
            GameplaySaveRequest request)
        {
            PlayerSnapshot.AnalyticsData analytics = _player.analytics ?? new PlayerSnapshot.AnalyticsData();
            long resolved = analytics.totalQuestionsResolved;
            long correct = analytics.totalCorrect;
            long incorrect = analytics.totalIncorrect;
            long timeout = analytics.totalTimeout;
            long abandoned = analytics.totalAbandoned;
            long scoreSum = analytics.responseScoreSum;
            long efficiencySum = analytics.responseEfficiencySum;
            long durationSum = analytics.responseDurationMillisecondsSum;
            long[] scoreHistogram = EnsureHistogram(analytics.responseScoreHistogram, 11);
            long[] efficiencyHistogram = EnsureHistogram(analytics.responseEfficiencyHistogram, 11);
            long[] durationHistogram = EnsureHistogram(analytics.responseDuration100msHistogram, 102);
            string appliedId = analytics.lastAppliedAttemptId ?? string.Empty;
            var silver = Copy(analytics.silver);
            var gold = Copy(analytics.gold);
            var diamond = Copy(analytics.diamond);
            string[] analyticsRoot = Join(root, "analytics");

            if (request.SavePoint == GameplaySavePoint.AttemptResolved &&
                request.Resolution != null &&
                request.Resolution.IsAcademic &&
                !string.Equals(appliedId, request.TransactionId, StringComparison.Ordinal))
            {
                AttemptResolution result = request.Resolution;
                bool isCorrect = result.Academic.IsCorrect;
                int score = Math.Max(0, Math.Min(10, result.Academic.ResponseScore));
                resolved++;
                if (isCorrect) correct++;
                else if (result.Academic.Outcome == QuestionOutcome.Timeout) timeout++;
                else if (result.Academic.Outcome == QuestionOutcome.Abandoned) abandoned++;
                else incorrect++;
                scoreSum += score;
                efficiencySum += isCorrect ? score * 10 : 0;
                durationSum += result.ResponseDurationMilliseconds;
                scoreHistogram[score]++;
                efficiencyHistogram[(isCorrect ? score * 10 : 0) / 10]++;
                durationHistogram[Math.Min(101, result.ResponseDurationMilliseconds / 100)]++;
                appliedId = request.TransactionId;
                PlayerSnapshot.RankAnalyticsData rank = result.Academic.RankAtCommit == AcademicRank.Gold
                    ? gold
                    : result.Academic.RankAtCommit == AcademicRank.Diamond ? diamond : silver;
                rank.resolved++;
                if (isCorrect) rank.correct++;
                rank.responseScoreSum += score;
                rank.responseEfficiencySum += isCorrect ? score * 10 : 0;
                AddQuestionAnalytics(builder, analyticsRoot, analytics, result, score, isCorrect);
            }

            builder.AddInteger(Join(analyticsRoot, "totalQuestionsResolved"), resolved);
            builder.AddInteger(Join(analyticsRoot, "totalCorrect"), correct);
            builder.AddInteger(Join(analyticsRoot, "totalIncorrect"), incorrect);
            builder.AddInteger(Join(analyticsRoot, "totalTimeout"), timeout);
            builder.AddInteger(Join(analyticsRoot, "totalAbandoned"), abandoned);
            builder.AddInteger(Join(analyticsRoot, "responseScoreSum"), scoreSum);
            builder.AddInteger(Join(analyticsRoot, "responseEfficiencySum"), efficiencySum);
            builder.AddInteger(Join(analyticsRoot, "responseDurationMillisecondsSum"), durationSum);
            builder.AddIntegerArray(Join(analyticsRoot, "responseScoreHistogram"), scoreHistogram);
            builder.AddIntegerArray(Join(analyticsRoot, "responseEfficiencyHistogram"), efficiencyHistogram);
            builder.AddIntegerArray(Join(analyticsRoot, "responseDuration100msHistogram"), durationHistogram);
            builder.AddInteger(Join(analyticsRoot, "totalPlaySeconds"), checked(
                analytics.totalPlaySeconds + (_activity == null ? 0 : _activity.PendingWholeSeconds)));
            builder.AddString(Join(analyticsRoot, "lastAppliedAttemptId"), appliedId);
            AddRankAnalytics(builder, analyticsRoot, "silver", silver);
            AddRankAnalytics(builder, analyticsRoot, "gold", gold);
            AddRankAnalytics(builder, analyticsRoot, "diamond", diamond);
        }

        private static PlayerSnapshot.RankAnalyticsData Copy(PlayerSnapshot.RankAnalyticsData source)
        {
            source = source ?? new PlayerSnapshot.RankAnalyticsData();
            return new PlayerSnapshot.RankAnalyticsData
            {
                resolved = source.resolved,
                correct = source.correct,
                responseScoreSum = source.responseScoreSum,
                responseEfficiencySum = source.responseEfficiencySum
            };
        }

        private static long[] EnsureHistogram(long[] source, int length)
        {
            var result = new long[length];
            if (source != null) Array.Copy(source, result, Math.Min(source.Length, length));
            return result;
        }

        private static void AddRankAnalytics(
            FirestorePatchDocumentBuilder builder,
            string[] root,
            string rank,
            PlayerSnapshot.RankAnalyticsData value)
        {
            string[] prefix = Join(root, "byRank", rank);
            builder.AddInteger(Join(prefix, "resolved"), value.resolved);
            builder.AddInteger(Join(prefix, "correct"), value.correct);
            builder.AddInteger(Join(prefix, "responseScoreSum"), value.responseScoreSum);
            builder.AddInteger(Join(prefix, "responseEfficiencySum"), value.responseEfficiencySum);
        }

        private static void AddQuestionAnalytics(
            FirestorePatchDocumentBuilder builder,
            string[] analyticsRoot,
            PlayerSnapshot.AnalyticsData analytics,
            AttemptResolution result,
            int score,
            bool correct)
        {
            long questionId = result.Academic.QuestionId.Value;
            PlayerSnapshot.QuestionAnalyticsData source = (analytics.byQuestion ??
                Array.Empty<PlayerSnapshot.QuestionAnalyticsData>()).FirstOrDefault(value =>
                    value != null && value.questionId == questionId);
            var value = source == null ? new PlayerSnapshot.QuestionAnalyticsData() :
                new PlayerSnapshot.QuestionAnalyticsData
                {
                    questionId = source.questionId,
                    resolved = source.resolved,
                    correct = source.correct,
                    incorrect = source.incorrect,
                    timeout = source.timeout,
                    abandoned = source.abandoned,
                    responseScoreSum = source.responseScoreSum,
                    responseDurationMillisecondsSum = source.responseDurationMillisecondsSum,
                    responseEfficiencySum = source.responseEfficiencySum
                };
            value.questionId = questionId;
            value.resolved++;
            if (correct) value.correct++;
            else if (result.Academic.Outcome == QuestionOutcome.Timeout) value.timeout++;
            else if (result.Academic.Outcome == QuestionOutcome.Abandoned) value.abandoned++;
            else value.incorrect++;
            value.responseScoreSum += score;
            value.responseDurationMillisecondsSum += result.ResponseDurationMilliseconds;
            value.responseEfficiencySum += correct ? score * 10 : 0;
            string[] prefix = Join(analyticsRoot, "byQuestion", "q" +
                questionId.ToString(CultureInfo.InvariantCulture));
            builder.AddInteger(Join(prefix, "questionId"), value.questionId);
            builder.AddInteger(Join(prefix, "resolved"), value.resolved);
            builder.AddInteger(Join(prefix, "correct"), value.correct);
            builder.AddInteger(Join(prefix, "incorrect"), value.incorrect);
            builder.AddInteger(Join(prefix, "timeout"), value.timeout);
            builder.AddInteger(Join(prefix, "abandoned"), value.abandoned);
            builder.AddInteger(Join(prefix, "responseScoreSum"), value.responseScoreSum);
            builder.AddInteger(Join(prefix, "responseDurationMillisecondsSum"),
                value.responseDurationMillisecondsSum);
            builder.AddInteger(Join(prefix, "responseEfficiencySum"), value.responseEfficiencySum);
        }

        private static void AddInventory(
            FirestorePatchDocumentBuilder builder,
            string[] root,
            string rank,
            RankQuestionInventorySnapshot value)
        {
            string[] prefix = Join(root, "academic", "inventories", rank);
            builder.AddInteger(Join(prefix, "cycle"), value.Cycle);
            builder.AddIntegerArray(Join(prefix, "pendingIds"), ToLongs(value.Pending));
            builder.AddIntegerArray(Join(prefix, "failedIds"), ToLongs(value.Failed));
            builder.AddIntegerArray(Join(prefix, "attemptedInAuditIds"), ToLongs(value.Attempted));
            builder.AddIntegerArray(Join(prefix, "clearedInCycleIds"), ToLongs(value.Cleared));
        }

        private static long[] ToLongs(IEnumerable<QuestionId> ids) =>
            ids.Select(value => value.Value).ToArray();

        private static string[] Join(IReadOnlyList<string> left, params string[] right)
        {
            var result = new string[left.Count + right.Length];
            for (int index = 0; index < left.Count; index++) result[index] = left[index];
            for (int index = 0; index < right.Length; index++) result[left.Count + index] = right[index];
            return result;
        }
    }
}
