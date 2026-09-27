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

        [Header("Fallback & Presentation")]
        [Tooltip("Static fallback texture shown when video is loading, missing, or lagging.")]
        [SerializeField] private Texture fallbackTexture;

        [Tooltip("Optional dedicated fallback graphic (Image or RawImage) in the UI hierarchy.")]
        [SerializeField] private UnityEngine.UI.Graphic fallbackGraphic;

        [Tooltip("Play automatically when enabled.")]
        [SerializeField] private bool autoPlay = true;

        [Tooltip("Timeout in seconds waiting for video to prepare before falling back to static image.")]
        [Range(1f, 15f)]
        [SerializeField] private float preparationTimeoutSeconds = 4f;

        [Tooltip("Maximum duration in seconds video can stall without frame progress before falling back to static image.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float maxStallSeconds = 2.0f;

        private VideoPlayer _player;
        private UnityEngine.UI.RawImage _rawImage;
        private Texture _originalRawImageTexture;
        private PlaybackDirection _direction = PlaybackDirection.Forward;
        private double _currentTime;
        private bool _isPausedForTurnaround;
        private float _pauseTimer;
        private float _scrubTimer;
        private bool _isSeeking;
        private bool _hasInitialized;

        private bool _isShowingFallback;
        private bool _isPreparing;
        private float _prepareTimer;
        private float _stallTimer;
        private double _lastObservedTime = -1;

        public LoopMode CurrentLoopMode
        {
            get => loopMode;
            set => loopMode = value;
        }

        public PlaybackDirection CurrentDirection => _direction;
        public bool IsReversing => _direction == PlaybackDirection.Reverse;
        public VideoClip ForwardClip => forwardClip;
        public VideoClip ReverseClip => reverseClip;

        public Texture FallbackTexture
        {
            get => fallbackTexture;
            set
            {
                fallbackTexture = value;
                if (_isShowingFallback && _rawImage != null && fallbackTexture != null)
                {
                    _rawImage.texture = fallbackTexture;
                }
            }
        }

        public bool IsShowingFallback => _isShowingFallback;

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

            if (_player != null && (_player.playOnAwake || autoPlay))
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
            _rawImage = GetComponent<UnityEngine.UI.RawImage>();

            if (_rawImage != null)
            {
                _originalRawImageTexture = _rawImage.texture;
                if (fallbackTexture != null)
                {
                    _rawImage.texture = fallbackTexture;
                    _isShowingFallback = true;
                }
            }

            if (fallbackGraphic != null)
            {
                fallbackGraphic.gameObject.SetActive(true);
            }

            if (_player == null) return;

            // In Editor or native builds, prevent native VideoPlayer from attempting to auto-play
            // un-resolved relative URLs before our script resolves the full local or remote path.
            _player.playOnAwake = false;

            // Background Live2D loop videos are ambient visuals without audio. Mute all tracks
            // so modern browser autoplay policies (Chrome/Safari/Edge/iOS/Android) never block playback.
            for (ushort i = 0; i < _player.controlledAudioTrackCount; i++)
            {
                _player.SetDirectAudioMute(i, true);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!ApplyWebSource(PlaybackDirection.Forward))
            {
                enabled = false;
                return;
            }
#else
            if (forwardClip != null)
            {
                _player.source = VideoSource.VideoClip;
                _player.clip = forwardClip;
            }
            else if (_player.clip != null)
            {
                forwardClip = _player.clip;
            }
            else if (!string.IsNullOrWhiteSpace(forwardUrl))
            {
                ApplyWebSource(PlaybackDirection.Forward);
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
            _player.errorReceived += OnVideoError;
            _player.prepareCompleted += OnPrepareCompleted;
        }

        private void UnregisterEvents()
        {
            if (_player == null) return;
            _player.loopPointReached -= OnVideoEndReached;
            _player.seekCompleted -= OnSeekCompleted;
            _player.errorReceived -= OnVideoError;
            _player.prepareCompleted -= OnPrepareCompleted;
        }

        private void Update()
        {
            if (_player == null) return;

            // Handle preparation timeout if video fails to load or CORS blocks it
            if (_isPreparing && !_player.isPrepared)
            {
                _prepareTimer += Time.unscaledDeltaTime;
                if (_prepareTimer >= preparationTimeoutSeconds)
                {
                    _isPreparing = false;
                    Debug.LogWarning($"[PingPongVideoPlayer] Video preparation timed out after {preparationTimeoutSeconds:F1}s. Showing static fallback image.", this);
                    ActivateFallback();
                }
                return;
            }

            if (!_player.isPrepared) return;

            // Video is prepared and playing: detect frame stalling / lagging
            if (_player.isPlaying)
            {
                if (_isShowingFallback && _stallTimer == 0f)
                {
                    RestoreVideoTexture();
                }

                if (!_isPausedForTurnaround && !_isSeeking)
                {
                    if (Math.Abs(_player.time - _lastObservedTime) < 0.0001)
                    {
                        _stallTimer += Time.unscaledDeltaTime;
                        if (_stallTimer >= maxStallSeconds && !_isShowingFallback)
                        {
                            Debug.LogWarning($"[PingPongVideoPlayer] Video stalled/buffering for {_stallTimer:F1}s. Showing static fallback image to prevent flickering.", this);
                            ActivateFallback();
                        }
                    }
                    else
                    {
                        _stallTimer = 0f;
                        _lastObservedTime = _player.time;
                        if (_isShowingFallback)
                        {
                            RestoreVideoTexture();
                        }
                    }
                }
            }

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

            _isPreparing = true;
            _prepareTimer = 0f;
            _stallTimer = 0f;
            _lastObservedTime = -1;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!ApplyWebSource(_direction)) return;
#else
            if (_player.source == VideoSource.Url)
            {
                if (!ApplyWebSource(_direction)) return;
            }
            else if (forwardClip != null && _player.clip == null)
            {
                _player.clip = forwardClip;
            }
#endif

            _player.playbackSpeed = playbackSpeed;
            _player.Play();
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            _isPreparing = false;
            Debug.LogWarning($"[PingPongVideoPlayer] Video playback error: {message}. Showing fallback static image.", this);
            ActivateFallback();
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            _isPreparing = false;
            _prepareTimer = 0f;
            _stallTimer = 0f;
            _lastObservedTime = -1;
            RestoreVideoTexture();
        }

        public void ActivateFallback()
        {
            _isShowingFallback = true;
            if (_rawImage != null && fallbackTexture != null)
            {
                _rawImage.texture = fallbackTexture;
            }
            if (fallbackGraphic != null)
            {
                fallbackGraphic.gameObject.SetActive(true);
            }
        }

        public void RestoreVideoTexture()
        {
            _isShowingFallback = false;
            Texture target = _player != null && _player.targetTexture != null
                ? (Texture)_player.targetTexture
                : _originalRawImageTexture;

            if (_rawImage != null && target != null)
            {
                _rawImage.texture = target;
            }
            if (fallbackGraphic != null)
            {
                fallbackGraphic.gameObject.SetActive(false);
            }
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
            if (_player.source == VideoSource.Url)
            {
                ApplyWebSource(PlaybackDirection.Forward);
            }
            else if (forwardClip != null)
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
                    if (_player.source == VideoSource.Url)
                    {
                        if (!ApplyWebSource(PlaybackDirection.Reverse)) return;
                    }
                    else
                    {
                        _player.clip = reverseClip;
                    }
#endif
                    try
                    {
                        if (_player.isPrepared) _player.time = 0;
                        _player.Play();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PingPongVideoPlayer] Reverse playback error: {ex.Message}", this);
                    }
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
                if (_player.source == VideoSource.Url)
                {
                    if (!ApplyWebSource(PlaybackDirection.Forward)) return;
                }
                else if (forwardClip != null)
                {
                    _player.clip = forwardClip;
                }
#endif

                try
                {
                    if (_player.isPrepared) _player.time = 0;
                    _player.Play();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PingPongVideoPlayer] Forward playback error: {ex.Message}", this);
                }
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

        private bool HasReversePlaybackSource()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return StreamingVideoPath.TryResolve(reverseUrl, out _);
#else
            if (_player != null && _player.source == VideoSource.Url)
            {
                return StreamingVideoPath.TryResolve(reverseUrl, out _);
            }
            return reverseClip != null;
#endif
        }

        private bool ApplyWebSource(PlaybackDirection direction)
        {
            if (_player == null) return false;
            string configured = direction == PlaybackDirection.Reverse
                ? reverseUrl
                : forwardUrl;
            if (!StreamingVideoPath.TryResolve(configured, out string url))
            {
                Debug.LogWarning($"[PingPongVideoPlayer] Missing video URL for {direction}.", this);
                return false;
            }

            if (_player.source != VideoSource.Url || _player.url != url)
            {
                _player.source = VideoSource.Url;
                _player.url = url;
            }
            return true;
        }
    }
}
