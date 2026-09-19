using System.Collections;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class CombatWorldImpulsePlayer : MonoBehaviour
    {
        private RectTransform _worldRoot;
        private Vector2 _authoredOrigin;
        private bool _reducedMotion;
        private CombatJuiceProfileDefinition _profile;
        private Coroutine _routine;

        public void Initialize(
            RectTransform worldRoot,
            bool reducedMotion,
            CombatJuiceProfileDefinition profile = null)
        {
            _worldRoot = worldRoot;
            _authoredOrigin = worldRoot == null ? Vector2.zero : worldRoot.anchoredPosition;
            _reducedMotion = reducedMotion;
            _profile = profile;
        }

        public void PlayCritical()
        {
            if (_worldRoot == null || _reducedMotion) return;
            PlayImpulse(
                _profile == null ? 0.18f : _profile.CriticalImpulseSeconds,
                _profile == null ? 14f : _profile.CriticalImpulseAmplitude,
                _profile == null ? 2 : _profile.CriticalImpulseOscillations,
                1f);
        }

        public void PlayNormal()
        {
            if (_worldRoot == null || _reducedMotion) return;
            PlayImpulse(
                _profile == null ? 0.12f : _profile.NormalImpulseSeconds,
                _profile == null ? 6f : _profile.NormalImpulseAmplitude,
                _profile == null ? 1 : _profile.NormalImpulseOscillations,
                1f);
        }

        public void PlayPlayerDamage()
        {
            if (_worldRoot == null || _reducedMotion) return;
            PlayImpulse(
                _profile == null ? 0.14f : _profile.PlayerDamageImpulseSeconds,
                _profile == null ? 8f : _profile.PlayerDamageImpulseAmplitude,
                _profile == null ? 1 : _profile.PlayerDamageImpulseOscillations,
                -1f);
        }

        public void CancelAndRestore()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_worldRoot != null) _worldRoot.anchoredPosition = _authoredOrigin;
        }

        private void PlayImpulse(
            float duration,
            float amplitude,
            int oscillations,
            float direction)
        {
            if (_routine != null) StopCoroutine(_routine);
            _worldRoot.anchoredPosition = _authoredOrigin;
            _routine = StartCoroutine(ImpulseRoutine(
                duration,
                amplitude,
                Mathf.Max(1, oscillations),
                Mathf.Sign(direction)));
        }

        private IEnumerator ImpulseRoutine(
            float duration,
            float amplitude,
            int oscillations,
            float direction)
        {
            if (duration <= 0f)
            {
                _routine = null;
                yield break;
            }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = 1f - t;
                float x = Mathf.Sin(t * Mathf.PI * 2f * oscillations) *
                    amplitude * envelope * direction;
                _worldRoot.anchoredPosition = _authoredOrigin + Vector2.right * x;
                yield return null;
            }
            _worldRoot.anchoredPosition = _authoredOrigin;
            _routine = null;
        }

        private void OnDisable()
        {
            CancelAndRestore();
        }
    }
}
