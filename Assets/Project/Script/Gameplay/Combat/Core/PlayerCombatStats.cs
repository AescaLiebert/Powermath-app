using System;
using PowerMath.Gameplay.Pets;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct PlayerCombatStats
    {
        public PlayerCombatStats(
            int effectiveAttack,
            double criticalRate,
            double criticalDamagePercent,
            int effectivePetAttack = 0,
            int bonusMaxHearts = 0,
            ActivePetPassiveSet petPassives = null,
            double powerCoinBonusPercent = 0d)
        {
            if (effectiveAttack <= 0) throw new ArgumentOutOfRangeException(nameof(effectiveAttack));
            if (criticalRate < 0d || criticalRate > 1d) throw new ArgumentOutOfRangeException(nameof(criticalRate));
            if (criticalDamagePercent < 0d) throw new ArgumentOutOfRangeException(nameof(criticalDamagePercent));
            if (effectivePetAttack < 0) throw new ArgumentOutOfRangeException(nameof(effectivePetAttack));
            if (bonusMaxHearts < 0) throw new ArgumentOutOfRangeException(nameof(bonusMaxHearts));
            if (powerCoinBonusPercent < 0d) throw new ArgumentOutOfRangeException(nameof(powerCoinBonusPercent));

            EffectiveAttack = effectiveAttack;
            CriticalRate = criticalRate;
            CriticalDamagePercent = criticalDamagePercent;
            EffectivePetAttack = effectivePetAttack;
            BonusMaxHearts = bonusMaxHearts;
            PetPassives = petPassives ?? ActivePetPassiveSet.Empty;
            PowerCoinBonusPercent = powerCoinBonusPercent;
        }

        public int EffectiveAttack { get; }
        public double CriticalRate { get; }
        public double CriticalDamagePercent { get; }
        public int EffectivePetAttack { get; }
        public int BonusMaxHearts { get; }
        public ActivePetPassiveSet PetPassives { get; }
        public double PowerCoinBonusPercent { get; }
        public double PowerCoinBonusMultiplier => 1d + PowerCoinBonusPercent / 100d;
    }
}
