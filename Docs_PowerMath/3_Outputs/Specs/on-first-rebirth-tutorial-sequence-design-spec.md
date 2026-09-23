---
slug: on-first-rebirth-tutorial-sequence
status: approved
source: manual and MathWorld VA Script
gdd_tags:
  - tutorial-system
  - run-reset
  - economy
  - gacha
  - pet-system
owner: game-design-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# On First Rebirth Tutorial Sequence Design

## 1 Player Experience

After the player first reaches the Rebirth moment, Power explains the permanent gain before asking for the action. A defeat starts with comfort, while an optional Stage-30 Rebirth begins immediately with the permanent-growth explanation. Once the reset is safely complete, the player spends the tutorial grant on a first companion, upgrades the sword, and learns where duplicate companions add lasting value.

The sequence is one story arc. The player never receives an unearned pet, upgrade, or result: all changes happen through the normal Rebirth, Gacha, and Weapon Ascend commands, and Power waits for each confirmed result.

## 2 Exact Voice and Interaction Order

| Order | Trigger or required completion | Spoken cue | Required state |
|---:|---|---|---|
| 1 | First death opens Rebirth Die window | `S4_01_VA` | Before restart confirmation |
| 2 | Player advances | `S4_02_VA` | Death route only after S4_01; voluntary Rebirth route begins here |
| 3 | Player advances | `S4_03_VA` | Highlight Legacy ATK and Power Coin panel |
| 4 | Death route only | `S4_04_VA` | Highlight `RebirthBtn` |
| 5 | Rebirth/restart settles and Stage 1 is authoritative | `S4_05_VA` | Death route only |
| 6 | Player advances | `S5_01_VA` | Highlight `PlayerMenu/Pet-GachaBtn` |
| 7 | Pet Gacha panel opens | `S5_02_VA` | Gacha panel visible |
| 8 | Player advances | `S5_03_VA` | Gacha panel visible |
| 9 | Player advances | `S5_04_VA` | Gacha panel visible |
| 10 | Player advances | `S5_05_VA` | Grant +180 PC once; highlight one pull then its confirmation |
| 11 | Normal gacha transaction and Pet Reveal finish | `S5_06_VA` | Result is authoritative |
| 12 | Player returns to Main Menu | `S6_01_VA` | Highlight `PlayerMenu/PlayerHubBtn` |
| 13 | Player Hub opens | `S6_02_VA` | Weapon area visible |
| 14 | Player advances | `S6_03_VA` | Highlight `AscendBtn` |
| 15 | Normal Weapon Ascend succeeds | `S6_04_VA` | Upgrade result is authoritative |
| 16 | Player advances | `S6_05_VA` | Highlight `PetTabMenu` |
| 17 | Pet tab opens | `S6_06_VA` | Pet collection visible |
| 18 | Player advances | `S6_07_VA` | Complete `OnFirstRebirth` |

The Thai dialogue and localization-key proposal for each cue are the recording authority in `Docs_PowerMath/3_Outputs/Voice_Acting/powermath-thai-tutorial-voice-script.md`.

## 3 Interaction Flow

```text
First death opens Rebirth Die window
  -> S4_01 -> S4_02 -> S4_03 -> S4_04
  -> player invokes ordinary Rebirth/restart command
  -> settlement commits -> Stage 1 is restored -> S4_05

Eligible Stage-30 Rebirth panel opens
  -> S4_02 -> S4_03
  -> player retains ordinary confirm or close choice
  -> confirmed Rebirth settles -> Stage 1 is restored

First confirmed Rebirth at Stage 1
  -> S5_01 -> Pet Gacha open -> S5_02 -> S5_03 -> S5_04 -> S5_05
  -> one normal 180-PC pull -> Pet Reveal -> S5_06 -> Main Menu
  -> S6_01 -> Player Hub -> S6_02 -> S6_03
  -> normal Weapon Ascend -> S6_04 -> S6_05 -> Pet tab -> S6_06 -> S6_07
  -> save complete
```

## 4 Feedback Loop

| Trigger | Visual | Voice | Completion gate |
|---|---|---|---|
| Rebirth benefit explanation | Focus cutout around full Legacy ATK and Power Coin panel | `S4_03_VA` | Player advances |
| Rebirth action | Focus cutout around `RebirthBtn` | `S4_04_VA` | Accepted standard command |
| Tutorial grant | Existing +180 Power Coin animation | `S5_05_VA` | Grant receipt committed once |
| First pull | Existing single-pull focus and confirm flow | No new narration while transaction resolves | Pet Reveal complete |
| Sword upgrade | Existing ascend feedback | `S6_04_VA` after success only | Upgrade receipt committed |
| Duplicate explanation | Pet tab remains visible | `S6_06_VA` | Player advances |

## 5 Presentation Rules

- Use the existing tutorial overlay, focus layer, tap-to-complete typewriter behavior, dedicated Voice channel, and music ducking.
- A focus step blocks unrelated input but invokes the normal target action when accepted.
- Dialogue resumes only after the relevant panel, receipt, reveal, or Stage 1 state is visible.
- Reduced Motion retains every dialogue, focus, and result gate; it suppresses only large entrance and movement effects.
- Voice clips are loaded by exact `S4_XX_VA`, `S5_XX_VA`, and `S6_XX_VA` cue IDs from `Assets/Project/Resources/Voice/`.

## 6 Edge Cases

- Closing optional Rebirth without confirming never grants +180 PC and never starts Session 05.
- Refreshing during dialogue resumes the saved incomplete step; refreshing during a transaction restores the confirmed receipt before dialogue resumes.
- Repeated focus taps accept only one command.
- Gacha or Weapon Ascend failure leaves the current step pending, shows the existing recoverable error, and never advances to a result line.
- Insufficient funds is not authored for the forced first Weapon Ascend path because the tutorial reward budget must make it affordable; a live balance regression still must leave the step pending rather than fake success.
- If the account already owns the guaranteed Follow-Up SSR, the normal pull stays authoritative and its real result is shown; the existing owned Follow-Up is used for the later demonstration as defined by the GDD.

## 7 GDD Alignment Check

This design follows `@tag:tutorial-system` for one active tutorial, saved progress, real production commands, authoritative result waits, focus locking, reconnect recovery, and reduced-motion coverage. It follows `@tag:run-reset`, `@tag:economy`, and `@tag:pet-system` for Stage-30 Rebirth, the separate one-time +180 PC grant, a normal 180-PC pull, and permanent duplicate value.

The GDD currently begins `OnFirstRebirth` only after the Stage 1 settlement. The owner approved the requested pre-settlement `OnFirstRebirthUnlock` extension with `lgtm` on 2026-09-22. The required playtest should verify that the death route comforts without delaying recovery, and that the voluntary route explains the permanent reward without making Rebirth feel compulsory.
