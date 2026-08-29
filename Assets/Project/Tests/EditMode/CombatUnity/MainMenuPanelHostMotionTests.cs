using System;
using NUnit.Framework;
using PowerMath.UI.Core;
using PowerMath.UI.MainMenu;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class MainMenuPanelHostMotionTests
    {
        private UiMotionProfileDefinition _profile;
        private ManualMotionDriver _driver;
        private MainMenuPanelHost _host;
        private VisualElement _panel;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<
                UiMotionProfileDefinition>();
            _driver = new ManualMotionDriver();
            _host = new MainMenuPanelHost(null, _driver, _profile);
            _panel = new VisualElement();
        }

        [TearDown]
        public void TearDown()
        {
            _host?.ForceCloseAll();
            _driver?.Dispose();
            UnityEngine.Object.DestroyImmediate(_profile);
        }

        [Test]
        public void AnimatedClose_KeepsIdentityUntilHiddenCompletion()
        {
            int closeCount = 0;
            _host.PanelClosed += _ => closeCount++;

            Assert.That(_host.TryOpen(
                MainMenuPanelId.PlayerHub,
                _panel,
                null), Is.True);
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Ignore));
            _driver.Complete();
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Position));

            Assert.That(_host.TryClose(
                MainMenuPanelId.PlayerHub,
                null), Is.True);
            Assert.That(
                _host.OpenPanel,
                Is.EqualTo(MainMenuPanelId.PlayerHub));
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(closeCount, Is.Zero);

            _driver.Complete();

            Assert.That(_host.OpenPanel, Is.EqualTo(MainMenuPanelId.None));
            Assert.That(_panel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(closeCount, Is.EqualTo(1));
        }

        [Test]
        public void ReopenDuringExit_ReversesWithoutPublishingClose()
        {
            int closeCount = 0;
            _host.PanelClosed += _ => closeCount++;
            _host.TryOpen(MainMenuPanelId.PlayerHub, _panel, null);
            _driver.Complete();
            _host.TryClose(MainMenuPanelId.PlayerHub, null);
            _driver.Sample(0.5f);

            Assert.That(_host.TryOpen(
                MainMenuPanelId.PlayerHub,
                _panel,
                null), Is.True);
            _driver.Complete();

            Assert.That(
                _host.OpenPanel,
                Is.EqualTo(MainMenuPanelId.PlayerHub));
            Assert.That(_panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_panel.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(closeCount, Is.Zero);
        }

        private sealed class ManualMotionDriver : IUiMotionDriver
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
