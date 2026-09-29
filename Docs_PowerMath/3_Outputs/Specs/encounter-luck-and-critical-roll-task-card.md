---
slug: encounter-luck-and-critical-roll
status: needs-human
source: manual
gdd_tags:
  - combat-stats
  - encounters
owner: project-owner
human_checkpoint: required
next_agent: human
blocked_by:
  - implementation-review
---

# Task Card: Encounter Luck and Critical Roll Audit

## Player-Facing Goal

Encounter Luck schedules the promised number of Event Stages per 20-Stage block, displayed Critical Rate matches the chance used for each successful player hit, and Editor Play Mode uses the same real Firebase test account path as a deployed build.

## Source

- Origin: Direct prompt on 2026-09-29
- Requested by: project owner
- Expected Encounter Luck: each 100% guarantees one Event Stage; remaining percentage is a chance for one more; cap total Luck at 500%.
- Follow-up: deployed WebGL is a real Firebase test account; remove the Editor sample-student identity and fixed development RNG seed.

## GDD Reference

- `@tag:combat-stats` - CR is clamped from 0% to 100%.
- `@tag:encounters` - Event Stages are scheduled within 20-Stage blocks and protected boss Stages are excluded.

## Type

- [x] Bug fix
- [ ] Feature
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- 20-Stage Event schedule generation and refresh for active runs.
- Critical-rate composition and local combat roll path audit.
- Runtime RNG seed generation for real runs.
- Editor authentication, bootstrap, reset, lifecycle, and profile paths.

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Combat/Core/StageMapModels.cs`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
- `Assets/Project/Script/Gameplay/Combat/Core/LocalRunEncounterEngine.cs`
- `Assets/Project/Script/Gameplay/Progression/PlayerStatProjection.cs`
- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`

### Out of Scope

- Changing critical damage or pet follow-up eligibility.
- Removing the separate offline question presentation fallback.
- Publishing or deploying a build, changing CI/build settings, or merging.

## Acceptance Criteria

- [x] For each 20-Stage block, every complete 100% of total Encounter Luck schedules one guaranteed eligible Event Stage.
- [x] A remaining percentage is one deterministic bonus roll for one additional Event Stage; 210% means two guarantees and a 10% chance for a third.
- [x] Total Encounter Luck is capped at 500%; authored fixed Events retain priority and count toward the block quota.
- [x] Protected Stages remain ineligible for generated Events.
- [x] Restored runs retain already reached Events and refresh unvisited Events under the current schedule rule.
- [x] Critical Rate remains clamped to 0–100% and is applied once to the primary player hit roll.
- [x] Existing pet follow-up critical behavior remains separately governed by its passive.
- [x] Combat RNG no longer uses an authorable fixed seed; runtime sessions receive a fresh seed.
- [x] Editor login, bootstrap, logout, reset, lifecycle, and leaderboard paths use direct Firebase services; sample student services are removed.
- [x] Follows `RULES_AND_POLICY.md`.

## Human Checkpoints

- [x] Owner supplied the intended Encounter Luck rule in the source prompt.
- [ ] Review implementation before merge.

## Router Decision

Workflow: `/fix-bug`

The overcount is in the schedule generator. The primary critical calculation uses one roll against the composed rate; the deployed WebGL runtime was loading deterministic seed `1337`, which repeated its sequence across identical action sequences.
