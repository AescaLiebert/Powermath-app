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
            double criticalDamagePercent)
        {
            Weapon = weapon;
            PetFlatAttack = petFlatAttack;
            PetMultiplierPercent = petMultiplierPercent;
            HasConfiguredPetStats = hasConfiguredPetStats;
            LegacyBasisPoints = legacyBasisPoints;
            EffectiveAttack = effectiveAttack;
            CriticalRate = criticalRate;
            CriticalDamagePercent = criticalDamagePercent;
        }

        public WeaponAscensionStats Weapon { get; }
        /// <summary>Flat ATK contributed by the equipped pet.</summary>
        public int PetFlatAttack { get; }
        /// <summary>Percentage multiplier bonus from the equipped pet. E.g. 10.0 = +10%.</summary>
        public double PetMultiplierPercent { get; }
        public bool HasConfiguredPetStats { get; }
        public long LegacyBasisPoints { get; }
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
                CriticalDamagePercent);
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
                    item.upgradeLevel))
                .ToArray();
            int petFlatAttack = EquippedPetAttackPolicy.Resolve(
                player?.loadout?.petId,
                petCatalog,
                records,
                out bool hasConfiguredPetStats,
                out double petMultiplierPercent);

            long legacyBasisPoints = checked(
                Math.Max(0, player?.progression?.legacyAtkBonusBasisPoints ?? 0) +
                additionalLegacyBasisPoints);

            // Formula: (WeaponATK + PetFlatATK) × (1 + PetMult%) × (1 + Legacy%)
            double petMultiplier = 1d + petMultiplierPercent / 100d;
            double legacyMultiplier = 1d + legacyBasisPoints / 10000d;
            double effectiveAttack = checked(weapon.Attack + petFlatAttack) * petMultiplier * legacyMultiplier;
            if (effectiveAttack > int.MaxValue)
                throw new OverflowException("Projected player ATK exceeds the supported range.");

            return new PlayerStatProjection(
                weapon,
                petFlatAttack,
                petMultiplierPercent,
                hasConfiguredPetStats,
                legacyBasisPoints,
                Math.Max(1, (int)Math.Round(
                    effectiveAttack,
                    MidpointRounding.AwayFromZero)),
                Math.Min(1d, Math.Max(0d,
                    baseCriticalRate + weapon.CriticalRatePercent / 100d)),
                Math.Max(0d,
                    baseCriticalDamagePercent + weapon.CriticalDamagePercent));
        }
    }
}
