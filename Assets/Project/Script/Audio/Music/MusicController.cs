using System;
using UnityEngine;

namespace PowerMath.Audio
{
    /// <summary>
    /// Core music controller managing background music, smooth lerped transitions,
    /// dedicated Big Boss channel separation without retracking, and volume ducking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MusicController : MonoBehaviour, IMusicController
    {
        private static MusicController _instance;

        public static MusicController Instance
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
        [SerializeField] private MusicLibraryDefinition library;

        [Header("Runtime State (Read Only)")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private bool isMuted = false;
        [SerializeField] private bool isBossActive = false;
        [SerializeField] private bool isEncounterOverrideActive = false;
        [SerializeField] private int duckingRequestCount = 0;
        [SerializeField] private string currentBattleBiomeId = string.Empty;

        // Dedicated Audio Channels
        private AudioSource _themeSource;
        private AudioSource _battleSource;
        private AudioSource _bossSource;

        private sealed class ChannelState
        {
            public AudioSource Source;
            public float TargetBaseVolume;
            public float CurrentBaseVolume;
            public float TrackVolumeScale = 1f;
            public float FadeSpeed = 1f;
            public bool StopOnZero = false;
            public bool IsCombat = false;
        }

        private ChannelState _themeChannel;
        private ChannelState _battleChannel;
        private ChannelState _bossChannel;

        private float _currentDuckMultiplier = 1f;
        private float _currentDuckPitchMultiplier = 1f;
        private float _duckProgress = 0f; // 0 = unducked (1.0), 1 = fully ducked (duckVolumeFactor / duckPitchFactor)
        private MusicTrackConfig _activeEncounterMusic;
        private MusicLibraryDefinition _preloadedLibrary;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public float MusicVolume
        {
            get => musicVolume;
            set => musicVolume = Mathf.Clamp01(value);
        }

        public bool IsMuted
        {
            get => isMuted;
            set => isMuted = value;
        }

        public bool IsBossActive => isBossActive;
        public bool IsEncounterOverrideActive => isEncounterOverrideActive;
        public bool IsDucked => duckingRequestCount > 0;
        public string CurrentBattleBiomeId => currentBattleBiomeId;
        public MusicLibraryDefinition Library => library;

        public AudioSource ThemeSource => _themeSource;
        public AudioSource BattleSource => _battleSource;
        public AudioSource BossSource => _bossSource;
        public float CurrentDuckMultiplier => _currentDuckMultiplier;
        public float CurrentDuckPitchMultiplier => _currentDuckPitchMultiplier;

        public static MusicController EnsureInstance()
        {
            if (_instance != null) return _instance;

            _instance = FindAnyObjectByType<MusicController>();
            if (_instance != null) return _instance;

            GameObject go = new GameObject("GameMusicController");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MusicController>();
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

            InitializeChannels();
            EnsureLibrary();
        }

        private void EnsureLibrary()
        {
            if (library == null)
            {
                library = Resources.Load<MusicLibraryDefinition>("MusicLibrary");
            }
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<MusicLibraryDefinition>();
            }

            if (_preloadedLibrary != library)
            {
                _preloadedLibrary = library;
                foreach (AudioClip clip in library.ConfiguredClips)
                {
                    PreloadClip(clip);
                }
            }
        }

        public void PreloadClip(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }
        }

        private void InitializeChannels()
        {
            if (_themeSource == null)
            {
                _themeSource = CreateChannelSource("ThemeChannel");
                _battleSource = CreateChannelSource("BattleChannel");
                _bossSource = CreateChannelSource("BossChannel");

                _themeChannel = new ChannelState { Source = _themeSource, IsCombat = false };
                _battleChannel = new ChannelState { Source = _battleSource, IsCombat = true };
                _bossChannel = new ChannelState { Source = _bossSource, IsCombat = true };
            }
        }

