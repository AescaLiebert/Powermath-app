using System;

namespace PowerMath.Gameplay.Combat
{
    public sealed class StageProgressionCalculator
    {
        private const double GrowthPerWorldLevel = 0.12d;
        private const double MinimumRandomFactor = 0.95d;
        private const double RandomFactorSpan = 0.10d;

        public static double GetPhaseGrowthMultiplier(int worldLevel)
        {
            if (worldLevel <= 12)
            {
                return 1d + GrowthPerWorldLevel * (worldLevel - 1);
            }
            if (worldLevel <= 28)
            {
                return 2.32d + 0.25d * (worldLevel - 12);
            }
            return 6.32d + 1.975d * (worldLevel - 28);
        }

        public int CalculateSpawnHp(
            StageId stage,
            int baseHp,
            double randomUnit)
        {
            if (baseHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseHp));
            }

            if (randomUnit < 0d || randomUnit > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(randomUnit));
            }

            double growthMultiplier = GetPhaseGrowthMultiplier(stage.WorldLevel);
            double scaledHp = baseHp * growthMultiplier;
            double randomFactor = MinimumRandomFactor + RandomFactorSpan * randomUnit;
            int rounded = (int)Math.Round(
                scaledHp * randomFactor,
                MidpointRounding.AwayFromZero
            );

            return Math.Max(1, rounded);
        }
    }
}
