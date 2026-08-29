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
            if (_routine != null) StopCoroutine(_routine);
            _worldRoot.anchoredPosition = _authoredOrigin;
            _routine = StartCoroutine(ImpulseRoutine());
        }

        public void CancelAndRestore()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_worldRoot != null) _worldRoot.anchoredPosition = _authoredOrigin;
        }

        private IEnumerator ImpulseRoutine()
        {
            float duration = _profile == null ? 0.18f : _profile.CriticalImpulseSeconds;
            float amplitude = _profile == null ? 14f : _profile.CriticalImpulseAmplitude;
            int oscillations = _profile == null ? 2 : _profile.CriticalImpulseOscillations;
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
                float x = Mathf.Sin(t * Mathf.PI * 2f * oscillations) * amplitude * envelope;
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
