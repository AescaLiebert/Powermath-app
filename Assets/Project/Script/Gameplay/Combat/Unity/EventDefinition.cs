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
        [SerializeField] private EventStageType eventStageType = EventStageType.ChallengeMonster;
        [SerializeField] private string englishName = "Challenge Monster";
        [SerializeField] private string thaiName = "มอนสเตอร์ท้าทาย";
        [TextArea, SerializeField]
        private string instructions =
            "Solve one harder question. You have one attempt; failure makes it flee.";
        [SerializeField] private Sprite eventSprite;
        [Tooltip("Optional Addressables key for remote streaming. Defaults to sprite asset name or eventId.")]
        [SerializeField] private string addressableKey;

        [Header("Audio Ownership (Optional Custom Overrides)")]
        [SerializeField] private SfxCueConfig customAppearSfx;
        [SerializeField] private SfxCueConfig customHurtSfx;
        [SerializeField] private SfxCueConfig customAttackSfx;
        [SerializeField] private SfxCueConfig customDieSfx;
        [SerializeField] private SfxCueConfig customEventRiskSfx;
        [SerializeField] private SfxCueConfig customChallengeSuccessSfx;
        [SerializeField] private SfxCueConfig customChallengeFailSfx;

        public string EventId => eventId;
        public EventStageType EventStageType => eventStageType;
        public string EnglishName => englishName;
        public string ThaiName => thaiName;
        public string Instructions => instructions;
        public Sprite EventSprite => eventSprite;
        public string AddressableKey => !string.IsNullOrWhiteSpace(addressableKey)
            ? addressableKey
            : (eventSprite != null && eventSprite.texture != null ? eventSprite.texture.name : (eventSprite != null ? eventSprite.name.Replace("_0", "") : eventId));

        public string GetDisplayName(string locale = "en") =>
            locale == "th" && !string.IsNullOrWhiteSpace(thaiName) ? thaiName : englishName;

        public SfxCueConfig AppearSfx => customAppearSfx;
        public SfxCueConfig HurtSfx => customHurtSfx;
        public SfxCueConfig AttackSfx => customAttackSfx;
        public SfxCueConfig DieSfx => customDieSfx;
        public SfxCueConfig EventRiskSfx => customEventRiskSfx;
        public SfxCueConfig ChallengeSuccessSfx => customChallengeSuccessSfx;
        public SfxCueConfig ChallengeFailSfx => customChallengeFailSfx;

        public EventData ToDomainData() => new EventData(
            eventId,
            englishName,
            eventStageType);
    }
}
