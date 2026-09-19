# ADR-019: Event Stage Scheduling and Challenge Resolution

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-09-14 |
| Author | Codex |
| GDD Section | `@tag:question-data`, `@tag:stage-progression`, `@tag:encounters`, `@tag:pet-system`, `@tag:feedback` |
| Extends | ADR-005, ADR-006, ADR-009, ADR-010, ADR-012 |

## Context

ADR-009 implemented fixed Event bindings, an implicit Challenge-only definition, numeric Event question IDs, retry-on-failure with heart damage, and no immediate Event reward. The canonical GDD now requires saved chance scheduling, one-trial flee-on-failure, Rank-aware prefixed Challenge IDs, pet-modified Event chance, and an immediate idempotent 10-20 Power Coin reward.

## Decision

- Keep `EventDefinition` independent from `EnemyDefinition`; add `EventStageType` and dispatch runtime behavior through typed handlers/configuration.
- Save one authoritative generated Challenge schedule for the whole run. Use one guaranteed eligible Stage and at most one bonus roll per 20-Stage block.
- Convert the authored base percent to basis points and calculate `clamp(baseChanceBasisPoints * petMultiplierBasisPoints / 10000, 0, 10000)`.
- Snapshot the account-wide pet Event multiplier when a run is created so changing pets/content cannot reroll an active run.
- Remove unused document and handler ownership fields from `EventDefinition`; centralize the physical `question/challenge-silver`, `question/challenge-gold`, and `question/challenge-diamond` mappings in `GameApiSettings` and dispatch Challenge behavior from `EventStageType`.
- Preserve ordinary numeric `QuestionId`. Add canonical string `ChallengeQuestionId` values (`csN`, `cgN`, `cdN`) and one shared per-Rank Challenge FIFO/reservation state across Challenge Event Definitions.
- Parse the deployed normal-question shape in each Challenge document: top-level `qN: { id, video-url, answer }`. Validate that Rank prefix and ordinal agree with the containing document and `qN` field.
- Persist a string `activeRun.questionContentId` additively; retain the legacy integer `activeRun.questionId` for ordinary-question compatibility.
- Resolve an unsuccessful valid Challenge attempt as `EnemyFled`, not `EnemyAttacked`: no heart loss, Event completed, Stage advanced.
- Calculate Challenge reward as `10 + ResponseScore` for correct answers and `10` for incorrect/timeout, with score clamped to 0-10.
- Save wallet delta, result, next encounter, Challenge inventory, and reward receipt atomically under the committed attempt ID.
- Extend the persisted presentation receipt so flee and reward feedback replay without replaying state mutations.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Make `EventDefinition` inherit `EnemyDefinition` | Reuses authored enemy fields directly | Imports cooldown/HP/boss assumptions into Minigames and contradicts the accepted independent Event boundary |
| Use a shared abstract ScriptableObject base | Shares identity/presentation fields | Unity serialization migration is broader and still does not share runtime behavior safely |
| Keep numeric `QuestionId` and strip the `c/rank` prefix | Small code change | Loses canonical Firebase identity and cannot validate Rank ownership from the item ID |
| Change all ordinary and Event IDs to strings immediately | One identifier type | Large migration across Rank FIFO, analytics, saves, fallback generation, and tests with no current GDD need |
| Select Challenge questions by stable hash | Minimal persistence | Does not provide the requested ordinary-enemy sequence behavior |
| Roll Event chance on every eligible Stage | Simple local resolver | Can generate many Events per block and violates the GDD cadence |
| Grant reward in a separate wallet command | Smaller attempt patch | Creates missing/duplicate reward windows around Stage advance and reconnect |

## Consequences

### Positive

- Event families remain extensible without a monster-field god object.
- Challenge cadence, question reservation, outcome, and reward are deterministic across reconnects.
- Failure is non-blocking and cannot accidentally trigger player-damage or counter-attack behavior.
- Ordinary Rank queues and audit placement remain isolated from harder Challenge content.

### Negative / Trade-offs

- Active-run schema, migration, receipt validation, and transaction patches grow.
- The pet collection model carries an additional SSR collection-passive projection, increasing catalog and migration surface area.
- Two question ID types remain intentional until a future approved catalog-wide migration.
- Fixed designer bindings count inside the one-to-two Challenge cap, so designers cannot use them to exceed the block cap without a future decision change.

### Migration

1. Add schedule, string content ID, Challenge inventory, and reward receipt fields with safe defaults.
2. Continue reading/writing the legacy numeric question ID for ordinary Rank attempts.
3. Restore a supported saved schedule verbatim; deterministically create the schedule when the additive schedule fields are absent, then persist it at the next authoritative gameplay checkpoint.
4. Restore an interrupted Challenge in ready state with its saved reservation and committed attempt identity; accepted results replay only from the persisted receipt.

## Approval

Accepted by the project owner with `lgtm` on 2026-09-14. The approved choices are an independent Event definition, fixed bindings inside the block cap, and a separate per-Rank Challenge FIFO. The owner clarified later on 2026-09-14 that physical Challenge Rank documents are centralized in `GameApiSettings` and all Challenge Events share those FIFO pools.

## Related

- ADR-005: Atomic Academic Progression and Question Catalog Boundary
- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary
- ADR-009: Data-Driven Stage Map and Encounter Runtime
- ADR-010: Direct-Firestore Pet Gacha Prototype
- ADR-012: Persisted Combat Presentation Receipts and Orchestration
- `Docs_PowerMath/3_Outputs/Specs/event-stage-challenge-migration-arch-plan.md`
