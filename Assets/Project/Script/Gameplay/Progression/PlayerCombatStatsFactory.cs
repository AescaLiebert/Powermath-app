using System;
using System.Collections.Generic;
using System.Linq;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Pets;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
    public static class PetCollectionEventMultiplierPolicy
    {
        public const int BaseMultiplierBasisPoints = 10000;

        public static int Calculate(PlayerSnapshot player, PetGachaCatalog catalog)
        {
            if (catalog == null || player?.inventory == null) return BaseMultiplierBasisPoints;

            PetOwnershipRecord[] records = player.inventory
                .Where(item => item != null)
                .Select(item => new PetOwnershipRecord(
                    item.itemId,
                    item.owned,
                    item.upgradeLevel,
                    item.count > 0 ? item.count : (item.owned ? 1 : 0)))
                .ToArray();

            PetCollectionStats stats = PetCollectionPolicy.Calculate(catalog, records);
            long multiplier = checked(BaseMultiplierBasisPoints + stats.TotalEncounterLuckBasisPoints);
            return multiplier > int.MaxValue ? int.MaxValue : (int)multiplier;
        }
    }

    public static class PlayerCombatStatsFactory
    {
        public static PlayerCombatStats Create(
            PlayerSnapshot player,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
        {
            return Create(
                player,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent);
        }

        public static PlayerCombatStats Create(
            PlayerSnapshot player,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
        {
            return PlayerStatProjectionFactory.Create(
                player,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent).ToCombatStats();
        }

        public static PlayerCombatStats Create(
            PlayerSnapshot player,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            PetGachaCatalog petCatalog)
        {
            return PlayerStatProjectionFactory.Create(
                player,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent,
                petCatalog).ToCombatStats();
        }
    }
}
