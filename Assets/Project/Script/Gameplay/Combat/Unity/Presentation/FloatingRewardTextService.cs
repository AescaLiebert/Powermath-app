using System.Collections;
using System.Globalization;
using UnityEngine;
using TMPro;

namespace PowerMath.Gameplay.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class FloatingRewardTextService : MonoBehaviour
    {
        private RectTransform _overlayRoot;
        private Camera _uiCamera;
        private FloatingRewardTextStyleDefinition _style;
        private FloatingRewardTextPool _pool;
        private bool _reducedMotion;
        private TMP_FontAsset _defaultFont;

        public bool IsReady => _pool != null && _overlayRoot != null;
        public int ActiveCount => _pool?.ActiveCount ?? 0;
        public int AvailableCount => _pool?.AvailableCount ?? 0;

        public void Initialize(
            RectTransform overlayRoot,
            Camera uiCamera,
            FloatingRewardTextView prefab,
            FloatingRewardTextStyleDefinition style,
            bool reducedMotion)
        {
            _overlayRoot = overlayRoot;
            _uiCamera = uiCamera;
            _style = style;
            _reducedMotion = reducedMotion;
            int capacity = style == null ? 8 : style.PoolCapacity;
            _pool = new FloatingRewardTextPool(overlayRoot, prefab, capacity);

            if (_style?.FontAsset == null)
            {
                _defaultFont = TMP_Settings.defaultFontAsset;
            }
        }

        public FloatingRewardTextView Spawn(RewardCurrencyKind kind, long amount, ICombatAnchor target, int spawnOrdinal = 0)
        {
            if (!IsReady || target == null || amount <= 0) return null;
            if (!target.TryGetLocalPoint(_overlayRoot, _uiCamera, out Vector2 point)) return null;
            return SpawnInternal(kind, amount, point, spawnOrdinal);
        }

        public FloatingRewardTextView Spawn(RewardCurrencyKind kind, long amount, Vector2 screenOrWorldPoint, int spawnOrdinal = 0)
        {
            if (!IsReady || amount <= 0) return null;
            Vector2 localPoint;
            if (_uiCamera != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _overlayRoot,
                    screenOrWorldPoint,
                    _uiCamera,
                    out localPoint);
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _overlayRoot,
                    screenOrWorldPoint,
                    null,
                    out localPoint);
            }
            return SpawnInternal(kind, amount, localPoint, spawnOrdinal);
        }

        private FloatingRewardTextView SpawnInternal(RewardCurrencyKind kind, long amount, Vector2 point, int spawnOrdinal)
        {
            if (!_pool.TryAcquire(out FloatingRewardTextView view)) return null;

            float overlap = _style == null ? 28f : _style.OverlapOffset;
            int lane = spawnOrdinal % 3 - 1;
            point.x += lane * overlap;
            // Float slightly higher than standard damage so it doesn't overlap perfectly
            point.y += 35f;

            float jitterX = _style == null ? 28f : _style.SpawnJitterX;
            float jitterY = _style == null ? 18f : _style.SpawnJitterY;
            point += new Vector2(
                UnityEngine.Random.Range(-jitterX, jitterX),
                UnityEngine.Random.Range(-jitterY, jitterY));
            point = ClampSpawnPoint(point);

            string text = "+" + amount.ToString(CultureInfo.InvariantCulture);
            Color color = _style != null ? _style.GetColor(kind) : GetFallbackColor(kind);
            float fontSize = _style == null ? 56f : _style.FontSize;

            TMP_FontAsset fontToUse = _style?.FontAsset != null ? _style.FontAsset : _defaultFont;
            if (fontToUse != null)
            {
                view.TextComponent.font = fontToUse;
                if (view.ShadowTextComponent != null)
                    view.ShadowTextComponent.font = fontToUse;
            }
            if (_style?.FontMaterial != null)
                view.TextComponent.fontSharedMaterial = _style.FontMaterial;
            if (view.ShadowTextComponent != null)
            {
                view.ShadowTextComponent.richText = true;
                view.ShadowTextComponent.extraPadding = true;
            }

            float startScale = _style == null ? 0.5f : _style.StartScale;
            Sprite currencyIcon = _style != null ? _style.GetIcon(kind) : null;
            view.Apply(text, color, fontSize, point, startScale, currencyIcon);
            StartCoroutine(PlayLifecycle(view, point, color));
            return view;
        }

        public void CancelAll()
        {
            StopAllCoroutines();
            _pool?.CancelAll();
        }

        private IEnumerator PlayLifecycle(FloatingRewardTextView view, Vector2 origin)
        {
            return PlayLifecycle(view, origin, Color.white);
        }

        private IEnumerator PlayLifecycle(
            FloatingRewardTextView view,
            Vector2 origin,
            Color targetColor)
        {
            float pop = _style == null ? 0.16f : _style.PopSeconds;
            float flightDuration = _style == null ? 0.52f : _style.DropSeconds;
            float hitHold = _style == null ? 0.06f : _style.PopHoldSeconds;
            float offscreenPadding = _style == null ? 72f : _style.OffscreenPadding;
            float burstHeight = _style == null ? 72f : _style.BurstHeight;
            float dropDistance = _style == null ? 104f : _style.DropDistance;
            float horizontalDistance = _style == null ? 150f : _style.FallHorizontalDistance;
            float kickAngle = _style == null ? 9f : _style.RotationalKick;
            float flashDuration = _style == null ? 0.08f : _style.FlashSeconds;
            float factor = _style == null ? 1.15f : _style.SquashStretchFactor;

            if (_reducedMotion)
            {
                pop = Mathf.Min(pop, 0.08f);
                flightDuration = Mathf.Min(flightDuration, 0.20f);
                hitHold = Mathf.Min(hitHold, 0.025f);
                burstHeight = 0f;
                dropDistance = 0f;
                horizontalDistance = 0f;
                kickAngle = 0f;
                flashDuration = 0f;
                factor = 0f;
            }

            float arcDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float rotationDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float maxRot = kickAngle * UnityEngine.Random.Range(0.75f, 1f) * rotationDir;

            try
            {
                // Contact hold only; position begins moving as soon as the hold releases.
                float elapsed = 0f;
                while (elapsed < hitHold)
                {
                    float delta = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                    elapsed += delta;
                    float holdT = hitHold <= 0f ? 1f : Mathf.Clamp01(elapsed / hitHold);
                    // Hold starts compressed tighter so the burst pop has more contrast.
                    view.SetFrame(Vector3.one * Mathf.Lerp(0.50f, 0.78f, holdT), 1f, origin, 0f, Color.white);
                    yield return null;
                }

                dropDistance = ResolveDropDistance(origin.y, dropDistance, offscreenPadding);
                CalculateJump(burstHeight, dropDistance, flightDuration, out float launchVelocityY, out float gravity);
                float horizontalVelocity = flightDuration <= 0f
                    ? 0f
                    : horizontalDistance / flightDuration * arcDir;
                elapsed = 0f;
                while (elapsed < flightDuration)
                {
                    float delta = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                    elapsed += delta;
                    float time = Mathf.Min(elapsed, flightDuration);
                    float t = flightDuration <= 0f ? 1f : Mathf.Clamp01(time / flightDuration);
                    float currentX = origin.x + horizontalVelocity * time;
                    float currentY = origin.y + launchVelocityY * time - 0.5f * gravity * time * time;
                    float currentRot = Mathf.Lerp(0f, maxRot * 2.2f, t);
                    float alpha = t < 0.78f ? 1f : Mathf.InverseLerp(1f, 0.78f, t);
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

        private static Color GetFallbackColor(RewardCurrencyKind kind)
        {
            return kind switch
            {
                RewardCurrencyKind.RankSilver => new Color(0.76f, 0.82f, 0.89f, 1f),
                RewardCurrencyKind.RankGold => new Color(1f, 0.78f, 0.25f, 1f),
                RewardCurrencyKind.RankDiamond => new Color(0.41f, 0.87f, 0.95f, 1f),
                RewardCurrencyKind.PowerCoin => new Color(1f, 0.55f, 0.05f, 1f),
                _ => Color.white
            };
        }

        private void OnDisable()
        {
            CancelAll();
        }
    }
}
