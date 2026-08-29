# Player Hub Data and Presentation Polish Test Plan

## Automated Results — 2026-08-29

- Unity script compilation: passed with zero feature errors.
- `PowerMath.Gameplay.Combat.UnityEditModeTests`: 25/25 passed.
- `PowerMath.Gameplay.Pets.UnityEditModeTests`: 5/5 passed, including all 15 migrated pet identities/icons.
- Focused Pet ATK and equipped-ownership policy: 2/2 passed.
- Focused shared-back and Player Hub UXML contract: 2/2 passed.
- Targeted `git diff --check`: passed; repository-wide check reports unrelated existing whitespace in scene/TMP assets.

## Known Baseline Failures

- `PetGachaCoreTests.Roll_UsesCategoryBoundaryThenExactPetWeights` expects random value `7000` to select `rare`, while the authored 7000/2700/300 ordering resolves that boundary to `middle`. This test was already present and is outside Player Hub behavior.
- Combat PlayMode: 4/4 scene tests time out at `Combat surface was not ready`. Console evidence shows their hydrated sample player cannot resolve a Firestore level/student document and the scene falls back to unavailable live persistence. No Player Hub exception or compile failure is emitted.

## Manual P0

1. Open Player Hub at desktop and narrow reference resolutions; verify the shared utility bar, environment-left layout, and workspace readability.
2. With two owned pets, tap the unequipped tile once. Verify immediate selected/equipping visuals, provisional avatar companion, blocked competing input, then saved badge/stat update.
3. Simulate a transport failure and stale revision. Verify rollback or authoritative refresh, readable notification, and same-transaction recovery.
4. Tap Weapon Ascend without enough Power Coins. Verify the button responds, shortfall notification appears, and no write begins.
5. Complete normal and tier-changing Ascends. Verify stat pulse, icon bounce, pooled particles, and stronger milestone overlay only on authored tier transitions.
6. Repeat with Reduced Motion. Verify no idle loop/particle burst and all final semantic states remain visible.
7. Open Biome Map, Profile Analytics, Pet Gacha, and Leaderboard. Verify one shared top back/currency row and no duplicate local close affordance.

## Content Checkpoint

All 15 production pet definitions intentionally retain `AttackBonus = 0` and empty ability rich text. Non-zero balance values and gameplay claims require owner-authored content approval.
