---
slug: event-stage-challenge-migration
status: needs-human
source: manual
gdd_tags:
  - core-loop
  - question-data
  - stage-progression
  - encounters
  - pet-system
  - feedback
  - guardrails
  - playtest
owner: codex
human_checkpoint: required
next_agent: reviewer-agent
blocked_by:
  - implementation-review-and-manual-playtest
---

# Task Card: Event Stage and Challenge Migration

## Player-Facing Goal

Challenge Monsters should be occasional, clearly telegraphed one-trial encounters. Every 20-Stage block contains a guaranteed Challenge Monster, a pet-modified roll may add a bonus Challenge Monster, and either success or failure resolves the Stage. Failure makes the Challenge Monster flee without damaging the player. Every resolved Challenge grants an immediate, idempotent 10-20 Power Coin reward based on response efficiency.

## Source

- Origin: manual prompt on 2026-09-14
- Requested by: project owner
- Canonical design: `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`

## GDD Reference

- `@tag:core-loop` - a failed one-trial Challenge Monster flees and progression continues.
- `@tag:question-data` - ordinary Rank question IDs remain non-negative numeric IDs unique within Rank.
- `@tag:stage-progression` - Events replace only eligible Normal Candidate Stages.
- `@tag:encounters` - one guaranteed plus at most one pet-modified bonus Challenge per 20-Stage block; one trial; 10-20 PC reward.
- `@tag:pet-system` - Event chance modifiers are account-wide SSR collection passives.

## Type

- [x] Feature migration
- [ ] Behavior-preserving refactor
- [ ] Bug fix
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- Event and Stage Map authoring
- Per-run encounter scheduling and recovery
- Challenge question catalog, identity, and sequence
- Combat result state and presentation actions
- Atomic Firestore attempt persistence and Power Coin wallet reward
- Pet collection passive projection into a run

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Combat/Unity/EventDefinition.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/StageMapDefinition.cs`
- `Assets/Project/Script/Gameplay/Combat/Core/StageMapModels.cs`
- `Assets/Project/Script/Gameplay/Combat/Core/LocalRunEncounterEngine.cs`
- `Assets/Project/Script/Gameplay/Combat/Core/LocalAttemptTransactionEngine.cs`
- `Assets/Project/Script/Gameplay/Academic/FirestoreEventQuestionCatalogRepository.cs`
- `Assets/Project/Script/Gameplay/Academic/FirestoreAcademicProgressionStore.cs`
- `Assets/Project/Script/PlayerData/PlayerSnapshot.cs`
- `Assets/Project/Script/Gameplay/Pets/Unity/PetDefinition.cs`

### Out of Scope

- Publishing Firebase rules or deploying a build
- Final Event, pet, UI, animation, or audio tuning
- Implementing Minigame runtime gameplay beyond its Event Stage type contract
- Changing ordinary Rank question IDs or Rank audit rules unless separately approved
- Merge, release, CI/build-setting, or dependency changes

## Acceptance Criteria

- [x] Each 20-Stage block has one saved guaranteed Challenge schedule entry on an eligible Normal Candidate Stage.
- [x] A single block-level bonus roll uses `eventEncounterLuckPercentage * petEncounterChanceMultiplier`, clamped to 0-100%, and can add no more than one generated Challenge.
- [x] Reload/reconnect cannot reroll a scheduled Event, selected challenge question, outcome, or reward.
- [x] A fixed Event binding retains priority and follows the approved cap rule.
- [x] `EventStageType` supports `ChallengeMonster` and `Minigame` without making Minigame inherit combat-only state.
- [x] `GameApiSettings` centrally maps `question/challenge-silver`, `question/challenge-gold`, and `question/challenge-diamond`; top-level `qN` items use `id`, `video-url`, and `answer`, with canonical matching IDs such as `cs1`, `cg1`, and `cd1`.
- [x] Challenge questions follow one shared FIFO per Rank across Event Definitions while remaining outside the five-question Rank audit.
- [x] One valid committed Challenge trial always resolves the Event Stage.
- [x] Correct clears the 1-HP target; incorrect/timeout consumes `Flee`, removes the target, advances the Stage, and removes no heart.
- [x] Reward is `10 + responseScore` for correct answers and `10` for failure, with response score clamped to 0-10.
- [x] Wallet mutation, Event resolution, next encounter, reward receipt, and attempt ID are saved atomically.
- [ ] Existing normal/boss combat, Rank question FIFO, presentation recovery, and run settlement remain unchanged after full regression and manual playtest.
- [ ] EditMode coverage is accepted after the full Unity Test Runner suite completes; focused scheduling, protected-stage, fixed-binding, ID/sequence, flee, reward, and transaction cases pass.
- [ ] Follows `RULES_AND_POLICY.md`.

## Human Checkpoints

- [x] Player-facing direction recorded in the canonical GDD on 2026-09-13.
- [x] Approved independent `EventDefinition` boundary on 2026-09-14.
- [x] Approved fixed Events counting inside the one-to-two block cap on 2026-09-14.
- [x] Approved separate per-Rank Challenge FIFO/reservation persistence on 2026-09-14.
- [ ] Review implementation before merge.
- [ ] Approve Firebase rule publication/deployment separately if required.

## Router Decision

Recommended workflow: `/implement-feature`

This request changes runtime behavior and persistence; it is not a behavior-preserving `/refactor`.

Next artifacts:

- `Docs_PowerMath/3_Outputs/Specs/event-stage-challenge-migration-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/event-stage-challenge-migration-arch-plan.md`
- `Docs_PowerMath/3_Outputs/ADRs/019-event-stage-scheduling-and-challenge-resolution.md`
- `Docs_PowerMath/3_Outputs/TestPlans/event-stage-challenge-migration-test-plan.md`
- `Docs_PowerMath/3_Outputs/DevLog/devlog-2026-09-14-event-stage-challenge-migration.md`
