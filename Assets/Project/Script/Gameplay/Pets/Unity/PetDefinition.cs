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
        [TextArea(2, 6)] [SerializeField] private string abilityRichText = string.Empty;

        [Header("Approved Runtime Stats")]
        [Min(0)] [SerializeField] private int attackBonus;

        public string PetId => petId?.Trim() ?? string.Empty;
        public string DisplayName => displayName?.Trim() ?? string.Empty;
        public Sprite Icon => icon;
        public Sprite PreviewSprite => previewSprite != null ? previewSprite : icon;
        public string AbilityRichText => abilityRichText ?? string.Empty;
        public int AttackBonus => attackBonus;

        public bool TryBuild(out PetGachaPet pet, out string error)
        {
            pet = null;
            error = string.Empty;
            try
            {
                if (icon == null)
                    throw new InvalidOperationException("Pet definitions require an icon.");
                pet = new PetGachaPet(PetId, DisplayName, attackBonus);
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
            if (!TryBuild(out _, out string error)) Debug.LogError(error, this);
        }
#endif
    }
}
