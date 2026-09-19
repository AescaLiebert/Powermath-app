using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.UI.Authentication.Announcements
{
    [Serializable]
    public sealed class AnnouncementCatalog
    {
        public int schemaVersion = 1;
        public string catalogRevision;
        public AnnouncementPatch[] patches;
    }

    [Serializable]
    public sealed class AnnouncementPatch
    {
        public string id;
        public string version;
        public string publishedAtUtc;
        public bool featured;
        public string titleEn;
        public string titleTh;
        [TextArea(4, 30)] public string bodyEn;
        [TextArea(4, 30)] public string bodyTh;

        public string GetTitle(string locale)
        {
            string localized = locale == "th" ? titleTh : titleEn;
            return string.IsNullOrWhiteSpace(localized)
                ? titleEn ?? string.Empty
                : localized;
        }

        public string GetBody(string locale)
        {
            string localized = locale == "th" ? bodyTh : bodyEn;
            return string.IsNullOrWhiteSpace(localized)
                ? bodyEn ?? string.Empty
                : localized;
        }
    }

    public static class AnnouncementCatalogLoader
    {
        private const string ResourcePath = "Announcements/Catalog";

        public static bool TryLoad(
            out AnnouncementCatalog catalog,
            out string error)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                catalog = null;
                error = "Announcement catalog was not found.";
                return false;
            }

            try
            {
                catalog = JsonUtility.FromJson<AnnouncementCatalog>(asset.text);
            }
            catch (Exception exception)
            {
                catalog = null;
                error = "Announcement catalog JSON is invalid: " +
                    exception.Message;
                return false;
            }

            return TryValidate(catalog, out error);
        }

        public static bool TryValidate(
            AnnouncementCatalog catalog,
            out string error)
        {
            if (catalog == null || catalog.schemaVersion != 1)
            {
                error = "Announcement catalog schema is unsupported.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(catalog.catalogRevision))
            {
                error = "Announcement catalog revision is required.";
                return false;
            }

            if (catalog.patches == null || catalog.patches.Length == 0)
            {
                error = "Announcement catalog must contain at least one patch.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < catalog.patches.Length; index++)
            {
                AnnouncementPatch patch = catalog.patches[index];
                if (patch == null || string.IsNullOrWhiteSpace(patch.id) ||
                    string.IsNullOrWhiteSpace(patch.version))
                {
                    error = "Every announcement patch requires an id and version.";
                    return false;
                }

                if (!ids.Add(patch.id))
                {
                    error = "Duplicate announcement patch id: " + patch.id;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(patch.titleEn) ||
                    string.IsNullOrWhiteSpace(patch.bodyEn))
                {
                    error = "Patch " + patch.id +
                        " requires English fallback content.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
