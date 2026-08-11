using System;

namespace PowerMath.Gameplay.Combat
{
    public static class ResponseDamagePolicy
    {
        public const int MinimumScore = 1;
        public const int MaximumScore = 10;
        public const int PercentPerPoint = 20;

        public static double GetMultiplier(int responseScore)
        {
            Validate(responseScore);
            return responseScore * PercentPerPoint / 100d;
        }

        public static int GetPercent(int responseScore)
        {
            Validate(responseScore);
            return checked(responseScore * PercentPerPoint);
        }

        private static void Validate(int responseScore)
        {
            if (responseScore < MinimumScore || responseScore > MaximumScore)
                throw new ArgumentOutOfRangeException(nameof(responseScore));
        }
    }

    public readonly struct DamageInput
    {
        public DamageInput(
            int effectiveAttack,
            double rankMultiplier,
            double buffMultiplier,
            double criticalDamagePercent,
            bool isCritical,
            int responseScore)
        {
            if (effectiveAttack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveAttack));
            }

            if (rankMultiplier < 0d || buffMultiplier < 0d ||
                criticalDamagePercent < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rankMultiplier),
                    "Damage multipliers cannot be negative."
                );
            }

            EffectiveAttack = effectiveAttack;
            RankMultiplier = rankMultiplier;
            BuffMultiplier = buffMultiplier;
            CriticalDamagePercent = criticalDamagePercent;
            IsCritical = isCritical;
            ResponseScore = responseScore;
            ResponseDamageMultiplier =
                ResponseDamagePolicy.GetMultiplier(responseScore);
        }

        public int EffectiveAttack { get; }

        public double RankMultiplier { get; }

        public double BuffMultiplier { get; }

        public double CriticalDamagePercent { get; }

        public bool IsCritical { get; }

        public int ResponseScore { get; }

        public double ResponseDamageMultiplier { get; }
    }

    public readonly struct DamageResult
    {
        public DamageResult(
            int finalDamage,
            double unroundedDamage,
            bool isCritical,
            double responseDamageMultiplier)
        {
            FinalDamage = finalDamage;
            UnroundedDamage = unroundedDamage;
            IsCritical = isCritical;
            ResponseDamageMultiplier = responseDamageMultiplier;
        }

        public int FinalDamage { get; }

        public double UnroundedDamage { get; }

        public bool IsCritical { get; }

        public double ResponseDamageMultiplier { get; }
    }

    public sealed class DamageCalculator
    {
        public DamageResult Calculate(DamageInput input)
        {
            double criticalMultiplier = input.IsCritical
                ? 1d + input.CriticalDamagePercent / 100d
                : 1d;

            double unrounded = input.EffectiveAttack *
                input.RankMultiplier *
                input.BuffMultiplier *
                criticalMultiplier *
                input.ResponseDamageMultiplier;

            int rounded = (int)Math.Round(
                unrounded,
                MidpointRounding.AwayFromZero
            );

            return new DamageResult(
                Math.Max(1, rounded),
                unrounded,
                input.IsCritical,
                input.ResponseDamageMultiplier);
        }
    }
}
