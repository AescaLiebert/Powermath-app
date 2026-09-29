using System;
using NUnit.Framework;
using PowerMath.Gameplay.Progression;
using PowerMath.Gameplay.Pets;
using PowerMath.PlayerData;
using System.Collections.Generic;

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
        public void CanSettle_RebirthBeforeStage31_ReturnsFalse()
        {
            PlayerSnapshot player = CreatePlayer(stage: 30);

            bool canSettle = RunSettlementPolicy.CanSettle(
                player,
                RunSettlementType.Rebirth,
                out string reason);

            Assert.That(canSettle, Is.False);
            Assert.That(reason, Is.EqualTo("Rebirth unlocks at Stage 31."));
        }

        [Test]
        public void CanSettle_RebirthAtStage31InValidPhase_ReturnsTrue()
        {
            PlayerSnapshot player = CreatePlayer(stage: 31, phase: "EnemyReady");

            bool canSettle = RunSettlementPolicy.CanSettle(
                player,
                RunSettlementType.Rebirth,
                out string reason);

            Assert.That(canSettle, Is.True);
            Assert.That(reason, Is.Empty);
        }

        [Test]
        public void CanSettle_DeathAtStage1_RemainsAvailable()
        {
            PlayerSnapshot player = CreatePlayer(stage: 1, phase: "RunDefeat");

            bool canSettle = RunSettlementPolicy.CanSettle(
                player, RunSettlementType.Death, out string reason);

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
            Assert.That(award.LegacyBasisPoints, Is.EqualTo(600)); // 12 * 50
            Assert.That(award.Prestige, Is.EqualTo(1));
            Assert.That(award.PowerCoins, Is.EqualTo(13)); // 12 flat + 1 weighted (weightedTenths=190, combinedMultiplier=10825)
        }

        [Test]
        public void Calculate_AtStage30_ComputesAccurateAward()
        {
            PlayerSnapshot player = CreatePlayer(stage: 30);

            RunSettlementAward award = RunSettlementPolicy.Calculate(player, RunSettlementType.Rebirth);

            Assert.That(award.StageReached, Is.EqualTo(30));
            Assert.That(award.LegacyBasisPoints, Is.EqualTo(1500)); // 30 * 50
            Assert.That(award.Prestige, Is.EqualTo(1));
            Assert.That(award.PowerCoins, Is.EqualTo(41)); // 30 flat + 11 weighted (weightedTenths=190, combinedMultiplier=12175)
        }

        [Test]
        public void Calculate_AtStage200_ComputesSignificantLateGameAward()
        {
            PlayerSnapshot player = CreatePlayer(stage: 200);
            player.activeRun.silverEarned = 0;
            player.activeRun.goldEarned = 0;
            player.activeRun.diamondEarned = 200; // Full Diamond clear

            RunSettlementAward award = RunSettlementPolicy.Calculate(player, RunSettlementType.Rebirth);

            Assert.That(award.StageReached, Is.EqualTo(200));
            Assert.That(award.LegacyBasisPoints, Is.EqualTo(10000)); // 200 * 50
            Assert.That(award.Prestige, Is.EqualTo(1));
            // 200 flat + 21616 weighted (weightedTenths=3000, combinedMultiplier=32425)
            Assert.That(award.PowerCoins, Is.EqualTo(21816));
        }

        [Test]
        public void Calculate_WhenTeleportedToStage180_GrantsFullRewards()
        {
            PlayerSnapshot player = CreatePlayer(stage: 180);
            player.activeRun.wasTeleported = true;

            RunSettlementAward normalAward = RunSettlementPolicy.Calculate(
                CreatePlayer(stage: 180), RunSettlementType.Rebirth);
            RunSettlementAward teleportAward = RunSettlementPolicy.Calculate(
                player, RunSettlementType.Rebirth);

            // Teleport penalty removed for admin tool convenience: 100% of rewards (multiplier = 1.0).
            Assert.That(normalAward.LegacyBasisPoints, Is.EqualTo(9000));
            Assert.That(teleportAward.LegacyBasisPoints, Is.EqualTo(9000));
            Assert.That(teleportAward.WasTeleported, Is.True);
            Assert.That(teleportAward.PowerCoins, Is.EqualTo(normalAward.PowerCoins));
        }

        [Test]
        public void Calculate_ThrowsWhenPlayerDataUnavailable()
        {
            var invalidPlayer = new PlayerSnapshot { progression = null, activeRun = null };

            Assert.Throws<InvalidOperationException>(() =>
                RunSettlementPolicy.Calculate(invalidPlayer, RunSettlementType.Rebirth));
        }

        [Test]
        public void Calculate_Rebirth_AppliesCollectionMultiplierAndFlatPetGrant()
        {
            PlayerSnapshot player = CreatePlayer(stage: 30);
            player.inventory = new[]
            {
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = "reward-pet",
                    owned = true,
                    count = 1
                }
            };
            var passive = new PetPassiveDefinition(
                "reward-pet:rebirth",
                PetPassiveEffectType.GrantPowerCoinsOnRebirth,
                180d);
            var pet = new PetGachaPet(
                "reward-pet",
                "Reward Pet",
                powerCoinBonusPercent: 10d,
                passive: passive);
            var catalog = new PetGachaCatalog(
                "reward-test-v1",
                new[]
                {
                    new PetGachaRarity(
                        "ssr",
                        "SSR",
                        10000,
                        new[] { pet })
                });

            RunSettlementAward award = RunSettlementPolicy.Calculate(
                player,
                RunSettlementType.Rebirth,
                catalog);

            Assert.That(award.PowerCoins, Is.EqualTo(225));
            Assert.That(award.PowerCoinBonusPercent, Is.EqualTo(10d));
        }

        [Test]
        public void Calculate_TwoTierDepthBonus_ScalesAcrossAllStages()
        {
            // Stage 1: 0% bonus (combinedMultiplier = 10000)
            PlayerSnapshot p1 = CreatePlayer(stage: 1);
            p1.activeRun.silverEarned = 100;
            p1.activeRun.goldEarned = 0;
            p1.activeRun.diamondEarned = 0;
            RunSettlementAward a1 = RunSettlementPolicy.Calculate(p1, RunSettlementType.Rebirth);

            // Stage 100: +74.25% bonus (combinedMultiplier = 17425)
            PlayerSnapshot p100 = CreatePlayer(stage: 100);
            p100.activeRun.silverEarned = 100;
            p100.activeRun.goldEarned = 0;
            p100.activeRun.diamondEarned = 0;
            RunSettlementAward a100 = RunSettlementPolicy.Calculate(p100, RunSettlementType.Rebirth);

            // Stage 101: +75.75% bonus (combinedMultiplier = 17575)
            PlayerSnapshot p101 = CreatePlayer(stage: 101);
            p101.activeRun.silverEarned = 100;
            p101.activeRun.goldEarned = 0;
            p101.activeRun.diamondEarned = 0;
            RunSettlementAward a101 = RunSettlementPolicy.Calculate(p101, RunSettlementType.Rebirth);

            Assert.That(a1.PowerCoins, Is.EqualTo(1));
            Assert.That(a101.PowerCoins, Is.GreaterThan(a100.PowerCoins));
        }

        [Test]
        public void Calculate_Rebirth_AtDeepStage_MaintainsConsistentPetPowerCoinBonusPercent()
        {
            PlayerSnapshot player = CreatePlayer(stage: 180);
            player.inventory = new[]
            {
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = "reward-pet",
                    owned = true,
                    count = 1
                }
            };
            var pet = new PetGachaPet(
                "reward-pet",
                "Reward Pet",
                powerCoinBonusPercent: 7d);
            var catalog = new PetGachaCatalog(
                "reward-test-v1",
                new[]
                {
                    new PetGachaRarity(
                        "ssr",
                        "SSR",
                        10000,
                        new[] { pet })
                });

            RunSettlementAward award = RunSettlementPolicy.Calculate(
                player,
                RunSettlementType.Rebirth,
                catalog);

            // PowerCoinBonusPercent must match the player's pet coin bonus (7%), NOT ballooned by depth bonus.
            Assert.That(award.PowerCoinBonusPercent, Is.EqualTo(7d));
        }
    }
}
