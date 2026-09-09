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
                Assert.That(profile.NormalDeathSeconds, Is.EqualTo(0.70f).Within(0.01f));
                Assert.That(profile.MajorDeathSeconds, Is.EqualTo(1.60f).Within(0.01f));
                Assert.That(profile.ReducedNormalDeathSeconds, Is.EqualTo(0.50f).Within(0.01f));
                Assert.That(profile.ReducedMajorDeathSeconds, Is.EqualTo(1.00f).Within(0.01f));
                Assert.That(profile.MajorDeathSeconds, Is.GreaterThan(profile.NormalDeathSeconds));
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
                Assert.That(whiteFlashed, Is.True);
                Assert.That(rewardDropped, Is.True);
                Assert.That(go.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AppearState_WhenActorIsHiddenAndInactive_ActivatesAndTransitionsToIdle()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Enemy, true);

                // Simulate actor having died and become hidden / inactive
                controller.CancelAndApply(ActorVisualState.Hidden);
                Assert.That(go.activeSelf, Is.False);
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Hidden));

                // Playing appear should activate the game object and transition to Idle
                var routine = controller.Play(PresentationActionKind.EnemyAppear);
                while (routine.MoveNext()) { }

                Assert.That(go.activeSelf, Is.True);
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Idle));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ConfigureSprites_SwapsSpriteOnStateChange_IdleAttackHurt()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            var tex = new Texture2D(4, 4);
            var idle = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.zero);
            var attack = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.zero);
            var hurt = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.zero);
            try
            {
                var image = go.GetComponent<Image>();
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Player, true);
                controller.ConfigureSprites(idle, attack, hurt);

                Assert.That(image.sprite, Is.EqualTo(idle));

                // Attack transition
                var attackRoutine = controller.Play(PresentationActionKind.PlayerPrimaryAttack);
                attackRoutine.MoveNext();
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Attacking));
                Assert.That(image.sprite, Is.EqualTo(attack));
                while (attackRoutine.MoveNext()) { }
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Idle));
                Assert.That(image.sprite, Is.EqualTo(idle));

                // Take damage transition
                var damageRoutine = controller.Play(PresentationActionKind.PlayerTakeDamage);
                damageRoutine.MoveNext();
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.TakingDamage));
                Assert.That(image.sprite, Is.EqualTo(hurt));
                while (damageRoutine.MoveNext()) { }
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Idle));
                Assert.That(image.sprite, Is.EqualTo(idle));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(idle);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(hurt);
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void ConfigureSprites_FallbackToIdle_WhenAttackOrHurtNull()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            var tex = new Texture2D(4, 4);
            var idle = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.zero);
            try
            {
                var image = go.GetComponent<Image>();
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Player, true);
                controller.ConfigureSprites(idle, null, null);

                Assert.That(image.sprite, Is.EqualTo(idle));

                var attackRoutine = controller.Play(PresentationActionKind.PlayerPrimaryAttack);
                attackRoutine.MoveNext();
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Attacking));
                Assert.That(image.sprite, Is.EqualTo(idle));
                while (attackRoutine.MoveNext()) { }

                var damageRoutine = controller.Play(PresentationActionKind.PlayerTakeDamage);
                damageRoutine.MoveNext();
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.TakingDamage));
                Assert.That(image.sprite, Is.EqualTo(idle));
                while (damageRoutine.MoveNext()) { }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(idle);
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void PointerInteraction_EnterAndExit_TogglesHoverState()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Enemy, true);

                Assert.That(controller.IsPointerHovered, Is.False);
                Assert.That(controller.IsPointerPressed, Is.False);

                controller.SimulatePointerEnter();
                Assert.That(controller.IsPointerHovered, Is.True);

                controller.SimulatePointerExit();
                Assert.That(controller.IsPointerHovered, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PointerInteraction_DownAndUp_TracksPressedAndFiresClick()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Enemy, true);

                bool clicked = false;
                controller.Clicked += () => clicked = true;

                controller.SimulatePointerDown();
                Assert.That(controller.IsPointerPressed, Is.True);

                controller.TriggerClick();
                Assert.That(clicked, Is.True);

                controller.SimulatePointerUp();
                Assert.That(controller.IsPointerPressed, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PointerInteraction_GlobalGateBlocksClick()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Enemy, true);
                bool allowed = false;
                bool clicked = false;
                controller.ConfigureInteractionEligibility(() => allowed);
                controller.Clicked += () => clicked = true;

                controller.TriggerClick();
                Assert.That(clicked, Is.False);

                allowed = true;
                controller.TriggerClick();
                Assert.That(clicked, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DeathProfile_PlayerIsAlwaysMajor_EnemyCanBeConfigured()
        {
            var playerGo = new GameObject("Player", typeof(RectTransform), typeof(Image));
            var enemyGo = new GameObject("Enemy", typeof(RectTransform), typeof(Image));
            try
            {
                var player = playerGo.AddComponent<ActorPresentationController>();
                player.Initialize(PresentationActor.Player, true);
                player.ConfigureDeathProfile(false);
                Assert.That(player.IsMajorDeath, Is.True);

                var enemy = enemyGo.AddComponent<ActorPresentationController>();
                enemy.Initialize(PresentationActor.Enemy, true);
                enemy.ConfigureDeathProfile(false);
                Assert.That(enemy.IsMajorDeath, Is.False);
                enemy.ConfigureDeathProfile(true);
                Assert.That(enemy.IsMajorDeath, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(enemyGo);
            }
        }

        [Test]
        public void PointerInteraction_PlayCombatAction_InterruptsInteraction()
        {
            var go = new GameObject("TestActor", typeof(RectTransform), typeof(Image));
            try
            {
                var controller = go.AddComponent<ActorPresentationController>();
                controller.Initialize(PresentationActor.Player, true);

                controller.SimulatePointerDown();
                Assert.That(controller.IsPointerPressed, Is.True);

                var attackRoutine = controller.Play(PresentationActionKind.PlayerPrimaryAttack);
                attackRoutine.MoveNext();
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Attacking));

                while (attackRoutine.MoveNext()) { }
                Assert.That(controller.State, Is.EqualTo(ActorVisualState.Idle));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
