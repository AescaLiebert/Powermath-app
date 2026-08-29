using System.Collections;
using System.Globalization;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public enum FloatingCombatTextSemantic
    {
        Damage,
        Healing,
        Rank
    }

    public readonly struct FloatingCombatTextRequest
    {
        public FloatingCombatTextRequest(
            string presentationId,
            FloatingCombatTextSemantic semantic,
            int acceptedValue,
            ICombatAnchor target,
            bool isCritical,
            int spawnOrdinal)
        {
            PresentationId = presentationId ?? string.Empty;
            Semantic = semantic;
            AcceptedValue = acceptedValue;
            Target = target;
            IsCritical = isCritical;
            SpawnOrdinal = spawnOrdinal;
        }

        public string PresentationId { get; }
        public FloatingCombatTextSemantic Semantic { get; }
        public int AcceptedValue { get; }
        public ICombatAnchor Target { get; }
        public bool IsCritical { get; }
        public int SpawnOrdinal { get; }
    }

    [DisallowMultipleComponent]
    public sealed class FloatingCombatTextService : MonoBehaviour
    {
        private RectTransform _overlayRoot;
        private Camera _uiCamera;
        private FloatingCombatTextStyleDefinition _style;
        private FloatingCombatTextPool _pool;
        private bool _reducedMotion;

        public bool IsReady => _pool != null && _overlayRoot != null;

        public void Initialize(
            RectTransform overlayRoot,
            Camera uiCamera,
            FloatingCombatTextView prefab,
            FloatingCombatTextStyleDefinition style,
            bool reducedMotion)
        {
            _overlayRoot = overlayRoot;
            _uiCamera = uiCamera;
            _style = style;
            _reducedMotion = reducedMotion;
            int capacity = style == null ? 8 : style.PoolCapacity;
            _pool = new FloatingCombatTextPool(overlayRoot, prefab, capacity);
        }

        public void Spawn(FloatingCombatTextRequest request)
        {
            if (!IsReady || request.Target == null || request.AcceptedValue <= 0 ||
                string.IsNullOrWhiteSpace(request.PresentationId))
                return;
            if (!request.Target.TryGetLocalPoint(_overlayRoot, _uiCamera, out Vector2 point))
                return;
            if (!_pool.TryAcquire(out FloatingCombatTextView view)) return;

            float overlap = _style == null ? 24f : _style.OverlapOffset;
            int lane = request.SpawnOrdinal % 3 - 1;
            point.x += lane * overlap;
            string value = request.IsCritical
                ? "CRITICAL\n-" + request.AcceptedValue.ToString(CultureInfo.InvariantCulture)
                : "-" + request.AcceptedValue.ToString(CultureInfo.InvariantCulture);
            Color color = request.IsCritical
                ? (_style == null ? new Color(1f, 0.78f, 0.18f, 1f) : _style.CriticalColor)
                : (_style == null ? Color.white : _style.NormalColor);
            float fontSize = _style == null ? 42f : _style.FontSize;
            if (_style?.FontAsset != null)
                view.TextComponent.font = _style.FontAsset;
            if (_style?.FontMaterial != null)
                view.TextComponent.fontSharedMaterial = _style.FontMaterial;
            view.Apply(value, color, fontSize, point);
            StartCoroutine(PlayLifecycle(view, point));
        }

        public void CancelAll()
        {
            StopAllCoroutines();
            _pool?.CancelAll();
        }

        private IEnumerator PlayLifecycle(FloatingCombatTextView view, Vector2 origin)
        {
            float pop = _style == null ? 0.12f : _style.PopSeconds;
            float hold = _style == null ? 0.24f : _style.HoldSeconds;
            float exit = _style == null ? 0.34f : _style.ExitSeconds;
            float slide = _style == null ? 74f : _style.SlideDistance;
            if (_reducedMotion)
            {
                pop = Mathf.Min(pop, 0.06f);
                exit = Mathf.Min(exit, 0.12f);
                slide = 0f;
            }

            float elapsed = 0f;
            while (elapsed < pop)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = pop <= 0f ? 1f : Mathf.Clamp01(elapsed / pop);
                float scale = Mathf.Lerp(0.65f, 1.12f, t);
                view.SetFrame(scale, t, origin);
                yield return null;
            }
            view.SetFrame(1f, 1f, origin);
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            elapsed = 0f;
            while (elapsed < exit)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = exit <= 0f ? 1f : Mathf.Clamp01(elapsed / exit);
                view.SetFrame(1f, 1f - t,
                    origin + Vector2.up * Mathf.Lerp(0f, slide, t));
                yield return null;
            }
            _pool.Release(view);
        }

        private void OnDisable()
        {
            CancelAll();
        }
    }
}
