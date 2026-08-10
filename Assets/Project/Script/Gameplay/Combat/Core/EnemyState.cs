using System;

namespace PowerMath.Gameplay.Combat
{
    public sealed class EnemyState
    {
        public EnemyState(EnemyDefinitionData definition, int spawnHp)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (spawnHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnHp));
            }

            MaximumHp = spawnHp;
            CurrentHp = spawnHp;
            RemainingCooldown = definition.MaximumCooldown;
        }

        public EnemyState(
            EnemyDefinitionData definition,
            int maximumHp,
            int currentHp,
            int remainingCooldown)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (maximumHp <= 0 || currentHp < 0 || currentHp > maximumHp)
                throw new ArgumentOutOfRangeException(nameof(currentHp));
            if (remainingCooldown < 0 || remainingCooldown > definition.MaximumCooldown)
                throw new ArgumentOutOfRangeException(nameof(remainingCooldown));
            MaximumHp = maximumHp;
            CurrentHp = currentHp;
            RemainingCooldown = remainingCooldown;
        }

        public EnemyDefinitionData Definition { get; }

        public int MaximumHp { get; }

        public int CurrentHp { get; private set; }

        public int RemainingCooldown { get; private set; }

        public bool IsDefeated => CurrentHp == 0;

        public void ConsumeCooldown()
        {
            if (IsDefeated)
            {
                throw new InvalidOperationException("A defeated enemy cannot commit an attempt.");
            }

            if (RemainingCooldown <= 0)
            {
                throw new InvalidOperationException(
                    "Enemy cooldown must resolve before another attempt."
                );
            }

            RemainingCooldown--;
        }

        public void RestoreCommittedCooldown()
        {
            RemainingCooldown = Math.Min(
                Definition.MaximumCooldown,
                RemainingCooldown + 1
            );
        }

        public int ApplyDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
            return CurrentHp;
        }

        public void ResetCooldown()
        {
            RemainingCooldown = Definition.MaximumCooldown;
        }
    }
}
