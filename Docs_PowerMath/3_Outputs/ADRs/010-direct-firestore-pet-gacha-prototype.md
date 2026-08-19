# ADR-010: Direct-Firestore Pet Gacha Prototype

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-13 |
| Author | Architect agent |
| GDD Section | `@tag:core-loop`, `@tag:economy`, `@tag:gacha`, `@tag:server-authority` |
| Extends | ADR-006 direct Firestore persistence; ADR-008 atomic Power Coin spending |

> Accepted by the project owner on 2026-08-13 (`lgtm prototype`).

## Context

Pet Gacha must preserve rarity totals while redistributing owned-pet chances, spend exactly 25 Power Coins, save permanent ownership, and recover one result idempotently. The current project intentionally uses anonymous direct Firestore without a trusted command backend, so it cannot provide tamper-proof server-generated randomness even though the GDD defines the server as authoritative.

## Decision

- Keep the accepted direct-Firestore prototype boundary and record its RNG limitation explicitly.
- Represent configured rarity rates as integer basis points totaling exactly 10,000.
- Select rarity first, then select a pet through exact integer within-rarity weights derived from the GDD redistribution formula.
- Use an injected unbiased cryptographic random source for the live prototype and deterministic sources for tests.
- Before rolling, refresh the private student map and validate revision, wallet, inventory, attempt state, catalog version, and the last gacha receipt.
- PATCH revision, the exact 25-Coin spend, optional new ownership, and the complete result receipt together with a Firestore update-time precondition.
- Return a matching saved receipt without rolling or spending again.
- Reveal a result only after the PATCH is acknowledged; reconnect recovers using the same transaction ID.
- Keep the feature unavailable when the human-authored catalog is missing or invalid. Do not invent production rarity rates, pet identities, art, stats, or audio.
- Do not equip the pulled pet or activate Pet ATK in this implementation slice.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Direct client RNG plus atomic Firestore receipt **(chosen for prototype)** | Fits current architecture; no deployment or dependency; concurrency-safe and recoverable | A modified client can influence RNG or requests; not production-grade authority |
| Trusted Cloud Function or REST command service | True server-generated roll and transactional authority | Requires backend architecture, deployment, secrets, authentication/rules, and separate approval |
| Deterministic hash of transaction ID | Easy retry consistency | Client can search transaction IDs for a desired result and there is no trusted secret |
| Save a pending roll then finalize in a second write | Explicit intermediate recovery | Adds a vulnerable second phase and cannot create trust without a backend |

## Consequences

### Positive

- Probability conservation and duplicate redistribution are deterministic and testable without floating-point roll drift.
- Confirm spam, refresh, and ambiguous network completion cannot create a second accepted spend for the same transaction ID.
- New ownership, empty duplicates, and resulting balance share one persistence boundary.
- Production catalog content remains data-driven and can be added without changing the probability engine.

### Negative / Trade-offs

- The prototype does not fully satisfy the GDD's trusted server-roll requirement.
- Anonymous clients can still tamper with balance, inventory, RNG, or request payloads under the accepted prototype security model.
- A single last receipt supports immediate retry/reconnect, not an unlimited audit ledger.
- Basis-point category rates provide 0.01 percentage-point authoring precision unless a future catalog version changes the representation.
- Gacha remains disabled until a valid production catalog is supplied.

### Migration

1. Add receipt leaves through missing-default repair without replacing existing wallet/inventory data.
2. Add the data-driven catalog, probability engine, command store, and UI behind catalog validation.
3. Before purchases, competitive rewards, or production launch depend on gacha integrity, move the command unchanged in intent to a trusted authenticated backend.
4. Preserve transaction ID, catalog version, pet ID, new/duplicate flag, cost, and resulting balance in the backend receipt contract.

## Related

- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary.
- ADR-008: Atomic Run Settlement and Weapon Ascension.
- `Docs_PowerMath/3_Outputs/Specs/pet-gacha-system-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/pet-gacha-system-arch-plan.md`
