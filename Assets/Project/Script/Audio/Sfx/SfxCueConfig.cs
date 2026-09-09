using System;
using UnityEngine;

namespace PowerMath.Audio
{
    /// <summary>
    /// Configurable sound cue supporting multi-clip variation, volume scaling, and pitch randomness.
    /// </summary>
    [Serializable]
    public sealed class SfxCueConfig
    {
        [Tooltip("One or more audio clip variations to prevent auditory fatigue.")]
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();

        [Range(0f, 1f)]
        [Tooltip("Relative volume scale for this cue.")]
        [SerializeField] private float volumeScale = 1f;

        [Tooltip("Pitch variation range (min, max). e.g., 0.96 to 1.04 provides subtle organic variation.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);

        private int _lastPlayedIndex = -1;

        public SfxCueConfig()
        {
            volumeScale = 1f;
            pitchRange = new Vector2(0.96f, 1.04f);
        }

        public SfxCueConfig(AudioClip clip, float volumeScale = 1f, float pitchVariation = 0.04f)
        {
            clips = clip != null ? new[] { clip } : Array.Empty<AudioClip>();
            this.volumeScale = Mathf.Clamp01(volumeScale);
            float variation = Mathf.Clamp(pitchVariation, 0f, 0.5f);
            pitchRange = new Vector2(1f - variation, 1f + variation);
        }

        public SfxCueConfig(AudioClip[] clips, float volumeScale = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
        {
            this.clips = clips ?? Array.Empty<AudioClip>();
            this.volumeScale = Mathf.Clamp01(volumeScale);
            pitchRange = new Vector2(pitchMin, pitchMax);
        }

        public AudioClip[] Clips => clips ?? Array.Empty<AudioClip>();
        public float VolumeScale => Mathf.Clamp01(volumeScale);
        public Vector2 PitchRange => pitchRange;

        public bool HasClip()
        {
            if (clips == null || clips.Length == 0) return false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return true;
            }
            return false;
        }

        public AudioClip PickClip(AudioClip fallback = null)
        {
            if (clips == null || clips.Length == 0) return fallback;

            if (clips.Length == 1) return clips[0] != null ? clips[0] : fallback;

            // Pick a non-null clip avoiding immediate repetition if possible
            int start = UnityEngine.Random.Range(0, clips.Length);
            for (int attempt = 0; attempt < clips.Length; attempt++)
            {
                int index = (start + attempt) % clips.Length;
                if (index != _lastPlayedIndex && clips[index] != null)
                {
                    _lastPlayedIndex = index;
                    return clips[index];
                }
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) return clips[i];
            }

            return fallback;
        }

        public float ResolvePitch()
        {
            if (pitchRange.x >= pitchRange.y) return pitchRange.x;
            return UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        }
    }

    /// <summary>
    /// Contract for entities (EnemyDefinition, EventDefinition) that can provide custom SFX overrides.
    /// </summary>
    public interface IEnemySfxProfile
    {
        SfxCueConfig AppearSfx { get; }
        SfxCueConfig HurtSfx { get; }
        SfxCueConfig AttackSfx { get; }
        SfxCueConfig DieSfx { get; }
    }

    /// <summary>
    /// Contract for challenge event entities that can provide custom SFX overrides.
    /// </summary>
    public interface IEventSfxProfile : IEnemySfxProfile
    {
        SfxCueConfig EventRiskSfx { get; }
        SfxCueConfig ChallengeSuccessSfx { get; }
        SfxCueConfig ChallengeFailSfx { get; }
    }

    /// <summary>
    /// Generic UI Animation SFX entry mapping a USS class name or style trigger to an audio cue.
    /// </summary>
    [Serializable]
    public sealed class UiAnimationSfxStyle
    {
        [Tooltip("USS class name or UI style trigger key (e.g., 'btn-primary', 'character-card', 'modal-window', 'shake-card').")]
        [SerializeField] private string styleClass = "btn-primary";

        [Tooltip("Sound played on pointer hover / focus enter.")]
        [SerializeField] private SfxCueConfig hoverSfx = new SfxCueConfig();

        [Tooltip("Sound played on pointer click / activate.")]
        [SerializeField] private SfxCueConfig clickSfx = new SfxCueConfig();

        [Tooltip("Optional custom animation trigger sound (e.g., card punch, error shake).")]
        [SerializeField] private SfxCueConfig animationSfx = new SfxCueConfig();

        public UiAnimationSfxStyle() { }

        public UiAnimationSfxStyle(string styleClass, SfxCueConfig hover, SfxCueConfig click, SfxCueConfig animation = null)
        {
            this.styleClass = styleClass;
            hoverSfx = hover ?? new SfxCueConfig();
            clickSfx = click ?? new SfxCueConfig();
            animationSfx = animation ?? new SfxCueConfig();
        }

        public string StyleClass => styleClass;
        public SfxCueConfig HoverSfx => hoverSfx;
        public SfxCueConfig ClickSfx => clickSfx;
        public SfxCueConfig AnimationSfx => animationSfx;
    }

    public enum PlayerSfxState
    {
        AttackSwing,
        AttackFail,
        Hit,
        CriticalHit,
        Hurt,
        Die
    }

    public enum EnemySfxState
    {
        Appear,
        Hurt,
        Attack,
        Die
    }

    public enum QuestionSequenceSfxState
    {
        PopUp,
        KeypadTap,
        KeypadClear,
        KeypadSubmit,
        CountdownTick,
        CountdownWarning,
        ResultSuccess,
        ResultFail,
        DamageMultiplying,
        RewardSticker
    }

    public enum BattleSfxState
    {
        BiomeTransition,
        StageAdvance,
        RunDefeat,
        RunComplete,
        ActorClick
    }

    public enum CharacterSelectionSfxState
    {
        CardHover,
        CardClick,
        CharacterAccepted,
        NameConfirmed,
        ButtonClick,
        NavigationBack
    }
}
