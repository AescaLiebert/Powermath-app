using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PowerMath.Gameplay.Pets.Tests
{
    public sealed class PetGachaCoreTests
    {
        [Test]
        public void Calculate_NoOwnedPetsDividesEveryRarityEqually()
        {
            PetGachaCatalog catalog = CreateCatalog();

            PetChance[] chances = new PetGachaProbabilityCalculator().Calculate(
                catalog,
                Array.Empty<string>());

            Assert.That(Find(chances, "common-a").GetPercent(), Is.EqualTo(35m));
            Assert.That(Find(chances, "common-b").GetPercent(), Is.EqualTo(35m));
            Assert.That(Find(chances, "rare-a").GetPercent(), Is.EqualTo(0.6m));
            Assert.That(chances.Sum(value => value.GetPercent()), Is.EqualTo(100m));
        }

        [Test]
        public void Calculate_OneOwnedPetHalvesItsBaseShareAndRedistributesWithinRarity()
        {
            PetChance[] chances = new PetGachaProbabilityCalculator().Calculate(
                CreateCatalog(),
                new[] { "rare-a" });

            Assert.That(Find(chances, "rare-a").GetPercent(), Is.EqualTo(0.3m));
            Assert.That(Find(chances, "rare-b").GetPercent(), Is.EqualTo(0.675m));
            Assert.That(
                chances.Where(value => value.RarityId == "rare")
                    .Sum(value => value.GetPercent()),
                Is.EqualTo(3m));
            Assert.That(Find(chances, "common-a").GetPercent(), Is.EqualTo(35m));
        }

        [Test]
        public void Calculate_CompletedRarityRestoresEqualShares()
        {
            string[] allRare = { "rare-a", "rare-b", "rare-c", "rare-d", "rare-e" };

            PetChance[] chances = new PetGachaProbabilityCalculator().Calculate(
                CreateCatalog(),
                allRare);

            foreach (PetChance chance in chances.Where(value => value.RarityId == "rare"))
                Assert.That(chance.GetPercent(), Is.EqualTo(0.6m));
        }

        [Test]
        public void Calculate_EveryRareOwnershipCombinationPreservesCategoryTotal()
        {
            string[] rareIds = { "rare-a", "rare-b", "rare-c", "rare-d", "rare-e" };
            var calculator = new PetGachaProbabilityCalculator();

            for (int mask = 0; mask < 1 << rareIds.Length; mask++)
            {
                string[] owned = rareIds
                    .Where((_, index) => (mask & (1 << index)) != 0)
                    .ToArray();
                PetChance[] chances = calculator.Calculate(CreateCatalog(), owned);
                Assert.That(
                    chances.Where(value => value.RarityId == "rare")
                        .Sum(value => value.GetPercent()),
                    Is.EqualTo(3m),
                    $"ownership mask {mask}");
            }
        }

        [Test]
        public void Roll_UsesCategoryBoundaryThenExactPetWeights()
        {
            PetGachaCatalog catalog = CreateCatalog();
            var random = new SequenceRandomSource(7000, 0);

            PetGachaResult result = new PetGachaRoller().Roll(
                catalog,
                new[] { "rare-a" },
                random);

            Assert.That(result.RarityId, Is.EqualTo("rare"));
            Assert.That(result.PetId, Is.EqualTo("rare-a"));
            Assert.That(result.WasNew, Is.False);
            Assert.That(random.RequestedMaximums, Is.EqualTo(new[] { 10000, 40 }));
        }

        [Test]
        public void TransactionPolicy_SpendsExactlyTwentyFiveForNewAndDuplicateResults()
        {
            PetGachaCatalog catalog = CreateSingleRarityCatalog();
            var command = new PetGachaCommand("pull-1", catalog.Version, 4);

            PetGachaReceipt duplicate = PetGachaTransactionPolicy.CreateReceipt(
                command,
                25,
                catalog,
                new[] { "pet-a" },
                new SequenceRandomSource(0, 0));
            PetGachaReceipt added = PetGachaTransactionPolicy.CreateReceipt(
                new PetGachaCommand("pull-2", catalog.Version, 5),
                40,
                catalog,
                new[] { "pet-a" },
                new SequenceRandomSource(0, 1));

            Assert.That(duplicate.WasNew, Is.False);
            Assert.That(duplicate.ResultingPowerCoins, Is.Zero);
            Assert.That(duplicate.Cost, Is.EqualTo(25));
            Assert.That(added.WasNew, Is.True);
            Assert.That(added.PetId, Is.EqualTo("pet-b"));
            Assert.That(added.ResultingPowerCoins, Is.EqualTo(15));
        }

        [Test]
        public void TransactionPolicy_RejectsInsufficientFundsBeforeRolling()
        {
            PetGachaCatalog catalog = CreateSingleRarityCatalog();

            Assert.Throws<InvalidOperationException>(() =>
                PetGachaTransactionPolicy.CreateReceipt(
                    new PetGachaCommand("pull", catalog.Version, 1),
                    24,
                    catalog,
                    Array.Empty<string>(),
                    new SequenceRandomSource(0, 0)));
        }

        [Test]
        public void TransactionPolicy_MatchingTransactionRecoversSavedReceipt()
        {
            var command = new PetGachaCommand("same", "v1", 3);
            var saved = new PetGachaReceipt("same", "v1", "pet-a", true, 25, 40);

            bool recovered = PetGachaTransactionPolicy.TryRecover(
                command,
                saved,
                out PetGachaReceipt receipt);

            Assert.That(recovered, Is.True);
            Assert.That(receipt.PetId, Is.EqualTo("pet-a"));
            Assert.That(receipt.ResultingPowerCoins, Is.EqualTo(40));
        }

        [Test]
        public void Catalog_RejectsRatesThatDoNotTotalTenThousand()
        {
            Assert.Throws<ArgumentException>(() => new PetGachaCatalog(
                "invalid",
                new[]
                {
                    new PetGachaRarity(
                        "only",
                        "Only",
                        9999,
                        new[] { new PetGachaPet("pet", "Pet") })
                }));
        }

        [Test]
        public void Catalog_RejectsDuplicatePetIdsAcrossRarities()
        {
            Assert.Throws<ArgumentException>(() => new PetGachaCatalog(
                "invalid",
                new[]
                {
                    new PetGachaRarity(
                        "a",
                        "A",
                        5000,
                        new[] { new PetGachaPet("duplicate", "One") }),
                    new PetGachaRarity(
                        "b",
                        "B",
                        5000,
                        new[] { new PetGachaPet("duplicate", "Two") })
                }));
        }

        [Test]
        public void PetAttackBonus_IsImmutableAndRejectsNegativeValues()
        {
            var pet = new PetGachaPet("pet-power", "Power Pet", 17);

            Assert.That(pet.AttackBonus, Is.EqualTo(17));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PetGachaPet("pet-invalid", "Invalid Pet", -1));
        }

        [Test]
        public void EquippedPetAttack_RequiresExactlyOneOwnedBaseRecord()
        {
            var catalog = new PetGachaCatalog(
                "stats-v1",
                new[]
                {
                    new PetGachaRarity(
                        "all",
                        "All",
                        10000,
                        new[] { new PetGachaPet("pet", "Pet", 9) })
                });

            int attack = EquippedPetAttackPolicy.Resolve(
                "pet",
                catalog,
                new[] { new PetOwnershipRecord("pet", true, 0) },
                out bool configured);

            Assert.That(attack, Is.EqualTo(9));
            Assert.That(configured, Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                EquippedPetAttackPolicy.Resolve(
                    "pet",
                    catalog,
                    new[] { new PetOwnershipRecord("pet", false, 0) },
                    out _));
            Assert.Throws<InvalidOperationException>(() =>
                EquippedPetAttackPolicy.Resolve(
                    "pet",
                    catalog,
                    new[]
                    {
                        new PetOwnershipRecord("pet", true, 0),
                        new PetOwnershipRecord("pet", true, 0)
                    },
                    out _));
        }

        private static PetChance Find(IEnumerable<PetChance> chances, string petId) =>
            chances.Single(value => value.PetId == petId);

        private static PetGachaCatalog CreateCatalog()
        {
            return new PetGachaCatalog(
                "test-v1",
                new[]
                {
                    new PetGachaRarity(
                        "common",
                        "Common",
                        7000,
                        new[]
                        {
                            new PetGachaPet("common-a", "Common A"),
                            new PetGachaPet("common-b", "Common B")
                        }),
                    new PetGachaRarity(
                        "middle",
                        "Middle",
                        2700,
                        new[] { new PetGachaPet("middle-a", "Middle A") }),
                    new PetGachaRarity(
                        "rare",
                        "Rare",
                        300,
                        new[]
                        {
                            new PetGachaPet("rare-a", "Rare A"),
                            new PetGachaPet("rare-b", "Rare B"),
                            new PetGachaPet("rare-c", "Rare C"),
                            new PetGachaPet("rare-d", "Rare D"),
                            new PetGachaPet("rare-e", "Rare E")
                        })
                });
        }

        private static PetGachaCatalog CreateSingleRarityCatalog()
        {
            return new PetGachaCatalog(
                "test-single",
                new[]
                {
                    new PetGachaRarity(
                        "all",
                        "All",
                        10000,
                        new[]
                        {
                            new PetGachaPet("pet-a", "Pet A"),
                            new PetGachaPet("pet-b", "Pet B")
                        })
                });
        }

        private sealed class SequenceRandomSource : IPetGachaRandomSource
        {
            private readonly Queue<int> _values;

            public SequenceRandomSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public List<int> RequestedMaximums { get; } = new List<int>();

            public int NextExclusive(int maximumExclusive)
            {
                RequestedMaximums.Add(maximumExclusive);
                int value = _values.Dequeue();
                Assert.That(value, Is.GreaterThanOrEqualTo(0));
                Assert.That(value, Is.LessThan(maximumExclusive));
                return value;
            }
        }
    }
}
