using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace PowerMath.Gameplay.Pets.Unity.Tests
{
    public sealed class PetGachaCatalogAssetTests
    {
        private const string CatalogPath =
            "Assets/Project/Resources/Pets/PetGachaCatalog.asset";

        [Test]
        public void PrototypeCatalog_IsLoadableAndMatchesApprovedContentPlan()
        {
            PetGachaCatalogDefinition definition =
                AssetDatabase.LoadAssetAtPath<PetGachaCatalogDefinition>(CatalogPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.TryBuildCatalog(out PetGachaCatalog catalog,
                out string error), Is.True, error);
            Assert.That(catalog.Version, Is.EqualTo("prototype-v2"));
            Assert.That(catalog.Rarities.Select(rarity => rarity.Id), Is.EqualTo(
                new[] { "rare", "super_rare", "ssr" }));
            Assert.That(catalog.Rarities.Select(rarity => rarity.RateBasisPoints),
                Is.EqualTo(new[] { 7000, 2700, 300 }));
            Assert.That(catalog.Rarities[0].Pets.Count, Is.EqualTo(5));
            Assert.That(catalog.Rarities[1].Pets.Count, Is.EqualTo(4));
            Assert.That(catalog.Rarities[2].Pets.Count, Is.EqualTo(5));
            Assert.That(catalog.GetTenPullGuaranteeRarity().Id,
                Is.EqualTo("super_rare"));
            Assert.That(catalog.GetSsrPityRarity().Id, Is.EqualTo("ssr"));
            Assert.That(catalog.Rarities.Sum(rarity => rarity.Pets.Count),
                Is.EqualTo(14));
            Assert.That(
                catalog.Rarities.SelectMany(rarity => rarity.Pets)
                    .Select(pet => pet.Id),
                Is.EqualTo(new[]
                {
                    "butterfly_spirit", "capybara", "jellumi",
                    "little_cozy", "mizu",
                    "cozy", "furbo", "twili", "trippi_troppi",
                    "sapphire", "auregriff", "golden_crane",
                    "lunamoth", "lumirin"
                }));
        }

        [TestCase("butterfly_spirit", "ButterflySpirit", "rare")]
        [TestCase("furbo", "Furbo", "super_rare")]
        [TestCase("sapphire", "Sapphire", "ssr")]
        [TestCase("lumirin", "Lumirin", "ssr")]
        public void PrototypeCatalog_ResolvesStableIdentityAndPlaceholderIcon(
            string petId,
            string expectedName,
            string expectedRarity)
        {
            PetGachaCatalogDefinition definition =
                AssetDatabase.LoadAssetAtPath<PetGachaCatalogDefinition>(CatalogPath);

            Assert.That(definition.TryResolvePet(petId, out var pet, out var rarity),
                Is.True);
            Assert.That(pet.DisplayName, Is.EqualTo(expectedName));
            Assert.That(pet.Icon, Is.Not.Null);
            Assert.That(rarity.rarityId, Is.EqualTo(expectedRarity));
        }
    }
}
