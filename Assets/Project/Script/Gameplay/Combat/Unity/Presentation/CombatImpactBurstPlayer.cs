using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PowerMath.Gameplay.Combat.Unity
{
    public enum CombatImpactBurstKind
    {
        Normal,
        Critical,
        PlayerDamage
    }

    [DisallowMultipleComponent]
    public sealed class CombatImpactBurstPlayer : MonoBehaviour
    {
        private const int RayCount = 6;

        private RectTransform _overlayRoot;
        private Camera _uiCamera;
        private CombatJuiceProfileDefinition _profile;
        private bool _reducedMotion;
        private RectTransform _burstRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _center;
        private Image _centerImage;
        private readonly RectTransform[] _rays = new RectTransform[RayCount];
        private readonly Image[] _rayImages = new Image[RayCount];
        private Coroutine _routine;

        public bool IsPlaying => _routine != null;

        public void Initialize(
            RectTransform overlayRoot,
            Camera uiCamera,
            CombatJuiceProfileDefinition profile,
            bool reducedMotion)
        {
            _overlayRoot = overlayRoot;
            _uiCamera = uiCamera;
            _profile = profile;
            _reducedMotion = reducedMotion;
            EnsureVisuals();
            HideAndRestore();
        }

        public void Play(ICombatAnchor anchor, CombatImpactBurstKind kind)
        {
            if (_overlayRoot == null || anchor == null) return;
            if (!anchor.TryGetLocalPoint(
                    _overlayRoot,
                    _uiCamera,
                    out Vector2 localPoint))
                return;

            EnsureVisuals();
            Cancel();
            Configure(kind);
            _burstRoot.anchoredPosition = localPoint;
            _burstRoot.gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _routine = StartCoroutine(BurstRoutine(kind));
        }

        public void Cancel()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            HideAndRestore();
        }

        private void EnsureVisuals()
        {
            if (_overlayRoot == null || _burstRoot != null) return;

            var root = new GameObject(
                "CombatImpactBurst",
                typeof(RectTransform),
                typeof(CanvasGroup));
            _burstRoot = root.GetComponent<RectTransform>();
            _burstRoot.SetParent(_overlayRoot, false);
            _burstRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _burstRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _burstRoot.pivot = new Vector2(0.5f, 0.5f);
            _burstRoot.sizeDelta = Vector2.zero;
            _burstRoot.SetAsLastSibling();

            _canvasGroup = root.GetComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            for (int index = 0; index < RayCount; index++)
            {
                var ray = new GameObject(
                    $"ImpactRay_{index + 1}",
                    typeof(RectTransform),
                    typeof(Image));
                RectTransform rect = ray.GetComponent<RectTransform>();
                rect.SetParent(_burstRoot, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    index * (360f / RayCount));
                _rays[index] = rect;
                _rayImages[index] = ray.GetComponent<Image>();
                _rayImages[index].raycastTarget = false;
            }

            var center = new GameObject(
                "ImpactCenter",
                typeof(RectTransform),
                typeof(Image));
            _center = center.GetComponent<RectTransform>();
            _center.SetParent(_burstRoot, false);
            _center.anchorMin = new Vector2(0.5f, 0.5f);
            _center.anchorMax = new Vector2(0.5f, 0.5f);
            _center.pivot = new Vector2(0.5f, 0.5f);
            _center.anchoredPosition = Vector2.zero;
            _center.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _centerImage = center.GetComponent<Image>();
            _centerImage.raycastTarget = false;
        }

        private void Configure(CombatImpactBurstKind kind)
        {
            Color color;
            float rayWidth;
            float rayLength;
            float centerSize;
            switch (kind)
            {
                case CombatImpactBurstKind.Critical:
                    color = new Color(1f, 0.78f, 0.16f, 1f);
                    rayWidth = 7f;
                    rayLength = 46f;
                    centerSize = 30f;
                    break;
                case CombatImpactBurstKind.PlayerDamage:
                    color = new Color(1f, 0.30f, 0.24f, 1f);
                    rayWidth = 6f;
                    rayLength = 38f;
                    centerSize = 26f;
                    break;
                default:
                    color = new Color(1f, 0.92f, 0.50f, 1f);
                    rayWidth = 5f;
                    rayLength = 32f;
                    centerSize = 22f;
                    break;
            }

            for (int index = 0; index < RayCount; index++)
            {
                _rays[index].sizeDelta = new Vector2(rayWidth, rayLength);
                _rays[index].localScale = Vector3.one;
                _rayImages[index].color = color;
            }
            _center.sizeDelta = new Vector2(centerSize, centerSize);
            _center.localScale = Vector3.one;
            _centerImage.color = Color.Lerp(color, Color.white, 0.55f);
        }

        private IEnumerator BurstRoutine(CombatImpactBurstKind kind)
        {
            float duration = ResolveDuration(kind);
            if (duration <= 0f)
            {
                HideAndRestore();
                _routine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float delta = Time.unscaledDeltaTime > 0f
                    ? Time.unscaledDeltaTime
                    : 0.05f;
                elapsed += delta;
                float t = Mathf.Clamp01(elapsed / duration);
                float fade = 1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.42f, 1f, t));
                _canvasGroup.alpha = fade;

                if (!_reducedMotion)
                {
                    float pop = 1f - Mathf.Pow(1f - t, 3f);
                    float settle = Mathf.Lerp(
                        1.12f,
                        1f,
                        Mathf.SmoothStep(0f, 1f, t));
                    float rayScale = Mathf.Lerp(0.20f, settle, pop);
                    for (int index = 0; index < RayCount; index++)
                        _rays[index].localScale = new Vector3(1f, rayScale, 1f);
                    float centerScale = Mathf.Lerp(
                        0.35f,
                        1.15f,
                        Mathf.Sin(t * Mathf.PI));
                    _center.localScale = Vector3.one * centerScale;
                }

                yield return null;
            }

            HideAndRestore();
            _routine = null;
        }

        private float ResolveDuration(CombatImpactBurstKind kind)
        {
            if (_profile == null)
            {
                return kind switch
                {
                    CombatImpactBurstKind.Critical => 0.26f,
                    CombatImpactBurstKind.PlayerDamage => 0.20f,
                    _ => 0.18f
                };
            }

            return kind switch
            {
                CombatImpactBurstKind.Critical => _profile.CriticalImpactBurstSeconds,
                CombatImpactBurstKind.PlayerDamage => _profile.PlayerDamageBurstSeconds,
                _ => _profile.NormalImpactBurstSeconds
            };
        }

        private void HideAndRestore()
        {
            if (_burstRoot == null) return;
            _burstRoot.localScale = Vector3.one;
            _burstRoot.localRotation = Quaternion.identity;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _burstRoot.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Cancel();
        }
    }
}
