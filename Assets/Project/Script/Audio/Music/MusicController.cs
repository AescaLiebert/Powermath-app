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
        private float _duckProgress = 0f; // 0 = unducked (1.0), 1 = fully ducked (duckVolumeFactor)

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
        public bool IsDucked => duckingRequestCount > 0;
        public string CurrentBattleBiomeId => currentBattleBiomeId;
        public MusicLibraryDefinition Library => library;

        public AudioSource ThemeSource => _themeSource;
        public AudioSource BattleSource => _battleSource;
        public AudioSource BossSource => _bossSource;
        public float CurrentDuckMultiplier => _currentDuckMultiplier;

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

        public void PlayBattleMusic(string biomeId = null)
        {
            InitializeChannels();
            EnsureLibrary();

            // Fade out theme music smoothly
            FadeOutChannel(_themeChannel, library.DefaultCrossfadeDuration, stopOnZero: true);

            currentBattleBiomeId = biomeId;
            AudioClip battleClip = library.GetBattleClipWithFallback(biomeId);
            MusicTrackConfig config = library.ResolveBattleTrack(biomeId);
            float targetScale = config?.VolumeScale ?? 0.85f;
            _battleChannel.TrackVolumeScale = targetScale;

            if (_battleSource.clip != battleClip)
            {
                // Smooth crossfade to new biome track
                _battleSource.clip = battleClip;
                _battleSource.loop = config?.Loop ?? true;
                _battleSource.time = 0f;
                _battleSource.Play();
            }
            else if (!_battleSource.isPlaying)
            {
                _battleSource.Play();
            }

            _battleChannel.FadeSpeed = 1f / library.DefaultCrossfadeDuration;
            _battleChannel.StopOnZero = false;

            // If Big Boss is currently interrupting, battle music remains running at 0 volume.
            // Otherwise, it fades up to full volume.
            _battleChannel.TargetBaseVolume = isBossActive ? 0f : 1f;
        }

        public void SetBossBattleActive(bool isActive)
        {
            InitializeChannels();
            EnsureLibrary();

            if (isBossActive == isActive) return;
            isBossActive = isActive;

            float duration = library.BossInterruptDuration;

            if (isActive)
            {
                // Boss arrives:
                // 1. Boss channel starts playing and fades in.
                AudioClip bossClip = library.GetBossClipWithFallback();
                float targetScale = library.BossBattleMusic?.VolumeScale ?? 0.95f;
                _bossChannel.TrackVolumeScale = targetScale;

                if (_bossSource.clip != bossClip || !_bossSource.isPlaying)
                {
                    _bossSource.clip = bossClip;
                    _bossSource.loop = library.BossBattleMusic?.Loop ?? true;
                    _bossSource.time = 0f;
                    _bossSource.Play();
                }

                _bossChannel.FadeSpeed = 1f / duration;
                _bossChannel.TargetBaseVolume = 1f;
                _bossChannel.StopOnZero = false;

                // 2. Normal battle channel fades to 0 volume BUT DOES NOT STOP!
                // It continues playing in the background without retracking.
                _battleChannel.FadeSpeed = 1f / duration;
                _battleChannel.TargetBaseVolume = 0f;
                _battleChannel.StopOnZero = false; // Keep playing silently!
            }
            else
            {
                // Boss finishes:
                // 1. Boss channel fades out and stops when 0.
                FadeOutChannel(_bossChannel, duration, stopOnZero: true);

                // 2. Normal battle channel fades back up smoothly at current timeline position.
                if (!_battleSource.isPlaying)
                {
                    _battleSource.Play();
                }
                _battleChannel.FadeSpeed = 1f / duration;
                _battleChannel.TargetBaseVolume = 1f;
                _battleChannel.StopOnZero = false;
            }
        }

        public void SetDucking(bool isDucked)
        {
            if (isDucked)
            {
                duckingRequestCount++;
            }
            else
            {
                duckingRequestCount = Mathf.Max(0, duckingRequestCount - 1);
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

            if (_bossChannel != null && library.BossBattleMusic != null)
            {
                _bossChannel.TrackVolumeScale = library.BossBattleMusic.VolumeScale;
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
