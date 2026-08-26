---
slug: ui-mockup-experience-slice-1
status: draft
source: manual
gdd_tags:
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human-tester
blocked_by:
  - slice-1-human-visual-review
---

# UI Mockup Experience Slice 1 Test Plan

## 1. Scope

This plan validates the shared Math:World UI foundation and the migrated Authentication and Bootstrap/Loading entry screens. It does not accept Main Menu or any feature panel migration.

## 2. Automated Contract Checks

Run `PowerMath.Gameplay.Combat.Unity.Tests.UiEntryAssetContractTests` in EditMode.

- [x] `AuthenticationAsset_PreservesLoginContract`
  - entry UXML imports and clones;
  - `login-screen`, username, six-digit password/PIN, remember-device toggle, sign-in button, and status label exist with the expected control types;
  - password mode and six-character maximum remain enforced;
  - no Guest or email control is introduced.
- [x] `BootstrapAsset_PreservesTruthfulStateContract`
  - entry UXML imports and clones;
  - screen, phase, status, detail, real progress, and retry controls exist;
  - retry starts hidden.

Evidence on 2026-08-26: 2/2 passed in 0.05 seconds.

The full `PowerMath.Gameplay.Combat.UnityEditModeTests` assembly completed six tests with one failure outside this slice: `CombatLobbyViewTests.MainMenuAsset_ContainsPetGachaInteractionContract` expected an untouched Main Menu inline overlay display value of `None` but Unity returned `Flex`. Slice 1's two contract tests passed in that run. Resolve or re-baseline that Main Menu assertion before claiming the complete assembly is green.

## 3. Authentication Functional Checks

- [ ] Empty username or PIN produces a concise inline validation state and does not call authentication.
- [ ] PIN input remains masked and accepts at most six characters.
- [ ] Enter and Sign In produce at most one active request.
- [ ] Busy state disables editable controls and communicates progress in text, not color alone.
- [ ] Invalid credentials produce the existing generic credential error and clear sensitive input as designed.
- [ ] Network failure is distinguishable from invalid credentials and remains recoverable.
- [ ] Remember This Device starts unchecked and the private-device warning remains visible.
- [ ] Successful sign-in returns to Bootstrap and continues the existing session flow.

## 4. Bootstrap State Checks

- [ ] Session check uses an indeterminate/bounded phase representation without invented percentage claims.
- [ ] Snapshot loading updates phase, status, detail, and real reported progress together.
- [ ] Recoverable failures expose `TRY AGAIN` and retain the blocking explanation.
- [ ] Non-recoverable/unavailable states remain explicit and do not show a false success state.
- [ ] Retry cannot start multiple bootstrap requests.
- [ ] Successful bootstrap transitions to Main Menu once.

## 5. Visual and Accessibility Checkpoint

- [x] Authentication and Bootstrap visual trees resolve at the 1920x1080 Panel Settings reference resolution.
- [x] Primary 16:9 renders use the approved flat Math:World direction with native text/controls and shape-driven decoration only.
- [x] No flattened mockup crop, unapproved character art, mock player value, or fake statistic was imported.
- [x] Project owner approves the 16:9 visual direction (`lgtm`, 2026-08-26).
- [ ] Validate the minimum supported window, 16:10, ultrawide, and high-DPI scaling once those exact window targets are confirmed.
- [ ] Validate keyboard-only focus order and visible focus treatment.
- [ ] Validate approximately 30% longer localized-like strings without clipped live controls.
- [ ] Validate reduced-motion behavior when the shared transition layer is introduced.

## 6. Acceptance Gate

Slice 1 is accepted only when its two automated contract tests pass, Authentication and Bootstrap state paths pass in Play Mode, the project owner approves the 16:9 renders, and the untouched Main Menu assembly failure is either fixed or documented as an accepted baseline by the responsible owner.
