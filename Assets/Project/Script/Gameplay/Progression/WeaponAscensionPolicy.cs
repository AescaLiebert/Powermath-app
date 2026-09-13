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
        public const int DefaultBaseWeaponAttack = 20;

        public static long GetNextCost(int currentLevel)
        {
            ValidateLevel(currentLevel);
            if (currentLevel >= MaximumLevel) return 0;
            return checked((long)Math.Round(10d + 4d * currentLevel + 0.35d * currentLevel * currentLevel, MidpointRounding.AwayFromZero));
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
            double progress = level / 100d;
            int attack = checked(baseWeaponAttack + (int)Math.Round(
                1480d * progress * progress,
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
