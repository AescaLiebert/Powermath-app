---
slug: compact-stat-multiplier-bugfix
status: approved
source: manual
gdd_tags:
  - player-hub
  - progression
  - stats
owner: implementation-agent
human_checkpoint: not-required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Compact Stat Multiplier & Presentation Bugfix

## Goal

Resolve stat calculation and presentation bugs in the PlayerHub compact stats panel:
1. Make the player ATK multiplier formula **additive** rather than compounding:
   `EffectiveAttack = (WeaponATK + PetFlatATK) * (1 + (PetMultiplier% + Legacy%))`
   Preventing exponential bloat where legacy ATK bonus was multiplied by pet multiplier.
2. Ensure Compact Stat ATK bonus percentage `(+additional_num%)` accurately represents the combined multiplier:
   `LegacyBasisPoints / 100d + PetMultiplierPercent`
   Excluding flat weapon ATK and flat pet ATK so it matches the Rebirth ATK panel (`+315%` when pet multiplier is 0%).
3. Remove Weapon Ascension CR and CD bonuses from `(+additional_num%)` in Compact Stat CR and CD:
   Only display `(+additional_num%)` when a pet provides a bonus to CR (`TotalCritRatePercent`) or CD (`TotalCritDamagePercent`).
   Weapon ascension bonuses remain part of the base/total percentage display.

## Changes Implemented

### Domain Policies
- `Assets/Project/Script/Gameplay/Progression/PlayerStatProjection.cs`:
  - Recalibrate `EffectiveAttack` calculation to additive multiplier: `(1d + (petMultiplierPercent / 100d) + (legacyBasisPoints / 10000d))`.

### UI Presentation
- `Assets/Project/Script/UI/MainMenu/RunEconomy/PlayerHubPanelController.cs`:
  - Update `CompactAttack` to calculate `additionalAtkPercent` as `(stats.LegacyBasisPoints / 100d) + stats.PetMultiplierPercent`.
  - Update `CompactCritRate` to only show `(+additional_num%)` from `stats.PetStats.TotalCritRatePercent`.
  - Update `CompactCritDamage` to only show `(+additional_num%)` from `stats.PetStats.TotalCritDamagePercent`.

### Unit Tests
- `Assets/Project/Tests/EditMode/Editor/PlayerHubDataTests.cs`:
  - Update compact stat tests to verify additive multiplier behavior, matching Rebirth bonus, and pet-only `(+additional_num%)` for CR and CD.
