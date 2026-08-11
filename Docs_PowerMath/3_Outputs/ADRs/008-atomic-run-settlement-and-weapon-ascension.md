# ADR-008: Atomic Run Settlement and Weapon Ascension

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-11 |
| Author | Architect agent |
| GDD Section | `@tag:combat-stats`, `@tag:run-reset`, `@tag:economy`, `@tag:server-authority` |
| Extends | ADR-006 direct Firestore persistence; ADR-007 public projection |

## Context

Combat can persist attempts and reach terminal phases but cannot atomically convert a finished run into Power Coins/Legacy ATK/Prestige or recover at Stage 1. Weapon upgrades are read-only array entries with no authoritative spend command. The approved design requires identical Stage-based death/Rebirth rewards, Stage 50+ optional Rebirth, Rank preservation with audit/question-runtime reset, lifetime leaderboard inputs, and a Level 0–100 ScriptableObject-driven Weapon Ascend.

## Decision

Accepted by the product owner on 2026-08-11.

- Add explicit Run Settlement and Weapon Ascend commands rather than extending the attempt-save request into a general economy mutation.
- Re-read the authenticated student's private map, derive mutations from refreshed saved state, and PATCH with an update-time precondition under the accepted anonymous direct-Firestore prototype limitation.
- Identify settlements by saved `runId`; persist the last receipt and create the next run ID in the same accepted PATCH.
- Track Rank Currency earned during the current run separately from lifetime wallet balances.
- Evaluate Power Coin reward with checked integer arithmetic and store Legacy ATK as integer basis points.
- Preserve active Rank but reset audit score/count, active attempt, and all Rank queue runtime from canonical question content on either settlement.
- Store Weapon Ascension canonically in the existing inventory item `weapon-ascension`; replace the validated full inventory array atomically when one level changes.
- Store only Weapon level. Derive cost/stats using `WeaponAscensionPolicy`; derive tier presentation using `WeaponAscensionCatalogDefinition`.
- Keep lifetime Highest Stage and Rank Currency outside reset. Public projection changes Current Stage and weapon presentation only.
- Compose immutable combat stats before an attempt. Do not let UI or Firestore submit derived ATK/CR/CD.
- Derive combat, Player Hub, and Rebirth preview values from one immutable `PlayerStatProjection`; presentation code must not reproduce the formula.
- Split settlement and Weapon/Player Hub presentation into separate controllers behind a thin shared composition facade.

## Alternatives Considered

| Alternative | Advantage | Rejected because |
| --- | --- | --- |
| Settle inside fatal attempt PATCH | One write | Couples combat resolution to reset summary/retry and cannot support optional safe-state Rebirth cleanly |
| Store Power Coin reward supplied by UI | Simple request | Trusts client-calculated mutable values and enables stale/double rewards |
| Reset active Rank with audit | Simplest full reset | Contradicts approved student-frustration goal |
| Preserve audit/queues | Less data rewritten | Contradicts corrected game-system reset semantics |
| Separate `weaponLevel` scalar outside inventory | Easy patch | Duplicates canonical item ownership/upgrade state and violates requested Firebase inventory ownership |
| Store derived weapon stats in Firestore | Easy rendering | Creates drift when curves/catalog change and exposes redundant authority |
| Cloud Function transaction | Stronger authority | New deployment/dependency/secret scope rejected for the current direct-Firestore prototype |

## Consequences

### Positive

- Retry/reconnect cannot grant one run or Ascend twice.
- Rank placement survives resets while question/audit runtime restarts predictably.
- Leaderboard lifetime order remains stable across resets.
- Designers can add/rename weapon appearance tiers without persistence migration.
- One Power Coin balance supports two simple kid-facing choices: Ascend or Pet Gacha.

### Negative / Trade-offs

- Anonymous clients remain tamperable; this is not production-grade server authority.
- Shared grade-document update contention continues.
- Replacing the full inventory array increases payload size and requires strict preservation/validation.
- Resetting all Rank queues rewrites more leaves and intentionally discards unfinished queue position.
- Weapon curve changes retroactively change derived stats for every saved level unless policy versions are added later.
- Final tier icons/assets are blocked on the later UI/art brief.
- Pet loadout IDs cannot contribute combat power until a real Pet stat definition is introduced; the UI reports this boundary explicitly.

## Migration

1. Missing-default repair adds run counters, receipts, Legacy basis points, and base weapon inventory item without replacing valid values.
2. Existing valid inventory items are preserved; exactly one canonical Ascension item is inserted only when absent.
3. Existing empty run IDs receive an opaque ID before combat/settlement is enabled.
4. Derived public weapon presentation refreshes on the next accepted private write.

## Related

- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary.
- ADR-007: Direct Firestore Social Projection and Private Analytics Boundary.
- `Docs_PowerMath/3_Outputs/Specs/run-settlement-weapon-ascend-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/run-settlement-weapon-ascend-arch-plan.md`