        private AudioSource CreateChannelSource(string channelName)
        {
            GameObject child = new GameObject(channelName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D flat stereo
            source.loop = true;
            source.volume = 0f;
            return source;
        }

        public void PlayLoginMusic(bool forceRestart = false)
        {
            InitializeChannels();
            EnsureLibrary();

            // Fade out any combat music
            FadeOutChannel(_battleChannel, library.DefaultCrossfadeDuration, stopOnZero: true);
            FadeOutChannel(_bossChannel, library.BossInterruptDuration, stopOnZero: true);
            isBossActive = false;
            isEncounterOverrideActive = false;
            _activeEncounterMusic = null;

            AudioClip loginClip = library.GetLoginClipWithFallback();
            float targetScale = library.LoginMusic?.VolumeScale ?? 0.85f;

            _themeChannel.TrackVolumeScale = targetScale;
            _themeChannel.StopOnZero = false;
            _themeChannel.FadeSpeed = 1f / library.DefaultCrossfadeDuration;
            _themeChannel.TargetBaseVolume = 1f;

            if (_themeSource.clip != loginClip || forceRestart || !_themeSource.isPlaying)
            {
                _themeSource.clip = loginClip;
                _themeSource.loop = library.LoginMusic?.Loop ?? true;
                if (forceRestart || !_themeSource.isPlaying)
                {
                    _themeSource.time = 0f;
                    _themeSource.Play();
                }
            }
        }

        public void PlayBattleMusic(string biomeId = null, bool suddenKickIn = false, bool forceRestart = false)
        {
            InitializeChannels();
            EnsureLibrary();

            // Fade out theme music smoothly
            FadeOutChannel(_themeChannel, library.DefaultCrossfadeDuration, stopOnZero: true);

            AudioClip battleClip = library.GetBattleClipWithFallback(biomeId);
            PreloadClip(battleClip);
            MusicTrackConfig config = library.ResolveBattleTrack(biomeId);
            float targetScale = config?.VolumeScale ?? 0.85f;
            _battleChannel.TrackVolumeScale = targetScale;

            bool isSameBiome = string.Equals(currentBattleBiomeId, biomeId, StringComparison.OrdinalIgnoreCase);
            bool isDifferentClip = (_battleSource.clip != battleClip);
            bool shouldSuddenKickIn = isDifferentClip && suddenKickIn;

            currentBattleBiomeId = biomeId;

            if (isDifferentClip || forceRestart)
            {
                _battleSource.clip = battleClip;
                _battleSource.loop = config?.Loop ?? true;
                _battleSource.time = 0f;

                if (shouldSuddenKickIn)
                {
                    if (_bossChannel != null)
                    {
                        _bossChannel.CurrentBaseVolume = 0f;
                        _bossChannel.TargetBaseVolume = 0f;
                        if (_bossSource != null && _bossSource.isPlaying)
                        {
                            _bossSource.Stop();
                        }
                        isBossActive = false;
                        isEncounterOverrideActive = false;
                        _activeEncounterMusic = null;
                    }

                    _battleChannel.CurrentBaseVolume = 1f;
                    _battleChannel.TargetBaseVolume = 1f;
                    _battleChannel.FadeSpeed = 1f;
                    _battleChannel.StopOnZero = false;
                    _battleSource.volume = Mathf.Clamp01(targetScale * masterVolume * musicVolume);
                    _battleSource.Play();
                }
                else
                {
                    // Start a newly selected biome track silently and crossfade
                    _battleChannel.CurrentBaseVolume = 0f;
                    _battleSource.volume = 0f;
                    _battleSource.Play();
                    _battleChannel.FadeSpeed = 1f / library.DefaultCrossfadeDuration;
                    _battleChannel.StopOnZero = false;
                    _battleChannel.TargetBaseVolume = isEncounterOverrideActive ? 0f : 1f;
                }
            }
            else
            {
                // Same music track: continuous playback must NOT be interrupted or restarted.
                if (!_battleSource.isPlaying)
                {
                    _battleSource.Play();
                }

                _battleChannel.FadeSpeed = 1f / library.DefaultCrossfadeDuration;
                _battleChannel.StopOnZero = false;
                _battleChannel.TargetBaseVolume = isEncounterOverrideActive ? 0f : 1f;
            }
        }

        public void SetBossBattleActive(bool isActive)
        {
            SetEncounterMusicOverride(null, isActive);
        }

        public void SetEncounterMusicOverride(MusicTrackConfig customTrack, bool isBossEncounter = false)
        {
            InitializeChannels();
            EnsureLibrary();

            bool hasCustomTrack = customTrack != null && customTrack.Clip != null;
            bool shouldOverride = hasCustomTrack || isBossEncounter;
            MusicTrackConfig desiredTrack = hasCustomTrack ? customTrack :
                (isBossEncounter ? library.BossBattleMusic : null);

            isBossActive = isBossEncounter;

            float duration = library.BossInterruptDuration;

            if (shouldOverride)
            {
                AudioClip overrideClip = hasCustomTrack
                    ? customTrack.Clip
                    : library.GetBossClipWithFallback();
                PreloadClip(overrideClip);
                float targetScale = desiredTrack?.VolumeScale ?? 0.95f;
                bool trackChanged = _bossSource.clip != overrideClip;

                isEncounterOverrideActive = true;
                _activeEncounterMusic = desiredTrack;
                _bossChannel.TrackVolumeScale = targetScale;

                if (trackChanged || !_bossSource.isPlaying)
                {
                    // A changed encounter track begins silent so it cannot pop at the
                    // previous override's full volume before the next audio tick.
                    if (trackChanged)
                    {
                        _bossChannel.CurrentBaseVolume = 0f;
                        _bossSource.volume = 0f;
                    }
                    _bossSource.clip = overrideClip;
                    _bossSource.loop = desiredTrack?.Loop ?? true;
                    _bossSource.time = 0f;
                    _bossSource.Play();
                }

                _bossChannel.FadeSpeed = 1f / duration;
                _bossChannel.TargetBaseVolume = 1f;
                _bossChannel.StopOnZero = false;

                // Underlying battle music continues silently so it can resume at
                // its current timeline position after the encounter override.
                _battleChannel.FadeSpeed = 1f / duration;
                _battleChannel.TargetBaseVolume = 0f;
                _battleChannel.StopOnZero = false;
            }
            else
            {
                if (!isEncounterOverrideActive) return;

                isEncounterOverrideActive = false;
                _activeEncounterMusic = null;
                FadeOutChannel(_bossChannel, duration, stopOnZero: true);

                _battleChannel.FadeSpeed = 1f / duration;
                _battleChannel.TargetBaseVolume = 1f;
                _battleChannel.StopOnZero = false;
            }
        }

        public void SetDucking(bool isDucked)
        {
            duckingRequestCount = isDucked ? 1 : 0;
            if (!isDucked)
            {
                EnsurePlayback();
            }
        }

        public void StopMusic(float fadeDuration = -1f)
        {
            InitializeChannels();
            EnsureLibrary();

            float duration = fadeDuration > 0f ? fadeDuration : library.DefaultCrossfadeDuration;
            FadeOutChannel(_themeChannel, duration, stopOnZero: true);
            FadeOutChannel(_battleChannel, duration, stopOnZero: true);
            FadeOutChannel(_bossChannel, duration, stopOnZero: true);
            isBossActive = false;
            isEncounterOverrideActive = false;
            _activeEncounterMusic = null;
            duckingRequestCount = 0;
            _duckProgress = 0f;
            _currentDuckMultiplier = 1f;
            _currentDuckPitchMultiplier = 1f;
            if (_themeSource != null) _themeSource.pitch = 1f;
            if (_battleSource != null) _battleSource.pitch = 1f;
            if (_bossSource != null) _bossSource.pitch = 1f;
        }

        public void ResetBattleState()
        {
            InitializeChannels();
            currentBattleBiomeId = string.Empty;
            isBossActive = false;
            isEncounterOverrideActive = false;
            _activeEncounterMusic = null;

            if (_battleSource != null)
            {
                _battleSource.Stop();
                _battleSource.time = 0f;
                _battleSource.clip = null;
            }
            if (_bossSource != null)
            {
                _bossSource.Stop();
                _bossSource.time = 0f;
                _bossSource.clip = null;
            }
            if (_battleChannel != null)
            {
                _battleChannel.CurrentBaseVolume = 0f;
                _battleChannel.TargetBaseVolume = 0f;
            }
            if (_bossChannel != null)
            {
                _bossChannel.CurrentBaseVolume = 0f;
                _bossChannel.TargetBaseVolume = 0f;
            }
        }

        public void FadeOutBossMusic(float fadeDuration = 1.2f)
        {
            InitializeChannels();
            EnsureLibrary();

            float duration = fadeDuration > 0f ? fadeDuration : library.BossInterruptDuration;
            FadeOutChannel(_bossChannel, duration, stopOnZero: true);
            isBossActive = false;
            isEncounterOverrideActive = false;
            _activeEncounterMusic = null;

            if (_battleChannel != null)
            {
                _battleChannel.FadeSpeed = 1f / duration;
                _battleChannel.TargetBaseVolume = 0f;
                _battleChannel.StopOnZero = false;
            }
        }

        public void EnsurePlayback()
        {
            if (AudioListener.pause)
            {
                AudioListener.pause = false;
            }

            if (_themeChannel != null && _themeChannel.TargetBaseVolume > 0.0001f && _themeSource != null && _themeSource.clip != null)
            {
                if (!_themeSource.isPlaying)
                {
                    _themeSource.UnPause();
                    if (!_themeSource.isPlaying) _themeSource.Play();
                }
            }
            if (_battleChannel != null && _battleChannel.TargetBaseVolume > 0.0001f && _battleSource != null && _battleSource.clip != null)
            {
                if (!_battleSource.isPlaying)
                {
                    _battleSource.UnPause();
                    if (!_battleSource.isPlaying) _battleSource.Play();
                }
            }
            if (_bossChannel != null && _bossChannel.TargetBaseVolume > 0.0001f && _bossSource != null && _bossSource.clip != null)
            {
                if (!_bossSource.isPlaying)
                {
                    _bossSource.UnPause();
                    if (!_bossSource.isPlaying) _bossSource.Play();
                }
            }
        }

        private void FadeOutChannel(ChannelState channel, float duration, bool stopOnZero)
        {
            if (channel == null) return;
            channel.TargetBaseVolume = 0f;
            channel.FadeSpeed = 1f / Mathf.Max(0.05f, duration);
            channel.StopOnZero = stopOnZero;
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Update step exposed for deterministic unit testing without real-time delays.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (library == null) EnsureLibrary();
            if (_themeChannel == null) InitializeChannels();

            // 1. Dynamically sync live track volume scales from library definition
            SyncTrackVolumeScales();

            // 2. Update Ducking with smoothstep easing curve for non-abrupt transitions
            float targetDuckProgress = (duckingRequestCount > 0) ? 1f : 0f;
            float duckSpeed = 1f / library.DuckFadeDuration;
            _duckProgress = Mathf.MoveTowards(_duckProgress, targetDuckProgress, duckSpeed * deltaTime);

            float smoothedT = Mathf.SmoothStep(0f, 1f, _duckProgress);
            _currentDuckMultiplier = Mathf.Lerp(1f, library.DuckVolumeFactor, smoothedT);
            _currentDuckPitchMultiplier = Mathf.Lerp(1f, library.DuckPitchFactor, smoothedT);

            // 3. Update Channel Volumes
            UpdateChannel(_themeChannel, deltaTime);
            UpdateChannel(_battleChannel, deltaTime);
            UpdateChannel(_bossChannel, deltaTime);
        }

        private void SyncTrackVolumeScales()
        {
            if (library == null) return;

            if (_themeChannel != null && library.LoginMusic != null)
            {
                _themeChannel.TrackVolumeScale = library.LoginMusic.VolumeScale;
            }

            if (_battleChannel != null)
            {
                MusicTrackConfig battleConfig = library.ResolveBattleTrack(currentBattleBiomeId);
                if (battleConfig != null)
                {
                    _battleChannel.TrackVolumeScale = battleConfig.VolumeScale;
                }
            }

            if (_bossChannel != null && _activeEncounterMusic != null)
            {
                _bossChannel.TrackVolumeScale = _activeEncounterMusic.VolumeScale;
            }
        }

        private void UpdateChannel(ChannelState channel, float deltaTime)
        {
            if (channel == null || channel.Source == null) return;

            // Smooth linear volume progression towards target
            channel.CurrentBaseVolume = Mathf.MoveTowards(
                channel.CurrentBaseVolume,
                channel.TargetBaseVolume,
                channel.FadeSpeed * deltaTime
            );

            if (channel.CurrentBaseVolume <= 0.0001f && channel.StopOnZero && channel.Source.isPlaying)
            {
                channel.Source.Stop();
            }
            else if (channel.TargetBaseVolume > 0.0001f && !channel.Source.isPlaying && channel.Source.clip != null)
            {
                channel.Source.Play();
            }

            // Smooth pitch calculation: slow down combat music when ducked.
            // On Unity WebGL, changing AudioSource.pitch on a looping track triggers an engine bug:
            // jsAudioMixinSetPitch computes (currentTime - curPosition / newPitch) where estimatePlaybackPosition()
            // evaluates (t % (loopEnd - loopStart)), producing NaN (0 % 0) and permanently muting the WebAudio channel.
            // In WebGL, ducking operates purely on volume with pitch locked at 1.0f.
#if UNITY_WEBGL && !UNITY_EDITOR
            if (channel.Source.pitch != 1f)
            {
                channel.Source.pitch = 1f;
            }
#else
            float targetPitch = channel.IsCombat ? _currentDuckPitchMultiplier : 1f;
            if (Mathf.Abs(channel.Source.pitch - targetPitch) > 0.001f)
            {
                channel.Source.pitch = targetPitch;
            }
#endif

            if (isMuted)
            {
                channel.Source.volume = 0f;
                return;
            }

            // Smooth volume calculation with ducking & master multipliers
            float duckMultiplier = channel.IsCombat ? _currentDuckMultiplier : 1f;
            float effectiveVolume = channel.CurrentBaseVolume * channel.TrackVolumeScale * duckMultiplier * masterVolume * musicVolume;

            channel.Source.volume = Mathf.Clamp01(effectiveVolume);
        }
    }
}
