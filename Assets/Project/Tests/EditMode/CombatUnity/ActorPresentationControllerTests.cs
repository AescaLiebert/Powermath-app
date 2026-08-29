using NUnit.Framework;
using PowerMath.Gameplay.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class ActorPresentationControllerTests
    {
        [Test]
        public void Initialize_DefaultsFctAnchorToCenterWithZeroOffset()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Enemy, false);

                Assert.That(controller.FctNormalizedAnchor, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(controller.FctOffset, Is.EqualTo(Vector2.zero));
                Assert.That(controller.DamageTextAnchor, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RectTransformCombatAnchor_ResolvesLocalCenterPoint()
        {
            var canvasGo = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            var root = canvasGo.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(1000f, 1000f);

            var targetGo = new GameObject("Target", typeof(RectTransform));
            targetGo.transform.SetParent(root, false);
            var targetRect = targetGo.GetComponent<RectTransform>();
            targetRect.sizeDelta = new Vector2(200f, 200f);
            targetRect.anchoredPosition = new Vector2(100f, 150f);

            try
            {
                var anchor = new RectTransformCombatAnchor(
                    targetRect,
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero);

                bool success = anchor.TryGetLocalPoint(root, null, out Vector2 localPoint);
                Assert.That(success, Is.True);
                Assert.That(localPoint.x, Is.EqualTo(100f).Within(0.01f));
                Assert.That(localPoint.y, Is.EqualTo(150f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void Initialize_CachesAuthoredColor_AndRestoreResetsColor()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var image = go.GetComponent<Image>();
                Color customColor = new Color(0.8f, 0.9f, 1f, 1f);
                image.color = customColor;

                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Player, false);

                Assert.That(controller.AuthoredColor, Is.EqualTo(customColor));
                Assert.That(controller.CurrentColor, Is.EqualTo(customColor));

                // Mutate color directly
                image.color = Color.red;
                Assert.That(controller.CurrentColor, Is.EqualTo(Color.red));

                // Restore
                controller.RestoreAuthoredPose(resetAlpha: true);
                Assert.That(controller.CurrentColor, Is.EqualTo(customColor));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CombatJuiceProfile_ExposesHitFlashProperties()
        {
            var profile = ScriptableObject.CreateInstance<CombatJuiceProfileDefinition>();
            try
            {
                Assert.That(profile.HitFlashRed.r, Is.GreaterThan(0.5f));
                Assert.That(profile.HitFlashWhite, Is.EqualTo(Color.white));
                Assert.That(profile.HitFlashCycles, Is.GreaterThanOrEqualTo(1));
                Assert.That(profile.HitFlashIntensity, Is.GreaterThan(0f));
                Assert.That(profile.DieHoldThreshold, Is.EqualTo(0.40f).Within(0.01f));
                Assert.That(profile.DieWhiteFlashThreshold, Is.EqualTo(0.55f).Within(0.01f));
                Assert.That(profile.DieDisappearThreshold, Is.EqualTo(0.75f).Within(0.01f));
                Assert.That(profile.RewardPopSeconds, Is.GreaterThan(0f));
                Assert.That(profile.RewardMagnetFlightSeconds, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void DieState_CompletesAndSetsActorToHidden()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Enemy, true);

                bool whiteFlashed = false;
                bool rewardDropped = false;
                controller.DyingWhiteFlashReached += () => whiteFlashed = true;
                controller.RewardDropTriggered += () => rewardDropped = true;

                var routine = controller.Play(PresentationActionKind.EnemyDie);
                while (routine.MoveNext()) { }

                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Hidden));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
