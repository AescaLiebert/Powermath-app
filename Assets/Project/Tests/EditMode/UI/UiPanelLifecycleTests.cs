using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core.Tests
{
    public sealed class UiPanelLifecycleTests
    {
        private UiMotionProfileDefinition _profile;
        private FakeMotionDriver _driver;
        private VisualElement _panel;
        private UiPanelLifecycle _lifecycle;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<
                UiMotionProfileDefinition>();
            _driver = new FakeMotionDriver();
            _panel = new VisualElement();
            _lifecycle = new UiPanelLifecycle(
                _panel,
                _driver,
                _profile);
        }

        [TearDown]
        public void TearDown()
        {
            _lifecycle?.Dispose();
            _driver?.Dispose();
            UnityEngine.Object.DestroyImmediate(_profile);
        }

        [Test]
        public void Enter_CompletesInIdleAndEnablesPicking()
        {
            int completed = 0;

            _lifecycle.Enter(() => completed++);
            Assert.That(_lifecycle.State, Is.EqualTo(UiMotionState.Entering));
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Ignore));

            _driver.Complete();

            Assert.That(_lifecycle.State, Is.EqualTo(UiMotionState.Idle));
            Assert.That(_lifecycle.IsStable, Is.True);
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void ExitDuringEnter_InvalidatesEnterAndFinishesHidden()
        {
            int enterCompleted = 0;
            int exitCompleted = 0;

            _lifecycle.Enter(() => enterCompleted++);
            _driver.Sample(0.5f);
            _lifecycle.Exit(() => exitCompleted++);
            _driver.Complete();

            Assert.That(enterCompleted, Is.Zero);
            Assert.That(exitCompleted, Is.EqualTo(1));
            Assert.That(_lifecycle.State, Is.EqualTo(UiMotionState.Hidden));
            Assert.That(
                _panel.style.display.value,
                Is.EqualTo(DisplayStyle.None));
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [Test]
        public void EnterDuringExit_ReversesAndFinishesIdle()
        {
            _lifecycle.Enter();
            _driver.Complete();
            int exitCompleted = 0;

            _lifecycle.Exit(() => exitCompleted++);
            _driver.Sample(0.5f);
            _lifecycle.Enter();
            _driver.Complete();

            Assert.That(exitCompleted, Is.Zero);
            Assert.That(_lifecycle.State, Is.EqualTo(UiMotionState.Idle));
            Assert.That(
                _panel.style.display.value,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Position));
        }

        [Test]
        public void ReducedMotion_UsesCrossfadeWithoutScaleOrOffset()
        {
            _driver.SetReducedMotion(true);

            _lifecycle.Enter();
            _driver.Sample(0.5f);

            Assert.That(_panel.style.scale.value.value.x, Is.EqualTo(1f));
            Assert.That(_panel.style.translate.value.y.value, Is.EqualTo(0f));
        }

        private sealed class FakeMotionDriver : IUiMotionDriver
        {
            private PendingTween _pending;

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
                return Begin(apply, completed);
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
                return Begin(apply, completed);
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
                return Begin(apply, completed);
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
                return Begin(apply, completed);
            }

            public void Cancel(VisualElement target, UiMotionChannel channel)
            {
                _pending?.Handle.Cancel();
            }

            public void Cancel(RectTransform target, UiMotionChannel channel)
            {
                _pending?.Handle.Cancel();
            }

            public void Sample(float value)
            {
                _pending?.Apply(value);
            }

            public void Complete()
            {
                PendingTween pending = _pending;
                if (pending == null)
                {
                    return;
                }
                _pending = null;
                pending.Apply(1f);
                pending.Handle.MarkComplete();
                pending.Completed?.Invoke();
            }

            public void Dispose()
            {
                _pending?.Handle.Cancel();
                _pending = null;
            }

            private UiMotionHandle Begin(
                Action<float> apply,
                Action completed)
            {
                _pending?.Handle.Cancel();
                PendingTween pending = null;
                UiMotionHandle handle = new UiMotionHandle(
                    0,
                    _ =>
                    {
                        if (ReferenceEquals(_pending, pending))
                        {
                            _pending = null;
                        }
                        pending.Handle.MarkComplete();
                    });
                pending = new PendingTween(handle, apply, completed);
                _pending = pending;
                return handle;
            }

            private sealed class PendingTween
            {
                public PendingTween(
                    UiMotionHandle handle,
                    Action<float> apply,
                    Action completed)
                {
                    Handle = handle;
                    Apply = apply;
                    Completed = completed;
                }

                public UiMotionHandle Handle { get; }
                public Action<float> Apply { get; }
                public Action Completed { get; }
            }
        }
    }
}
