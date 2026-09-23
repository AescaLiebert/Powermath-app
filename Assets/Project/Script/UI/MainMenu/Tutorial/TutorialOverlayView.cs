using System;
using PowerMath.Gameplay.Tutorial;
using PowerMath.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.Tutorial
{
    public sealed class TutorialOverlayView : IDisposable
    {
        private readonly VisualElement _screenRoot;
        private readonly VisualElement _presentationHost;
        private readonly VisualElement _overlay;
        private readonly Button _focusTarget;
        private readonly VisualElement _dialogue;
        private readonly Image _power;
        private readonly Label _speaker;
        private readonly Label _body;
        private readonly Button _advance;
        private readonly Label _status;
        private readonly bool _reducedMotion;
        private TutorialStep _step;
        private TutorialFocusTarget _target;
        private bool _hasTarget;
        private string _playerName;
        private string _emotionClass;
        private bool _busy;
        private IVisualElementScheduledItem _focusPulse;
        private IVisualElementScheduledItem _emotionEntrance;
        private IVisualElementScheduledItem _typingSchedule;
        private bool _focusPulseExpanded;
        private bool _isTyping;
        private bool _musicDucked;
        private string _fullBodyText = string.Empty;
        private int _typedCharCount;
        private const int TypingIntervalMs = 24; // ~40 chars per second

        public bool IsTyping => _isTyping;

        public TutorialOverlayView(
            VisualElement screenRoot,
            bool reducedMotion)
        {
            _screenRoot = screenRoot ?? throw new ArgumentNullException(nameof(screenRoot));
            _overlay = Require<VisualElement>("tutorial-overlay");
            // A UXML template instance is wrapped in a TemplateContainer. An
            // absolutely-positioned child resolves against that wrapper, which
            // otherwise has no useful size because the instance sits at the end
            // of the main-menu flex hierarchy. Stretch the wrapper rather than
            // detaching the overlay so its template-local stylesheet remains in
            // scope.
            _presentationHost = _overlay.parent ?? _overlay;
            _presentationHost.pickingMode = PickingMode.Ignore;
            _overlay.pickingMode = PickingMode.Ignore;
            StretchPresentationHost();
            _focusTarget = Require<Button>("tutorial-focus-target");
            _dialogue = Require<VisualElement>("tutorial-dialogue");
            _power = Require<Image>("tutorial-power");
            _speaker = Require<Label>("tutorial-speaker");
            _body = Require<Label>("tutorial-body");
            _advance = _screenRoot.Q<Button>("tutorial-advance");
            _status = Require<Label>("tutorial-status");
            _reducedMotion = reducedMotion;
            _overlay.EnableInClassList("is-reduced-motion", reducedMotion);
            if (_advance != null) _advance.clicked += OnAdvance;
            _focusTarget.clicked += OnTarget;
            _overlay.RegisterCallback<ClickEvent>(OnOverlayClicked);
            _screenRoot.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            LocalizationService.Changed += RefreshLocale;
            Hide();
        }

        public event Action AdvanceRequested;
        public event Action TargetRequested;

        public bool IsVisible => _overlay.ClassListContains("is-visible");

        public bool IsAttached => _overlay.panel != null;

        public void BringToFront()
        {
            StretchPresentationHost();
            _presentationHost.BringToFront();
            _overlay.BringToFront();
        }

        public void Show(
            TutorialStep step,
            string playerName,
            TutorialFocusTarget focusTarget)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
            _playerName = playerName ?? string.Empty;
            _target = focusTarget;
            _hasTarget = focusTarget.IsAvailable;
            _busy = false;
            _status.text = string.Empty;
            _overlay.EnableInClassList("is-narrow", _screenRoot.resolvedStyle.width < 720f);
            BringToFront();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0f;
            _overlay.style.right = 0f;
            _overlay.style.top = 0f;
            _overlay.style.bottom = 0f;
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.style.opacity = 1f;
            // The visible overlay is the exclusive pointer surface. Its focus
            // proxy remains clickable as a child, while every transparent area
            // consumes input instead of leaking it to unrelated controls.
            _overlay.pickingMode = PickingMode.Position;
            _overlay.AddToClassList("is-visible");
            bool focus = step.Kind == TutorialStepKind.FocusAction && _hasTarget;
            // Interaction is its own presentation state: only the focused
            // target remains on screen until its semantic action succeeds.
            _dialogue.style.display = focus || step.HidesPresentation
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _focusTarget.EnableInClassList("is-visible", focus);
            _focusTarget.style.display = focus
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _focusTarget.style.opacity = focus ? 1f : 0f;
            _focusTarget.pickingMode = focus
                ? PickingMode.Position
                : PickingMode.Ignore;
            _focusTarget.SetEnabled(focus);
            if (_advance != null)
            {
                _advance.style.display = step.Kind == TutorialStepKind.Dialogue
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _advance.style.opacity = step.Kind == TutorialStepKind.Dialogue ? 1f : 0f;
                _advance.SetEnabled(step.Kind == TutorialStepKind.Dialogue);
            }
            ApplyCopy();
            ApplyEmotion();
            if (focus)
            {
                PositionFocus();
                StartFocusPulse();
            }
            else StopFocusPulse();

            if (step.Kind == TutorialStepKind.Dialogue || !string.IsNullOrWhiteSpace(step.AudioCueId))
            {
                // Duck biome/battle music while tutorial voice is playing.
                // Guard against stacking: Show() is called per-step, not once.
                if (!_musicDucked)
                {
                    PowerMath.Audio.MusicController.Instance?.SetDucking(true);
                    _musicDucked = true;
                }
                PowerMath.Audio.VoiceController.Instance?.PlayVoiceCue(
                    step.AudioCueId,
                    step.SpeakerKey,
                    step.EmotionId,
                    volumeScale: 2.0f);
            }
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;
            _advance?.SetEnabled(!busy && _step?.Kind == TutorialStepKind.Dialogue);
            _focusTarget.SetEnabled(!busy && _step?.Kind == TutorialStepKind.FocusAction);
            _status.text = busy ? LocalizationService.Get("common.saving") : string.Empty;
        }

        public void ShowError(string localizationKey)
        {
            _busy = false;
            _status.text = LocalizationService.Get(localizationKey);
            if (_step?.Kind == TutorialStepKind.Dialogue) _advance?.SetEnabled(true);
            if (_step?.Kind == TutorialStepKind.FocusAction) _focusTarget.SetEnabled(true);
        }

        public void Hide()
        {
            StopTyping();
            PowerMath.Audio.VoiceController.Instance?.StopVoice(true);
            // Release music duck when the overlay closes.
            if (_musicDucked)
            {
                PowerMath.Audio.MusicController.Instance?.SetDucking(false);
                _musicDucked = false;
            }
            _step = null;
            _target = default;
            _hasTarget = false;
            _busy = false;
            _overlay.RemoveFromClassList("is-visible");
            _overlay.style.opacity = 0f;
            _overlay.style.display = DisplayStyle.None;
            _overlay.pickingMode = PickingMode.Ignore;
            _focusTarget.RemoveFromClassList("is-visible");
            _focusTarget.style.display = DisplayStyle.None;
            _focusTarget.style.opacity = 0f;
            _focusTarget.pickingMode = PickingMode.Ignore;
            _focusTarget.SetEnabled(false);
            StopFocusPulse();
            _dialogue.style.display = DisplayStyle.None;
            _status.text = string.Empty;
        }

        public void Dispose()
        {
            Dispose(hidePresentation: true);
        }

        public void Dispose(bool hidePresentation)
        {
            StopTyping();
            LocalizationService.Changed -= RefreshLocale;
            _screenRoot.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (_advance != null) _advance.clicked -= OnAdvance;
            _focusTarget.clicked -= OnTarget;
            _overlay.UnregisterCallback<ClickEvent>(OnOverlayClicked);
            if (hidePresentation) Hide();
        }

        private void OnAdvance()
        {
            if (_busy || _step?.Kind != TutorialStepKind.Dialogue) return;

            // If text is still typing, tapping immediately reveals the full text
            if (_isTyping)
            {
                CompleteTypingImmediately();
                return;
            }

            // Text is already fully shown; stop voice and advance to next step
            PowerMath.Audio.VoiceController.Instance?.StopVoice(false);
            PowerMath.Audio.SfxController.Instance?.PlayUiStyle("tutorial-advance", true);
            AdvanceRequested?.Invoke();
        }

        private void OnTarget()
        {
            if (_busy || _step?.Kind != TutorialStepKind.FocusAction) return;
            PowerMath.Audio.VoiceController.Instance?.StopVoice(false);
            PowerMath.Audio.SfxController.Instance?.PlayUiStyle("tutorial-focus", true);
            TargetRequested?.Invoke();
        }

        private void OnOverlayClicked(ClickEvent evt)
        {
            if (_busy || _step?.Kind != TutorialStepKind.Dialogue) return;
            // The dedicated button already dispatches this action if present. All other
            // visible overlay surfaces use tap-to-continue for mobile play.
            if (_advance != null && evt.target is VisualElement target && _advance.Contains(target)) return;
            OnAdvance();
            evt.StopPropagation();
        }

        private void RefreshLocale()
        {
            if (_step == null) return;
            ApplyCopy();
        }

        private void ApplyCopy()
        {
            if (_step == null) return;
            _speaker.text = LocalizationService.Get(_step.SpeakerKey);
            if (_advance != null) _advance.text = LocalizationService.Get("tutorial.advance");

            _fullBodyText = LocalizationService.Get(_step.TextKey, _playerName) ?? string.Empty;
            if (_step.Kind != TutorialStepKind.Dialogue || _reducedMotion || string.IsNullOrEmpty(_fullBodyText))
            {
                StopTyping();
                _body.text = _fullBodyText;
            }
            else
            {
                StartTyping();
            }
        }

        public void CompleteTypingImmediately()
        {
            StopTyping();
            _typedCharCount = _fullBodyText?.Length ?? 0;
            if (_body != null)
            {
                _body.text = _fullBodyText ?? string.Empty;
            }
        }

        private void StartTyping()
        {
            StopTyping();
            _isTyping = true;
            _typedCharCount = 0;
            if (_body != null)
            {
                _body.text = string.Empty;
            }
            _typingSchedule = _screenRoot.schedule.Execute(OnTypingTick).Every(TypingIntervalMs);
        }

        private void OnTypingTick()
        {
            if (!_isTyping || string.IsNullOrEmpty(_fullBodyText))
            {
                StopTyping();
                return;
            }

            if (_typedCharCount >= _fullBodyText.Length)
            {
                CompleteTypingImmediately();
                return;
            }

            _typedCharCount = AdvanceCharIndex(_fullBodyText, _typedCharCount);
            if (_body != null)
            {
                _body.text = _fullBodyText.Substring(0, _typedCharCount);
            }

            if (_typedCharCount >= _fullBodyText.Length)
            {
                CompleteTypingImmediately();
            }
        }

        private void StopTyping()
        {
            _isTyping = false;
            _typingSchedule?.Pause();
            _typingSchedule = null;
        }

        private static int AdvanceCharIndex(string text, int currentIndex)
        {
            int idx = currentIndex;
            do
            {
                if (idx < text.Length && text[idx] == '<')
                {
                    int tagEnd = text.IndexOf('>', idx);
                    if (tagEnd != -1)
                    {
                        idx = tagEnd + 1;
                        continue;
                    }
                }
                idx++;
                break;
            } while (idx < text.Length);

            return Mathf.Clamp(idx, 0, text.Length);
        }

        private void ApplyEmotion()
        {
            string nextEmotionClass = string.IsNullOrWhiteSpace(_step?.EmotionId)
                ? string.Empty
                : "tutorial-emotion--" + _step.EmotionId.ToLowerInvariant();
            Sprite sprite = string.IsNullOrWhiteSpace(_step?.SpriteResourcePath)
                ? null
                : Resources.Load<Sprite>(_step.SpriteResourcePath);
            bool changed = !string.Equals(
                    _emotionClass, nextEmotionClass, StringComparison.Ordinal) ||
                _power.sprite != sprite;
            if (!string.IsNullOrEmpty(_emotionClass))
                _overlay.RemoveFromClassList(_emotionClass);
            _emotionClass = nextEmotionClass;
            if (!string.IsNullOrEmpty(_emotionClass))
                _overlay.AddToClassList(_emotionClass);
            _power.sprite = sprite;
            _power.style.display = sprite == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (changed && sprite != null) PlayEmotionEntrance();
        }

        private void OnGeometryChanged(GeometryChangedEvent _)
        {
            StretchPresentationHost();
            _overlay.EnableInClassList("is-narrow", _screenRoot.resolvedStyle.width < 720f);
            if (_hasTarget && _step?.Kind == TutorialStepKind.FocusAction)
                PositionFocus();
        }

        private void StretchPresentationHost()
        {
            _presentationHost.style.position = Position.Absolute;
            _presentationHost.style.left = 0f;
            _presentationHost.style.right = 0f;
            _presentationHost.style.top = 0f;
            _presentationHost.style.bottom = 0f;
        }

        private void PositionFocus()
        {
            if (!_hasTarget || _screenRoot.panel == null) return;
            if (_target.UiTarget != null)
            {
                Rect bounds = _target.UiTarget.ChangeCoordinatesTo(
                    _screenRoot, _target.UiTarget.contentRect);
                PositionFocus(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
                return;
            }
            RectTransform sceneTarget = _target.SceneTarget;
            if (sceneTarget == null) return;
            var corners = new Vector3[4];
            sceneTarget.GetWorldCorners(corners);
            Rect focusRect = _target.NormalizedFocusRect;
            Vector3 bottomLeft = corners[0];
            Vector3 up = corners[1] - corners[0];
            Vector3 right = corners[3] - corners[0];
            corners[0] = bottomLeft + right * focusRect.xMin + up * focusRect.yMin;
            corners[1] = bottomLeft + right * focusRect.xMin + up * focusRect.yMax;
            corners[2] = bottomLeft + right * focusRect.xMax + up * focusRect.yMax;
            corners[3] = bottomLeft + right * focusRect.xMax + up * focusRect.yMin;
            Canvas canvas = sceneTarget.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 firstScreen = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 first = RuntimePanelUtils.ScreenToPanel(_screenRoot.panel, firstScreen);
            float minX = first.x;
            float maxX = first.x;
            float minY = first.y;
            float maxY = first.y;
            for (int index = 1; index < corners.Length; index++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corners[index]);
                Vector2 panel = RuntimePanelUtils.ScreenToPanel(_screenRoot.panel, screen);
                minX = Mathf.Min(minX, panel.x);
                maxX = Mathf.Max(maxX, panel.x);
                minY = Mathf.Min(minY, panel.y);
                maxY = Mathf.Max(maxY, panel.y);
            }
            PositionFocus(minX, minY, maxX, maxY);
        }

        private void PositionFocus(float minX, float minY, float maxX, float maxY)
        {
            const float padding = 14f;
            _focusTarget.style.left = minX - padding;
            _focusTarget.style.top = minY - padding;
            _focusTarget.style.width = Mathf.Max(72f, maxX - minX + padding * 2f);
            _focusTarget.style.height = Mathf.Max(72f, maxY - minY + padding * 2f);
        }

        private void StartFocusPulse()
        {
            StopFocusPulse();
            if (_reducedMotion) return;
            _focusPulseExpanded = false;
            _focusPulse = _focusTarget.schedule.Execute(() =>
            {
                _focusPulseExpanded = !_focusPulseExpanded;
                _focusTarget.EnableInClassList(
                    "is-attention-pulse", _focusPulseExpanded);
            }).Every(650);
        }

        private void StopFocusPulse()
        {
            _focusPulse?.Pause();
            _focusPulse = null;
            _focusPulseExpanded = false;
            _focusTarget.RemoveFromClassList("is-attention-pulse");
        }

        private void PlayEmotionEntrance()
        {
            _emotionEntrance?.Pause();
            _emotionEntrance = null;
            _power.RemoveFromClassList("is-emotion-entering");
            if (_reducedMotion) return;
            _power.AddToClassList("is-emotion-entering");
            _emotionEntrance = _power.schedule.Execute(() =>
            {
                _power.RemoveFromClassList("is-emotion-entering");
                _emotionEntrance = null;
            }).StartingIn(16);
        }

        private T Require<T>(string name) where T : VisualElement
        {
            T value = _screenRoot.Q<T>(name);
            if (value == null)
                throw new InvalidOperationException(
                    "Tutorial overlay requires UI element '" + name + "'.");
            return value;
        }
    }
}
