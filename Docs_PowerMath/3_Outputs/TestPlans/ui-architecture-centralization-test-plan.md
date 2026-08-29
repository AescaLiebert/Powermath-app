---
slug: ui-architecture-centralization
status: automated-pilot-passed-pending-human-review
source: manual
gdd_tags:
  - feedback
  - player-experience
  - playtest
owner: qa-agent
human_checkpoint: required
next_agent: human
blocked_by: []
---

# Test Plan: UI Architecture Centralization

## 1. Test Matrix

| Surface | Normal motion | Reduced motion | Pointer | Keyboard | Scene disable/unload |
| --- | --- | --- | --- | --- | --- |
| Main Menu panel host | Required | Required | Required | Required | Required |
| Main Menu bootstrap/return | Required | Required | Required | Required | Required |
| Player Hub feedback | Required | Required | Required | Required | Required |
| Gacha/Rebirth/Social/Admin | Required | Required | Required | Required | Required |
| Combat transient UI | Required | Required | Required | Required | Required |
| Authentication/Bootstrap | Required | Required | Required | Required | Required |

## 2. Foundation EditMode Tests

- `Enter` moves Hidden -> Entering -> Idle and invokes completion once.
- `Exit` moves Idle/Hovered/Pressed -> Exiting -> Hidden and invokes completion once.
- Exit during enter samples current state and finishes Hidden.
- Enter during exit samples current state and finishes Idle.
- Starting Lifecycle cancels conflicting Feedback, Interaction, and Ambient channels.
- Disposing a binding unregisters all UI Toolkit callbacks and leaves no active tween.
- `Cancel(...ApplyHidden)` applies `display: none`, ignored picking, authored transform cleanup, and no focus.
- `Cancel(...ApplyIdle)` applies visible, normal picking, authored transform/opacity, and no active tween.
- Reduced motion removes translation, overshoot, shake, and ambient loops while preserving completion callbacks.
- Focus plus pointer overlap remains Hovered until both are absent.

## 3. Panel Host Regression Tests

- A second panel cannot open while one panel owns the host.
- Reopening the already-open same panel is idempotent.
- Rejected open/close starts no tween and publishes no event.
- Accepted open publishes `PanelOpened` once at the documented lifecycle point.
- Accepted close publishes `PanelClosed` once and restores opener/fallback focus.
- `ForceCloseAll` applies Hidden synchronously and does not publish normal return feedback.
- Interaction gate remains authoritative for Navigation and TerminalAction scopes.
- Busy transaction panels cannot be bypassed by animation cancellation.

## 4. Migration Contract Tests

- Each migrated view binds successfully against its UXML subtree.
- Missing required elements produce one actionable binding failure that includes view, type, and element name.
- Feature views do not query outside their assigned subtree.
- Direct `LeanTween.*`, `LTDescr`, and tween-ID ownership are absent outside the shared driver after each migrated feature lands.
- Migrated targets do not retain USS transitions for opacity/scale/translate channels owned by the driver.
- Typed `UiSemanticState` produces exactly one semantic state class.
- Required scene dependencies are serialized before their `Find`/`Resources.Load` fallback is removed.

## 5. PlayMode Stress Tests

1. Open/close each Main Menu panel twenty times with mouse, keyboard, and mixed input.
2. Trigger close at 10%, 50%, and 90% of enter; reopen at the same points of exit.
3. Move pointer in/out while keyboard focus remains on the control.
4. Spam press/release while a panel begins exit.
5. Disable the `UIDocument` GameObject during enter, idle, feedback, and exit.
6. Unload the scene during every lifecycle phase.
7. Verify exactly one visible panel, one panel-host identity, expected focus, and no lingering interaction shield after every scenario.

## 6. Feature Regression

- Main Menu approved bootstrap and session-return order remains unchanged.
- Player Hub upgrade/equip commands remain idempotent and feature feedback still combines visual and approved audio cues.
- Gacha confirmation/result cannot duplicate a pull or spend.
- Rebirth/death settlement remains terminal and cannot be bypassed through close/reopen spam.
- Leaderboard refresh and Profile rename retain in-flight/busy protections.
- Combat Attack cannot fire through a lifecycle blocker.
- Authentication validation, retry, and scene navigation remain unchanged.
- Bootstrap version/session/error states remain readable and retry remains actionable.

## 7. Visual and Accessibility Review

- Review 16:9, 16:10, ultrawide, and minimum supported window sizes.
- Confirm authored final layout is restored after resize during motion.
- Confirm normal and reduced-motion recordings communicate the same lifecycle and semantic states.
- Confirm no idle animation competes with question, warning, result, or error information.
- Confirm pointer hover and keyboard focus have equivalent clarity.

## 8. Performance Acceptance

- No new feature-owned permanent `Update()` loop.
- No recurring GC allocation after all motion is idle.
- Hidden targets have no active ambient or interaction tweens.
- Tween count returns to baseline after repeated navigation and scene unload.
- Profile before changing the existing project performance budgets.

## 9. Pilot Exit Criteria

- Foundation and Main Menu panel-host tests pass.
- Twenty-cycle navigation stress completes without stuck input, wrong focus, or duplicate events.
- Main Menu transition and Player Hub feature regressions pass in normal and reduced motion.
- Human approves the Main Menu pilot feel before migration continues to the remaining panels.

## 10. Automated Pilot Results — 2026-08-29

- Unity 6000.5.3f1 batch compilation: passed with zero compiler errors.
- `PowerMath.UI.Core.Tests`: 7/7 passed, covering lifecycle reversal, reduced motion, replacement, and channel priority.
- `PowerMath.Gameplay.Combat.Unity.Tests`: 28/28 passed, including panel identity/exclusivity, async close/reopen, interaction gate, and UI asset contracts.
- Source ownership scan: direct `LeanTween.*` and `LTDescr` usage is confined to `LeanTweenUiDriver`; test files contain only negative contract assertions.
- Remaining manual gate: normal/reduced-motion visual review, pointer/keyboard focus review, supported aspect ratios, and twenty-cycle navigation stress in the Unity Editor.
