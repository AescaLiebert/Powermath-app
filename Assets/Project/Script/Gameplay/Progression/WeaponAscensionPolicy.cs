using System;

namespace PowerMath.Gameplay.Progression
{
    public readonly struct WeaponAscensionStats
    {
        public WeaponAscensionStats(int level, int attack, int criticalRatePercent, int criticalDamagePercent)
        {
            Level = level;
            Attack = attack;
            CriticalRatePercent = criticalRatePercent;
            CriticalDamagePercent = criticalDamagePercent;
        }

        public int Level { get; }
        public int Attack { get; }
        public int CriticalRatePercent { get; }
        public int CriticalDamagePercent { get; }
    }

    public static class WeaponAscensionPolicy
    {
        public const string CanonicalItemId = "weapon-ascension";
        public const int MaximumLevel = 100;
        public const int DefaultBaseWeaponAttack = 5;

        public static long GetNextCost(int currentLevel)
        {
            ValidateLevel(currentLevel);
            if (currentLevel >= MaximumLevel) return 0;
            return checked((long)Math.Ceiling(8d * Math.Pow(1.06d, currentLevel)));
        }

        public static WeaponAscensionStats GetStats(int level)
        {
            return GetStats(level, DefaultBaseWeaponAttack);
        }

        public static WeaponAscensionStats GetStats(int level, int baseWeaponAttack)
        {
            ValidateLevel(level);
            if (baseWeaponAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(baseWeaponAttack));
            int attack = checked(baseWeaponAttack + (int)Math.Round(
                20d * Math.Pow(level / 20d, 1.2d),
                MidpointRounding.AwayFromZero));
            return new WeaponAscensionStats(
                level,
                attack,
                (level / 10) * 3,
                (level / 15) * 7);
        }

        private static void ValidateLevel(int level)
        {
            if (level < 0 || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
        }
    }
}
