using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public sealed class UiInteractionMotionBinder : IDisposable
    {
        private readonly VisualElement _target;
        private readonly IUiMotionDriver _driver;
        private readonly UiMotionProfileDefinition _profile;
        private bool _hovered;
        private bool _focused;
        private bool _pressed;
        private bool _disposed;
        private float _scale = 1f;
        private float _offsetY;

        public UiInteractionMotionBinder(
            VisualElement target,
            IUiMotionDriver driver,
            UiMotionProfileDefinition profile)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));

            _target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            _target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _target.RegisterCallback<FocusInEvent>(OnFocusIn);
            _target.RegisterCallback<FocusOutEvent>(OnFocusOut);
            _target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public UiMotionState State { get; private set; } = UiMotionState.Idle;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            _target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
            _target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _target.UnregisterCallback<FocusInEvent>(OnFocusIn);
            _target.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            _target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            _driver.Cancel(_target, UiMotionChannel.Interaction);
            _target.style.scale = new Scale(Vector3.one);
            _target.style.translate = new Translate(0f, 0f, 0f);
        }

        private void OnPointerEnter(PointerEnterEvent _)
        {
            _hovered = true;
            Refresh();
        }

        private void OnPointerLeave(PointerLeaveEvent _)
        {
            _hovered = false;
            _pressed = false;
            Refresh();
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0)
            {
                return;
            }
            _pressed = true;
            Refresh();
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.button != 0)
            {
                return;
            }
            _pressed = false;
            Refresh();
        }

        private void OnFocusIn(FocusInEvent _)
        {
            _focused = true;
            Refresh();
        }

        private void OnFocusOut(FocusOutEvent _)
        {
            _focused = false;
            _pressed = false;
            Refresh();
        }

        private void OnDetach(DetachFromPanelEvent _)
        {
            Dispose();
        }

        private void Refresh()
        {
            if (_disposed)
            {
                return;
            }

            float targetScale = 1f;
            float targetOffset = 0f;
            float duration = _profile.HoverSeconds;
            if (_pressed)
            {
                State = UiMotionState.Pressed;
                duration = _profile.PressSeconds;
                if (!_driver.ReducedMotion)
                {
                    targetScale = _profile.PressScale;
                    targetOffset = _profile.PressOffsetY;
                }
            }
            else if (_hovered || _focused)
            {
                State = UiMotionState.Hovered;
                if (!_driver.ReducedMotion)
                {
                    targetScale = _profile.HoverScale;
                    targetOffset = _profile.HoverOffsetY;
                }
            }
            else
            {
                State = UiMotionState.Idle;
            }

            float startScale = _scale;
            float startOffset = _offsetY;
            _driver.Tween(
                _target,
                UiMotionChannel.Interaction,
                _driver.ReducedMotion ? 0f : duration,
                UiMotionEasing.OutBack,
                value =>
                {
                    _scale = Mathf.Lerp(startScale, targetScale, value);
                    _offsetY = Mathf.Lerp(startOffset, targetOffset, value);
                    _target.style.scale = new Scale(
                        new Vector3(_scale, _scale, 1f));
                    _target.style.translate = new Translate(
                        0f,
                        _offsetY,
                        0f);
                });
        }
    }
}
