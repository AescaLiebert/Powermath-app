using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core.Tests
{
    public sealed class LeanTweenUiDriverTests
    {
        private GameObject _host;
        private IUiMotionDriver _driver;
        private VisualElement _target;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("UI motion driver test host");
            _driver = _host.AddComponent<UiMotionDriverProvider>().Driver;
            _target = new VisualElement();
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Dispose();
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void HigherPriorityMotion_RejectsConflictingLowerChannel()
        {
            int lowerCompleted = 0;
            UiMotionHandle lifecycle = _driver.Tween(
                _target,
                UiMotionChannel.Lifecycle,
                10f,
                UiMotionEasing.Linear,
                _ => { });

            UiMotionHandle interaction = _driver.Tween(
                _target,
                UiMotionChannel.Interaction,
                1f,
                UiMotionEasing.Linear,
                _ => Assert.Fail("Rejected motion must not sample."),
                () => lowerCompleted++);

            Assert.That(lifecycle.IsActive, Is.True);
            Assert.That(interaction.IsActive, Is.False);
            Assert.That(lowerCompleted, Is.EqualTo(1));
        }

        [Test]
        public void StartingHigherPriorityMotion_CancelsLowerChannel()
        {
            UiMotionHandle ambient = _driver.Tween(
                _target,
                UiMotionChannel.Ambient,
                10f,
                UiMotionEasing.Linear,
                _ => { },
                loopPingPong: true);

            UiMotionHandle feedback = _driver.Tween(
                _target,
                UiMotionChannel.Feedback,
                10f,
                UiMotionEasing.Linear,
                _ => { });

            Assert.That(ambient.IsActive, Is.False);
            Assert.That(feedback.IsActive, Is.True);
        }

        [Test]
        public void StartingSameChannel_ReplacesPreviousHandle()
        {
            UiMotionHandle first = _driver.Tween(
                _target,
                UiMotionChannel.Feedback,
                10f,
                UiMotionEasing.Linear,
                _ => { });

            UiMotionHandle second = _driver.Tween(
                _target,
                UiMotionChannel.Feedback,
                10f,
                UiMotionEasing.Linear,
                _ => { });

            Assert.That(first.IsActive, Is.False);
            Assert.That(second.IsActive, Is.True);
        }
    }
}
