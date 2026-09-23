---
slug: on-first-rebirth-tutorial-sequence
status: implementation-complete-pending-editor-validation
source: manual and MathWorld VA Script
gdd_tags:
  - tutorial-system
  - run-reset
  - economy
  - gacha
  - pet-system
owner: project-owner
human_checkpoint: required
next_agent: qa-agent
blocked_by: []
---

# Task Card On First Rebirth Tutorial Sequence

## Goal

Deliver Sessions 04, 05, and 06 as one ordered first-Rebirth onboarding journey with an exact Thai voice-actor recording list. It teaches why Rebirth grants permanent value, then guides the player through a one-time Pet Gacha pull and one Weapon Ascend before returning them to play.

The approved recording order is maintained in `Docs_PowerMath/3_Outputs/Voice_Acting/powermath-thai-tutorial-voice-script.md` as takes 21–38, mapped directly to the imported `S4_01_VA`–`S4_05_VA`, `S5_01_VA`–`S5_06_VA`, and `S6_01_VA`–`S6_07_VA` clips.

## Requested Runtime Flow

```text
OnFirstRebirthUnlock
  death opens Rebirth Die window -> 21 -> 22 -> 23 -> focus RebirthBtn -> settle -> 25
  Stage 31+ (after Stage 30 is beaten) opens Rebirth panel -> 22 -> 23 -> player may confirm normally
  authoritative Rebirth settlement returns Stage 1
    -> 25 (for either the death or voluntary route)
    -> OnFirstRebirth Session 05 -> Pet Gacha pull -> Pet Reveal -> Main Menu
    -> Session 06 -> Player Hub -> Weapon Ascend -> Pet tab -> final dialogue
```

## Current State

- The authored catalog now contains `OnFirstRebirthUnlock` and `OnFirstRebirth`, alongside the existing sequences.
- `FirstRebirthTutorialAdapter` converts panel-open and authoritative settlement, reveal, ascend, and tab-completion receipts into persisted semantic tutorial transitions.
- Takes 21–38 resolve to the imported cue IDs and exact Thai localization text. The +180 Power Coin grant is a one-time, persisted lifecycle command with existing reward feedback.

## Target State

1. Add a pre-settlement `OnFirstRebirthUnlock` presentation with the death and voluntary Stage-30+ entry variants described in the voice sheet.
2. Start `OnFirstRebirth` only after an authoritative Rebirth settlement has returned the player to Stage 1.
3. Keep Pet Gacha, the +180 Power Coin tutorial grant, one-pull confirmation, Pet Reveal, Weapon Ascend, and Pet tab interactions on their ordinary validated production commands.
4. Treat Session 06 as the final chapter of `OnFirstRebirth`, avoiding a second root tutorial and queue race.
5. Persist every eligible, active, waiting, and completed step with existing tutorial-map semantics; grant neither currency nor rewards from presentation state alone.

## GDD Alignment and Decision Needed

The canonical GDD currently describes one `OnFirstRebirth` sequence beginning after the first death or optional Rebirth has already settled at Stage 1. The requested `OnFirstRebirthUnlock` adds a new pre-settlement sequence and changes when the Rebirth explanation occurs. That is a product-design change and requires an owner decision before assets, state-machine triggers, localization, or runtime code are changed.

The requested flow otherwise preserves the GDD requirements for Stage-30 Rebirth eligibility, Stage-1 return after settlement, the separate one-time +180 Power Coin tutorial grant, one normal 180-PC Pet Gacha purchase, authoritative result waits, and Player Hub guidance.

## Acceptance Criteria After Approval

- [x] Death route plays takes 21–25 in the documented order, without covering an unsettled terminal transaction.
- [x] Voluntary Rebirth at Stage 30+ plays reused takes 22–23 when the panel opens, retains a normal user-confirmed Rebirth choice, and follows an authoritative settlement with take 25.
- [x] The post-settlement sequence cannot start before Stage 1 is authoritative and its predecessor is complete.
- [x] The +180 PC grant is idempotent, and the tutorial uses the real Pet Gacha and Weapon Ascend commands/results.
- [x] Pet Reveal, return to Main Menu, upgrade success, and Pet tab open are real completion gates, not timed assumptions.
- [x] All takes 21–38 resolve to exact approved Thai localization strings and imported audio cues.
- [ ] Tutorial completion persistence still needs an in-Editor smoke test for refresh/reconnect coverage.

## Human Checkpoint

Approve whether the pre-settlement `OnFirstRebirthUnlock` branch becomes canonical GDD behavior, including whether a player who closes the voluntary Rebirth panel before confirming should resume that incomplete explanation on the next eligible panel open.
