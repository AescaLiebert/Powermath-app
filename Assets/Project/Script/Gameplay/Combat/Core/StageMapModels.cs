using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerMath.Gameplay.Combat
{
    public enum StageEncounterKind
    {
        NormalMonster,
        MiniBoss,
        BigBoss,
        FinalBoss,
        ChallengeEvent
    }

    public sealed class MonsterData
    {
        public MonsterData(string id, string name, StageEncounterKind kind,
            string biomeId, int maximumCooldown, int baseHp, int hpMultiplierBasisPoints = 10000)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(biomeId)) throw new ArgumentException("Monster identity is required.");
            if (kind == StageEncounterKind.ChallengeEvent) throw new ArgumentException("Events are not monsters.");
            if (maximumCooldown <= 0 || baseHp <= 0 || hpMultiplierBasisPoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumCooldown));
            Id = id.Trim(); Name = name.Trim(); Kind = kind; BiomeId = biomeId.Trim();
            MaximumCooldown = maximumCooldown;
            BaseHp = baseHp;
            HpMultiplierBasisPoints = hpMultiplierBasisPoints;
        }
        public string Id { get; }
        public string Name { get; }
        public StageEncounterKind Kind { get; }
        public string BiomeId { get; }
        public int MaximumCooldown { get; }
        public int BaseHp { get; }
        public int HpMultiplierBasisPoints { get; }
    }

    public sealed class EventData
    {
        public EventData(string id, string title, string questionDocumentId, string handlerKey)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(questionDocumentId) || string.IsNullOrWhiteSpace(handlerKey))
                throw new ArgumentException("Event identity, content, and handler are required.");
            Id = id.Trim(); Title = title.Trim(); QuestionDocumentId = questionDocumentId.Trim();
            HandlerKey = handlerKey.Trim();
        }
        public string Id { get; }
        public string Title { get; }
        public string QuestionDocumentId { get; }
        public string HandlerKey { get; }
    }

    public sealed class BiomeData
    {
        private readonly Dictionary<int, MonsterData> _bosses;
        public BiomeData(string id, string title, int firstStage, int lastStage,
            IReadOnlyList<MonsterData> normalMonsters, IReadOnlyDictionary<int, MonsterData> bosses)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Biome identity is required.");
            if (firstStage < StageId.First || lastStage > StageId.Final || firstStage > lastStage)
                throw new ArgumentOutOfRangeException(nameof(firstStage));
            if (normalMonsters == null || normalMonsters.Count == 0)
                throw new ArgumentException("Each biome needs normal monsters.");
            Id = id.Trim(); Title = title.Trim(); FirstStage = firstStage; LastStage = lastStage;
            NormalMonsters = normalMonsters.ToArray();
            _bosses = bosses == null ? new Dictionary<int, MonsterData>() :
                new Dictionary<int, MonsterData>(bosses);
        }
        public string Id { get; }
        public string Title { get; }
        public int FirstStage { get; }
        public int LastStage { get; }
        public IReadOnlyList<MonsterData> NormalMonsters { get; }
        public bool Contains(StageId stage) => stage.Value >= FirstStage && stage.Value <= LastStage;
        public bool TryGetBoss(StageId stage, out MonsterData monster) => _bosses.TryGetValue(stage.Value, out monster);
    }

    public sealed class StageMapData
    {
        private readonly Dictionary<int, EventData> _events;
        public StageMapData(string catalogVersion, int normalHpBaseline,
            int growthBasisPoints, int variationBasisPoints,
            IReadOnlyList<BiomeData> biomes, IReadOnlyDictionary<int, EventData> events)
        {
            if (string.IsNullOrWhiteSpace(catalogVersion)) throw new ArgumentException("Catalog version is required.");
            if (normalHpBaseline <= 0 || growthBasisPoints < 0 || variationBasisPoints < 0 || variationBasisPoints > 9999)
                throw new ArgumentOutOfRangeException(nameof(normalHpBaseline));
            if (biomes == null || biomes.Count != 7) throw new ArgumentException("Exactly seven biomes are required.");
            CatalogVersion = catalogVersion.Trim(); NormalHpBaseline = normalHpBaseline;
            GrowthBasisPoints = growthBasisPoints; VariationBasisPoints = variationBasisPoints;
            Biomes = biomes.ToArray();
            _events = events == null ? new Dictionary<int, EventData>() : new Dictionary<int, EventData>(events);
        }
        public string CatalogVersion { get; }
        public int NormalHpBaseline { get; }
        public int GrowthBasisPoints { get; }
        public int VariationBasisPoints { get; }
        public IReadOnlyList<BiomeData> Biomes { get; }
        public BiomeData GetBiome(StageId stage) => Biomes.First(value => value.Contains(stage));
        public bool TryGetEvent(StageId stage, out EventData value) => _events.TryGetValue(stage.Value, out value);
        public IReadOnlyCollection<string> EventQuestionDocumentIds =>
            _events.Values.Select(value => value.QuestionDocumentId).Distinct(StringComparer.Ordinal).ToArray();
    }

    public sealed class EncounterSelection
    {
        public EncounterSelection(StageId stage, string biomeId, string biomeTitle,
            StageEncounterKind kind, string encounterId, string displayName,
            int maximumHp, int maximumCooldown, string questionDocumentId = "")
        {
            Stage = stage; BiomeId = biomeId ?? string.Empty; BiomeTitle = biomeTitle ?? string.Empty;
            Kind = kind; EncounterId = encounterId ?? string.Empty; DisplayName = displayName ?? string.Empty;
            MaximumHp = maximumHp; MaximumCooldown = maximumCooldown;
            QuestionDocumentId = questionDocumentId ?? string.Empty;
        }
        public StageId Stage { get; }
        public string BiomeId { get; }
        public string BiomeTitle { get; }
        public StageEncounterKind Kind { get; }
        public string EncounterId { get; }
        public string DisplayName { get; }
        public int MaximumHp { get; }
        public int MaximumCooldown { get; }
        public string QuestionDocumentId { get; }
        public bool IsEvent => Kind == StageEncounterKind.ChallengeEvent;
    }

    public static class StageClassificationPolicy
    {
        public static StageEncounterKind Classify(StageId stage, int biomeLastStage, bool isFinalBiome)
        {
            if (stage.Value == biomeLastStage)
                return isFinalBiome ? StageEncounterKind.FinalBoss : StageEncounterKind.BigBoss;
            if (stage.Value % 5 == 0)
                return StageEncounterKind.MiniBoss;
            return StageEncounterKind.NormalMonster;
        }

        public static StageEncounterKind Classify(StageId stage)
        {
            if (stage.Value == StageId.Final) return StageEncounterKind.FinalBoss;
            if (stage.Value % 5 == 0) return StageEncounterKind.MiniBoss;
            return StageEncounterKind.NormalMonster;
        }

        public static bool IsProtected(StageId stage) => stage.Value % 5 == 0;
    }

    public sealed class StageEncounterResolver
    {
        private readonly StageMapData _map;
        public StageEncounterResolver(StageMapData map) => _map = map ?? throw new ArgumentNullException(nameof(map));

        public EncounterSelection Resolve(string runId, StageId stage)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run ID is required.", nameof(runId));
            BiomeData biome = _map.GetBiome(stage);
            bool isFinalBiome = _map.Biomes != null && _map.Biomes.Count > 0 &&
                _map.Biomes[_map.Biomes.Count - 1] == biome;
            StageEncounterKind kind = StageClassificationPolicy.Classify(stage, biome.LastStage, isFinalBiome);
            if (kind == StageEncounterKind.NormalMonster && _map.TryGetEvent(stage, out EventData eventData))
                return new EncounterSelection(stage, biome.Id, biome.Title, StageEncounterKind.ChallengeEvent,
                    eventData.Id, eventData.Title, 1, 0, eventData.QuestionDocumentId);

            MonsterData monster;
            if (kind == StageEncounterKind.NormalMonster)
            {
                ulong hash = StableHash64.Compute(runId, _map.CatalogVersion, stage.Value.ToString(), "monster");
                monster = biome.NormalMonsters[(int)(hash % (ulong)biome.NormalMonsters.Count)];
            }
            else if (!biome.TryGetBoss(stage, out monster))
                throw new InvalidOperationException($"Stage {stage.Value} has no {kind} binding.");
            if (monster.Kind != kind) throw new InvalidOperationException("Boss binding class does not match Stage classification.");
            int hp = StageHpPolicy.Calculate(_map, monster, runId, stage);
            return new EncounterSelection(stage, biome.Id, biome.Title, kind, monster.Id, monster.Name,
                hp, monster.MaximumCooldown);
        }
    }

    public static class StageHpPolicy
    {
        public static int Calculate(StageMapData map, MonsterData monster, string runId, StageId stage)
        {
            long growth = 10000L + (long)(stage.WorldLevel - 1) * map.GrowthBasisPoints;
            ulong hash = StableHash64.Compute(runId, map.CatalogVersion, stage.Value.ToString(), "hp");
            int span = map.VariationBasisPoints * 2 + 1;
            long variation = 10000L - map.VariationBasisPoints + (long)(hash % (ulong)span);
            long baseline = monster.BaseHp > 0 ? (long)monster.BaseHp : (long)map.NormalHpBaseline;
            long numerator = checked(baseline * growth);
            numerator = checked(numerator * monster.HpMultiplierBasisPoints);
            numerator = checked(numerator * variation);
            return Math.Max(1, checked((int)((numerator + 500000000000L) / 1000000000000L)));
        }
    }

    public static class StableHash64
    {
        public static ulong Compute(params string[] values)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (string value in values ?? Array.Empty<string>())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
                foreach (byte item in bytes) { hash ^= item; hash *= prime; }
                hash ^= 255; hash *= prime;
            }
            return hash;
        }
    }

    public static class DevelopmentStageMapFactory
    {
        public static StageMapData Create()
        {
            var biomes = new List<BiomeData>();
            int[] biomeLengths = { 30, 30, 30, 30, 40, 30, 25 };
            int currentFirst = 1;
            for (int biomeIndex = 0; biomeIndex < 7; biomeIndex++)
            {
                int first = currentFirst;
                int last = first + biomeLengths[biomeIndex] - 1;
                currentFirst = last + 1;
                bool isFinalBiome = biomeIndex == 6;
                string biomeId = $"biome-{biomeIndex + 1}";
                int normalBaseHp = 30 + biomeIndex * 5;
                var normals = new[]
                {
                    new MonsterData($"{biomeId}-monster-a", $"Biome {biomeIndex + 1} Scout",
                        StageEncounterKind.NormalMonster, biomeId, 3, normalBaseHp, 10000),
                    new MonsterData($"{biomeId}-monster-b", $"Biome {biomeIndex + 1} Guardian",
                        StageEncounterKind.NormalMonster, biomeId, 2, normalBaseHp + 5, 10000)
                };
                var bosses = new Dictionary<int, MonsterData>();
                for (int stage = first; stage <= last; stage++)
                {
                    StageId stageId = new StageId(stage);
                    if (!StageClassificationPolicy.IsProtected(stageId)) continue;
                    StageEncounterKind kind = StageClassificationPolicy.Classify(stageId, last, isFinalBiome);
                    int bossBaseHp = kind == StageEncounterKind.MiniBoss ? 45 + biomeIndex * 15
                        : kind == StageEncounterKind.BigBoss ? 100 + biomeIndex * 50 : 500;
                    bosses.Add(stage, new MonsterData($"boss-{stage}",
                        kind == StageEncounterKind.FinalBoss ? "Final Boss" : $"{kind} {stage}",
                        kind, biomeId, kind == StageEncounterKind.MiniBoss ? 3 : 2, bossBaseHp, 10000));
                }
                biomes.Add(new BiomeData(biomeId, $"Biome {biomeIndex + 1}",
                    first, last, normals, bosses));
            }
            return new StageMapData("development-stage-map-v1", 40, 1200, 500,
                biomes, new Dictionary<int, EventData>
                {
                    { 7, new EventData("challenge-monster", "Challenge Monster",
                        "challenge", "challenge-monster") }
                });
        }
    }
}
