using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public interface IUiPanelLifecycle : IDisposable
    {
        UiMotionState State { get; }
        bool IsStable { get; }
        VisualElement Root { get; }

        void Enter(Action completed = null);
        void Exit(Action completed = null);
        void CancelAndApply(UiMotionEndState endState);
    }

    public sealed class UiPanelLifecycle : IUiPanelLifecycle
    {
        private readonly IUiMotionDriver _driver;
        private readonly UiMotionProfileDefinition _profile;
        private UiMotionHandle _active;
        private float _opacity;
        private float _scale;
        private float _offsetY;
        private int _revision;
        private bool _disposed;

        public UiPanelLifecycle(
            VisualElement root,
            IUiMotionDriver driver,
            UiMotionProfileDefinition profile)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            ApplyHidden();
        }

        public UiMotionState State { get; private set; }
        public bool IsStable => State == UiMotionState.Hidden ||
                                State == UiMotionState.Idle;
        public VisualElement Root { get; }

        public void Enter(Action completed = null)
        {
            ThrowIfDisposed();
            int revision = ++_revision;
            CancelActive();

            if (State == UiMotionState.Hidden)
            {
                _opacity = 0f;
                _scale = _driver.ReducedMotion ? 1f : _profile.EnterScale;
                _offsetY = _driver.ReducedMotion ? 0f : _profile.EnterOffsetY;
            }

            Root.EnableInClassList("is-hidden", false);
            Root.style.display = DisplayStyle.Flex;
            Root.pickingMode = PickingMode.Ignore;
            State = UiMotionState.Entering;
            ApplyCurrent();

            float startOpacity = _opacity;
            float startScale = _scale;
            float startOffset = _offsetY;
            float duration = _driver.ReducedMotion
                ? _profile.ReducedCrossfadeSeconds
                : _profile.EnterSeconds;
            _active = _driver.Tween(
                Root,
                UiMotionChannel.Lifecycle,
                duration,
                _driver.ReducedMotion
                    ? UiMotionEasing.OutCubic
                    : UiMotionEasing.OutBack,
                value =>
                {
                    _opacity = Mathf.Lerp(startOpacity, 1f, value);
                    _scale = Mathf.Lerp(startScale, 1f, value);
                    _offsetY = Mathf.Lerp(startOffset, 0f, value);
                    ApplyCurrent();
                },
                () =>
                {
                    if (revision != _revision || _disposed)
                    {
                        return;
                    }
                    _active = null;
                    ApplyIdle();
                    completed?.Invoke();
                });
        }

        public void Exit(Action completed = null)
        {
            ThrowIfDisposed();
            if (State == UiMotionState.Hidden)
            {
                completed?.Invoke();
                return;
            }

            int revision = ++_revision;
            CancelActive();
            Root.pickingMode = PickingMode.Ignore;
            State = UiMotionState.Exiting;

            float startOpacity = _opacity;
            float startScale = _scale;
            float startOffset = _offsetY;
            float targetScale = _driver.ReducedMotion
                ? 1f
                : _profile.ExitScale;
            float targetOffset = _driver.ReducedMotion
                ? 0f
                : _profile.ExitOffsetY;
            float duration = _driver.ReducedMotion
                ? _profile.ReducedCrossfadeSeconds
                : _profile.ExitSeconds;
            _active = _driver.Tween(
                Root,
                UiMotionChannel.Lifecycle,
                duration,
                UiMotionEasing.OutCubic,
                value =>
                {
                    _opacity = Mathf.Lerp(startOpacity, 0f, value);
                    _scale = Mathf.Lerp(startScale, targetScale, value);
                    _offsetY = Mathf.Lerp(startOffset, targetOffset, value);
                    ApplyCurrent();
                },
                () =>
                {
                    if (revision != _revision || _disposed)
                    {
                        return;
                    }
                    _active = null;
                    ApplyHidden();
                    completed?.Invoke();
                });
        }

        public void CancelAndApply(UiMotionEndState endState)
        {
            if (_disposed)
            {
                return;
            }
            _revision++;
            CancelActive();
            switch (endState)
            {
                case UiMotionEndState.ApplyHidden:
                    ApplyHidden();
                    break;
                case UiMotionEndState.ApplyIdle:
                    ApplyIdle();
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _revision++;
            CancelActive();
            ApplyHidden();
            _disposed = true;
        }

        private void ApplyIdle()
        {
            _opacity = 1f;
            _scale = 1f;
            _offsetY = 0f;
            Root.EnableInClassList("is-hidden", false);
            Root.style.display = DisplayStyle.Flex;
            Root.pickingMode = PickingMode.Position;
            State = UiMotionState.Idle;
            ApplyCurrent();
        }

        private void ApplyHidden()
        {
            _opacity = 0f;
            _scale = 1f;
            _offsetY = 0f;
            Root.EnableInClassList("is-hidden", true);
            Root.style.display = DisplayStyle.None;
            Root.pickingMode = PickingMode.Ignore;
            State = UiMotionState.Hidden;
            ApplyCurrent();
        }

        private void ApplyCurrent()
        {
            Root.style.opacity = _opacity;
            Root.style.scale = new Scale(new Vector3(_scale, _scale, 1f));
            Root.style.translate = new Translate(0f, _offsetY, 0f);
        }

        private void CancelActive()
        {
            _active?.Cancel();
            _active = null;
            _driver.Cancel(Root, UiMotionChannel.Lifecycle);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UiPanelLifecycle));
            }
        }
    }
}
