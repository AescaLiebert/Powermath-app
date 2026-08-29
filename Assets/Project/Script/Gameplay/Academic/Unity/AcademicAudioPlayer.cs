using UnityEngine;

namespace PowerMath.Gameplay.Academic.Unity
{
    public sealed class AcademicAudioPlayer
    {
        private const int SampleRate = 22050;
        private readonly AudioSource _source;
        private readonly AudioClip _currency;
        private readonly AudioClip _promotion;
        private readonly AudioClip _adjustment;

        public AcademicAudioPlayer(AudioSource source)
        {
            _source = source;
            _currency = CreateTone("RankCurrency", 740f, 0.12f, 0.10f);
            _promotion = CreateFanfare("RankPromotion", 0.45f, 0.20f);
            _adjustment = CreateHarmonicChord("RankAdjustment", 330f, 440f, 0.35f, 0.14f);
        }

        public void PlayCurrency() => Play(_currency);
        public void PlayTransition(RankTransition transition) =>
            Play(transition.IsPromotion ? _promotion : _adjustment);

        private void Play(AudioClip clip)
        {
            if (_source != null && clip != null)
            {
                _source.PlayOneShot(clip);
            }
        }

        private static AudioClip CreateFanfare(
            string name,
            float duration,
            float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = duration / notes.Length;
            int noteSamples = Mathf.Max(1, Mathf.CeilToInt(SampleRate * noteDuration));

            for (int index = 0; index < sampleCount; index++)
            {
                int noteIndex = Mathf.Min(notes.Length - 1, index / noteSamples);
                float freq = notes[noteIndex];
                float totalFade = 1f - (index / (float)sampleCount);
                float noteProgress = (index % noteSamples) / (float)noteSamples;
                float noteEnvelope = 1f - (noteProgress * 0.4f);

                // Fundamental plus soft overtone
                float wave = Mathf.Sin(2f * Mathf.PI * freq * index / SampleRate) * 0.75f +
                             Mathf.Sin(2f * Mathf.PI * (freq * 2f) * index / SampleRate) * 0.25f;

                samples[index] = wave * amplitude * totalFade * noteEnvelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateHarmonicChord(
            string name,
            float freq1,
            float freq2,
            float duration,
            float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];

            for (int index = 0; index < sampleCount; index++)
            {
                float fade = 1f - index / (float)sampleCount;
                float wave = (Mathf.Sin(2f * Mathf.PI * freq1 * index / SampleRate) +
                              Mathf.Sin(2f * Mathf.PI * freq2 * index / SampleRate)) * 0.5f;
                samples[index] = wave * amplitude * fade;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateTone(
            string name,
            float frequency,
            float duration,
            float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float fade = 1f - index / (float)sampleCount;
                samples[index] = Mathf.Sin(
                    2f * Mathf.PI * frequency * index / SampleRate
                ) * amplitude * fade;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
