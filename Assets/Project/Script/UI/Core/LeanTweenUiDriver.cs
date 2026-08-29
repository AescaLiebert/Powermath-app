using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public sealed class LeanTweenUiDriver : IUiMotionDriver
    {
        private readonly struct MotionKey : IEquatable<MotionKey>
        {
            public MotionKey(object target, UiMotionChannel channel)
            {
                Target = target;
                Channel = channel;
            }

            public object Target { get; }
            public UiMotionChannel Channel { get; }

            public bool Equals(MotionKey other)
            {
                return ReferenceEquals(Target, other.Target) &&
                       Channel == other.Channel;
            }

            public override bool Equals(object obj)
            {
                return obj is MotionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (RuntimeHelpers.GetHashCode(Target) * 397) ^
                           (int)Channel;
                }
            }
        }

        private readonly MonoBehaviour _host;
        private readonly Dictionary<MotionKey, UiMotionHandle> _active =
            new Dictionary<MotionKey, UiMotionHandle>();
        private bool _disposed;

        public LeanTweenUiDriver(MonoBehaviour host, bool reducedMotion)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            ReducedMotion = reducedMotion;
            LeanTween.init();
        }

        public bool ReducedMotion { get; private set; }

        public void SetReducedMotion(bool reducedMotion)
        {
            ReducedMotion = reducedMotion;
        }

        public UiMotionHandle Tween(
            VisualElement target,
            UiMotionChannel channel,
            float seconds,
            UiMotionEasing easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false)
        {
            return Start(
                target,
                channel,
                seconds,
                descriptor => descriptor.setEase(ToLeanTweenType(easing)),
                apply,
                completed,
                delaySeconds,
                loopPingPong);
        }

        public UiMotionHandle Tween(
            VisualElement target,
            UiMotionChannel channel,
            float seconds,
            AnimationCurve easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false)
        {
            return Start(
                target,
                channel,
                seconds,
                descriptor => descriptor.setEase(easing),
                apply,
                completed,
                delaySeconds,
                loopPingPong);
        }

        public UiMotionHandle Tween(
            RectTransform target,
            UiMotionChannel channel,
            float seconds,
            UiMotionEasing easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false)
        {
            return Start(
                target,
                channel,
                seconds,
                descriptor => descriptor.setEase(ToLeanTweenType(easing)),
                apply,
                completed,
                delaySeconds,
                loopPingPong);
        }

        public UiMotionHandle Tween(
            RectTransform target,
            UiMotionChannel channel,
            float seconds,
            AnimationCurve easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false)
        {
            return Start(
                target,
                channel,
                seconds,
                descriptor => descriptor.setEase(easing),
                apply,
                completed,
                delaySeconds,
                loopPingPong);
        }

        public void Cancel(VisualElement target, UiMotionChannel channel)
        {
            Cancel(new MotionKey(target, channel));
        }

        public void Cancel(RectTransform target, UiMotionChannel channel)
        {
            Cancel(new MotionKey(target, channel));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UiMotionHandle[] handles = new UiMotionHandle[_active.Count];
            _active.Values.CopyTo(handles, 0);
            for (int index = 0; index < handles.Length; index++)
            {
                handles[index].Cancel();
            }
            _active.Clear();
        }

        private UiMotionHandle Start(
            object target,
            UiMotionChannel channel,
            float seconds,
            Action<LTDescr> configureEase,
            Action<float> apply,
            Action completed,
            float delaySeconds,
            bool loopPingPong)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LeanTweenUiDriver));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }

            MotionKey key = new MotionKey(target, channel);
            if (HasHigherPriorityMotion(target, channel))
            {
                completed?.Invoke();
                return UiMotionHandle.Completed();
            }
            CancelConflicts(target, channel);
            Cancel(key);

            if (seconds <= 0f && delaySeconds <= 0f)
            {
                apply(1f);
                completed?.Invoke();
                return UiMotionHandle.Completed();
            }

            UiMotionHandle handle = null;
            LTDescr descriptor = LeanTween.value(
                    _host.gameObject,
                    0f,
                    1f,
                    Mathf.Max(0.0001f, seconds))
                .setIgnoreTimeScale(true)
                .setOnUpdate(value => apply(value));
            configureEase(descriptor);
            if (delaySeconds > 0f)
            {
                descriptor.setDelay(delaySeconds);
            }
            if (loopPingPong)
            {
                descriptor.setLoopPingPong();
            }
            else
            {
                descriptor.setOnComplete(() => Complete(key, handle, completed));
            }

            handle = new UiMotionHandle(
                descriptor.id,
                value => CancelHandle(key, value));
            _active[key] = handle;
            return handle;
        }

        private bool HasHigherPriorityMotion(
            object target,
            UiMotionChannel requestedChannel)
        {
            for (int value = 0; value < (int)requestedChannel; value++)
            {
                MotionKey key = new MotionKey(
                    target,
                    (UiMotionChannel)value);
                if (_active.TryGetValue(key, out UiMotionHandle handle) &&
                    handle.IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private void Complete(
            MotionKey key,
            UiMotionHandle handle,
            Action completed)
        {
            if (handle == null ||
                !_active.TryGetValue(key, out UiMotionHandle current) ||
                !ReferenceEquals(current, handle))
            {
                return;
            }

            _active.Remove(key);
            handle.MarkComplete();
            completed?.Invoke();
        }

        private void CancelConflicts(object target, UiMotionChannel channel)
        {
            switch (channel)
            {
                case UiMotionChannel.Lifecycle:
                    Cancel(new MotionKey(target, UiMotionChannel.Feedback));
                    Cancel(new MotionKey(target, UiMotionChannel.Interaction));
                    Cancel(new MotionKey(target, UiMotionChannel.Ambient));
                    break;
                case UiMotionChannel.Feedback:
                    Cancel(new MotionKey(target, UiMotionChannel.Interaction));
                    Cancel(new MotionKey(target, UiMotionChannel.Ambient));
                    break;
                case UiMotionChannel.Interaction:
                    Cancel(new MotionKey(target, UiMotionChannel.Ambient));
                    break;
            }
        }

        private void Cancel(MotionKey key)
        {
            if (_active.TryGetValue(key, out UiMotionHandle handle))
            {
                CancelHandle(key, handle);
            }
        }

        private void CancelHandle(MotionKey key, UiMotionHandle handle)
        {
            if (!handle.IsActive)
            {
                return;
            }
            if (_active.TryGetValue(key, out UiMotionHandle current) &&
                ReferenceEquals(current, handle))
            {
                _active.Remove(key);
            }
            if (LeanTween.isTweening(handle.TweenId))
            {
                LeanTween.cancel(handle.TweenId);
            }
            handle.MarkComplete();
        }

        private static LeanTweenType ToLeanTweenType(UiMotionEasing easing)
        {
            switch (easing)
            {
                case UiMotionEasing.OutCubic:
                    return LeanTweenType.easeOutCubic;
                case UiMotionEasing.OutBack:
                    return LeanTweenType.easeOutBack;
                case UiMotionEasing.InOutSine:
                    return LeanTweenType.easeInOutSine;
                default:
                    return LeanTweenType.linear;
            }
        }
    }
}
