---
slug: stage-map-biomes-enemies-events
status: e2e-pending
source: manual
gdd_tags:
  - core-loop
  - stage-progression
  - encounters
  - feedback
  - player-experience
  - guardrails
  - playtest
owner: game-design-agent
human_checkpoint: required
next_agent: product-owner
blocked_by: []
---

# Task Card: Stage Map, Biomes, Enemies, and Events

> Player-facing design approved by the product owner on 2026-08-11 (`LGTM`). Recommended fixed MVP Event scheduling, Rank-audit exclusion, and Challenge Monster heart-loss failure policy are accepted for architecture planning.

## Goal

Replace the single-enemy Stage prototype with a data-defined 200-Stage journey: seven biome regions, biome-specific normal monster pools and backgrounds, predictable Mini-Boss/Big-Boss/Final-Boss cadence, an informational pseudo-map, a readable biome-shift sequence, and extensible Event encounters that can replace only normal Stages.

## Current State

- `CombatLobbyCompositionRoot` supplies one serialized `EnemyDefinition` for the entire run.
- `EnemyDefinition` owns identity, Base HP, cooldown, and one sprite; it has no biome or encounter-class model.
- `LocalCombatEngine` respawns that same enemy and generates HP from its Base HP after every Stage advance.
- The GDD previously described four quarter-journey biome changes and generic fixed encounters, which conflicts with the new 30-Stage biome cadence and Event rules.
- The existing Stage 200 `RunComplete` rule remains valid and is preserved as the Final Boss override.
- **Flexible Biome Cadence (2026-09 Update)**: Biomes support flexible lengths (e.g., Biome 5: 121–160, Biome 6: 161–190, Biome 7: 191–215, totaling 215 stages). Boss classification is decoupled from `% 30` to biome-aware rules (`stage == biome.LastStage` $\rightarrow$ `BigBoss` / `FinalBoss`, intermediate `% 5 == 0` $\rightarrow$ `MiniBoss`). `StageId.Final` is updated to 215.

## Requested Scope

- Ordered `StageMapDefinition` route covering Stages 1-200.
- Seven biomes: six 30-Stage regions and one 20-Stage finale region.
- `BiomeDefinition` data for background, map landmark, normal monster pool, and boss bindings.
- Normal encounters except protected boss Stages; Event replacement only for normal candidates.
- Mini-Boss every Stage divisible by 5, Big Boss every Stage divisible by 30, Final Boss override at Stage 200.
- Normal monsters receive Stage-generated HP rather than authored per-monster HP.
- Mini/Big/Final bosses use fixed unique presentation and data-defined HP-spike modifiers.
- Informational pseudo-map showing all biomes and the student's current position without level selection/teleportation.
- Biome title/background/encounter transition sequence after Stages 30/60/90/120/150/180.
- Independent `EventDefinition` model with custom sprite, handler, question source, persistence, and reward/failure policies.
- Initial Challenge Monster Event with 1 HP and a harder question from a separate question document.

## Out of Scope

- Final biome/monster names, illustrations, animation, audio, backgrounds, or map composition polish.
- Final HP multipliers, Event frequency, Event rewards, or question content authoring.
- Slot Game/RNG Event implementation; only the extensible Event contract is designed now.
- Firebase rule publication, deployment, build settings, dependencies, automated tests, merge, or release.

## Human Decisions Needed

1. Approve seven ranges: `1-30`, `31-60`, `61-90`, `91-120`, `121-150`, `151-180`, `181-200`.
2. Approve Stage classification priority: Stage 200 Final Boss, then `%30` Big Boss, then `%5` Mini-Boss, then Normal/Event.
3. Confirm whether every protected boss Stage must bind a distinct boss definition/sprite, or whether a biome may reuse one Mini-Boss definition.
4. Approve the recommended Challenge Monster failure rule: incorrect/timeout costs one heart, leaves the 1-HP Event active, and death follows the normal run-settlement path.
5. Approve that Challenge Event questions do not affect the five-question Rank audit by default; Event Rank Currency/rewards must be explicitly configured.
6. Choose MVP Event scheduling: explicit fixed Stage bindings (recommended) or random eligible-Stage selection.

## Checkpoints

- [x] Approve amended GDD and player-facing design. Approved 2026-08-11 (`LGTM`).
- [x] Approve architecture, persistence schema, and ScriptableObject boundaries before implementation. Approved 2026-08-11 (`LGTM`).
- [x] Implement Stage classification, deterministic encounters, Stage HP, seven-biome fallback, ScriptableObject authoring, Event questions, saved encounter state, biome shift, and informational pseudo-map.
- [ ] Review final art/UI/audio brief before presentation polish.
- [ ] Owner performs E2E; no automated test scripts requested.
- [ ] Review before merge.
