---
slug: pet-gacha-system
status: approved
source: manual
gdd_tags:
  - core-loop
  - combat-stats
  - economy
  - gacha
  - server-authority
  - feedback
owner: codex
human_checkpoint: required
next_agent: game-design-agent
blocked_by: []
---

# Task Card: Pet Gacha System

> Design direction approved by the project owner on 2026-08-13 (`lgtm`). The approved implementation slice ends at permanent ownership and profile projection; pet equip/loadout and Pet ATK remain separate.

## Player-Facing Goal

Let a student spend 25 permanent Power Coins from the Main Menu to reveal one pet through a transparent, server-authoritative gacha pull. Before committing, the student can inspect the exact current per-pet probabilities and understand that an owned pet can be rolled again for no additional reward.

## Source

- Origin: Manual prompt
- Reference: `I want to move on to implement pet gacha system. @GDD_PowerMathProject.md`
- Requested by: Project owner

## GDD Reference

- `@tag:core-loop` - The Lobby can spend Power Coins through Pet Gacha.
- `@tag:combat-stats` - Equipped pets can contribute Pet ATK and other clamped player stats.
- `@tag:economy` - One pull costs exactly 25 Power Coins; only Weapon Ascend and Pet Gacha spend Power Coins.
- `@tag:gacha` - Preserve each rarity's total rate, halve an owned pet's base share while unowned pets exist in that rarity, restore equal odds when the rarity is complete, and make duplicates empty results.
- `@tag:server-authority` - Gacha, currencies, and unlocks are authoritative, saved, and idempotent.
- `@tag:feedback` - Significant actions require visual and audio feedback while protecting response and clarity.

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- Main Menu gacha entry and pull presentation
- Pet catalog and rarity probability calculation
- Permanent pet ownership
- Power Coin spending
- Idempotent persistence and reconnect recovery
- Player snapshot/projection needed to show balance and owned state

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Progression/`
- `Assets/Project/Script/PlayerData/`
- `Assets/Project/Script/UI/MainMenu/`
- `Assets/Project/UI/MainMenuUI.uxml`
- `Docs_PowerMath/3_Outputs/ADRs/006-direct-firestore-player-question-and-youtube-boundary.md`
- `Docs_PowerMath/3_Outputs/ADRs/007-direct-firestore-social-profile-projection.md`
- `Docs_PowerMath/3_Outputs/ADRs/008-atomic-run-settlement-and-weapon-ascension.md`

### Out of Scope

- Pet leveling, merging, duplicate conversion, pity, wishlists, or multi-pulls
- Buying Power Coins or spending Rank Currency
- Authoring final pet art, audio, rarity rates, catalog membership, or stat balance without human-approved content
- Pet equip/loadout and combat-stat application unless explicitly added at the design checkpoint
- Challenger League pet effects, which remain disabled by the GDD

## Acceptance Criteria

- [ ] The UI shows the current Power Coin balance, fixed 25-Power-Coin cost, exact current per-pet probabilities, owned state, and empty-duplicate rule before confirmation.
- [ ] The probability calculator preserves each configured rarity category total and follows the owned/unowned redistribution formula in `@tag:gacha`.
- [ ] A rarity with every pet owned returns to its original equal distribution.
- [ ] Confirming a valid pull spends exactly 25 Power Coins and resolves exactly one server-authoritative pet result.
- [ ] A new-pet result permanently adds ownership; a duplicate does not modify the owned pet or grant compensation.
- [ ] Insufficient balance cannot start or spend on a pull.
- [ ] Repeated input, retry, refresh, and reconnect cannot double-spend, reroll, or grant twice for the same transaction ID.
- [ ] New-pet and duplicate outcomes each communicate the result with visual and audio feedback, with an accessible reduced-motion presentation.
- [ ] The system works with WebGL pointer input and mobile-compatible touch targets.
- [ ] Automated tests cover probability conservation, ownership combinations, duplicate handling, insufficient funds, and idempotency.
- [ ] Existing run settlement, Weapon Ascend, combat, profile, and leaderboard behavior does not regress.
- [ ] Follows `RULES_AND_POLICY.md`.

## Human Checkpoints

- [x] Design/game-feel approval
- [x] Prototype rarity rates and 15-pet catalog approved for implementation on 2026-08-13; pet stats remain deferred and the shared icon remains placeholder content
- [x] Equip/loadout and Pet ATK application are deferred to a separate implementation slice
- [x] Architecture approval - direct-Firestore prototype path accepted on 2026-08-13
- [ ] PR review before merge
- [ ] Team status publish approval

## Router Decision

Recommended workflow: `/implement-feature`

Next artifact:

- `Docs_PowerMath/3_Outputs/Specs/pet-gacha-system-design-spec.md`
