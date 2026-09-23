using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(fileName = "StageMapDefinition", menuName = "PowerMath/Stage Map/Stage Map Definition")]
    public sealed class StageMapDefinition : ScriptableObject
    {
        /// <summary>
        /// Pins a specific stage to either a fixed monster encounter or a fixed event.
        /// Exactly one of <see cref="monsterDefinition"/> / <see cref="eventDefinition"/> must be set.
        /// Only non-protected normal stages (not multiples of 5) are eligible.
        /// </summary>
        [Serializable] public sealed class FixedStageBinding
        {
            [Min(1)] public int stage;
            [Tooltip("Set this to pin a specific monster to this stage (mutually exclusive with Event).")]
            public EnemyDefinition monsterDefinition;
            [Tooltip("Set this to pin a specific event to this stage (mutually exclusive with Monster).")]
            public EventDefinition eventDefinition;
        }

        [SerializeField] private string catalogVersion = "stage-map-v1";

        [Header("World Level Growth By Phase (Basis Points: 10000 = 1.0x / 100%)")]
        [Tooltip("Phase 1: Early Game (WL 1-12, Stages 1-60). Default 1500 (+15% per WL).")]
        [Min(0), SerializeField] private int worldLevelGrowthBasisPoints = 1200;
        [Tooltip("Phase 2: Mid Game (WL 13-24, Stages 61-120). Default 3500 (+35% per WL).")]
        [Min(0), SerializeField] private int phase2GrowthBasisPoints = 3500;
        [Tooltip("Phase 3: Transition Bridge (WL 25-30, Stages 121-150). Default 8000 (+80% per WL).")]
        [Min(0), SerializeField] private int phase3GrowthBasisPoints = 8000;
        [Tooltip("Phase 4: Late Game (WL 31-40, Stages 151-200). Default 15000 (+150% per WL). Set to 50000 for +5.0x per WL.")]
        [Min(0), SerializeField] private int phase4GrowthBasisPoints = 15000;

        [Range(0, 4999), SerializeField] private int hpVariationBasisPoints = 500;
        [Range(0f, 100f), SerializeField] private float eventEncounterLuckPercentage;
        [SerializeField] private BiomeDefinition[] biomes = Array.Empty<BiomeDefinition>();
        [SerializeField] private EventDefinition[] eventDefinitions = Array.Empty<EventDefinition>();
        [SerializeField] private FixedStageBinding[] fixedStages = Array.Empty<FixedStageBinding>();

        public int WorldLevelGrowthBasisPoints => worldLevelGrowthBasisPoints;
        public int Phase1GrowthBasisPoints => worldLevelGrowthBasisPoints;
        public int Phase2GrowthBasisPoints => phase2GrowthBasisPoints > 0 ? phase2GrowthBasisPoints : 3500;
        public int Phase3GrowthBasisPoints => phase3GrowthBasisPoints > 0 ? phase3GrowthBasisPoints : 8000;
        public int Phase4GrowthBasisPoints => phase4GrowthBasisPoints > 0 ? phase4GrowthBasisPoints : 15000;
        public IReadOnlyList<BiomeDefinition> Biomes => biomes ?? Array.Empty<BiomeDefinition>();
        public IReadOnlyList<FixedStageBinding> FixedStages => fixedStages ?? Array.Empty<FixedStageBinding>();

        public bool TryMap(out StageMapData data, out string error)
        {
            data = null; error = string.Empty;
            try
            {
                ValidateAuthoring();
                var mappedBiomes = new List<BiomeData>();
                foreach (BiomeDefinition biome in biomes)
                {
                    var bosses = biome.BossBindings.ToDictionary(value => value.stage,
                        value => value.monster.ToMonsterData());
                    mappedBiomes.Add(new BiomeData(biome.BiomeId, biome.FallbackTitle,
                        biome.SecondaryTitle,
                        biome.FirstStage, biome.LastStage,
                        biome.NormalMonsters.Select(value => value.ToMonsterData()).ToArray(), bosses));
                }

                // Split fixedStages into two typed dictionaries.
                var fixedEvents = (fixedStages ?? Array.Empty<FixedStageBinding>())
                    .Where(b => b?.eventDefinition != null)
                    .ToDictionary(b => b.stage, b => b.eventDefinition.ToDomainData());

                var fixedMonsters = (fixedStages ?? Array.Empty<FixedStageBinding>())
                    .Where(b => b?.monsterDefinition != null)
                    .ToDictionary(b => b.stage, b => b.monsterDefinition.ToMonsterData());

                int p1 = worldLevelGrowthBasisPoints > 0 ? worldLevelGrowthBasisPoints : 1200;
                int p2 = phase2GrowthBasisPoints > 0 ? phase2GrowthBasisPoints : 3500;
                int p3 = phase3GrowthBasisPoints > 0 ? phase3GrowthBasisPoints : 8000;
                int p4 = phase4GrowthBasisPoints > 0 ? phase4GrowthBasisPoints : 15000;

                data = new StageMapData(catalogVersion,
                    p1, hpVariationBasisPoints, mappedBiomes,
                    fixedEvents,
                    eventDefinitions.Select(value => value.ToDomainData()).ToArray(),
                    Mathf.RoundToInt(eventEncounterLuckPercentage * 100f),
                    fixedMonsters,
                    p2, p3, p4);
                return true;
            }
            catch (Exception exception) { error = exception.Message; return false; }
        }

        public BiomeDefinition FindBiome(string id) =>
            (biomes ?? Array.Empty<BiomeDefinition>()).FirstOrDefault(value => value != null && value.BiomeId == id);

        public EnemyDefinition FindMonster(string id) =>
            // Search biome normal/boss pools first, then fixed stage monster pins.
            (biomes ?? Array.Empty<BiomeDefinition>()).Where(value => value != null)
                .SelectMany(value => value.NormalMonsters.Concat(
                    value.BossBindings.Where(x => x != null && x.monster != null).Select(x => x.monster)))
                .Concat((fixedStages ?? Array.Empty<FixedStageBinding>())
                    .Where(b => b?.monsterDefinition != null)
                    .Select(b => b.monsterDefinition))
                .FirstOrDefault(value => value != null && value.EnemyId == id);

        public EventDefinition FindEvent(string id) =>
            (eventDefinitions ?? Array.Empty<EventDefinition>())
                .Concat((fixedStages ?? Array.Empty<FixedStageBinding>())
                    .Where(b => b?.eventDefinition != null)
                    .Select(b => b.eventDefinition))
                .FirstOrDefault(value => value != null && value.EventId == id);

        private void ValidateAuthoring()
        {
            if (string.IsNullOrWhiteSpace(catalogVersion)) throw new InvalidOperationException("Stage Map catalogVersion is required.");
            if (biomes == null || biomes.Length != 7) throw new InvalidOperationException("Stage Map requires exactly seven biomes.");
            int expectedFirst = 1;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < biomes.Length; i++)
            {
                BiomeDefinition biome = biomes[i];
                if (biome == null || !ids.Add(biome.BiomeId)) throw new InvalidOperationException("Biome IDs must be present and unique.");
                if (string.IsNullOrWhiteSpace(biome.FallbackTitle))
                    throw new InvalidOperationException($"Biome {biome.BiomeId} needs an official fallback title.");
                if (biome.FirstStage != expectedFirst) throw new InvalidOperationException("Biome ranges must be ordered without gaps.");
                if (biome.NormalMonsters.Count == 0 || biome.NormalMonsters.Any(value => value == null || value.EncounterKind != StageEncounterKind.NormalMonster))
                    throw new InvalidOperationException($"{biome.FallbackTitle} needs valid normal monsters.");
                bool isFinalBiome = i == biomes.Length - 1;
                foreach (int stage in Enumerable.Range(biome.FirstStage, biome.LastStage - biome.FirstStage + 1)
                    .Where(value => StageClassificationPolicy.IsProtected(new StageId(value))))
                {
                    BiomeDefinition.BossBinding binding = biome.BossBindings.SingleOrDefault(value => value != null && value.stage == stage);
                    StageEncounterKind expectedKind = StageClassificationPolicy.Classify(new StageId(stage), biome.LastStage, isFinalBiome);
                    if (binding?.monster == null || binding.monster.EncounterKind != expectedKind)
                        throw new InvalidOperationException($"Stage {stage} needs exactly one matching boss binding ({expectedKind}).");
                }
                expectedFirst = biome.LastStage + 1;
            }
            if (expectedFirst != StageId.Final + 1) throw new InvalidOperationException($"Biome ranges must end at Stage {StageId.Final}.");
            if (eventDefinitions == null ||
                !eventDefinitions.Any(value => value != null &&
                    value.EventStageType == EventStageType.ChallengeMonster))
                throw new InvalidOperationException("Stage Map requires at least one chance-based Challenge Monster Event.");
            if (eventDefinitions.Any(value => value == null) ||
                eventDefinitions.GroupBy(value => value.EventId).Any(group =>
                    string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
                throw new InvalidOperationException("Chance-based Event IDs must be present and unique.");

            // Validate fixed stage bindings.
            var usedStages = new HashSet<int>();
            int fixedChallengeCountPerBlock = 0; // reused per block below
            foreach (FixedStageBinding binding in fixedStages ?? Array.Empty<FixedStageBinding>())
            {
                if (binding == null) throw new InvalidOperationException("Fixed stage binding cannot be null.");

                bool hasMonster = binding.monsterDefinition != null;
                bool hasEvent = binding.eventDefinition != null;

                if (!hasMonster && !hasEvent)
                    throw new InvalidOperationException($"Fixed stage binding at Stage {binding.stage} must have either a monster or an event assigned.");
                if (hasMonster && hasEvent)
                    throw new InvalidOperationException($"Fixed stage binding at Stage {binding.stage} cannot have both a monster and an event assigned — pick one.");
                if (binding.stage < 1 || binding.stage > StageId.Final)
                    throw new InvalidOperationException($"Fixed stage binding stage {binding.stage} is out of range.");
                if (StageClassificationPolicy.IsProtected(new StageId(binding.stage)))
                    throw new InvalidOperationException($"Stage {binding.stage} is a protected boss stage and cannot have a fixed stage binding.");
                if (!usedStages.Add(binding.stage))
                    throw new InvalidOperationException($"Stage {binding.stage} has more than one fixed stage binding.");

                if (hasMonster)
                {
                    if (binding.monsterDefinition.EncounterKind != StageEncounterKind.NormalMonster)
                        throw new InvalidOperationException(
                            $"Fixed monster at Stage {binding.stage} must have EncounterKind NormalMonster (got {binding.monsterDefinition.EncounterKind}).");
                }
            }

            // Per-block challenge event cap (counts both chance-scheduled and fixed).
            for (int blockStart = StageId.First; blockStart <= StageId.Final; blockStart += EventScheduleGenerator.StagesPerBlock)
            {
                int blockEnd = Math.Min(StageId.Final, blockStart + EventScheduleGenerator.StagesPerBlock - 1);
                int fixedChallenges = (fixedStages ?? Array.Empty<FixedStageBinding>()).Count(b =>
                    b != null && b.eventDefinition != null &&
                    b.eventDefinition.EventStageType == EventStageType.ChallengeMonster &&
                    b.stage >= blockStart && b.stage <= blockEnd);
                if (fixedChallenges > 2)
                    throw new InvalidOperationException(
                        $"Stages {blockStart}-{blockEnd} cannot contain more than two fixed Challenge Events.");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (biomes != null && biomes.Length > 0 && !TryMap(out _, out string error)) PowerMath.Diagnostics.AppLog.Error("Combat", error, this);
        }
#endif
    }
}
