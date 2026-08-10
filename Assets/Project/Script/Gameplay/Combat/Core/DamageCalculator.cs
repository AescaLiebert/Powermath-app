using System;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct DamageInput
    {
        public DamageInput(
            int effectiveAttack,
            double rankMultiplier,
            double buffMultiplier,
            double criticalDamagePercent,
            bool isCritical)
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
        }

        public int EffectiveAttack { get; }

        public double RankMultiplier { get; }

        public double BuffMultiplier { get; }

        public double CriticalDamagePercent { get; }

        public bool IsCritical { get; }
    }

    public readonly struct DamageResult
    {
        public DamageResult(int finalDamage, double unroundedDamage, bool isCritical)
        {
            FinalDamage = finalDamage;
            UnroundedDamage = unroundedDamage;
            IsCritical = isCritical;
        }

        public int FinalDamage { get; }

        public double UnroundedDamage { get; }

        public bool IsCritical { get; }
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
                criticalMultiplier;

            int rounded = (int)Math.Round(
                unrounded,
                MidpointRounding.AwayFromZero
            );

            return new DamageResult(Math.Max(1, rounded), unrounded, input.IsCritical);
        }
    }
}
