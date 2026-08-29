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

        [Tooltip("Critical damage also includes a text label so color is not the only signal.")]
        [SerializeField] private Color criticalColor = new Color(1f, 0.78f, 0.18f, 1f);

        [Tooltip("Base point size on the reusable TMP component.")]
        [SerializeField, Min(1f)] private float fontSize = 42f;

        [Header("Motion - Approved Starting Values")]
        [Tooltip("Pop duration. Starting value requires the FCT readability micro-test.")]
        [SerializeField, Min(0f)] private float popSeconds = 0.12f;

        [Tooltip("Readable hold duration. Starting value requires the FCT readability micro-test.")]
        [SerializeField, Min(0f)] private float holdSeconds = 0.24f;

        [Tooltip("Slide/fade duration. Starting value requires the FCT readability micro-test.")]
        [SerializeField, Min(0f)] private float exitSeconds = 0.34f;

        [Tooltip("Local upward travel during Exit.")]
        [SerializeField] private float slideDistance = 74f;

        [Tooltip("Deterministic horizontal separation for simultaneous values.")]
        [SerializeField] private float overlapOffset = 24f;

        public int PoolCapacity => Mathf.Max(1, poolCapacity);
        public TMP_FontAsset FontAsset => fontAsset;
        public Material FontMaterial => fontMaterial;
        public Color NormalColor => normalColor;
        public Color CriticalColor => criticalColor;
        public float FontSize => Mathf.Max(1f, fontSize);
        public float PopSeconds => Mathf.Max(0f, popSeconds);
        public float HoldSeconds => Mathf.Max(0f, holdSeconds);
        public float ExitSeconds => Mathf.Max(0f, exitSeconds);
        public float SlideDistance => slideDistance;
        public float OverlapOffset => overlapOffset;
    }
}
