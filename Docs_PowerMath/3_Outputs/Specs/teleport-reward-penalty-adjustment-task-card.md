---
slug: teleport-reward-penalty-adjustment
status: approved
source: manual
gdd_tags:
  - economy
  - run-reset
  - admin-tools
owner: game-design-agent
human_checkpoint: not-required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Teleport Reward Penalty Adjustment

## Goal
Adjust the teleport settlement reward multiplier from 10% (`0.10`) to 100% (`1.0`) so that teleportation (currently an admin-only testing tool) awards full Power Coins and Legacy ATK upon Rebirth or Defeat settlement.

## Context
Stage teleportation is currently an administrative and QA tool accessed via the Admin section in Settings (`FirestoreAdminTuningService`). When invoked, it sets `activeRun.wasTeleported = true`. Previously, `RunSettlementPolicy` imposed a 90% reward reduction (`TeleportPenaltyMultiplier = 0.10d`). Since public waypoint systems are not active and teleportation is restricted to admin testing, this penalty needlessly obstructed administrative economy and balance testing.

## Changes Implemented

### Domain Policies
- `Assets/Project/Script/Gameplay/Progression/RunSettlementPolicy.cs`:
  - Updated `TeleportPenaltyMultiplier` from `0.10d` to `1.0d`.

### UI Presentation
- `Assets/Project/Script/UI/MainMenu/RunEconomy/RunSettlementPanelController.cs`:
  - Updated `RenderPreview` so `menu.teleportReducedRewards` is only presented if `RunSettlementPolicy.TeleportPenaltyMultiplier < 1.0d`.

### Unit Tests
- `Assets/Project/Tests/EditMode/Editor/RunSettlementPolicyTests.cs`:
  - Updated `Calculate_WhenTeleportedToStage180_GrantsFullRewards` to assert that teleported runs receive 100% of normal Legacy ATK basis points and Power Coins.

### Documentation
- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`:
  - Updated Section 7 (Teleport Settlement Reward Policy) documenting the `1.0` multiplier and rationale.

## Checkpoints
- [x] Domain policy updated to 1.0 multiplier
- [x] UI status guard updated
- [x] EditMode unit test updated
- [x] GDD synchronized
