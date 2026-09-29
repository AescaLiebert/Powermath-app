---
slug: flexible-stage-progression-and-importer
status: ready-for-review
source: manual
gdd_tags:
  - core-loop
  - stage-progression
  - server-authority
  - encounters
owner: implementation-agent
human_checkpoint: required
next_agent: code-review-agent
blocked_by: []
---

# Flexible Stage Progression, Dynamic StageId.Final, and Enemy Importer

## Required behavior

- StageMapDefinition and StageMapModels support flexible biome counts (any count >= 1; remove hardcoded 7-biome limit).
- StageId.Final is dynamic, initialized from the highest stage (LastStage) of the active StageMap (defaults to 280 for current 9 biomes).
- Biomes must be continuous without gaps (first biome starts at Stage 1, each subsequent starts at previous.LastStage + 1, and the last biome ends at StageId.Final).
- Progression persistence updates milestone tracking:
  - Add `finalStageReached` and `finalStageReachedAtUnixSeconds` to `PlayerSnapshot.ProgressionData` and persistence pipelines.
  - Latched when the final stage is cleared and `_stage.TryNext(out _)` returns false (`CombatPhase.RunComplete` or `stage >= StageId.Final`).
  - Read legacy `firstStage200Reached` as backwards-compatible fallback.
- `EnemyDatabaseImporter` dynamically discovers all BiomeXX assets without hardcoded 1-7 limits and dynamically determines `isFinalBiome` based on the highest LastStage biome.

## Acceptance

- [x] All unit and edit mode tests pass.
- [x] Unity runtime compiles cleanly without errors.
- [x] Stage Map resolves 9 biomes up to stage 280 without "Stage Map requires exactly seven biomes" exception.
- [x] EnemyDatabaseImporter matches and links enemies across all biomes.
- [x] Persistence writes `finalStageReached` and preserves existing player state.
