using System;
using UnityEngine;
using UnityEngine.Video;

namespace PowerMath.UI.Shared
{
    /// <summary>
    /// Universal looping video controller that supports both Linear and Ping-Pong (Forward-then-Reverse) looping.
    /// Can be attached to any GameObject with a VideoPlayer component across any scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class PingPongVideoPlayer : MonoBehaviour
    {
        public enum LoopMode
        {
            /// <summary>
            /// Plays forward to the end, then reverses smoothly back to the start, repeating continuously.
            /// Eliminates abrupt loop cuts on videos that do not naturally seamlessly tile.
            /// </summary>
            PingPong,

            /// <summary>
            /// Standard forward looping from start to finish.
            /// </summary>
            Linear,

            /// <summary>
            /// Plays once and stops at the end.
            /// </summary>
            Once
        }

        public enum PlaybackDirection
        {
            Forward,
            Reverse
        }

        [Header("Loop Configuration")]
        [Tooltip("Looping style: PingPong (forward then reverse) or Linear.")]
        [SerializeField] private LoopMode loopMode = LoopMode.PingPong;

        [Tooltip("Primary forward video clip. If left empty, uses the clip already assigned to the VideoPlayer.")]
        [SerializeField] private VideoClip forwardClip;

        [Tooltip("Optional pre-rendered reverse video clip. When assigned, provides 100% hardware-accelerated, stutter-free reverse playback across all platforms (WebGL, Mobile, PC).")]
        [SerializeField] private VideoClip reverseClip;

        [Tooltip("StreamingAssets-relative or hosted forward URL used by WebGL.")]
        [SerializeField] private string forwardUrl;

        [Tooltip("StreamingAssets-relative or hosted reverse URL used by WebGL.")]
        [SerializeField] private string reverseUrl;

        [Header("Playback Settings")]
        [Tooltip("Playback speed multiplier.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float playbackSpeed = 1f;

        [Tooltip("Pause duration in seconds when changing directions (adds natural easing at peaks).")]
        [Range(0f, 1f)]
        [SerializeField] private float directionChangePauseSeconds = 0f;

        [Tooltip("When using single-clip runtime reverse scrubbing, the target interval between seek updates (in seconds).")]
        [Range(0.016f, 0.1f)]
        [SerializeField] private float reverseScrubInterval = 0.033f;

        private VideoPlayer _player;
        private PlaybackDirection _direction = PlaybackDirection.Forward;
        private double _currentTime;
        private bool _isPausedForTurnaround;
        private float _pauseTimer;
        private float _scrubTimer;
        private bool _isSeeking;
        private bool _hasInitialized;

        public LoopMode CurrentLoopMode
        {
            get => loopMode;
            set => loopMode = value;
        }

        public PlaybackDirection CurrentDirection => _direction;
        public bool IsReversing => _direction == PlaybackDirection.Reverse;
        public VideoClip ForwardClip => forwardClip;
        public VideoClip ReverseClip => reverseClip;

        public event Action<PlaybackDirection> DirectionChanged;
        public event Action LoopCycleCompleted;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            RegisterEvents();

            if (_player != null && _player.playOnAwake)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void EnsureInitialized()
        {
            if (_hasInitialized) return;

            _player = GetComponent<VideoPlayer>();
            if (_player == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!ApplyWebSource(PlaybackDirection.Forward))
            {
                enabled = false;
                return;
            }
#else
            // Inherit the forward clip from the VideoPlayer if not authored explicitly
            if (forwardClip == null && _player.clip != null)
            {
                forwardClip = _player.clip;
            }
#endif

            // We handle the loop transitions in this component
            _player.isLooping = false;
            _player.playbackSpeed = playbackSpeed;

            _hasInitialized = true;
        }

        private void RegisterEvents()
        {
            if (_player == null) return;
            _player.loopPointReached += OnVideoEndReached;
            _player.seekCompleted += OnSeekCompleted;
        }

        private void UnregisterEvents()
        {
            if (_player == null) return;
            _player.loopPointReached -= OnVideoEndReached;
            _player.seekCompleted -= OnSeekCompleted;
        }

        private void Update()
        {
            if (_player == null || !_player.isPrepared) return;

            // Handle momentary pause at turnaround point
            if (_isPausedForTurnaround)
            {
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer <= 0f)
                {
                    _isPausedForTurnaround = false;
                    ResumeAfterTurnaround();
                }
                return;
            }

            // If in PingPong mode without a dedicated reverse clip, perform runtime scrubbing
            if (loopMode == LoopMode.PingPong && !HasReversePlaybackSource() &&
                _direction == PlaybackDirection.Reverse)
            {
                UpdateRuntimeReverseScrub();
            }
        }

        /// <summary>
        /// Plays the video from current state.
        /// </summary>
        public void Play()
        {
            EnsureInitialized();
            if (_player == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!ApplyWebSource(_direction)) return;
#else
            if (forwardClip != null && _player.clip == null)
            {
                _player.clip = forwardClip;
            }
#endif

            _player.playbackSpeed = playbackSpeed;
            _player.Play();
        }

        /// <summary>
        /// Pauses the video.
        /// </summary>
        public void Pause()
        {
            if (_player != null)
            {
                _player.Pause();
            }
        }

        /// <summary>
        /// Stops video playback and resets to the starting frame.
        /// </summary>
        public void Stop()
        {
            if (_player == null) return;

            _player.Stop();
            _direction = PlaybackDirection.Forward;
            _isPausedForTurnaround = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            ApplyWebSource(PlaybackDirection.Forward);
#else
            if (forwardClip != null)
            {
                _player.clip = forwardClip;
            }
#endif
        }

        /// <summary>
        /// Configures forward and reverse clips at runtime.
        /// </summary>
        public void SetClips(VideoClip forward, VideoClip reverse = null)
        {
            forwardClip = forward;
            reverseClip = reverse;

            if (_player != null)
            {
                _player.clip = _direction == PlaybackDirection.Reverse && reverseClip != null
                    ? reverseClip
                    : forwardClip;
            }
        }

        private void OnVideoEndReached(VideoPlayer source)
        {
            if (loopMode == LoopMode.Once)
            {
                return;
            }

            if (loopMode == LoopMode.Linear)
            {
                // Restart forward
                _player.time = 0;
                _player.Play();
                LoopCycleCompleted?.Invoke();
                return;
            }

            // PingPong mode
            if (directionChangePauseSeconds > 0f)
            {
                _isPausedForTurnaround = true;
                _pauseTimer = directionChangePauseSeconds;
                _player.Pause();
            }
            else
            {
                SwitchDirection();
            }
        }

        private void ResumeAfterTurnaround()
        {
            SwitchDirection();
        }

        private void SwitchDirection()
        {
            if (_direction == PlaybackDirection.Forward)
            {
                _direction = PlaybackDirection.Reverse;
                DirectionChanged?.Invoke(_direction);

                if (HasReversePlaybackSource())
                {
                    // Seamless dual-clip swap: play the pre-rendered reversed video forward
#if UNITY_WEBGL && !UNITY_EDITOR
                    if (!ApplyWebSource(PlaybackDirection.Reverse)) return;
#else
                    _player.clip = reverseClip;
#endif
                    _player.time = 0;
                    _player.Play();
                }
                else
                {
                    // Fallback to runtime reverse scrubbing
                    _player.Pause();
                    _currentTime = _player.length > 0 ? _player.length : _player.time;
                    _isSeeking = false;
                }
            }
            else
            {
                _direction = PlaybackDirection.Forward;
                DirectionChanged?.Invoke(_direction);
                LoopCycleCompleted?.Invoke();

#if UNITY_WEBGL && !UNITY_EDITOR
                if (!ApplyWebSource(PlaybackDirection.Forward)) return;
#else
                if (forwardClip != null)
                {
                    _player.clip = forwardClip;
                }
#endif

                _player.time = 0;
                _player.Play();
            }
        }

        private void UpdateRuntimeReverseScrub()
        {
            _scrubTimer += Time.deltaTime;
            if (_scrubTimer < reverseScrubInterval)
            {
                return;
            }
            _scrubTimer = 0f;

            _currentTime -= reverseScrubInterval * playbackSpeed;
            if (_currentTime <= 0.05)
            {
                _currentTime = 0;
                SwitchDirection();
                return;
            }

            if (!_isSeeking)
            {
                _isSeeking = true;
                _player.time = _currentTime;
            }
        }

        private void OnSeekCompleted(VideoPlayer source)
        {
            _isSeeking = false;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private bool HasReversePlaybackSource()
        {
            return StreamingVideoPath.TryResolve(reverseUrl, out _);
        }

        private bool ApplyWebSource(PlaybackDirection direction)
        {
            string configured = direction == PlaybackDirection.Reverse
                ? reverseUrl
                : forwardUrl;
            if (!StreamingVideoPath.TryResolve(configured, out string url))
            {
                Debug.LogWarning($"[PingPongVideoPlayer] Missing WebGL {direction} video URL.", this);
                return false;
            }

            _player.source = VideoSource.Url;
            _player.url = url;
            return true;
        }
#else
        private bool HasReversePlaybackSource()
        {
            return reverseClip != null;
        }
#endif
    }
}
