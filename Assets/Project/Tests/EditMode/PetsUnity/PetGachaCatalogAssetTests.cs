using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace PowerMath.Gameplay.Pets.Unity.Tests
{
    public sealed class PetGachaCatalogAssetTests
    {
        private const string CatalogPath =
            "Assets/Project/Resources/PetGachaCatalog.asset";

        [Test]
        public void PrototypeCatalog_IsLoadableAndMatchesApprovedContentPlan()
        {
            PetGachaCatalogDefinition definition =
                AssetDatabase.LoadAssetAtPath<PetGachaCatalogDefinition>(CatalogPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.TryBuildCatalog(out PetGachaCatalog catalog,
                out string error), Is.True, error);
            Assert.That(catalog.Version, Is.EqualTo("prototype-v1"));
            Assert.That(catalog.Rarities.Select(rarity => rarity.Id), Is.EqualTo(
                new[] { "rare", "super_rare", "ssr" }));
            Assert.That(catalog.Rarities.Select(rarity => rarity.RateBasisPoints),
                Is.EqualTo(new[] { 7000, 2700, 300 }));
            Assert.That(catalog.Rarities.All(rarity => rarity.Pets.Count == 5),
                Is.True);
            Assert.That(catalog.Rarities.Sum(rarity => rarity.Pets.Count),
                Is.EqualTo(15));
            Assert.That(
                catalog.Rarities.SelectMany(rarity => rarity.Pets)
                    .Select(pet => pet.Id),
                Is.EqualTo(new[]
                {
                    "ember_fox", "moss_turtle", "cloud_finch",
                    "pebble_golem", "moon_bunny",
                    "prism_owl", "rune_lynx", "tide_serpent",
                    "gear_griffin", "bloom_stag",
                    "infinity_dragon", "chrono_phoenix", "astral_kirin",
                    "crown_sphinx", "wisdom_leviathan"
                }));
        }

        [TestCase("ember_fox", "Ember Fox", "rare")]
        [TestCase("prism_owl", "Prism Owl", "super_rare")]
        [TestCase("infinity_dragon", "Infinity Dragon", "ssr")]
        [TestCase("wisdom_leviathan", "Wisdom Leviathan", "ssr")]
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
