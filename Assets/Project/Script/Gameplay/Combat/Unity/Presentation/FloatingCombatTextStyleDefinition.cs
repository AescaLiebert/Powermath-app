using UnityEngine;
using TMPro;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "FloatingCombatTextStyle",
        menuName = "PowerMath/Combat/Floating Combat Text Style")]
    public sealed class FloatingCombatTextStyleDefinition : ScriptableObject
    {
        [Header("Pool")]
        [Tooltip("Starting capacity; tune after profiling simultaneous presentation actions.")]
        [SerializeField, Min(1)] private int poolCapacity = 8;

        [Header("Text")]
        [Tooltip("Optional TMP font asset. Leave empty to use the project TMP default.")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Tooltip("Optional shared TMP material authored for outline/shadow treatment.")]
        [SerializeField] private Material fontMaterial;

        [Tooltip("Normal accepted-damage color.")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Critical damage color.")]
        [SerializeField] private Color criticalColor = new Color(1f, 0.78f, 0.18f, 1f);

        [Header("Icons")]
        [Tooltip("Optional sprite icon displayed next to critical combat text.")]
        [SerializeField] private Sprite critIcon;

        [Tooltip("Base point size on the reusable TMP component.")]
        [SerializeField, Min(1f)] private float fontSize = 42f;

        [Header("Motion - Approved Starting Values")]
        [Tooltip("Pop duration. Starting value requires the FCT readability micro-test.")]
        [SerializeField, Min(0f)] private float popSeconds = 0.10f;

        [Tooltip("Deterministic horizontal separation for simultaneous values.")]
        [SerializeField] private float overlapOffset = 24f;

        [Header("Juice & Pop-Drop-Fade Dynamics")]
        [Tooltip("Burst arc peak height above spawn origin.")]
        [SerializeField, Min(0f)] private float burstHeight = 62f;

        [Tooltip("Editable vertical distance the damage text falls from its burst apex.")]
        [SerializeField, Min(0f)] private float dropDistance = 88f;

        [Tooltip("Duration of the gravity drop and bounce.")]
        [SerializeField, Min(0f)] private float dropSeconds = 0.32f;

        [Tooltip("Brief contact hold before the ballistic launch begins.")]
        [SerializeField, Min(0f)] private float popHoldSeconds = 0.06f;

        [Tooltip("Normalized point in the flight when late fade begins.")]
        [SerializeField, Range(0.5f, 1f)] private float fadeStartNormalized = 0.85f;

        [Tooltip("Editable horizontal travel during the falling trajectory.")]
        [SerializeField, Min(0f)] private float fallHorizontalDistance = 180f;

        [Tooltip("Distance beyond the overlay bottom before the text is pooled.")]
        [SerializeField, Min(0f)] private float offscreenPadding = 72f;

        [Tooltip("Initial rotational kick in degrees on explosive pop.")]
        [SerializeField, Range(0f, 25f)] private float rotationalKick = 10f;

        [Tooltip("Duration of pure white impact flash at spawn.")]
        [SerializeField, Min(0f)] private float flashSeconds = 0.07f;

        [Tooltip("Squash & stretch exaggeration factor (1.0 = standard, > 1.0 = more juicy).")]
        [SerializeField, Range(0.5f, 2f)] private float squashStretchFactor = 1.35f;

        [Tooltip("Random horizontal scatter offset applied on spawn.")]
        [SerializeField, Min(0f)] private float spawnJitterX = 26f;

        [Tooltip("Random vertical scatter offset applied on spawn.")]
        [SerializeField, Min(0f)] private float spawnJitterY = 16f;

        public int PoolCapacity => Mathf.Max(1, poolCapacity);
        public TMP_FontAsset FontAsset => fontAsset;
        public Material FontMaterial => fontMaterial;
        public Color NormalColor => normalColor;
        public Color CriticalColor => criticalColor;
        public Sprite CritIcon => critIcon;
        public float FontSize => Mathf.Max(1f, fontSize);
        public float PopSeconds => Mathf.Max(0f, popSeconds);
        public float OverlapOffset => overlapOffset;

        public float BurstHeight => Mathf.Max(0f, burstHeight);
        public float DropDistance => Mathf.Max(0f, dropDistance);
        public float DropSeconds => Mathf.Max(0f, dropSeconds);
        public float PopHoldSeconds => Mathf.Max(0f, popHoldSeconds);
        public float FadeStartNormalized => Mathf.Clamp(fadeStartNormalized, 0.5f, 1f);
        public float FallHorizontalDistance => Mathf.Max(0f, fallHorizontalDistance);
        public float OffscreenPadding => Mathf.Max(0f, offscreenPadding);
        public float RotationalKick => rotationalKick;
        public float FlashSeconds => Mathf.Max(0f, flashSeconds);
        public float SquashStretchFactor => Mathf.Max(0.1f, squashStretchFactor);
        public float SpawnJitterX => Mathf.Max(0f, spawnJitterX);
        public float SpawnJitterY => Mathf.Max(0f, spawnJitterY);
    }
}
