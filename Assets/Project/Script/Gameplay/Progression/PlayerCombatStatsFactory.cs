using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Pets;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
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
