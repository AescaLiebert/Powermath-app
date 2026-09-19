using System;
using System.Globalization;
using UnityEngine;

namespace PowerMath.UI.Authentication.Announcements
{
    public static class AnnouncementPreferenceStore
    {
        private const string DateKey =
            "powermath.announcements.suppressedLocalDate";
        private const string RevisionKey =
            "powermath.announcements.suppressedCatalogRevision";
        private const string DateFormat = "yyyy-MM-dd";

        public static bool IsSuppressedToday(
            string catalogRevision,
            DateTime localNow)
        {
            string storedDate = PlayerPrefs.GetString(DateKey, string.Empty);
            string storedRevision = PlayerPrefs.GetString(
                RevisionKey,
                string.Empty);
            return IsSuppressed(
                storedDate,
                storedRevision,
                catalogRevision,
                localNow);
        }

        public static bool IsSuppressed(
            string storedDate,
            string storedRevision,
            string catalogRevision,
            DateTime localNow)
        {
            return string.Equals(
                    storedDate,
                    localNow.ToString(DateFormat, CultureInfo.InvariantCulture),
                    StringComparison.Ordinal);
        }

        public static void SuppressToday(
            string catalogRevision,
            DateTime localNow)
        {
            PlayerPrefs.SetString(
                DateKey,
                localNow.ToString(DateFormat, CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(
                RevisionKey,
                catalogRevision ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
