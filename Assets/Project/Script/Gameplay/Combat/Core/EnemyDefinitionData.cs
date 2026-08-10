using System;

namespace PowerMath.Gameplay.Combat
{
    public sealed class EnemyDefinitionData
    {
        public EnemyDefinitionData(
            string enemyId,
            string displayName,
            int baseHp,
            int maximumCooldown)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID is required.", nameof(enemyId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "Enemy display name is required.",
                    nameof(displayName)
                );
            }

            if (baseHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseHp));
            }

            if (maximumCooldown <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCooldown));
            }

            EnemyId = enemyId.Trim();
            DisplayName = displayName.Trim();
            BaseHp = baseHp;
            MaximumCooldown = maximumCooldown;
        }

        public string EnemyId { get; }

        public string DisplayName { get; }

        public int BaseHp { get; }

        public int MaximumCooldown { get; }
    }
}
