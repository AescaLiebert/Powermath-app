using System;

namespace PowerMath.UI.Core
{
    public enum UiMotionState
    {
        Hidden,
        Entering,
        Idle,
        Hovered,
        Pressed,
        Exiting
    }

    public enum UiMotionChannel
    {
        Lifecycle,
        Feedback,
        Interaction,
        Ambient
    }

    public enum UiMotionEasing
    {
        Linear,
        OutCubic,
        OutBack,
        InOutSine
    }

    public enum UiMotionEndState
    {
        ApplyHidden,
        ApplyIdle
    }

    public sealed class UiMotionHandle
    {
        private Action<UiMotionHandle> _cancel;

        internal UiMotionHandle(int tweenId, Action<UiMotionHandle> cancel)
        {
            TweenId = tweenId;
            _cancel = cancel;
            IsActive = true;
        }

        private UiMotionHandle()
        {
        }

        public bool IsActive { get; private set; }

        internal int TweenId { get; }

        public void Cancel()
        {
            if (IsActive)
            {
                _cancel?.Invoke(this);
            }
        }

        internal void MarkComplete()
        {
            IsActive = false;
            _cancel = null;
        }

        internal static UiMotionHandle Completed()
        {
            return new UiMotionHandle();
        }
    }
}
