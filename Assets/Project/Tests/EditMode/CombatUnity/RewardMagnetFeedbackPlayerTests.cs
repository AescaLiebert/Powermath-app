using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class RewardMagnetFeedbackPlayerTests
    {
        private sealed class ImmediateMotionDriver : IUiMotionDriver
        {
            public bool ReducedMotion => true;
            public void SetReducedMotion(bool reducedMotion) { }

            public UiMotionHandle Tween(
                VisualElement target,
                UiMotionChannel channel,
                float seconds,
                UiMotionEasing easing,
                System.Action<float> apply,
                System.Action completed = null,
                float delaySeconds = 0,
                bool loopPingPong = false)
            {
                apply?.Invoke(1f);
                completed?.Invoke();
                return null;
            }

            public UiMotionHandle Tween(
                VisualElement target,
                UiMotionChannel channel,
                float seconds,
                AnimationCurve easing,
                System.Action<float> apply,
                System.Action completed = null,
                float delaySeconds = 0,
                bool loopPingPong = false)
            {
                apply?.Invoke(1f);
                completed?.Invoke();
                return null;
            }

            public UiMotionHandle Tween(
                RectTransform target,
                UiMotionChannel channel,
                float seconds,
                UiMotionEasing easing,
                System.Action<float> apply,
                System.Action completed = null,
                float delaySeconds = 0,
                bool loopPingPong = false)
            {
                apply?.Invoke(1f);
                completed?.Invoke();
                return null;
            }

            public UiMotionHandle Tween(
                RectTransform target,
                UiMotionChannel channel,
                float seconds,
                AnimationCurve easing,
                System.Action<float> apply,
                System.Action completed = null,
                float delaySeconds = 0,
                bool loopPingPong = false)
            {
                apply?.Invoke(1f);
                completed?.Invoke();
                return null;
            }

            public void Cancel(VisualElement target, UiMotionChannel channel) { }
            public void Cancel(RectTransform target, UiMotionChannel channel) { }
            public void Dispose() { }
        }

        [Test]
        public void PlayRewardDropAndMagnet_StepsCurrencyIncrementProgressively()
        {
            var root = new VisualElement();
            var silverLabel = new Label("0") { name = "profile-silver-value" };
            root.Add(silverLabel);

            var driver = new ImmediateMotionDriver();
            var player = new RewardMagnetFeedbackPlayer(root, driver);

            var incrementalSteps = new List<long>();
            IEnumerator routine = player.PlayRewardDropAndMagnet(
                RewardCurrencyKind.RankSilver,
                startAmount: 0,
                grantAmount: 25,
                screenOrWorldOrigin: new Vector2(200f, 200f),
                onIncrement: val => incrementalSteps.Add(val));

            // Execute coroutine synchronously
            while (routine.MoveNext()) { }

            Assert.That(incrementalSteps.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(incrementalSteps[incrementalSteps.Count - 1], Is.EqualTo(25));
            Assert.That(silverLabel.text, Is.EqualTo("25"));

            player.Dispose();
        }

        [Test]
        public void PlayRewardDropAndMagnet_PowerCoin_TargetsPowerCoinLabel()
        {
            var root = new VisualElement();
            var coinLabel = new Label("0") { name = "player-menu-power-coins" };
            root.Add(coinLabel);

            var driver = new ImmediateMotionDriver();
            var player = new RewardMagnetFeedbackPlayer(root, driver);

            IEnumerator routine = player.PlayRewardDropAndMagnet(
                RewardCurrencyKind.PowerCoin,
                startAmount: 10,
                grantAmount: 5,
                screenOrWorldOrigin: new Vector2(300f, 300f));

            while (routine.MoveNext()) { }

            Assert.That(coinLabel.text, Is.EqualTo("15"));

            player.Dispose();
        }

        [Test]
        public void Dispose_CleansUpOverlayLayer()
        {
            var root = new VisualElement();
            var driver = new ImmediateMotionDriver();
            var player = new RewardMagnetFeedbackPlayer(root, driver);

            Assert.That(root.Q<VisualElement>("reward-magnet-overlay"), Is.Not.Null);

            player.Dispose();
            Assert.That(root.Q<VisualElement>("reward-magnet-overlay"), Is.Null);
        }
    }
}
