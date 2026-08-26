using NUnit.Framework;
using PowerMath.UI.MainMenu;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class MainMenuPanelHostTests
    {
        [Test]
        public void TryOpenAndClose_ControlsVisibilityAndIdentity()
        {
            var host = new MainMenuPanelHost();
            var panel = new VisualElement();
            panel.AddToClassList("is-hidden");
            var opener = new Button();

            Assert.That(
                host.TryOpen(MainMenuPanelId.WorldMap, panel, opener),
                Is.True
            );
            Assert.That(host.OpenPanel, Is.EqualTo(MainMenuPanelId.WorldMap));
            Assert.That(panel.ClassListContains("is-hidden"), Is.False);
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            Assert.That(
                host.TryClose(MainMenuPanelId.WorldMap, opener),
                Is.True
            );
            Assert.That(host.OpenPanel, Is.EqualTo(MainMenuPanelId.None));
            Assert.That(panel.ClassListContains("is-hidden"), Is.True);
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void TryOpen_RejectsCompetingPanelUntilCurrentCloses()
        {
            var host = new MainMenuPanelHost();
            var navigator = new VisualElement();
            var playerHub = new VisualElement();
            playerHub.AddToClassList("is-hidden");

            Assert.That(
                host.TryOpen(MainMenuPanelId.WorldMap, navigator, null),
                Is.True
            );
            Assert.That(
                host.TryOpen(MainMenuPanelId.PlayerHub, playerHub, null),
                Is.False
            );
            Assert.That(host.OpenPanel, Is.EqualTo(MainMenuPanelId.WorldMap));
            Assert.That(playerHub.ClassListContains("is-hidden"), Is.True);
        }

        [Test]
        public void ForceCloseAll_AllowsTerminalSettlementToTakePriority()
        {
            var host = new MainMenuPanelHost();
            var playerHub = new VisualElement();
            var settlement = new VisualElement();
            var playerHubButton = new Button();
            var rebirthButton = new Button();

            Assert.That(
                host.TryOpen(
                    MainMenuPanelId.PlayerHub,
                    playerHub,
                    playerHubButton),
                Is.True
            );

            host.ForceCloseAll();

            Assert.That(playerHub.ClassListContains("is-hidden"), Is.True);
            Assert.That(host.OpenPanel, Is.EqualTo(MainMenuPanelId.None));
            Assert.That(
                host.TryOpen(
                    MainMenuPanelId.Rebirth,
                    settlement,
                    rebirthButton),
                Is.True
            );
            Assert.That(host.OpenPanel, Is.EqualTo(MainMenuPanelId.Rebirth));
            Assert.That(settlement.ClassListContains("is-hidden"), Is.False);
        }
    }
}
