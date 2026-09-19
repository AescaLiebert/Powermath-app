---
slug: "waypoint-and-response-multiplier"
status: in-progress
source: manual
gdd_tags:
  - "answer-scoring"
  - "combat-stats"
  - "stage-progression"
  - "waypoint-system"
  - "run-reset"
  - "economy"
owner: "antigravity"
human_checkpoint: approved
next_agent: implementation-agent
blocked_by: []
---

# Task Card: Response Damage Multiplier Refactor & Waypoint System GDD Specification

## Player-Facing Goal

1. Answering math questions correctly will always deal at least 100% base Effective ATK damage, eliminating the penalty on careful students while granting bonus damage up to 200% for rapid responses.
2. After Rebirth, players can interact with a mysterious Cat Witch Girl in the Biome Map window to select an eligible Biome and teleport directly there for free (one-time per run). Unlocking a Waypoint to Biome N requires having defeated the closing Big Boss of Biome N+1 (e.g. defeating Stage 90 Biome 3 Big Boss to skip Biome 1 to Stage 31).
3. The system enforces a strict "No Free Reward" policy: skipped stages grant 0 Flat Power Coins and 0 Legacy ATK, ensuring economy integrity against suicide-farming exploits while preserving the minimum Rebirth threshold of Stage 30.

## Source

- Origin: User design request and friction gameplay feedback
- Requested by: Product Owner / Lead Designer

## GDD Reference

- `@tag:answer-scoring` - Response Damage Multiplier scaled from 100% to 200%.
- `@tag:combat-stats` - Effective ATK scaling and final damage bounds.
- `@tag:waypoint-system` - Cat Witch Girl Biome teleport, once per run, No Free Reward policy.
- `@tag:stage-progression` - Biome Map window interaction and World Level escalation.
- `@tag:run-reset` & `@tag:economy` - `StagesClearedThisRun` vs `StageReached` reward separation.

## Type

- [x] Feature
- [x] Refactor

## Scope

### Systems Affected

- Combat Core (`ResponseDamagePolicy`, `DamageCalculator`, `CombatModels`)
- Unit Tests (`CombatCoreTests`)
- Canonical GDD (`GDD_PowerMathProject.md`)

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Combat/Core/DamageCalculator.cs`
- `Assets/Project/Tests/EditMode/Combat/CombatCoreTests.cs`
- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`

### Out of Scope

- Client UI animation/prefab implementation of the Cat Witch Girl (specification in GDD first).
- Server database schema migration for Waypoint telemetry (covered under future sprint task).

## Acceptance Criteria

- [x] `ResponseDamagePolicy.GetPercent` maps Scores 1..10 to 100%, 110%, 120%, 130%, 140%, 150%, 160%, 170%, 180%, 200%.
- [x] `ResponseDamagePolicy.GetMultiplier` maps Scores 1..10 to 1.0x..2.0x.
- [x] Unit tests in `CombatCoreTests.cs` pass with the new multiplier scale.
- [x] Canonical GDD updated with the new Response Multiplier formula and table.
- [x] Canonical GDD updated with `@tag:waypoint-system` detailing Cat Witch Girl, map integration, single-use per run, and "No Free Reward" economy protection.
- [x] Follows `RULES_AND_POLICY.md`.

## Human Checkpoints

- [x] Design/game-feel approval (Approved via review policy)
- [x] Architecture approval (Approved via review policy)
- [ ] PR review before merge
