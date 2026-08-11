# DevLog: Stage Map, Biomes, Enemies, and Events

Date: 2026-08-11  
Status: Implementation complete; owner E2E and final art/UI brief pending.

## Delivered

- Added immutable seven-biome Stage Map domain data, protected-Stage classification, deterministic normal-monster selection, and generated Stage HP with boss multipliers.
- Added `StageMapDefinition`, `BiomeDefinition`, expanded `EnemyDefinition`, and independent `EventDefinition` ScriptableObject authoring/validation.
- Replaced the one-enemy runtime path with `LocalRunEncounterEngine`, including encounter changes, boss cadence, biome changes, Challenge Event retry/heart-loss behavior, and saved restoration validation.
- Split attempt results into Rank and Event question content. Event questions load from their own Firestore document and never change Rank currency or the five-question audit.
- Added canonical biome, encounter, and Event-attempt fields to player snapshots, default migration, Firestore load/save, and death/rebirth cleanup.
- Added a seven-region informational world map, current-biome marker, Event-specific combat messaging, and biome title/background transition hook.
- Added a complete development fallback Stage Map and Stage 7 Challenge so the feature remains inspectable before production content assets are authored.

## Verification

- Unity 6000.5.3f1 full asset import and Tundra assembly compilation succeeded.
- Standalone Academic.Core, Combat.Core, Combat.Unity, and Assembly-CSharp compiler passes succeeded.
- Automated tests were not run or authored per product-owner direction; owner will perform E2E.

## Content Handoff

- Assign a production `StageMapDefinition` to `CombatLobbyCompositionRoot` when biome names, backgrounds, monster/boss sprites, and fixed Event Stages are ready.
- The live question project must contain the Event document ID referenced by each `EventDefinition` (the fallback Challenge uses `challenge`) with the existing `items[{id, video_link, answer}]` schema.
- Final pseudo-map, transition motion, and encounter UI polish remain intentionally pending the owner's UI brief.
- Firebase rules were not published, and build/CI settings were not changed.
