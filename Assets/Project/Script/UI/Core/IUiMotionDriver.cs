using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public interface IUiMotionDriver : IDisposable
    {
        bool ReducedMotion { get; }

        void SetReducedMotion(bool reducedMotion);

        UiMotionHandle Tween(
            VisualElement target,
            UiMotionChannel channel,
            float seconds,
            UiMotionEasing easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false);

        UiMotionHandle Tween(
            VisualElement target,
            UiMotionChannel channel,
            float seconds,
            AnimationCurve easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false);

        UiMotionHandle Tween(
            RectTransform target,
            UiMotionChannel channel,
            float seconds,
            UiMotionEasing easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false);

        UiMotionHandle Tween(
            RectTransform target,
            UiMotionChannel channel,
            float seconds,
            AnimationCurve easing,
            Action<float> apply,
            Action completed = null,
            float delaySeconds = 0f,
            bool loopPingPong = false);

        void Cancel(VisualElement target, UiMotionChannel channel);

        void Cancel(RectTransform target, UiMotionChannel channel);
    }
}
