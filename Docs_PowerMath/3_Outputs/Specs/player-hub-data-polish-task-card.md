---
slug: player-hub-data-polish
status: ready-for-review
source: manual
gdd_tags:
  - core-loop
  - combat-stats
  - economy
  - gacha
  - server-authority
  - feedback
  - player-experience
owner: codex
human_checkpoint: required
next_agent: reviewer-agent
blocked_by: []
---

# Task Card: Player Hub Data and Presentation Polish

## Player-Facing Goal

Turn Player Hub into a full-screen equipment home where the student can read current permanent power at a glance, see their avatar standing in the environment, inspect and equip an owned pet, and compare or ascend their weapon without losing the shared Power Coin and exit controls used by other Main Menu destinations.

## Source

- Origin: Manual prompt on 2026-08-29.
- Reference: `Docs_PowerMath/3_Outputs/UI_Mockups/player-hub-icon-driven-v16.png`.
- Requested by: Project owner.

## GDD Reference

- `@tag:core-loop` - The Lobby owns permanent equipment and Power Coin actions.
- `@tag:combat-stats` - Equipped pets and weapons may modify ATK, CR, CD, and maximum hearts through data-defined clamps.
- `@tag:economy` - Power Coins are permanent; Weapon Ascend and Pet Gacha are their only spending systems.
- `@tag:gacha` - Pet identity and permanent ownership already originate from the approved gacha catalog and saved inventory.
- `@tag:server-authority` - Equip and ascend actions must be saved, idempotent, and recoverable.
- `@tag:feedback` - Significant actions require visual and audio feedback.
- `@tag:player-experience` - Response and clarity take priority over spectacle.

## Current Implementation

- Player Hub is a modal card with combat-stat and Weapon Ascension columns.
- `PetGachaCatalogDefinition` owns pet identity, display name, icon, and rarity membership for gacha.
- `PlayerSnapshot.inventory` already persists generic ownership records; `loadout.petId` already stores the equipped pet ID.
- Pet combat contribution is intentionally hard-coded as unconfigured in `PlayerStatProjectionFactory`.
- The current working tree already contains unrelated edits in `PlayerSnapshot`, Player Hub styling, scenes, and other systems; this feature must preserve them.

## Target Slice

### Systems Affected

- Full-screen Player Hub information hierarchy and interaction states.
- Shared Main Menu utility-bar presentation contract.
- Reusable pet definition data referenced by the gacha catalog.
- Pure owned-pet inventory projection over the existing player snapshot.
- Equipped-pet selection and authoritative persistence boundary.
- Pet stat projection into the existing shared combat/UI calculation path.
- Interaction feedback and Reduced Motion behavior.

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Pets/`
- `Assets/Project/Script/Gameplay/Progression/PlayerStatProjection.cs`
- `Assets/Project/Script/PlayerData/PlayerSnapshot.cs`
- `Assets/Project/Script/UI/MainMenu/RunEconomy/PlayerHubPanelController.cs`
- `Assets/Project/UI/MainMenu/PlayerHubPanel.uxml`
- `Assets/Project/UI/MainMenu/PlayerHubPanel.uss`
- `Assets/Project/UI/MainMenu/MainMenuShell.uxml`
- `Docs_PowerMath/3_Outputs/ADRs/010-direct-firestore-pet-gacha-prototype.md`

### Out of Scope

- New pet identities, final pet art, rarity rates, or balance values invented by automation.
- Pet leveling, merging, duplicate compensation, active abilities, or passive-effect execution.
- Weapon inventory redesign; the approved Weapon Ascension path remains intact.
- Migrating every Main Menu destination to the shared utility bar in the same implementation diff.
- Firestore rule publication, backend deployment, build/CI changes, packages, secrets, merge, or release.

## Acceptance Criteria

- [x] Player Hub uses the approved full-screen left-character/right-workspace hierarchy without a single box covering the avatar/environment area.
- [x] The top utility contract exposes Power Coins and one context-aware back/exit action and can be reused by Player Hub, Biome Map, Profile Analytics, Pet Gacha, and Leaderboard.
- [x] One pet definition is the canonical source for pet identity, presentation, and approved stat data; gacha references it rather than duplicating those fields.
- [x] Owned-pet inventory is a projection over `PlayerSnapshot.inventory`, not a second persisted inventory.
- [x] Only owned, catalog-resolvable pets can be equipped; invalid saved IDs fail visibly and do not contribute stats.
- [x] Tapping an owned pet tile immediately selects it and starts one idempotent `loadout.petId` equip action without a second Equip button.
- [x] The equipped pet contributes only approved data-defined stats through the same projection used by combat and Player Hub.
- [x] Weapon Ascension behavior, cost, atomic save, and recovery remain unchanged while idle, insufficient-funds, success, stat-change, icon-bounce, particle, and milestone feedback are added.
- [x] A reusable semantic in-app notification system communicates shortfall, pending, failure, success, and milestone states without becoming gameplay authority.
- [x] Selection/equip, Weapon Ascend, and notification motion use standard USS/LeanTween/pooled-particle animation techniques with a Reduced Motion path.
- [x] Pointer, keyboard, and mobile-compatible touch interactions retain visible focus and selected/equipped states.
- [x] EditMode tests cover inventory validation, stat policy, corrupt ownership, gacha-catalog references, shared navigation, and Player Hub UI contracts. Direct Firestore integration remains a manual test boundary.
- [ ] Existing gacha, run settlement, combat, profile, and leaderboard tests do not regress.

The final regression checkbox remains open because the existing pet probability suite has one unrelated boundary expectation failure and all four Combat PlayMode scene tests currently fail their pre-existing live-Firebase readiness setup. Feature-focused suites and compilation are green; see the test plan and DevLog.

## Human Checkpoints

- [x] Design/game-feel approval - approved by the project owner on 2026-08-29 with instant pet-tile equip, expanded Weapon Ascend juice, image-only equipped-item anchors, and a reusable notification requirement.
- [x] Architecture approval - approved by the project owner on 2026-08-29.
- [ ] Approve authored pet stat/balance content before enabling non-zero production bonuses.
- [ ] PR review before merge.
- [ ] Team status publish approval.

## Router Decision

Recommended workflow: `/implement-feature`

Next artifact after design approval:

- `Docs_PowerMath/3_Outputs/Specs/player-hub-data-polish-arch-plan.md`
