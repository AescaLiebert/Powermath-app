# ADR-021: Data-Driven Pet Collection and Passive Runtime

| Field | Value |
| --- | --- |
| Status | **Accepted** |
| Date | 2026-09-17 |
| Author | Architect agent |
| GDD Section | `@tag:combat-stats`, `@tag:economy`, `@tag:pet-system`, `@tag:gacha`, `@tag:server-authority`, `@tag:feedback` |
| Extends | ADR-010, ADR-012, ADR-013, ADR-016 |

Approved by the project owner (`LGTM`) on 2026-09-17. Trusted backend deployment and final follower/heart presentation remain separate human checkpoints.

## Context

The pet implementation now needs duplicate copy counts, account-wide stacked stats, reusable SSR passive rules, saved trigger progress, and combat presentation. The current partial implementation embeds pet names in core enums/booleans, uses ambiguous inventory fields, saves fixed passive counters, and relies on a direct anonymous Firestore prototype that cannot enforce ownership or RNG authority.

## Decision

- Represent ownership as one canonical positive copy count per stable pet ID; derive inventory rows, stats, and passive availability from this collection.
- Represent pet stats as typed modifiers and apply checked summation plus data-defined clamps.
- Represent passives with generic trigger, effect, encounter filter, magnitude, reset scope, and stack rule descriptors. Core code never branches on a pet name.
- Ship SSR passives as `UniquePerDefinition` unless a later approved balance change enables another stack rule. Duplicate SSR copies continue stacking their Main Stat.
- Save only canonical counts and irreducible generic passive runtime progress/pending values. Never save derived stat totals or pet-specific `hasPassive` flags as authority.
- Resolve passive effects from semantic authoritative gameplay events and apply them in the same idempotent transaction as their source operation.
- Treat equipped pet as presentation-only. A receipt-driven presenter may animate the equipped cosmetic pet for any account-wide pet action.
- Support 1x and 10x pulls through a complete ordered multi-result receipt. The receipt records per-result copy deltas and SSR pity before/after values so retries never reroll or double-spend.
- For production exploit resistance, move gacha and passive commands behind a trusted authenticated backend that re-derives ownership and effects. Direct Firestore remains prototype-only until that migration is explicitly approved and deployed.

## Alternatives Considered

| Option | Pros | Cons |
| --- | --- | --- |
| Continue adding pet-name enum values and booleans | Smallest immediate diff | Every pet changes many systems; fixed save leaves; high regression risk |
| Scriptable callback/behavior per pet | Flexible authoring | Unity object behavior leaks into pure domain and server implementation; difficult deterministic validation |
| Generic trigger/effect descriptors with pure resolver **(chosen)** | Data-driven, testable, portable to backend, deterministic | Requires migration and disciplined catalog validation |
| Save derived totals and `hasPassive` flags | Fast reads | Duplicated authority can drift or be forged |
| Keep direct anonymous Firestore as production authority | No backend deployment | Cannot meet ownership confirmation or exploit-resistance requirement |
| Dedicated pet subcollection per player | Natural document separation | Conflicts with current shared player-document prototype and adds multi-document transaction/migration complexity |

## Consequences

### Positive

- New pets usually require content authoring rather than edits across combat, persistence, and UI.
- Duplicate display and collection power share one canonical count.
- Passive progress, reset, idempotency, and stacking rules become inspectable and testable.
- Cosmetic equip cannot accidentally remove power.
- The same pure resolver contract can run in Unity tests and a trusted backend.
- Presentation can evolve after the visual brief without changing gameplay authority.

### Negative / Trade-offs

- The save migration touches high-risk combat/economy state and must be incremental.
- A trusted backend requires a separately approved runtime, deployment, authentication/rules, and operational ownership.
- Generic descriptors cannot safely expose arbitrary scripting; new effect families still require reviewed engine/backend support.
- The owner approved the supplied CSV as the per-pet content authority and the generic `MainStatEffect` interpretation.
- The larger 10-pull receipt and persistent pity schema increase migration and recovery-test surface.

### Migration

1. Add regression tests around current approved behavior before moving code.
2. Add generic descriptors/projector/resolver behind compatibility adapters.
3. Migrate legacy pet inventory rows to canonical counts with a version marker.
4. Dual-read old passive leaves and canonical-write generic runtime state.
5. Move combat and settlement callers to semantic events/effects, then remove pet-name members.
6. Add follower/heart presentation ports; defer final animation content.
7. After explicit approval, deploy the trusted command backend and tighten Firestore rules.
8. Remove compatibility fields only after migration verification.

## Related

- ADR-010: Direct-Firestore Pet Gacha Prototype.
- ADR-012: Persisted Combat Presentation Receipts and Orchestration.
- ADR-013: Canonical Pet Definitions, Loadout, and Shared Main Menu Feedback.
- ADR-016: Player Lifecycle and Live Service Boundaries.
- `Docs_PowerMath/3_Outputs/Specs/pet-collection-passive-runtime-standardization-arch-plan.md`
- `Docs_PowerMath/3_Outputs/Specs/pet-collection-passive-runtime-standardization-impact-analysis.md`
