using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "CombatJuiceProfile",
        menuName = "PowerMath/Combat/Combat Juice Profile")]
    public sealed class CombatJuiceProfileDefinition : ScriptableObject
    {
        [Header("Actor Durations - Approved Starting Values")]
        [SerializeField, Min(0f)] private float playerAttackSeconds = 0.55f;
        [SerializeField, Min(0f)] private float enemyAttackSeconds = 0.60f;
        [SerializeField, Min(0f)] private float failedAttackSeconds = 0.30f;
        [SerializeField, Min(0f)] private float takeDamageSeconds = 0.30f;
        [SerializeField, Min(0f)] private float walkSeconds = 0.32f;
        [SerializeField, Min(0f)] private float appearSeconds = 0.45f;
        [SerializeField, Min(0f)] private float dieSeconds = 0.90f;
        [SerializeField, Min(0f)] private float rebirthSeconds = 0.85f;
        [SerializeField, Min(0f)] private float reducedMotionSeconds = 0.12f;

        [Header("Actor Motion")]
        [SerializeField, Min(0f)] private float attackTravel = 56f;
        [SerializeField, Min(0f)] private float failedAttackDrop = 12f;
        [SerializeField, Min(0f)] private float damageReactionTravel = 12f;
        [SerializeField, Min(0f)] private float walkTravel = 18f;
        [SerializeField, Min(0f)] private float walkBounce = 5f;
        [SerializeField, Min(0f)] private float deathDrop = 72f;
        [SerializeField, Min(0f)] private float deathRotation = 18f;
        [SerializeField, Min(0f)] private float rebirthRise = 58f;

        [Header("Critical Impact")]
        [SerializeField, Min(0f)] private float criticalImpulseSeconds = 0.18f;
        [SerializeField, Min(0f)] private float criticalImpulseAmplitude = 14f;
        [SerializeField, Min(1)] private int criticalImpulseOscillations = 2;

        [Header("Hit Reaction & Flash")]
        [SerializeField] private Color hitFlashRed = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color hitFlashWhite = new Color(1f, 1f, 1f, 1f);
        [SerializeField, Min(1)] private int hitFlashCycles = 3;
        [SerializeField, Min(0f)] private float hitFlashIntensity = 1f;

        [Header("Die State & Reward Magnet Feedback")]
        [SerializeField, Range(0f, 1f)] private float dieHoldThreshold = 0.40f;
        [SerializeField, Range(0f, 1f)] private float dieWhiteFlashThreshold = 0.55f;
        [SerializeField, Range(0f, 1f)] private float dieDisappearThreshold = 0.75f;
        [SerializeField, Min(0.01f)] private float rewardPopSeconds = 0.30f;
        [SerializeField, Min(0.01f)] private float rewardMagnetFlightSeconds = 0.48f;
        [SerializeField, Min(0.001f)] private float rewardStaggerSeconds = 0.06f;
        [SerializeField, Min(1f)] private float rewardTargetBounceScale = 1.35f;
        [SerializeField, Min(0.01f)] private float rewardTargetBounceSeconds = 0.22f;

        public float PlayerAttackSeconds => playerAttackSeconds;
        public float EnemyAttackSeconds => enemyAttackSeconds;
        public float FailedAttackSeconds => failedAttackSeconds;
        public float TakeDamageSeconds => takeDamageSeconds;
        public float WalkSeconds => walkSeconds;
        public float AppearSeconds => appearSeconds;
        public float DieSeconds => dieSeconds;
        public float RebirthSeconds => rebirthSeconds;
        public float ReducedMotionSeconds => reducedMotionSeconds;
        public float AttackTravel => attackTravel;
        public float FailedAttackDrop => failedAttackDrop;
        public float DamageReactionTravel => damageReactionTravel;
        public float WalkTravel => walkTravel;
        public float WalkBounce => walkBounce;
        public float DeathDrop => deathDrop;
        public float DeathRotation => deathRotation;
        public float RebirthRise => rebirthRise;
        public float CriticalImpulseSeconds => criticalImpulseSeconds;
        public float CriticalImpulseAmplitude => criticalImpulseAmplitude;
        public int CriticalImpulseOscillations => Mathf.Max(1, criticalImpulseOscillations);
        public Color HitFlashRed => hitFlashRed;
        public Color HitFlashWhite => hitFlashWhite;
        public int HitFlashCycles => Mathf.Max(1, hitFlashCycles);
        public float HitFlashIntensity => hitFlashIntensity;
        public float DieHoldThreshold => dieHoldThreshold;
        public float DieWhiteFlashThreshold => dieWhiteFlashThreshold;
        public float DieDisappearThreshold => dieDisappearThreshold;
        public float RewardPopSeconds => rewardPopSeconds;
        public float RewardMagnetFlightSeconds => rewardMagnetFlightSeconds;
        public float RewardStaggerSeconds => rewardStaggerSeconds;
        public float RewardTargetBounceScale => rewardTargetBounceScale;
        public float RewardTargetBounceSeconds => rewardTargetBounceSeconds;
    }
}
