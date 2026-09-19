---
slug: run-reward-and-pet-gacha-rebalance
status: approved
source: manual
gdd_tags:
  - economy
  - run-reset
  - gacha
owner: game-design-agent
human_checkpoint: not-required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Run Reward & Pet Gacha Rebalance

## Goal

Rebalance the Power Coin economy by adjusting the run settlement payout formula and the pet gacha pull cost:
1. Increase Pet Gacha pull cost from 25 PC to **180 PC**.
2. Retain the flat stage reward upon run settlement at **1 PC per cleared stage**.
3. Scale the non-linear / stageReached-dependent completion factor by **16.67×** (`numerator / 1,200,000L` instead of `numerator / 20,000,000L`), calibrated to keep Diamond runs at Stage 50 around 600 PC and Silver Stage 30 runs around 105 PC.
4. Update `OnFirstRebirth` tutorial grant to **180 PC** so the guaranteed starter Follow-Up SSR pull remains affordable.

## Changes Implemented

### Domain Policies
- `Assets/Project/Script/Gameplay/Pets/Core/PetGachaRoller.cs`:
  - `PetGachaTransactionPolicy.PullCost = 180;`
- `Assets/Project/Script/Gameplay/Progression/RunSettlementPolicy.cs`:
  - `flatStageCoins = checked(stage * 1L);`
  - `weightedCoins = numerator / 1200000L;`

### Unit Tests
- `Assets/Project/Tests/EditMode/Pets/PetGachaCoreTests.cs`:
  - Updated pull cost assertions and insufficient funds boundary checks to 180 PC.
- `Assets/Project/Tests/EditMode/Editor/RunSettlementPolicyTests.cs`:
  - Updated Stage 12 expected settlement award to 33 PC (12 flat + 21 weighted).
  - Updated Stage 30 expected settlement award to 83 PC (30 flat + 53 weighted with mock run inputs).

### Documentation
- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`:
  - Synchronized Pet Gacha cost, Run-Reward formula (CompletionFactor = StageReached / 12, Flat = 1 PC), tutorial grant, and guardrails.

## Checkpoints
- [x] Domain policies updated and verified
- [x] Unit tests updated and synchronized
- [x] GDD documentation updated
