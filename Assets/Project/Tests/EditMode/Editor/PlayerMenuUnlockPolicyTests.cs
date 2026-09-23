using NUnit.Framework;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;

namespace PowerMath.Tests.EditMode
{
    public sealed class PlayerMenuUnlockPolicyTests
    {
        [Test]
        public void LocksUntilPlayerHasReachedStageThirtyOne()
        {
            PlayerSnapshot player = Player(highestStage: 30, currentStage: 30);

            Assert.That(PlayerMenuUnlockPolicy.IsHubAndGachaUnlocked(player), Is.False);

            player.progression.highestStage = 31;
            Assert.That(PlayerMenuUnlockPolicy.IsHubAndGachaUnlocked(player), Is.True);
        }

        [Test]
        public void KeepsLegacyPlayerUnlockedAfterRebirth()
        {
            PlayerSnapshot player = Player(highestStage: 31, currentStage: 1);
            player.onboarding.legacyPlayer = true;
            player.activeRun.currentStage = 1;

            Assert.That(PlayerMenuUnlockPolicy.IsHubAndGachaUnlocked(player), Is.True);
        }

        [TestCase("Death")]
        [TestCase("Rebirth")]
        public void SettlementUnlocksHubAndGachaBeforeStageThirtyOne(string type)
        {
            PlayerSnapshot player = Player(highestStage: 8, currentStage: 1);
            player.lastRunSettlement = new PlayerSnapshot.RunSettlementData
            {
                runId = "settled-run",
                type = type
            };

            Assert.That(PlayerMenuUnlockPolicy.IsHubAndGachaUnlocked(player), Is.True);
        }

        private static PlayerSnapshot Player(int highestStage, int currentStage) =>
            new PlayerSnapshot
            {
                progression = new PlayerSnapshot.ProgressionData
                {
                    highestStage = highestStage,
                    currentStage = currentStage
                },
                activeRun = new PlayerSnapshot.ActiveRunData
                {
                    currentStage = currentStage
                },
                onboarding = new PlayerSnapshot.OnboardingData()
            };
    }
}
