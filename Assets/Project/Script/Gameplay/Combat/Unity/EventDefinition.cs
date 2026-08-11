using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "EventDefinition",
        menuName = "PowerMath/Stage Map/Event Definition")]
    public sealed class EventDefinition : ScriptableObject
    {
        [SerializeField] private string eventId = "challenge-monster";
        [SerializeField] private string fallbackTitle = "Challenge Monster";
        [TextArea, SerializeField]
        private string instructions =
            "Solve one harder question. Wrong answers cost one heart.";
        [SerializeField] private Sprite eventSprite;
        [SerializeField] private string questionDocumentId = "challenge";
        [SerializeField] private string handlerKey = "challenge-monster";

        public string EventId => eventId;
        public string FallbackTitle => fallbackTitle;
        public string Instructions => instructions;
        public Sprite EventSprite => eventSprite;
        public string QuestionDocumentId => questionDocumentId;

        public EventData ToDomainData() => new EventData(
            eventId,
            fallbackTitle,
            questionDocumentId,
            handlerKey);
    }
}
