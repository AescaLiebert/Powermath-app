using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(fileName = "StageMapDefinition", menuName = "PowerMath/Stage Map/Stage Map Definition")]
    public sealed class StageMapDefinition : ScriptableObject
    {
        [Serializable] public sealed class FixedEventBinding
        {
            [Min(1)] public int stage;
            public EventDefinition eventDefinition;
        }
        [SerializeField] private string catalogVersion = "stage-map-v1";
        [Min(1), SerializeField] private int normalHpBaseline = 40;
        [Min(0), SerializeField] private int worldLevelGrowthBasisPoints = 1200;
        [Range(0, 4999), SerializeField] private int hpVariationBasisPoints = 500;
        [Range(0f, 100f), SerializeField] private float eventEncounterLuckPercentage;
        [SerializeField] private BiomeDefinition[] biomes = Array.Empty<BiomeDefinition>();
        [SerializeField] private EventDefinition[] eventDefinitions = Array.Empty<EventDefinition>();
        [SerializeField] private FixedEventBinding[] fixedEvents = Array.Empty<FixedEventBinding>();
        public IReadOnlyList<BiomeDefinition> Biomes => biomes ?? Array.Empty<BiomeDefinition>();
        public IReadOnlyList<FixedEventBinding> FixedEvents => fixedEvents ?? Array.Empty<FixedEventBinding>();

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
                var events = fixedEvents.ToDictionary(value => value.stage,
                    value => value.eventDefinition.ToDomainData());
                data = new StageMapData(catalogVersion, normalHpBaseline,
                    worldLevelGrowthBasisPoints, hpVariationBasisPoints, mappedBiomes, events,
                    eventDefinitions.Select(value => value.ToDomainData()).ToArray(),
                    Mathf.RoundToInt(eventEncounterLuckPercentage * 100f));
                return true;
            }
            catch (Exception exception) { error = exception.Message; return false; }
        }

        public BiomeDefinition FindBiome(string id) =>
            (biomes ?? Array.Empty<BiomeDefinition>()).FirstOrDefault(value => value != null && value.BiomeId == id);
        public EnemyDefinition FindMonster(string id) =>
            (biomes ?? Array.Empty<BiomeDefinition>()).Where(value => value != null)
                .SelectMany(value => value.NormalMonsters.Concat(value.BossBindings.Where(x => x != null && x.monster != null).Select(x => x.monster)))
                .FirstOrDefault(value => value != null && value.EnemyId == id);
        public EventDefinition FindEvent(string id) =>
            (eventDefinitions ?? Array.Empty<EventDefinition>())
                .Concat((fixedEvents ?? Array.Empty<FixedEventBinding>())
                    .Where(value => value != null && value.eventDefinition != null)
                    .Select(value => value.eventDefinition))
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
            foreach (FixedEventBinding binding in fixedEvents ?? Array.Empty<FixedEventBinding>())
                if (binding == null || binding.eventDefinition == null || binding.stage < 1 || binding.stage > StageId.Final ||
                    StageClassificationPolicy.IsProtected(new StageId(binding.stage)))
                    throw new InvalidOperationException("Events require unique eligible normal Stages.");
            if ((fixedEvents ?? Array.Empty<FixedEventBinding>()).GroupBy(value => value.stage).Any(group => group.Count() > 1))
                throw new InvalidOperationException("Only one Event may bind a Stage.");
            for (int blockStart = StageId.First; blockStart <= StageId.Final; blockStart += EventScheduleGenerator.StagesPerBlock)
            {
                int blockEnd = Math.Min(StageId.Final, blockStart + EventScheduleGenerator.StagesPerBlock - 1);
                int fixedChallenges = (fixedEvents ?? Array.Empty<FixedEventBinding>()).Count(value =>
                    value != null && value.eventDefinition != null &&
                    value.eventDefinition.EventStageType == EventStageType.ChallengeMonster &&
                    value.stage >= blockStart && value.stage <= blockEnd);
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
