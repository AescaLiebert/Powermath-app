using System;

namespace PowerMath.Gameplay.Combat
{
    public static class ResponseDamagePolicy
    {
        public const int MinimumScore = 1;
        public const int MaximumScore = 10;

        private static readonly int[] PercentByScore =
        {
            100, // Score 1: Base hit (1.0x)
            110, // Score 2: +10%
            120, // Score 3: +20%
            130, // Score 4: +30%
            140, // Score 5: +40%
            150, // Score 6: +50%
            160, // Score 7: +60%
            170, // Score 8: +70%
            180, // Score 9: +80%
            200  // Score 10: Max speed / Grace (+100%, 2.0x)
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
            double unroundedDamage,
            int finalDamage)
        {
            EffectiveAttack = effectiveAttack;
            RankMultiplier = rankMultiplier;
            BuffMultiplier = buffMultiplier;
            CriticalMultiplier = criticalMultiplier;
            ResponseScore = responseScore;
            ResponseMultiplier = responseMultiplier;
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
                input.ResponseDamageMultiplier;

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
                unrounded,
                finalDamage);

            return new DamageResult(
                finalDamage,
                unrounded,
                input.IsCritical,
                breakdown);
        }
    }
}
