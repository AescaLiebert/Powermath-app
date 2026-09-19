using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Audio
{
    /// <summary>
    /// Dedicated audio controller for dialogue voice lines, character speech, and narration.
    /// Manages dual-channel voice playback with adaptive interruption (smoothly fading out
    /// the previous voice over 60-80ms so consecutive voice lines never pop, click, or overlap).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VoiceController : MonoBehaviour, IVoiceController
    {
        private static VoiceController _instance;

        public static VoiceController Instance
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

        [Header("Runtime State")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float voiceVolume = 1f;
        [SerializeField] private bool isMuted = false;

        private const float DefaultInterruptFadeSeconds = 0.08f;
        private const float DefaultStopFadeSeconds = 0.06f;
        private const int SampleRate = 22050;

        // Dual AudioSources for smooth adaptive crossfading/interruption
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _activeSource;

        private Coroutine _fadeRoutineA;
        private Coroutine _fadeRoutineB;

        // Cached procedural fallback voice clips
        private readonly Dictionary<string, AudioClip> _proceduralClipCache =
            new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public float VoiceVolume
        {
            get => voiceVolume;
            set => voiceVolume = Mathf.Clamp01(value);
        }

        public bool IsMuted
        {
            get => isMuted;
            set => isMuted = value;
        }

        public bool IsSpeaking
        {
            get
            {
                bool a = _sourceA != null && _sourceA.isPlaying;
                bool b = _sourceB != null && _sourceB.isPlaying;
                return a || b;
            }
        }

        public static VoiceController EnsureInstance()
        {
            if (_instance != null) return _instance;

            _instance = FindAnyObjectByType<VoiceController>();
            if (_instance != null) return _instance;

            GameObject go = new GameObject("GameVoiceController");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
            _instance = go.AddComponent<VoiceController>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeSources();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void InitializeSources()
        {
            if (_sourceA == null)
            {
                GameObject childA = new GameObject("VoiceChannel_A");
                childA.transform.SetParent(transform, false);
                _sourceA = childA.AddComponent<AudioSource>();
                ConfigureVoiceSource(_sourceA);
            }

            if (_sourceB == null)
            {
                GameObject childB = new GameObject("VoiceChannel_B");
                childB.transform.SetParent(transform, false);
                _sourceB = childB.AddComponent<AudioSource>();
                ConfigureVoiceSource(_sourceB);
            }
        }

        private static void ConfigureVoiceSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D voice / dialogue
            source.loop = false;
        }

        public void PlayVoice(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null || isMuted) return;

            InitializeSources();

            // Select active source and fading source
            AudioSource incomingSource;
            AudioSource outgoingSource;

            if (_activeSource == _sourceA)
            {
                incomingSource = _sourceB;
                outgoingSource = _sourceA;
            }
            else
            {
                incomingSource = _sourceA;
                outgoingSource = _sourceB;
            }

            // Adaptively interrupt outgoing source if it is playing
            if (outgoingSource != null && outgoingSource.isPlaying)
            {
                StartFadeOut(outgoingSource, DefaultInterruptFadeSeconds);
            }

            // Cancel any pending fade-out on incoming source
            CancelFade(incomingSource);

            float effectiveVolume = Mathf.Clamp01(volumeScale * voiceVolume * masterVolume);
            incomingSource.clip = clip;
            incomingSource.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
            incomingSource.volume = effectiveVolume;
            incomingSource.Play();

            _activeSource = incomingSource;
        }

        public void PlayVoiceCue(string cueId, string speakerKey = null, string emotionId = null, float volumeScale = 1f)
        {
            if (isMuted) return;

            // 1. Check if an audio clip exists in Resources or SfxLibrary
            AudioClip clip = null;

            if (!string.IsNullOrWhiteSpace(cueId))
            {
                clip = Resources.Load<AudioClip>("Voice/" + cueId) ??
                       Resources.Load<AudioClip>(cueId);
            }

            // 2. Check SfxLibrary UiStyles if available
            if (clip == null && !string.IsNullOrWhiteSpace(cueId) && SfxController.Instance?.Library != null)
            {
                UiAnimationSfxStyle style = SfxController.Instance.Library.FindUiStyle(cueId);
                if (style != null && style.ClickSfx != null && style.ClickSfx.HasClip())
                {
                    clip = style.ClickSfx.PickClip();
                }
            }

            // 3. Fall back to adaptive procedural speaker vocalization
            if (clip == null)
            {
                string key = ResolveProceduralKey(speakerKey, emotionId, cueId);
                clip = GetOrCreateProceduralClip(key, emotionId);
            }

            if (clip != null)
            {
                PlayVoice(clip, volumeScale);
            }
        }

        public void StopVoice(bool immediate = false)
        {
            if (_activeSource == null) return;

            if (immediate)
            {
                CancelFade(_sourceA);
                CancelFade(_sourceB);
                if (_sourceA != null) _sourceA.Stop();
                if (_sourceB != null) _sourceB.Stop();
                _activeSource = null;
            }
            else if (_activeSource.isPlaying)
            {
                StartFadeOut(_activeSource, DefaultStopFadeSeconds);
                _activeSource = null;
            }
        }

        private void StartFadeOut(AudioSource source, float duration)
        {
            if (source == null) return;

            if (source == _sourceA)
            {
                CancelFade(_sourceA);
                _fadeRoutineA = StartCoroutine(FadeOutRoutine(_sourceA, duration));
            }
            else if (source == _sourceB)
            {
                CancelFade(_sourceB);
                _fadeRoutineB = StartCoroutine(FadeOutRoutine(_sourceB, duration));
            }
        }

        private void CancelFade(AudioSource source)
        {
            if (source == _sourceA && _fadeRoutineA != null)
            {
                StopCoroutine(_fadeRoutineA);
                _fadeRoutineA = null;
            }
            else if (source == _sourceB && _fadeRoutineB != null)
            {
                StopCoroutine(_fadeRoutineB);
                _fadeRoutineB = null;
            }
        }

        private static IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            if (source == null || duration <= 0f)
            {
                source?.Stop();
                yield break;
            }

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration && source != null && source.isPlaying)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (source != null)
            {
                source.Stop();
                source.volume = startVolume;
            }
        }

        #region Procedural Speaker Vocalization Generator

        private static string ResolveProceduralKey(string speakerKey, string emotionId, string cueId)
        {
            string speaker = speakerKey ?? "speaker";
            string emotion = emotionId ?? "neutral";
            string cue = cueId ?? "dialogue";
            return $"{speaker}_{emotion}_{cue}".ToLowerInvariant();
        }

        private AudioClip GetOrCreateProceduralClip(string cacheKey, string emotionId)
        {
            if (_proceduralClipCache.TryGetValue(cacheKey, out AudioClip existing) && existing != null)
            {
                return existing;
            }

            AudioClip generated = GenerateEmotionVocalization(cacheKey, emotionId);
            _proceduralClipCache[cacheKey] = generated;
            return generated;
        }

        private static AudioClip GenerateEmotionVocalization(string name, string emotionId)
        {
            string emotion = (emotionId ?? string.Empty).ToLowerInvariant();

            // Tailor pitch, duration, and harmonic tone to character emotional context
            float fStart = 480f;
            float fEnd = 580f;
            float duration = 0.16f;
            float vibratoRate = 0f;

            switch (emotion)
            {
                case "grateful":
                case "cheerful":
                case "welcoming":
                    fStart = 520f;
                    fEnd = 720f;
                    duration = 0.18f;
                    break;
                case "teach":
                case "confident":
                    fStart = 440f;
                    fEnd = 660f;
                    duration = 0.15f;
                    break;
                case "scared":
                case "threat":
                    fStart = 340f;
                    fEnd = 310f;
                    duration = 0.22f;
                    vibratoRate = 18f;
                    break;
                case "warning":
                    fStart = 400f;
                    fEnd = 400f;
                    duration = 0.14f;
                    vibratoRate = 12f;
                    break;
                default:
                    fStart = 480f;
                    fEnd = 580f;
                    duration = 0.15f;
                    break;
            }

            return CreateVocalizationTone("Voice_FB_" + name, fStart, fEnd, duration, vibratoRate);
        }

        private static AudioClip CreateVocalizationTone(string name, float startFreq, float endFreq, float duration, float vibratoRate)
        {
            int sampleCount = Mathf.Max(128, Mathf.CeilToInt(duration * SampleRate));
            float[] data = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;

                // Frequency interpolation
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                if (vibratoRate > 0f)
                {
                    freq += Mathf.Sin(t * Mathf.PI * 2f * vibratoRate) * (startFreq * 0.08f);
                }

                phase += (2f * Mathf.PI * freq) / SampleRate;

                // Smooth bell envelope (attack, sustain, gentle release)
                float envelope = Mathf.Sin(t * Mathf.PI);

                // Fundamental tone + gentle warm 2nd harmonic
                float sample = (Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f) * envelope * 0.35f;
                data[i] = sample;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
