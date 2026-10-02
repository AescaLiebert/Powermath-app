---
slug: hub-pet-preview-stat-labels
status: completed
source: manual
gdd_tags:
  - player-hub
  - pets
  - localization
owner: implementation-agent
human_checkpoint: not-required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Player Hub Pet Preview Stat Labels

## Goal

Add localized text labels (e.g. `+10 พลังโจมตี` / `+10 ATK`, `+5% โชคลาศ` / `+5% Luck`, etc.) to the Pet Preview stat display in the Player Hub (`player-hub-pet-preview`), matching the presentation used by Pet Detail in the Pet Gacha inspect modal (`pet-gacha-inspect-popup`).

## Requirements

1. **Localized Stat Value Labels**:
   - In `PlayerHubView.cs`, update `FormatPetStat` to use `LocalizationService.Get(...)` with keys from `Assets/Project/Resources/Localization/UI.json`:
     - `menu.petStatPlayerAttack` (`+{0} ATK` / `+{0} พลังโจมตี`)
     - `menu.petStatPlayerAttackPercent` (`+{0:0.#}% ATK` / `+{0:0.#}% พลังโจมตี`)
     - `menu.petStatPetAttack` (`+{0} Pet ATK` / `+{0} พลังโจมตีสัตว์เลี้ยง`)
     - `menu.petStatPetAttackPercent` (`+{0:0.#}% Pet ATK` / `+{0:0.#}% พลังโจมตีสัตว์เลี้ยง`)
     - `menu.petStatCritRate` (`+{0:0.#}% Crit Rate` / `+{0:0.#}% อัตราคริติคอล`)
     - `menu.petStatCritDamage` (`+{0:0.#}% Crit Damage` / `+{0:0.#}% ความเสียหายคริติคอล`)
     - `menu.petStatEncounterLuck` (`+{0:0.#}% Luck` / `+{0:0.#}% โชคลาศ`)
     - `menu.petStatPowerCoins` (`+{0:0.#}% Money Bonus` / `+{0:0.#}% อัตรารวย`)
     - `menu.petStatHeart` (`+{0} Heart` / `+{0} หัวใจ`)
     - `menu.collectionPet` (`Collection Pet` / `สัตว์เลี้ยงสะสม`)
   - Preserve multiplication by `safeCount` when player owns multiple copies.
2. **Stat Section Header**:
   - In `PlayerHubPanel.uxml`, rename the duplicate `player-hub-pet-preview-description` (line 164) to `player-hub-pet-preview-stat-title` with class `loc-menu.petStat player-hub-pet-stat-title`.
   - In `PlayerHubPanel.uss`, style `.player-hub-pet-stat-title` to match `.player-hub-pet-passive-name` (22px bold navy).
   - In `PetGachaPanel.uxml`, add `class="loc-menu.petStat player-hub-pet-stat-title"` to `pet-gacha-inspect-stat-title`.
3. **Synchronization**:
   - Route `PetGachaPanelController.FormatPetStat` through `PlayerHubView.FormatPetStat(pet, 1)` to maintain single-source-of-truth across UI panels.
4. **Verification**:
   - Add unit tests verifying `PlayerHubView.FormatPetStat` across Thai and English locales, multiple copies, and all pet stat variants.
