---
slug: main-menu-tween-transitions
status: draft
source: manual
gdd_tags:
  - core-loop
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human
blocked_by: []
---

# Test Plan: Main Menu Tween Transitions

## 1. Test Plan Summary

| Test Area | Priority | Type | Platform |
| --- | --- | --- | --- |
| Bootstrap presentation order and cleanup | P0 | Manual + state probe | Windows Editor / WebGL follow-up |
| Accepted panel-close return order | P0 | Automated + manual | Windows Editor |
| Input/focus lock lifecycle | P0 | Automated + state probe | Keyboard/mouse |
| Cancellation, disable, missing dependency | P0 | Automated + manual | Windows Editor |
| Reduced motion | P1 | Manual | Windows Editor / mobile-compatible layout |
| Resolution and frame-rate behavior | P1 | Manual | 16:9, 16:10, ultrawide, minimum window |
| Audio hook/content | P2 | Manual after clips are approved | Target build |

## 2. Functional Tests

### TEST: Bootstrap order

- GIVEN: a hydrated player session enters `MainMenuScene`.
- WHEN: combat/session presentation becomes ready.
- THEN: `BATTLE START` appears, cover recedes, Player/Enemy arrive, then top HUD, Player Menu, and Player Dashboard reveal together.
- PRIORITY: P0.

### TEST: Session return for approved panels

- GIVEN: Navigator, Pet Gacha, Leaderboard, or Profile Analytics is open and safely closable.
- WHEN: the existing controller accepts Close/Back.
- THEN: the panel hides; Top HUD, Player Menu, and Dashboard start together, finish together using the shared duration, and input unlocks once.
- PRIORITY: P0.

### TEST: Excluded/terminal close paths

- GIVEN: `ForceCloseAll`, Rebirth, Player Hub, or another non-approved source closes.
- WHEN: the panel host clears state.
- THEN: no requested session-return sequence is published by `ForceCloseAll`; transaction eligibility remains owned by the feature controller.
- PRIORITY: P0.

### TEST: Focus restoration

- GIVEN: a panel was opened by keyboard or pointer.
- WHEN: its close is accepted and the return sequence completes.
- THEN: the existing opener/fallback remains the focus target and becomes actionable only after the blocker clears.
- PRIORITY: P0.

### TEST: Unavailable session

- GIVEN: Main Menu is entered directly without a hydrated session or combat setup fails.
- WHEN: the unavailable state renders.
- THEN: the cover/input blocker is removed and the unavailable UI remains readable.
- PRIORITY: P0.

## 3. Edge Cases

- [x] Repeated accepted close publishes one `PanelClosed` event.
- [x] Rejected repeated close publishes no additional event.
- [x] `ForceCloseAll` publishes no session-return event.
- [x] Missing optional audio does not affect timing or state.
- [x] Missing session cancels presentation and restores stable UI.
- [ ] Spam twenty open/close/back cycles using physical keyboard and mouse.
- [ ] Disable the UIDocument GameObject during title, character, and HUD phases.
- [ ] Unload the scene during each phase.
- [ ] Remove Player and Enemy references independently and confirm fail-soft completion.
- [ ] Resize during character/HUD entrance and verify authored final positions.

## 4. Performance Tests

- [ ] Profile bootstrap and return at the target WebGL/mobile-compatible resolution.
- [x] Confirm no permanent `Update()` loop exists; only bounded LeanTweens and sequencing coroutines sample during presentation.
- [ ] Confirm no recurring GC allocation after the sequence completes.
- [ ] Confirm frame spikes end in the same final position/opacity/input state.
- [ ] Confirm the 1024×1536 temporary Player texture is within the accepted memory budget or reduce import size after visual review.

## 5. Platform-Specific Tests

| Test | Unity WebGL/mobile direction | Windows Editor |
| --- | --- | --- |
| Pointer/touch blocker | No click-through during moving targets | No mouse click-through |
| Keyboard focus | Web keyboard focus remains predictable | Tab/Enter/Escape restore correctly |
| Reduced motion | Crossfade only, no large translation | Same semantic path |
| Aspect ratio | Safe-area groups finish at authored USS layout | 16:9 reference capture |

## 6. Regression Checklist

- [x] Main Menu panel identity/exclusivity tests.
- [x] Main Menu entry UXML bindings.
- [x] Affected Combat Unity EditMode assembly.
- [ ] Gacha accepted transaction cannot be cancelled or duplicated.
- [ ] Rebirth terminal settlement still uses silent `ForceCloseAll`.
- [ ] Navigator Back and Escape restore its opener.
- [ ] Leaderboard/Profile refresh/rename state survives panel return.
- [ ] Combat Attack cannot fire through the transition blocker.
- [ ] Logout/scene unload leaves no callback or coroutine active.

## 7. Current Automated Evidence

- Focused `MainMenuPanelHostTests` + `UiEntryAssetContractTests`: **11/11 passed** on 2026-08-27 after viewport-staging/LeanTween regression coverage was added.
- `PowerMath.Gameplay.Combat.UnityEditModeTests`: **15/15 passed** on 2026-08-27.
- Direct transition-state verification: **3/3 passed** for off-screen direction, final authored transforms/input restoration, and reduced-motion crossfade-only behavior.
- Unity compilation: no C# errors. One unrelated LeanTween example obsolescence warning remains.

## 8. Human Game-Feel Review

Review the approved starting values rather than treating them as final:

1. Is `BATTLE START` readable without feeling like a long cutscene?
2. Do Player/Enemy stop with enough weight and no floating impression?
3. Does repeated panel navigation remain responsive with the simultaneous shared starting duration of 0.28 seconds?
4. Is the temporary Player scale/placement acceptable for continued iteration?
5. Does reduced motion preserve clarity without large displacement?

## 9. Bug Report Template

```text
BUG: Main Menu transition — {short title}
SEVERITY: Critical / Major / Minor / Cosmetic
PHASE: Bootstrap title / Character reveal / Top HUD / Player Menu / Dashboard / Panel return
REPRO STEPS:
  1. {scene/session setup}
  2. {input}
  3. {interruption or resolution}
EXPECTED: {stable presentation and input state}
ACTUAL: {observed result}
PLATFORM/RESOLUTION/FPS: {details}
FREQUENCY: Always / Sometimes / Rare
SCREENSHOT/VIDEO: {path or link}
```
