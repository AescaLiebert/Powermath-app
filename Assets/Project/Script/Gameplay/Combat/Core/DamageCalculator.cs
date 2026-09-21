using System;

namespace PowerMath.Gameplay.Combat
{
    public static class AttackDamageVariancePolicy
    {
        public const double MinimumMultiplier = 0.90d;
        public const double MaximumMultiplier = 1.10d;

        public static double GetMultiplier(double randomUnit)
        {
            if (randomUnit < 0d || randomUnit > 1d)
                throw new ArgumentOutOfRangeException(nameof(randomUnit));

            return MinimumMultiplier +
                (MaximumMultiplier - MinimumMultiplier) * randomUnit;
        }
    }

    public static class ResponseDamagePolicy
    {
        public const int MinimumScore = 1;
        public const int MaximumScore = 10;

        private static readonly int[] PercentByScore =
        {
            100, // Score 1: Base hit (1.0x)
            105, // Score 2: +5%
            110, // Score 3: +10%
            115, // Score 4: +15%
            120, // Score 5: +20%
            125, // Score 6: +25%
            130, // Score 7: +30%
            135, // Score 8: +35%
            140, // Score 9: +40%
            150  // Score 10: Max speed / Grace (+50%, 1.5x)
        };

        public static double GetMultiplier(int responseScore)
        {
            Validate(responseScore);
            return GetPercent(responseScore) / 100d;
        }

        public static int GetPercent(int responseScore)
        {
            Validate(responseScore);
            return PercentByScore[responseScore - MinimumScore];
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
            int responseScore,
            double varianceMultiplier = 1d)
        {
            if (effectiveAttack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveAttack));
            }

            if (rankMultiplier < 0d || buffMultiplier < 0d ||
                criticalDamagePercent < 0d || varianceMultiplier < 0d)
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
            VarianceMultiplier = varianceMultiplier;
        }

        public int EffectiveAttack { get; }

        public double RankMultiplier { get; }

        public double BuffMultiplier { get; }

        public double CriticalDamagePercent { get; }

        public bool IsCritical { get; }

        public int ResponseScore { get; }

        public double ResponseDamageMultiplier { get; }

        public double VarianceMultiplier { get; }
    }

    public readonly struct DamageResult
    {
        public DamageResult(
            int finalDamage,
            double unroundedDamage,
            bool isCritical,
            DamageBreakdown breakdown)
        {
            FinalDamage = finalDamage;
            UnroundedDamage = unroundedDamage;
            IsCritical = isCritical;
            Breakdown = breakdown;
        }

        public int FinalDamage { get; }

        public double UnroundedDamage { get; }

        public bool IsCritical { get; }

        public double ResponseDamageMultiplier => Breakdown.ResponseMultiplier;

        public DamageBreakdown Breakdown { get; }
    }

    public readonly struct DamageBreakdown
    {
        public DamageBreakdown(
            int effectiveAttack,
            double rankMultiplier,
            double buffMultiplier,
            double criticalMultiplier,
            int responseScore,
            double responseMultiplier,
            double varianceMultiplier,
            double unroundedDamage,
            int finalDamage)
        {
            EffectiveAttack = effectiveAttack;
            RankMultiplier = rankMultiplier;
            BuffMultiplier = buffMultiplier;
            CriticalMultiplier = criticalMultiplier;
            ResponseScore = responseScore;
            ResponseMultiplier = responseMultiplier;
            VarianceMultiplier = varianceMultiplier;
            UnroundedDamage = unroundedDamage;
            FinalDamage = finalDamage;
            IsAvailable = true;
        }

        public bool IsAvailable { get; }
        public int EffectiveAttack { get; }
        public double RankMultiplier { get; }
        public double BuffMultiplier { get; }
        public double CriticalMultiplier { get; }
        public int ResponseScore { get; }
        public double ResponseMultiplier { get; }
        public double VarianceMultiplier { get; }
        public double UnroundedDamage { get; }
        public int FinalDamage { get; }
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
                input.ResponseDamageMultiplier *
                input.VarianceMultiplier;

            int rounded = (int)Math.Round(
                unrounded,
                MidpointRounding.AwayFromZero
            );

            int finalDamage = Math.Max(1, rounded);
            var breakdown = new DamageBreakdown(
                input.EffectiveAttack,
                input.RankMultiplier,
                input.BuffMultiplier,
                criticalMultiplier,
                input.ResponseScore,
                input.ResponseDamageMultiplier,
                input.VarianceMultiplier,
                unrounded,
                finalDamage);

            return new DamageResult(
                finalDamage,
                unrounded,
                input.IsCritical,
                breakdown);
        }
    }

    public static class PetCombatPolicy
    {
        public static int CalculateDamage(
            int effectivePetAttack,
            double passiveMagnitude,
            double rankMultiplier,
            bool isCritical,
            double criticalDamagePercent,
            double varianceMultiplier = 1d)
        {
            if (effectivePetAttack <= 0 || passiveMagnitude <= 0d || rankMultiplier <= 0d)
            {
                return 0;
            }

            if (varianceMultiplier < 0d)
                throw new ArgumentOutOfRangeException(nameof(varianceMultiplier));

            double rawDamage = effectivePetAttack * passiveMagnitude *
                rankMultiplier * varianceMultiplier;
            if (rawDamage > int.MaxValue)
            {
                throw new OverflowException("Pet damage exceeds the supported range.");
            }

            int damage = Math.Max(1, (int)Math.Round(
                rawDamage,
                MidpointRounding.AwayFromZero));

            if (isCritical)
            {
                double criticalDamage = damage * (1d + criticalDamagePercent / 100d);
                if (criticalDamage > int.MaxValue)
                {
                    throw new OverflowException("Critical pet damage exceeds the supported range.");
                }

                damage = Math.Max(1, (int)Math.Round(
                    criticalDamage,
                    MidpointRounding.AwayFromZero));
            }

            return damage;
        }
    }
}
