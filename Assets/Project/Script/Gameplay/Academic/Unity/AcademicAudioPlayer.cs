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
            _promotion = CreateTone("RankPromotion", 880f, 0.32f, 0.16f);
            _adjustment = CreateTone("RankAdjustment", 280f, 0.26f, 0.10f);
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
