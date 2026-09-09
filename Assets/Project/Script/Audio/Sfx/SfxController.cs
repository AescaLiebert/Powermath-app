using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Audio
{
    /// <summary>
    /// Core global SFX controller managing dynamic AudioSource voice pooling,
    /// volume levels, and typed playback methods for Player, Enemy, Question Sequence, Battle, and UI styles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxController : MonoBehaviour, ISfxController
    {
        private static SfxController _instance;

        public static SfxController Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnsureInstance();
                }
                return _instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private SfxLibraryDefinition library;
        [SerializeField] private int initialVoiceCount = 12;
        [SerializeField] private int maximumVoiceCount = 24;

        [Header("Runtime State")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private bool isMuted = false;

        private readonly List<AudioSource> _voicePool = new List<AudioSource>();
        private int _nextVoiceIndex = 0;

        public SfxLibraryDefinition Library => library;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        public bool IsMuted
        {
            get => isMuted;
            set => isMuted = value;
        }

        public static SfxController EnsureInstance()
        {
            if (_instance != null) return _instance;

            _instance = FindAnyObjectByType<SfxController>();
            if (_instance != null) return _instance;

            GameObject go = new GameObject("GameSfxController");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SfxController>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureLibrary();
            InitializeVoices();
        }

        private void EnsureLibrary()
        {
            if (library == null)
            {
                library = Resources.Load<SfxLibraryDefinition>("SfxLibrary");
            }
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SfxLibraryDefinition>();
            }
        }

        private void InitializeVoices()
        {
            if (_voicePool.Count > 0) return;

            for (int i = 0; i < initialVoiceCount; i++)
            {
                CreateVoice();
            }
        }

        private AudioSource CreateVoice()
        {
            GameObject voiceGo = new GameObject($"SfxVoice_{_voicePool.Count}");
            voiceGo.transform.SetParent(transform, false);
            AudioSource source = voiceGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D Flat UI / Combat
            source.loop = false;
            _voicePool.Add(source);
            return source;
        }

        private AudioSource GetAvailableVoice()
        {
            InitializeVoices();

            // 1. Look for an idle voice
            for (int i = 0; i < _voicePool.Count; i++)
            {
                AudioSource voice = _voicePool[i];
                if (voice != null && !voice.isPlaying)
                {
                    return voice;
                }
            }

            // 2. Expand pool if under maximum
            if (_voicePool.Count < maximumVoiceCount)
            {
                return CreateVoice();
            }

            // 3. Steal oldest voice (round-robin)
            AudioSource stolen = _voicePool[_nextVoiceIndex % _voicePool.Count];
            _nextVoiceIndex++;
            return stolen;
        }

        public void PlayCue(SfxCueConfig cue, float volumeMultiplier = 1f)
        {
            if (isMuted || cue == null) return;
            EnsureLibrary();

            AudioClip clip = cue.PickClip();
            if (clip == null) return;

            AudioSource voice = GetAvailableVoice();
            if (voice == null) return;

            float pitch = cue.ResolvePitch();
            float effectiveVolume = cue.VolumeScale * volumeMultiplier * masterVolume * sfxVolume;

            voice.pitch = pitch;
            voice.PlayOneShot(clip, Mathf.Clamp01(effectiveVolume));
        }

        public void PlayCueWithFallback(SfxCueConfig cue, string fallbackKey, float volumeMultiplier = 1f)
        {
            if (isMuted) return;
            EnsureLibrary();

            AudioClip fallbackClip = library.GetFallbackClip(fallbackKey);
            AudioClip clip = (cue != null && cue.HasClip()) ? cue.PickClip(fallbackClip) : fallbackClip;
            if (clip == null) return;

            AudioSource voice = GetAvailableVoice();
            if (voice == null) return;

            float pitch = cue != null ? cue.ResolvePitch() : 1f;
            float cueScale = cue != null ? cue.VolumeScale : 0.8f;
            float effectiveVolume = cueScale * volumeMultiplier * masterVolume * sfxVolume;

            voice.pitch = pitch;
            voice.PlayOneShot(clip, Mathf.Clamp01(effectiveVolume));
        }

        public void PlayPlayer(PlayerSfxState state)
        {
            EnsureLibrary();
            switch (state)
            {
                case PlayerSfxState.AttackSwing:
                    PlayCueWithFallback(library.PlayerAttackSwing, "swing");
                    break;
                case PlayerSfxState.AttackFail:
                    PlayCueWithFallback(library.PlayerAttackFail, "fail");
                    break;
                case PlayerSfxState.Hit:
                    PlayCueWithFallback(library.PlayerHit, "hit");
                    break;
                case PlayerSfxState.CriticalHit:
                    PlayCueWithFallback(library.PlayerCriticalHit, "crit");
                    break;
                case PlayerSfxState.Hurt:
                    PlayCueWithFallback(library.PlayerHurt, "hurt");
                    break;
                case PlayerSfxState.Die:
                    PlayCueWithFallback(library.PlayerDie, "die_major");
                    break;
            }
        }

        public void PlayEnemy(
            EnemySfxState state,
            string encounterKind = null,
            IEnemySfxProfile customProfile = null,
            bool isMajorDeath = false)
        {
            EnsureLibrary();

            // 1. Check custom individual profile (EnemyDefinition or EventDefinition)
            if (customProfile != null)
            {
                SfxCueConfig customCue = null;
                switch (state)
                {
                    case EnemySfxState.Appear: customCue = customProfile.AppearSfx; break;
                    case EnemySfxState.Hurt: customCue = customProfile.HurtSfx; break;
                    case EnemySfxState.Attack: customCue = customProfile.AttackSfx; break;
                    case EnemySfxState.Die: customCue = customProfile.DieSfx; break;
                }
                if (customCue != null && customCue.HasClip())
                {
                    PlayCue(customCue);
                    return;
                }
            }

            // 2. Check per-encounter-kind binding
            if (!string.IsNullOrEmpty(encounterKind))
            {
                EncounterKindSfxBinding binding = library.FindEncounterBinding(encounterKind);
                if (binding != null)
                {
                    SfxCueConfig kindCue = null;
                    switch (state)
                    {
                        case EnemySfxState.Appear: kindCue = binding.AppearSfx; break;
                        case EnemySfxState.Hurt: kindCue = binding.HurtSfx; break;
                        case EnemySfxState.Attack: kindCue = binding.AttackSfx; break;
                        case EnemySfxState.Die: kindCue = binding.DieSfx; break;
                    }
                    if (kindCue != null && kindCue.HasClip())
                    {
                        PlayCue(kindCue);
                        return;
                    }
                }
            }

            // 3. Global library defaults with procedural synth fallbacks
            switch (state)
            {
                case EnemySfxState.Appear:
                    PlayCueWithFallback(library.EnemyAppear, "enemy_appear");
                    break;
                case EnemySfxState.Hurt:
                    PlayCueWithFallback(library.EnemyHurt, "enemy_hurt");
                    break;
                case EnemySfxState.Attack:
                    PlayCueWithFallback(library.EnemyAttack, "enemy_attack");
                    break;
                case EnemySfxState.Die:
                    if (isMajorDeath)
                    {
                        PlayCueWithFallback(library.EnemyDieMajor, "die_major");
                    }
                    else
                    {
                        PlayCueWithFallback(library.EnemyDieNormal, "die_normal");
                    }
                    break;
            }
        }

        public void PlayQuestion(QuestionSequenceSfxState state)
        {
            EnsureLibrary();
            switch (state)
            {
                case QuestionSequenceSfxState.PopUp:
                    PlayCueWithFallback(library.QuestionPopUp, "popup");
                    break;
                case QuestionSequenceSfxState.KeypadTap:
                    PlayCueWithFallback(library.QuestionKeypadTap, "click");
                    break;
                case QuestionSequenceSfxState.KeypadClear:
                    PlayCueWithFallback(library.QuestionKeypadClear, "click");
                    break;
                case QuestionSequenceSfxState.KeypadSubmit:
                    PlayCueWithFallback(library.QuestionKeypadSubmit, "popup");
                    break;
                case QuestionSequenceSfxState.CountdownTick:
                    PlayCueWithFallback(library.QuestionCountdownTick, "tick");
                    break;
                case QuestionSequenceSfxState.CountdownWarning:
                    PlayCueWithFallback(library.QuestionCountdownWarning, "tick");
                    break;
                case QuestionSequenceSfxState.ResultSuccess:
                    PlayCueWithFallback(library.QuestionResultSuccess, "success");
                    break;
                case QuestionSequenceSfxState.ResultFail:
                    PlayCueWithFallback(library.QuestionResultFail, "failure");
                    break;
                case QuestionSequenceSfxState.DamageMultiplying:
                    PlayCueWithFallback(library.QuestionDamageMultiplying, "multiplier");
                    break;
                case QuestionSequenceSfxState.RewardSticker:
                    PlayCueWithFallback(library.QuestionRewardSticker, "success");
                    break;
            }
        }

        public void PlayBattle(BattleSfxState state)
        {
            EnsureLibrary();
            switch (state)
            {
                case BattleSfxState.BiomeTransition:
                    PlayCueWithFallback(library.BattleBiomeTransition, "biome_transition");
                    break;
                case BattleSfxState.StageAdvance:
                    PlayCueWithFallback(library.BattleStageAdvance, "success");
                    break;
                case BattleSfxState.RunDefeat:
                    PlayCueWithFallback(library.BattleRunDefeat, "failure");
                    break;
                case BattleSfxState.RunComplete:
                    PlayCueWithFallback(library.BattleRunComplete, "multiplier");
                    break;
                case BattleSfxState.ActorClick:
                    PlayCueWithFallback(library.ActorInteractiveClick, "click");
                    break;
            }
        }

        public void PlayCharacterSelection(CharacterSelectionSfxState state)
        {
            EnsureLibrary();
            switch (state)
            {
                case CharacterSelectionSfxState.CardHover:
                    PlayCueWithFallback(library.CharacterCardHover, "click");
                    break;
                case CharacterSelectionSfxState.CardClick:
                    PlayCueWithFallback(library.CharacterCardClick, "popup");
                    break;
                case CharacterSelectionSfxState.CharacterAccepted:
                    PlayCueWithFallback(library.CharacterAccepted, "success");
                    break;
                case CharacterSelectionSfxState.NameConfirmed:
                    PlayCueWithFallback(library.NameConfirmed, "success");
                    break;
                case CharacterSelectionSfxState.ButtonClick:
                    PlayCueWithFallback(library.UiButtonClick, "click");
                    break;
                case CharacterSelectionSfxState.NavigationBack:
                    PlayCueWithFallback(library.UiNavigationBack, "click");
                    break;
            }
        }

        public void PlayUiStyle(string ussClassOrStyleKey, bool isClick = true)
        {
            EnsureLibrary();
            UiAnimationSfxStyle style = library.FindUiStyle(ussClassOrStyleKey);
            if (style != null)
            {
                SfxCueConfig cue = isClick ? style.ClickSfx : style.HoverSfx;
                if (cue != null && cue.HasClip())
                {
                    PlayCue(cue);
                    return;
                }
            }

            // Generic UI fallback
            PlayCueWithFallback(null, isClick ? "click" : "tick");
        }
    }
}
