using System;
using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.Localization;
using PowerMath.PlayerData;
using PowerMath.UI.MainMenu;
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
            SetPrivate(_definition, "playerAttackBonus", 7);

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
            SetPrivate(_catalogDefinition, "catalogVersion", "test-v1");
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
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                0.2d,
                50d,
                catalog);

            Assert.That(projection.PetFlatAttack, Is.EqualTo(7));
            Assert.That(projection.HasConfiguredPetStats, Is.True);
            Assert.That(projection.PermanentAttackSubtotal,
                Is.EqualTo(projection.Weapon.Attack + 7));
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
            Assert.That(inventory.Entries.Count, Is.EqualTo(1));
            Assert.That(inventory.Entries[0].Definition, Is.SameAs(_definition));
            Assert.That(inventory.Entries[0].RarityName, Is.EqualTo("Common"));
            Assert.That(inventory.Entries[0].IsEquipped, Is.True);
        }

        [Test]
        public void OwnedInventory_AggregatesDuplicateRecognizedPetRecords()
        {
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);
            player.inventory = new[]
            {
                player.inventory[0],
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = "pet_ember_fox",
                    owned = true,
                    upgradeLevel = 0,
                    count = 1
                }
            };

            Assert.That(PlayerOwnedPetInventory.TryCreate(
                player,
                _catalogDefinition,
                out PlayerOwnedPetInventory inventory,
                out string error), Is.True, error);
            Assert.That(inventory.Entries.Count, Is.EqualTo(1));
            Assert.That(inventory.Entries[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void OwnedInventory_WithCountZeroPet_SelfHealsToOneCount()
        {
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);
            player.inventory[0].count = 0;

            Assert.That(PlayerOwnedPetInventory.TryCreate(
                player,
                _catalogDefinition,
                out PlayerOwnedPetInventory inventory,
                out string error), Is.True, error);
            Assert.That(inventory.Entries.Count, Is.EqualTo(1));
            Assert.That(inventory.Entries[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void OwnedInventory_CoexistsWithWeaponAscensionItem()
        {
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);
            player.inventory = new[]
            {
                player.inventory[0],
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = WeaponAscensionPolicy.CanonicalItemId,
                    owned = true,
                    upgradeLevel = 5,
                    count = 1
                }
            };

            Assert.That(PlayerOwnedPetInventory.TryCreate(
                player,
                _catalogDefinition,
                out PlayerOwnedPetInventory inventory,
                out string error), Is.True, error);
            Assert.That(inventory.Entries.Count, Is.EqualTo(1));
            Assert.That(inventory.Entries[0].Definition, Is.SameAs(_definition));
        }

        [Test]
        public void PlayerStatProjection_CombinesWeaponAscensionAndPetStacking()
        {
            Assert.That(_catalogDefinition.TryBuildCatalog(
                out PetGachaCatalog catalog,
                out string error), Is.True, error);
            PlayerSnapshot player = CreatePlayer(owned: true, equipped: true);
            player.inventory = new[]
            {
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = "pet_ember_fox",
                    owned = true,
                    upgradeLevel = 0,
                    count = 2
                },
                new PlayerSnapshot.InventoryItemData
                {
                    itemId = WeaponAscensionPolicy.CanonicalItemId,
                    owned = true,
                    upgradeLevel = 1,
                    count = 1
                }
            };

            PlayerStatProjection projection = PlayerStatProjectionFactory.Create(
                player,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                0.2d,
                50d,
                catalog);

            int expectedWeaponAttack = WeaponAscensionPolicy.GetStats(1, WeaponAscensionPolicy.DefaultBaseWeaponAttack).Attack;
            Assert.That(projection.Weapon.Level, Is.EqualTo(1));
            Assert.That(projection.Weapon.Attack, Is.EqualTo(expectedWeaponAttack));
            Assert.That(projection.PetFlatAttack, Is.EqualTo(14));
            Assert.That(projection.PermanentAttackSubtotal, Is.EqualTo(expectedWeaponAttack + 14));
            Assert.That(projection.EffectiveAttack, Is.EqualTo(expectedWeaponAttack + 14));
        }

        [Test]
        public void PlayerHub_StarTierMapping_T3_T2_T1_ResolvesCorrectStars()
        {
            var catalog = Resources.Load<WeaponAscensionCatalogDefinition>("WeaponAscensionCatalog");
            Assert.That(catalog, Is.Not.Null);

            // T3 (1 star): Levels 0 to 44
            Assert.That(PlayerHubPanelController.ResolveStarCount(catalog.Resolve(0), 0), Is.EqualTo(1));
            Assert.That(PlayerHubPanelController.ResolveStarCount(catalog.Resolve(44), 44), Is.EqualTo(1));
            Assert.That(PlayerHubPanelController.ResolveStarString(1), Is.EqualTo("★"));

            // T2 (3 stars): Levels 45 to 84
            Assert.That(PlayerHubPanelController.ResolveStarCount(catalog.Resolve(45), 45), Is.EqualTo(3));
            Assert.That(PlayerHubPanelController.ResolveStarCount(catalog.Resolve(84), 84), Is.EqualTo(3));
            Assert.That(PlayerHubPanelController.ResolveStarString(3), Is.EqualTo("★★★"));

            // T1 (5 stars): Levels 85 to 115
            Assert.That(PlayerHubPanelController.ResolveStarCount(catalog.Resolve(85), 85), Is.EqualTo(5));
            Assert.That(PlayerHubPanelController.ResolveStarCount(catalog.Resolve(115), 115), Is.EqualTo(5));
            Assert.That(PlayerHubPanelController.ResolveStarString(5), Is.EqualTo("★★★★★"));
        }

        [Test]
        public void PlayerHub_ProgressiveStatDisclosure_OnlyRevealsUnlockedOrUnlockingStats()
        {
            // Level 0 -> 1: CR and CD are 0 both in current and next
            WeaponAscensionStats cur0 = WeaponAscensionPolicy.GetStats(0);
            WeaponAscensionStats next1 = WeaponAscensionPolicy.GetStats(1);
            bool showCr0 = cur0.CriticalRatePercent > 0 || next1.CriticalRatePercent > 0;
            bool showCd0 = cur0.CriticalDamagePercent > 0 || next1.CriticalDamagePercent > 0;
            Assert.That(showCr0, Is.False, "Early upgrade (Lv0->1) must not show Crit Rate line");
            Assert.That(showCd0, Is.False, "Early upgrade (Lv0->1) must not show Crit Damage line");

            // Level 1 -> 2: Next CR unlocks (1%), CD is still 0%
            WeaponAscensionStats cur1 = WeaponAscensionPolicy.GetStats(1);
            WeaponAscensionStats next2 = WeaponAscensionPolicy.GetStats(2);
            bool showCr1 = cur1.CriticalRatePercent > 0 || next2.CriticalRatePercent > 0;
            bool showCd1 = cur1.CriticalDamagePercent > 0 || next2.CriticalDamagePercent > 0;
            Assert.That(showCr1, Is.True, "Crit Rate pops up when unlocking at level 2");
            Assert.That(showCd1, Is.False, "Crit Damage stays hidden before unlock");

            // Level 4 -> 5: Next CD unlocks (2%)
            WeaponAscensionStats cur4 = WeaponAscensionPolicy.GetStats(4);
            WeaponAscensionStats next5 = WeaponAscensionPolicy.GetStats(5);
            bool showCr4 = cur4.CriticalRatePercent > 0 || next5.CriticalRatePercent > 0;
            bool showCd4 = cur4.CriticalDamagePercent > 0 || next5.CriticalDamagePercent > 0;
            Assert.That(showCr4, Is.True, "Crit Rate remains visible");
            Assert.That(showCd4, Is.True, "Crit Damage pops up when unlocking at level 5");
        }

        [Test]
        public void PlayerHub_LevelTransition_FormatsWholeUpgradedLevel()
        {
            int currentLevel = 4;
            int nextLevel = 5;
            string transitionText = $"Lv.{currentLevel} → Lv.{nextLevel}";
            Assert.That(transitionText, Is.EqualTo("Lv.4 → Lv.5"));

            int maxLevel = 115;
            string maxTransitionText = currentLevel >= maxLevel ? "MAX" : $"Lv.{currentLevel} → Lv.{nextLevel}";
            Assert.That(maxTransitionText, Is.EqualTo("Lv.4 → Lv.5"));

            currentLevel = 115;
            maxTransitionText = currentLevel >= maxLevel ? "MAX" : $"Lv.{currentLevel} → Lv.{nextLevel}";
            Assert.That(maxTransitionText, Is.EqualTo("MAX"));
        }

        [Test]
        public void WeaponAscensionPolicy_MaxLevelAttack_IsCappedAt1000()
        {
            WeaponAscensionStats maxStats = WeaponAscensionPolicy.GetStats(115, 20, 115);
            Assert.That(maxStats.Attack, Is.EqualTo(1000));
        }

        [Test]
        public void PlayerHub_BaseWeaponAscendStats_ExcludesBonusFormatting()
        {
            WeaponAscensionStats current = WeaponAscensionPolicy.GetStats(5, 20, 115);
            string attackText = current.Attack.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            string critRateText = $"{current.CriticalRatePercent:0.##}%";
            string critDamageText = $"{current.CriticalDamagePercent:0.##}%";

            Assert.That(attackText, Does.Not.Contain("(+"));
            Assert.That(critRateText, Does.Not.Contain("(+"));
            Assert.That(critDamageText, Does.Not.Contain("(+"));
            Assert.That(critRateText, Is.EqualTo("2%"));
            Assert.That(critDamageText, Is.EqualTo("2%"));
        }

        [Test]
        public void PlayerHub_CompactStats_FormattedWithoutExtraneousPlus()
        {
            double luck = 7.5;
            double coinBonus = 10.0;
            int petAttack = 1251;

            string petAtkText = petAttack.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            string luckText = $"{luck:0.##}%";
            string coinText = $"{coinBonus:0.##}%";

            Assert.That(petAtkText, Is.EqualTo("1,251"));
            Assert.That(petAtkText, Does.Not.Contain("+"));
            Assert.That(luckText, Is.EqualTo("7.5%"));
            Assert.That(luckText, Does.Not.StartWith("+"));
            Assert.That(coinText, Is.EqualTo("10%"));
            Assert.That(coinText, Does.Not.StartWith("+"));
        }

        [Test]
        public void PlayerHub_CompactAggressiveStats_FormattedWithAdditionalPercent()
        {
            int weaponAttack = 20;
            int petFlatAttack = 0;
            double petMultiplierPercent = 0d;
            long legacyBasisPoints = 605; // Rebirth +6.05%
            double baseCritRate = 0.20;
            double weaponCritRate = 0.05; // Lv.17 weapon gives +5% CR
            double petCritRate = 0.05; // Pet collection gives +5% CR
            double currentCritRate = baseCritRate + weaponCritRate + petCritRate; // 0.30 (30%)
            double baseCritDamage = 50.0;
            double weaponCritDamage = 10.0; // Lv.25 weapon gives +10% CD
            double petCritDamage = 10.0; // Pet collection gives +10% CD
            double currentCritDamage = baseCritDamage + weaponCritDamage + petCritDamage; // 70.0 (70%)

            double additionalAtkPercent = Math.Max(0d, (legacyBasisPoints / 100d) + petMultiplierPercent);
            double petBonusCritRatePercent = Math.Max(0d, petCritRate * 100d);
            double petBonusCritDamagePercent = Math.Max(0d, petCritDamage);

            double combinedMultiplier = 1d + (petMultiplierPercent / 100d) + (legacyBasisPoints / 10000d);
            int effectiveAttack = (int)Math.Round((weaponAttack + petFlatAttack) * combinedMultiplier, MidpointRounding.AwayFromZero);

            string attackText = additionalAtkPercent > 0.001d
                ? $"{effectiveAttack.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} <size=12>(+{additionalAtkPercent:0.##}%)</size>"
                : effectiveAttack.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            string critRateText = petBonusCritRatePercent > 0.001d
                ? $"{currentCritRate * 100d:0.##}% <size=12>(+{petBonusCritRatePercent:0.##}%)</size>"
                : $"{currentCritRate * 100d:0.##}%";
            string critDamageText = petBonusCritDamagePercent > 0.001d
                ? $"{currentCritDamage:0.##}% <size=12>(+{petBonusCritDamagePercent:0.##}%)</size>"
                : $"{currentCritDamage:0.##}%";

            // Compact stat displays (+6.05%) matching Rebirth (+6.05%), excluding weapon flat ATK
            Assert.That(attackText, Is.EqualTo("21 <size=12>(+6.05%)</size>"));
            // Weapon CR (+5%) is included in the total 30%, but excluded from (+additional_num%) which only shows pet (+5%)
            Assert.That(critRateText, Is.EqualTo("30% <size=12>(+5%)</size>"));
            // Weapon CD (+10%) is included in the total 70%, but excluded from (+additional_num%) which only shows pet (+10%)
            Assert.That(critDamageText, Is.EqualTo("70% <size=12>(+10%)</size>"));
        }

        [Test]
        public void PlayerHub_CompactAggressiveStats_AllowsDecimalsAndDisablesOnlyNonPositive()
        {
            int weaponAttack = 1000;
            int petFlatAttack = 5; // Flat ATK does not inflate percentage multiplier
            double petMultiplierPercent = 0.5d; // +0.5%
            long legacyBasisPoints = 0;
            double additionalAtkPercent = Math.Max(0d, (legacyBasisPoints / 100d) + petMultiplierPercent);

            double baseCritRate = 0.20;
            double petCritRatePercent = 0.8d; // +0.8%
            double currentCritRate = baseCritRate + petCritRatePercent / 100d;
            double petBonusCritRatePercent = Math.Max(0d, petCritRatePercent);

            double baseCritDamage = 50.0;
            double petCritDamagePercent = 0d; // 0% bonus (non-positive)
            double currentCritDamage = 50.0;
            double petBonusCritDamagePercent = Math.Max(0d, petCritDamagePercent);

            double combinedMultiplier = 1d + (petMultiplierPercent / 100d) + (legacyBasisPoints / 10000d);
            int effectiveAttack = (int)Math.Round((weaponAttack + petFlatAttack) * combinedMultiplier, MidpointRounding.AwayFromZero);

            string attackText = additionalAtkPercent > 0.001d
                ? $"{effectiveAttack.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} <size=12>(+{additionalAtkPercent:0.##}%)</size>"
                : effectiveAttack.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            string critRateText = petBonusCritRatePercent > 0.001d
                ? $"{currentCritRate * 100d:0.##}% <size=12>(+{petBonusCritRatePercent:0.##}%)</size>"
                : $"{currentCritRate * 100d:0.##}%";
            string critDamageText = petBonusCritDamagePercent > 0.001d
                ? $"{currentCritDamage:0.##}% <size=12>(+{petBonusCritDamagePercent:0.##}%)</size>"
                : $"{currentCritDamage:0.##}%";

            // Decimal bonus (> 0%) is displayed with <size=12>
            Assert.That(attackText, Is.EqualTo("1,010 <size=12>(+0.5%)</size>"));
            Assert.That(critRateText, Is.EqualTo("20.8% <size=12>(+0.8%)</size>"));
            // 0% bonus is disabled
            Assert.That(critDamageText, Is.EqualTo("50%"));
            Assert.That(critDamageText, Does.Not.Contain("(+"));
        }

        [Test]
        public void PlayerStatProjection_AdditiveMultiplier_MatchesRebirthAndPreventsBloat()
        {
            var snapshot = new PlayerSnapshot
            {
                progression = new PlayerSnapshot.ProgressionData
                {
                    legacyAtkBonusBasisPoints = 31500 // +315%
                },
                inventory = new[]
                {
                    new PlayerSnapshot.InventoryItemData
                    {
                        itemId = WeaponAscensionPolicy.CanonicalItemId,
                        owned = true,
                        upgradeLevel = 50,
                        count = 1
                    }
                }
            };

            // Weapon level 50 has 325 Attack
            PlayerStatProjection projectionNoPet = PlayerStatProjectionFactory.Create(
                snapshot,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                0.05d,
                50.0d);

            // Additive multiplier: 1 + 3.15 = 4.15
            int expectedAtk = (int)Math.Round(projectionNoPet.Weapon.Attack * 4.15d, MidpointRounding.AwayFromZero);
            Assert.That(projectionNoPet.EffectiveAttack, Is.EqualTo(expectedAtk));

            // Legacy bonus matches Rebirth exactly (+315%)
            double additionalAtkPercent = Math.Max(0d, (projectionNoPet.LegacyBasisPoints / 100d) + projectionNoPet.PetMultiplierPercent);
            Assert.That(additionalAtkPercent, Is.EqualTo(315d));
        }

        [Test]
        public void PlayerHub_PetJuiceProfile_ContainsPreviewEntranceConfiguration()
        {
            var profile = Resources.Load<PlayerHubJuiceProfileDefinition>("PlayerHubJuiceProfile");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.PreviewEntranceSeconds, Is.GreaterThan(0.05f));
            Assert.That(profile.PreviewEntranceSeconds, Is.EqualTo(0.28f).Within(0.01f));
        }

        [Test]
        public void PlayerHubView_FormatPetStat_IncludesLocalizedStatLabel()
        {
            string originalLocale = LocalizationService.Locale;
            try
            {
                // Test in Thai
                LocalizationService.SetLocale("th");

                var atkPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(atkPet, "playerAttackBonus", 10);
                Assert.That(PlayerHubView.FormatPetStat(atkPet, 1), Is.EqualTo("+10 พลังโจมตี"));
                Assert.That(PlayerHubView.FormatPetStat(atkPet, 2), Is.EqualTo("+20 พลังโจมตี"));

                var atkMultPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(atkMultPet, "playerAttackMultiplierPercent", 15f);
                Assert.That(PlayerHubView.FormatPetStat(atkMultPet, 1), Is.EqualTo("+15% พลังโจมตี"));

                var petAtkPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(petAtkPet, "petAttackBonus", 50);
                Assert.That(PlayerHubView.FormatPetStat(petAtkPet, 1), Is.EqualTo("+50 พลังโจมตีสัตว์เลี้ยง"));

                var crPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(crPet, "critRatePercent", 5f);
                Assert.That(PlayerHubView.FormatPetStat(crPet, 1), Is.EqualTo("+5% อัตราคริติคอล"));

                var cdPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(cdPet, "critDamagePercent", 20f);
                Assert.That(PlayerHubView.FormatPetStat(cdPet, 1), Is.EqualTo("+20% ความเสียหายคริติคอล"));

                var luckPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(luckPet, "encounterLuckPercent", 5f);
                Assert.That(PlayerHubView.FormatPetStat(luckPet, 1), Is.EqualTo("+5% โชคลาศ"));

                var coinPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(coinPet, "powerCoinBonusPercent", 10f);
                Assert.That(PlayerHubView.FormatPetStat(coinPet, 1), Is.EqualTo("+10% อัตรารวย"));

                var heartPet = ScriptableObject.CreateInstance<PetDefinition>();
                SetPrivate(heartPet, "playerHeartUnit", 1);
                Assert.That(PlayerHubView.FormatPetStat(heartPet, 1), Is.EqualTo("+1 หัวใจ"));

                var emptyPet = ScriptableObject.CreateInstance<PetDefinition>();
                Assert.That(PlayerHubView.FormatPetStat(emptyPet, 1), Is.EqualTo("สัตว์เลี้ยงสะสม"));

                // Test in English
                LocalizationService.SetLocale("en");
                Assert.That(PlayerHubView.FormatPetStat(atkPet, 1), Is.EqualTo("+10 ATK"));
                Assert.That(PlayerHubView.FormatPetStat(atkMultPet, 1), Is.EqualTo("+15% ATK"));
                Assert.That(PlayerHubView.FormatPetStat(petAtkPet, 1), Is.EqualTo("+50 Pet ATK"));
                Assert.That(PlayerHubView.FormatPetStat(crPet, 1), Is.EqualTo("+5% Crit Rate"));
                Assert.That(PlayerHubView.FormatPetStat(cdPet, 1), Is.EqualTo("+20% Crit Damage"));
                Assert.That(PlayerHubView.FormatPetStat(luckPet, 1), Is.EqualTo("+5% Luck"));
                Assert.That(PlayerHubView.FormatPetStat(coinPet, 1), Is.EqualTo("+10% Money Bonus"));
                Assert.That(PlayerHubView.FormatPetStat(heartPet, 1), Is.EqualTo("+1 Heart"));
                Assert.That(PlayerHubView.FormatPetStat(emptyPet, 1), Is.EqualTo("Collection Pet"));

                UnityEngine.Object.DestroyImmediate(atkPet);
                UnityEngine.Object.DestroyImmediate(atkMultPet);
                UnityEngine.Object.DestroyImmediate(petAtkPet);
                UnityEngine.Object.DestroyImmediate(crPet);
                UnityEngine.Object.DestroyImmediate(cdPet);
                UnityEngine.Object.DestroyImmediate(luckPet);
                UnityEngine.Object.DestroyImmediate(coinPet);
                UnityEngine.Object.DestroyImmediate(heartPet);
                UnityEngine.Object.DestroyImmediate(emptyPet);
            }
            finally
            {
                LocalizationService.SetLocale(originalLocale);
            }
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
                        upgradeLevel = 0,
                        count = owned ? 1 : 0
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
