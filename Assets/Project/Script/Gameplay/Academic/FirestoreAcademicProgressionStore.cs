using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerMath.Gameplay.Combat;
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
        private int _highestStage;

        public FirestoreAcademicProgressionStore(
            GameApiSettings settings,
            string levelDocumentId,
            string username,
            int highestStage)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _levelDocumentId = levelDocumentId ?? string.Empty;
            _username = username ?? string.Empty;
            _highestStage = Math.Max(1, highestStage);
        }

        public IEnumerator Save(
            GameplaySaveRequest request,
            long nextRevision,
            Action completed,
            Action<string> failed)
        {
            if (request.Academic == null) throw new ArgumentException("Academic state is required.", nameof(request));
            if (!_settings.TryGetLevelDocumentById(_levelDocumentId, out string url))
            {
                failed?.Invoke("Player progression document is not configured.");
                yield break;
            }

            string updateTime;
            using (UnityWebRequest get = UnityWebRequest.Get(url))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(get.downloadHandler.text, out JsonValue root, out _) ||
                    !root.TryGet("updateTime", out JsonValue updateValue) ||
                    updateValue.Kind != JsonValueKind.String)
                {
                    failed?.Invoke("Could not refresh player progression before saving.");
                    yield break;
                }
                updateTime = updateValue.Text;
            }

            FirestorePatchPlan plan = BuildPlan(request, nextRevision);
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
            completed?.Invoke();
        }

        private FirestorePatchPlan BuildPlan(GameplaySaveRequest request, long revision)
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
            builder.AddInteger(Join(root, "wallet", "silver"), snapshot.Balances.Silver);
            builder.AddInteger(Join(root, "wallet", "gold"), snapshot.Balances.Gold);
            builder.AddInteger(Join(root, "wallet", "diamond"), snapshot.Balances.Diamond);
            builder.AddInteger(Join(root, "academic", "auditScore"), snapshot.AuditScore);
            builder.AddInteger(Join(root, "academic", "auditResolvedCount"), snapshot.AuditResolvedCount);
            AddInventory(builder, root, "silver", snapshot.Silver);
            AddInventory(builder, root, "gold", snapshot.Gold);
            AddInventory(builder, root, "diamond", snapshot.Diamond);
            builder.AddInteger(Join(root, "activeRun", "currentStage"), combat.Stage.Value);
            builder.AddString(Join(root, "activeRun", "enemyId"), combat.EnemyId);
            builder.AddInteger(Join(root, "activeRun", "enemyCurrentHp"), combat.EnemyCurrentHp);
            builder.AddInteger(Join(root, "activeRun", "enemyMaximumHp"), combat.EnemyMaximumHp);
            builder.AddInteger(Join(root, "activeRun", "enemyRemainingCooldown"), combat.EnemyRemainingCooldown);
            builder.AddInteger(Join(root, "activeRun", "enemyMaximumCooldown"), combat.EnemyMaximumCooldown);
            builder.AddInteger(Join(root, "activeRun", "playerCurrentHearts"), combat.PlayerCurrentHearts);
            builder.AddInteger(Join(root, "activeRun", "playerMaximumHearts"), combat.PlayerMaximumHearts);
            builder.AddString(Join(root, "activeRun", "phase"), combat.Phase.ToString());

            bool hasActiveAttempt = request.ActiveQuestion != null &&
                (request.SavePoint == GameplaySavePoint.AttemptCommitted ||
                 request.SavePoint == GameplaySavePoint.AnswerWindowOpened);
            if (hasActiveAttempt)
            {
                builder.AddString(Join(root, "activeRun", "committedAttemptId"), request.TransactionId);
                builder.AddString(Join(root, "academic", "activeAttempt", "transactionId"), request.TransactionId);
                builder.AddString(Join(root, "academic", "activeAttempt", "rank"), request.ActiveQuestion.Rank.ToString());
                builder.AddInteger(Join(root, "academic", "activeAttempt", "questionId"), request.ActiveQuestion.Id.Value);
                builder.AddString(Join(root, "academic", "activeAttempt", "state"), request.SavePoint.ToString());
            }
            else
            {
                builder.AddString(Join(root, "activeRun", "committedAttemptId"), string.Empty);
                builder.AddNull(Join(root, "academic", "activeAttempt"));
            }
            return builder.Build();
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
