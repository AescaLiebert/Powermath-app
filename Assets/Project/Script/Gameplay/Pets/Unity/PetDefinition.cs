using System;
using UnityEngine;

namespace PowerMath.Gameplay.Pets
{
    [CreateAssetMenu(fileName = "Pet", menuName = "PowerMath/Pets/Pet Definition")]
    public sealed class PetDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string petId = string.Empty;
        [SerializeField] private string displayName = string.Empty;

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite previewSprite;
        [Tooltip("Optional Addressables key for icon streaming.")]
        [SerializeField] private string iconAddressableKey;
        [Tooltip("Optional Addressables key for preview sprite streaming.")]
        [SerializeField] private string previewAddressableKey;
        [TextArea(2, 6)] [SerializeField] private string abilityRichText = string.Empty;

        [Header("Approved Runtime Stats")]
        [Tooltip("Flat ATK added to player before multipliers (e.g. ButterflySpirit +3, Jellumi +3, Furbo +7).")]
        [Min(0)] [SerializeField] private int playerAttackBonus;
        [Tooltip("Percentage multiplier on Player ATK (e.g. Auregriff +30%).")]
        [Min(0f)] [SerializeField] private float playerAttackMultiplierPercent;
        [Tooltip("Flat ATK dealt by pet (e.g. Little Cozy +5, Sapphire +30).")]
        [Min(0)] [SerializeField] private int petAttackBonus;
        [Tooltip("Percentage multiplier on Pet ATK (e.g. Cozy +5%).")]
        [Min(0f)] [SerializeField] private float petAttackMultiplierPercent;
        [Tooltip("Critical Rate percentage bonus (e.g. GoldenCrane +10%).")]
        [Min(0f)] [SerializeField] private float critRatePercent;
        [Tooltip("Critical Damage percentage bonus (e.g. Twili +10%).")]
        [Min(0f)] [SerializeField] private float critDamagePercent;
        [Tooltip("Encounter Luck percentage bonus (e.g. Capybara +25%).")]
        [Min(0f)] [SerializeField] private float encounterLuckPercent;
        [Tooltip("Power Coin run settlement bonus percentage (e.g. Mizu +1%, Trippi Troppi +5%, Lunamoth +10%).")]
        [Min(0f)] [SerializeField] private float powerCoinBonusPercent;
        [Tooltip("Maximum player hearts addition (e.g. Lumirin +1).")]
        [Min(0)] [SerializeField] private int playerHeartUnit;

        [Header("SSR Passive")]
        [SerializeField] private PetPassiveEffectType passiveType = PetPassiveEffectType.None;
        [SerializeField] private PetPassiveStackRule passiveStackRule =
            PetPassiveStackRule.UniquePerDefinition;
        [Min(0f)] [SerializeField] private float passiveMagnitude = 1f;
        [Min(1)] [SerializeField] private int passiveTriggerCount = 1;
        [Min(1)] [SerializeField] private int passiveMaximumStacks = 1;
        [SerializeField] private PetPassiveResetScope passiveResetScope =
            PetPassiveResetScope.Never;
        [SerializeField] private PetEncounterFilter passiveEncounterFilter =
            PetEncounterFilter.Any;
        [TextArea(2, 4)] [SerializeField] private string passiveDescription = string.Empty;

        public string PetId => petId?.Trim() ?? string.Empty;
        public string DisplayName => displayName?.Trim() ?? string.Empty;
        public Sprite Icon => icon;
        public Sprite PreviewSprite => previewSprite != null ? previewSprite : icon;
        public string IconAddressableKey => !string.IsNullOrWhiteSpace(iconAddressableKey)
            ? iconAddressableKey
            : (icon != null && icon.texture != null ? icon.texture.name : (icon != null ? icon.name.Replace("_0", "") : petId));
        public string PreviewAddressableKey => !string.IsNullOrWhiteSpace(previewAddressableKey)
            ? previewAddressableKey
            : (previewSprite != null && previewSprite.texture != null ? previewSprite.texture.name : (previewSprite != null ? previewSprite.name.Replace("_0", "") : IconAddressableKey));
        public string AbilityRichText => abilityRichText ?? string.Empty;

        public int PlayerAttackBonus => playerAttackBonus;
        public float PlayerAttackMultiplierPercent => playerAttackMultiplierPercent;
        public int PetAttackBonus => petAttackBonus;
        public float PetAttackMultiplierPercent => petAttackMultiplierPercent;
        public float CritRatePercent => critRatePercent;
        public float CritDamagePercent => critDamagePercent;
        public float EncounterLuckPercent => encounterLuckPercent;
        public float PowerCoinBonusPercent => powerCoinBonusPercent;
        public int PlayerHeartUnit => playerHeartUnit;
        public PetPassiveEffectType PassiveType => passiveType;
        public PetPassiveStackRule PassiveStackRule => passiveStackRule;
        public float PassiveMagnitude => passiveMagnitude;
        public int PassiveTriggerCount => passiveTriggerCount;
        public int PassiveMaximumStacks => passiveMaximumStacks;
        public PetPassiveResetScope PassiveResetScope => passiveResetScope;
        public PetEncounterFilter PassiveEncounterFilter => passiveEncounterFilter;
        public string PassiveDescription => passiveDescription ?? string.Empty;

        /// <summary>Backwards-compatible: Total flat ATK contribution.</summary>
        public int AttackBonus => playerAttackBonus > 0 ? playerAttackBonus : petAttackBonus;
        /// <summary>Backwards-compatible: ATK multiplier.</summary>
        public float AttackMultiplierPercent => playerAttackMultiplierPercent > 0f ? playerAttackMultiplierPercent : petAttackMultiplierPercent;
        /// <summary>Backwards-compatible: Basis points from EncounterLuckPercent.</summary>
        public int EventEncounterChanceBonusBasisPoints => (int)Math.Round(encounterLuckPercent * 100f);

        public bool TryBuild(out PetGachaPet pet, out string error)
        {
            pet = null;
            error = string.Empty;
            try
            {
                if (icon == null)
                    throw new InvalidOperationException("Pet definitions require an icon.");
                PetPassiveDefinition passive = passiveType == PetPassiveEffectType.None
                    ? null
                    : new PetPassiveDefinition(
                        PetId + ":" + passiveType,
                        passiveType,
                        passiveMagnitude,
                        passiveTriggerCount,
                        passiveStackRule,
                        passiveMaximumStacks,
                        passiveResetScope,
                        passiveEncounterFilter,
                        passiveDescription);
                pet = new PetGachaPet(
                    PetId,
                    DisplayName,
                    playerAttackBonus,
                    playerAttackMultiplierPercent,
                    petAttackBonus,
                    petAttackMultiplierPercent,
                    critRatePercent,
                    critDamagePercent,
                    encounterLuckPercent,
                    powerCoinBonusPercent,
                    playerHeartUnit,
                    passive);
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is OverflowException)
            {
                error = exception.Message;
                return false;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!TryBuild(out _, out string error)) PowerMath.Diagnostics.AppLog.Error("Pets", error, this);
        }
#endif
    }
}
