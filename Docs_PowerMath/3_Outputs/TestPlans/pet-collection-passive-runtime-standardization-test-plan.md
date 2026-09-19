# Pet Collection and Passive Runtime Standardization Test Plan

## 1. Test Plan Summary

| Test Area | Priority | Type | Platform |
| --- | --- | --- | --- |
| 180-PC single-pull transaction and recovery | P0 | Automated + manual | Editor, WebGL |
| Canonical copy counts and duplicate migration | P0 | Automated + Firebase emulator/manual | Editor, WebGL |
| Collection-wide stat aggregation | P0 | Automated | Pure C# / EditMode |
| Generic passive triggers and saved progress | P0 | Automated | Pure C# / EditMode |
| Cosmetic equip invariance | P0 | Automated + manual | Editor, WebGL |
| Inventory `xN` rendering | P1 | EditMode + manual | Editor, mobile WebGL |
| Heart and pet-action presentation | P1 | Manual after approved visual brief | Editor, mobile WebGL |
| Trusted command authority | P0 | Integration/security | Firebase emulator + deployed staging |

## 2. Functional Tests

```text
TEST: Duplicate Furbo stacks Player ATK
GIVEN: The collection owns Furbo count 2
WHEN: Collection stats are projected
THEN: Player flat ATK contribution is exactly 14 and inventory has one Furbo entry with count 2
PRIORITY: P0
```

```text
TEST: Equip is cosmetic
GIVEN: The same owned collection and two different owned pet IDs
WHEN: Equipped pet changes between those IDs or is cleared
THEN: Every projected stat and active passive is unchanged
PRIORITY: P0
```

```text
TEST: Unique SSR passive with duplicate copies
GIVEN: Four copies of an SSR whose passive stack rule is UniquePerDefinition
WHEN: Active passives are projected
THEN: Main Stat uses four copies and passive magnitude uses one stack
PRIORITY: P0
```

```text
TEST: Auregriff third attack
GIVEN: Auregriff is owned, one enemy survives three successful attacks, and stage progress is unchanged
WHEN: The third successful player attack resolves
THEN: Only the third attack receives the authored +0.25 multiplier and saved stageAttackCount is 3
PRIORITY: P0
```

```text
TEST: Lumirin Big Boss restore
GIVEN: Lumirin is owned, the player is below maximum hearts, and a Big Boss is defeated
WHEN: The encounter resolves
THEN: One heart is restored, never beyond maximum, and a PetPassiveRestore heart event is emitted
PRIORITY: P0
```

```text
TEST: Gacha product boundary
GIVEN: A current catalog and 180 PC
WHEN: One pull is committed
THEN: Exactly 180 PC is deducted and exactly one canonical pet count increments
PRIORITY: P0
```

```text
TEST: Supported pull packs
GIVEN: The current ordered idempotency receipt
WHEN: A command requests 1x, 10x, or another pack size
THEN: 1x costs 180 PC, 10x costs 1,800 PC, and every other pack size is rejected before mutation
PRIORITY: P0
```

```text
TEST: 10x SR-or-better guarantee
GIVEN: The first nine results contain only R pets
WHEN: The tenth result resolves
THEN: It is rolled from the configured SR rarity unless SSR hard pity applies
PRIORITY: P0
```

```text
TEST: 90-pull SSR hard pity and reset
GIVEN: 89 consecutive individual results without an SSR
WHEN: Pull 90 resolves
THEN: It is SSR and the counter resets to zero; a natural earlier SSR performs the same reset
PRIORITY: P0
```

## 3. Edge and Abuse Cases

