using NUnit.Framework;
using System.Linq;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;
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

            Assert.That(root.Q<Label>("combat-stage-label").text, Is.EqualTo("STAGE 6 / 200"));
            Assert.That(root.Q<Label>("combat-enemy-name").text, Is.EqualTo("Rock Titan"));
            Assert.That(root.Q<Label>("combat-enemy-hp-label").text, Is.EqualTo("23 / 40 HP"));
            Assert.That(root.Q<Label>("combat-hearts-label").text, Is.EqualTo("♥ ♥ ♡"));
            VisualElement actions = root.Q<VisualElement>("combat-enemy-actions");
            Assert.That(actions.childCount, Is.EqualTo(3));
            Assert.That(
                actions[0].ClassListContains("hud-enemy-action--spent"),
                Is.True);
            Assert.That(
                actions[2].ClassListContains("hud-enemy-action--attack"),
                Is.True);
            Assert.That(
                actions[2].ClassListContains("hud-enemy-action--danger"),
                Is.True);
            Assert.That(root.Q<Button>("combat-attack-button").enabledSelf, Is.True);
            Assert.That(
                root.Q<Label>("combat-simulation-badge").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
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
    }
}
