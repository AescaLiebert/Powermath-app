using System;
using NUnit.Framework;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;

namespace PowerMath.Tests.EditMode
{
    public sealed class RunSettlementPolicyTests
    {
        private static PlayerSnapshot CreatePlayer(int stage, string phase = "EnemyReady", string committedAttempt = "")
        {
            return new PlayerSnapshot
            {
                progression = new PlayerSnapshot.ProgressionData
                {
                    currentStage = stage,
                    highestStage = stage,
                    legacyAtkBonusBasisPoints = 0,
                    prestige = 0
                },
                activeRun = new PlayerSnapshot.ActiveRunData
                {
                    runId = "test-run-123",
                    currentStage = stage,
                    phase = phase,
                    committedAttemptId = committedAttempt,
                    silverEarned = 10,
                    goldEarned = 5,
                    diamondEarned = 2,
                    bonusMultiplierBasisPoints = 10000
                },
                wallet = new PlayerSnapshot.WalletData
                {
                    powerCoins = 100
                }
            };
        }

        [Test]
        public void CanSettle_RebirthUnderStage30_ReturnsFalseWithReason()
        {
            PlayerSnapshot player = CreatePlayer(stage: 15);

            bool canSettle = RunSettlementPolicy.CanSettle(
                player,
                RunSettlementType.Rebirth,
                out string reason);

            Assert.That(canSettle, Is.False);
            Assert.That(reason, Is.EqualTo("Rebirth unlocks at Stage 30."));
        }

        [Test]
        public void CanSettle_RebirthAtStage30InValidPhase_ReturnsTrue()
        {
            PlayerSnapshot player = CreatePlayer(stage: 30, phase: "EnemyReady");

            bool canSettle = RunSettlementPolicy.CanSettle(
                player,
                RunSettlementType.Rebirth,
                out string reason);

            Assert.That(canSettle, Is.True);
            Assert.That(reason, Is.Empty);
        }

        [Test]
        public void CanSettle_RebirthDuringUnresolvedQuestion_ReturnsFalse()
        {
            PlayerSnapshot player = CreatePlayer(stage: 30, phase: "EnemyReady", committedAttempt: "attempt-456");

            bool canSettle = RunSettlementPolicy.CanSettle(
                player,
                RunSettlementType.Rebirth,
                out string reason);

            Assert.That(canSettle, Is.False);
            Assert.That(reason, Is.EqualTo("Finish the current question first."));
        }

        [Test]
        public void Calculate_UnderStage50_ComputesPreviewAwardWithoutThrowing()
        {
            PlayerSnapshot player = CreatePlayer(stage: 12);

            RunSettlementAward award = RunSettlementPolicy.Calculate(player, RunSettlementType.Rebirth);

            Assert.That(award.StageReached, Is.EqualTo(12));
            Assert.That(award.LegacyBasisPoints, Is.EqualTo(300)); // 12 * 25
            Assert.That(award.Prestige, Is.EqualTo(1));
            Assert.That(award.PowerCoins, Is.EqualTo(13)); // 12 flat + 1 weighted
        }

        [Test]
        public void Calculate_AtStage30_ComputesAccurateAward()
        {
            PlayerSnapshot player = CreatePlayer(stage: 30);

            RunSettlementAward award = RunSettlementPolicy.Calculate(player, RunSettlementType.Rebirth);

            Assert.That(award.StageReached, Is.EqualTo(30));
            Assert.That(award.LegacyBasisPoints, Is.EqualTo(750)); // 30 * 25
            Assert.That(award.Prestige, Is.EqualTo(1));
            Assert.That(award.PowerCoins, Is.EqualTo(33)); // 30 flat + 3 weighted
        }

        [Test]
        public void Calculate_ThrowsWhenPlayerDataUnavailable()
        {
            var invalidPlayer = new PlayerSnapshot { progression = null, activeRun = null };

            Assert.Throws<InvalidOperationException>(() =>
                RunSettlementPolicy.Calculate(invalidPlayer, RunSettlementType.Rebirth));
        }
    }
}
