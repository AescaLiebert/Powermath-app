using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "CombatRuntimeSettings",
        menuName = "PowerMath/Combat/Runtime Settings")]
    public sealed class CombatRuntimeSettingsDefinition : ScriptableObject
    {
        [Header("Development Simulation")]
        [SerializeField] private int randomSeed = 1337;
        [Range(0f, 1f)]
        [SerializeField] private float criticalRate = 0.2f;
        [Min(0f)]
        [SerializeField] private float criticalDamagePercent = 50f;

        [Header("Player")]
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

        public int RandomSeed => randomSeed;
        public double CriticalRate => criticalRate;
        public double CriticalDamagePercent => criticalDamagePercent;
        public int MaximumHearts => maximumHearts;
        public int MaximumAnswerLength => maximumAnswerLength;
        public double PreparationSeconds => preparationSeconds;
        public double AnswerSeconds => answerSeconds;
        public bool ReducedMotion => reducedMotion;

#if UNITY_EDITOR
        public void ConfigurePrototype(int seed)
        {
            randomSeed = seed;
            criticalRate = 0.2f;
            criticalDamagePercent = 50f;
            maximumHearts = 3;
            maximumAnswerLength = 6;
            preparationSeconds = 1f;
            answerSeconds = 10f;
            reducedMotion = false;
        }
#endif
    }
}
