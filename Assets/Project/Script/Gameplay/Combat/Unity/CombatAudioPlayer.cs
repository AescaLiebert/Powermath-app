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
        private readonly AudioClip _biomeTransition;
        private readonly BattleSfxLibraryDefinition _library;
        private readonly AudioClip[] _fallbackSwings;
        private readonly AudioClip[] _fallbackHits;
        private readonly AudioClip[] _fallbackClicks;
        private int _swingIndex;
        private int _hitIndex;
        private int _clickIndex;

        public CombatAudioPlayer(
            AudioSource source,
            BattleSfxLibraryDefinition library = null)
        {
            _source = source;
            _library = library ??
                Resources.Load<BattleSfxLibraryDefinition>("BattleSfxLibrary");
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _fallbackSwings = new[]
            {
                CreateSweep("CombatSwingA", 940f, 280f, 0.11f, 0.12f, 0.20f),
                CreateSweep("CombatSwingB", 780f, 220f, 0.13f, 0.11f, 0.16f),
                CreateSweep("CombatSwingC", 1120f, 360f, 0.09f, 0.10f, 0.24f)
            };
            _fallbackHits = new[]
            {
                CreateTransient("CombatHitA", 105f, 0.11f, 0.18f, 0.70f),
                CreateTransient("CombatHitB", 135f, 0.09f, 0.17f, 0.62f),
                CreateTransient("CombatHitC", 82f, 0.14f, 0.19f, 0.76f)
            };
            _fallbackClicks = new[]
            {
                CreateTone("CombatClickA", 560f, 0.035f, 0.030f),
                CreateTone("CombatClickB", 620f, 0.032f, 0.028f),
                CreateTone("CombatClickC", 510f, 0.038f, 0.030f)
            };
            _commit = _library?.Commit ?? CreateSweep("CombatCommit", 310f, 470f, 0.09f, 0.09f, 0f);
            _key = _library?.Keypad ?? _fallbackClicks[0];
            _success = _library?.Success ?? CreateHarmonicChord(
                "CombatSuccess", new[] { 523.25f, 659.25f, 783.99f }, 0.22f, 0.13f);
            _timeout = _library?.Failure ?? CreateSweep("CombatFailure", 300f, 145f, 0.24f, 0.12f, 0f);
            _hit = First(_library?.Hits) ?? _fallbackHits[0];
            _critical = _library?.Critical ?? CreateTransient("CombatCritical", 68f, 0.24f, 0.24f, 0.82f);
            _enemyAttack = _library?.EnemyAttack ?? CreateSweep("EnemyAttack", 210f, 72f, 0.24f, 0.18f, 0.12f);
            _defeat = _library?.NormalDeath ?? CreateSweep("EnemyDefeat", 510f, 125f, 0.32f, 0.13f, 0.05f);
            _biomeTransition = _library?.BiomeTransition ?? CreateHarmonicChord(
                "CombatBiomeTransition",
                new[] { 392f, 493.88f, 587.33f, 783.99f },
                0.75f,
                0.16f
            );
        }

        public void PlayPopUp()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayQuestion(PowerMath.Audio.QuestionSequenceSfxState.PopUp);
            else
                Play(_commit);
        }

        public void PlayCommit()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayQuestion(PowerMath.Audio.QuestionSequenceSfxState.KeypadSubmit);
            else
                Play(_commit);
        }

        public void PlayKey()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayQuestion(PowerMath.Audio.QuestionSequenceSfxState.KeypadTap);
            else
                Play(_key);
        }

        public void PlaySuccess()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayQuestion(PowerMath.Audio.QuestionSequenceSfxState.ResultSuccess);
            else
                Play(_success);
        }

        public void PlayTimeout()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayQuestion(PowerMath.Audio.QuestionSequenceSfxState.ResultFail);
            else
                Play(_timeout);
        }

        public void PlayHit(bool critical)
        {
            if (PowerMath.Audio.SfxController.Instance != null)
            {
                PowerMath.Audio.SfxController.Instance.PlayPlayer(critical
                    ? PowerMath.Audio.PlayerSfxState.CriticalHit
                    : PowerMath.Audio.PlayerSfxState.Hit);
                return;
            }

            if (critical)
            {
                Play(_critical);
                return;
            }

            PlayVariant(_library?.Hits, _fallbackHits, ref _hitIndex, _hit, true);
        }

        public void PlayEnemyAttack()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayEnemy(PowerMath.Audio.EnemySfxState.Attack);
            else
                Play(_enemyAttack);
        }

        public void PlayDefeat()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayBattle(PowerMath.Audio.BattleSfxState.RunDefeat);
            else
                Play(_defeat);
        }

        public void PlayBiomeTransition()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayBattle(PowerMath.Audio.BattleSfxState.BiomeTransition);
            else
                Play(_biomeTransition);
        }

        public void PlaySwing()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayPlayer(PowerMath.Audio.PlayerSfxState.AttackSwing);
            else
                PlayVariant(_library?.SwordSwings, _fallbackSwings, ref _swingIndex, _commit, true);
        }

        public void PlayActorClick()
        {
            if (PowerMath.Audio.SfxController.Instance != null)
                PowerMath.Audio.SfxController.Instance.PlayBattle(PowerMath.Audio.BattleSfxState.ActorClick);
            else
                PlayVariant(_library?.ActorClicks, _fallbackClicks, ref _clickIndex, _key, false);
        }

        public void PlayDeath(bool major)
        {
            if (PowerMath.Audio.SfxController.Instance != null)
            {
                PowerMath.Audio.SfxController.Instance.PlayEnemy(PowerMath.Audio.EnemySfxState.Die, isMajorDeath: major);
                return;
            }

            Play(major
                ? _library?.MajorDeath ?? _critical
                : _library?.NormalDeath ?? _defeat);
        }

        private void Play(AudioClip clip)
        {
            if (_source != null && clip != null)
            {
                _source.PlayOneShot(clip);
            }
        }

        private void PlayVariant(
            AudioClip[] clips,
            AudioClip[] generatedFallbacks,
            ref int index,
            AudioClip fallback,
            bool varyPitch)
        {
            AudioClip[] candidates = HasClip(clips) ? clips : generatedFallbacks;
            AudioClip clip = First(candidates);
            int selectedIndex = index++;
            if (candidates != null && candidates.Length > 0)
            {
                int attempts = candidates.Length;
                while (attempts-- > 0)
                {
                    clip = candidates[selectedIndex++ % candidates.Length];
                    if (clip != null) break;
                }
            }
            if (clip == null) clip = fallback;
            if (_source == null || clip == null) return;
            float originalPitch = _source.pitch;
            if (varyPitch)
                _source.pitch = 0.97f + (index % 3) * 0.03f;
            _source.PlayOneShot(clip);
            _source.pitch = originalPitch;
        }

        private static bool HasClip(AudioClip[] clips) => First(clips) != null;

        private static AudioClip First(AudioClip[] clips)
        {
            if (clips == null) return null;
            for (int index = 0; index < clips.Length; index++)
                if (clips[index] != null) return clips[index];
            return null;
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

        private static AudioClip CreateHarmonicChord(
            string name,
            float[] frequencies,
            float duration,
            float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float componentAmplitude = amplitude / Mathf.Max(1, frequencies.Length);
            for (int index = 0; index < sampleCount; index++)
            {
                float progress = index / (float)sampleCount;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float value = 0f;
                for (int frequencyIndex = 0; frequencyIndex < frequencies.Length; frequencyIndex++)
                {
                    value += Mathf.Sin(
                        2f * Mathf.PI * frequencies[frequencyIndex] * index / SampleRate
                    ) * componentAmplitude * envelope;
                }
                samples[index] = value;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSweep(
            string name,
            float startFrequency,
            float endFrequency,
            float duration,
            float amplitude,
            float noiseMix)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float progress = index / (float)sampleCount;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.35f);
                float noise = PseudoNoise(index) * noiseMix;
                samples[index] = (Mathf.Sin(phase) * (1f - noiseMix) + noise) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateTransient(
            string name,
            float bodyFrequency,
            float duration,
            float amplitude,
            float noiseMix)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float progress = index / (float)sampleCount;
                float envelope = Mathf.Exp(-7f * progress);
                float body = Mathf.Sin(2f * Mathf.PI * bodyFrequency * index / SampleRate);
                samples[index] = (body * (1f - noiseMix) + PseudoNoise(index) * noiseMix) *
                    amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float PseudoNoise(int index)
        {
            float value = Mathf.Sin(index * 12.9898f + 78.233f) * 43758.5453f;
            return (value - Mathf.Floor(value)) * 2f - 1f;
        }
    }
}
