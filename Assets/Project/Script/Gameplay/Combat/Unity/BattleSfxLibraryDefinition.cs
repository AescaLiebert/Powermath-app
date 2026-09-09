using System;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "BattleSfxLibrary",
        menuName = "PowerMath/Combat/Battle SFX Library")]
    public sealed class BattleSfxLibraryDefinition : ScriptableObject
    {
        [Header("Repeated Cues")]
        [Tooltip("Three or more variants avoid repetition during long sessions.")]
        [SerializeField] private AudioClip[] swordSwings = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] hits = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] actorClicks = Array.Empty<AudioClip>();

        [Header("Outcome Cues")]
        [SerializeField] private AudioClip commit;
        [SerializeField] private AudioClip keypad;
        [SerializeField] private AudioClip success;
        [SerializeField] private AudioClip failure;
        [SerializeField] private AudioClip critical;
        [SerializeField] private AudioClip enemyAttack;
        [SerializeField] private AudioClip normalDeath;
        [SerializeField] private AudioClip majorDeath;
        [SerializeField] private AudioClip biomeTransition;

        public AudioClip Commit => commit;
        public AudioClip Keypad => keypad;
        public AudioClip Success => success;
        public AudioClip Failure => failure;
        public AudioClip Critical => critical;
        public AudioClip EnemyAttack => enemyAttack;
        public AudioClip NormalDeath => normalDeath;
        public AudioClip MajorDeath => majorDeath;
        public AudioClip BiomeTransition => biomeTransition;
        public AudioClip[] SwordSwings => swordSwings ?? Array.Empty<AudioClip>();
        public AudioClip[] Hits => hits ?? Array.Empty<AudioClip>();
        public AudioClip[] ActorClicks => actorClicks ?? Array.Empty<AudioClip>();
    }
}
