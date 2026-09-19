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
        [SerializeField, Range(0.05f, 0.95f)] private float playerAttackImpactNormalized = 0.40f;
        [SerializeField, Range(0.05f, 0.95f)] private float enemyAttackImpactNormalized = 0.47f;
        [SerializeField, Min(0f)] private float failedAttackSeconds = 0.30f;
        [SerializeField, Min(0f)] private float takeDamageSeconds = 0.30f;
        [SerializeField, Min(0f)] private float walkSeconds = 0.32f;
        [SerializeField, Min(0f)] private float appearSeconds = 0.45f;
        [SerializeField, Min(0f)] private float dieSeconds = 0.90f;
        [SerializeField, Min(0f)] private float normalDeathSeconds = 0.70f;
        [SerializeField, Min(0f)] private float majorDeathSeconds = 1.60f;
        [SerializeField, Min(0f)] private float reducedNormalDeathSeconds = 0.50f;
        [SerializeField, Min(0f)] private float reducedMajorDeathSeconds = 1.00f;
        [SerializeField, Min(0f)] private float rebirthSeconds = 0.85f;
        [SerializeField, Min(0f)] private float reducedMotionSeconds = 0.12f;

        [Header("Actor Motion")]
        [SerializeField, Min(0f)] private float attackTravel = 56f;
        [SerializeField, Range(0.05f, 0.35f)] private float attackAnticipationNormalized = 0.22f;
        [SerializeField, Min(0f)] private float attackAnticipationTravel = 12f;
        [SerializeField] private Vector2 attackAnticipationScale = new Vector2(1.06f, 0.94f);
        [SerializeField] private Vector2 attackSwingScale = new Vector2(1.08f, 0.94f);
        [SerializeField] private Vector2 attackImpactScale = new Vector2(0.92f, 1.08f);
        [SerializeField, Min(0f)] private float failedAttackDrop = 12f;
        [SerializeField, Min(0f)] private float damageReactionTravel = 24f;
        [SerializeField, Min(0f)] private float criticalDamageReactionTravel = 30f;
        [SerializeField, Min(0f)] private float playerDamageReactionTravel = 20f;
        [SerializeField, Min(0f)] private float walkTravel = 18f;
        [SerializeField, Min(0f)] private float walkBounce = 5f;
        [SerializeField, Min(0f)] private float deathDrop = 72f;
        [SerializeField, Min(0f)] private float deathRotation = 18f;
        [SerializeField, Range(0f, 0.25f)] private float normalDeathDropRatio = 0.04f;
        [SerializeField, Range(0f, 0.25f)] private float majorDeathDropRatio = 0.08f;
        [SerializeField, Range(0f, 0.1f)] private float majorDeathShakeRatio = 0.015f;
        [SerializeField, Min(0f)] private float rebirthRise = 58f;

        [Header("Idle Breathing - Subtle Starting Values")]
        [SerializeField, Min(0.5f)] private float idleBreathSeconds = 3.2f;
        [SerializeField, Range(0f, 0.05f)] private float idleBreathScaleX = 0.004f;
        [SerializeField, Range(0f, 0.05f)] private float idleBreathScaleY = 0.012f;
        [SerializeField, Range(0f, 8f)] private float idleBreathRisePixels = 2f;

        [Header("Combat World Impact")]
        [SerializeField, Min(0f)] private float normalImpulseSeconds = 0.12f;
        [SerializeField, Min(0f)] private float normalImpulseAmplitude = 6f;
        [SerializeField, Min(1)] private int normalImpulseOscillations = 1;
        [SerializeField, Min(0f)] private float criticalImpulseSeconds = 0.18f;
        [SerializeField, Min(0f)] private float criticalImpulseAmplitude = 14f;
        [SerializeField, Min(1)] private int criticalImpulseOscillations = 2;

        [Header("Player Damage Impact - Starting Values")]
        [SerializeField, Min(0f)] private float playerDamageImpulseSeconds = 0.14f;
        [SerializeField, Min(0f)] private float playerDamageImpulseAmplitude = 8f;
        [SerializeField, Min(1)] private int playerDamageImpulseOscillations = 1;

        [Header("Hit Reaction & Flash")]
        [SerializeField, Min(0f)] private float normalHitStopSeconds = 0.060f;
        [SerializeField, Min(0f)] private float criticalHitStopSeconds = 0.075f;
        [SerializeField, Min(0f)] private float playerDamageHitStopSeconds = 0.060f;
        [SerializeField, Min(0f)] private float reducedMotionHitStopSeconds = 0.025f;
        [SerializeField, Min(0f)] private float normalPostHitHoldSeconds = 0.20f;
        [SerializeField, Min(0f)] private float criticalPostHitHoldSeconds = 0.35f;
        [SerializeField, Min(0f)] private float normalImpactBurstSeconds = 0.18f;
        [SerializeField, Min(0f)] private float criticalImpactBurstSeconds = 0.26f;
        [SerializeField, Min(0f)] private float playerDamageBurstSeconds = 0.20f;
        [SerializeField] private Color hitFlashRed = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color hitFlashWhite = new Color(1f, 1f, 1f, 1f);
        [SerializeField, Min(1)] private int hitFlashCycles = 1;
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
        public float PlayerAttackImpactNormalized => Mathf.Clamp(
            playerAttackImpactNormalized <= 0f ? 0.40f : playerAttackImpactNormalized,
            0.05f,
            0.95f);
        public float EnemyAttackImpactNormalized => Mathf.Clamp(
            enemyAttackImpactNormalized <= 0f ? 0.47f : enemyAttackImpactNormalized,
            0.05f,
            0.95f);
        public float FailedAttackSeconds => failedAttackSeconds;
        public float TakeDamageSeconds => takeDamageSeconds;
        public float WalkSeconds => walkSeconds;
        public float AppearSeconds => appearSeconds;
        public float DieSeconds => dieSeconds;
        public float NormalDeathSeconds => normalDeathSeconds;
        public float MajorDeathSeconds => majorDeathSeconds;
        public float ReducedNormalDeathSeconds => reducedNormalDeathSeconds;
        public float ReducedMajorDeathSeconds => reducedMajorDeathSeconds;
        public float RebirthSeconds => rebirthSeconds;
        public float ReducedMotionSeconds => reducedMotionSeconds;
        public float AttackTravel => attackTravel;
        public float AttackAnticipationNormalized => Mathf.Clamp(
            attackAnticipationNormalized,
            0.05f,
            0.35f);
        public float AttackAnticipationTravel => attackAnticipationTravel;
        public Vector2 AttackAnticipationScale => attackAnticipationScale;
        public Vector2 AttackSwingScale => attackSwingScale;
        public Vector2 AttackImpactScale => attackImpactScale;
        public float FailedAttackDrop => failedAttackDrop;
        public float DamageReactionTravel => damageReactionTravel;
        public float CriticalDamageReactionTravel => criticalDamageReactionTravel;
        public float PlayerDamageReactionTravel => playerDamageReactionTravel;
        public float WalkTravel => walkTravel;
        public float WalkBounce => walkBounce;
        public float DeathDrop => deathDrop;
        public float DeathRotation => deathRotation;
        public float NormalDeathDropRatio => normalDeathDropRatio;
        public float MajorDeathDropRatio => majorDeathDropRatio;
        public float MajorDeathShakeRatio => majorDeathShakeRatio;
        public float RebirthRise => rebirthRise;
        public float IdleBreathSeconds => Mathf.Max(0.5f, idleBreathSeconds);
        public float IdleBreathScaleX => idleBreathScaleX;
        public float IdleBreathScaleY => idleBreathScaleY;
        public float IdleBreathRisePixels => idleBreathRisePixels;
        public float NormalImpulseSeconds => normalImpulseSeconds;
        public float NormalImpulseAmplitude => normalImpulseAmplitude;
        public int NormalImpulseOscillations => Mathf.Max(1, normalImpulseOscillations);
        public float CriticalImpulseSeconds => criticalImpulseSeconds;
        public float CriticalImpulseAmplitude => criticalImpulseAmplitude;
        public int CriticalImpulseOscillations => Mathf.Max(1, criticalImpulseOscillations);
        public float PlayerDamageImpulseSeconds => playerDamageImpulseSeconds;
        public float PlayerDamageImpulseAmplitude => playerDamageImpulseAmplitude;
        public int PlayerDamageImpulseOscillations => Mathf.Max(1, playerDamageImpulseOscillations);
        public float NormalHitStopSeconds => normalHitStopSeconds;
        public float CriticalHitStopSeconds => criticalHitStopSeconds;
        public float PlayerDamageHitStopSeconds => playerDamageHitStopSeconds;
        public float ReducedMotionHitStopSeconds => reducedMotionHitStopSeconds;
        public float NormalPostHitHoldSeconds => normalPostHitHoldSeconds;
        public float CriticalPostHitHoldSeconds => criticalPostHitHoldSeconds;
        public float NormalImpactBurstSeconds => normalImpactBurstSeconds;
        public float CriticalImpactBurstSeconds => criticalImpactBurstSeconds;
        public float PlayerDamageBurstSeconds => playerDamageBurstSeconds;
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
