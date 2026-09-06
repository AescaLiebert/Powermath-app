using NUnit.Framework;
using System.Linq;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.UI.Core;
using UnityEditor;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class CombatLobbyViewTests
    {
        private const string MainMenuUxml =
            "Assets/Project/UI/MainMenuUI.uxml";

        [Test]
        public void MainMenuAsset_SatisfiesCombatViewContract()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            Assert.That(asset, Is.Not.Null, "Main Menu UXML must be importable.");

            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root);

            Assert.DoesNotThrow(view.Bind);
            for (int digit = 0; digit <= 9; digit++)
            {
                Assert.That(
                    root.Q<Button>($"combat-digit-{digit}"),
                    Is.Not.Null,
                    $"Digit {digit} must exist in the combat numpad."
                );
            }
        }

        [Test]
        public void ProfileSummary_PreservesHierarchyAndRuntimeDataElements()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();

            VisualElement profile = root.Q<VisualElement>("profile-panel");
            VisualElement shadow = root.Q<FigmaShadowElement>(
                "Player Summary Shadow");

            Assert.That(profile, Is.Not.Null);
            Assert.That(shadow, Is.Not.Null);
            Assert.That(shadow.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(shadow.parent, Is.SameAs(profile.parent));
            Assert.That(profile.Q<VisualElement>("Player Summary"), Is.Not.Null);
            Assert.That(profile.Q<VisualElement>("Avatar Placeholder"), Is.Not.Null);
            Assert.That(profile.Q<Label>("player-display-name"), Is.Not.Null);
            Assert.That(profile.Q<Label>("profile-silver-value"), Is.Not.Null);
            Assert.That(profile.Q<Label>("profile-gold-value"), Is.Not.Null);
            Assert.That(profile.Q<Label>("profile-diamond-value"), Is.Not.Null);

            foreach (string rankName in new[] { "sil_cur", "gold_cur", "dia_cur" })
            {
                VisualElement rank = profile.Q<VisualElement>(rankName);
                VisualElement currency = rank.Q<VisualElement>("Currency");
                VisualElement icon = rank.Q<VisualElement>("icon");
                Assert.That(currency.parent, Is.SameAs(rank));
                Assert.That(icon.parent, Is.SameAs(rank));
                Assert.That(currency.Q<Label>(), Is.Not.Null,
                    $"{rankName} Currency must own its amount text.");
            }

            VisualElement profileGroup = profile.Q<VisualElement>("profile");
            Assert.That(profile.Q<VisualElement>("ProfilePic").parent,
                Is.SameAs(profileGroup));
        }

        [Test]
        public void NavigatorAndPlayerMenu_PreserveControlsHierarchyAndData()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            VisualElement navigator = root.Q<VisualElement>("main-navigator");
            VisualElement playerMenu = root.Q<VisualElement>("Player Menu");
            VisualElement playerMenuContent = root.Q<VisualElement>(
                "Primary Navigation");

            Assert.That(navigator, Is.Not.Null);
            Assert.That(root.Q<Button>("combat-map-button").parent,
                Is.SameAs(navigator));
            Assert.That(root.Q<Button>("leaderboard").parent,
                Is.SameAs(navigator));
            Assert.That(root.Q<Button>("setting").parent,
                Is.SameAs(navigator));
            Assert.That(navigator.Q<FigmaShadowElement>(
                "Utility / MAP Shadow"), Is.Not.Null);
            Assert.That(navigator.Q<FigmaShadowElement>(
                "Utility / CUP Shadow"), Is.Not.Null);
            Assert.That(navigator.Q<FigmaShadowElement>(
                "Utility / SET Shadow"), Is.Not.Null);

            Assert.That(playerMenu, Is.Not.Null);
            Assert.That(playerMenuContent.parent, Is.SameAs(playerMenu));
            Assert.That(playerMenu.Q<FigmaShadowElement>(
                "Player Menu Shadow"), Is.Not.Null);
            Assert.That(root.Q<Button>("Collapse Handle").parent,
                Is.SameAs(playerMenu));
            VisualElement powerCoinField = playerMenuContent.Q<VisualElement>(
                "Power-Coin_Currency-field");
            Assert.That(powerCoinField, Is.Not.Null);
            Assert.That(root.Q<Label>("Currency_Value").parent.parent,
                Is.SameAs(powerCoinField));
            Assert.That(powerCoinField.Q<VisualElement>("Power-Coin-Icon"),
                Is.Not.Null);
            Assert.That(playerMenuContent.Q<Button>("player-hub-button"),
                Is.Not.Null);
            Assert.That(playerMenuContent.Q<Button>("pet-gacha-button"),
                Is.Not.Null);
            Assert.That(playerMenuContent.Q<Button>("rebirth-button"),
                Is.Not.Null);
        }

        [Test]
        public void Render_ProjectsCombatSnapshotIntoPlayerFacingUi()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root);
            var snapshot = new CombatSnapshot(
                new StageId(6),
                "rock-titan",
                "Rock Titan",
                23,
                40,
                1,
                3,
                2,
                3,
                CombatPhase.EnemyReady,
                true
            );

            view.Render(snapshot);

            Assert.That(root.Q<Label>("combat-stage-label").text, Is.EqualTo("STAGE 6"));
            Assert.That(root.Q<Label>("combat-enemy-name").text, Is.EqualTo("Rock Titan"));
            Assert.That(root.Q<Label>("combat-enemy-hp-label").text, Is.EqualTo("23 / 40"));
            Assert.That(root.Q<Label>("combat-hearts-label").text, Is.EqualTo("♥ ♥ ♡"));
            var heartIcons = root.Query<VisualElement>(
                className: "hud-loadout-heart").ToList();
            Assert.That(heartIcons.Count, Is.EqualTo(5));
            Assert.That(heartIcons[0].ClassListContains("is-empty"), Is.False);
            Assert.That(heartIcons[1].ClassListContains("is-empty"), Is.False);
            Assert.That(heartIcons[2].ClassListContains("is-empty"), Is.True);
            Assert.That(heartIcons[3].ClassListContains("is-unused"), Is.True);
            Assert.That(heartIcons[4].ClassListContains("is-unused"), Is.True);
            VisualElement actions = root.Q<VisualElement>("combat-enemy-actions");
            Assert.That(actions.childCount, Is.EqualTo(1));
            Assert.That(
                actions[0].ClassListContains("hud-enemy-action--attack"),
                Is.True);
            Assert.That(
                actions[0].ClassListContains("hud-enemy-action--danger"),
                Is.True);
            Assert.That(
                actions[0].ClassListContains("figma-grid-gap-right"),
                Is.False,
                "A one-item action row must not reserve a trailing grid gap.");
            Assert.That(view.CanAttack, Is.True);
        }

        [Test]
        public void Render_EnemyActionsUseFixedGapsWithoutStretchingLastItem()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root);
            var snapshot = new CombatSnapshot(
                new StageId(6),
                "rock-titan",
                "Rock Titan",
                40,
                40,
                4,
                4,
                4,
                4,
                CombatPhase.EnemyReady,
                true
            );

            view.Render(snapshot);

            VisualElement actions = root.Q<VisualElement>("combat-enemy-actions");
            Assert.That(actions.childCount, Is.EqualTo(4));
            for (int index = 0; index < actions.childCount - 1; index++)
            {
                Assert.That(
                    actions[index].ClassListContains("figma-grid-gap-right"),
                    Is.True,
                    $"Action {index} must retain the shared fixed gap.");
            }
            Assert.That(
                actions[actions.childCount - 1]
                    .ClassListContains("figma-grid-gap-right"),
                Is.False,
                "The final action must not add a trailing gap.");
        }

        [Test]
        public void AnswerFeedback_ReplacesNumpadWithOrderedVisualStack()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root);

            view.ShowAttempt(true);
            view.SetRetainedQuestionLayout(true);
            view.ShowAnswerFeedback(
                "✓",
                "CORRECT",
                "Building your attack power",
                true);
            view.AddFeedbackStep("BASE ATK", "50", false);
            view.AddFeedbackStep("RESPONSE SCORE 10", "×2.00", false);
            view.AddFeedbackStep("FINAL DAMAGE", "100", true);

            Assert.That(
                root.Q<VisualElement>("combat-attempt-panel")
                    .ClassListContains("combat-attempt-panel--retained-video"),
                Is.True);
            Assert.That(
                root.Q<VisualElement>("combat-answer-content").style.display.value,
                Is.EqualTo(DisplayStyle.None));
            Assert.That(
                root.Q<VisualElement>("combat-feedback-card").style.display.value,
                Is.EqualTo(DisplayStyle.Flex));
            Assert.That(
                root.Q<VisualElement>("combat-score-stack").childCount,
                Is.EqualTo(5),
                "Three score rows should be connected by two visual arrows.");
        }

        [Test]
        public void MainMenuAsset_ProjectsRankWithoutAuditFields()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            using var view = new AcademicProgressionView(root);
            view.Bind();
            view.Render(new AcademicProgressionProjection(
                AcademicRank.Gold,
                new RankCurrencyBalances(2, 3, 4),
                true
            ));

            Assert.That(root.Q<Label>("academic-rank-label").text, Is.EqualTo("RANK GOLD"));
            Assert.That(root.Q<Label>("academic-rank-multiplier").text, Does.Contain("1.5"));
            Assert.That(root.Q<Label>("academic-active-currency").text, Is.EqualTo("Gold: 3"));
            Assert.That(root.Q<VisualElement>("Rank Icon")?.ClassListContains("hud-slot-icon--rank-gold"), Is.True);
            Assert.That(root.Q<VisualElement>("audit-score"), Is.Null);
            Assert.That(root.Q<VisualElement>("audit-count"), Is.Null);
        }

        [Test]
        public void MainMenuAsset_ContainsPetGachaInteractionContract()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml);
            Assert.That(asset, Is.Not.Null);

            VisualElement root = asset.CloneTree();
            foreach (string name in new[]
            {
                "pet-gacha-button",
                "pet-gacha-modal",
                "pet-gacha-balance",
                "pet-gacha-odds-list",
                "pet-gacha-warning",
                "pet-gacha-pull",
                "pet-gacha-confirmation",
                "pet-gacha-confirm",
                "pet-gacha-result",
                "pet-gacha-result-state",
                "pet-gacha-continue"
            })
            {
                Assert.That(root.Q<VisualElement>(name), Is.Not.Null, name);
            }

            Assert.That(
                root.Q<Label>("pet-gacha-warning").text,
                Does.Contain("duplicate").IgnoreCase);
            Assert.That(
                root.Q<VisualElement>("pet-gacha-confirmation")
                    .ClassListContains("is-hidden"),
                Is.True);
            Assert.That(
                root.Q<VisualElement>("pet-gacha-result")
                    .ClassListContains("is-hidden"),
                Is.True);
            VisualElement modal = root.Q<VisualElement>("pet-gacha-modal");
            Assert.That(modal.focusable, Is.True);
            Assert.That(modal.ClassListContains("pet-gacha-modal"), Is.True);
            Assert.That(root.Q<VisualElement>(className: "pet-gacha-orbit"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "pet-gacha-result-copy"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>(className: "pet-gacha-result-showcase"), Is.Not.Null);

            string[] unsupportedButtonText = root.Query<Button>().ToList()
                .Select(button => button.text?.ToUpperInvariant() ?? string.Empty)
                .Where(text => text.Contains("X10") || text.Contains("HISTORY") ||
                    text.Contains("GUARANTEE") || text.Contains("DETAILS"))
                .ToArray();
            Assert.That(
                unsupportedButtonText,
                Is.Empty,
                "Slice 4 must remain the approved one-pull experience.");
        }

        [Test]
        public void RequestAttack_FiresAttackRequestedWhenReady()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root);
            view.Bind();

            bool attackFired = false;
            view.AttackRequested += () => attackFired = true;

            // When not ready, RequestAttack does not fire
            view.RequestAttack();
            Assert.That(attackFired, Is.False);

            // When snapshot is ready, RequestAttack fires
            var snapshot = new CombatSnapshot(
                new StageId(1),
                "goblin",
                "Goblin",
                20,
                20,
                3,
                3,
                3,
                3,
                CombatPhase.EnemyReady,
                true
            );
            view.Render(snapshot);
            Assert.That(view.CanAttack, Is.True);

            view.RequestAttack();
            Assert.That(attackFired, Is.True);
        }

        [Test]
        public void AcademicProgressionView_ShowRankTransition_PopulatesJuiceElementsAndFiresContinue()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            var progressionView = new AcademicProgressionView(root);
            progressionView.Bind();

            VisualElement modal = root.Q<VisualElement>("academic-rank-modal");
            Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.None));

            var transition = new RankTransition(
                AcademicRank.Silver,
                AcademicRank.Gold
            );
            progressionView.ShowRankTransition(transition);

            Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Label>("academic-rank-modal-header").text, Is.EqualTo("RANK UP"));
            Assert.That(root.Q<Label>("academic-rank-modal-route").text, Does.Contain("Silver").And.Contain("Gold"));
            Assert.That(root.Q<Label>("academic-rank-name-prev").text, Is.EqualTo("SILVER"));
            Assert.That(root.Q<Label>("academic-rank-name-curr").text, Is.EqualTo("GOLD"));

            bool continueFired = false;
            progressionView.ContinueRequested += () => continueFired = true;

            Button button = root.Q<Button>("academic-rank-continue-button");
            Assert.That(button, Is.Not.Null);

            progressionView.RequestContinue();
            Assert.That(continueFired, Is.True, "ContinueRequested should fire on RequestContinue");

            progressionView.HideRankTransition();
            Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.None));
            progressionView.Dispose();
        }

        [Test]
        public void MainMenuAsset_ContainsBiomeTransitionContract()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            Assert.That(asset, Is.Not.Null);

            VisualElement root = asset.CloneTree();
            VisualElement transition = root.Q<VisualElement>("combat-biome-transition");
            Assert.That(transition, Is.Not.Null);
            Assert.That(transition.ClassListContains("combat-biome-transition"), Is.True);
            Assert.That(root.Q<VisualElement>("combat-biome-transition-backdrop"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("combat-biome-transition-card"), Is.Not.Null);
            Assert.That(root.Q<Label>("combat-biome-transition-kicker"), Is.Not.Null);
            Assert.That(root.Q<Label>("combat-biome-transition-title"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("combat-biome-transition-accent"), Is.Not.Null);
        }

        [Test]
        public void PlayBiomeTransition_PopulatesTitleAndCoordinatesSequence()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                MainMenuUxml
            );
            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root, reducedMotion: true);

            string renderedBiome = null;
            string renderedEncounter = null;
            string crossfadedBiome = null;
            float crossfadedDuration = 0f;

            StageMapData stageMap = DevelopmentStageMapFactory.Create();
            view.ConfigureStageMap(
                stageMap,
                biomeId => renderedBiome = biomeId,
                encounterId => renderedEncounter = encounterId,
                (biomeId, duration) =>
                {
                    crossfadedBiome = biomeId;
                    crossfadedDuration = duration;
                    return null;
                }
            );

            var destinationSnapshot = new CombatSnapshot(
                new StageId(31),
                "biome-2-scout",
                "Crystal Scout",
                80,
                80,
                3,
                3,
                3,
                3,
                CombatPhase.EnemyReady,
                true,
                "biome-2",
                "Crystal Caverns",
                StageEncounterKind.NormalMonster,
                string.Empty,
                0
            );

            var routine = view.PlayBiomeTransition(destinationSnapshot);
            while (routine.MoveNext())
            {
                // Advance routine
            }

            Assert.That(root.Q<Label>("combat-biome-transition-title").text, Is.EqualTo("CRYSTAL CAVERNS"));
            Assert.That(root.Q<Label>("combat-biome-transition-kicker").text, Is.EqualTo("ENTERING NEW BIOME"));
            Assert.That(crossfadedBiome, Is.EqualTo("biome-2"));
            Assert.That(crossfadedDuration, Is.GreaterThan(0f));
            Assert.That(renderedEncounter, Is.EqualTo("biome-2-scout"));
        }
    }
}
