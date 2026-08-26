---
slug: ui-mockup-experience-slice-4
status: draft
source: manual
gdd_tags:
  - gacha
  - economy
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human-tester
blocked_by:
  - slice-4-human-visual-review
---

# UI Mockup Experience Slice 4 Test Plan

## 1. Scope

Validate the current one-pull Pet Gacha visual migration without changing the 25 Power Coin cost, category weights, owned-pet weighting, duplicate outcome, catalog authority, persistence, or idempotency. x10, guarantee/pity, history, and Details remain out of scope.

## 2. Automated Evidence

- [x] Focused UI, panel-host, and catalog EditMode contracts: 16/16 passed on 2026-08-27.
- [x] The stable Main Menu entry imports and clones the focused Pet Gacha template and stylesheet.
- [x] Every existing Pet Gacha controller binding resolves to the required UI Toolkit type.
- [x] The modal, confirmation, and result layers start hidden and the decision layers are focusable.
- [x] Unsupported x10, History, Guarantee, and Details actions are absent.
- [x] The shared host rejects competing manual panels and restores focus to the Pet Gacha opener.
- [x] Unity compiled all changed runtime scripts and UI assets with no source errors.

The full EditMode project run completed 88 tests: 87 passed and the same pre-existing `PetGachaCoreTests.Roll_UsesCategoryBoundaryThenExactPetWeights` test failed because it expected category `rare` and received `middle`. Slice 4 did not modify the catalog, probability calculator, random source, or this expectation.

## 3. Functional Scenarios

### TEST: Preview shows the authoritative spend decision

GIVEN: a hydrated player and valid catalog are available  
WHEN: Pet Gacha opens  
THEN: current Power Coins, one-pull cost, projected balance, catalog version, category totals, individual pet probabilities, and owned markers come from the current player/catalog calculation.  
PRIORITY: P0

### TEST: Confirmation precedes the irreversible pull

GIVEN: the player is eligible and has at least 25 Power Coins  
WHEN: Pull is selected  
THEN: confirmation states the exact spend, before/after balance, and empty-duplicate rule before any command is issued.  
PRIORITY: P0

### TEST: Accepted pull has one readable result

GIVEN: the confirmed transaction is accepted  
WHEN: its receipt returns  
THEN: the result identifies rarity, pet, resulting balance, and either NEW ownership or DUPLICATE with no pet changes; Continue remains briefly locked so the result cannot be skipped accidentally.  
PRIORITY: P0

### TEST: Recoverable transport reuses the pending transaction

GIVEN: the command outcome is uncertain because transport failed  
WHEN: Recover Pull is selected  
THEN: the controller reuses the same pending transaction ID, keeps Cancel and Close disabled, and does not start a second spend.  
PRIORITY: P0

### TEST: Unsafe or unaffordable players cannot pull

GIVEN: insufficient Power Coins, a committed question, RunDefeat, or unavailable catalog/store configuration  
WHEN: the lobby and Pet Gacha render  
THEN: Pull is disabled with a labeled reason and no command is issued.  
PRIORITY: P0

## 4. Edge and Abuse Cases

- [ ] Spam the opener and verify only one host-managed panel remains open.
- [ ] Spam Pull/Confirm and verify one spend and one accepted receipt.
- [ ] Disconnect after Confirm, reconnect, and Recover using the same transaction ID.
- [ ] Attempt Close, Cancel, or competing-panel navigation while the transaction is committed/busy.
- [ ] Verify a stale preview or insufficient-funds rejection refreshes authoritative balance and odds.
- [ ] Verify owned status halves only that pet's individual share and redistributes within its rarity.
- [ ] Verify a duplicate grants no level, item, currency, or compensation.
- [ ] Verify long pet names, large balances, and a full catalog remain readable and scrollable.

## 5. Game-Experience Evaluation

| Component | Slice 4 evidence |
| --- | --- |
| Clarity | Live odds, cost, projected balance, owned markers, and duplicate consequence appear before confirmation. |
| Motivation | The reveal celebrates a new permanent collection addition while describing a duplicate honestly. |
| Response | Confirmation, saving, recoverable transport, success, and error states are explicit and input-safe. |
| Satisfaction | New and duplicate receipts use distinct labeled result treatments plus the existing audio hooks. |
| Fit | Flat cream, cyan, cobalt, coral, and gold geometry follows the approved Animo-inspired direction without importing unapproved character art. |

No new numeric tuning values were introduced; the existing 600 ms result readability lock was preserved.

## 6. Visual and Accessibility Review

- [x] No mock player, coin, pet ownership, rarity result, or live probability value was baked into controller output.
- [x] Confirmation, saving, error, new, and duplicate states use text labels in addition to color.
- [x] Reduced-motion mode removes reveal transition duration.
- [x] Focus targets exist for the modal, confirmation, and result layers; panel close restores opener focus.
- [ ] Review preview, confirmation, busy/recovery, new result, and duplicate result at 1920x1080 in Play Mode with a real session.
- [ ] Validate keyboard-only focus order, Escape/close policy, and screen-reader-equivalent labels where supported.
- [ ] Validate minimum supported window, 16:10, ultrawide, and high-DPI scaling.
- [ ] Confirm commit, error, new-pet, and duplicate audio feedback with production audio enabled.
- [x] Project owner approved the Slice 4 visual direction (`LGTM`, 2026-08-27).

## 7. Acceptance Gate

Slice 4 is accepted when the project owner approves the preview/confirmation/result visuals, the functional and abuse scenarios pass against a real hydrated session, the supported Windows viewport matrix is complete, and transaction feedback is verified through visual plus audio channels.
