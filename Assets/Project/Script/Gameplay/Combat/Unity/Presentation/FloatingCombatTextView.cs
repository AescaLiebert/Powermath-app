using TMPro;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class FloatingCombatTextView : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("TMP component edited on the reusable FCT prefab.")]
        [SerializeField] private TextMeshProUGUI textComponent;

        [Tooltip("CanvasGroup used by the Pop/Hold/Slide-Fade lifecycle.")]
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform;
        public TextMeshProUGUI TextComponent => textComponent;

        private void Awake()
        {
            CacheComponents();
        }

        public void ConfigureRuntimeComponents(
            TextMeshProUGUI text,
            CanvasGroup group)
        {
            textComponent = text;
            canvasGroup = group;
            CacheComponents();
        }

        public void Apply(string value, Color color, float fontSize, Vector2 position)
        {
            CacheComponents();
            textComponent.text = value ?? string.Empty;
            textComponent.color = color;
            textComponent.fontSize = fontSize;
            _rectTransform.anchoredPosition = position;
            _rectTransform.localScale = Vector3.one * 0.65f;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
        }

        public void SetFrame(float scale, float alpha, Vector2 position)
        {
            _rectTransform.localScale = Vector3.one * scale;
            _rectTransform.anchoredPosition = position;
            canvasGroup.alpha = alpha;
        }

        public void ResetForPool()
        {
            CacheComponents();
            textComponent.text = string.Empty;
            _rectTransform.localScale = Vector3.one;
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void CacheComponents()
        {
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
