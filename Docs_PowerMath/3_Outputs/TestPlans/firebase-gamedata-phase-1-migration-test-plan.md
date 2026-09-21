# Firebase gamedata Phase 1 migration test plan

Date: 2026-09-21

Scope: non-destructive client compatibility and checked-in Firestore contract.
Do not publish rules, delete legacy documents, or mutate production player data
while executing this plan.

## Automated EditMode regression

Run these tests from `PlayerLifecycleTests`:

1. `GameplayReceiptsAndCountersRoundTripFromFirestore`
2. `V5CommittedNormalEncounter_RestoresItsPrematurelySpentCooldown`
3. `V5CooldownCorrectionIsPersistedWithSchemaUpgrade`
4. `DefaultsDoNotOverwriteExistingWalletOrUnknownFeatureData`
5. `NullStringFieldInPlayerDataIsRepairedSafely`
6. `TutorialMigrationAndDefaultsCreateAnEmptyDynamicMap`

Expected results:

- Event schedule, Challenge sequence/receipt, passive counters,
  `questionContentId`, audit correctness, and Pet Gacha result rows are restored.
- A V5 committed Normal Monster save patches cooldown `1` to `2`, schema version
  `5` to `6`, and revision `7` to `8` in one patch plan.
- Planning the same V6 save does not patch the cooldown again.
- Existing wallet values and unknown feature maps are not included in the repair
  mask.
- Firestore `nullValue` string leaves are repaired with the documented default.

## Rules verification before publication

Use the Firebase Rules emulator or a disposable Firebase test project. Do not
validate these cases against production player documents.

1. GET `level-1/{username}`, `level-2/{username}`, and `level-3/{username}`;
   expect allow.
2. LIST any level collection; expect deny.
3. UPDATE only the existing top-level `gamedata` map; expect allow.
4. UPDATE `userdata`, add a top-level field, or remove a top-level field; expect
   deny.
5. CREATE or DELETE a player document; expect deny.
6. GET each Rank and Challenge question document; expect allow.
7. GET an unlisted question document or attempt to write question data; expect
   deny.
8. Confirm the remaining `competition` and `leaderboard-public` compatibility
   cases remain unchanged. Confirm `competition-2` is absent and returns deny.

## Staging save compatibility

Use copied/anonymized fixtures or a disposable player account.

1. Back up the source document and record its Firestore `updateTime`.
2. Sign in through each configured cohort and verify Grade 4, Grade 5, and Grade
   6 resolution.
3. Load a current V6 save containing Event, Challenge, passive, audit, and
   multi-pull Gacha state; verify the next save preserves those values.
4. Load a V5 committed Normal Monster fixture; verify the first bootstrap writes
   the cooldown correction with schema/revision, then the second bootstrap is
   idempotent.
5. Include an unknown sibling map under `gamedata`; verify it survives default
   repair and gameplay persistence.
6. Force an optimistic-concurrency conflict; verify the client reloads rather
   than overwriting the newer document.
7. Reset a disposable account; verify passive counters, teleport state, and
   first-pull state return to defaults while `userdata` remains unchanged.

## Rollback check

If staging validation fails, restore the backed-up document, revert the client
build, and leave the legacy collections/rules in place. Phase 1 contains no
automatic deletion or irreversible schema conversion.
