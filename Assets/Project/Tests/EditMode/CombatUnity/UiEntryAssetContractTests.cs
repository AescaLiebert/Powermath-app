using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using PowerMath.UI.Core;

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
        private const string PlayerMenuUxml =
            "Assets/Project/UI/MainMenu/PlayerMenuPanel.uxml";
        private const string RebirthUxml =
            "Assets/Project/UI/MainMenu/RebirthPanel.uxml";
        private const string MainMenuTransitionViewScript =
            "Assets/Project/Script/UI/MainMenu/Transitions/MainMenuTransitionView.cs";
        private const string MainMenuTransitionControllerScript =
            "Assets/Project/Script/UI/MainMenu/Transitions/MainMenuTransitionController.cs";
        private const string CombatLobbyCompositionRootScript =
            "Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs";
        private const string PlayerHubFeedbackScript =
            "Assets/Project/Script/UI/MainMenu/RunEconomy/PlayerHubFeedbackPlayer.cs";
        private const string PlayerHubStyle =
            "Assets/Project/UI/MainMenu/PlayerHubPanel.uss";
        private const string MainMenuExperienceStyle =
            "Assets/Project/UI/MainMenu/MainMenuExperience.uss";

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
            VisualElement playerPreparation = Require<VisualElement>(
                root,
                "player-preparation"
            );

            Assert.That(retry.ClassListContains("is-hidden"), Is.True);
            Assert.That(playerPreparation.ClassListContains("is-hidden"), Is.True);
            Assert.That(
                playerPreparation.style.display.keyword,
                Is.EqualTo(StyleKeyword.Null),
                "The hidden preparation overlay must not override display inline."
            );
        }

        [Test]
        public void RebirthAsset_PreservesFigmaHierarchyAndRuntimeContract()
        {
            VisualElement root = Clone(RebirthUxml);

            VisualElement modal = Require<VisualElement>(root, "run-settlement-modal");
            VisualElement window = Require<VisualElement>(root, "Rebirth / Window");
            VisualElement header = Require<VisualElement>(window, "Rebirth / Header");
            VisualElement body = Require<VisualElement>(window, "Rebirth / Body");
            Require<VisualElement>(header, "Rebirth / Title Group");
            Require<Label>(header, "Title");
            Require<Button>(header, "Button / Close");
            Require<VisualElement>(body, "Rebirth / Comparison List");
            Require<Label>(body, "Progress Value");
            Require<Button>(body, "Button / Rebirth");
            Require<Button>(body, "run-settlement-continue");
            Require<Label>(body, "run-settlement-status");

            Assert.That(root.Query<VisualElement>(
                name: "Component / Rebirth Comparison Row").ToList().Count, Is.EqualTo(3));
            Assert.That(root.Query<Label>(className: "rebirth-number").ToList().Count, Is.EqualTo(6));
            Assert.That(root.Query<RebirthDirectionHead>().ToList().Count, Is.EqualTo(3));
            Assert.That(root.Query<Label>().ToList().Any(label => label.text == "MOVE"), Is.False);
            Assert.That(modal.ClassListContains("is-hidden"), Is.True);
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
            Require<ProgressBar>(root, "stage-progress");
            Require<Label>(root, "wallet-summary");
            Require<Label>(root, "loadout-summary");
            Require<Button>(root, "logout-button");
            Require<Button>(root, "leaderboard");
            Button rebirthButton = Require<Button>(root, "rebirth-button");
            Button playerHubButton = Require<Button>(root, "player-hub-button");
            Button petGachaButton = Require<Button>(root, "pet-gacha-button");
            Require<VisualElement>(root, "combat-layer");
            Button mapButton = Require<Button>(root, "combat-map-button");
            Assert.That(mapButton.Q<ChainLockVectorElement>("combat-map-lock"), Is.Not.Null);
            Assert.That(playerHubButton.Q<ChainLockVectorElement>("player-hub-lock"), Is.Not.Null);
            Assert.That(petGachaButton.Q<ChainLockVectorElement>("pet-gacha-lock"), Is.Not.Null);
            Assert.That(rebirthButton.Q<ChainLockVectorElement>(), Is.Null);
            VisualElement navigator = Require<VisualElement>(
                root,
                "combat-map-modal"
            );
            Require<VisualElement>(root, "combat-map-route");
            Require<Button>(root, "combat-map-close");
            Require<Label>(root, "profile-silver-value");
            Require<Label>(root, "profile-gold-value");
            Require<Label>(root, "profile-diamond-value");
            Require<Button>(root, "Collapse Handle");
            Require<Label>(root, "Chevron");
            Require<Label>(root, "Currency_Value");
            Require<Label>(root, "player-menu-atk");
            Require<VisualElement>(root, "Power-Coin_Currency-field");
            Require<VisualElement>(root, "Power-Coin-Icon");
            VisualElement playerLoadout = Require<VisualElement>(
                root,
                "Player Loadout");
            Require<VisualElement>(root, "Round_Panel_outer");
            Require<VisualElement>(root, "Panel_Inner");
            Require<Label>(root, "CharacterName");
            VisualElement health = Require<VisualElement>(root, "Health");
            VisualElement inventory = Require<VisualElement>(
                root,
                "Player Loadout / Inventory Row");
            Require<VisualElement>(root, "Quick Slot / Pet");
            Require<VisualElement>(root, "Quick Slot / Weapon");
            Require<VisualElement>(root, "Quick Slot / Empty 1");
            Require<Label>(root, "dashboard-pet");
            Require<Label>(root, "dashboard-weapon");
            Assert.That(health.Query<VisualElement>(
                className: "hud-loadout-heart").ToList().Count,
                Is.EqualTo(5));
            Assert.That(inventory.Children().Count(child =>
                child.ClassListContains("hud-loadout-slot")),
                Is.EqualTo(5));
            Assert.That(inventory.parent, Is.SameAs(playerLoadout));
            Require<VisualElement>(root, "combat-enemy-actions");
            Require<VisualElement>(root, "combat-answer-content");
            Require<VisualElement>(root, "combat-feedback-card");
            Require<VisualElement>(root, "combat-score-stack");
            Image correctResultSticker = Require<Image>(
                root,
                "combat-result-sticker-correct");
            Image failResultSticker = Require<Image>(
                root,
                "combat-result-sticker-fail");
            Require<Label>(root, "combat-feedback-title");
            Require<Label>(root, "combat-battle-banner");
            Require<VisualElement>(root, "main-menu-transition-layer");
            Require<VisualElement>(root, "main-menu-transition-cover");
            Require<Label>(root, "battle-start-title");
            Require<VisualElement>(root, "battle-start-accent-left");
            Require<VisualElement>(root, "battle-start-accent-right");
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
                correctResultSticker.style.display.keyword,
                Is.EqualTo(StyleKeyword.Null),
                "The correct sticker must be passive result-panel content."
            );
            Assert.That(
                failResultSticker.style.display.keyword,
                Is.EqualTo(StyleKeyword.Null),
                "The fail sticker must be passive result-panel content."
            );
            Assert.That(correctResultSticker.sprite, Is.Not.Null);
            Assert.That(failResultSticker.sprite, Is.Not.Null);
            string[] mainMenuDependencies = AssetDatabase.GetDependencies(
                MainMenuUxml,
                true);
            Assert.That(mainMenuDependencies, Does.Contain(
                "Assets/Project/Art/Character/Sticker_Power_Correct.PNG"));
            Assert.That(mainMenuDependencies, Does.Contain(
                "Assets/Project/Art/Character/Sticker_Power_Fail.PNG"));
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
            Require<Button>(root, "player-hub-tab-weapon");
            Require<Button>(root, "player-hub-tab-pets");
            Require<VisualElement>(root, "player-hub-pet-grid");
            Require<Image>(root, "player-hub-pet-preview-icon");
            Require<Label>(root, "player-hub-pet-preview-rarity");
            Require<Label>(root, "player-hub-pet-preview-name");
            Require<Label>(root, "player-hub-pet-preview-description");
            Require<Image>(root, "player-hub-equipped-pet");
            Require<Image>(root, "player-hub-equipped-weapon");
            Require<VisualElement>(root, "player-hub-particle-layer");
            Require<VisualElement>(root, "player-hub-milestone");
            Require<VisualElement>(root, "main-menu-utility-bar");
            Require<Button>(root, "main-menu-utility-back");
            Require<Label>(root, "main-menu-utility-power-coins");
            Assert.That(root.Q<Label>("main-menu-utility-title"), Is.Null);
            Assert.That(root.Q<Label>("main-menu-utility-gold"), Is.Null);
            Assert.That(root.Q<Label>("main-menu-utility-diamonds"), Is.Null);
            Require<VisualElement>(root, "main-menu-notification");
            Require<Label>(root, "main-menu-notification-text");

            VisualElement rebirth = Require<VisualElement>(
                root,
                "run-settlement-modal"
            );
            Require<VisualElement>(root, "Rebirth / Window");
            Require<VisualElement>(root, "Rebirth / Header");
            Require<VisualElement>(root, "Rebirth / Title Group");
            Require<Label>(root, "Title");
            Require<VisualElement>(root, "Rebirth / Body");
            Require<VisualElement>(root, "Rebirth / Comparison List");
            Assert.That(root.Query<VisualElement>(
                name: "Component / Rebirth Comparison Row").ToList().Count, Is.EqualTo(3));
            Assert.That(root.Query<Label>(className: "rebirth-number").ToList().Count, Is.EqualTo(6));
            Require<Label>(root, "Progress Value");
            Assert.That(root.Query<RebirthDirectionHead>().ToList().Count, Is.EqualTo(3));
            Require<Label>(root, "run-settlement-status");
            Require<Button>(root, "Button / Close");
            Require<Button>(root, "Button / Rebirth");
            Require<Button>(root, "run-settlement-continue");
            Assert.That(root.Q<Label>("run-settlement-title"), Is.Null);
            Assert.That(root.Query<Label>().ToList().Any(label => label.text == "MOVE"), Is.False);

            Assert.That(playerHub.ClassListContains("is-hidden"), Is.True);
            Assert.That(rebirth.ClassListContains("is-hidden"), Is.True);
            Assert.That(root.Q<VisualElement>("weapon-inventory"), Is.Null);
            Assert.That(root.Q<VisualElement>("pet-equip-button"), Is.Null);
        }

        [Test]
        public void MainMenuTransition_UsesViewportStagingAndSharedMotionDriver()
        {
            string viewSource = File.ReadAllText(MainMenuTransitionViewScript);
            string controllerSource = File.ReadAllText(
                MainMenuTransitionControllerScript);

            Assert.That(viewSource, Does.Contain("Vector2.down *"));
            Assert.That(viewSource, Does.Contain("Vector2.left *"));
            Assert.That(viewSource, Does.Contain("Vector2.up *"));
            Assert.That(viewSource, Does.Contain("style.translate"));
            Assert.That(controllerSource, Does.Contain("IUiMotionDriver"));
            Assert.That(controllerSource, Does.Contain("UiMotionChannel.Lifecycle"));
            Assert.That(controllerSource, Does.Contain("AnimateSessionUi"));
            Assert.That(controllerSource, Does.Not.Contain("LeanTween.value"));
            Assert.That(controllerSource, Does.Not.Contain("LTDescr"));
            Assert.That(controllerSource, Does.Not.Contain("EnsureLeanTweenDriver"));
            Assert.That(controllerSource, Does.Contain("_view.ApplySessionUiProgress"));
            Assert.That(controllerSource, Does.Not.Contain("SessionStaggerSeconds"));
            Assert.That(controllerSource, Does.Not.Contain("PlaySessionReturn"));
            Assert.That(controllerSource, Does.Not.Contain("PanelClosed"));
        }

        [Test]
        public void InvalidQuestionFallback_ReleasesBootstrapPresentation()
        {
            string source = File.ReadAllText(CombatLobbyCompositionRootScript);
            int messageIndex = source.IndexOf(
                "Questions are offline and the development fallback is invalid.",
                System.StringComparison.Ordinal);
            Assert.That(messageIndex, Is.GreaterThanOrEqualTo(0));

            int handlerIndex = source.LastIndexOf(
                "SetUnavailable(",
                messageIndex,
                System.StringComparison.Ordinal);
            Assert.That(handlerIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                source.Substring(handlerIndex, messageIndex - handlerIndex),
                Does.Not.Contain("_view?"),
                "Fallback failure must use the handler that restores final UI state.");
        }

        [Test]
        public void QuestionFallback_IsIsolatedFromAuthoritativeProgression()
        {
            string source = File.ReadAllText(CombatLobbyCompositionRootScript);

            Assert.That(source, Does.Contain("isolateQuestionFallback: true"));
            Assert.That(source, Does.Contain(
                "isolateQuestionFallback || snapshot.academic == null"));
            Assert.That(source, Does.Contain(
                "progressionStore == null || isolateQuestionFallback"));
            Assert.That(source, Does.Contain(
                "progressionStore != null && !isolateQuestionFallback"));
            Assert.That(source, Does.Contain(
                "PRACTICE QUESTIONS ACTIVE; PROGRESS IS NOT SAVED"));
            Assert.That(source, Does.Contain(
                "? \"practice-\" + System.Guid.NewGuid().ToString(\"N\")"));
        }

        [Test]
        public void PlayerHubFeedback_UsesDriverWithoutConflictingTransforms()
        {
            string feedbackSource = File.ReadAllText(PlayerHubFeedbackScript);
            string styleSource = File.ReadAllText(PlayerHubStyle);

            Assert.That(feedbackSource, Does.Contain("IUiMotionDriver"));
            Assert.That(feedbackSource, Does.Contain("UiMotionChannel.Feedback"));
            Assert.That(feedbackSource, Does.Contain("UiMotionChannel.Ambient"));
            Assert.That(feedbackSource, Does.Not.Contain("LeanTween."));
            Assert.That(feedbackSource, Does.Not.Contain("LTDescr"));
            Assert.That(
                RuleBody(styleSource, ".player-hub-weapon-aura"),
                Does.Not.Contain("transition-property"));
            Assert.That(
                RuleBody(styleSource, ".player-hub-weapon-icon"),
                Does.Not.Contain("transition-property"));
            Assert.That(
                RuleBody(styleSource, ".player-hub-comparison-card"),
                Does.Not.Contain("scale"));
            Assert.That(
                RuleBody(styleSource, ".player-hub-upgrade"),
                Does.Not.Contain("translate"));
            Assert.That(
                RuleBody(styleSource, ".player-hub-pet-preview-icon"),
                Does.Not.Contain("transition-property"));
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
            ScrollView leaderboardList = Require<ScrollView>(
                root,
                "leaderboard-list"
            );
            Require<Image>(root, ".character-sprite-placeholder");
            Require<VisualElement>(root, "MW-Leaderboard-Throne-bg 1");
            Require<VisualElement>(root, "MW-Leaderboard-Crown 1");
            Require<Label>(root, "leaderboard-throne-stage");
            Require<Label>(root, "leaderboard-throne-silver");
            Require<Label>(root, "leaderboard-self-rank");
            Require<Button>(root, "leaderboard-jump-to-self");
            Assert.That(leaderboard.Q<FigmaGradientElement>(
                "Leaderboard / Screen Gradient"), Is.Not.Null);
            Assert.That(leaderboard.Q<FigmaShadowElement>(
                "Leaderboard / Modal Shadow"), Is.Not.Null);
            Assert.That(leaderboard.Q<LeaderboardPodiumVector>(
                "Vector / Throne Podium"), Is.Not.Null);
            Assert.That(leaderboardList.mode, Is.EqualTo(ScrollViewMode.Vertical));
            Assert.That(
                leaderboardList.horizontalScrollerVisibility,
                Is.EqualTo(ScrollerVisibility.Hidden));
            Assert.That(
                leaderboardList.verticalScrollerVisibility,
                Is.EqualTo(ScrollerVisibility.AlwaysVisible));
            Assert.That(leaderboardList.mouseWheelScrollSize, Is.EqualTo(120f));

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

        [Test]
        public void PlayerMenuAsset_PreservesFigmaStructureAndTokensContract()
        {
            VisualElement root = Clone(PlayerMenuUxml);

            VisualElement playerMenu = Require<VisualElement>(root, "Player Menu");
            VisualElement shadow = Require<FigmaShadowElement>(root, "Player Menu Shadow");
            Button toggle = Require<Button>(root, "Collapse Handle");
            Label chevron = Require<Label>(toggle, "Chevron");
            VisualElement content = Require<VisualElement>(root, "Primary Navigation");
            VisualElement powerCoinField = Require<VisualElement>(
                content,
                "Power-Coin_Currency-field");
            VisualElement currency = Require<VisualElement>(
                powerCoinField,
                "Currency");
            VisualElement powerCoinIcon = Require<VisualElement>(
                powerCoinField,
                "Power-Coin-Icon");
            Label powerCoinValue = Require<Label>(root, "Currency_Value");
            Require<Label>(root, "player-menu-atk");
            Require<Button>(root, "player-hub-button");
            Require<Button>(root, "pet-gacha-button");
            Require<Button>(root, "rebirth-button");

            Assert.That(shadow.parent, Is.SameAs(playerMenu));
            Assert.That(shadow.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(toggle.parent, Is.SameAs(playerMenu));
            Assert.That(content.parent, Is.SameAs(playerMenu));
            Assert.That(currency.parent, Is.SameAs(powerCoinField));
            Assert.That(powerCoinIcon.parent, Is.SameAs(powerCoinField));
            Assert.That(powerCoinValue.parent, Is.SameAs(currency));
            Assert.That(chevron.text, Is.EqualTo("<"));

            string style = File.ReadAllText(MainMenuExperienceStyle);
            string collapsedToggle = RuleBody(
                style,
                ".hud-player-menu-toggle.is-collapsed");
            Assert.That(collapsedToggle, Does.Contain("translate: -230px 0"));
            Assert.That(style, Does.Contain(
                "coin_power.png"));
            Assert.That(style, Does.Contain("--power-coin-field-fill"));
            Assert.That(style, Does.Contain("--figma-shadow-inset: 1"));
            Assert.That(style, Does.Contain("--loadout-slot-fill-0"));
            Assert.That(style, Does.Contain("--figma-gradient-stop-1: 0.58"));
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

        private static string RuleBody(string source, string selector)
        {
            int selectorIndex = source.IndexOf(
                selector,
                System.StringComparison.Ordinal);
            Assert.That(selectorIndex, Is.GreaterThanOrEqualTo(0));
            int bodyStart = source.IndexOf('{', selectorIndex);
            int bodyEnd = source.IndexOf('}', bodyStart + 1);
            Assert.That(bodyStart, Is.GreaterThan(selectorIndex));
            Assert.That(bodyEnd, Is.GreaterThan(bodyStart));
            return source.Substring(bodyStart + 1, bodyEnd - bodyStart - 1);
        }
    }
}
