# DevLog: 2026-08-28 — Data-Driven Combat Presentation and Game Juice

## Goal

Implement the approved receipt-driven combat presentation architecture: readable player-first action sequences, stable interaction gating, target-relative FCT, animated enemy action queues, and distinct recoverable Death/Rebirth flows.

## What I Did

- [x] Added versioned attempt and settlement presentation receipts with exact-ID acknowledgement and fail-closed recovery.
- [x] Added a Unity-free semantic plan builder, actor state policy, UI lifecycle state, recovery resolver, and readiness policy.
- [x] Added Player/Enemy presentation controllers, pooled TMP FCT anchored to the affected sprite, critical combat-root impulse, and Reduced Motion handling.
- [x] Added first-left queue arm/consume/cancel/reflow/initiate behavior with stable-state reporting.
- [x] Added shared scoped interaction gating and a UI shield across combat, navigation, questions, and terminal settlement.
- [x] Separated Death and Rebirth presentation while retaining the same authoritative settlement rules; Death is replayed after refresh until acknowledgement.
- [x] Added ScriptableObject tuning boundaries for actor/impact juice and FCT style/pooling.
- [x] Added EditMode contract coverage and a manual/automated QA plan.

## Key Decisions

- Accepted gameplay data remains authoritative; presentation consumes immutable receipts and never recalculates damage, rank, rewards, or settlement.
- Presentation completion is a persisted obligation identified by `presentationId`, so refresh cannot silently skip Death or unlock combat early.
- Player actions always precede target reaction and enemy action; the semantic plan is an ordered list so follow-up/counter actions can be appended later.
- Runtime-created world/FX roots preserve the user-edited scene while providing a safe impulse boundary and target-relative FCT compatibility path.
- Starting timing and motion values live in `CombatJuiceProfile` and `FloatingCombatTextStyle`; they remain provisional until human playtesting.

## Bugs Found

- [x] FCT used the player menu/current-attack UI location instead of the affected enemy sprite → replaced with a sprite-relative combat anchor and pooled TMP service.
- [x] Restored `PresentingResult` was normalized to Ready → preserved the phase and added receipt recovery before completion.
- [x] Actor `OnDisable` could call `SetActive(false)` while already disabling → changed to direct deterministic cleanup.
- [x] Queue initiate classes were removed in the same call they were created → scheduled the transition and kept the queue unstable until it settles.

## Verification

- Combat Core, Presentation Core, and the full Combat Unity assembly compile with zero errors.
- EditMode test sources compile with zero errors against the newly built Core and Presentation Core assemblies.
- `git diff --check` is used for whitespace validation.
- Unity test execution and visual playtesting are pending because the currently open Editor owns the project lock and is in Play Mode.

## Game Feel Notes

No human feel claim is made yet. The approved starting values prioritize response and clarity: player attack around 0.55 s, critical impulse 0.18 s, FCT Pop/Hold/Exit 0.12/0.24/0.34 s, and Death 0.90 s. Validate FCT readability, queue cadence, impulse intensity, Death weight, Rebirth clarity, and Reduced Motion on target layouts before tuning.

## Next Session

- Exit Play Mode, run all EditMode/PlayMode tests, and resolve any regression.
- Perform the P0 refresh/Death/idempotency tests from the test plan with Firestore test data.
- Assign a production TMP FCT prefab/font/material if desired; the runtime fallback is functional.
- Conduct the required human timing/motion/FCT/audio/Death-Rebirth review before merge.

---

## Git Commit Summary

```text
feat(combat): add receipt-driven game juice presentation

- persist and recover attempt and settlement presentation obligations
- add actor, FCT, critical impulse, action queue, UI lifecycle, and input gate presenters
- add deterministic plan/readiness tests and QA handoff
```
