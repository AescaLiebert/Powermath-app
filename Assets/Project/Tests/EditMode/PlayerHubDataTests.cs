using System;
using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using UnityEngine;

namespace PowerMath.Tests.EditMode
{
    public sealed class PlayerHubDataTests
    {
        private Texture2D _texture;
        private Sprite _sprite;
        private PetDefinition _definition;
        private PetGachaCatalogDefinition _catalogDefinition;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(2, 2);
            _sprite = Sprite.Create(
                _texture,
                new Rect(0, 0, 2, 2),
                new Vector2(0.5f, 0.5f));
            _definition = ScriptableObject.CreateInstance<PetDefinition>();
            SetPrivate(_definition, "petId", "pet_ember_fox");
            SetPrivate(_definition, "displayName", "Ember Fox");
            SetPrivate(_definition, "icon", _sprite);
            SetPrivate(_definition, "previewSprite", _sprite);
            SetPrivate(_definition, "attackBonus", 7);

            _catalogDefinition = ScriptableObject.CreateInstance<
                PetGachaCatalogDefinition>();
            var rarity = new PetGachaCatalogDefinition.RarityContent
            {
                rarityId = "common",
                displayName = "Common",
                displayColor = Color.green,
                rateBasisPoints = PetGachaCatalog.TotalRateBasisPoints,
                pets = new[] { _definition }
            };
            SetPrivate(_catalogDefinition, "version", "test-v1");
            SetPrivate(_catalogDefinition, "rarities", new[] { rarity });
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_catalogDefinition);
            UnityEngine.Object.DestroyImmediate(_definition);
            UnityEngine.Object.DestroyImmediate(_sprite);
            UnityEngine.Object.DestroyImmediate(_texture);
        }

        [Test]
        public void EquippedOwnedPet_ContributesCatalogAttackBonus()
        {
            Assert.That(_catalogDefinition.TryBuildCatalog(
                out PetGachaCatalog catalog,
                out string error), Is.True, error);
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);

            PlayerStatProjection projection = PlayerStatProjectionFactory.Create(
                player,
                5,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                0.2d,
                50d,
                catalog);

            Assert.That(projection.PetAttack, Is.EqualTo(7));
            Assert.That(projection.HasConfiguredPetStats, Is.True);
            Assert.That(projection.PermanentAttackSubtotal,
                Is.EqualTo(projection.BaseAttack + projection.Weapon.Attack + 7));
        }

        [Test]
        public void EquippedUnownedPet_FailsClosed()
        {
            Assert.That(_catalogDefinition.TryBuildCatalog(
                out PetGachaCatalog catalog,
                out string error), Is.True, error);
            PlayerSnapshot player = CreatePlayer(owned: false, equipped: true);

            Assert.Throws<InvalidOperationException>(() =>
                PlayerStatProjectionFactory.Create(
                    player,
                    5,
                    WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                    0.2d,
                    50d,
                    catalog));
        }

        [Test]
        public void OwnedInventory_MapsRarityAndEquippedState()
        {
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);

            bool success = PlayerOwnedPetInventory.TryCreate(
                player,
                _catalogDefinition,
                out PlayerOwnedPetInventory inventory,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(inventory.Entries, Has.Count.EqualTo(1));
            Assert.That(inventory.Entries[0].Definition, Is.SameAs(_definition));
            Assert.That(inventory.Entries[0].RarityName, Is.EqualTo("Common"));
            Assert.That(inventory.Entries[0].IsEquipped, Is.True);
        }

        [Test]
        public void OwnedInventory_RejectsDuplicateRecognizedPetRecords()
        {
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);
            player.inventory = new[]
            {
                player.inventory[0],
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = "pet_ember_fox",
                    owned = true,
                    upgradeLevel = 0
                }
            };

            Assert.That(PlayerOwnedPetInventory.TryCreate(
                player,
                _catalogDefinition,
                out _,
                out _), Is.False);
        }

        private static PlayerSnapshot CreatePlayer(bool owned, bool equipped)
        {
            return new PlayerSnapshot
            {
                wallet = new PlayerSnapshot.WalletData(),
                progression = new PlayerSnapshot.ProgressionData(),
                loadout = new PlayerSnapshot.LoadoutData
                {
                    petId = equipped ? "pet_ember_fox" : string.Empty
                },
                inventory = new[]
                {
                    new PlayerSnapshot.InventoryItemData
                    {
                        itemId = "pet_ember_fox",
                        owned = owned,
                        upgradeLevel = 0
                    }
                }
            };
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
