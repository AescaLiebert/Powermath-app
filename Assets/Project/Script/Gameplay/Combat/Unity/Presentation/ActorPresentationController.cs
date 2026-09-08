using System;
using System.Collections;
using PowerMath.Gameplay.Combat.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PowerMath.Gameplay.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class ActorPresentationController : MonoBehaviour,
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Combat Anchors")]
        [Tooltip("Normalized anchor within the RectTransform for FCT spawn (0.5, 0.5 = center).")]
        [SerializeField] private Vector2 fctNormalizedAnchor = new Vector2(0.5f, 0.5f);
        [Tooltip("Pixel offset added to the FCT spawn position.")]
        [SerializeField] private Vector2 fctOffset = Vector2.zero;

        [Header("State Machine Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;
        [SerializeField] private Sprite hurtSprite;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Graphic _graphic;
        private Color _authoredColor = Color.white;
        private PresentationActor _actor;
        private Vector2 _authoredPosition;
        private Quaternion _authoredRotation;
        private bool _reducedMotion;
        private CombatJuiceProfileDefinition _profile;
        private bool _isPlaying;
        private int _playSessionId;
        private bool _whiteFlashTriggered;

        public event Action Clicked;
        public event Action DyingWhiteFlashReached;
        public event Action RewardDropTriggered;

        public ActorVisualState State { get; private set; } = ActorVisualState.Hidden;
        public bool IsPlayer => _actor == PresentationActor.Player;
        public bool IsIdle => State == ActorVisualState.Idle;
        public ICombatAnchor DamageTextAnchor { get; private set; }
        public Vector2 FctNormalizedAnchor => fctNormalizedAnchor;
        public Vector2 FctOffset => fctOffset;
        public Color AuthoredColor => _authoredColor;
        public Color CurrentColor => _graphic != null ? _graphic.color : _authoredColor;
        public Sprite IdleSprite => idleSprite;
        public Sprite AttackSprite => attackSprite;
        public Sprite HurtSprite => hurtSprite;

        public void ConfigureSprites(Sprite idle, Sprite attack = null, Sprite hurt = null)
        {
            idleSprite = idle;
            attackSprite = attack;
            hurtSprite = hurt;
            UpdateSprite();
        }

        public void Initialize(
            PresentationActor actor,
            bool reducedMotion,
            CombatJuiceProfileDefinition profile = null,
            Vector2? restPosition = null)
        {
            if (actor != PresentationActor.Player && actor != PresentationActor.Enemy)
                throw new ArgumentOutOfRangeException(nameof(actor));
            _actor = actor;
            _reducedMotion = reducedMotion;
            _profile = profile;
            _rectTransform = transform as RectTransform;
            if (_rectTransform == null)
                throw new InvalidOperationException("Actor presentation requires a RectTransform.");
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _graphic = GetComponent<Graphic>();
            if (_graphic != null)
            {
                _graphic.raycastTarget = true;
                _authoredColor = _graphic.color;
                if (_authoredColor.a <= 0.01f)
                    _authoredColor = new Color(_authoredColor.r, _authoredColor.g, _authoredColor.b, 1f);
                if (_graphic is Image img && idleSprite == null && img.sprite != null)
                    idleSprite = img.sprite;
            }
            else
            {
                _authoredColor = Color.white;
            }
            _authoredPosition = restPosition ?? _rectTransform.anchoredPosition;
            _authoredRotation = _rectTransform.localRotation;
            DamageTextAnchor = new RectTransformCombatAnchor(
                _rectTransform, fctNormalizedAnchor, fctOffset);
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            State = ActorVisualState.Idle;
            UpdateSprite();
        }

        public void SetAuthoredRestPosition(Vector2 restPosition)
        {
            _authoredPosition = restPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke();
        }

        public void TriggerClick()
        {
            Clicked?.Invoke();
        }

        public void ConfigureDamageTextAnchor(Vector2 normalizedAnchor, Vector2 offset)
        {
            fctNormalizedAnchor = normalizedAnchor;
            fctOffset = offset;
            if (_rectTransform != null)
            {
                DamageTextAnchor = new RectTransformCombatAnchor(
                    _rectTransform, fctNormalizedAnchor, fctOffset);
            }
        }

        public IEnumerator Play(PresentationActionKind action)
        {
            ActorVisualState target = ResolveState(action);
            if (!ActorPresentationStatePolicy.CanTransition(_actor, State, target))
                throw new InvalidOperationException(
                    $"Illegal {_actor} presentation transition {State} -> {target}.");
            if (_isPlaying)
                throw new InvalidOperationException("Actor presentation is already active.");

            int sessionId = ++_playSessionId;
            _isPlaying = true;
            try
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }

                RestoreAuthoredPose(resetAlpha: false);
                State = target;
                UpdateSprite();
                _whiteFlashTriggered = false;
                float duration = ResolveDuration(target);
                float elapsed = 0f;
                Vector2 direction = _actor == PresentationActor.Player
                    ? Vector2.right
                    : Vector2.left;
                while (elapsed < duration)
                {
                    if (sessionId != _playSessionId) yield break;
                    float delta = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.05f;
                    elapsed += delta;
                    float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                    ApplyFrame(target, t, direction);
                    yield return null;
                }

                if (sessionId != _playSessionId) yield break;

                bool terminal = target == ActorVisualState.Dying;
                if (terminal)
                {
                    RestoreAuthoredPose(resetAlpha: false);
                    State = ActorVisualState.Hidden;
                    UpdateSprite();
                    if (_canvasGroup != null) _canvasGroup.alpha = 0f;
                    if (_graphic != null) _graphic.color = _authoredColor;
                    gameObject.SetActive(false);
                }
                else
                {
                    RestoreAuthoredPose(resetAlpha: true);
                    State = ActorVisualState.Idle;
                    UpdateSprite();
                }
            }
            finally
            {
                if (sessionId == _playSessionId)
                {
                    _isPlaying = false;
                }
            }
        }

        public void CancelAndApply(ActorVisualState finalState)
        {
            _playSessionId++;
            _isPlaying = false;
            RestoreAuthoredPose(resetAlpha: false);
            State = finalState;
            UpdateSprite();
            if (_graphic != null) _graphic.color = _authoredColor;
            if (_canvasGroup != null)
                _canvasGroup.alpha = finalState == ActorVisualState.Hidden ? 0f : 1f;
            gameObject.SetActive(finalState != ActorVisualState.Hidden);
        }

        private void ApplyFrame(ActorVisualState state, float t, Vector2 direction)
        {
            float travelScale = _reducedMotion ? 0.2f : 1f;
            switch (state)
            {
                case ActorVisualState.Attacking:
                {
                    float lunge = t < 0.4f ? t / 0.4f : 1f - (t - 0.4f) / 0.6f;
                    _rectTransform.anchoredPosition = _authoredPosition +
                        direction * (Value(p => p.AttackTravel, 56f) * travelScale *
                            Mathf.Clamp01(lunge));
                    break;
                }
                case ActorVisualState.FailedAttack:
                    _rectTransform.anchoredPosition = _authoredPosition +
                        Vector2.down * (Mathf.Sin(t * Mathf.PI) *
                            Value(p => p.FailedAttackDrop, 12f) * travelScale);
                    break;
                case ActorVisualState.TakingDamage:
                {
                    _rectTransform.anchoredPosition = _authoredPosition +
                        direction * (-Mathf.Sin(t * Mathf.PI * 4f) *
                            Value(p => p.DamageReactionTravel, 12f) *
                            (1f - t) * travelScale);

                    if (_graphic != null)
                    {
                        int cycles = _profile != null ? _profile.HitFlashCycles : 3;
                        Color flashRed = _profile != null ? _profile.HitFlashRed : new Color(1f, 0.2f, 0.2f, 1f);
                        Color flashWhite = _profile != null ? _profile.HitFlashWhite : Color.white;
                        float intensity = (_profile != null ? _profile.HitFlashIntensity : 1f) * (1f - t);

                        float phase = Mathf.Sin(t * Mathf.PI * 2f * cycles);
                        Color flashColor = phase >= 0f ? flashRed : flashWhite;
                        _graphic.color = Color.Lerp(_authoredColor, flashColor, intensity);
                    }
                    break;
                }
                case ActorVisualState.Walking:
                    _rectTransform.anchoredPosition = _authoredPosition +
                        direction * (Mathf.Sin(t * Mathf.PI) *
                            Value(p => p.WalkTravel, 18f) * travelScale) +
                        Vector2.up * (Mathf.Sin(t * Mathf.PI * 2f) *
                            Value(p => p.WalkBounce, 5f) * travelScale);
                    break;
                case ActorVisualState.Appearing:
                    _canvasGroup.alpha = t;
                    _rectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, t);
                    break;
                case ActorVisualState.Dying:
                {
                    float holdThresh = Value(p => p.DieHoldThreshold, 0.40f);
                    float flashThresh = Value(p => p.DieWhiteFlashThreshold, 0.55f);
                    float fadeThresh = Value(p => p.DieDisappearThreshold, 0.75f);

                    float motionT = holdThresh <= 0f ? 1f : Mathf.Clamp01(t / holdThresh);
                    _rectTransform.anchoredPosition = _authoredPosition +
                        Vector2.down * (Value(p => p.DeathDrop, 72f) * motionT * travelScale);
                    _rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                        (_actor == PresentationActor.Player ? -1f : 1f) *
                        Value(p => p.DeathRotation, 18f) * motionT * travelScale);

                    if (_graphic != null)
                    {
                        if (t < flashThresh)
                        {
                            _graphic.color = _authoredColor;
                        }
                        else if (t < fadeThresh)
                        {
                            float flashProgress = fadeThresh <= flashThresh
                                ? 1f
                                : Mathf.Clamp01((t - flashThresh) / (fadeThresh - flashThresh));
                            _graphic.color = Color.Lerp(_authoredColor, Color.white, flashProgress);
                        }
                        else
                        {
                            _graphic.color = Color.white;
                        }
                    }

                    if (!_whiteFlashTriggered && t >= flashThresh)
                    {
                        _whiteFlashTriggered = true;
                        DyingWhiteFlashReached?.Invoke();
                        RewardDropTriggered?.Invoke();
                    }

                    if (_canvasGroup != null)
                    {
                        if (t < fadeThresh)
                        {
                            _canvasGroup.alpha = 1f;
                        }
                        else
                        {
                            float fadeProgress = Mathf.Clamp01((t - fadeThresh) / Mathf.Max(0.01f, 1f - fadeThresh));
                            _canvasGroup.alpha = 1f - fadeProgress;
                        }
                    }
                    break;
                }
                case ActorVisualState.Rebirthing:
                    _canvasGroup.alpha = Mathf.Clamp01(t * 2f);
                    _rectTransform.anchoredPosition = _authoredPosition +
                        Vector2.up * (Mathf.Sin(t * Mathf.PI) *
                            Value(p => p.RebirthRise, 58f) * travelScale);
                    _rectTransform.localScale = Vector3.one *
                        Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(t * 1.5f));
                    break;
            }
        }

        private float ResolveDuration(ActorVisualState state)
        {
            if (_reducedMotion) return Value(p => p.ReducedMotionSeconds, 0.12f);
            switch (state)
            {
                case ActorVisualState.Attacking:
                    return _actor == PresentationActor.Player
                        ? Value(p => p.PlayerAttackSeconds, 0.55f)
                        : Value(p => p.EnemyAttackSeconds, 0.60f);
                case ActorVisualState.Walking: return Value(p => p.WalkSeconds, 0.32f);
                case ActorVisualState.TakingDamage: return Value(p => p.TakeDamageSeconds, 0.30f);
                case ActorVisualState.Appearing: return Value(p => p.AppearSeconds, 0.45f);
                case ActorVisualState.Dying: return Value(p => p.DieSeconds, 0.90f);
                case ActorVisualState.Rebirthing: return Value(p => p.RebirthSeconds, 0.85f);
                default: return Value(p => p.FailedAttackSeconds, 0.30f);
            }
        }

        private float Value(Func<CombatJuiceProfileDefinition, float> selector, float fallback)
        {
            return _profile == null ? fallback : selector(_profile);
        }

        private ActorVisualState ResolveState(PresentationActionKind action)
        {
            switch (action)
            {
                case PresentationActionKind.PlayerPrimaryAttack:
                case PresentationActionKind.EnemyAttack:
                    return ActorVisualState.Attacking;
                case PresentationActionKind.PlayerFailedAttack:
                    return ActorVisualState.FailedAttack;
                case PresentationActionKind.PlayerTakeDamage:
                case PresentationActionKind.EnemyTakeDamage:
                    return ActorVisualState.TakingDamage;
                case PresentationActionKind.EnemyWalk:
                    return ActorVisualState.Walking;
                case PresentationActionKind.EnemyAppear:
                    return ActorVisualState.Appearing;
                case PresentationActionKind.PlayerDie:
                case PresentationActionKind.EnemyDie:
                    return ActorVisualState.Dying;
                case PresentationActionKind.PlayerRebirth:
                    return ActorVisualState.Rebirthing;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action,
                        "Action does not map to an actor visual state.");
            }
        }

        public void RestoreAuthoredPose(bool resetAlpha = true)
        {
            if (_rectTransform == null) return;
            _rectTransform.anchoredPosition = _authoredPosition;
            _rectTransform.localRotation = _authoredRotation;
            _rectTransform.localScale = Vector3.one;
            if (_graphic != null) _graphic.color = _authoredColor;
            if (resetAlpha && _canvasGroup != null) _canvasGroup.alpha = 1f;
            UpdateSprite();
        }

        private void OnDisable()
        {
            _playSessionId++;
            _isPlaying = false;
            if (_rectTransform != null) RestoreAuthoredPose(resetAlpha: false);
            if (_graphic != null) _graphic.color = _authoredColor;
            State = ActorVisualState.Hidden;
            UpdateSprite();
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        private void UpdateSprite()
        {
            if (_graphic is Image image)
            {
                Sprite target = State switch
                {
                    ActorVisualState.Attacking => attackSprite != null ? attackSprite : idleSprite,
                    ActorVisualState.TakingDamage or ActorVisualState.Dying => hurtSprite != null ? hurtSprite : idleSprite,
                    _ => idleSprite
                };
                if (target != null)
                {
                    image.sprite = target;
                    image.overrideSprite = null;
                }
            }
        }
    }
}
