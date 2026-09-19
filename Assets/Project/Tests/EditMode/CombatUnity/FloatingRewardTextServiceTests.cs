using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class FloatingRewardTextServiceTests
    {
        [Test]
        public void StyleDefinition_ReturnsCorrectCurrencyColors()
        {
            var style = ScriptableObject.CreateInstance<FloatingRewardTextStyleDefinition>();
            try
            {
                Color silver = style.GetColor(RewardCurrencyKind.RankSilver);
                Color gold = style.GetColor(RewardCurrencyKind.RankGold);
                Color diamond = style.GetColor(RewardCurrencyKind.RankDiamond);
                Color powerCoin = style.GetColor(RewardCurrencyKind.PowerCoin);

                Assert.That(silver.r, Is.EqualTo(0.76f).Within(0.01f));
                Assert.That(silver.g, Is.EqualTo(0.82f).Within(0.01f));
                Assert.That(silver.b, Is.EqualTo(0.89f).Within(0.01f));

                Assert.That(gold.r, Is.EqualTo(1.0f).Within(0.01f));
                Assert.That(gold.g, Is.EqualTo(0.78f).Within(0.01f));
                Assert.That(gold.b, Is.EqualTo(0.25f).Within(0.01f));

                Assert.That(diamond.r, Is.EqualTo(0.41f).Within(0.01f));
                Assert.That(diamond.g, Is.EqualTo(0.87f).Within(0.01f));
                Assert.That(diamond.b, Is.EqualTo(0.95f).Within(0.01f));

                // Power Coin must be orange
                Assert.That(powerCoin.r, Is.EqualTo(1.0f).Within(0.01f));
                Assert.That(powerCoin.g, Is.EqualTo(0.55f).Within(0.01f));
                Assert.That(powerCoin.b, Is.EqualTo(0.05f).Within(0.01f));
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
                var pool = new FloatingRewardTextPool(root, null, 3);
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
        public void Service_Spawn_SetsTextFormatAndColor()
        {
            var hostGo = new GameObject("ServiceHost", typeof(RectTransform));
            var root = hostGo.GetComponent<RectTransform>();
            var service = hostGo.AddComponent<FloatingRewardTextService>();
            var style = ScriptableObject.CreateInstance<FloatingRewardTextStyleDefinition>();

            try
            {
                service.Initialize(root, null, null, style, false);
                Assert.That(service.IsReady, Is.True);

                // Spawn Gold floater
                FloatingRewardTextView goldView = service.Spawn(RewardCurrencyKind.RankGold, 750, Vector2.zero);
                Assert.That(goldView, Is.Not.Null);
                Assert.That(goldView.TextComponent.text, Is.EqualTo("+750"));
                Assert.That(goldView.TextComponent.color.r, Is.EqualTo(style.GoldColor.r).Within(0.01f));
                Assert.That(goldView.TextComponent.color.g, Is.EqualTo(style.GoldColor.g).Within(0.01f));
                Assert.That(goldView.TextComponent.color.b, Is.EqualTo(style.GoldColor.b).Within(0.01f));

                // Spawn Power Coin floater (must be orange)
                FloatingRewardTextView powerView = service.Spawn(RewardCurrencyKind.PowerCoin, 42, Vector2.zero);
                Assert.That(powerView, Is.Not.Null);
                Assert.That(powerView.TextComponent.text, Is.EqualTo("+42"));
                Assert.That(powerView.TextComponent.color.r, Is.EqualTo(style.PowerCoinColor.r).Within(0.01f));
                Assert.That(powerView.TextComponent.color.g, Is.EqualTo(style.PowerCoinColor.g).Within(0.01f));
                Assert.That(powerView.TextComponent.color.b, Is.EqualTo(style.PowerCoinColor.b).Within(0.01f));

                // Spawn Silver floater
                FloatingRewardTextView silverView = service.Spawn(RewardCurrencyKind.RankSilver, 100, Vector2.zero);
                Assert.That(silverView, Is.Not.Null);
                Assert.That(silverView.TextComponent.text, Is.EqualTo("+100"));
                Assert.That(silverView.TextComponent.color.r, Is.EqualTo(style.SilverColor.r).Within(0.01f));

                // Spawn Diamond floater
                FloatingRewardTextView diamondView = service.Spawn(RewardCurrencyKind.RankDiamond, 5, Vector2.zero);
                Assert.That(diamondView, Is.Not.Null);
                Assert.That(diamondView.TextComponent.text, Is.EqualTo("+5"));
                Assert.That(diamondView.TextComponent.color.r, Is.EqualTo(style.DiamondColor.r).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(style);
                Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void Service_Spawn_IgnoresZeroOrNegativeAmount()
        {
            var hostGo = new GameObject("ServiceHost", typeof(RectTransform));
            var root = hostGo.GetComponent<RectTransform>();
            var service = hostGo.AddComponent<FloatingRewardTextService>();

            try
            {
                service.Initialize(root, null, null, null, false);
                FloatingRewardTextView zeroView = service.Spawn(RewardCurrencyKind.PowerCoin, 0, Vector2.zero);
                FloatingRewardTextView negView = service.Spawn(RewardCurrencyKind.RankGold, -10, Vector2.zero);

                Assert.That(zeroView, Is.Null);
                Assert.That(negView, Is.Null);
                Assert.That(service.ActiveCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void StyleDefinition_DefaultJuiceParameters_AreValid()
        {
            var style = ScriptableObject.CreateInstance<FloatingRewardTextStyleDefinition>();
            try
            {
                Assert.That(style.BurstHeight, Is.GreaterThan(0f));
                Assert.That(style.FallHorizontalDistance, Is.GreaterThan(0f));
                Assert.That(style.DropDistance, Is.GreaterThanOrEqualTo(80f)); // Grounded drop
                Assert.That(style.DropSeconds, Is.GreaterThan(0f));
                Assert.That(style.PopHoldSeconds, Is.GreaterThan(0f));
                Assert.That(style.OffscreenPadding, Is.GreaterThan(0f));
                Assert.That(style.RotationalKick, Is.GreaterThan(0f));
                Assert.That(style.FlashSeconds, Is.GreaterThan(0f));
                Assert.That(style.SquashStretchFactor, Is.GreaterThanOrEqualTo(1.0f));
                Assert.That(style.SpawnJitterX, Is.GreaterThan(0f));
                Assert.That(style.SpawnJitterY, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(style);
            }
        }

        [Test]
        public void View_SetFrame_AppliesSquashStretchRotationAndColor()
        {
            var go = new GameObject("TestView", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(FloatingRewardTextView));
            var view = go.GetComponent<FloatingRewardTextView>();
            view.ConfigureRuntimeComponents(go.GetComponent<TextMeshProUGUI>(), go.GetComponent<CanvasGroup>());

            try
            {
                Vector3 targetScale = new Vector3(0.75f, 1.45f, 1f);
                Vector2 targetPos = new Vector2(25f, 60f);
                float targetRot = 8.5f;
                Color targetColor = new Color(1f, 0.55f, 0.05f, 1f);

                view.SetFrame(targetScale, 0.85f, targetPos, targetRot, targetColor);

                Assert.That(view.RectTransform.localScale.x, Is.EqualTo(0.75f).Within(0.001f));
                Assert.That(view.RectTransform.localScale.y, Is.EqualTo(1.45f).Within(0.001f));
                Assert.That(view.RectTransform.anchoredPosition.x, Is.EqualTo(25f).Within(0.001f));
                Assert.That(view.RectTransform.anchoredPosition.y, Is.EqualTo(60f).Within(0.001f));
                Assert.That(view.RectTransform.localEulerAngles.z, Is.EqualTo(8.5f).Within(0.01f));
                Assert.That(view.TextComponent.color.g, Is.EqualTo(0.55f).Within(0.01f));

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
        public void View_IconImage_AppliesAndResetsProperly()
        {
            var rootGo = new GameObject("FRTRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(FloatingRewardTextView));
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(rootGo.transform, false);
            var iconGo = new GameObject("IconImage", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            iconGo.transform.SetParent(rootGo.transform, false);

            var view = rootGo.GetComponent<FloatingRewardTextView>();
            var text = textGo.GetComponent<TextMeshProUGUI>();
            var iconImage = iconGo.GetComponent<UnityEngine.UI.Image>();
            var testSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);

            try
            {
                view.ConfigureRuntimeComponents(text, rootGo.GetComponent<CanvasGroup>(), null, iconImage);

                // Floater without icon
                view.Apply("+50", Color.white, 56f, Vector2.zero, 0.5f, null);
                Assert.That(iconImage.gameObject.activeSelf, Is.False);

                // Floater with icon
                view.Apply("+100", Color.yellow, 56f, Vector2.zero, 0.5f, testSprite);
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
