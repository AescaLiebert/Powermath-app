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

    public enum EventStageType
    {
        ChallengeMonster,
        Minigame
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
        public EventData(string id, string title)
            : this(id, title, EventStageType.ChallengeMonster)
        {
        }

        public EventData(string id, string title, EventStageType type)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Event identity and title are required.");
            Id = id.Trim(); Title = title.Trim(); Type = type;
        }
        public string Id { get; }
        public string Title { get; }
        public EventStageType Type { get; }
    }

    public sealed class BiomeData
    {
        private readonly Dictionary<int, MonsterData> _bosses;
        public BiomeData(string id, string title, int firstStage, int lastStage,
            IReadOnlyList<MonsterData> normalMonsters, IReadOnlyDictionary<int, MonsterData> bosses)
            : this(id, title, string.Empty, firstStage, lastStage, normalMonsters, bosses)
        {
        }

        public BiomeData(string id, string title, string secondaryTitle, int firstStage, int lastStage,
            IReadOnlyList<MonsterData> normalMonsters, IReadOnlyDictionary<int, MonsterData> bosses)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Biome identity is required.");
            if (firstStage < StageId.First || lastStage > StageId.Final || firstStage > lastStage)
                throw new ArgumentOutOfRangeException(nameof(firstStage));
            if (normalMonsters == null || normalMonsters.Count == 0)
                throw new ArgumentException("Each biome needs normal monsters.");
            Id = id.Trim(); Title = title.Trim();
            SecondaryTitle = secondaryTitle?.Trim() ?? string.Empty;
            FirstStage = firstStage; LastStage = lastStage;
            NormalMonsters = normalMonsters.ToArray();
            _bosses = bosses == null ? new Dictionary<int, MonsterData>() :
                new Dictionary<int, MonsterData>(bosses);
        }
        public string Id { get; }
        public string Title { get; }
        public string SecondaryTitle { get; }
        public int FirstStage { get; }
        public int LastStage { get; }
        public IReadOnlyList<MonsterData> NormalMonsters { get; }
        public int MidpointStage => FirstStage +
            (int)Math.Ceiling((LastStage - FirstStage + 1) / 2d);
        public string ResolveTitle(StageId stage) =>
            !string.IsNullOrWhiteSpace(SecondaryTitle) && LastStage > FirstStage &&
            stage.Value >= MidpointStage ? SecondaryTitle : Title;
        public bool Contains(StageId stage) => stage.Value >= FirstStage && stage.Value <= LastStage;
        public bool TryGetBoss(StageId stage, out MonsterData monster) => _bosses.TryGetValue(stage.Value, out monster);
    }

    public sealed class StageMapData
    {
        private readonly Dictionary<int, EventData> _fixedEvents;
        private readonly Dictionary<int, MonsterData> _fixedMonsters;
        private readonly Dictionary<string, EventData> _eventDefinitions;

        public StageMapData(string catalogVersion,
            int growthBasisPoints, int variationBasisPoints,
            IReadOnlyList<BiomeData> biomes,
            IReadOnlyDictionary<int, EventData> fixedEvents,
            IReadOnlyList<EventData> chanceEvents = null,
            int eventEncounterLuckBasisPoints = 0,
            IReadOnlyDictionary<int, MonsterData> fixedMonsters = null,
            int phase2GrowthBasisPoints = 3500,
            int phase3GrowthBasisPoints = 8000,
            int phase4GrowthBasisPoints = 15000)
        {
            if (string.IsNullOrWhiteSpace(catalogVersion)) throw new ArgumentException("Catalog version is required.");
            if (growthBasisPoints < 0 || variationBasisPoints < 0 || variationBasisPoints > 9999)
                throw new ArgumentOutOfRangeException(nameof(growthBasisPoints));
            if (phase2GrowthBasisPoints < 0 || phase3GrowthBasisPoints < 0 || phase4GrowthBasisPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(phase2GrowthBasisPoints));
            if (biomes == null || biomes.Count != 7) throw new ArgumentException("Exactly seven biomes are required.");
            if (eventEncounterLuckBasisPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(eventEncounterLuckBasisPoints));
            CatalogVersion = catalogVersion.Trim();
            GrowthBasisPoints = growthBasisPoints;
            Phase1GrowthBasisPoints = growthBasisPoints;
            Phase2GrowthBasisPoints = phase2GrowthBasisPoints;
            Phase3GrowthBasisPoints = phase3GrowthBasisPoints;
            Phase4GrowthBasisPoints = phase4GrowthBasisPoints;
            VariationBasisPoints = variationBasisPoints;
            Biomes = biomes.ToArray();
            EventEncounterLuckBasisPoints = eventEncounterLuckBasisPoints;
            _fixedEvents = fixedEvents == null
                ? new Dictionary<int, EventData>()
                : new Dictionary<int, EventData>(fixedEvents);
            _fixedMonsters = fixedMonsters == null
                ? new Dictionary<int, MonsterData>()
                : new Dictionary<int, MonsterData>(fixedMonsters);
            _eventDefinitions = new Dictionary<string, EventData>(StringComparer.Ordinal);
            foreach (EventData value in _fixedEvents.Values.Concat(chanceEvents ?? Array.Empty<EventData>()))
            {
                if (value == null) throw new ArgumentException("Event definitions cannot be null.");
                if (_eventDefinitions.TryGetValue(value.Id, out EventData existing) &&
                    (existing.Type != value.Type ||
                     !string.Equals(existing.Title, value.Title, StringComparison.Ordinal) ||
                     existing.Type != value.Type))
                    throw new ArgumentException($"Duplicate Event ID '{value.Id}'.");
                _eventDefinitions[value.Id] = value;
            }
            ChanceEvents = (chanceEvents ?? Array.Empty<EventData>()).ToArray();
        }

        [Obsolete("normalHpBaseline is deprecated. Monster BaseHp is defined on EnemyDefinition / MonsterData.")]
        public StageMapData(string catalogVersion, int normalHpBaseline,
            int growthBasisPoints, int variationBasisPoints,
            IReadOnlyList<BiomeData> biomes,
            IReadOnlyDictionary<int, EventData> fixedEvents,
            IReadOnlyList<EventData> chanceEvents = null,
            int eventEncounterLuckBasisPoints = 0,
            IReadOnlyDictionary<int, MonsterData> fixedMonsters = null,
            int phase2GrowthBasisPoints = 3500,
            int phase3GrowthBasisPoints = 8000,
            int phase4GrowthBasisPoints = 15000)
            : this(catalogVersion, growthBasisPoints, variationBasisPoints, biomes, fixedEvents,
                chanceEvents, eventEncounterLuckBasisPoints, fixedMonsters,
                phase2GrowthBasisPoints, phase3GrowthBasisPoints, phase4GrowthBasisPoints)
        {
        }

        public string CatalogVersion { get; }
        [Obsolete("NormalHpBaseline is removed. Monster BaseHp is defined on EnemyDefinition / MonsterData.")]
        public int NormalHpBaseline => 0;
        public int GrowthBasisPoints { get; }
        public int Phase1GrowthBasisPoints { get; }
        public int Phase2GrowthBasisPoints { get; }
        public int Phase3GrowthBasisPoints { get; }
        public int Phase4GrowthBasisPoints { get; }
        public int VariationBasisPoints { get; }
        public int EventEncounterLuckBasisPoints { get; }
        public IReadOnlyList<BiomeData> Biomes { get; }
        public IReadOnlyList<EventData> ChanceEvents { get; }
        public BiomeData GetBiome(StageId stage) => Biomes.First(value => value.Contains(stage));
        public bool TryGetEvent(StageId stage, out EventData value) => TryGetFixedEvent(stage, out value);
        public bool TryGetFixedEvent(StageId stage, out EventData value) =>
            _fixedEvents.TryGetValue(stage.Value, out value);
        public bool TryGetFixedMonster(StageId stage, out MonsterData monster) =>
            _fixedMonsters.TryGetValue(stage.Value, out monster);
        public bool TryGetEventById(string eventId, out EventData value) =>
            _eventDefinitions.TryGetValue(eventId ?? string.Empty, out value);
        public IReadOnlyCollection<int> FixedEventStages => _fixedEvents.Keys.ToArray();
        public IReadOnlyCollection<int> FixedMonsterStages => _fixedMonsters.Keys.ToArray();
        public IReadOnlyCollection<string> EventQuestionDocumentIds =>
            _eventDefinitions.Values.Any(value => value.Type == EventStageType.ChallengeMonster)
                ? new[] { EventQuestionCatalog.DefaultDocumentId }
                : Array.Empty<string>();
    }

    public sealed class EventScheduleSnapshot
    {
        public const int CurrentVersion = 1;
        private readonly HashSet<int> _generatedStages;

        public EventScheduleSnapshot(string catalogVersion, string eventId,
            IEnumerable<int> generatedStages, int baseChanceBasisPoints,
            int petMultiplierBasisPoints, int version = CurrentVersion)
        {
            if (string.IsNullOrWhiteSpace(catalogVersion))
                throw new ArgumentException("Schedule catalog version is required.", nameof(catalogVersion));
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("Scheduled Event ID is required.", nameof(eventId));
            if (version != CurrentVersion)
                throw new ArgumentOutOfRangeException(nameof(version));
            if (baseChanceBasisPoints < 0 || petMultiplierBasisPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(baseChanceBasisPoints));

            int[] stages = (generatedStages ?? Array.Empty<int>()).OrderBy(value => value).ToArray();
            if (stages.Any(value => value < StageId.First || value > StageId.Final) ||
                stages.Distinct().Count() != stages.Length)
                throw new ArgumentException("Scheduled Event Stages must be unique and in range.", nameof(generatedStages));

            Version = version;
            CatalogVersion = catalogVersion.Trim();
            EventId = eventId.Trim();
            BaseChanceBasisPoints = baseChanceBasisPoints;
            PetMultiplierBasisPoints = petMultiplierBasisPoints;
            GeneratedStages = stages;
            _generatedStages = new HashSet<int>(stages);
        }

        public int Version { get; }
        public string CatalogVersion { get; }
        public string EventId { get; }
        public int BaseChanceBasisPoints { get; }
        public int PetMultiplierBasisPoints { get; }
        public IReadOnlyList<int> GeneratedStages { get; }
        public bool Contains(StageId stage) => _generatedStages.Contains(stage.Value);
    }

    public static class EventScheduleGenerator
    {
        public const int StagesPerBlock = 20;
        public const int MaxEventsPerBlock = 5;

        public static EventScheduleSnapshot Create(
            string runId,
            StageMapData map,
            int petMultiplierBasisPoints = 10000)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("Run ID is required.", nameof(runId));
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (petMultiplierBasisPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(petMultiplierBasisPoints));

            EventData[] challenges = map.ChanceEvents
                .Where(value => value.Type == EventStageType.ChallengeMonster)
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ToArray();
            if (challenges.Length == 0)
                throw new InvalidOperationException("At least one chance-based Challenge Monster Event is required.");

            int petLuckBasisPoints = Math.Max(0, petMultiplierBasisPoints - 10000);
            int totalLuckBasisPoints = checked(map.EventEncounterLuckBasisPoints + petLuckBasisPoints);

            var generated = new List<int>();
            for (int blockStart = StageId.First;
                 blockStart <= StageId.Final;
                 blockStart += StagesPerBlock)
            {
                int blockEnd = Math.Min(StageId.Final, blockStart + StagesPerBlock - 1);
                int fixedChallengeCount = 0;
                var eligible = new List<int>();
                for (int stage = blockStart; stage <= blockEnd; stage++)
                {
                    var stageId = new StageId(stage);
                    if (map.TryGetFixedEvent(stageId, out EventData fixedEvent))
                    {
                        if (fixedEvent.Type == EventStageType.ChallengeMonster)
                            fixedChallengeCount++;
                        continue;
                    }
                    // Fixed-monster pins are also ineligible for random event placement.
                    if (map.TryGetFixedMonster(stageId, out _)) continue;
                    if (!StageClassificationPolicy.IsProtected(stageId))
                        eligible.Add(stage);
                }

                if (fixedChallengeCount > MaxEventsPerBlock)
                    throw new InvalidOperationException(
                        $"Stages {blockStart}-{blockEnd} contain more than {MaxEventsPerBlock} fixed Challenge Events.");

                int blockChallengeCount = fixedChallengeCount;
                if (blockChallengeCount == 0 && eligible.Count > 0)
                {
                    generated.Add(TakeStage(eligible, runId, map.CatalogVersion,
                        blockStart, "guaranteed"));
                    blockChallengeCount++;
                }

                int guaranteedBonus = totalLuckBasisPoints / 10000;
                for (int i = 0; i < guaranteedBonus && blockChallengeCount < MaxEventsPerBlock && eligible.Count > 0; i++)
                {
                    generated.Add(TakeStage(eligible, runId, map.CatalogVersion,
                        blockStart, $"bonus-guaranteed-{i + 1}"));
                    blockChallengeCount++;
                }

                int remainderChance = totalLuckBasisPoints % 10000;
                if (blockChallengeCount < MaxEventsPerBlock && eligible.Count > 0 && remainderChance > 0)
                {
                    ulong roll = StableHash64.Compute(runId, map.CatalogVersion,
                        blockStart.ToString(), "event-bonus-roll") % 10000UL;
                    if (roll < (ulong)remainderChance)
                    {
                        generated.Add(TakeStage(eligible, runId, map.CatalogVersion,
                            blockStart, "bonus"));
                        blockChallengeCount++;
                    }
                }
            }

            // EventId remains a valid pool anchor for persisted v1 schedules.
            // The resolver deterministically selects the actual Event per Stage.
            return new EventScheduleSnapshot(map.CatalogVersion, challenges[0].Id,
                generated, map.EventEncounterLuckBasisPoints, petMultiplierBasisPoints);
        }

        private static int TakeStage(List<int> eligible, string runId,
            string catalogVersion, int blockStart, string purpose)
        {
            if (eligible.Count == 0)
                throw new InvalidOperationException(
                    $"The Stage block beginning at {blockStart} has no eligible Event Stage.");
            ulong hash = StableHash64.Compute(runId, catalogVersion,
                blockStart.ToString(), purpose);
            int index = (int)(hash % (ulong)eligible.Count);
            int stage = eligible[index];
            eligible.RemoveAt(index);
            return stage;
        }
    }

    public static class EventScheduleRefreshPolicy
    {
        public static EventScheduleSnapshot PreserveReachedStages(
            StageMapData map,
            EventScheduleSnapshot current,
            EventScheduleSnapshot refreshed,
            StageId currentStage)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (current == null) return refreshed ?? throw new ArgumentNullException(nameof(refreshed));
            if (refreshed == null) throw new ArgumentNullException(nameof(refreshed));
            if (!string.Equals(current.CatalogVersion, refreshed.CatalogVersion,
                    StringComparison.Ordinal) ||
                !string.Equals(map.CatalogVersion, refreshed.CatalogVersion,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Event schedules must use the active Stage Map catalog.");

            var merged = new List<int>();
            for (int blockStart = StageId.First;
                 blockStart <= StageId.Final;
                 blockStart += EventScheduleGenerator.StagesPerBlock)
            {
                int blockEnd = Math.Min(
                    StageId.Final,
                    blockStart + EventScheduleGenerator.StagesPerBlock - 1);
                int fixedCount = map.FixedEventStages.Count(stage =>
                    stage >= blockStart && stage <= blockEnd &&
                    map.TryGetFixedEvent(new StageId(stage), out EventData fixedEvent) &&
                    fixedEvent.Type == EventStageType.ChallengeMonster);
                int capacity = Math.Max(0, EventScheduleGenerator.MaxEventsPerBlock - fixedCount);

                int[] preserved = current.GeneratedStages
                    .Where(stage => stage >= blockStart && stage <= blockEnd &&
                        stage <= currentStage.Value)
                    .OrderBy(stage => stage)
                    .Take(capacity)
                    .ToArray();
                merged.AddRange(preserved);
                capacity -= preserved.Length;
                if (capacity <= 0) continue;

                merged.AddRange(refreshed.GeneratedStages
                    .Where(stage => stage >= blockStart && stage <= blockEnd &&
                        stage > currentStage.Value && !merged.Contains(stage))
                    .OrderBy(stage => stage)
                    .Take(capacity));
            }

            return new EventScheduleSnapshot(
                refreshed.CatalogVersion,
                refreshed.EventId,
                merged,
                refreshed.BaseChanceBasisPoints,
                refreshed.PetMultiplierBasisPoints,
                refreshed.Version);
        }
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
        private EventScheduleSnapshot _schedule;
        public StageEncounterResolver(StageMapData map, EventScheduleSnapshot schedule = null)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _schedule = schedule;
            if (_schedule != null)
            {
                if (!string.Equals(_schedule.CatalogVersion, map.CatalogVersion, StringComparison.Ordinal) ||
                    !map.TryGetEventById(_schedule.EventId, out _))
                    throw new ArgumentException("Saved Event schedule does not match the Stage Map catalog.", nameof(schedule));
            }
        }

        public EventScheduleSnapshot Schedule => _schedule;

        public void ReplaceSchedule(EventScheduleSnapshot schedule)
        {
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));
            if (!string.Equals(schedule.CatalogVersion, _map.CatalogVersion,
                    StringComparison.Ordinal) ||
                !_map.TryGetEventById(schedule.EventId, out _))
                throw new ArgumentException(
                    "Event schedule does not match the Stage Map catalog.",
                    nameof(schedule));
            _schedule = schedule;
        }

        public EncounterSelection Resolve(string runId, StageId stage)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run ID is required.", nameof(runId));
            BiomeData biome = _map.GetBiome(stage);
            bool isFinalBiome = _map.Biomes != null && _map.Biomes.Count > 0 &&
                _map.Biomes[_map.Biomes.Count - 1] == biome;
            StageEncounterKind kind = StageClassificationPolicy.Classify(stage, biome.LastStage, isFinalBiome);
            EventData eventData = null;
            if (kind == StageEncounterKind.NormalMonster &&
                (!_map.TryGetFixedEvent(stage, out eventData) &&
                 !(_schedule != null && _schedule.Contains(stage) &&
                   TryResolveScheduledEvent(runId, stage, out eventData))))
                eventData = null;
            if (eventData != null)
                return new EncounterSelection(stage, biome.Id, biome.ResolveTitle(stage), StageEncounterKind.ChallengeEvent,
                    eventData.Id, eventData.Title, 1, 0,
                    eventData.Type == EventStageType.ChallengeMonster
                        ? EventQuestionCatalog.DefaultDocumentId
                        : string.Empty);

            MonsterData monster;
            if (kind == StageEncounterKind.NormalMonster)
            {
                // 1. Authored pin: fixed stage binding overrides the random pool.
                if (!_map.TryGetFixedMonster(stage, out monster))
                {
                    // 2. Hash-pick from the biome's normal monster pool.
                    ulong hash = StableHash64.Compute(runId, _map.CatalogVersion, stage.Value.ToString(), "monster");
                    monster = biome.NormalMonsters[(int)(hash % (ulong)biome.NormalMonsters.Count)];
                }
            }
            else if (!biome.TryGetBoss(stage, out monster))
                throw new InvalidOperationException($"Stage {stage.Value} has no {kind} binding.");
            if (monster.Kind != kind) throw new InvalidOperationException("Boss binding class does not match Stage classification.");
            int hp = StageHpPolicy.Calculate(_map, monster, runId, stage);
            return new EncounterSelection(stage, biome.Id, biome.ResolveTitle(stage), kind, monster.Id, monster.Name,
                hp, monster.MaximumCooldown);
        }

        private bool TryResolveScheduledEvent(
            string runId,
            StageId stage,
            out EventData eventData)
        {
            EventData[] pool = _map.ChanceEvents
                .Where(value => value.Type == EventStageType.ChallengeMonster)
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ToArray();
            if (pool.Length == 0)
            {
                eventData = null;
                return false;
            }

            ulong hash = StableHash64.Compute(
                runId,
                _map.CatalogVersion,
                stage.Value.ToString(),
                "challenge-event");
            eventData = pool[(int)(hash % (ulong)pool.Length)];
            return true;
        }
    }

    public static class StageHpPolicy
    {
        public static long CalculateGrowthBasisPoints(
            int worldLevel,
            int phase1GrowthBasisPoints = 1500,
            int phase2GrowthBasisPoints = 3500,
            int phase3GrowthBasisPoints = 8000,
            int phase4GrowthBasisPoints = 15000)
        {
            if (worldLevel <= 12)
            {
                // Phase 1: Early Game (WL 1-12, Stages 1-60): Standard growth (+15% per WorldLevel by default)
                return 10000L + (long)(worldLevel - 1) * phase1GrowthBasisPoints;
            }
            long baseEarly = 10000L + 11L * phase1GrowthBasisPoints;
            if (worldLevel <= 24)
            {
                // Phase 2: Mid Game (WL 13-24, Stages 61-120): Intermediate acceleration (+35% per WorldLevel by default)
                return baseEarly + (long)(worldLevel - 12) * phase2GrowthBasisPoints;
            }
            long baseMid = baseEarly + 12L * phase2GrowthBasisPoints;
            if (worldLevel <= 30)
            {
                // Phase 3: Transition Bridge (WL 25-30, Stages 121-150): Step-up ramp (+80% per WorldLevel by default)
                return baseMid + (long)(worldLevel - 24) * phase3GrowthBasisPoints;
            }
            // Phase 4: Late Game (WL 31-40, Stages 151-200): Data-driven endgame growth (+150% or custom per WorldLevel)
            long baseBridge = baseMid + 6L * phase3GrowthBasisPoints;
            return baseBridge + (long)(worldLevel - 30) * phase4GrowthBasisPoints;
        }

        public static int Calculate(StageMapData map, MonsterData monster, string runId, StageId stage)
        {
            long growth = CalculateGrowthBasisPoints(
                stage.WorldLevel,
                map.Phase1GrowthBasisPoints,
                map.Phase2GrowthBasisPoints,
                map.Phase3GrowthBasisPoints,
                map.Phase4GrowthBasisPoints);
            ulong hash = StableHash64.Compute(runId, map.CatalogVersion, stage.Value.ToString(), "hp");
            int span = map.VariationBasisPoints * 2 + 1;
            long variation = 10000L - map.VariationBasisPoints + (long)(hash % (ulong)span);
            long baseline = Math.Max(1, (long)monster.BaseHp);
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
                if (value == null) continue;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }
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
                        : kind == StageEncounterKind.BigBoss ? 100 + biomeIndex * 50 : 5000;
                    int bossCooldown = (biomeIndex < 2 || kind == StageEncounterKind.MiniBoss) ? 3 : 2;
                    bosses.Add(stage, new MonsterData($"boss-{stage}",
                        kind == StageEncounterKind.FinalBoss ? "Final Boss" : $"{kind} {stage}",
                        kind, biomeId, bossCooldown, bossBaseHp, 10000));
                }
                biomes.Add(new BiomeData(biomeId, $"Biome {biomeIndex + 1}",
                    first, last, normals, bosses));
            }
            var challenge = new EventData("challenge-monster", "Challenge Monster",
                EventStageType.ChallengeMonster);
            return new StageMapData("development-stage-map-v1", 1200, 500,
                biomes, new Dictionary<int, EventData>
                {
                    { 7, challenge }
                }, new[] { challenge }, 0,
                null, 3500, 8000, 20000);
        }
    }
}
