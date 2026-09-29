---
slug: teleport-reward-penalty-adjustment
status: ready-for-review
gdd_tags:
  - economy
  - run-reset
  - admin-tools
owner: implementation-agent
human_checkpoint: not-required
next_agent: code-review-agent
blocked_by: []
---

# DevLog: Teleport Reward Penalty Adjustment (10% to 100%)

**Date:** 2026-09-29  
**Status:** Implemented  

## Context

Previously, teleporting during an active run marked `activeRun.wasTeleported = true` and imposed a 90% penalty (`TeleportPenaltyMultiplier = 0.10d`), reducing run settlement awards (Legacy ATK basis points and Power Coins) down to 10%. Because stage teleportation is currently an admin/developer tool only, this penalty hindered debugging and testing endgame settlement flows, economy balance, and rebirth milestones.

## Changes

- **Run Settlement Domain Policy:**
  - In `Assets/Project/Script/Gameplay/Progression/RunSettlementPolicy.cs`, updated `TeleportPenaltyMultiplier` from `0.10d` to `1.0d` (granting 100% of settlement rewards).
- **Run Settlement UI Presentation:**
  - In `Assets/Project/Script/UI/MainMenu/RunEconomy/RunSettlementPanelController.cs`, guarded `_status.text = LocalizationService.Get("menu.teleportReducedRewards")` with `RunSettlementPolicy.TeleportPenaltyMultiplier < 1.0d` so that players and admins do not see a misleading "rewards reduced to 10%" warning when full rewards are granted.
- **Unit Tests:**
  - In `Assets/Project/Tests/EditMode/Editor/RunSettlementPolicyTests.cs`, updated `Calculate_WhenTeleportedToStage180_GrantsFullRewards` to verify that teleported runs receive 100% of Legacy ATK and Power Coins.
- **GDD & Documentation:**
  - In `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`, updated Section 7 (Teleport Settlement Reward Policy) to reflect 100% rewards while teleportation remains an admin-only feature.
  - Added task card `Docs_PowerMath/3_Outputs/Specs/teleport-reward-penalty-adjustment-task-card.md`.

## Verification

- `git diff --check` confirmed no whitespace or syntax formatting issues.
- Calculation logic verified: `stage * 50L * 1.0d` and `powerCoins * 1.0d` equal full unpenalized rewards.
- UI status text maintains clean display without erroneous reduction warnings.
