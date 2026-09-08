using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;
using PowerMath.UI.Core;

namespace PowerMath.Tests.EditMode
{
    public sealed class FeatureVersionLockTests
    {
        private const string MainMenuUxml = "Assets/Project/UI/MainMenuUI.uxml";
        private const string PlayerMenuUxml = "Assets/Project/UI/MainMenu/PlayerMenuPanel.uxml";

        [Test]
        public void FeatureAvailability_DefaultsToFalseForV1Features_WhenManifestIsNull()
        {
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap, null), Is.False);
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PlayerHub, null), Is.False);
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha, null), Is.False);
        }

        [Test]
        public void FeatureAvailability_DefaultsToFalseForV1Features_WhenFeaturesNullInManifest()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.0.0.0",
                features = null
            };

            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap, manifest), Is.False);
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PlayerHub, manifest), Is.False);
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha, manifest), Is.False);
        }

        [Test]
        public void FeatureAvailability_ReflectsManifestFlagsWhenConfigured()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.0.0.0",
                features = new GameVersionManifest.FeatureFlags
                {
                    biomeMap = true,
                    playerHub = false,
                    petGacha = true
                }
            };

            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap, manifest), Is.True);
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PlayerHub, manifest), Is.False);
            Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha, manifest), Is.True);
        }

        [Test]
        public void FeatureAvailability_UsesPlayerSessionStoreManifest_WhenAvailable()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.1.0.0",
                features = new GameVersionManifest.FeatureFlags
                {
                    biomeMap = true,
                    playerHub = true,
                    petGacha = true
                }
            };

            PlayerSessionStore.Instance?.SetVersionManifest(manifest);

            if (PlayerSessionStore.Instance != null)
            {
                Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap), Is.True);
                Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PlayerHub), Is.True);
                Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha), Is.True);

                // Reset back to null
                PlayerSessionStore.Instance.SetVersionManifest(null);
                Assert.That(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap), Is.False);
            }
        }

        [Test]
        public void MainMenuUxml_ContainsChainLocksOnRestrictedFeatures_AndLeavesRebirthUnchained()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxml);
            Assert.That(tree, Is.Not.Null, $"Failed to load {MainMenuUxml}");

            VisualElement root = tree.CloneTree();

            Button mapButton = root.Q<Button>("combat-map-button");
            Assert.That(mapButton, Is.Not.Null);
            ChainLockVectorElement mapLock = mapButton.Q<ChainLockVectorElement>("combat-map-lock");
            Assert.That(mapLock, Is.Not.Null, "combat-map-button must contain combat-map-lock ChainLockVectorElement");

            Button hubButton = root.Q<Button>("player-hub-button");
            Assert.That(hubButton, Is.Not.Null);
            ChainLockVectorElement hubLock = hubButton.Q<ChainLockVectorElement>("player-hub-lock");
            Assert.That(hubLock, Is.Not.Null, "player-hub-button must contain player-hub-lock ChainLockVectorElement");

            Button gachaButton = root.Q<Button>("pet-gacha-button");
            Assert.That(gachaButton, Is.Not.Null);
            ChainLockVectorElement gachaLock = gachaButton.Q<ChainLockVectorElement>("pet-gacha-lock");
            Assert.That(gachaLock, Is.Not.Null, "pet-gacha-button must contain pet-gacha-lock ChainLockVectorElement");

            Button rebirthButton = root.Q<Button>("rebirth-button");
            Assert.That(rebirthButton, Is.Not.Null);
            ChainLockVectorElement rebirthLock = rebirthButton.Q<ChainLockVectorElement>();
            Assert.That(rebirthLock, Is.Null, "rebirth-button must NOT contain any ChainLockVectorElement");
        }

        [Test]
        public void PlayerMenuUxml_ContainsChainLocksOnRestrictedFeatures_AndLeavesRebirthUnchained()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PlayerMenuUxml);
            Assert.That(tree, Is.Not.Null, $"Failed to load {PlayerMenuUxml}");

            VisualElement root = tree.CloneTree();

            Button hubButton = root.Q<Button>("player-hub-button");
            Assert.That(hubButton, Is.Not.Null);
            ChainLockVectorElement hubLock = hubButton.Q<ChainLockVectorElement>("player-hub-lock");
            Assert.That(hubLock, Is.Not.Null, "player-hub-button in PlayerMenuPanel must contain player-hub-lock");

            Button gachaButton = root.Q<Button>("pet-gacha-button");
            Assert.That(gachaButton, Is.Not.Null);
            ChainLockVectorElement gachaLock = gachaButton.Q<ChainLockVectorElement>("pet-gacha-lock");
            Assert.That(gachaLock, Is.Not.Null, "pet-gacha-button in PlayerMenuPanel must contain pet-gacha-lock");

            Button rebirthButton = root.Q<Button>("rebirth-button");
            Assert.That(rebirthButton, Is.Not.Null);
            ChainLockVectorElement rebirthLock = rebirthButton.Q<ChainLockVectorElement>();
            Assert.That(rebirthLock, Is.Null, "rebirth-button in PlayerMenuPanel must NOT contain any ChainLockVectorElement");
        }
    }
}
