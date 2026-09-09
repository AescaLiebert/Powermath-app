using PowerMath.Audio;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "PowerMath/Combat/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject, IEnemySfxProfile
    {
        [Header("Identity")]
        [SerializeField] private string enemyId = "rock-titan";
        [SerializeField] private string displayName = "Rock Titan";
        [SerializeField] private string thaiDisplayName = string.Empty;
        [SerializeField] private string biomeId = "biome-1";
        [SerializeField] private StageEncounterKind encounterKind = StageEncounterKind.NormalMonster;

        [Header("Combat")]
        [Min(1)]
        [SerializeField] private int baseHp = 40;
        [Min(1)]
        [SerializeField] private int maximumCooldown = 3;
        [Min(1)]
        [SerializeField] private int hpMultiplierBasisPoints = 10000;

        [Header("Presentation")]
        [SerializeField] private Sprite enemySprite;

        [Header("Audio Ownership (Optional Custom Overrides)")]
        [Tooltip("Custom SFX played when this specific enemy appears. If unassigned, defaults to encounter kind / global SFX.")]
        [SerializeField] private SfxCueConfig customAppearSfx;
        [Tooltip("Custom SFX played when this specific enemy takes damage.")]
        [SerializeField] private SfxCueConfig customHurtSfx;
        [Tooltip("Custom SFX played when this specific enemy attacks.")]
        [SerializeField] private SfxCueConfig customAttackSfx;
        [Tooltip("Custom SFX played when this specific enemy dies.")]
        [SerializeField] private SfxCueConfig customDieSfx;

        public Sprite EnemySprite => enemySprite;
        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public string ThaiDisplayName => thaiDisplayName;
        public string EnglishDisplayName => displayName;
        public string BiomeId => biomeId;
        public StageEncounterKind EncounterKind => encounterKind;
        public int BaseHp => baseHp;
        public int MaximumCooldown => maximumCooldown;
        public int HpMultiplierBasisPoints => hpMultiplierBasisPoints;

        public SfxCueConfig AppearSfx => customAppearSfx;
        public SfxCueConfig HurtSfx => customHurtSfx;
        public SfxCueConfig AttackSfx => customAttackSfx;
        public SfxCueConfig DieSfx => customDieSfx;

        public string GetDisplayName(string locale = "en")
        {
            if (string.Equals(locale, "th", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(thaiDisplayName))
                return thaiDisplayName;
            return displayName;
        }

        public MonsterData ToMonsterData()
        {
            return new MonsterData(enemyId, displayName, encounterKind, biomeId,
                Mathf.Max(1, maximumCooldown), Mathf.Max(1, baseHp), Mathf.Max(1, hpMultiplierBasisPoints));
        }

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
            Sprite sprite,
            string thaiName = "")
        {
            enemyId = id;
            displayName = name;
            thaiDisplayName = thaiName;
            baseHp = hp;
            maximumCooldown = cooldown;
            enemySprite = sprite;
            biomeId = "biome-1";
            encounterKind = StageEncounterKind.NormalMonster;
            hpMultiplierBasisPoints = 10000;
        }
#endif
    }
}
