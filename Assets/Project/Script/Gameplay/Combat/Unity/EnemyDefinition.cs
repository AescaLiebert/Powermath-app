using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "PowerMath/Combat/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string enemyId = "rock-titan";
        [SerializeField] private string displayName = "Rock Titan";

        [Header("Combat")]
        [Min(1)]
        [SerializeField] private int baseHp = 40;
        [Min(1)]
        [SerializeField] private int maximumCooldown = 3;

        [Header("Presentation")]
        [SerializeField] private Sprite enemySprite;

        public Sprite EnemySprite => enemySprite;

        public EnemyDefinitionData ToDomainData()
        {
            return new EnemyDefinitionData(
                enemyId,
                displayName,
                Mathf.Max(1, baseHp),
                Mathf.Max(1, maximumCooldown)
            );
        }

#if UNITY_EDITOR
        public void ConfigurePrototype(
            string id,
            string name,
            int hp,
            int cooldown,
            Sprite sprite)
        {
            enemyId = id;
            displayName = name;
            baseHp = hp;
            maximumCooldown = cooldown;
            enemySprite = sprite;
        }
#endif
    }
}
