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
            public string appearanceReference;
            public string milestoneFeedbackKey;
            [TextArea] public string childFriendlyDescription;
        }

        [SerializeField] private Tier[] tiers = Array.Empty<Tier>();

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
            foreach (Tier tier in tiers)
            {
                if (tier == null || string.IsNullOrWhiteSpace(tier.tierId) ||
                    !ids.Add(tier.tierId) || tier.unlockLevel <= previousLevel ||
                    tier.unlockLevel < 0 || tier.unlockLevel > WeaponAscensionPolicy.MaximumLevel ||
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
