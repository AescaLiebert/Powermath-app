using System;
using System.Globalization;

namespace PowerMath.Gameplay.Academic
{
    public enum QuestionContentKind
    {
        RankQuestion,
        EventQuestion
    }

    public readonly struct QuestionId : IEquatable<QuestionId>
    {
        private readonly long _value;

        public QuestionId(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _value = value;
        }

        public long Value => _value;

        public bool Equals(QuestionId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is QuestionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(QuestionId left, QuestionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QuestionId left, QuestionId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct QuestionKey : IEquatable<QuestionKey>
    {
        public QuestionKey(AcademicRank rank, QuestionId id)
        {
            Rank = rank;
            Id = id;
        }

        public AcademicRank Rank { get; }
        public QuestionId Id { get; }

        public bool Equals(QuestionKey other) => Rank == other.Rank && Id == other.Id;
        public override bool Equals(object obj) => obj is QuestionKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked { return (Rank.GetHashCode() * 397) ^ Id.GetHashCode(); }
        }
        public override string ToString() => $"{Rank}:{Id}";
        public static bool operator ==(QuestionKey left, QuestionKey right) => left.Equals(right);
        public static bool operator !=(QuestionKey left, QuestionKey right) => !left.Equals(right);
    }

    public sealed class QuestionDefinition
    {
        public QuestionDefinition(
            QuestionId id,
            AcademicRank rank,
            Uri videoUri,
            int correctAnswer,
            string youtubeVideoId = "")
        {
            if (videoUri == null || !videoUri.IsAbsoluteUri)
            {
                throw new ArgumentException("An absolute video URI is required.", nameof(videoUri));
            }

            if (correctAnswer < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(correctAnswer));
            }

            Id = id;
            Rank = rank;
            Key = new QuestionKey(rank, id);
            VideoUri = videoUri;
            YouTubeVideoId = youtubeVideoId ?? string.Empty;
            CorrectAnswer = correctAnswer;
            AnswerLength = correctAnswer
                .ToString(CultureInfo.InvariantCulture)
                .Length;
        }

        public QuestionId Id { get; }
        public AcademicRank Rank { get; }
        public QuestionKey Key { get; }
        public Uri VideoUri { get; }
        public string YouTubeVideoId { get; }
        public int CorrectAnswer { get; }
        public int AnswerLength { get; }
    }

    public sealed class QuestionPresentationDescriptor
    {
        public QuestionPresentationDescriptor(
            QuestionId id,
            AcademicRank rank,
            Uri videoUri,
            string developmentPrompt,
            string youtubeVideoId = "")
            : this(id, rank, videoUri, developmentPrompt, youtubeVideoId,
                QuestionContentKind.RankQuestion, string.Empty)
        {
        }

        public QuestionPresentationDescriptor(
            QuestionId id,
            AcademicRank rank,
            Uri videoUri,
            string developmentPrompt,
            string youtubeVideoId,
            QuestionContentKind contentKind,
            string sourceId)
        {
            Id = id;
            Rank = rank;
            VideoUri = videoUri ?? throw new ArgumentNullException(nameof(videoUri));
            DevelopmentPrompt = developmentPrompt ?? string.Empty;
            YouTubeVideoId = youtubeVideoId ?? string.Empty;
            ContentKind = contentKind;
            SourceId = sourceId ?? string.Empty;
        }

        public QuestionId Id { get; }
        public AcademicRank Rank { get; }
        public Uri VideoUri { get; }
        public string DevelopmentPrompt { get; }
        public string YouTubeVideoId { get; }
        public QuestionContentKind ContentKind { get; }
        public string SourceId { get; }
    }
}
