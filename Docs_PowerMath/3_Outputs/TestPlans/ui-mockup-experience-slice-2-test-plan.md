---
slug: ui-mockup-experience-slice-2
status: draft
source: manual
gdd_tags:
  - core-loop
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human-tester
blocked_by:
  - slice-2-human-visual-review
  - combat-playmode-fixture-player-id
---

# UI Mockup Experience Slice 2 Test Plan

## 1. Test Plan Summary

| Test area | Priority | Type | Platform |
| --- | --- | --- | --- |
| Stable Main Menu binding contract | P0 | Automated EditMode | Unity Editor |
| Combat snapshot projection and Attack contract | P0 | Automated EditMode + PlayMode | Windows Editor |
| Navigator exclusivity, close, and focus restoration | P0 | Automated EditMode + manual | Windows Editor |
| Informational-only map disclosure | P0 | Automated contract + manual | Windows Editor |
| Main Menu and Navigator composition | P1 | Human visual | 1920x1080 reference |
| Keyboard, scaling, and long-copy behavior | P1 | Manual | Supported Windows matrix |

## 2. Automated Evidence

- [x] `PowerMath.Gameplay.Combat.UnityEditModeTests`: 9/9 passed on 2026-08-26.
- [x] Main Menu entry UXML imports and clones through all nested templates.
- [x] Existing combat view projection, rank-without-hidden-audit, and Pet Gacha binding contracts remain green.
- [x] Main Menu shell, combat, and Navigator required names/types bind exactly once through the stable entry asset.
- [x] `MainMenuPanelHost` opens/closes one panel and rejects a competing panel until the current panel closes.
- [x] Unity compilation completed with no source errors after the final import.

The existing four `PowerMath.Gameplay.Combat.PlayModeTests` were executed. All four stopped on the same test-environment error before their combat assertions: `Player ID cannot identify the Firestore level and student fields.` This slice did not change player ID parsing, Firestore routing, or the test fixtures. Repair or re-baseline that fixture before claiming the PlayMode combat regression gate is green.

## 3. Functional Tests

### TEST: Main Menu presents the current authoritative snapshot

GIVEN: a hydrated player session  
WHEN: Main Menu opens  
THEN: display name, Stage, progress, wallet, loadout, Rank, Rank Currency, encounter, HP, enemy cooldown/Event rule, and Attack eligibility reflect the current snapshot without mock values.  
PRIORITY: P0

### TEST: Attack remains one authoritative commit

GIVEN: the encounter is ready  
WHEN: the player presses Attack repeatedly or combines pointer and keyboard input  
THEN: the existing presenter accepts one committed attempt, disables conflicting input, and displays the question flow once.  
PRIORITY: P0

### TEST: Navigator is informational

GIVEN: the combat phase permits map viewing  
WHEN: the player selects Open Navigator  
THEN: the saved biome route appears, the current biome is marked with text and shape, and the panel states `ROUTE PREVIEW — NO FAST TRAVEL`; selecting a route row performs no travel or write.  
PRIORITY: P0

### TEST: Navigator close restores the combat decision

GIVEN: Navigator was opened from the combat HUD  
WHEN: Back to Battle is selected  
THEN: Navigator hides, focus returns to Open Navigator, and the same encounter remains ready.  
PRIORITY: P0

### TEST: Competing panel requests are rejected

GIVEN: Navigator is open  
WHEN: another presentation panel requests the shared host  
THEN: the second request is rejected until Navigator closes; no economy or combat state changes.  
PRIORITY: P0

## 4. Edge and Abuse Cases

- [ ] Spam Open Navigator and Back to Battle; only one panel identity exists and no focus is lost.
- [ ] Attempt Navigator during a committed question; its button remains unavailable.
- [ ] Open/close on the first rendered frame after Main Menu loads.
- [ ] Render a missing Stage Map; the UI fails closed with a truthful empty/preparing state.
- [ ] Render long player, enemy, biome, and loadout names without covering Attack or cooldown information.
- [ ] Verify Stage 1 and Stage 200 labels and progress remain distinct from World Level and Rank.
- [ ] Verify Event encounters replace the cooldown copy with the authoritative Event consequence.
- [ ] Verify unavailable player data disables action without hiding the reason.

## 5. Performance and Platform Checks

- [ ] Confirm no per-frame allocation was introduced by the templates or panel host.
- [ ] Profile Main Menu at the Windows target of 60 FPS; no new continuous polling is expected.
- [ ] Validate reference 16:9, minimum supported window, 16:10, ultrawide, and high-DPI scaling.
- [ ] Complete Main Menu and Navigator navigation with keyboard only.
- [ ] Confirm every focused button has a visible non-color-only focus treatment.
- [ ] Confirm reduced motion does not alter the final visible/open state.

## 6. Human Visual Checkpoint

- [x] Main Menu shell reviewed at the 1920x1080 Panel Settings reference resolution.
- [x] Combat-ready, answer-entry, and Navigator compositions reviewed at 1920x1080 using the resolved UI Toolkit tree.
- [x] Navigator clearly communicates that it is informational and has no fast travel.
- [x] No mockup crop, unapproved character, or fabricated live player value was imported.
- [x] Project owner approves the Slice 2 visual direction (`lgtm`, 2026-08-26).

## 7. Regression Checklist

- [ ] Authentication and Bootstrap entry contracts remain green.
- [ ] Logout retains its existing service and recovery behavior.
- [ ] Profile card still opens Profile Analytics.
- [ ] Rebirth, Player Hub, Pet Gacha, Leaderboard, and Profile panels retain their current controllers and bindings.
- [ ] Pet Gacha confirmation/result overlays can still override their semantic hidden defaults when the controller opens them.
- [ ] Hidden audit score/count fields remain absent from the player-facing tree.
- [ ] No Main Menu action writes travel, Stage, Rank, currency, or encounter data outside existing authoritative paths.

## 8. Acceptance Gate

Slice 2 is accepted when the project owner approves the Main Menu/Navigator visuals, the four combat PlayMode fixtures can pass their player-ID setup, the functional checklist passes in Play Mode, and the requested Windows viewport matrix is complete.
