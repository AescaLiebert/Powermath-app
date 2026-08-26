using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class UiEntryAssetContractTests
    {
        private const string AuthenticationUxml =
            "Assets/Project/UI/AuthenticationUI.uxml";
        private const string BootstrapUxml =
            "Assets/Project/UI/BootstrapUI.uxml";
        private const string MainMenuUxml =
            "Assets/Project/UI/MainMenuUI.uxml";

        [Test]
        public void AuthenticationAsset_PreservesLoginContract()
        {
            VisualElement root = Clone(AuthenticationUxml);

            Require<VisualElement>(root, "login-screen");
            Require<VisualElement>(root, "safe-area");
            Require<VisualElement>(root, "login-panel");
            Require<Label>(root, "auth-title");
            Require<TextField>(root, "username-field");
            TextField password = Require<TextField>(root, "password-field");
            Require<Toggle>(root, "remember-device-toggle");
            Require<Label>(root, "auth-status");
            Require<Button>(root, "login-button");

            Assert.That(password.maxLength, Is.EqualTo(6));
            Assert.That(password.isPasswordField, Is.True);
            Assert.That(root.Q<VisualElement>("guest-button"), Is.Null);
            Assert.That(root.Q<VisualElement>("email-field"), Is.Null);
        }

        [Test]
        public void BootstrapAsset_PreservesTruthfulStateContract()
        {
            VisualElement root = Clone(BootstrapUxml);

            Require<VisualElement>(root, "bootstrap-screen");
            Require<VisualElement>(root, "safe-area");
            Require<VisualElement>(root, "bootstrap-card");
            Require<Label>(root, "bootstrap-status");
            Require<ProgressBar>(root, "bootstrap-progress");
            Require<Label>(root, "bootstrap-detail");
            Button retry = Require<Button>(root, "retry-button");

            Assert.That(retry.ClassListContains("is-hidden"), Is.True);
        }

        [Test]
        public void MainMenuAsset_PreservesShellCombatAndNavigatorContracts()
        {
            VisualElement root = Clone(MainMenuUxml);

            Require<VisualElement>(root, "main-menu-screen");
            Require<VisualElement>(root, "safe-area");
            Require<VisualElement>(root, "content");
            Require<VisualElement>(root, "profile-panel");
            Require<Label>(root, "player-display-name");
            Require<Label>(root, "current-stage-label");
            Require<ProgressBar>(root, "stage-progress");
            Require<Label>(root, "wallet-summary");
            Require<Label>(root, "loadout-summary");
            Require<Button>(root, "logout-button");
            Require<Button>(root, "leaderboard");
            Require<Button>(root, "rebirth-button");
            Require<Button>(root, "player-hub-button");
            Require<Button>(root, "pet-gacha-button");
            Require<VisualElement>(root, "combat-layer");
            Require<Button>(root, "combat-attack-button");
            Require<Button>(root, "combat-map-button");
            VisualElement navigator = Require<VisualElement>(
                root,
                "combat-map-modal"
            );
            Require<VisualElement>(root, "combat-map-route");
            Require<Button>(root, "combat-map-close");
            Require<Label>(root, "profile-silver-value");
            Require<Label>(root, "profile-gold-value");
            Require<Label>(root, "profile-diamond-value");
            Require<Button>(root, "player-menu-toggle");
            Require<Label>(root, "player-menu-power-coins");
            Require<Label>(root, "player-menu-atk");
            Require<VisualElement>(root, "player-dashboard");
            Require<Label>(root, "dashboard-pet");
            Require<Label>(root, "dashboard-weapon");
            Require<VisualElement>(root, "combat-enemy-actions");
            VisualElement enemyHpShell = Require<VisualElement>(
                root,
                "combat-enemy-hp-shell"
            );
            Label enemyName = Require<Label>(root, "combat-enemy-name");
            Label enemyHp = Require<Label>(root, "combat-enemy-hp-label");

            Assert.That(
                enemyHpShell.Q<Label>("combat-enemy-name"),
                Is.SameAs(enemyName)
            );
            Assert.That(
                enemyHpShell.Q<Label>("combat-enemy-hp-label"),
                Is.SameAs(enemyHp)
            );

            Assert.That(navigator.ClassListContains("is-hidden"), Is.True);
            Assert.That(
                root.Q<VisualElement>("combat-enemy-image"),
                Is.Null,
                "Enemy art must remain on the scene Canvas, not UI Toolkit.");

            TemplateContainer[] passThroughLayers = root
                .Query<TemplateContainer>(className: "mw-template-fill")
                .ToList()
                .ToArray();
            Assert.That(passThroughLayers.Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(
                passThroughLayers.All(layer =>
                    layer.pickingMode == PickingMode.Ignore),
                Is.True,
                "Full-screen template layers must not intercept button clicks.");

            Assert.That(root.Q<Button>("combat-map-button").text.Length, Is.EqualTo(1));
            Assert.That(root.Q<Button>("leaderboard").text.Length, Is.EqualTo(1));
            Assert.That(root.Q<Button>("setting").text.Length, Is.EqualTo(1));
        }

        [Test]
        public void MainMenuAsset_PreservesPlayerHubAndRebirthContracts()
        {
            VisualElement root = Clone(MainMenuUxml);

            VisualElement playerHub = Require<VisualElement>(
                root,
                "player-hub-modal"
            );
            Require<Button>(root, "player-hub-close");
            Require<Label>(root, "player-hub-effective-atk");
            Require<Label>(root, "player-hub-atk-breakdown");
            Require<Label>(root, "player-hub-legacy-bonus");
            Require<Label>(root, "player-hub-pet-status");
            Require<Label>(root, "player-hub-weapon-name");
            Require<Label>(root, "player-hub-weapon-current");
            Require<Label>(root, "player-hub-weapon-next");
            Require<Label>(root, "player-hub-weapon-cost");
            Require<Label>(root, "player-hub-balance");
            Require<Button>(root, "player-hub-weapon-upgrade");
            Require<Label>(root, "player-hub-status");

            VisualElement rebirth = Require<VisualElement>(
                root,
                "run-settlement-modal"
            );
            Require<Label>(root, "run-settlement-title");
            Require<Label>(root, "run-settlement-stage");
            Require<Label>(root, "run-settlement-coins");
            Require<Label>(root, "run-settlement-coins-gain");
            Require<Label>(root, "run-settlement-legacy");
            Require<Label>(root, "run-settlement-legacy-gain");
            Require<Label>(root, "run-settlement-attack");
            Require<Label>(root, "run-settlement-prestige");
            Require<Label>(root, "run-settlement-status");
            Require<Button>(root, "run-settlement-close");
            Require<Button>(root, "run-settlement-confirm");
            Require<Button>(root, "run-settlement-continue");

            Assert.That(playerHub.ClassListContains("is-hidden"), Is.True);
            Assert.That(rebirth.ClassListContains("is-hidden"), Is.True);
            Assert.That(root.Q<VisualElement>("weapon-inventory"), Is.Null);
            Assert.That(root.Q<VisualElement>("pet-equip-button"), Is.Null);
        }

        [Test]
        public void MainMenuAsset_PreservesLeaderboardAndProfileAnalyticsContracts()
        {
            VisualElement root = Clone(MainMenuUxml);

            VisualElement leaderboard = Require<VisualElement>(
                root,
                "leaderboard-modal"
            );
            Require<Label>(root, "leaderboard-cohort");
            Require<Label>(root, "leaderboard-throne");
            Require<Label>(root, "leaderboard-self");
            Require<Label>(root, "leaderboard-status");
            Require<Button>(root, "leaderboard-refresh");
            Require<Button>(root, "leaderboard-close");
            Require<ScrollView>(root, "leaderboard-list");

            VisualElement profile = Require<VisualElement>(
                root,
                "profile-analytics-modal"
            );
            Require<Button>(root, "profile-analytics-close");
            Require<Label>(root, "profile-summary");
            Require<Label>(root, "profile-adventure");
            Require<Label>(root, "profile-economy");
            Require<Label>(root, "profile-learning");
            Require<Label>(root, "profile-ranks");
            TextField displayName = Require<TextField>(
                root,
                "profile-display-name-input"
            );
            Require<Button>(root, "profile-display-name-save");
            Require<Label>(root, "profile-display-name-status");

            Assert.That(leaderboard.focusable, Is.True);
            Assert.That(profile.focusable, Is.True);
            Assert.That(leaderboard.ClassListContains("is-hidden"), Is.True);
            Assert.That(profile.ClassListContains("is-hidden"), Is.True);
            Assert.That(displayName.maxLength, Is.EqualTo(20));

            string allText = string.Join(" ", root.Query<Label>().ToList()
                .Select(label => label.text ?? string.Empty)).ToUpperInvariant();
            Assert.That(allText, Does.Not.Contain("AUDIT SCORE"));
            Assert.That(allText, Does.Not.Contain("AUDIT COUNT"));
            Assert.That(allText, Does.Not.Contain("FOCUS NEXT"));
            Assert.That(allText, Does.Not.Contain("MASTERY"));
            Assert.That(allText, Does.Not.Contain("STREAK"));
            Assert.That(root.Q<VisualElement>("leaderboard-grade-tabs"), Is.Null);
        }

        private static VisualElement Clone(string path)
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                path
            );
            Assert.That(asset, Is.Not.Null, $"UXML must be importable: {path}");
            return asset.CloneTree();
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            T element = root.Q<T>(name);
            Assert.That(element, Is.Not.Null, $"Missing {typeof(T).Name} '{name}'.");
            return element;
        }
    }
}
