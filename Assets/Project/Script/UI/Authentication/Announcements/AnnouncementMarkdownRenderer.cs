using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Authentication.Announcements
{
    public sealed class AnnouncementMarkdownRenderer
    {
        private static readonly Regex ImagePattern = new Regex(
            @"^!\[(?<alt>[^\]]*)\]\((?<source>[^\)]+)\)$",
            RegexOptions.Compiled);
        private static readonly Regex BoldPattern = new Regex(
            @"\*\*(.+?)\*\*",
            RegexOptions.Compiled);
        private static readonly Regex ItalicPattern = new Regex(
            @"(?<!\*)\*([^*]+?)\*(?!\*)",
            RegexOptions.Compiled);
        private static readonly Regex CodePattern = new Regex(
            @"`([^`]+?)`",
            RegexOptions.Compiled);

        private readonly AnnouncementMediaPresenter _media;

        public AnnouncementMarkdownRenderer(AnnouncementMediaPresenter media)
        {
            _media = media;
        }

        public void Render(VisualElement container, string markdown)
        {
            if (container == null)
            {
                return;
            }

            _media?.Clear();
            container.Clear();
            string normalized = (markdown ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            var paragraph = new StringBuilder();

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                string trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    FlushParagraph(container, paragraph);
                    continue;
                }

                Match image = ImagePattern.Match(trimmed);
                if (image.Success)
                {
                    FlushParagraph(container, paragraph);
                    AddMedia(
                        container,
                        image.Groups["source"].Value.Trim(),
                        image.Groups["alt"].Value.Trim());
                    continue;
                }

                if (trimmed == "---")
                {
                    FlushParagraph(container, paragraph);
                    container.Add(new VisualElement
                    {
                        name = "announcement-divider"
                    }.WithClass("announcement-divider"));
                    continue;
                }

                bool isHeading = trimmed.StartsWith("# ", StringComparison.Ordinal) ||
                    trimmed.StartsWith("## ", StringComparison.Ordinal) ||
                    trimmed.StartsWith("### ", StringComparison.Ordinal);
                bool isList = trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                    trimmed.StartsWith("* ", StringComparison.Ordinal);
                bool isCallout = trimmed.StartsWith("> ", StringComparison.Ordinal);
                if (isHeading || isList || isCallout)
                {
                    FlushParagraph(container, paragraph);
                    if (isHeading) TryAddHeading(container, trimmed);
                    else if (isList) TryAddListItem(container, trimmed);
                    else TryAddCallout(container, trimmed);
                    continue;
                }

                if (paragraph.Length > 0)
                {
                    paragraph.Append(' ');
                }
                paragraph.Append(trimmed);
            }

            FlushParagraph(container, paragraph);
        }

        public static string SanitizeInline(string value)
        {
            string encoded = (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
            encoded = BoldPattern.Replace(encoded, "<b>$1</b>");
            encoded = ItalicPattern.Replace(encoded, "<i>$1</i>");
            encoded = CodePattern.Replace(
                encoded,
                "<color=#8D6422>$1</color>");
            return encoded;
        }

        private static bool TryAddHeading(
            VisualElement container,
            string line)
        {
            int level = line.StartsWith("### ", StringComparison.Ordinal) ? 3 :
                line.StartsWith("## ", StringComparison.Ordinal) ? 2 :
                line.StartsWith("# ", StringComparison.Ordinal) ? 1 : 0;
            if (level == 0)
            {
                return false;
            }

            var label = CreateText(line.Substring(level + 1));
            label.AddToClassList("announcement-heading");
            label.AddToClassList("announcement-heading--" + level);
            container.Add(label);
            return true;
        }

        private static bool TryAddListItem(
            VisualElement container,
            string line)
        {
            if (!line.StartsWith("- ", StringComparison.Ordinal) &&
                !line.StartsWith("* ", StringComparison.Ordinal))
            {
                return false;
            }

            var label = CreateText("• " + line.Substring(2));
            label.AddToClassList("announcement-list-item");
            container.Add(label);
            return true;
        }

        private static bool TryAddCallout(
            VisualElement container,
            string line)
        {
            if (!line.StartsWith("> ", StringComparison.Ordinal))
            {
                return false;
            }

            var label = CreateText(line.Substring(2));
            label.AddToClassList("announcement-callout");
            container.Add(label);
            return true;
        }

        private void AddMedia(
            VisualElement container,
            string source,
            string alt)
        {
            if (_media != null && _media.TryAdd(container, source, alt))
            {
                return;
            }

            var fallback = CreateText(
                string.IsNullOrWhiteSpace(alt)
                    ? "Media unavailable"
                    : alt);
            fallback.AddToClassList("announcement-media-fallback");
            container.Add(fallback);
        }

        private static void FlushParagraph(
            VisualElement container,
            StringBuilder paragraph)
        {
            if (paragraph.Length == 0)
            {
                return;
            }

            var label = CreateText(paragraph.ToString());
            label.AddToClassList("announcement-paragraph");
            container.Add(label);
            paragraph.Clear();
        }

        private static Label CreateText(string text)
        {
            return new Label(SanitizeInline(text))
            {
                enableRichText = true,
                pickingMode = PickingMode.Ignore
            };
        }
    }

    internal static class AnnouncementVisualElementExtensions
    {
        public static T WithClass<T>(this T element, string className)
            where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}
