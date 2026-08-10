using System;

namespace PowerMath.Gameplay.Combat
{
    public sealed class StageProgressionCalculator
    {
        private const double GrowthPerWorldLevel = 0.12d;
        private const double MinimumRandomFactor = 0.95d;
        private const double RandomFactorSpan = 0.10d;

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

            int growthSteps = stage.WorldLevel - 1;
            double scaledHp = baseHp * (1d + GrowthPerWorldLevel * growthSteps);
            double randomFactor = MinimumRandomFactor + RandomFactorSpan * randomUnit;
            int rounded = (int)Math.Round(
                scaledHp * randomFactor,
                MidpointRounding.AwayFromZero
            );

            return Math.Max(1, rounded);
        }
    }
}
