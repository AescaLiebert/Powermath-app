using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.Gameplay.Combat.Presentation;
using PowerMath.UI.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class CombatLobbyViewTests
    {
        private const string MainMenuUxml =
            "Assets/Project/UI/MainMenuUI.uxml";

        [Test]
        public void BiomeDefinition_UsesSecondBackgroundFromExactMidpoint()
        {
            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            var texture = new Texture2D(4, 4);
            var primary = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
            var secondary = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);
            try
            {
                SetPrivateField(biome, "firstStage", 1);
                SetPrivateField(biome, "lastStage", 30);
                SetPrivateField(biome, "backgroundSprite", primary);
                SetPrivateField(biome, "secondaryBackgroundSprite", secondary);

                Assert.That(biome.MidpointStage, Is.EqualTo(16));
                Assert.That(biome.ResolveBackground(15), Is.SameAs(primary));
                Assert.That(biome.ResolveBackground(16), Is.SameAs(secondary));
                Assert.That(biome.ResolveBackground(30), Is.SameAs(secondary));
            }
            finally
            {
                Object.DestroyImmediate(biome);
                Object.DestroyImmediate(primary);
                Object.DestroyImmediate(secondary);
                Object.DestroyImmediate(texture);
            }
        }

        private static void SetPrivateField<T>(BiomeDefinition target, string name, T value)
        {
            FieldInfo field = typeof(BiomeDefinition).GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected serialized field {name}.");
            field.SetValue(target, value);
        }

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
            Assert.That(root.Q<Label>("combat-hearts-label").text, Is.EqualTo("H H -"));
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
                "OK",
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

            view.ShowAnswerFeedback("OK", "CORRECT", "OK", true);
            Image correctSticker = root.Q<Image>(
                "combat-result-sticker-correct");
            Image failSticker = root.Q<Image>(
                "combat-result-sticker-fail");
            Assert.That(correctSticker, Is.Not.Null);
            Assert.That(failSticker, Is.Not.Null);
            Assert.That(correctSticker.sprite, Is.Not.Null);
            Assert.That(failSticker.sprite, Is.Not.Null);
            Assert.That(
                root.Q<VisualElement>("combat-feedback-card")
                    .ClassListContains("combat-feedback-card--negative"),
                Is.False);

            view.ShowAnswerFeedback("NO", "TRY AGAIN", "OK", false);
            Assert.That(
                root.Q<VisualElement>("combat-feedback-card")
                    .ClassListContains("combat-feedback-card--negative"),
                Is.True);

            view.HideAnswerFeedback();
            Assert.That(
                root.Q<VisualElement>("combat-feedback-card").style.display.value,
                Is.EqualTo(DisplayStyle.None));
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
        public void PlayBiomeTransition_ChangesBackgroundWithoutRevealingDestinationEnemy()
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
                snapshot => renderedBiome = snapshot.BiomeId,
                encounterId => renderedEncounter = encounterId,
                (snapshot, duration) =>
                {
                    crossfadedBiome = snapshot.BiomeId;
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
            // A detached tree has no scheduled lifecycle updates. Advance only
            // to the title hold, after the background callback has completed.
            Assert.That(routine.MoveNext(), Is.True);
            Assert.That(routine.Current, Is.TypeOf<WaitForSecondsRealtime>());

            Assert.That(root.Q<Label>("combat-biome-transition-title").text, Is.EqualTo("CRYSTAL CAVERNS"));
            Assert.That(root.Q<Label>("combat-biome-transition-kicker").text, Is.Empty);
            Assert.That(crossfadedBiome, Is.EqualTo("biome-2"));
            Assert.That(crossfadedDuration, Is.GreaterThan(0f));
            Assert.That(renderedBiome, Is.Null);
            Assert.That(renderedEncounter, Is.Null,
                "The encounter belongs to the subsequent entrance, after the title exits.");
            (routine as System.IDisposable)?.Dispose();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EncounterEntrance_WaitsForBiomeTransition_RecoverySkipsIt(bool biomeChanged)
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxml);
            using var view = new CombatLobbyView(asset.CloneTree(), reducedMotion: true);
            var actorObject = new GameObject("Destination enemy", typeof(RectTransform),
                typeof(UnityEngine.UI.Image));
            var calls = new List<string>();
            try
            {
                var actor = actorObject.AddComponent<ActorPresentationController>();
                actor.Initialize(PresentationActor.Enemy, true);
                actor.CancelAndApply(ActorVisualState.Hidden);
                view.ConfigureStageMap(DevelopmentStageMapFactory.Create(),
                    _ => calls.Add("bind background"),
                    _ => calls.Add("bind encounter"),
                    (_, __) =>
                    {
                        calls.Add("transition background");
                        return null;
                    });
                var feedback = new CombatFeedbackPlayer(view, null, true, enemyActor: actor);
                var destination = new CombatSnapshot(new StageId(31), "biome-2-scout",
                    "Crystal Scout", 80, 80, 3, 3, 3, 3, CombatPhase.PresentingResult,
                    true, "biome-2", "Crystal Caverns", StageEncounterKind.NormalMonster,
                    string.Empty, 0);
                IEnumerator entrance = feedback.PlayEncounterEntrance(destination, biomeChanged);

                Assert.That(entrance.MoveNext(), Is.True);
                if (biomeChanged)
                {
                    Assert.That(calls, Is.Empty,
                        "Destination binding must wait for the yielded transition.");
                    Assert.That(actor.State, Is.EqualTo(ActorVisualState.Hidden));
                    var transition = (IEnumerator)entrance.Current;
                    Assert.That(transition.MoveNext(), Is.True);
                    Assert.That(calls, Is.EqualTo(new[] { "transition background" }));
                    Assert.That(actor.State, Is.EqualTo(ActorVisualState.Hidden));
                    (transition as System.IDisposable)?.Dispose();

                    // Resume the caller as Unity does only after its yielded child
                    // completes. Lifecycle timing is covered in scene tests.
                    Assert.That(entrance.MoveNext(), Is.True);
                }

                Assert.That(calls, Is.EqualTo(biomeChanged
                    ? new[] { "transition background", "bind background", "bind encounter" }
                    : new[] { "bind background", "bind encounter" }));
                Assert.That(actor.State, Is.EqualTo(ActorVisualState.Hidden));
                Assert.That(view.IsEnemyActionQueueStable, Is.False,
                    "The new queue must initiate before interaction can reopen.");
                var appear = (IEnumerator)entrance.Current;
                Assert.That(appear.MoveNext(), Is.True);
                Assert.That(actor.State, Is.EqualTo(ActorVisualState.Appearing));
                (appear as System.IDisposable)?.Dispose();
                (entrance as System.IDisposable)?.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
            }
        }

        [Test]
        public void CombatLobbyView_ResolvesLocalizedEnemyNameAndRefreshesOnLocaleChange()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainMenuUxml);
            VisualElement root = asset.CloneTree();
            using var view = new CombatLobbyView(root, reducedMotion: true);

            string currentLocale = "en";
            string resolvedEncounter = null;
            view.ConfigureStageMap(
                DevelopmentStageMapFactory.Create(),
                _ => { },
                id => resolvedEncounter = id,
                null,
                encounterId =>
                {
                    if (encounterId == "B1-N01")
                    {
                        return currentLocale == "th" ? "สไลม์น้ำแข็ง" : "Frost Slime";
                    }
                    return null;
                }
            );

            var snapshot = new CombatSnapshot(
                new StageId(1),
                "B1-N01",
                "Frost Slime",
                30,
                30,
                3,
                3,
                3,
                3,
                CombatPhase.EnemyReady,
                true,
                "biome-1",
                "Verdant Grove",
                StageEncounterKind.NormalMonster,
                string.Empty,
                0
            );

            view.Render(snapshot);
            Assert.That(root.Q<Label>("combat-enemy-name").text, Is.EqualTo("Frost Slime"));

            // Switch to Thai locale and refresh
            currentLocale = "th";
            view.RefreshLocalizedEnemyName();
            Assert.That(root.Q<Label>("combat-enemy-name").text, Is.EqualTo("สไลม์น้ำแข็ง"));
            Assert.That(root.Q<Label>("combat-enemy-name-shadow").text, Is.EqualTo("สไลม์น้ำแข็ง"));
        }
    }
}
