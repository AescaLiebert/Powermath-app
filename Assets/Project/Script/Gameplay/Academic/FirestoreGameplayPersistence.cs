using System;
using System.Collections;
using System.Linq;
using PowerMath.Gameplay.Combat;
using PowerMath.PlayerData;
using UnityEngine;

namespace PowerMath.Gameplay.Academic
{
    public sealed class FirestoreGameplayPersistence : IGameplayPersistence
    {
        private readonly MonoBehaviour _host;
        private readonly FirestoreAcademicProgressionStore _store;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private Coroutine _operation;
        private int _generation;

        public FirestoreGameplayPersistence(
            MonoBehaviour host,
            FirestoreAcademicProgressionStore store,
            PlayerSnapshot player,
            FirestoreLeaderboardProjectionPublisher publisher)
        {
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _publisher = publisher;
        }

        public void Save(
            GameplaySaveRequest request,
            Action completed,
            Action<string> failed)
        {
            if (_operation != null)
            {
                failed?.Invoke("Another authoritative save is still in progress.");
                return;
            }
            if (request.SavePoint == GameplaySavePoint.PresentationCompleted &&
                !HasMatchingPendingPresentation(request.PresentationId))
            {
                failed?.Invoke(
                    "Presentation acknowledgement does not match the pending result.");
                return;
            }
            int generation = ++_generation;
            _operation = _host.StartCoroutine(SaveRoutine(generation, request, completed, failed));
        }

        public void Cancel()
        {
            _generation++;
            if (_operation != null) _host.StopCoroutine(_operation);
            _operation = null;
        }

        private IEnumerator SaveRoutine(
            int generation,
            GameplaySaveRequest request,
            Action completed,
            Action<string> failed)
        {
            bool succeeded = false;
            string failure = string.Empty;
            long revision = _player.revision + 1;
            yield return _store.Save(
                request,
                revision,
                () => succeeded = true,
                message => failure = message);

            if (generation != _generation) yield break;
            if (!succeeded)
            {
                _operation = null;
                failed?.Invoke(failure);
                yield break;
            }

            Apply(request, revision);
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            if (_publisher != null)
            {
                string projectionFailure = string.Empty;
                yield return _publisher.Publish(
                    _player,
                    () => { },
                    message => projectionFailure = message);
                if (!string.IsNullOrEmpty(projectionFailure))
                    PowerMath.Diagnostics.AppLog.Warning("Academic", projectionFailure);
            }
            _operation = null;
            completed?.Invoke();
        }

        private void Apply(GameplaySaveRequest request, long revision)
        {
            _player.revision = revision;
            _player.progression.currentStage = request.Snapshot.Combat.Stage.Value;
            _player.progression.highestStage = Math.Max(
                _player.progression.highestStage,
                request.Snapshot.Combat.Stage.Value);
            _player.progression.firstStage200ReachedAtUnixSeconds =
                _store.LastFirstStage200ReachedAtUnixSeconds;
            _player.progression.firstStage200Reached =
                _player.progression.firstStage200ReachedAtUnixSeconds > 0;
            if (request.SavePoint == GameplaySavePoint.AttemptResolved && request.Resolution != null)
                _player.progression.totalDamage = checked(
                    _player.progression.totalDamage + Math.Max(0, request.Resolution.Combat.FinalDamage));
            _player.progression.activeRank = request.Academic.ActiveRank.ToString();
            _player.wallet.silver = request.Academic.Balances.Silver;
            _player.wallet.gold = request.Academic.Balances.Gold;
            _player.wallet.diamond = request.Academic.Balances.Diamond;

            _player.activeRun = _player.activeRun ?? new PlayerSnapshot.ActiveRunData();
            CombatSnapshot combat = request.Snapshot.Combat;
            _player.activeRun.currentStage = combat.Stage.Value;
            _player.activeRun.biomeId = combat.BiomeId;
            _player.activeRun.biomeTitle = combat.BiomeTitle;
            _player.activeRun.encounterKind = combat.EncounterKind.ToString();
            _player.activeRun.encounterId = combat.EnemyId;
            _player.activeRun.eventAttemptOrdinal = combat.EventAttemptOrdinal;
            _player.activeRun.enemyId = combat.EnemyId;
            _player.activeRun.enemyCurrentHp = combat.EnemyCurrentHp;
            _player.activeRun.enemyMaximumHp = combat.EnemyMaximumHp;
            _player.activeRun.enemyRemainingCooldown = combat.EnemyRemainingCooldown;
            _player.activeRun.enemyMaximumCooldown = combat.EnemyMaximumCooldown;
            _player.activeRun.playerCurrentHearts = combat.PlayerCurrentHearts;
            _player.activeRun.playerMaximumHearts = combat.PlayerMaximumHearts;
            _player.activeRun.phase = combat.Phase.ToString();
            _player.activeRun.committedAttemptId = request.ActiveQuestion == null
                ? string.Empty
                : request.TransactionId;
            _player.activeRun.questionContentKind = request.ActiveQuestion == null
                ? string.Empty : request.ActiveQuestion.ContentKind.ToString();
            _player.activeRun.questionDocumentId = request.ActiveQuestion == null
                ? string.Empty : request.ActiveQuestion.SourceId;
            _player.activeRun.questionId = request.ActiveQuestion == null
                ? 0 : request.ActiveQuestion.Id.Value;
            if (request.SavePoint == GameplaySavePoint.AttemptResolved &&
                request.Resolution?.Presentation != null)
            {
                _player.activeRun.pendingPresentation = ToPlayer(
                    request.Resolution.Presentation);
            }
            else if (request.SavePoint == GameplaySavePoint.PresentationCompleted ||
                     request.SavePoint == GameplaySavePoint.AttemptCommitted)
            {
                _player.activeRun.pendingPresentation = null;
            }
            if (request.SavePoint == GameplaySavePoint.AttemptResolved &&
                request.Resolution != null && request.Resolution.IsAcademic)
            {
                long delta = Math.Max(0, request.Resolution.Academic.CurrencyDelta);
                switch (request.Resolution.Academic.RankAtCommit.Tier)
                {
                    case AcademicRankTier.Gold: _player.activeRun.goldEarned = checked(_player.activeRun.goldEarned + delta); break;
                    case AcademicRankTier.Diamond: _player.activeRun.diamondEarned = checked(_player.activeRun.diamondEarned + delta); break;
                    default: _player.activeRun.silverEarned = checked(_player.activeRun.silverEarned + delta); break;
                }
            }

            _player.academic = _player.academic ?? new PlayerSnapshot.AcademicData();
            _player.academic.auditScore = request.Academic.AuditScore;
            _player.academic.auditResolvedCount = request.Academic.AuditResolvedCount;
            _player.academic.silver = ToPlayer(request.Academic.Silver);
            _player.academic.gold = ToPlayer(request.Academic.Gold);
            _player.academic.diamond = ToPlayer(request.Academic.Diamond);
            PlayerAnalyticsUpdater.Apply(_player, request);
            _player.analytics = _player.analytics ?? new PlayerSnapshot.AnalyticsData();
            _player.analytics.totalPlaySeconds = _store.LastTotalPlaySeconds;
        }

