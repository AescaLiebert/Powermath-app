using UnityEngine;

namespace PowerMath.UI.MainMenu
{
    [CreateAssetMenu(
        fileName = "PlayerHubJuiceProfile",
        menuName = "PowerMath/UI/Player Hub Juice Profile")]
    public sealed class PlayerHubJuiceProfileDefinition : ScriptableObject
    {
        [SerializeField, Min(0.05f)] private float pressSeconds = 0.12f;
        [SerializeField, Min(0.05f)] private float successSeconds = 0.34f;
        [SerializeField, Min(0.05f)] private float failureSeconds = 0.32f;
        [SerializeField, Min(0.2f)] private float idleSeconds = 1.6f;
        [SerializeField, Range(4, 24)] private int particleCount = 12;
        [SerializeField, Min(0.2f)] private float milestoneSeconds = 1.3f;
        [SerializeField] private AudioClip upgradeSuccessClip;
        [SerializeField] private AudioClip milestoneClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip petEquipClip;

        public float PressSeconds => pressSeconds;
        public float SuccessSeconds => successSeconds;
        public float FailureSeconds => failureSeconds;
        public float IdleSeconds => idleSeconds;
        public int ParticleCount => particleCount;
        public float MilestoneSeconds => milestoneSeconds;
        public AudioClip UpgradeSuccessClip => upgradeSuccessClip;
        public AudioClip MilestoneClip => milestoneClip;
        public AudioClip FailureClip => failureClip;
        public AudioClip PetEquipClip => petEquipClip;
    }
}
