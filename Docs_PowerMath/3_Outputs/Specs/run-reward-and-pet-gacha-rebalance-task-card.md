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
1. Retain Pet Gacha pull cost at **180 PC**.
2. Retain the flat stage reward upon run settlement at **1 PC per cleared stage**.
3. Recalibrate the weighted completion factor with divisor **180,000,000L** (`numerator / 180000000L`), targeting an endgame ratio of **100 Diamond ≈ 10,000 PC** at Stage 190–200 and capping multi-hit boss runs (e.g. 334+ questions at Stage 190) around **~30,000 PC**.
4. Retain `OnFirstRebirth` tutorial grant at **180 PC** so the guaranteed starter Follow-Up SSR pull remains affordable.

## Changes Implemented

### Domain Policies
- `Assets/Project/Script/Gameplay/Pets/Core/PetGachaRoller.cs`:
  - `PetGachaTransactionPolicy.PullCost = 180;`
- `Assets/Project/Script/Gameplay/Progression/RunSettlementPolicy.cs`:
  - `flatStageCoins = checked(stage * 1L);`
  - `weightedCoins = numerator / 180000000L;`

### Unit Tests
- `Assets/Project/Tests/EditMode/Pets/PetGachaCoreTests.cs`:
  - Pull cost assertions and insufficient funds boundary checks verified at 180 PC.
- `Assets/Project/Tests/EditMode/Editor/RunSettlementPolicyTests.cs`:
  - Updated Stage 12 expected settlement award to 13 PC (12 flat + 1 weighted).
  - Updated Stage 30 expected settlement award to 41 PC (30 flat + 11 weighted with mock run inputs).
  - Updated Stage 30 pet collection test award to 225 PC (45 scaled base + 180 passive).
  - Updated Stage 200 expected settlement award to 21,816 PC (200 flat + 21,616 weighted with 200 Diamond).

### Documentation
- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`:
  - Synchronized Pet Gacha cost, Run-Reward formula (CompletionFactor divisor = 18,000, Flat = 1 PC, two-tier depth bonus), and late-stage target range (19k–22k PC).

## Checkpoints
- [x] Domain policies updated and verified
- [x] Unit tests updated and synchronized
- [x] GDD documentation updated
