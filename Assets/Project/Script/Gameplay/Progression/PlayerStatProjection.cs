using System;
using System.Linq;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Pets;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
    public readonly struct PlayerStatProjection
    {
        public PlayerStatProjection(
            WeaponAscensionStats weapon,
            int petFlatAttack,
            double petMultiplierPercent,
            bool hasConfiguredPetStats,
            long legacyBasisPoints,
            int effectiveAttack,
            double criticalRate,
            double criticalDamagePercent,
            PetCollectionStats petStats = default)
        {
            Weapon = weapon;
            PetFlatAttack = petFlatAttack;
            PetMultiplierPercent = petMultiplierPercent;
            HasConfiguredPetStats = hasConfiguredPetStats;
            LegacyBasisPoints = legacyBasisPoints;
            EffectiveAttack = effectiveAttack;
            CriticalRate = criticalRate;
            CriticalDamagePercent = criticalDamagePercent;
            PetStats = petStats;
        }

        public WeaponAscensionStats Weapon { get; }
        /// <summary>Flat ATK contributed by the pet collection.</summary>
        public int PetFlatAttack { get; }
        /// <summary>Percentage multiplier bonus from the pet collection. E.g. 10.0 = +10%.</summary>
        public double PetMultiplierPercent { get; }
        public bool HasConfiguredPetStats { get; }
        public long LegacyBasisPoints { get; }
        public PetCollectionStats PetStats { get; }
        /// <summary>WeaponATK + PetFlatATK before any multipliers.</summary>
        public int PermanentAttackSubtotal => checked(Weapon.Attack + PetFlatAttack);
        /// <summary>ATK added by the Rebirth/Legacy multiplier (rounded effective minus subtotal).</summary>
        public int LegacyBonusAttack => checked(EffectiveAttack - PermanentAttackSubtotal);
        public int EffectiveAttack { get; }
        public double CriticalRate { get; }
        public double CriticalDamagePercent { get; }

        public PlayerCombatStats ToCombatStats()
        {
            return new PlayerCombatStats(
                EffectiveAttack,
                CriticalRate,
                CriticalDamagePercent,
                effectivePetAttack: PetStats.EffectivePetAttack,
                bonusMaxHearts: PetStats.TotalPlayerHeartBonus,
                petPassives: PetStats.ActivePassives);
        }
    }

    public static class PlayerStatProjectionFactory
    {
        /// <summary>
        /// Creates a stat projection using the default base weapon attack from
        /// <see cref="WeaponAscensionPolicy.DefaultBaseWeaponAttack"/>.
        /// </summary>
        public static PlayerStatProjection Create(
            PlayerSnapshot player,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            long additionalLegacyBasisPoints = 0)
        {
            return Create(
                player,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent,
                null,
                additionalLegacyBasisPoints);
        }

        public static PlayerStatProjection Create(
            PlayerSnapshot player,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            long additionalLegacyBasisPoints = 0)
        {
            return Create(
                player,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent,
                null,
                additionalLegacyBasisPoints);
        }

        public static PlayerStatProjection Create(
            PlayerSnapshot player,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            PetGachaCatalog petCatalog,
            long additionalLegacyBasisPoints = 0)
        {
            if (additionalLegacyBasisPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(additionalLegacyBasisPoints));

            int weaponLevel = (player?.inventory ??
                    Array.Empty<PlayerSnapshot.InventoryItemData>())
                .Where(item => item != null &&
                    item.itemId == WeaponAscensionPolicy.CanonicalItemId)
                .Select(item => item.upgradeLevel)
                .DefaultIfEmpty(0)
                .Single();
            WeaponAscensionStats weapon = WeaponAscensionPolicy.GetStats(
                weaponLevel,
                Math.Max(0, baseWeaponAttack));

            PetOwnershipRecord[] records =
                (player?.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
                .Where(item => item != null)
                .Select(item => new PetOwnershipRecord(
                    item.itemId,
                    item.owned,
                    item.upgradeLevel,
                    item.count > 0 ? item.count : (item.owned ? 1 : 0)))
                .ToArray();

            // Validate cosmetic equip if one is set
            if (!string.IsNullOrWhiteSpace(player?.loadout?.petId))
            {
                PetEquipPolicy.Validate(
                    player.loadout.petId,
                    petCatalog,
                    records);
            }

            PetCollectionStats petStats = PetCollectionPolicy.Calculate(petCatalog, records);
            bool hasConfiguredPetStats = petStats.TotalOwnedPetsCount > 0;
            int petFlatAttack = petStats.TotalPlayerFlatAttack;
            double petMultiplierPercent = petStats.TotalPlayerAttackMultiplierPercent;

            long legacyBasisPoints = checked(
                Math.Max(0, player?.progression?.legacyAtkBonusBasisPoints ?? 0) +
                additionalLegacyBasisPoints);

            // Formula: (WeaponATK + PetFlatATK) × (1 + PetMult%) × (1 + Legacy%)
            double petMultiplier = 1d + petMultiplierPercent / 100d;
            double legacyMultiplier = 1d + legacyBasisPoints / 10000d;
            double effectiveAttack = checked(weapon.Attack + petFlatAttack) * petMultiplier * legacyMultiplier;
            if (effectiveAttack > int.MaxValue)
                throw new OverflowException("Projected player ATK exceeds the supported range.");

            double finalCritRate = Math.Min(1d, Math.Max(0d,
                baseCriticalRate + weapon.CriticalRatePercent / 100d + petStats.TotalCritRatePercent / 100d));
            double finalCritDamage = Math.Max(0d,
                baseCriticalDamagePercent + weapon.CriticalDamagePercent + petStats.TotalCritDamagePercent);

            return new PlayerStatProjection(
                weapon,
                petFlatAttack,
                petMultiplierPercent,
                hasConfiguredPetStats,
                legacyBasisPoints,
                Math.Max(1, (int)Math.Round(
                    effectiveAttack,
                    MidpointRounding.AwayFromZero)),
                finalCritRate,
                finalCritDamage,
                petStats);
        }
    }
}
