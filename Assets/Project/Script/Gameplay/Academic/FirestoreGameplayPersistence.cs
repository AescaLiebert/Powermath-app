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
        private Coroutine _operation;
        private int _generation;

        public FirestoreGameplayPersistence(
            MonoBehaviour host,
            FirestoreAcademicProgressionStore store,
            PlayerSnapshot player)
        {
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _player = player ?? throw new ArgumentNullException(nameof(player));
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
            _operation = null;
            if (!succeeded)
            {
                failed?.Invoke(failure);
                yield break;
            }

            Apply(request, revision);
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            completed?.Invoke();
        }

        private void Apply(GameplaySaveRequest request, long revision)
        {
            _player.revision = revision;
            _player.progression.currentStage = request.Snapshot.Combat.Stage.Value;
            _player.progression.highestStage = Math.Max(
                _player.progression.highestStage,
                request.Snapshot.Combat.Stage.Value);
            _player.progression.activeRank = request.Academic.ActiveRank.ToString();
            _player.wallet.silver = request.Academic.Balances.Silver;
            _player.wallet.gold = request.Academic.Balances.Gold;
            _player.wallet.diamond = request.Academic.Balances.Diamond;

            _player.activeRun = _player.activeRun ?? new PlayerSnapshot.ActiveRunData();
            CombatSnapshot combat = request.Snapshot.Combat;
            _player.activeRun.currentStage = combat.Stage.Value;
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

            _player.academic = _player.academic ?? new PlayerSnapshot.AcademicData();
            _player.academic.auditScore = request.Academic.AuditScore;
            _player.academic.auditResolvedCount = request.Academic.AuditResolvedCount;
            _player.academic.silver = ToPlayer(request.Academic.Silver);
            _player.academic.gold = ToPlayer(request.Academic.Gold);
            _player.academic.diamond = ToPlayer(request.Academic.Diamond);
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
    }
}
