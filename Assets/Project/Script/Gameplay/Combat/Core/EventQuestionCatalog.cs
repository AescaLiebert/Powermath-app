using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct ChallengeQuestionId : IEquatable<ChallengeQuestionId>
    {
        public ChallengeQuestionId(string value)
        {
            if (!TryParse(value, out ChallengeQuestionId parsed))
                throw new ArgumentException(
                    "Challenge question ID must be c + Rank initial + positive ordinal (for example cs1).",
                    nameof(value));
            Value = parsed.Value;
            Rank = parsed.Rank;
            Ordinal = parsed.Ordinal;
        }

        private ChallengeQuestionId(string value, AcademicRank rank, int ordinal)
        {
            Value = value;
            Rank = rank;
            Ordinal = ordinal;
        }

        public string Value { get; }
        public AcademicRank Rank { get; }
        public int Ordinal { get; }

        public static ChallengeQuestionId Create(AcademicRank rank, long ordinal)
        {
            if (ordinal <= 0 || ordinal > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            char rankCode = rank.Tier == AcademicRankTier.Gold
                ? 'g'
                : rank.Tier == AcademicRankTier.Diamond ? 'd' : 's';
            return new ChallengeQuestionId(
                "c" + rankCode + ordinal.ToString(CultureInfo.InvariantCulture),
                rank,
                (int)ordinal);
        }

        public static bool TryParse(string value, out ChallengeQuestionId id)
        {
            id = default;
            string normalized = value?.Trim();
            if (string.IsNullOrEmpty(normalized) || normalized.Length < 3 || normalized[0] != 'c')
                return false;
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
                return false;

            AcademicRank rank;
            switch (normalized[1])
            {
                case 's': rank = AcademicRank.Silver; break;
                case 'g': rank = AcademicRank.Gold; break;
                case 'd': rank = AcademicRank.Diamond; break;
                default: return false;
            }

            if (!int.TryParse(normalized.Substring(2), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int ordinal) || ordinal <= 0)
                return false;

            string canonical = "c" + normalized[1] +
                ordinal.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(normalized, canonical, StringComparison.Ordinal))
                return false;

            id = new ChallengeQuestionId(normalized, rank, ordinal);
            return true;
        }

        public bool Equals(ChallengeQuestionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ChallengeQuestionId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(ChallengeQuestionId left, ChallengeQuestionId right) => left.Equals(right);
        public static bool operator !=(ChallengeQuestionId left, ChallengeQuestionId right) => !left.Equals(right);
    }

    public sealed class ChallengeQuestionDefinition
    {
        public ChallengeQuestionDefinition(ChallengeQuestionId id, Uri videoUri,
            int correctAnswer, string youtubeVideoId = "")
        {
            if (videoUri == null || !videoUri.IsAbsoluteUri)
                throw new ArgumentException("An absolute video URI is required.", nameof(videoUri));
            if (correctAnswer < 0) throw new ArgumentOutOfRangeException(nameof(correctAnswer));
            Id = id;
            VideoUri = videoUri;
            CorrectAnswer = correctAnswer;
            YouTubeVideoId = youtubeVideoId ?? string.Empty;
            AnswerLength = correctAnswer.ToString(CultureInfo.InvariantCulture).Length;
        }

        public ChallengeQuestionId Id { get; }
        public AcademicRank Rank => Id.Rank;
        public QuestionId SequenceId => new QuestionId(Id.Ordinal);
        public Uri VideoUri { get; }
        public string YouTubeVideoId { get; }
        public int CorrectAnswer { get; }
        public int AnswerLength { get; }
    }

    public readonly struct ChallengeQuestionSequenceSnapshot
    {
        public ChallengeQuestionSequenceSnapshot(int silverCursor, int goldCursor,
            int diamondCursor, string reservedDocumentId = "", string reservedQuestionId = "")
        {
            if (silverCursor < 0 || goldCursor < 0 || diamondCursor < 0)
                throw new ArgumentOutOfRangeException(nameof(silverCursor));
            SilverCursor = silverCursor;
            GoldCursor = goldCursor;
            DiamondCursor = diamondCursor;
            ReservedDocumentId = reservedDocumentId ?? string.Empty;
            ReservedQuestionId = reservedQuestionId ?? string.Empty;
        }

        public int SilverCursor { get; }
        public int GoldCursor { get; }
        public int DiamondCursor { get; }
        public string ReservedDocumentId { get; }
        public string ReservedQuestionId { get; }

        public int GetCursor(AcademicRank rank) => rank.Tier == AcademicRankTier.Gold
            ? GoldCursor
            : rank.Tier == AcademicRankTier.Diamond ? DiamondCursor : SilverCursor;
    }

    public sealed class EventQuestionCatalog
    {
        public const string DefaultDocumentId = "challenge";
        private readonly Dictionary<string, Dictionary<AcademicRankTier, ChallengeQuestionDefinition[]>> _documents;

        public EventQuestionCatalog(
            IReadOnlyDictionary<string, IReadOnlyList<ChallengeQuestionDefinition>> documents)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            _documents = new Dictionary<string, Dictionary<AcademicRankTier, ChallengeQuestionDefinition[]>>(
                StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyList<ChallengeQuestionDefinition>> pair in documents)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    throw new ArgumentException("Each Event question document needs an ID and questions.");
                var byRank = new Dictionary<AcademicRankTier, ChallengeQuestionDefinition[]>();
                foreach (AcademicRankTier tier in Enum.GetValues(typeof(AcademicRankTier)))
                {
                    ChallengeQuestionDefinition[] questions = pair.Value
                        .Where(value => value != null && value.Rank.Tier == tier)
                        .OrderBy(value => value.Id.Ordinal).ToArray();
                    if (questions.Length == 0 ||
                        questions.Select(value => value.Id).Distinct().Count() != questions.Length)
                        throw new ArgumentException(
                            $"Event question document '{pair.Key}' needs unique questions for Rank {tier}.");
                    byRank.Add(tier, questions);
                }
                _documents.Add(pair.Key.Trim(), byRank);
            }
        }

        public EventQuestionCatalog(
            IReadOnlyDictionary<string, IReadOnlyList<QuestionDefinition>> legacyDocuments)
            : this(ConvertLegacy(legacyDocuments))
        {
        }

        public ChallengeQuestionDefinition Select(string documentId, AcademicRank rank, int cursor)
        {
            if (!_documents.TryGetValue(documentId ?? string.Empty, out var byRank) ||
                !byRank.TryGetValue(rank.Tier, out ChallengeQuestionDefinition[] questions))
                throw new InvalidOperationException(
                    $"Event question document '{documentId}' has no {rank} questions.");
            return questions[Math.Max(0, cursor) % questions.Length];
        }

        public bool TryGet(string documentId, ChallengeQuestionId id,
            out ChallengeQuestionDefinition definition)
        {
            definition = null;
            return _documents.TryGetValue(documentId ?? string.Empty, out var byRank) &&
                byRank.TryGetValue(id.Rank.Tier, out ChallengeQuestionDefinition[] questions) &&
                (definition = questions.FirstOrDefault(value => value.Id == id)) != null;
        }

        public int GetCount(string documentId, AcademicRank rank)
        {
            if (!_documents.TryGetValue(documentId ?? string.Empty, out var byRank) ||
                !byRank.TryGetValue(rank.Tier, out ChallengeQuestionDefinition[] questions))
                return 0;
            return questions.Length;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<ChallengeQuestionDefinition>> ConvertLegacy(
            IReadOnlyDictionary<string, IReadOnlyList<QuestionDefinition>> documents)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));
            var result = new Dictionary<string, IReadOnlyList<ChallengeQuestionDefinition>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IReadOnlyList<QuestionDefinition>> pair in documents)
            {
                result.Add(pair.Key, (pair.Value ?? Array.Empty<QuestionDefinition>())
                    .Where(value => value != null)
                    .Select(value => new ChallengeQuestionDefinition(
                        ChallengeQuestionId.Create(value.Rank, value.Id.Value),
                        value.VideoUri,
                        value.CorrectAnswer,
                        value.YouTubeVideoId))
                    .ToArray());
            }
            return result;
        }
    }

    public sealed class ChallengeQuestionSequence
    {
        private readonly EventQuestionCatalog _catalog;
        private int _silverCursor;
        private int _goldCursor;
        private int _diamondCursor;
        private string _reservedDocumentId = string.Empty;
        private ChallengeQuestionId _reservedQuestionId;

        public ChallengeQuestionSequence(EventQuestionCatalog catalog,
            ChallengeQuestionSequenceSnapshot snapshot = default)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _silverCursor = snapshot.SilverCursor;
            _goldCursor = snapshot.GoldCursor;
            _diamondCursor = snapshot.DiamondCursor;
            if (!string.IsNullOrWhiteSpace(snapshot.ReservedQuestionId))
            {
                if (!ChallengeQuestionId.TryParse(snapshot.ReservedQuestionId, out _reservedQuestionId) ||
                    string.IsNullOrWhiteSpace(snapshot.ReservedDocumentId) ||
                    !_catalog.TryGet(snapshot.ReservedDocumentId, _reservedQuestionId, out _))
                    throw new ArgumentException("Saved Challenge question reservation is invalid.", nameof(snapshot));
                _reservedDocumentId = snapshot.ReservedDocumentId;
            }
        }

        public ChallengeQuestionDefinition Reserve(string documentId, AcademicRank rank)
        {
            if (!string.IsNullOrEmpty(_reservedDocumentId))
            {
                if (_reservedQuestionId.Rank != rank ||
                    !string.Equals(_reservedDocumentId, documentId, StringComparison.Ordinal) ||
                    !_catalog.TryGet(documentId, _reservedQuestionId, out ChallengeQuestionDefinition reserved))
                    throw new InvalidOperationException("A different Challenge question is already reserved.");
                return reserved;
            }

            ChallengeQuestionDefinition question = _catalog.Select(documentId, rank, GetCursor(rank));
            _reservedDocumentId = documentId;
            _reservedQuestionId = question.Id;
            return question;
        }

        public void Resolve(ChallengeQuestionId id)
        {
            EnsureReserved(id);
            AcademicRank rank = id.Rank;
            int count = _catalog.GetCount(_reservedDocumentId, rank);
            SetCursor(rank, (GetCursor(rank) + 1) % count);
            ClearReservation();
        }

        public void Void(ChallengeQuestionId id)
        {
            EnsureReserved(id);
            ClearReservation();
        }

        public ChallengeQuestionSequenceSnapshot Export() =>
            new ChallengeQuestionSequenceSnapshot(
                _silverCursor,
                _goldCursor,
                _diamondCursor,
                _reservedDocumentId,
                string.IsNullOrEmpty(_reservedDocumentId) ? string.Empty : _reservedQuestionId.Value);

        private int GetCursor(AcademicRank rank) => rank.Tier == AcademicRankTier.Gold
            ? _goldCursor
            : rank.Tier == AcademicRankTier.Diamond ? _diamondCursor : _silverCursor;

        private void SetCursor(AcademicRank rank, int value)
        {
            if (rank.Tier == AcademicRankTier.Gold) _goldCursor = value;
            else if (rank.Tier == AcademicRankTier.Diamond) _diamondCursor = value;
            else _silverCursor = value;
        }

        private void EnsureReserved(ChallengeQuestionId id)
        {
            if (string.IsNullOrEmpty(_reservedDocumentId) || _reservedQuestionId != id)
                throw new InvalidOperationException("Challenge question reservation does not match the active attempt.");
        }

        private void ClearReservation()
        {
            _reservedDocumentId = string.Empty;
            _reservedQuestionId = default;
        }
    }

    public static class ChallengeRewardPolicy
    {
        public const int MinimumPowerCoins = 10;
        public const int MaximumPowerCoins = 200;

        private static readonly int[] BaseByBiome = { 0, 10, 15, 25, 35, 50, 75, 100 };

        public static int GetBaseReward(int biomeIndex)
        {
            int clamped = Math.Max(1, Math.Min(7, biomeIndex));
            return BaseByBiome[clamped];
        }

        public static int Calculate(QuestionOutcome outcome, int responseScore, int biomeIndex = 1)
        {
            int baseReward = GetBaseReward(biomeIndex);
            if (outcome != QuestionOutcome.Correct)
                return baseReward;
            int score = Math.Max(0, Math.Min(10, responseScore));
            return baseReward + (baseReward * score) / 10;
        }
    }
}
