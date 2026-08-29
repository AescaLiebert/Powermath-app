# Test Plan: Data-Driven Combat Presentation and Game Juice

## 1. Test Plan Summary

| Test Area | Priority | Type | Platform |
|---|---:|---|---|
| Receipt recovery and idempotency | P0 | Automated + manual | Editor, WebGL |
| Interaction locking | P0 | Automated + manual | Editor, WebGL/mobile |
| Death/Rebirth settlement | P0 | Manual | Editor, WebGL |
| Action ordering and queue reflow | P1 | Automated + manual | Editor, WebGL |
| Actor/FCT/critical feedback | P1 | Manual | Editor, WebGL/mobile |
| UI lifecycle and Reduced Motion | P1 | Manual | Editor, WebGL/mobile |
| Performance and pooling | P2 | Profiler | Target hardware |

## 2. Functional Tests

### P0 — Authority, recovery, and lock safety

**TEST: Exact receipt completion**  
**GIVEN:** An accepted attempt has a pending presentation receipt.  
**WHEN:** completion is requested with another presentation ID.  
**THEN:** completion is rejected, the receipt remains pending, and gameplay remains locked. The matching ID completes once.  
**PRIORITY:** P0

**TEST: Refresh during ordinary result presentation**  
**GIVEN:** Firestore contains an accepted attempt with `pendingPresentation.status = pending`.  
**WHEN:** the browser refreshes at player attack, FCT, enemy reaction, queue consume, and enemy attack.  
**THEN:** the saved semantic result reconstructs without additional damage/reward; input remains locked until recovery reaches stable state and acknowledgement persists.  
**PRIORITY:** P0

**TEST: Death is mandatory and replayable**  
**GIVEN:** an accepted enemy attack reduces the player to zero hearts.  
**WHEN:** the result plays normally, and separately when refreshing before settlement acknowledgement.  
**THEN:** player Take Damage and Die finish before the Death panel enters; no Stage 1 controls are exposed; Retry is the only terminal action; refresh replays Death from the saved settlement receipt.  
**PRIORITY:** P0

**TEST: Rebirth is visually distinct**  
**GIVEN:** the player accepts Rebirth settlement.  
**WHEN:** the authoritative command succeeds.  
**THEN:** cancel is no longer available, no Die animation plays, Rebirth presentation completes, the saved result panel enters, and Continue acknowledges before reload.  
**PRIORITY:** P0

**TEST: Interaction unlock predicate**  
**GIVEN:** each combination of authority pending, plan non-empty, actor non-idle, queue moving, and blocking UI moving.  
**WHEN:** the player clicks or submits keyboard input.  
**THEN:** no gameplay/navigation command is accepted until every predicate is stable; question input remains available only during its intended answer window.  
**PRIORITY:** P0

### P1 — Presentation correctness

**TEST: Player-first sequence**  
**GIVEN:** correct, incorrect, timeout, critical, lethal, Walk, and Attack outcomes.  
**WHEN:** each accepted receipt is presented.  
**THEN:** steps follow the deterministic semantic plan; lethal player damage cancels enemy retaliation; future appended follow-up/counter steps preserve list order.  
**PRIORITY:** P1

**TEST: Enemy action queue lifecycle**  
**GIVEN:** cooldowns of 1, 2, maximum, event risk, newly spawned enemy, and an exhausted list.  
**WHEN:** an attempt commits and resolves.  
**THEN:** the first-left token arms, exits after the player action, is removed, survivors reflow, and new tokens visibly enter before Attack unlocks.  
**PRIORITY:** P1

**TEST: Target-relative FCT**  
**GIVEN:** enemy and player anchors at multiple Canvas scales/aspect ratios.  
**WHEN:** damage is accepted.  
**THEN:** pooled TMP text originates above the affected sprite, shows the exact accepted value, follows Pop/Hold/Slide-Fade, and simultaneous values separate deterministically.  
**PRIORITY:** P1

**TEST: Critical feedback and Reduced Motion**  
**GIVEN:** the same critical receipt with Reduced Motion off and on.  
**WHEN:** the impact step runs.  
**THEN:** critical text has a non-color label and distinct reaction; normal mode impulses only the combat world root; Reduced Motion removes the impulse and preserves readable timing/state completion.  
**PRIORITY:** P1

