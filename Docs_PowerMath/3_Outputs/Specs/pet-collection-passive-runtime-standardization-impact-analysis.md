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
owner: architect-agent
human_checkpoint: required
next_agent: human
blocked_by:
  - architecture-approval
---

# Impact Analysis: Pet Collection and Passive Runtime Standardization

## Refactor Goal

Replace pet-name-specific stat/passive logic and ambiguous inventory records with a generic, count-based collection projection and idempotent passive runtime, while preserving cosmetic equip and preparing semantic follower/heart presentation hooks.

## Current Implementation vs. Target Architecture

| Area | Current implementation | Target architecture |
| --- | --- | --- |
| Copy ownership | General `inventory[]` rows with `owned`, `upgradeLevel`, and newly added `count`; duplicate rows may be aggregated in some readers | One canonical positive copy count per stable pet ID with explicit migration from legacy rows |
| Inventory UI | `PlayerOwnedPetInventory` now aggregates rows and exposes `Count` | Reads canonical projection only; one row/card per ID with `xN` |
| Stat aggregation | `PetCollectionPolicy` stacks values but coerces invalid counts to one and has no clamp catalog | Typed modifiers, fail-closed validation, checked aggregation, and data-defined clamps |
| Passive identity | `PetPassiveType.SapphireFollowUp` and `HasAuregriffThirdAttack` name content in core code | Generic trigger/effect/filter/stack-rule descriptors and keyed runtime state |
| Combat triggers | `LocalRunEncounterEngine` contains pet-specific counters and formulas | Passive evaluator consumes semantic combat events and returns deterministic effect commands |
| Saved runtime | `stageAttackCount`, `bigBossesDefeated`, `pendingPetFollowUpDamage` are fixed leaves | Versioned passive state keyed by passive definition and reset scope, saved atomically with the source operation |
| Gacha receipt | One legacy pet ID/new flag is saved even when current UI can request multiple pulls | Single pull only for this slice, or a complete ordered multi-result receipt before multi-pull is enabled |
| Authority | Client rolls and directly PATCHes anonymous Firestore | Trusted authenticated command computes roll/count/passive results; client presents receipts |
| Pet presentation | No combat follower implementation found under `main_Canvas` | Presentation-only follower reacts to semantic receipts; final feel waits for brief |
| Heart presentation | Core emits `HeartChanged`, but no presentation subscription was found | Dedicated presenter consumes reason-coded heart changes; final visuals wait for brief |

## Audit Findings

### P0 — Authority claim is not currently enforceable

`Firebase/firestore.rules` permits direct prototype updates without authenticated per-player command validation. ADR-010 explicitly accepts that a modified client can tamper with wallet, inventory, RNG, and payloads. Client refresh/preconditions prevent accidental concurrency bugs but cannot confirm legitimate ownership or stop intentional forging.

Impact: “less exploits” can be improved locally, but production ownership confirmation requires the trusted backend checkpoint described by ADR-021.

### P0 — Multi-pull recovery is incomplete

The current command and UI support arbitrary/10-pull results, but Firestore saves only `lastPetGachaPetId` plus aggregate `lastPetGachaWasNew`. Retrying a committed multi-pull can recover only one result even though the inventory was changed for all results. `PullCount` is not restricted to approved pack sizes.

Recommendation: ship single-pull only in this slice. Re-enable multi-pull only after saving the ordered result list, per-pet count deltas, pull count, and full command fingerprint.

### P1 — Content names are embedded in domain contracts

`PetPassiveType`, `PetCollectionStats`, `PlayerCombatStats`, `RunSettlementPolicy`, and `LocalRunEncounterEngine` expose Sapphire, Auregriff, GoldenCrane, Lunamoth, and Lumirin-specific members. Each new pet requires edits across content, projection, combat, persistence, and tests.

Recommendation: use generic passive trigger/effect/filter and data-driven magnitude/reset/stack rules. Stable pet IDs remain content identifiers only.

### P1 — Invalid data can silently grant power

Several readers convert missing/zero/negative counts into one and sometimes treat `upgradeLevel` as copy count. This keeps old saves alive but conflates weapon upgrade semantics with pet ownership and can grant a copy from corrupt data.

Recommendation: migrate legacy presence to `count=1` once at a schema boundary, then require a strictly positive count everywhere else.

### P1 — Identity comparison is inconsistent

Some collections use `OrdinalIgnoreCase`, while catalog dictionaries and Firestore updates use `Ordinal`. `PetGachaCatalogDefinition.TryResolvePet` also accepts display name as identity. Renaming/localizing a display name can therefore affect persistence, and case variants can split counts.

