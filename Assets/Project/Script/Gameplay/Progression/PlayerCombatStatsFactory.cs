using PowerMath.Gameplay.Combat;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
    public static class PlayerCombatStatsFactory
    {
        public static PlayerCombatStats Create(
            PlayerSnapshot player,
            int baseAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
        {
            return Create(
                player,
                baseAttack,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent);
        }

        public static PlayerCombatStats Create(
            PlayerSnapshot player,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
        {
            return PlayerStatProjectionFactory.Create(
                player,
                baseAttack,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent).ToCombatStats();
        }
    }
}
