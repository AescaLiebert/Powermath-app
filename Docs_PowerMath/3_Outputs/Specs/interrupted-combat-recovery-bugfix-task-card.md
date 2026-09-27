---
slug: interrupted-combat-recovery-bugfix
status: in-review
source: owner report, 2026-09-27
gdd_tags:
  - core-loop
  - stage-progression
  - encounters
workflow: /fix-bug
human_checkpoint: review before merge or live account repair
---

# Interrupted combat recovery bugfix

## Player goal

Leaving or refreshing during a committed question must resolve the attempt once, show the outcome, grant any Challenge reward, advance the Stage when the Challenge flees, and return to an interactive lobby.

## Evidence and root cause

The reported Grade 6 account was read on 2026-09-27. It has Stage 67, a Challenge Event, phase `Committed`, a committed attempt ID, a reserved Challenge question, and no pending presentation. No live data was changed.

Startup recovery resolved this state locally as a timeout and created a Flee receipt. It then played the result and saved only `PresentationCompleted`. The persistence validator permits a Challenge Power Coin increase only on `AttemptResolved`. Thus the acknowledgement is rejected after the Flee animation, while the presentation interaction lock remains held. The saved stage and reward never advance.

## Fix scope

- Add a distinct `InterruptedAttemptResolved` save point for the synthetic timeout receipt.
- Save Stage, wallet reward, question cursor, and pending presentation together before playing recovery.
- Reconstruct a committed Rank question reservation and record its timeout in the Rank audit and analytics before saving normal combat recovery.
- Acknowledge the presentation in a second save; the first save makes refresh during the animation repeatable.
- Validate receipt, attempt identity, source encounter, destination, and wallet amount before the first save.
- Release interaction locks and show an unavailable state if any authoritative save fails.

## Regression cases

- Refresh during a committed or open Challenge question: one Flee, one reward, one Stage advance, next encounter ready.
- Refresh after the recovery save and before presentation acknowledgement: replay the saved receipt without another reward or Stage advance.
- Refresh after acknowledgement: load the ready destination directly.
- Normal enemy interrupted attempt: resolve timeout without a Challenge reward, then return to ready or defeat state.
- Interrupted Rank question: record one timeout and one audit result, including the fifth-result Rank boundary.
- Save conflict/network failure: display failure and allow navigation/reload; do not permit another attack on unsaved local state.
- Corrupt or mismatched receipt: reject or discard safely without applying a duplicate reward.

## Checkpoints

No Firebase document, rules, deployment, build settings, or secrets were changed. Live account repair and release require owner review.

## Verification on 2026-09-27

- Focused interrupted Challenge, Rank, and UI settlement EditMode tests passed.
- Firestore academic save guard class: 18 passed, 0 failed.
- Academic core class: 49 passed, 0 failed.
- Broader combat core namespace: 122 passed, 3 failed in damage, rank currency, and pet answer expectation cases outside this recovery path. These remain open for separate review.
- Production WebGL reconnect and live-account recovery have not been run; the account remains at its saved committed Stage 67 until this fix is released or a separately approved repair is made.
