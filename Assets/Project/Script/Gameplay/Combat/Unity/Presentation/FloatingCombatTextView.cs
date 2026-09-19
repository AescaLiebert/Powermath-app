using TMPro;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class FloatingCombatTextView : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("TMP component for the primary foreground text.")]
        [SerializeField] private TextMeshProUGUI textComponent;

        [Tooltip("Optional back drop shadow TMP component (second layer).")]
        [SerializeField] private TextMeshProUGUI shadowTextComponent;

        [Tooltip("Optional image icon displayed alongside combat text (e.g. crit icon).")]
        [SerializeField] private UnityEngine.UI.Image iconImage;

        [Tooltip("CanvasGroup used by the Pop/Hold/Slide-Fade lifecycle.")]
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform _rectTransform;

        public RectTransform RectTransform => _rectTransform;
        public TextMeshProUGUI TextComponent => textComponent;
        public TextMeshProUGUI ShadowTextComponent => shadowTextComponent;
        public UnityEngine.UI.Image IconImage => iconImage;
        public bool HasShadow => shadowTextComponent != null;

        private void Awake()
        {
            CacheComponents();
        }

        public void ConfigureRuntimeComponents(
            TextMeshProUGUI text,
            CanvasGroup group,
            TextMeshProUGUI shadow = null,
            UnityEngine.UI.Image icon = null)
        {
            textComponent = text;
            canvasGroup = group;
            shadowTextComponent = shadow;
            iconImage = icon;
            CacheComponents();
        }

        public void Apply(string value, Color color, float fontSize, Vector2 position, Sprite icon = null)
        {
            CacheComponents();
            if (textComponent != null)
            {
                textComponent.text = value ?? string.Empty;
                textComponent.color = color;
                textComponent.fontSize = fontSize;
            }

            if (shadowTextComponent != null)
            {
                shadowTextComponent.text = value ?? string.Empty;
                shadowTextComponent.fontSize = fontSize;
            }

            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }

            _rectTransform.anchoredPosition = position;
            _rectTransform.localScale = Vector3.one * 0.65f;
            _rectTransform.localEulerAngles = Vector3.zero;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
        }

        public void SetFrame(float scale, float alpha, Vector2 position)
        {
            SetFrame(Vector3.one * scale, alpha, position, 0f, null);
        }

        public void SetFrame(Vector3 scale, float alpha, Vector2 position, float rotationZ = 0f, Color? colorOverride = null)
        {
            _rectTransform.localScale = scale;
            _rectTransform.anchoredPosition = position;
            _rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
            if (colorOverride.HasValue && textComponent != null)
            {
                textComponent.color = colorOverride.Value;
            }
        }

        public void ResetForPool()
        {
            CacheComponents();
            if (textComponent != null) textComponent.text = string.Empty;
            if (shadowTextComponent != null) shadowTextComponent.text = string.Empty;
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.gameObject.SetActive(false);
            }
            _rectTransform.localScale = Vector3.one;
            _rectTransform.localEulerAngles = Vector3.zero;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void CacheComponents()
        {
            if (_rectTransform == null) _rectTransform = (RectTransform)transform;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (iconImage == null)
            {
                // First try direct child search by name 'IconImage'
                Transform iconT = transform.Find("IconImage");
                if (iconT == null && transform.parent != null)
                {
                    iconT = transform.parent.Find("IconImage");
                }
                if (iconT != null)
                {
                    iconImage = iconT.GetComponent<UnityEngine.UI.Image>();
                }
                if (iconImage == null)
                {
                    iconImage = GetComponentInChildren<UnityEngine.UI.Image>(true);
                }
            }

            if (textComponent == null || shadowTextComponent == null)
            {
                var tmpComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpComponents.Length > 1)
                {
                    TextMeshProUGUI foreground = null;
                    TextMeshProUGUI shadow = null;

                    foreach (var tmp in tmpComponents)
                    {
                        string objName = tmp.gameObject.name.ToLowerInvariant();
                        if (objName.Contains("shadow"))
                        {
                            shadow = tmp;
                        }
                        else if (tmp.transform != transform)
                        {
                            foreground = tmp;
                        }
                    }

                    if (foreground == null)
                    {
                        foreach (var tmp in tmpComponents)
                        {
                            if (tmp.transform != transform)
                            {
                                foreground = tmp;
                                break;
                            }
                        }
                    }

                    if (shadow == null)
                    {
                        foreach (var tmp in tmpComponents)
                        {
                            if (tmp != foreground)
                            {
                                shadow = tmp;
                                break;
                            }
                        }
                    }

                    if (textComponent == null) textComponent = foreground ?? tmpComponents[0];
                    if (shadowTextComponent == null) shadowTextComponent = shadow;
                }
                else if (tmpComponents.Length == 1)
                {
                    if (textComponent == null) textComponent = tmpComponents[0];
                }
            }
        }
    }
}
