# DevLog — Firebase gamedata Phase 1 migration

Date: 2026-09-21

GDD references: `@tag:server-authority`, `@tag:pet-system`, `@tag:gacha`.

## Outcome

Implemented the non-destructive first migration phase for the existing direct
Firestore prototype. No live Firebase data or rules were changed.

## Changes

1. Restored read/write parity in `FirestoreRestClient` for Event scheduling,
   Challenge sequencing and reward receipts, passive runtime counters,
   `questionContentId`, `auditCorrectCount`, and complete Pet Gacha result rows.
2. Added null-safe collections in `PlayerSchemaMigrator` so missing schedule,
   Challenge, and Gacha arrays/maps cannot remain null after bootstrap.
3. Made the V5-to-V6 committed Normal Monster cooldown correction durable by
   including it in the same masked patch as the schema-version and revision
   update. A V6 retry does not apply the correction again.
4. Extended additive defaults and account-reset payloads for the restored fields,
   including passive counters, teleport state, and first-Gacha-pull state.
5. Repaired Firestore `nullValue` string leaves with their safe defaults while
   continuing to reject incompatible non-null types.
6. Aligned `GameApiSettings.asset` with the active per-player collections and
   Grade 4-6 bands, removing superseded serialized shared-document fields.
7. Updated checked-in Firestore rules and setup documentation for the active
   `level-{n}/{username}` layout, while retaining legacy collection rules for
   rollback compatibility and adding Challenge question reads.
8. Added focused EditMode regression coverage and a staging/rules test plan.

## Safety and rollout

- Firestore rules were not published.
- Production documents were not read, written, renamed, or deleted.
- The remaining legacy `competition` rule is retained for rollback; the user
  confirmed that `competition-2` was migrated and deleted, so its rule is gone.
- Rule publication and any later data cleanup remain human checkpoints.

## Verification

- Unity 6 compiled `Assembly-CSharp` and `Assembly-CSharp-Editor` without C#
  compiler errors after the migration changes.
- All six focused `PlayerLifecycleTests` checks passed from the freshly compiled
  assemblies.
- Focused tests are listed in
  `Docs_PowerMath/3_Outputs/TestPlans/firebase-gamedata-phase-1-migration-test-plan.md`.
