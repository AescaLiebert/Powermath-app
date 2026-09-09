using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Audio
{
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "PowerMath/Audio/Music Library")]
    public sealed class MusicLibraryDefinition : ScriptableObject
    {
        private const int SampleRate = 22050;

        [Header("Core Tracks")]
        [SerializeField] private MusicTrackConfig loginMusic = new MusicTrackConfig(null, 0.85f, true);
        [SerializeField] private MusicTrackConfig defaultBattleMusic = new MusicTrackConfig(null, 0.85f, true);
        [SerializeField] private MusicTrackConfig bossBattleMusic = new MusicTrackConfig(null, 0.95f, true);

        [Header("Biome Tracks")]
        [Tooltip("Optional biome-specific battle themes.")]
        [SerializeField] private BiomeMusicBinding[] biomeBattleTracks = Array.Empty<BiomeMusicBinding>();

        [Header("Transition & Lerp Tuning")]
        [Range(0.1f, 5f)]
        [Tooltip("Duration in seconds for standard crossfades between tracks.")]
        [SerializeField] private float defaultCrossfadeDuration = 1.2f;

        [Range(0.1f, 5f)]
        [Tooltip("Duration in seconds for Big Boss interruption transition.")]
        [SerializeField] private float bossInterruptDuration = 0.8f;

        [Range(0.05f, 0.5f)]
        [Tooltip("Target volume multiplier when ducked by Question Sequence / YouTube (-80% to -90% volume reduction = 0.10 to 0.20).")]
        [SerializeField] private float duckVolumeFactor = 0.15f;

        [Range(0.1f, 3f)]
        [Tooltip("Duration in seconds to smoothly lerp volume in and out of ducking.")]
        [SerializeField] private float duckFadeDuration = 0.75f;

        // Runtime synthesized fallback cache
        private static AudioClip _fallbackLoginClip;
        private static AudioClip _fallbackBattleClip;
        private static AudioClip _fallbackBossClip;

        public MusicTrackConfig LoginMusic => loginMusic;
        public MusicTrackConfig DefaultBattleMusic => defaultBattleMusic;
        public MusicTrackConfig BossBattleMusic => bossBattleMusic;
        public IReadOnlyList<BiomeMusicBinding> BiomeBattleTracks => biomeBattleTracks ?? Array.Empty<BiomeMusicBinding>();

        public float DefaultCrossfadeDuration => Mathf.Max(0.05f, defaultCrossfadeDuration);
        public float BossInterruptDuration => Mathf.Max(0.05f, bossInterruptDuration);
        public float DuckVolumeFactor => Mathf.Clamp(duckVolumeFactor, 0.05f, 0.5f);
        public float DuckFadeDuration => Mathf.Max(0.05f, duckFadeDuration);

        public MusicTrackConfig ResolveBattleTrack(string biomeId)
        {
            if (!string.IsNullOrEmpty(biomeId) && biomeBattleTracks != null)
            {
                for (int i = 0; i < biomeBattleTracks.Length; i++)
                {
                    BiomeMusicBinding binding = biomeBattleTracks[i];
                    if (binding != null && string.Equals(binding.BiomeId, biomeId, StringComparison.OrdinalIgnoreCase))
                    {
                        if (binding.Track != null && binding.Track.Clip != null)
                        {
                            return binding.Track;
                        }
                    }
                }
            }

            return defaultBattleMusic;
        }

        public AudioClip GetLoginClipWithFallback()
        {
            if (loginMusic != null && loginMusic.Clip != null)
                return loginMusic.Clip;

            if (_fallbackLoginClip == null)
                _fallbackLoginClip = CreateSynthLoop("Fallback_LoginMusic", new[] { 261.63f, 329.63f, 392.00f, 523.25f }, 3.0f, 0.18f);

            return _fallbackLoginClip;
        }

        public AudioClip GetBattleClipWithFallback(string biomeId = null)
        {
            MusicTrackConfig config = ResolveBattleTrack(biomeId);
            if (config != null && config.Clip != null)
                return config.Clip;

            if (_fallbackBattleClip == null)
                _fallbackBattleClip = CreateSynthLoop("Fallback_BattleMusic", new[] { 220.00f, 261.63f, 329.63f, 440.00f }, 2.5f, 0.20f);

            return _fallbackBattleClip;
        }

        public AudioClip GetBossClipWithFallback()
        {
            if (bossBattleMusic != null && bossBattleMusic.Clip != null)
                return bossBattleMusic.Clip;

            if (_fallbackBossClip == null)
                _fallbackBossClip = CreateSynthLoop("Fallback_BossMusic", new[] { 110.00f, 130.81f, 164.81f, 220.00f }, 2.0f, 0.24f);

            return _fallbackBossClip;
        }

        private static AudioClip CreateSynthLoop(string name, float[] frequencies, float duration, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float componentAmp = amplitude / Mathf.Max(1, frequencies.Length);

            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                // Seamless looping window: sin^2 envelope
                float loopEnvelope = Mathf.Sin(progress * Mathf.PI);
                float value = 0f;

                for (int f = 0; f < frequencies.Length; f++)
                {
                    float freq = frequencies[f];
                    // Subtly modulate frequency for warmth
                    float modFreq = freq * (1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 2f * progress));
                    value += Mathf.Sin(2f * Mathf.PI * modFreq * i / SampleRate) * componentAmp;
                }

                samples[i] = value * loopEnvelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
