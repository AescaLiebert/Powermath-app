using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class CombatAudioPlayer
    {
        private const int SampleRate = 22050;
        private readonly AudioSource _source;
        private readonly AudioClip _commit;
        private readonly AudioClip _key;
        private readonly AudioClip _success;
        private readonly AudioClip _timeout;
        private readonly AudioClip _hit;
        private readonly AudioClip _critical;
        private readonly AudioClip _enemyAttack;
        private readonly AudioClip _defeat;

        public CombatAudioPlayer(AudioSource source)
        {
            _source = source;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _commit = CreateTone("CombatCommit", 330f, 0.08f, 0.10f);
            _key = CreateTone("CombatKey", 520f, 0.035f, 0.035f);
            _success = CreateTone("CombatSuccess", 660f, 0.14f, 0.12f);
            _timeout = CreateTone("CombatTimeout", 180f, 0.18f, 0.10f);
            _hit = CreateTone("CombatHit", 110f, 0.12f, 0.16f);
            _critical = CreateTone("CombatCritical", 90f, 0.20f, 0.22f);
            _enemyAttack = CreateTone("EnemyAttack", 70f, 0.22f, 0.18f);
            _defeat = CreateTone("EnemyDefeat", 440f, 0.30f, 0.14f);
        }

        public void PlayCommit() => Play(_commit);
        public void PlayKey() => Play(_key);
        public void PlaySuccess() => Play(_success);
        public void PlayTimeout() => Play(_timeout);
        public void PlayHit(bool critical) => Play(critical ? _critical : _hit);
        public void PlayEnemyAttack() => Play(_enemyAttack);
        public void PlayDefeat() => Play(_defeat);

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
