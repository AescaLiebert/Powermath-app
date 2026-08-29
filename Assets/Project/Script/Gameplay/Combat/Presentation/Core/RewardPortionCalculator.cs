using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Combat.Presentation
{
    public static class RewardPortionCalculator
    {
        public static long[] CalculatePortions(long totalAmount, int maxIcons = 5)
        {
            if (totalAmount <= 0) return Array.Empty<long>();
            if (maxIcons <= 0) maxIcons = 5;

            if (totalAmount == 1) return new[] { 1L };

            int count = (int)Math.Min(totalAmount, (long)maxIcons);
            if (count < 2) count = 2;

            long[] portions = new long[count];

            // Distribute with pleasant natural variation while guaranteeing sum == totalAmount
            float[] weights = GetWeights(count);
            float totalWeight = 0f;
            for (int i = 0; i < count; i++) totalWeight += weights[i];

            long allocated = 0;
            for (int i = 0; i < count - 1; i++)
            {
                long portion = (long)Math.Floor(totalAmount * (weights[i] / totalWeight));
                if (portion < 1) portion = 1;
                portions[i] = portion;
                allocated += portion;
            }

            // Remainder assigned to the final item
            long lastPortion = totalAmount - allocated;
            if (lastPortion <= 0)
            {
                // Balance so every icon has at least 1
                portions[count - 1] = 1;
                long overflow = (allocated + 1) - totalAmount;
                for (int i = 0; i < count - 1 && overflow > 0; i++)
                {
                    if (portions[i] > 1)
                    {
                        long take = Math.Min(portions[i] - 1, overflow);
                        portions[i] -= take;
                        overflow -= take;
                    }
                }
            }
            else
            {
                portions[count - 1] = lastPortion;
            }

            return portions;
        }

        private static float[] GetWeights(int count)
        {
            switch (count)
            {
                case 2:
                    return new[] { 0.6f, 0.4f };
                case 3:
                    return new[] { 0.35f, 0.25f, 0.40f };
                case 4:
                    return new[] { 0.30f, 0.20f, 0.30f, 0.20f };
                case 5:
                    return new[] { 0.28f, 0.12f, 0.32f, 0.16f, 0.12f };
                case 6:
                default:
                    return new[] { 0.25f, 0.10f, 0.25f, 0.15f, 0.15f, 0.10f };
            }
        }
    }
}
