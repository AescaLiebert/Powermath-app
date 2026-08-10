using NUnit.Framework;
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
            Assert.That(root.Q<Label>("combat-cooldown-label").text, Does.Contain("1"));
            Assert.That(root.Q<Label>("combat-hearts-label").text, Is.EqualTo("♥ ♥ ♡"));
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
    }
}
