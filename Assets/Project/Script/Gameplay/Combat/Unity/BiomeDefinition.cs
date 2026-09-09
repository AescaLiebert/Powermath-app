using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "BiomeDefinition",
        menuName = "PowerMath/Stage Map/Biome Definition")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class BossBinding
        {
            [Min(1)] public int stage;
            public EnemyDefinition monster;
        }

        [SerializeField] private string biomeId = "biome-1";
        [SerializeField] private string fallbackTitle = "Biome";
        [Min(1), SerializeField] private int firstStage = 1;
        [Min(1), SerializeField] private int lastStage = 30;
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("Optional visual used from the deterministic midpoint onward.")]
        [SerializeField] private Sprite secondaryBackgroundSprite;
        [SerializeField] private Sprite mapLandmarkSprite;
        [SerializeField] private Vector2 normalizedMapPosition = new Vector2(0.1f, 0.5f);
        [SerializeField] private EnemyDefinition[] normalMonsters = Array.Empty<EnemyDefinition>();
        [SerializeField] private BossBinding[] bossBindings = Array.Empty<BossBinding>();

        public string BiomeId => biomeId;
        public string FallbackTitle => fallbackTitle;
        public int FirstStage => firstStage;
        public int LastStage => lastStage;
        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite SecondaryBackgroundSprite => secondaryBackgroundSprite;
        public int MidpointStage => firstStage +
            Mathf.CeilToInt((lastStage - firstStage + 1) / 2f);
        public Sprite MapLandmarkSprite => mapLandmarkSprite;
        public Vector2 NormalizedMapPosition => normalizedMapPosition;
        public IReadOnlyList<EnemyDefinition> NormalMonsters =>
            normalMonsters ?? Array.Empty<EnemyDefinition>();
        public IReadOnlyList<BossBinding> BossBindings =>
            bossBindings ?? Array.Empty<BossBinding>();

        public Sprite ResolveBackground(int stage)
        {
            return secondaryBackgroundSprite != null && lastStage > firstStage &&
                stage >= MidpointStage
                ? secondaryBackgroundSprite
                : backgroundSprite;
        }
    }
}
