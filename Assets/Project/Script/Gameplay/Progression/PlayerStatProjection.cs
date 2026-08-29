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
            int baseAttack,
            WeaponAscensionStats weapon,
            int petAttack,
            bool hasConfiguredPetStats,
            long legacyBasisPoints,
            int effectiveAttack,
            double criticalRate,
            double criticalDamagePercent)
        {
            BaseAttack = baseAttack;
            Weapon = weapon;
            PetAttack = petAttack;
            HasConfiguredPetStats = hasConfiguredPetStats;
            LegacyBasisPoints = legacyBasisPoints;
            EffectiveAttack = effectiveAttack;
            CriticalRate = criticalRate;
            CriticalDamagePercent = criticalDamagePercent;
        }

        public int BaseAttack { get; }
        public WeaponAscensionStats Weapon { get; }
        public int PetAttack { get; }
        public bool HasConfiguredPetStats { get; }
        public long LegacyBasisPoints { get; }
        public int PermanentAttackSubtotal => checked(BaseAttack + Weapon.Attack + PetAttack);
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
        public static PlayerStatProjection Create(
            PlayerSnapshot player,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            long additionalLegacyBasisPoints = 0)
        {
            return Create(
                player,
                baseAttack,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent,
                null,
                additionalLegacyBasisPoints);
        }

        public static PlayerStatProjection Create(
            PlayerSnapshot player,
            int baseAttack,
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
                baseWeaponAttack);

            int resolvedBaseAttack = Math.Max(1, baseAttack);
            ResolveEquippedPetStats(
                player,
                petCatalog,
                out int petAttack,
                out bool hasConfiguredPetStats);
            long legacyBasisPoints = checked(
                Math.Max(0, player?.progression?.legacyAtkBonusBasisPoints ?? 0) +
                additionalLegacyBasisPoints);
            int subtotal = checked(resolvedBaseAttack + weapon.Attack + petAttack);
            double effectiveAttack = subtotal * (1d + legacyBasisPoints / 10000d);
            if (effectiveAttack > int.MaxValue)
                throw new OverflowException("Projected player ATK exceeds the supported range.");

            return new PlayerStatProjection(
                resolvedBaseAttack,
                weapon,
                petAttack,
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

        private static void ResolveEquippedPetStats(
            PlayerSnapshot player,
            PetGachaCatalog petCatalog,
            out int petAttack,
            out bool hasConfiguredPetStats)
        {
            PetOwnershipRecord[] records =
                (player?.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
                .Where(item => item != null)
                .Select(item => new PetOwnershipRecord(
                    item.itemId,
                    item.owned,
                    item.upgradeLevel))
                .ToArray();
            petAttack = EquippedPetAttackPolicy.Resolve(
                player?.loadout?.petId,
                petCatalog,
                records,
                out hasConfiguredPetStats);
        }
    }
}