- [ ] Reject negative, zero canonical, fractional, overflowing, or missing pet counts; legacy missing `count` maps to exactly one only for an owned record.
- [ ] Aggregate legacy duplicate rows with checked arithmetic and canonical case-insensitive pet ID matching.
- [ ] Reject unknown or unowned equipped pet IDs without changing the previous loadout.
- [ ] Re-submit the same gacha transaction ID and verify no second spend or count increment.
- [ ] Disconnect after transaction commit but before UI response; reconnect must recover the same receipt.
- [ ] Attempt a forged catalog version, preview revision, cost, pet ID, count, and passive flag.
- [ ] Confirm a defeated enemy never also attacks and a pet follow-up never recursively triggers another pet action.
- [ ] Obtain a pet while remaining in the active lobby; verify collection stats, maximum hearts, passives, equipped sprite, and future encounter multiplier update without a refresh or enemy reset.
- [ ] Kill an enemy with player damage while Follow-Up is queued; verify the next combat enemy appears, the pet acts before Idle-Ready, and a pet kill advances again without Rank Currency or audit credit.
- [ ] Confirm heart restore at full hearts emits no false gain event.
- [ ] Confirm stage-scoped attack count resets on stage advance and survives refresh within the same stage.
- [ ] Exercise `int.MaxValue` copy counts and reward totals; operations must fail closed on overflow.

## 4. Performance Tests

- [ ] Inventory rebuild allocates by unique pet definition, not total copy count.
- [ ] Passive evaluation has no per-frame polling or `Update` allocations.
- [ ] Combat remains within the existing mobile WebGL frame budget with every shipping SSR passive active.
- [ ] Player document size remains within Firestore limits at maximum catalog size.

## 5. Platform-Specific Tests

| Test | Primary Platform: mobile WebGL | Secondary Platform: Editor/desktop |
| --- | --- | --- |
| Inventory selection | One tap selects/equips; `xN` remains legible | Mouse click has identical result |
| Rapid pull input | Interaction gate permits one transaction | Repeated clicks/Enter cannot double-spend |
| Refresh/reconnect | Same receipt and counts return | Domain reload restores the same state |
| Reduced motion | Future follower/heart effect uses reduced-motion route | Full animation route remains readable |

## 6. Regression Checklist

- [ ] Gacha rarity totals remain exactly 10,000 basis points for every ownership combination.
- [ ] Run settlement PC bonus and Lunamoth +180 rebirth reward are applied once.
- [ ] Existing weapon, rank, critical, and response-score damage calculations remain unchanged.
- [ ] Challenge/Event encounter rewards and flee flow remain unchanged.
- [ ] Player profile migration preserves non-pet inventory rows.
- [ ] Leaderboard and lifecycle writes do not persist derived pet totals as authority.
- [ ] Empty collection and missing optional loadout remain valid.

## 7. Authority and Reconnect Tests

- [ ] Trusted command handler reads authenticated player ownership and current catalog server-side.
- [ ] Client-provided passive availability, RNG result, and count delta are ignored.
- [ ] Currency, count, passive progress, and idempotency receipt commit atomically.
- [ ] Two simultaneous pull commands cannot overspend or lose a copy increment.
- [ ] Direct anonymous Firestore rules cannot mutate authoritative production pet/economy leaves after backend migration.

## 8. Current Verification Record — 2026-09-17

- PASS: Unity imported all changed scripts/assets and completed compilation/domain reload after the prior stale errors.
- PASS: `Assembly-CSharp` compiled independently against Unity's generated response files after the final settlement/count fix.
- PASS: 16 targeted pet-domain tests executed through the Unity-bundled Mono runtime, including x10 guarantee, ordered copy deltas, strict recovery shape, hard pity, and natural SSR reset.
- PASS: 8 targeted combat/passive tests executed through the Unity-bundled Mono runtime, including same-target/carried Follow-Up, immediate collection hot reload, maximum-heart expansion, and future encounter-schedule refresh.
- PASS: The scoped pet/combat implementation diff reported no whitespace errors; the repository-wide check still reports unrelated pre-existing art metadata whitespace.
- PENDING: Full Unity Test Runner suite, Firebase emulator integration, mobile WebGL, and visual follower/heart tests.
