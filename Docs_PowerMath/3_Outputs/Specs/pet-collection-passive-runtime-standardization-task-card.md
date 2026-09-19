---
slug: pet-collection-passive-runtime-standardization
status: needs-human
source: manual
gdd_tags:
  - combat-stats
  - economy
  - pet-system
  - gacha
  - server-authority
  - feedback
owner: project-owner
human_checkpoint: required
next_agent: human
blocked_by:
  - trusted-pet-command-authority-decision
  - final-pet-follower-animation-brief
---

# Task Card: Pet Collection and Passive Runtime Standardization

## Player-Facing Goal

Every valid 180-PC pull permanently improves the player's pet collection. Duplicate pets appear once in inventory with an `xN` count, all authored per-copy stats stack, equipped pet choice remains cosmetic, and each unlocked SSR passive resolves predictably without duplicate rewards, refresh exploits, or pet-name-specific code. The equipped pet is visible beside the player and can later receive approved idle, Follow-Up, and Counter-Attack presentation.

## Source

- Origin: Manual prompt on 2026-09-17.
- References: `GDD_PowerMathProject.md` and `Math_World Database - Pet Database.csv`.
- Requested by: Project owner.

## GDD Reference

- `@tag:combat-stats` - Every owned pet copy contributes account-wide; equip is cosmetic.
- `@tag:economy` - One pet pull costs exactly 180 Power Coins.
- `@tag:pet-system` - Copies stack; SSR passives are account-wide; duplicate SSR passives do not duplicate by default.
- `@tag:gacha` - Probability redistribution preserves rarity totals and each result increments a copy count.
- `@tag:server-authority` - Currency, gacha, ownership, combat triggers, and rewards are authoritative and idempotent.
- `@tag:feedback` - Pet actions and heart changes require distinct visual/audio feedback.

## Type

- [x] Feature
- [x] Refactor
- [x] Bug fix
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

The router selects `/implement-feature` because current behavior changes; `/refactor` alone forbids behavior changes.

## Scope

### Systems Affected

- Canonical pet definitions and catalog validation.
- Gacha pricing, receipts, copy-count updates, and reconnect recovery.
- Player inventory projection and cosmetic equip validation.
- Account-wide stat aggregation and formula/clamp policy.
- Generic SSR passive definitions, stack rules, trigger evaluation, and saved runtime state.
- Combat resolution and semantic pet-action/heart-change presentation events.
- Firebase player schema, migration, command validation, and authority boundary.
- EditMode, persistence, combat, and presentation contract tests.

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Pets/`
- `Assets/Project/Script/Gameplay/Combat/Core/`
- `Assets/Project/Script/Gameplay/Progression/`
- `Assets/Project/Script/Gameplay/Academic/`
- `Assets/Project/Script/PlayerData/`
- `Assets/Project/Script/Session/`
- `Assets/Project/Script/UI/MainMenu/`
- `Firebase/firestore.rules`
- `Docs_PowerMath/3_Outputs/ADRs/010-direct-firestore-pet-gacha-prototype.md`
- `Docs_PowerMath/3_Outputs/ADRs/013-canonical-pet-definitions-loadout-and-shared-main-menu-feedback.md`

### Out of Scope

- Final pet follower art, animation timing, audio, VFX, and heart gain/loss animation content until the owner supplies the promised brief.
- New passive balance values not present in the GDD or supplied CSV.
- Firebase deployment, secrets, dependency changes, CI/build settings, or security-rule publication without explicit approval.
- Pet leveling, merging, sacrifice, duplicate conversion, or equip-based power.
- Pull pack sizes other than the approved 1x and 10x products.

## Acceptance Criteria

- [x] A pull costs exactly 180 PC and allowed pack sizes are validated at the domain boundary.
- [x] A 10x costs exactly 1,800 PC, returns ten ordered receipt items, and guarantees at least one SR-or-better result.
- [x] SSR hard pity triggers on the 90th individual pull across 1x/10x transactions and resets immediately on any SSR.
- [x] Firebase stores one canonical count per pet ID; legacy duplicate rows migrate by checked summation.
- [x] Inventory renders one tile per pet definition and an `xN` count.
- [x] Two Furbo copies contribute exactly +14 flat Player ATK.
- [x] Changing or clearing the equipped cosmetic pet does not change any stat or active passive.
- [ ] All authored per-copy stats use one documented formula and data-defined clamps.
- [x] SSR passive stacking is data-defined; the shipping GDD rule is one active passive instance per unique SSR definition while duplicate copies still add Main Stat.
- [x] Core enums and runtime fields describe trigger/effect semantics, never a specific pet name.
- [ ] Passive progress is scoped, idempotent, saved with combat state, and validated against current owned copy counts.
- [x] A Follow-Up action cannot recursively trigger another Follow-Up action.
- [ ] Follow-Up carryover, Counter-Attack order, heart gain/loss, and reconnect recovery have semantic presentation events.
- [ ] A modified client cannot grant a pet, increase count, spend a forged amount, or execute an unowned passive in the production authority design.
- [ ] Existing gacha probability conservation, combat, run settlement, profile, and leaderboard behavior does not regress.
- [ ] Works with WebGL pointer/touch and reduced-motion settings.
- [ ] Follows `RULES_AND_POLICY.md`.

## Human Decisions

1. Approved 2026-09-17: use generic `MainStatEffect` and treat the supplied CSV as the approved per-pet content table.
2. Approve a trusted Firebase command backend for production exploit resistance. Keeping direct anonymous Firestore can improve consistency but cannot prove ownership or prevent a modified client from forging state.
3. Superseded 2026-09-17: owner approved 10x for 1,800 PC, one SR-or-better guarantee per pack, and SSR hard pity on the 90th individual pull (nine 10x packs). Every result and pity delta must be persisted in the idempotency receipt.
4. Approved 2026-09-17: default SSR passive stack rule is `UniquePerDefinition`; future `PerCopy` or capped rules remain schema capability only until separately balanced.
5. Approved 2026-09-18: `petPresentation` is the equipped cosmetic follower, idles beside the player, and serially charges the enemy for Follow-Up. A Follow-Up carried past a player kill resolves against the next valid combat enemy before Idle-Ready; its kill performs normal Stage progression but grants no additional Rank Currency or audit credit.

## Human Checkpoints

- [x] Design/content-source resolution (owner `LGTM`, 2026-09-17)
- [x] Architecture and ADR-021 approval (owner `LGTM`, 2026-09-17)
- [ ] Trusted backend/deployment approval before any Firebase deployment
- [x] Follow-Up follower animation/state brief approval (owner, 2026-09-18)
- [ ] Counter-Attack and heart animation detail approval
- [ ] PR review before merge
- [ ] Team status publish approval

## Router Decision

- Workflow: `/implement-feature`
- Current artifact: verified local implementation; trusted backend and final presentation remain gated
- Next agent: human
- Human checkpoint: trusted backend deployment and final presentation brief remain required

## Context Pack

- `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md` at `@tag:combat-stats`, `@tag:economy`, `@tag:pet-system`, `@tag:gacha`, `@tag:server-authority`, and `@tag:feedback`.
- `Docs_PowerMath/3_Outputs/Specs/pet-collection-passive-runtime-standardization-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/pet-collection-passive-runtime-standardization-impact-analysis.md`
- `Docs_PowerMath/3_Outputs/Specs/pet-collection-passive-runtime-standardization-arch-plan.md`
- `Docs_PowerMath/3_Outputs/ADRs/021-data-driven-pet-collection-passive-runtime.md`