Recommendation: canonical IDs use one comparison rule and display names never resolve persistent identity. A one-time explicit alias map may exist only in migration code.

### P1 — Gacha and inventory do not share one canonicalization rule

Inventory display/stat readers aggregate duplicate legacy rows, while the gacha writer increments only the first exact-case match. A malformed save can remain non-canonical and receive inconsistent totals.

Recommendation: one `PetCollectionSnapshot` parser validates/normalizes for all readers and writers; writes always emit canonical representation.

### P1 — Passive progress is coupled to encounter engine fields

Stage attack count, defeated Big Boss count, and pending Follow-Up damage are fixed fields on the combat snapshot. This does not scale to more passives, reset scopes, effect caps, or several passives on the same trigger.

Recommendation: `PetPassiveRuntimeState` stores per-passive progress/pending values with scope and last-applied operation ID.

### P2 — Heart and pet presentation contracts are incomplete

Heart gain/loss events exist, but no main-canvas presenter subscription was found. Equipped pet presentation beside the player and attack-state movement are not implemented.

Recommendation: add semantic presentation ports now; implement visual behavior only after the owner's brief.

### P2 — Current tests contradict the new duplicate behavior

`PlayerHubDataTests.OwnedInventory_RejectsDuplicateRecognizedPetRecords` expects duplicate legacy rows to fail, while `PlayerOwnedPetInventory.TryCreate` now aggregates them. This indicates the current dirty worktree is not at a coherent regression baseline.

Recommendation: replace the test with explicit migration/canonicalization cases after architecture approval.

## Class Responsibility Table

| Current class | Current responsibility/problem | Target owner |
| --- | --- | --- |
| `PetDefinition` | Identity plus every stat field plus pet-named passive enum | Identity/presentation and arrays of validated modifier/passive descriptors |
| `PetGachaPet` | Gacha item plus expanding stat/passive payload | Immutable catalog entry containing generic descriptors |
| `PetCollectionPolicy` | Aggregation and pet-specific passive flags | Generic `PetCollectionProjector` producing stat totals and active passive instances |
| `EquippedPetAttackPolicy` | Historic equip-powered API now used partly for validation | `PetEquipPolicy` validates cosmetic selection only |
| `PlayerOwnedPetInventory` | UI projection plus legacy aggregation | UI adapter over canonical `PetCollectionSnapshot` |
| `FirestorePetGachaCommandStore` | Refresh, roll, inventory mutation, receipt, patch, local projection | Client gateway only; trusted command owns validation, roll, count mutation, and receipt |
| `PlayerCombatStats` | Player stats plus pet-name booleans | Numeric combat stats plus generic `ActivePassiveSet` |
| `LocalRunEncounterEngine` | Core combat plus pet-specific passive rules | Core combat emits/consumes semantic events/effects through `IPetPassiveResolver` |
| `RunSettlementPolicy` | Settlement plus Lunamoth-specific branch | Settlement applies generic authoritative economy modifiers/effects |
| `CombatLobbyCompositionRoot` | Large scene composition | Wires passive runtime and presentation ports; no passive rules |

## Blast Radius

- High: save schema/migration, gacha idempotency, combat resolution ordering, run settlement, and Firestore authority.
- Medium: Player Hub inventory/equip UI, stat projection, leaderboard equipped-pet projection, tutorial forced-pull flow.
- Low until visual brief: main-canvas follower and heart presentation adapters.

## Incremental Migration

1. Freeze behavior with tests for 180-PC single pull, probability conservation, Furbo `x2 = +14`, cosmetic equip invariance, passive non-recursion, and reconnect idempotency.
2. Introduce generic core descriptors/projection behind adapters; retain old save fields read-only.
3. Add schema migration to canonical copy counts and versioned passive state; dual-read, canonical-write.
4. Move combat/run settlement to generic effect commands and remove pet-name members.
5. Restrict gacha to single pull and persist a command fingerprint/complete receipt.
6. Add trusted command backend and tighten Firestore rules after explicit deployment approval.
7. Add semantic follower/heart presenters; implement final animation after the visual brief.
8. Remove compatibility reads only after deployed-save telemetry or explicit migration sign-off.

## Regression Gates

- No implementation begins until the content-source and architecture decisions are approved.
- No backend, rule, dependency, build, secret, or deployment change occurs without explicit approval.
- Existing dirty worktree changes are preserved; implementation must patch only approved files and review overlapping diffs first.
- Unity compilation and focused EditMode tests must pass before scene/presentation work begins.
- Editor/WebGL reconnect tests are mandatory before removing compatibility fields.

