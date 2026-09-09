using PowerMath.Audio;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "EventDefinition",
        menuName = "PowerMath/Stage Map/Event Definition")]
    public sealed class EventDefinition : ScriptableObject, IEventSfxProfile
    {
        [SerializeField] private string eventId = "challenge-monster";
        [SerializeField] private string fallbackTitle = "Challenge Monster";
        [TextArea, SerializeField]
        private string instructions =
            "Solve one harder question. Wrong answers cost one heart.";
        [SerializeField] private Sprite eventSprite;
        [SerializeField] private string questionDocumentId = "challenge";
        [SerializeField] private string handlerKey = "challenge-monster";

        [Header("Audio Ownership (Optional Custom Overrides)")]
        [SerializeField] private SfxCueConfig customAppearSfx;
        [SerializeField] private SfxCueConfig customHurtSfx;
        [SerializeField] private SfxCueConfig customAttackSfx;
        [SerializeField] private SfxCueConfig customDieSfx;
        [SerializeField] private SfxCueConfig customEventRiskSfx;
        [SerializeField] private SfxCueConfig customChallengeSuccessSfx;
        [SerializeField] private SfxCueConfig customChallengeFailSfx;

        public string EventId => eventId;
        public string FallbackTitle => fallbackTitle;
        public string Instructions => instructions;
        public Sprite EventSprite => eventSprite;
        public string QuestionDocumentId => questionDocumentId;

        public SfxCueConfig AppearSfx => customAppearSfx;
        public SfxCueConfig HurtSfx => customHurtSfx;
        public SfxCueConfig AttackSfx => customAttackSfx;
        public SfxCueConfig DieSfx => customDieSfx;
        public SfxCueConfig EventRiskSfx => customEventRiskSfx;
        public SfxCueConfig ChallengeSuccessSfx => customChallengeSuccessSfx;
        public SfxCueConfig ChallengeFailSfx => customChallengeFailSfx;

        public EventData ToDomainData() => new EventData(
            eventId,
            fallbackTitle,
            questionDocumentId,
            handlerKey);
    }
}
