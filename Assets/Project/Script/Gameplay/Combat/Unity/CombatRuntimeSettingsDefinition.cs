using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "CombatRuntimeSettings",
        menuName = "PowerMath/Combat/Runtime Settings")]
    public sealed class CombatRuntimeSettingsDefinition : ScriptableObject
    {
        [Header("Combat Defaults")]
        [Range(0f, 1f)]
        [SerializeField] private float criticalRate = 0.2f;
        [Min(0f)]
        [SerializeField] private float criticalDamagePercent = 50f;

        [Header("Player")]
        [Min(0)]
        [SerializeField] private int baseWeaponAttack = 5;
        [Min(1)]
        [SerializeField] private int maximumHearts = 3;

        [Header("Answer Window")]
        [Min(1)]
        [SerializeField] private int maximumAnswerLength = 6;
        [Min(0f)]
        [SerializeField] private float preparationSeconds = 1f;
        [Min(0.1f)]
        [SerializeField] private float answerSeconds = 10f;

        [Header("Accessibility")]
        [SerializeField] private bool reducedMotion;

        public double CriticalRate => criticalRate;
        public double CriticalDamagePercent => criticalDamagePercent;
        public int MaximumHearts => maximumHearts;
        public int BaseWeaponAttack => baseWeaponAttack;
        public int MaximumAnswerLength => maximumAnswerLength;
        public double PreparationSeconds => preparationSeconds;
        public double AnswerSeconds => answerSeconds;
        public bool ReducedMotion => reducedMotion;

    }
}
