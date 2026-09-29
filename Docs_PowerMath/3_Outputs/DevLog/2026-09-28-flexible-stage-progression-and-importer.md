# Flexible Stage Progression, Dynamic StageId.Final, and Enemy Importer

- **Date:** 2026-09-28
- **Category:** Combat / Progression / Editor Tooling / Persistence

## Context

The combat system previously enforced a rigid 7-biome requirement (`biomes.Length == 7` / `biomes.Count == 7`), failing with an error when the StageMap was expanded to 9 biomes (stages 1 to 280). In addition, `StageId.Final` was a compile-time constant (`215`), which blocked dynamic map lengths, and progression milestone tracking in Firebase was locked to `firstStage200Reached` rather than detecting when all stage content has been completed.

## Changes Made

### 1. Flexible Biome Count & Dynamic `StageId.Final`
- Refactored `StageId.Final` from a compile-time constant to a dynamic static property (`public static int Final { get; set; } = 280;`).
- Relaxed validation in `StageMapDefinition.cs` and `StageMapModels.cs` to allow any biome count (`>= 1`).
- Dynamically sets `StageId.Final` to the last biome's `LastStage` when `StageMapDefinition` or `StageMapData` is instantiated or loaded.
- Added `FinalStage` property to `StageMapData` and updated `EventScheduleGenerator` block loops to use `map.FinalStage`.
- Updated `MainMenuViewModel.FinalStage` to dynamically read `StageId.Final`.

### 2. Firestore Progression & Milestone Tracking
- Added `finalStageReached` and `finalStageReachedAtUnixSeconds` to `PlayerSnapshot.ProgressionData`.
- Added baseline default initialization in `PlayerSchemaMigrator`, `PlayerDefaultsPlanner`, and reset routines (`FirestorePlayerResetService`, `PlayerResetPayloadBuilder`).
- Updated `FirestoreAcademicProgressionStore` to latch `finalStageReached` and record `finalStageReachedAtUnixSeconds` when `_highestStage >= StageId.Final` or `combat.Phase == CombatPhase.RunComplete` (when `_stage.TryNext(out _)` has no subsequent stage content).
- Maintained full backwards compatibility:
  - Dual-writing: Writes both `finalStageReached` and `firstStage200Reached`.
  - Dual-reading: In `FirestoreRestClient`, `finalStageReached` defaults to `firstStage200Reached` if absent on legacy documents.
- Updated `FirestoreAdminTuningService` and `FirestoreLeaderboardProjectionPublisher` to track and publish `finalStageReachedAtUnixSeconds`.
- Updated `ProfileAnalyticsRuntime` to display the `Final Stage` timestamp, falling back to `firstStage200ReachedAtUnixSeconds`.

### 3. Flexible `EnemyDatabaseImporter` Tool
- Removed hardcoded biome index limit (`row.BiomeNumber <= 7`) in `FindBestMatch`.
- Dynamically discovers all `BiomeDefinition` assets in the project, sorts them by `FirstStage`, and assigns `isFinalBiome` dynamically to the last biome (Biome 9, Stage 280).
- Dynamically updates `StageId.Final` during import.
- Verified all boss bindings across stages 1 to 280:
  - Each biome has valid boss bindings for intermediate `% 5` stages (`MiniBoss`).
  - Each intermediate biome's last stage is `BigBoss`.
  - The final biome's last stage (Biome 9 Stage 280) is `FinalBoss` (`B7-B01_Astral-Judgement`).

### 4. Tests
- Added unit tests in `CombatCoreTests.cs` verifying flexible biome count handling (e.g. 3 biomes ending at stage 50) and dynamic `StageId.Final` updates.
- Added tests in `AdminResetTests.cs` and `VersionAndMigrationTests.cs` verifying default persistence planning for `finalStageReached`.
- Updated test event schedule loops to check up to `map.FinalStage`.