**TEST: UI lifecycle cleanup**  
**GIVEN:** attempt, feedback, banner, biome transition, and settlement UI during Entering/Exiting.  
**WHEN:** a newer transition, disable, scene unload, or recovery interrupts it.  
**THEN:** each unit deterministically ends in Hidden or Idle with no stale blocker, scheduled item, or click target.  
**PRIORITY:** P1

## 3. Edge Case Tests

- [ ] Spam Attack, number keys, Submit, Backspace, navigation, and Retry throughout every presentation frame.
- [ ] Refresh before and after attempt completion acknowledgement and settlement acknowledgement.
- [ ] Retry the same Firestore command after a simulated lost response; confirm no double reward, damage, rank change, or settlement.
- [ ] Test zero enemy HP, zero hearts, maximum cooldown, cooldown one, empty/reinitiated queue, and Stage/biome boundary.
- [ ] Disable the composition root mid-FCT, impulse, actor action, queue initiate, queue consume, and UI transition.
- [ ] Test missing optional TMP prefab/profile assets; runtime fallbacks must remain functional.
- [ ] Inject unknown receipt/schema versions; bootstrap must fail closed with update-required messaging.
- [ ] Test a content-load failure after attempt reservation; question/cooldown rollback and ready state must remain valid.
- [ ] Verify event encounters use Event Risk rather than fabricated Walk/Attack data.

## 4. Performance Tests

- [ ] Unity Profiler: no per-frame GC allocations while combat is Idle or an actor action is running.
- [ ] FCT pool stabilizes at configured capacity under burst damage; no orphaned TMP objects after scene disable.
- [ ] UI Toolkit queue transitions maintain target frame rate on primary mobile hardware and desktop WebGL.
- [ ] Combat world impulse does not move navigation/settlement layers or trigger Canvas rebuild spikes outside its root.
- [ ] Memory returns to baseline after 100 attempts and repeated Death/Rebirth recovery cycles.

## 5. Platform-Specific Tests

| Test | WebGL/mobile | Editor desktop |
|---|---|---|
| Input lock | Touch cannot pierce shield or activate covered controls | Mouse and keyboard cannot bypass gate |
| Refresh/recovery | Browser reload replays pending receipt | Stop/play with persisted test data reconstructs receipt |
| Layout | FCT and queue stay anchored at narrow/tall safe-area layouts | Resize Game view across supported aspect ratios |
| Reduced Motion | Device setting/config disables impulse | Runtime setting produces the same semantic result |

## 6. Regression Checklist

- [ ] Attempt idempotency and duplicate command receipts.
- [ ] Correct/incorrect/timeout damage and academic currency behavior.
- [ ] Rank promotion/demotion values and presentation labels.
- [ ] Question preparation/answer timing, reserved-question recovery, and content-failure rollback.
- [ ] Stage advance, biome transition, enemy spawn, and Run Complete.
- [ ] Death and Rebirth settlement economics and reset payload.
- [ ] Main Menu navigation, modal close, and terminal panel routing.
- [ ] Existing UI Toolkit asset-contract tests.

## 7. Automation Coverage and Current Limitation

EditMode sources cover deterministic plan order, critical/FCT steps, lethal cancellation, Death/Rebirth differences, unknown-version recovery, readiness gates, schema migration defaults, gate scopes, receipt creation, and matching completion IDs. The assemblies compile successfully in isolation. The test runner still needs to be executed after the currently open Unity Editor leaves Play Mode; a second Unity process cannot acquire the project lock.

## 8. Bug Report Template

```text
BUG: [Combat presentation] concise title
SEVERITY: Critical / Major / Minor / Cosmetic
RECEIPT: presentationId, attemptId, version, outcome
REPRO STEPS:
  1. State the saved combat/settlement precondition.
  2. State the action or refresh timing.
  3. State the repeated input, if any.
EXPECTED: Expected semantic order, lock state, and final saved state.
ACTUAL: Observed order, lock state, and saved state.
PLATFORM: Editor / WebGL, browser/device, resolution, Reduced Motion setting
FREQUENCY: Always / Sometimes / Rare
SCREENSHOT/VIDEO/LOG: link
```
