using System;

namespace PowerMath.Gameplay.Combat
{
    public readonly struct PlayerCombatStats
    {
        public PlayerCombatStats(int effectiveAttack, double criticalRate, double criticalDamagePercent)
        {
            if (effectiveAttack <= 0) throw new ArgumentOutOfRangeException(nameof(effectiveAttack));
            if (criticalRate < 0d || criticalRate > 1d) throw new ArgumentOutOfRangeException(nameof(criticalRate));
            if (criticalDamagePercent < 0d) throw new ArgumentOutOfRangeException(nameof(criticalDamagePercent));
            EffectiveAttack = effectiveAttack;
            CriticalRate = criticalRate;
            CriticalDamagePercent = criticalDamagePercent;
        }

        public int EffectiveAttack { get; }
        public double CriticalRate { get; }
        public double CriticalDamagePercent { get; }
    }
}
