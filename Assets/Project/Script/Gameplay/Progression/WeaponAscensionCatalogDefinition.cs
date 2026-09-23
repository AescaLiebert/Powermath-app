using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Gameplay.Progression
{
    [CreateAssetMenu(fileName = "WeaponAscensionCatalog", menuName = "PowerMath/Progression/Weapon Ascension Catalog")]
    public sealed class WeaponAscensionCatalogDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Tier
        {
            public string tierId;
            [Min(0)] public int unlockLevel;
            public string localizedDisplayNameKey;
            public string displayName = "Sword";
            public Sprite icon;
            [Tooltip("Optional Addressables key for icon streaming.")]
            public string iconAddressableKey;
            public string appearanceReference;
            public string milestoneFeedbackKey;
            [TextArea] public string childFriendlyDescription;

            public string IconAddressableKey => !string.IsNullOrWhiteSpace(iconAddressableKey)
                ? iconAddressableKey
                : (icon != null && icon.texture != null ? icon.texture.name : (icon != null ? icon.name.Replace("_0", "") : tierId));
        }

        public const int DefaultLevelsPerTier = 5;

        [SerializeField, Min(1)] private int levelsPerTier = DefaultLevelsPerTier;
        [SerializeField] private Tier[] tiers = Array.Empty<Tier>();

        public int LevelsPerTier => levelsPerTier > 0 ? levelsPerTier : DefaultLevelsPerTier;
        public IReadOnlyList<Tier> Tiers => tiers ?? Array.Empty<Tier>();
        public int MaximumLevel => (tiers != null && tiers.Length > 0) ? (tiers.Length * LevelsPerTier) : 0;

        public int GetTierIndex(int level)
        {
            if (tiers == null || tiers.Length == 0) return 0;
            return Mathf.Clamp(level / LevelsPerTier, 0, tiers.Length - 1);
        }

        public int GetSubLevelInTier(int level)
        {
            if (level <= 0) return 0;
            int max = MaximumLevel;
            if (max > 0 && level >= max) return LevelsPerTier;
            return level % LevelsPerTier;
        }

        public bool IsMilestoneAwakening(int level)
        {
            return level > 0 && (level % LevelsPerTier == 0);
        }

        public Tier Resolve(int level)
        {
            Tier result = null;
            foreach (Tier tier in tiers ?? Array.Empty<Tier>())
            {
                if (tier != null && tier.unlockLevel <= level &&
                    (result == null || tier.unlockLevel > result.unlockLevel)) result = tier;
            }
            return result;
        }

        public bool TryValidate(out string error)
        {
            error = string.Empty;
            if (tiers == null || tiers.Length == 0 || tiers[0] == null || tiers[0].unlockLevel != 0)
            {
                error = "Weapon catalog must begin with a Level 0 tier.";
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int previousLevel = -1;
            int maxLevel = MaximumLevel;
            foreach (Tier tier in tiers)
            {
                if (tier == null || string.IsNullOrWhiteSpace(tier.tierId) ||
                    !ids.Add(tier.tierId) || tier.unlockLevel <= previousLevel ||
                    tier.unlockLevel < 0 || (maxLevel > 0 && tier.unlockLevel > maxLevel) ||
                    string.IsNullOrWhiteSpace(tier.displayName) || tier.icon == null ||
                    string.IsNullOrWhiteSpace(tier.appearanceReference) ||
                    string.IsNullOrWhiteSpace(tier.milestoneFeedbackKey))
                {
                    error = "Weapon tiers require unique IDs, sorted unique levels, names, icons, appearance references, and feedback keys.";
                    return false;
                }
                previousLevel = tier.unlockLevel;
            }
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!TryValidate(out string error)) PowerMath.Diagnostics.AppLog.Warning("Progression", error, this);
        }
#endif
    }
}