        private static PlayerSnapshot.RankInventoryData ToPlayer(
            RankQuestionInventorySnapshot snapshot)
        {
            return new PlayerSnapshot.RankInventoryData
            {
                cycle = snapshot.Cycle,
                pendingIds = snapshot.Pending.Select(id => id.Value).ToArray(),
                failedIds = snapshot.Failed.Select(id => id.Value).ToArray(),
                attemptedInAuditIds = snapshot.Attempted.Select(id => id.Value).ToArray(),
                clearedInCycleIds = snapshot.Cleared.Select(id => id.Value).ToArray()
            };
        }

        private bool HasMatchingPendingPresentation(string presentationId)
        {
            return !string.IsNullOrWhiteSpace(presentationId) &&
                string.Equals(
                    _player.activeRun?.pendingPresentation?.presentationId,
                    presentationId,
                    StringComparison.Ordinal);
        }

        private static PlayerSnapshot.AttemptPresentationData ToPlayer(
            AttemptPresentationReceipt receipt)
        {
            return new PlayerSnapshot.AttemptPresentationData
            {
                version = receipt.Version,
                presentationId = receipt.PresentationId,
                attemptId = receipt.AttemptId,
                outcome = receipt.Outcome.ToString(),
                responseScore = receipt.ResponseScore,
                finalDamage = receipt.FinalDamage,
                isCritical = receipt.IsCritical,
                resolvedEnemyHpAfter = receipt.ResolvedEnemyHpAfter,
                enemyDefeated = receipt.EnemyDefeated,
                enemyAttacked = receipt.EnemyAttacked,
                playerDefeated = receipt.PlayerDefeated,
                stageAdvanced = receipt.StageAdvanced,
                biomeChanged = receipt.BiomeChanged,
                source = ToPlayer(receipt.Source),
                destination = ToPlayer(receipt.Destination),
                previousRank = receipt.RankTransition.Previous.ToString(),
                currentRank = receipt.RankTransition.Current.ToString()
            };
        }

        private static PlayerSnapshot.CombatPresentationData ToPlayer(
            CombatPresentationSnapshot snapshot)
        {
            return new PlayerSnapshot.CombatPresentationData
            {
                stage = snapshot.Stage.Value,
                biomeId = snapshot.BiomeId,
                encounterId = snapshot.EncounterId,
                encounterKind = snapshot.EncounterKind.ToString(),
                enemyCurrentHp = snapshot.EnemyCurrentHp,
                enemyMaximumHp = snapshot.EnemyMaximumHp,
                enemyRemainingCooldown = snapshot.EnemyRemainingCooldown,
                enemyMaximumCooldown = snapshot.EnemyMaximumCooldown,
                playerCurrentHearts = snapshot.PlayerCurrentHearts,
                playerMaximumHearts = snapshot.PlayerMaximumHearts,
                phase = snapshot.Phase.ToString()
            };
        }
    }
}
