---
slug: firebase-gamedata-phase-1-migration
status: implemented-awaiting-human-review
source: manual
gdd_tags:
  - server-authority
  - pet-system
  - gacha
owner: implementation-agent
human_checkpoint: required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: Firebase Gamedata Phase 1 Migration

## Goal

Make the existing per-player Firestore save path reliable before introducing or deleting schema fields. Persisted gameplay state must survive authentication/bootstrap, schema upgrades must commit their transformations atomically with the version marker, and the checked-in Firebase contract must describe the paths used by the client.

## Current Failure

- The client reads `level-1/{username}`, `level-2/{username}`, and `level-3/{username}`, while the checked-in rules and README still describe shared `competition*` documents.
- Gameplay writers persist current audit, event, challenge, passive-runtime, and gacha receipt fields that `FirestoreRestClient.TryMapPlayer` does not restore.
- `PlayerDefaultsPlanner` advances `schemaVersion` without persisting the V5-to-V6 cooldown transformation.
- The serialized settings asset still contains superseded shared-document fields and maps cohorts to Grade 1-3 instead of the GDD-approved Grade 4-6.

## Scope

- Restore every currently persisted `PlayerSnapshot` field during bootstrap.
- Add safe missing defaults for the current active-run/economy schema.
- Persist the V5-to-V6 transformation in the same Firestore patch that advances the schema version.
- Add regression coverage for mapper round trips and migration patch behavior.
- Align `Firebase/firestore.rules`, `Firebase/README.md`, and `GameApiSettings.asset` with the per-player path already used by the client.
- Preserve the remaining legacy `competition` rule and all existing player fields for rollback compatibility. `competition-2` is excluded because it has been migrated and deleted.

## Out of Scope

- Publishing Firestore rules or modifying live Firebase data.
- Deleting or renaming legacy fields.
- Introducing the proposed `petCollection` canonical map.
- Firebase Authentication, trusted backend deployment, dependencies, CI/build settings, secrets, or PR merge.

## Acceptance Criteria

- [x] Bootstrap restores audit correctness, event schedule, challenge cursor/receipt, passive progress, question content ID, and full gacha results.
- [x] A V5 committed Normal Monster save writes the cooldown correction and schema version atomically.
- [x] Retrying after a successful migration cannot apply the correction twice.
- [x] Existing unknown fields and the remaining legacy `competition` path remain untouched; confirmed-deleted `competition-2` is no longer referenced.
- [x] Checked-in rules cover current per-player paths but are not deployed.
- [x] Runtime cohorts resolve to the GDD-approved Grade 4-6 labels.
- [x] Focused tests pass with no new compiler errors.

## Router Decision

- Workflow: `/fix-bug`
- Current artifact: this approved task card
- Next agent: implementation-agent
- Human checkpoint: required before rule publication or PR merge

## Context Pack

- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md` at `@tag:pet-system`, `@tag:gacha`, and `@tag:server-authority`.
- `Docs_PowerMath/3_Outputs/ADRs/016-player-lifecycle-and-live-service-boundaries.md`
- `Docs_PowerMath/3_Outputs/ADRs/021-data-driven-pet-collection-passive-runtime.md`
- `Docs_PowerMath/3_Outputs/Specs/pet-collection-passive-runtime-standardization-arch-plan.md`
