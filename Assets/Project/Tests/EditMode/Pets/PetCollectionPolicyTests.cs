using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PowerMath.Gameplay.Pets.Tests
{
    public sealed class PetCollectionPolicyTests
    {
        [Test]
        public void Calculate_EmptyCollection_ReturnsDefaultZeroStats()
        {
            PetCollectionStats stats = PetCollectionPolicy.Calculate(Array.Empty<KeyValuePair<PetGachaPet, int>>());

            Assert.That(stats.TotalPlayerFlatAttack, Is.EqualTo(0));
            Assert.That(stats.TotalPlayerAttackMultiplierPercent, Is.EqualTo(0f));
            Assert.That(stats.TotalPetFlatAttack, Is.EqualTo(0));
            Assert.That(stats.TotalPetAttackMultiplierPercent, Is.EqualTo(0f));
            Assert.That(stats.EffectivePetAttack, Is.EqualTo(0));
            Assert.That(stats.TotalCritRatePercent, Is.EqualTo(0f));
            Assert.That(stats.TotalCritDamagePercent, Is.EqualTo(0f));
            Assert.That(stats.TotalEncounterLuckBasisPoints, Is.EqualTo(0));
            Assert.That(stats.TotalPowerCoinBonusPercent, Is.EqualTo(0f));
            Assert.That(stats.TotalPlayerHeartBonus, Is.EqualTo(0));
            Assert.That(stats.ActivePassives.Count, Is.Zero);
        }

        [Test]
        public void Calculate_DuplicateFurbo_MultipliesFlatPlayerAttackByCount()
        {
            // Furbo (SR-02): +7 Player ATK. 2x Furbo = +14 Player ATK
            var furbo = new PetGachaPet("furbo", "Furbo", playerAttackBonus: 7);
            var records = new[] { Pair(furbo, 2) };

            PetCollectionStats stats = PetCollectionPolicy.Calculate(records);

            Assert.That(stats.TotalPlayerFlatAttack, Is.EqualTo(14));
            Assert.That(stats.TotalPlayerAttackMultiplierPercent, Is.EqualTo(0f));
        }

        [Test]
        public void Calculate_MultipleDifferentPets_StacksAdditiveStatsCorrectly()
        {
            // 2x Furbo (+14 Player ATK) + 1x ButterflySpirit (+3 Player ATK) = 17
            var furbo = new PetGachaPet("furbo", "Furbo", playerAttackBonus: 7);
            var butterfly = new PetGachaPet("butterfly_spirit", "ButterflySpirit", playerAttackBonus: 3);
            var records = new[]
            {
                Pair(furbo, 2),
                Pair(butterfly, 1)
            };

            PetCollectionStats stats = PetCollectionPolicy.Calculate(records);

            Assert.That(stats.TotalPlayerFlatAttack, Is.EqualTo(17));
        }

        [Test]
        public void Calculate_SSRAuregriff_StacksMultiplierAndUnlocksPassiveOnce()
        {
            // Auregriff (SSR-02): +30% Player ATK Multiplier, Passive: 3rd hit deals 1.25x
            // 2x Auregriff = +60% Player ATK Multiplier, Passive active once
            var auregriff = new PetGachaPet(
                "auregriff", "Auregriff",
                playerAttackMultiplierPercent: 30f,
                passive: Passive(
                    "auregriff",
                    PetPassiveEffectType.ModifyEveryNthPlayerAttack,
                    0.25d,
                    triggerCount: 3,
                    resetScope: PetPassiveResetScope.Stage));

            var records = new[] { Pair(auregriff, 2) };

            PetCollectionStats stats = PetCollectionPolicy.Calculate(records);

            Assert.That(stats.TotalPlayerAttackMultiplierPercent, Is.EqualTo(60f));
            Assert.That(stats.ActivePassives.HasEffect(
                PetPassiveEffectType.ModifyEveryNthPlayerAttack), Is.True);
        }

        [Test]
        public void Calculate_SSRLumirin_IncreasesMaxHeartsByCountAndEnablesBigBossRegen()
        {
            // Lumirin (SSR-05): +1 Player Heart Unit, Passive: Big Boss defeat regens 1 Heart
            // 2x Lumirin = +2 Max Hearts, Passive active once
            var lumirin = new PetGachaPet(
                "lumirin", "Lumirin",
                playerHeartUnit: 1,
                passive: Passive(
                    "lumirin",
                    PetPassiveEffectType.RestoreHeartsOnEncounterDefeat,
                    1d,
                    encounterFilter: PetEncounterFilter.BigBoss));

            var records = new[] { Pair(lumirin, 2) };

            PetCollectionStats stats = PetCollectionPolicy.Calculate(records);

            Assert.That(stats.TotalPlayerHeartBonus, Is.EqualTo(2));
            Assert.That(stats.ActivePassives.HasEffect(
                PetPassiveEffectType.RestoreHeartsOnEncounterDefeat), Is.True);
        }

        [Test]
        public void Calculate_SSRSapphireAndCozy_CalculatesEffectivePetAttackWithMultiplier()
        {
            // Sapphire (SSR-01): +30 Pet Flat ATK, Passive: Follow-up attack
            // Cozy (SR-01): +5% Pet ATK
            var sapphire = new PetGachaPet(
                "sapphire", "Sapphire",
                petAttackBonus: 30,
                passive: Passive(
                    "sapphire",
                    PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack,
                    1d));
            var cozy = new PetGachaPet(
                "cozy", "Cozy",
                petAttackMultiplierPercent: 5f);

            var records = new[]
            {
                Pair(sapphire, 1),
                Pair(cozy, 1)
            };

            PetCollectionStats stats = PetCollectionPolicy.Calculate(records);

            // 30 * 1.05 = 31.5 -> 32
            Assert.That(stats.TotalPetFlatAttack, Is.EqualTo(30));
            Assert.That(stats.TotalPetAttackMultiplierPercent, Is.EqualTo(5f));
            Assert.That(stats.EffectivePetAttack, Is.EqualTo(32));
            Assert.That(stats.ActivePassives.HasEffect(
                PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack), Is.True);
        }

        [Test]
        public void Calculate_GoldenCraneAndLunamoth_UnlocksPassivesAndBonuses()
        {
            var goldenCrane = new PetGachaPet(
                "golden_crane", "GoldenCrane",
                critRatePercent: 10f,
                passive: Passive(
                    "golden_crane",
                    PetPassiveEffectType.EnablePetFollowUpCritical,
                    1d));
            var lunamoth = new PetGachaPet(
                "lunamoth", "Lunamoth",
                powerCoinBonusPercent: 10f,
                passive: Passive(
                    "lunamoth",
                    PetPassiveEffectType.GrantPowerCoinsOnRebirth,
                    180d,
                    resetScope: PetPassiveResetScope.Run));

            var records = new[]
            {
                Pair(goldenCrane, 1),
                Pair(lunamoth, 1)
            };

            PetCollectionStats stats = PetCollectionPolicy.Calculate(records);

            Assert.That(stats.TotalCritRatePercent, Is.EqualTo(10f));
            Assert.That(stats.ActivePassives.HasEffect(
                PetPassiveEffectType.EnablePetFollowUpCritical), Is.True);
            Assert.That(stats.TotalPowerCoinBonusPercent, Is.EqualTo(10f));
            Assert.That(stats.ActivePassives.HasEffect(
                PetPassiveEffectType.GrantPowerCoinsOnRebirth), Is.True);

        }

        [Test]
        public void Calculate_UniquePassive_DoesNotStackDuplicateCopies()
        {
            var pet = new PetGachaPet(
                "follow-up",
                "Follow Up",
                passive: Passive(
                    "follow-up",
                    PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack,
                    1d));

            PetCollectionStats stats = PetCollectionPolicy.Calculate(new[] { Pair(pet, 4) });

            Assert.That(stats.ActivePassives.SumMagnitude(
                PetPassiveEffectType.FollowUpAfterSuccessfulPlayerAttack), Is.EqualTo(1d));
        }

        [Test]
        public void Calculate_PerCopyPassive_UsesOwnedCopyCount()
        {
            var passive = new PetPassiveDefinition(
                "future:per-copy",
                PetPassiveEffectType.GrantPowerCoinsOnRebirth,
                5d,
                stackRule: PetPassiveStackRule.PerCopy);
            var pet = new PetGachaPet("future", "Future", passive: passive);

            PetCollectionStats stats = PetCollectionPolicy.Calculate(new[] { Pair(pet, 3) });

            Assert.That(stats.ActivePassives.SumMagnitude(
                PetPassiveEffectType.GrantPowerCoinsOnRebirth), Is.EqualTo(15d));
        }

        private static KeyValuePair<PetGachaPet, int> Pair(PetGachaPet pet, int count) =>
            new KeyValuePair<PetGachaPet, int>(pet, count);

        private static PetPassiveDefinition Passive(
            string petId,
            PetPassiveEffectType effectType,
            double magnitude,
            int triggerCount = 1,
            PetPassiveResetScope resetScope = PetPassiveResetScope.Never,
            PetEncounterFilter encounterFilter = PetEncounterFilter.Any) =>
            new PetPassiveDefinition(
                petId + ":" + effectType,
                effectType,
                magnitude,
                triggerCount,
                PetPassiveStackRule.UniquePerDefinition,
                1,
                resetScope,
                encounterFilter);
    }
}
