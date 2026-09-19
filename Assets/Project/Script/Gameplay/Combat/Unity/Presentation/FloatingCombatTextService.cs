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
        public int ActiveCount => _pool?.ActiveCount ?? 0;
        public int AvailableCount => _pool?.AvailableCount ?? 0;

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

        public FloatingCombatTextView Spawn(FloatingCombatTextRequest request)
        {
            if (!IsReady || request.Target == null || request.AcceptedValue <= 0 ||
                string.IsNullOrWhiteSpace(request.PresentationId))
                return null;
            if (!request.Target.TryGetLocalPoint(_overlayRoot, _uiCamera, out Vector2 point))
                return null;
            if (!_pool.TryAcquire(out FloatingCombatTextView view)) return null;

            float overlap = _style == null ? 24f : _style.OverlapOffset;
            int lane = request.SpawnOrdinal % 3 - 1;
            point.x += lane * overlap;

            float jitterX = _style == null ? 26f : _style.SpawnJitterX;
            float jitterY = _style == null ? 16f : _style.SpawnJitterY;
            point += new Vector2(
                UnityEngine.Random.Range(-jitterX, jitterX),
                UnityEngine.Random.Range(-jitterY, jitterY));
            point = ClampSpawnPoint(point);

            string value = "-" + request.AcceptedValue.ToString(CultureInfo.InvariantCulture);
            Color color = request.IsCritical
                ? (_style == null ? new Color(1f, 0.78f, 0.18f, 1f) : _style.CriticalColor)
                : (_style == null ? Color.white : _style.NormalColor);
            float fontSize = _style == null ? 42f : _style.FontSize;
            if (request.IsCritical) fontSize *= 1.12f;

            if (_style?.FontAsset != null)
            {
                view.TextComponent.font = _style.FontAsset;
                if (view.ShadowTextComponent != null)
                    view.ShadowTextComponent.font = _style.FontAsset;
            }
            if (_style?.FontMaterial != null)
                view.TextComponent.fontSharedMaterial = _style.FontMaterial;
            if (view.ShadowTextComponent != null)
            {
                view.ShadowTextComponent.richText = true;
                view.ShadowTextComponent.extraPadding = true;
            }

            Sprite critIcon = request.IsCritical ? _style?.CritIcon : null;
            view.Apply(value, color, fontSize, point, critIcon);
            StartCoroutine(PlayLifecycle(view, point, color, request.IsCritical));
            return view;
        }

        public void CancelAll()
        {
            StopAllCoroutines();
            _pool?.CancelAll();
        }

        private IEnumerator PlayLifecycle(FloatingCombatTextView view, Vector2 origin)
        {
            return PlayLifecycle(view, origin, Color.white, false);
        }

        private IEnumerator PlayLifecycle(
            FloatingCombatTextView view,
            Vector2 origin,
            Color targetColor,
            bool isCritical)
        {
            float pop = _style == null ? 0.14f : _style.PopSeconds;
            float flightDuration = _style == null ? 0.52f : _style.DropSeconds;
            float hitHold = _style == null ? 0.06f : _style.PopHoldSeconds;
            float fadeStart = _style == null ? 0.85f : _style.FadeStartNormalized;
            float offscreenPadding = _style == null ? 72f : _style.OffscreenPadding;
            float burstHeight = _style == null ? 62f : _style.BurstHeight;
            float dropDistance = _style == null ? 104f : _style.DropDistance;
            float fallHorizontalDistance = _style == null ? 180f : _style.FallHorizontalDistance;
            float kickAngle = _style == null ? 10f : _style.RotationalKick;
            float flashDuration = _style == null ? 0.07f : _style.FlashSeconds;
            float factor = _style == null ? 1.35f : _style.SquashStretchFactor;

            if (isCritical)
            {
                burstHeight *= 1.25f;
                dropDistance *= 1.20f;
                factor *= 1.20f;
                kickAngle *= 1.25f;
            }

            if (_reducedMotion)
            {
                pop = Mathf.Min(pop, 0.08f);
                flightDuration = Mathf.Min(flightDuration, 0.20f);
                hitHold = Mathf.Min(hitHold, 0.025f);
                fadeStart = 1f;
                burstHeight = 0f;
                dropDistance = 0f;
                fallHorizontalDistance = 0f;
                kickAngle = 0f;
                flashDuration = 0f;
                factor = 0f;
            }

            // Arc and rotation choose their sides independently.
            float arcDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float rotationDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float maxRot = kickAngle * UnityEngine.Random.Range(0.75f, 1f) * rotationDir;

            try
            {
                // Contact hold: this is the FDT's hit-stop beat. Position does not ease toward an apex.
                float elapsed = 0f;
                while (elapsed < hitHold)
                {
                    float delta = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                    elapsed += delta;
                    float holdT = hitHold <= 0f ? 1f : Mathf.Clamp01(elapsed / hitHold);
                    // Hold starts compressed tighter so the burst pop has more contrast.
                    float holdScale = Mathf.Lerp(0.50f, 0.78f, holdT);
                    view.SetFrame(Vector3.one * holdScale, 1f, origin, 0f, Color.white);
                    yield return null;
                }

                if (!_reducedMotion)
                {
                    dropDistance = ResolveDropDistance(origin.y, dropDistance, offscreenPadding);
                }
                CalculateJump(burstHeight, dropDistance, flightDuration, out float launchVelocityY, out float gravity);
                elapsed = 0f;
                float horizontalVelocity = flightDuration <= 0f
                    ? 0f
                    : fallHorizontalDistance / flightDuration * arcDir;
                while (elapsed < flightDuration)
                {
                    float delta = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                    elapsed += delta;
                    float time = Mathf.Min(elapsed, flightDuration);
                    float t = flightDuration <= 0f ? 1f : Mathf.Clamp01(time / flightDuration);
                    float currentX = origin.x + horizontalVelocity * time;
                    float currentY = origin.y + launchVelocityY * time - 0.5f * gravity * time * time;
                    float currentRot = Mathf.Lerp(0f, maxRot * 2.2f, t);
                    float alpha = t < fadeStart ? 1f : Mathf.InverseLerp(1f, fadeStart, t);
                    float popT = pop <= 0f ? 1f : Mathf.Clamp01(time / pop);
                    // Snap up fast (0→0.55) with a bigger overshoot peak, then settle back to 1.
                    float popScale = popT < 0.55f
                        ? Mathf.Lerp(0.78f, 1.32f * Mathf.Max(1f, factor), popT / 0.55f)
                        : Mathf.Lerp(1.32f * Mathf.Max(1f, factor), 1f, (popT - 0.55f) / 0.45f);
                    Color currentColor = flashDuration > 0f && time < flashDuration
                        ? Color.Lerp(Color.white, targetColor, time / flashDuration)
                        : targetColor;
                    view.SetFrame(Vector3.one * popScale, alpha, new Vector2(currentX, currentY), currentRot, currentColor);
                    yield return null;
                }
            }
            finally
            {
                _pool.Release(view);
            }
        }

        private static void CalculateJump(
            float apexHeight,
            float dropDistance,
            float duration,
            out float launchVelocityY,
            out float gravity)
        {
            if (duration <= 0f || apexHeight <= 0f)
            {
                launchVelocityY = 0f;
                gravity = 0f;
                return;
            }

            float gravityRoot = (Mathf.Sqrt(2f * apexHeight) +
                Mathf.Sqrt(2f * (apexHeight + Mathf.Max(0f, dropDistance)))) / duration;
            gravity = gravityRoot * gravityRoot;
            launchVelocityY = Mathf.Sqrt(2f * gravity * apexHeight);
        }

        private Vector2 ClampSpawnPoint(Vector2 point)
        {
            if (_overlayRoot == null || _overlayRoot.rect.width <= 0f || _overlayRoot.rect.height <= 0f)
                return point;
            const float margin = 48f;
            point.x = Mathf.Clamp(point.x, _overlayRoot.rect.xMin + margin, _overlayRoot.rect.xMax - margin);
            point.y = Mathf.Clamp(point.y, _overlayRoot.rect.yMin + margin, _overlayRoot.rect.yMax - margin);
            return point;
        }

        private float ResolveDropDistance(float originY, float configuredDistance, float padding)
        {
            if (_overlayRoot == null || _overlayRoot.rect.height <= 0f) return configuredDistance;
            float distanceToOffscreen = originY - (_overlayRoot.rect.yMin - padding);
            return Mathf.Max(configuredDistance, distanceToOffscreen);
        }

        private void OnDisable()
        {
            CancelAll();
        }
    }
}
