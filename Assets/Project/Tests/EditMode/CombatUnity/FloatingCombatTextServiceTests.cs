using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class FloatingCombatTextServiceTests
    {
        private sealed class TestCombatAnchor : ICombatAnchor
        {
            public bool TryGetLocalPoint(RectTransform overlayRoot, Camera camera, out Vector2 point)
            {
                point = Vector2.zero;
                return true;
            }
        }

        [Test]
        public void StyleDefinition_DefaultJuiceParameters_AreValid()
        {
            var style = ScriptableObject.CreateInstance<FloatingCombatTextStyleDefinition>();
            try
            {
                Assert.That(style.BurstHeight, Is.GreaterThan(0f));
                Assert.That(style.DropDistance, Is.GreaterThanOrEqualTo(80f)); // Grounded drop
                Assert.That(style.DropSeconds, Is.GreaterThan(0f));
                Assert.That(style.PopHoldSeconds, Is.GreaterThan(0f));
                Assert.That(style.FadeStartNormalized, Is.GreaterThanOrEqualTo(0.5f));
                Assert.That(style.FallHorizontalDistance, Is.GreaterThan(0f));
                Assert.That(style.OffscreenPadding, Is.GreaterThan(0f));
                Assert.That(style.RotationalKick, Is.GreaterThan(0f));
                Assert.That(style.FlashSeconds, Is.GreaterThan(0f));
                Assert.That(style.SquashStretchFactor, Is.GreaterThanOrEqualTo(1.0f));
                Assert.That(style.SpawnJitterX, Is.GreaterThan(0f));
                Assert.That(style.SpawnJitterY, Is.GreaterThan(0f));
                Assert.That(style.NormalColor.a, Is.GreaterThan(0f));
                Assert.That(style.CriticalColor.a, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(style);
            }
        }

        [Test]
        public void Pool_AcquireAndRelease_MaintainsCorrectCounts()
        {
            var rootGo = new GameObject("Root", typeof(RectTransform));
            var root = rootGo.GetComponent<RectTransform>();
            try
            {
                var pool = new FloatingCombatTextPool(root, null, 3);
                Assert.That(pool.AvailableCount, Is.EqualTo(3));
                Assert.That(pool.ActiveCount, Is.EqualTo(0));

                Assert.That(pool.TryAcquire(out var view1), Is.True);
                Assert.That(pool.TryAcquire(out var view2), Is.True);
                Assert.That(pool.TryAcquire(out var view3), Is.True);
                Assert.That(pool.TryAcquire(out var view4), Is.False);
                Assert.That(view4, Is.Null);

                Assert.That(pool.ActiveCount, Is.EqualTo(3));
                Assert.That(pool.AvailableCount, Is.EqualTo(0));

                pool.Release(view2);
                Assert.That(pool.ActiveCount, Is.EqualTo(2));
                Assert.That(pool.AvailableCount, Is.EqualTo(1));

                Assert.That(pool.TryAcquire(out var reacquired), Is.True);
                Assert.That(reacquired, Is.SameAs(view2));

                pool.CancelAll();
                Assert.That(pool.ActiveCount, Is.EqualTo(0));
                Assert.That(pool.AvailableCount, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void Service_Spawn_NormalDamage_FormatsCorrectly()
        {
            var hostGo = new GameObject("ServiceHost", typeof(RectTransform));
            var root = hostGo.GetComponent<RectTransform>();
            var service = hostGo.AddComponent<FloatingCombatTextService>();
            var style = ScriptableObject.CreateInstance<FloatingCombatTextStyleDefinition>();

            try
            {
                service.Initialize(root, null, null, style, false);
                Assert.That(service.IsReady, Is.True);

                var anchor = new TestCombatAnchor();
                var request = new FloatingCombatTextRequest(
                    presentationId: "attack_1",
                    semantic: FloatingCombatTextSemantic.Damage,
                    acceptedValue: 125,
                    target: anchor,
                    isCritical: false,
                    spawnOrdinal: 0);

                FloatingCombatTextView view = service.Spawn(request);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.TextComponent.text, Is.EqualTo("-125"));
                Assert.That(view.TextComponent.color.r, Is.EqualTo(style.NormalColor.r).Within(0.01f));
                Assert.That(view.TextComponent.color.g, Is.EqualTo(style.NormalColor.g).Within(0.01f));
                Assert.That(view.TextComponent.color.b, Is.EqualTo(style.NormalColor.b).Within(0.01f));
                Assert.That(view.TextComponent.fontSize, Is.EqualTo(style.FontSize).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(style);
                Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void Service_Spawn_CriticalDamage_FormatsCorrectly()
        {
            var hostGo = new GameObject("ServiceHost", typeof(RectTransform));
            var root = hostGo.GetComponent<RectTransform>();
            var service = hostGo.AddComponent<FloatingCombatTextService>();
            var style = ScriptableObject.CreateInstance<FloatingCombatTextStyleDefinition>();

            try
            {
                service.Initialize(root, null, null, style, false);
                Assert.That(service.IsReady, Is.True);

                var anchor = new TestCombatAnchor();
                var request = new FloatingCombatTextRequest(
                    presentationId: "crit_attack_1",
                    semantic: FloatingCombatTextSemantic.Damage,
                    acceptedValue: 450,
                    target: anchor,
                    isCritical: true,
                    spawnOrdinal: 1);

                FloatingCombatTextView view = service.Spawn(request);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.TextComponent.text, Is.EqualTo("-450"));
                Assert.That(view.TextComponent.color.r, Is.EqualTo(style.CriticalColor.r).Within(0.01f));
                Assert.That(view.TextComponent.color.g, Is.EqualTo(style.CriticalColor.g).Within(0.01f));
                Assert.That(view.TextComponent.color.b, Is.EqualTo(style.CriticalColor.b).Within(0.01f));
                Assert.That(view.TextComponent.fontSize, Is.EqualTo(style.FontSize * 1.12f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(style);
                Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void Service_Spawn_IgnoresInvalidRequests()
        {
            var hostGo = new GameObject("ServiceHost", typeof(RectTransform));
            var root = hostGo.GetComponent<RectTransform>();
            var service = hostGo.AddComponent<FloatingCombatTextService>();

            try
            {
                service.Initialize(root, null, null, null, false);
                var anchor = new TestCombatAnchor();

                var zeroRequest = new FloatingCombatTextRequest("p1", FloatingCombatTextSemantic.Damage, 0, anchor, false, 0);
                var negRequest = new FloatingCombatTextRequest("p2", FloatingCombatTextSemantic.Damage, -5, anchor, false, 0);
                var nullTargetRequest = new FloatingCombatTextRequest("p3", FloatingCombatTextSemantic.Damage, 10, null, false, 0);
                var emptyIdRequest = new FloatingCombatTextRequest("", FloatingCombatTextSemantic.Damage, 10, anchor, false, 0);

                Assert.That(service.Spawn(zeroRequest), Is.Null);
                Assert.That(service.Spawn(negRequest), Is.Null);
                Assert.That(service.Spawn(nullTargetRequest), Is.Null);
                Assert.That(service.Spawn(emptyIdRequest), Is.Null);
                Assert.That(service.ActiveCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void View_SetFrame_AppliesSquashStretchRotationAndColor()
        {
            var go = new GameObject("TestCombatView", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(FloatingCombatTextView));
            var view = go.GetComponent<FloatingCombatTextView>();
            view.ConfigureRuntimeComponents(go.GetComponent<TextMeshProUGUI>(), go.GetComponent<CanvasGroup>());

            try
            {
                Vector3 targetScale = new Vector3(0.80f, 1.45f, 1f);
                Vector2 targetPos = new Vector2(30f, 75f);
                float targetRot = 12.5f;
                Color targetColor = new Color(1f, 0.78f, 0.18f, 1f);

                view.SetFrame(targetScale, 0.9f, targetPos, targetRot, targetColor);

                Assert.That(view.RectTransform.localScale.x, Is.EqualTo(0.80f).Within(0.001f));
                Assert.That(view.RectTransform.localScale.y, Is.EqualTo(1.45f).Within(0.001f));
                Assert.That(view.RectTransform.anchoredPosition.x, Is.EqualTo(30f).Within(0.001f));
                Assert.That(view.RectTransform.anchoredPosition.y, Is.EqualTo(75f).Within(0.001f));
                Assert.That(view.RectTransform.localEulerAngles.z, Is.EqualTo(12.5f).Within(0.01f));
                Assert.That(view.TextComponent.color.r, Is.EqualTo(1f).Within(0.01f));
                Assert.That(view.TextComponent.color.g, Is.EqualTo(0.78f).Within(0.01f));

                view.ResetForPool();
                Assert.That(view.RectTransform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(view.RectTransform.localEulerAngles, Is.EqualTo(Vector3.zero));
                Assert.That(view.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void View_SupportsTwoLayers_MaintainsShadowText()
        {
            var rootGo = new GameObject("Root", typeof(RectTransform), typeof(CanvasGroup), typeof(FloatingCombatTextView));
            var fgGo = new GameObject("Foreground", typeof(RectTransform), typeof(TextMeshProUGUI));
            var shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(TextMeshProUGUI));
            fgGo.transform.SetParent(rootGo.transform, false);
            shadowGo.transform.SetParent(rootGo.transform, false);

            var view = rootGo.GetComponent<FloatingCombatTextView>();
            var fgText = fgGo.GetComponent<TextMeshProUGUI>();
            var shadowText = shadowGo.GetComponent<TextMeshProUGUI>();
            shadowText.color = Color.black;

            try
            {
                view.ConfigureRuntimeComponents(fgText, rootGo.GetComponent<CanvasGroup>(), shadowText);
                Assert.That(view.HasShadow, Is.True);
                Assert.That(view.ShadowTextComponent, Is.SameAs(shadowText));

                view.Apply("-250", Color.red, 48f, Vector2.zero);
                Assert.That(fgText.text, Is.EqualTo("-250"));
                Assert.That(shadowText.text, Is.EqualTo("-250"));
                Assert.That(fgText.color.r, Is.EqualTo(1f).Within(0.01f));
                Assert.That(shadowText.color.r, Is.EqualTo(0f).Within(0.01f)); // Shadow remains dark

                view.ResetForPool();
                Assert.That(fgText.text, Is.Empty);
                Assert.That(shadowText.text, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void View_AutoDetectsTwoLayers_InHierarchy()
        {
            var rootGo = new GameObject("FCTTextShadow (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(FloatingCombatTextView));
            var childGo = new GameObject("FCTText (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            childGo.transform.SetParent(rootGo.transform, false);

            var view = rootGo.GetComponent<FloatingCombatTextView>();
            var rootTmp = rootGo.GetComponent<TextMeshProUGUI>();
            var childTmp = childGo.GetComponent<TextMeshProUGUI>();

            try
            {
                // Trigger auto-detection via Apply
                view.Apply("-99", Color.white, 60f, Vector2.zero);

                Assert.That(view.HasShadow, Is.True);
                Assert.That(view.TextComponent, Is.SameAs(childTmp)); // Foreground is child
                Assert.That(view.ShadowTextComponent, Is.SameAs(rootTmp)); // Shadow is root
                Assert.That(childTmp.text, Is.EqualTo("-99"));
                Assert.That(rootTmp.text, Is.EqualTo("-99"));
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void View_IconImage_AppliesAndResetsProperly()
        {
            var rootGo = new GameObject("FCTRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(FloatingCombatTextView));
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(rootGo.transform, false);
            var iconGo = new GameObject("IconImage", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            iconGo.transform.SetParent(rootGo.transform, false);

            var view = rootGo.GetComponent<FloatingCombatTextView>();
            var text = textGo.GetComponent<TextMeshProUGUI>();
            var iconImage = iconGo.GetComponent<UnityEngine.UI.Image>();
            var testSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            try
            {
                view.ConfigureRuntimeComponents(text, rootGo.GetComponent<CanvasGroup>(), null, iconImage);

                // Normal hit (no icon)
                view.Apply("-100", Color.white, 40f, Vector2.zero, null);
                Assert.That(iconImage.gameObject.activeSelf, Is.False);

                // Critical hit (with icon)
                view.Apply("-200", Color.yellow, 45f, Vector2.zero, testSprite);
                Assert.That(iconImage.gameObject.activeSelf, Is.True);
                Assert.That(iconImage.sprite, Is.SameAs(testSprite));

                // Reset for pool
                view.ResetForPool();
                Assert.That(iconImage.gameObject.activeSelf, Is.False);
                Assert.That(iconImage.sprite, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(testSprite);
                Object.DestroyImmediate(rootGo);
            }
        }
    }
}
