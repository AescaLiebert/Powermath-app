using System;
using System.Collections.Generic;

namespace PowerMath.UI.MainMenu
{
    [Flags]
    public enum InteractionScope
    {
        None = 0,
        Lobby = 1,
        Navigation = 2,
        Question = 4,
        ModalDismiss = 8,
        TerminalAction = 16,
        All = Lobby | Navigation | Question | ModalDismiss | TerminalAction
    }

    public readonly struct InteractionGateSnapshot
    {
        public InteractionGateSnapshot(InteractionScope blockedScopes, int lockCount)
        {
            BlockedScopes = blockedScopes;
            LockCount = lockCount;
        }

        public InteractionScope BlockedScopes { get; }
        public int LockCount { get; }
        public bool IsBlocked => BlockedScopes != InteractionScope.None;
    }

    public interface IInteractionLock : IDisposable
    {
        string OwnerId { get; }
        InteractionScope BlockedScopes { get; }
        bool IsReleased { get; }
    }

    public interface IMainMenuInteractionGate
    {
        event Action<InteractionGateSnapshot> Changed;
        InteractionGateSnapshot Snapshot { get; }
        bool IsAllowed(InteractionScope scope);
        IInteractionLock Acquire(string ownerId, InteractionScope blockedScopes);
        string ActiveLeasesSummary { get; }
    }

    public sealed class MainMenuInteractionGate : IMainMenuInteractionGate
    {
        private readonly Dictionary<int, Lease> _leases = new Dictionary<int, Lease>();
        private int _nextLeaseId;

        public event Action<InteractionGateSnapshot> Changed;

        public string ActiveLeasesSummary
        {
            get
            {
                if (_leases.Count == 0) return "None";
                var items = new List<string>(_leases.Count);
                foreach (KeyValuePair<int, Lease> pair in _leases)
                {
                    items.Add($"[#{pair.Key}:{pair.Value.OwnerId}|{pair.Value.BlockedScopes}]");
                }
                return string.Join(", ", items);
            }
        }

        public InteractionGateSnapshot Snapshot
        {
            get
            {
                InteractionScope blocked = InteractionScope.None;
                foreach (KeyValuePair<int, Lease> pair in _leases)
                    blocked |= pair.Value.BlockedScopes;
                return new InteractionGateSnapshot(blocked, _leases.Count);
            }
        }

        public bool IsAllowed(InteractionScope scope)
        {
            return (Snapshot.BlockedScopes & scope) == 0;
        }

        public IInteractionLock Acquire(string ownerId, InteractionScope blockedScopes)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Interaction lock owner is required.", nameof(ownerId));
            if (blockedScopes == InteractionScope.None)
                throw new ArgumentOutOfRangeException(nameof(blockedScopes));

            int leaseId = ++_nextLeaseId;
            var lease = new Lease(this, leaseId, ownerId, blockedScopes);
            _leases.Add(leaseId, lease);
            PowerMath.Diagnostics.AppLog.Warning(
                "Gate",
                $"[Gate.Acquire] id={leaseId}, owner='{ownerId}', blocked={blockedScopes}. Active: {ActiveLeasesSummary}");
            PublishChanged();
            return lease;
        }

        private void Release(int leaseId)
        {
            if (!_leases.TryGetValue(leaseId, out Lease lease)) return;
            _leases.Remove(leaseId);
            PowerMath.Diagnostics.AppLog.Warning(
                "Gate",
                $"[Gate.Release] id={leaseId}, owner='{lease?.OwnerId}'. Remaining: {ActiveLeasesSummary}");
            PublishChanged();
        }

        private void PublishChanged()
        {
            Changed?.Invoke(Snapshot);
        }

        private sealed class Lease : IInteractionLock
        {
            private MainMenuInteractionGate _owner;
            private readonly int _leaseId;

            public Lease(
                MainMenuInteractionGate owner,
                int leaseId,
                string ownerId,
                InteractionScope blockedScopes)
            {
                _owner = owner;
                _leaseId = leaseId;
                OwnerId = ownerId;
                BlockedScopes = blockedScopes;
            }

            public string OwnerId { get; }
            public InteractionScope BlockedScopes { get; }
            public bool IsReleased => _owner == null;

            public void Dispose()
            {
                MainMenuInteractionGate owner = _owner;
                if (owner == null) return;
                _owner = null;
                owner.Release(_leaseId);
            }
        }
    }
}
