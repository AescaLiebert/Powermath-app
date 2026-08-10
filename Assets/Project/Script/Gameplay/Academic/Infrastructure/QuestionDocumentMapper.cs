using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Academic.Infrastructure
{
    public sealed class QuestionDocumentMapper
    {
        public bool TryMap(
            QuestionDocumentDto document,
            AcademicRank rank,
            out QuestionDefinition definition,
            out string error)
        {
            definition = null;
            error = string.Empty;
            if (document == null)
            {
                error = "Question document cannot be null.";
                return false;
            }

            if (document.id < 0)
            {
                error = "Question id cannot be negative.";
                return false;
            }

            if (document.answer < 0 || document.answer > int.MaxValue)
            {
                error = $"Question '{document.id}' has an unsupported answer.";
                return false;
            }

            if (!YouTubeVideoAddress.TryParse(
                document.video_link,
                out Uri uri,
                out string videoId))
            {
                error = $"Question '{document.id}' requires a supported HTTPS YouTube video_link.";
                return false;
            }

            definition = new QuestionDefinition(
                new QuestionId(document.id),
                rank,
                uri,
                (int)document.answer,
                videoId
            );
            return true;
        }

        public QuestionCatalogLoadResult MapCatalog(
            IEnumerable<RankedQuestionDocument> documents)
        {
            if (documents == null)
            {
                return QuestionCatalogLoadResult.Failure("Question documents are required.");
            }

            var definitions = new List<QuestionDefinition>();
            var errors = new List<string>();
            foreach (RankedQuestionDocument ranked in documents)
            {
                string error = string.Empty;
                if (ranked != null && TryMap(
                    ranked.Document,
                    ranked.Rank,
                    out QuestionDefinition definition,
                    out error))
                {
                    definitions.Add(definition);
                }
                else
                {
                    errors.Add(ranked == null ? "Ranked question cannot be null." : error);
                }
            }

            if (errors.Count > 0)
                return QuestionCatalogLoadResult.Failure(errors.ToArray());

            QuestionCatalogBuildResult build = QuestionCatalog.TryCreate(definitions);
            return build.IsSuccess
                ? QuestionCatalogLoadResult.Success(build.Catalog)
                : QuestionCatalogLoadResult.Failure(new List<string>(build.Errors).ToArray());
        }
    }

    public sealed class RankedQuestionDocument
    {
        public RankedQuestionDocument(AcademicRank rank, QuestionDocumentDto document)
        {
            Rank = rank;
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public AcademicRank Rank { get; }
        public QuestionDocumentDto Document { get; }
    }

    public static class YouTubeVideoAddress
    {
        public static bool TryParse(string value, out Uri uri, out string videoId)
        {
            uri = null;
            videoId = string.Empty;
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri candidate) ||
                !string.Equals(candidate.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;

            string host = candidate.Host.ToLowerInvariant();
            if (host == "youtu.be")
            {
                videoId = FirstPathSegment(candidate.AbsolutePath);
            }
            else if (host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal))
            {
                string path = candidate.AbsolutePath.Trim('/');
                if (string.Equals(path, "watch", StringComparison.OrdinalIgnoreCase))
                    videoId = ReadQueryValue(candidate.Query, "v");
                else if (path.StartsWith("embed/", StringComparison.OrdinalIgnoreCase) ||
                         path.StartsWith("shorts/", StringComparison.OrdinalIgnoreCase))
                    videoId = FirstPathSegment(path.Substring(path.IndexOf('/') + 1));
            }

            if (!IsValidVideoId(videoId))
            {
                videoId = string.Empty;
                return false;
            }

            uri = candidate;
            return true;
        }

        private static string FirstPathSegment(string path)
        {
            string value = (path ?? string.Empty).Trim('/');
            int slash = value.IndexOf('/');
            return slash < 0 ? value : value.Substring(0, slash);
        }

        private static string ReadQueryValue(string query, string expectedName)
        {
            foreach (string pair in (query ?? string.Empty).TrimStart('?').Split('&'))
            {
                int equals = pair.IndexOf('=');
                string name = equals < 0 ? pair : pair.Substring(0, equals);
                if (string.Equals(Uri.UnescapeDataString(name), expectedName, StringComparison.Ordinal))
                    return Uri.UnescapeDataString(equals < 0 ? string.Empty : pair.Substring(equals + 1));
            }
            return string.Empty;
        }

        private static bool IsValidVideoId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64) return false;
            foreach (char character in value)
            {
                if (!char.IsLetterOrDigit(character) && character != '-' && character != '_')
                    return false;
            }
            return true;
        }
    }
}
