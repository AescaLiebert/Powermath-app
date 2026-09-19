# Test Plan: `OnFirstCreate` Tutorial

Date: 2026-09-15

## 1. Test Summary

| Area | Priority | Type | Current result |
|---|---:|---|---|
| Pure state transitions and terminal completion | P0 | EditMode + standalone harness | Five focused tests and end-to-end reducer harness pass |
| V4-to-V5/default/reset and dynamic-map patching | P0 | EditMode | Three focused tests pass; Unity runner pending |
| Combat commit/result checkpoint integration | P0 | compile + PlayMode/manual | Runtime compiles; interaction/recovery playtest pending |
| Input gate and semantic target fallback | P0 | PlayMode/manual | Runtime compiles; scene validation pending |
| English/Thai localization contract | P1 | static + manual | JSON valid and keys unique; visual wrapping pending |
| UXML/USS overlay import and responsive layout | P1 | static + manual | XML valid; Unity import/mobile visual QA pending |
| Direct-Firestore reconnect/idempotency | P0 | EditMode + live manual | Policy compiles; live retry/conflict test pending |

## 2. Automated Verification Completed

- Compiled `PowerMath.Gameplay.Tutorial.Core` sources with Unity's C# compiler references.
- Compiled the modified Combat Unity assembly, the complete runtime `Assembly-CSharp`, and the complete Editor test assembly against the updated references with zero compiler errors.
- Compiled the tutorial EditMode tests against the Tutorial Core assembly with zero compiler errors.
- Executed five authored state-machine/safe-state EditMode methods and three lifecycle migration/idempotency/completion-guard methods through the compiled assemblies: 8 passed, 0 failed.
- Ran a standalone reducer scenario covering safe-Lobby start, new-player variant, guided commit, mismatched transaction rejection, truthful timeout branch, same-encounter rejection, next-enemy handoff, durable completion, and terminal no-replay behavior: `TUTORIAL_CORE_VALIDATION_OK`.
- Parsed `UI.json`; localization JSON is valid and tutorial keys have no duplicates.
- Parsed `TutorialOverlay.uxml` and `MainMenuShell.uxml` as XML successfully.
- Ran scoped `git diff --check`; no whitespace errors were reported. Windows line-ending notices reflect the existing checkout policy.

The Unity Test Runner was not started because the project is open in the interactive Unity Editor and this environment cannot safely acquire a second project lock. Compilation used copies/overrides of Unity's generated response files and did not modify build settings.

## 3. P0 Functional Scenarios

- [ ] Force the overlay template to be missing/detached; confirm no tutorial interaction lease remains and the localized recovery message is shown.
- [ ] Finish character creation as a new player; confirm `OnFirstCreate` queues, saves, and opens `welcome-new` only when the standard Lobby is stable.
- [ ] Enter with a legacy character-complete account and no tutorial entry; confirm `welcome-returning`, no Stage/run reset, and the current standard enemy is guided.
- [ ] Enter with a legacy character-complete account at a stable standard Lobby; confirm `welcome-returning` appears during that entry session without requiring another combat event.
- [ ] Enter during a Challenge/Event, pending result, settlement, or panel transition; confirm the tutorial remains hidden and resumes at the next safe standard Lobby.
- [ ] Tap everywhere except the highlighted target; confirm no game command is accepted. Tap the enemy repeatedly; confirm exactly one normal attempt commit.
- [ ] During every focus step, click/tap the transparent scrim, navigation, settings, panels, CombatSurface, and keyboard shortcuts; confirm only the visible focus proxy can dispatch an action.
- [ ] Verify the overlay releases before video/preparation/answer timing so keypad, submit, and timer operate normally.
- [ ] Complete the first attempt as correct, incorrect, and timeout; confirm only the matching truthful dialogue appears after combat feedback is stable.
- [ ] Close the app on every dialogue/focus step, reopen, and confirm the tutorial restarts from its authored safe entry instead of resuming a stale scene target.
- [ ] Close after a one-time reward has been claimed, reopen, and confirm the tutorial may restart but the reward is not granted again.
- [ ] Refresh with a pending result receipt; confirm combat replay finishes first and the tutorial outcome advances once by matching transaction ID.
- [ ] Let the first enemy survive; advance the result dialogue, continue normal combat, and confirm the handoff waits for a different standard encounter.
- [ ] Defeat the first enemy; confirm the scared handoff appears only after the next enemy is ready. Advance it and confirm durable `Completed` state.
- [ ] Reconnect after completion and edit copy/sprite/audio content; confirm the tutorial does not replay.

## 4. Persistence and Failure Scenarios

- [ ] Migrate representative V1-V4 player payloads and confirm an empty V5 `tutorialMap` without false completion.
- [ ] Retry the same tutorial operation ID and confirm revision/state do not advance twice.
- [ ] Attempt to reopen a completed entry and confirm the command is rejected.
- [ ] Simulate a revision conflict between tutorial and combat writes; confirm the scene reports recovery and does not optimistically advance.
- [ ] Confirm every successful tutorial save updates the shared live player revision before the next combat save.
- [ ] Run full admin reset and confirm `tutorialMap` is empty; run Death/Rebirth and confirm it is preserved.
- [ ] Load unknown status, step, or newer content version and confirm update-required recovery without a gameplay mutation.

## 5. Localization, Accessibility, and Presentation

- [ ] Switch between Thai and English on every visible dialogue; confirm current text refreshes without step rewind.
- [ ] Test the smallest supported mobile viewport; confirm Thai copy wraps, the dialogue does not truncate, and target size remains usable.
- [ ] Verify the enemy primary target and Attack-button fallback are never exposed together.
- [ ] Verify scrim contrast on every biome background and confirm Power art does not cover the focus target.
- [ ] Verify Reduced Motion preserves focus contrast, copy, and state transitions.
- [ ] Test muted/missing optional audio and missing Power sprite behavior; progression must remain unchanged.

## 6. Regression and Release Gates

- [ ] Run all EditMode and relevant PlayMode tests in the open Editor after asset refresh.
- [ ] Re-run normal combat, Challenge combat, pending-result recovery, Death/Rebirth, and Main Menu panel gate scenarios.
- [ ] Review final motion, Power art, audio, contrast, and mobile hit sizing with the project owner.
- [ ] Keep Firebase-rule/schema publication, builds, CI, merge, and release outside this task until separately approved.
