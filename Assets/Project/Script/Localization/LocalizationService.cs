using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PowerMath.Localization
{
    public static class LocalizationService
    {
        [Serializable] public sealed class Entry { public string key; public string en; public string th; }
        [Serializable] public sealed class Catalog { public Entry[] entries; }
        private const string PreferenceKey = "powermath.preferences.locale";
        private static Dictionary<string, Entry> _entries;
        private static string _locale;
        public static event Action Changed;
        public static string Locale => _locale ??= Normalize(PlayerPrefs.GetString(PreferenceKey, "th"));
        public static string Normalize(string locale) => locale == "en" ? "en" : "th";

        public static void SetLocale(string locale)
        {
            locale = Normalize(locale);
            bool changed = Locale != locale;
            _locale = locale;
            PlayerPrefs.SetString(PreferenceKey, locale);
            PlayerPrefs.Save();
            if (changed) Changed?.Invoke();
        }

        public static string Get(string key, params object[] args)
        {
            if (_entries == null)
            {
                _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
                var asset = Resources.Load<TextAsset>("Localization/UI");
                var catalog = asset == null ? null : JsonUtility.FromJson<Catalog>(asset.text);
                foreach (var entry in catalog?.entries ?? Array.Empty<Entry>())
                    if (!_entries.TryAdd(entry.key, entry)) PowerMath.Diagnostics.AppLog.Error("Localization", "Duplicate localization key: " + entry.key);
            }
            if (!_entries.TryGetValue(key, out var value)) return "[" + key + "]";
            string text = Locale == "th" && !string.IsNullOrEmpty(value.th) ? value.th : value.en;
            return args.Length == 0 ? text : string.Format(CultureInfo.InvariantCulture, text, args);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _entries = null; _locale = null; Changed = null; }
    }
}
