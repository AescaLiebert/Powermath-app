using UnityEngine;

namespace PowerMath.UI.Core
{
    [CreateAssetMenu(
        fileName = "UiMotionProfile",
        menuName = "PowerMath/UI/Motion Profile")]
    public sealed class UiMotionProfileDefinition : ScriptableObject
    {
        [Header("Lifecycle Starting Values")]
        [Tooltip("Starting value. Validate with repeated panel navigation.")]
        [SerializeField, Min(0f)] private float enterSeconds = 0.22f;
        [Tooltip("Starting value. Close should acknowledge without blocking navigation.")]
        [SerializeField, Min(0f)] private float exitSeconds = 0.16f;
        [SerializeField, Range(0.5f, 1f)] private float enterScale = 0.90f;
        [SerializeField] private float enterOffsetY = 12f;
        [SerializeField, Range(0.5f, 1f)] private float exitScale = 0.96f;
        [SerializeField] private float exitOffsetY = -6f;

        [Header("Interaction Starting Values")]
        [SerializeField, Min(0f)] private float hoverSeconds = 0.16f;
        [SerializeField, Min(0f)] private float pressSeconds = 0.08f;
        [SerializeField, Range(1f, 1.2f)] private float hoverScale = 1.035f;
        [SerializeField] private float hoverOffsetY = -2f;
        [SerializeField, Range(0.5f, 1f)] private float pressScale = 0.94f;
        [SerializeField] private float pressOffsetY = 2f;

        [Header("Ambient and Accessibility")]
        [SerializeField, Min(0.2f)] private float idleSeconds = 1.60f;
        [SerializeField, Min(0f)] private float reducedCrossfadeSeconds = 0.12f;

        public float EnterSeconds => enterSeconds;
        public float ExitSeconds => exitSeconds;
        public float EnterScale => enterScale;
        public float EnterOffsetY => enterOffsetY;
        public float ExitScale => exitScale;
        public float ExitOffsetY => exitOffsetY;
        public float HoverSeconds => hoverSeconds;
        public float PressSeconds => pressSeconds;
        public float HoverScale => hoverScale;
        public float HoverOffsetY => hoverOffsetY;
        public float PressScale => pressScale;
        public float PressOffsetY => pressOffsetY;
        public float IdleSeconds => idleSeconds;
        public float ReducedCrossfadeSeconds => reducedCrossfadeSeconds;
    }
}
