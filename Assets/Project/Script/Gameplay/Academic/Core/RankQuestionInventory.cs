using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerMath.Gameplay.Academic
{
    public sealed class RankQuestionInventory
    {
        private readonly List<QuestionId> _canonical;
        private readonly List<QuestionId> _pending;
        private readonly List<QuestionId> _failedThisAudit;
        private readonly HashSet<QuestionId> _attemptedThisAudit;
        private readonly HashSet<QuestionId> _clearedThisCycle;
        private QuestionId _reserved;

        public RankQuestionInventory(
            AcademicRank rank,
            IEnumerable<QuestionId> canonicalOrder)
        {
            Rank = rank;
            _canonical = canonicalOrder?.ToList() ??
                throw new ArgumentNullException(nameof(canonicalOrder));
            if (_canonical.Count < AuditWindow.RequiredResults ||
                _canonical.Distinct().Count() != _canonical.Count)
            {
                throw new ArgumentException(
                    "A Rank inventory requires at least five distinct questions.",
                    nameof(canonicalOrder)
                );
            }

            _pending = new List<QuestionId>(_canonical);
            _failedThisAudit = new List<QuestionId>();
            _attemptedThisAudit = new HashSet<QuestionId>();
            _clearedThisCycle = new HashSet<QuestionId>();
        }

        public RankQuestionInventory(
            AcademicRank rank,
            IEnumerable<QuestionId> canonicalOrder,
            RankQuestionInventorySnapshot snapshot)
            : this(rank, canonicalOrder)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            ValidateSnapshotIds(snapshot.Pending, nameof(snapshot.Pending));
            ValidateSnapshotIds(snapshot.Failed, nameof(snapshot.Failed));
            ValidateSnapshotIds(snapshot.Attempted, nameof(snapshot.Attempted));
            ValidateSnapshotIds(snapshot.Cleared, nameof(snapshot.Cleared));

            CycleNumber = snapshot.Cycle;
            _pending.Clear();
            _pending.AddRange(snapshot.Pending);
            _failedThisAudit.AddRange(snapshot.Failed);
            _attemptedThisAudit.UnionWith(snapshot.Attempted);
            _clearedThisCycle.UnionWith(snapshot.Cleared);
            if (snapshot.Reserved.HasValue)
            {
                ValidateSnapshotIds(new[] { snapshot.Reserved.Value }, nameof(snapshot.Reserved));
                if (_pending.Contains(snapshot.Reserved.Value))
                    throw new ArgumentException("A reserved question cannot also be pending.");
                _reserved = snapshot.Reserved.Value;
                HasReservation = true;
            }

            // A newly initialized account stores an empty queue. Seed it from
            // the sorted shared catalog without treating that as a new cycle.
            if (_pending.Count == 0 && _failedThisAudit.Count == 0 &&
                _attemptedThisAudit.Count == 0 && _clearedThisCycle.Count == 0)
                _pending.AddRange(_canonical);
        }

        private RankQuestionInventory(RankQuestionInventory source)
        {
            Rank = source.Rank;
            _canonical = new List<QuestionId>(source._canonical);
            _pending = new List<QuestionId>(source._pending);
            _failedThisAudit = new List<QuestionId>(source._failedThisAudit);
            _attemptedThisAudit = new HashSet<QuestionId>(source._attemptedThisAudit);
            _clearedThisCycle = new HashSet<QuestionId>(source._clearedThisCycle);
            _reserved = source._reserved;
            HasReservation = source.HasReservation;
            CycleNumber = source.CycleNumber;
        }

        public AcademicRank Rank { get; }
        public bool HasReservation { get; private set; }
        public int CycleNumber { get; private set; }
        public int PendingCount => _pending.Count;
        public int FailedCount => _failedThisAudit.Count;
        public QuestionId ReservedQuestionId => HasReservation ? _reserved : default;
        public IReadOnlyList<QuestionId> PendingQuestions => _pending.ToArray();

        public RankQuestionInventory Clone()
        {
            return new RankQuestionInventory(this);
        }

        public RankQuestionInventorySnapshot Export()
        {
            return new RankQuestionInventorySnapshot(
                CycleNumber,
                _pending.ToArray(),
                _failedThisAudit.ToArray(),
                _attemptedThisAudit.ToArray(),
                _clearedThisCycle.ToArray(),
                HasReservation ? _reserved : (QuestionId?)null
            );
        }

        public bool TryReserve(out QuestionId questionId)
        {
            questionId = default;
            if (HasReservation)
            {
                return false;
            }

            int candidateIndex = FindCandidateIndex();
            if (candidateIndex < 0 && _pending.Count == 0)
            {
                BeginNextCycle();
                candidateIndex = FindCandidateIndex();
            }

            if (candidateIndex < 0)
            {
                return false;
            }

            _reserved = _pending[candidateIndex];
            _pending.RemoveAt(candidateIndex);
            HasReservation = true;
            questionId = _reserved;
            return true;
        }

        public void ResolveReserved(bool correct)
        {
            EnsureReservation();
            _attemptedThisAudit.Add(_reserved);
            if (correct)
            {
                _clearedThisCycle.Add(_reserved);
            }
            else
            {
                _failedThisAudit.Add(_reserved);
            }

            ClearReservation();
        }

        public void VoidReserved()
        {
            EnsureReservation();
            _pending.Insert(0, _reserved);
            ClearReservation();
        }

        public void CompleteAudit()
        {
            if (HasReservation)
            {
                throw new InvalidOperationException(
                    "An audit cannot complete with a reserved question."
                );
            }

            if (_failedThisAudit.Count > 0)
            {
                _pending.InsertRange(0, _failedThisAudit);
            }

            _failedThisAudit.Clear();
            _attemptedThisAudit.Clear();
        }

        private int FindCandidateIndex()
        {
            for (int index = 0; index < _pending.Count; index++)
            {
                if (!_attemptedThisAudit.Contains(_pending[index]))
                {
                    return index;
                }
            }

            return -1;
        }

        private void BeginNextCycle()
        {
            CycleNumber++;
            _clearedThisCycle.Clear();
            _pending.AddRange(_canonical);
        }

        private void EnsureReservation()
        {
            if (!HasReservation)
            {
                throw new InvalidOperationException("No question is reserved.");
            }
        }

        private void ClearReservation()
        {
            _reserved = default;
            HasReservation = false;
        }

        private void ValidateSnapshotIds(IEnumerable<QuestionId> ids, string field)
        {
            QuestionId[] values = ids?.ToArray() ?? Array.Empty<QuestionId>();
            if (values.Distinct().Count() != values.Length ||
                values.Any(id => !_canonical.Contains(id)))
                throw new ArgumentException("Persisted question IDs do not match the active Rank catalog.", field);
        }
    }

    public sealed class RankQuestionInventorySet
    {
        private readonly Dictionary<AcademicRankTier, RankQuestionInventory> _inventories;

        public RankQuestionInventorySet(QuestionCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _inventories = new Dictionary<AcademicRankTier, RankQuestionInventory>();
            foreach (AcademicRankTier tier in Enum.GetValues(typeof(AcademicRankTier)))
            {
                var rank = new AcademicRank(tier);
                _inventories.Add(
                    tier,
                    new RankQuestionInventory(
                        rank,
                        catalog.GetRankQuestions(rank).Select(item => item.Id)
                    )
                );
            }
        }

        public RankQuestionInventorySet(
            QuestionCatalog catalog,
            AcademicPersistenceSnapshot snapshot)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            _inventories = new Dictionary<AcademicRankTier, RankQuestionInventory>();
            foreach (AcademicRankTier tier in Enum.GetValues(typeof(AcademicRankTier)))
            {
                var rank = new AcademicRank(tier);
                _inventories.Add(
                    tier,
                    new RankQuestionInventory(
                        rank,
                        catalog.GetRankQuestions(rank).Select(item => item.Id),
                        snapshot.Get(rank)
                    )
                );
            }
        }

        private RankQuestionInventorySet(RankQuestionInventorySet source)
        {
            _inventories = source._inventories.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone()
            );
        }

        public RankQuestionInventory Get(AcademicRank rank)
        {
            return _inventories[rank.Tier];
        }

        public RankQuestionInventorySet Clone()
        {
            return new RankQuestionInventorySet(this);
        }
    }
}
