---
slug: ui-mockup-experience-slice-3
status: draft
source: manual
gdd_tags:
  - run-reset
  - economy
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human-tester
blocked_by:
  - slice-3-human-visual-review
---

# UI Mockup Experience Slice 3 Test Plan

## 1. Scope

Validate the Player Hub and Rebirth visual migration without changing `PlayerStatProjection`, Weapon Ascension cost/receipts, Rebirth eligibility/rewards, settlement idempotency, persistence, or leaderboard publication.

## 2. Automated Evidence

- [x] `PowerMath.Gameplay.Combat.UnityEditModeTests`: 11/11 passed on 2026-08-26.
- [x] The stable Main Menu entry imports and clones the focused Player Hub and Rebirth templates.
- [x] Every existing Player Hub and settlement binding resolves to the required UI Toolkit type.
- [x] Both panels are hidden by semantic class before a controller opens them.
- [x] The shared host rejects competing manual panels and allows a terminal settlement to take presentation priority.
- [x] Unity compiled all changed runtime scripts with no source errors.

The full EditMode project run completed 88 tests: 87 passed and one pre-existing Pet Gacha core test failed, `PetGachaCoreTests.Roll_UsesCategoryBoundaryThenExactPetWeights`, because it expected category `rare` and received `middle`. Slice 3 did not modify the Pet Gacha catalog, probability engine, random source, or that test.

## 3. Functional Scenarios

### TEST: Player Hub shows current permanent power

GIVEN: an authoritative player snapshot is loaded  
WHEN: Player Hub opens  
THEN: Effective ATK, Base/Weapon/Pet breakdown, Legacy bonus, Pet status, current weapon, next weapon, Power Coin balance, and upgrade cost come from the existing projection/catalog/policy path.  
PRIORITY: P0

### TEST: Weapon Ascension remains one safe transaction

GIVEN: the player is eligible and has enough Power Coins  
WHEN: Upgrade is pressed repeatedly  
THEN: one pending transaction ID is reused, conflicting close/upgrade input is disabled while saving, and success or failure is labeled without inventing a result.  
PRIORITY: P0

### TEST: Rebirth preview explains the irreversible outcome

GIVEN: Stage 50+ and no committed or unresolved question  
WHEN: Rebirth opens  
THEN: the preview shows authoritative Stage, Power Coin reward, Legacy ATK reward, Effective ATK change, Prestige change, plus explicit KEEP and RESET groups before confirmation.  
PRIORITY: P0

### TEST: Rebirth remains unavailable in unsafe states

GIVEN: Stage below 50, a committed question, or another policy-ineligible state  
WHEN: the lobby renders  
THEN: the existing Rebirth button eligibility remains disabled and no settlement command is issued.  
PRIORITY: P0

### TEST: Terminal settlement takes presentation priority

GIVEN: another host-managed panel is open  
WHEN: the authoritative run enters `RunDefeat`  
THEN: that panel closes, the run summary opens, combat cannot restart, and settlement still executes at most once for the run ID.  
PRIORITY: P0

## 4. Edge and Abuse Cases

- [ ] Spam Player Hub open/close and confirm only one host panel remains open.
- [ ] Spam Upgrade; verify one spend and one accepted Weapon Ascension result.
- [ ] Disconnect during Weapon Ascension and retry with the same pending transaction ID.
- [ ] Attempt to close either panel while its authoritative operation is busy.
- [ ] Trigger `RunDefeat` while another reference panel is open.
- [ ] Retry a failed death settlement; rewards do not duplicate.
- [ ] Reconnect before and after a successful Rebirth; the run is either terminal or already at fresh Stage 1, never settled twice.
- [ ] Verify long weapon names and large coin/attack values wrap without covering the action.
- [ ] Verify KEEP/RESET remains understandable without relying on green/red color.

## 5. Game-Experience Evaluation

| Component | Slice 3 evidence |
| --- | --- |
| Clarity | Current/next/cost are grouped before Upgrade; Rebirth previews rewards and names preserved/reset state before confirmation. |
| Motivation | Permanent attack, Power Coins, Legacy ATK, and Prestige remain connected to authoritative persistent state. |
| Response | One shared host arbitrates panels; busy operations disable conflicting controls and do not silently cancel. |
| Satisfaction | Busy, accepted, and failure states have distinct visual treatment while existing accepted-result copy remains authoritative. |
| Fit | The cream, cyan, cobalt, coral, and gold geometry continues the approved Math:World visual language without unapproved character art. |

No new numeric tuning values were introduced.

## 6. Visual and Accessibility Review

- [x] Player Hub reviewed at 1920x1080 with truthful preparing placeholders in the controller-free render harness.
- [x] Rebirth reviewed at 1920x1080 with explicit KEEP and RESET groups.
- [x] No mock player, weapon, currency, Stage, reward, or Prestige value was baked into the templates.
- [x] No inventory/equip surface or new progression behavior was added.
- [ ] Validate keyboard-only focus order and focus restoration in Play Mode with a real session.
- [ ] Validate minimum supported window, 16:10, ultrawide, and high-DPI scaling.
- [ ] Confirm accepted Weapon Ascension and Rebirth audio feedback with production audio enabled.
- [ ] Project owner approves the Slice 3 visual direction.

## 7. Acceptance Gate

Slice 3 is accepted when the project owner approves both panel visuals, the functional and abuse scenarios pass against a real hydrated session, the supported Windows viewport matrix is complete, and accepted Weapon Ascension/Rebirth feedback is verified through visual plus audio channels.
