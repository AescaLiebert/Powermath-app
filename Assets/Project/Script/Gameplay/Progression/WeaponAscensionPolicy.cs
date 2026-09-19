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
        public const int DefaultLevelsPerTier = 5;
        public const int DefaultTierCount = 23;
        public const int DefaultMaximumLevel = DefaultTierCount * DefaultLevelsPerTier; // 115
        public const int MaximumLevel = DefaultMaximumLevel;
        public const int DefaultBaseWeaponAttack = 20;

        public static long GetNextCost(int currentLevel)
        {
            return GetNextCost(currentLevel, DefaultMaximumLevel);
        }

        public static long GetNextCost(int currentLevel, int maxLevel)
        {
            ValidateLevel(currentLevel, maxLevel);
            if (currentLevel >= maxLevel) return 0;
            return checked((long)Math.Round(10d + 4d * currentLevel + 0.35d * currentLevel * currentLevel, MidpointRounding.AwayFromZero));
        }

        public static WeaponAscensionStats GetStats(int level)
        {
            return GetStats(level, DefaultBaseWeaponAttack, DefaultMaximumLevel);
        }

        public static WeaponAscensionStats GetStats(int level, int baseWeaponAttack)
        {
            return GetStats(level, baseWeaponAttack, DefaultMaximumLevel);
        }

        public static WeaponAscensionStats GetStats(int level, int baseWeaponAttack, int maxLevel)
        {
            ValidateLevel(level, maxLevel);
            if (baseWeaponAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(baseWeaponAttack));
            double progress = maxLevel > 0 ? (double)level / maxLevel : 0d;
            int attack = checked(baseWeaponAttack + (int)Math.Round(
                1480d * (progress > 0d ? Math.Pow(progress, 1.4d) : 0d),
                MidpointRounding.AwayFromZero));
            int criticalRatePercent = (int)Math.Round((level / (double)DefaultLevelsPerTier) * 1.5d, MidpointRounding.AwayFromZero);
            int criticalDamagePercent = (level / DefaultLevelsPerTier) * 2;
            return new WeaponAscensionStats(
                level,
                attack,
                criticalRatePercent,
                criticalDamagePercent);
        }

        public static WeaponAscensionStats GetStats(int level, int baseWeaponAttack, WeaponAscensionCatalogDefinition catalog)
        {
            int maxLevel = catalog != null && catalog.MaximumLevel > 0
                ? catalog.MaximumLevel
                : DefaultMaximumLevel;
            return GetStats(level, baseWeaponAttack, maxLevel);
        }

        private static void ValidateLevel(int level)
        {
            ValidateLevel(level, DefaultMaximumLevel);
        }

        private static void ValidateLevel(int level, int maxLevel)
        {
            if (level < 0 || (maxLevel > 0 && level > maxLevel))
                throw new ArgumentOutOfRangeException(nameof(level));
        }
    }
}
