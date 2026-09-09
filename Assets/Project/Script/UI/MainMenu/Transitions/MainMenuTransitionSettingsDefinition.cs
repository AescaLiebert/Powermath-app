using UnityEngine;

namespace PowerMath.UI.MainMenu
{
    [CreateAssetMenu(
        fileName = "MainMenuTransitionSettings",
        menuName = "PowerMath/UI/Main Menu Transition Settings")]
    public sealed class MainMenuTransitionSettingsDefinition : ScriptableObject
    {
        [Header("Bootstrap")]
        [Min(0f)] [SerializeField] private float initialSettleSeconds = 0.05f;
        [Min(0f)] [SerializeField] private float titleEntrySeconds = 0.25f;
        [Min(0f)] [SerializeField] private float titleHoldSeconds = 0.80f;
        [Min(0f)] [SerializeField] private float titleExitSeconds = 0.30f;
        [Min(0f)] [SerializeField] private float sceneRevealSeconds = 0.35f;
        [Min(0f)] [SerializeField] private float characterEntrySeconds = 0.45f;
        [Min(0f)] [SerializeField] private float characterStaggerSeconds = 0.06f;

        [Header("Session Return")]
        [Tooltip("Shared duration for Top HUD, Player Menu, and Dashboard.")]
        [Min(0f)] [SerializeField] private float sessionEntrySeconds = 0.28f;

        [Header("Canvas Travel")]
        [Min(0f)] [SerializeField] private float playerTravelPixels = 260f;
        [Min(0f)] [SerializeField] private float enemyTravelPixels = 260f;
        [SerializeField] private AnimationCurve characterEase =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Reduced Motion")]
        [Min(0f)] [SerializeField] private float reducedCrossfadeSeconds = 0.20f;

        [Header("Optional Audio")]
        [SerializeField] private AudioClip battleStartImpact;
        [SerializeField] private AudioClip characterWhoosh;
        [SerializeField] private AudioClip sessionComplete;

        public float InitialSettleSeconds => initialSettleSeconds;
        public float TitleEntrySeconds => titleEntrySeconds;
        public float TitleHoldSeconds => titleHoldSeconds;
        public float TitleExitSeconds => titleExitSeconds;
        public float SceneRevealSeconds => sceneRevealSeconds;
        public float CharacterEntrySeconds => characterEntrySeconds;
        public float CharacterStaggerSeconds => characterStaggerSeconds;
        public float SessionEntrySeconds => sessionEntrySeconds;
        public float PlayerTravelPixels => playerTravelPixels;
        public float EnemyTravelPixels => enemyTravelPixels;
        public AnimationCurve CharacterEase => characterEase;
        public float ReducedCrossfadeSeconds => reducedCrossfadeSeconds;
        public AudioClip BattleStartImpact => battleStartImpact;
        public AudioClip CharacterWhoosh => characterWhoosh;
        public AudioClip SessionComplete => sessionComplete;
    }
}
