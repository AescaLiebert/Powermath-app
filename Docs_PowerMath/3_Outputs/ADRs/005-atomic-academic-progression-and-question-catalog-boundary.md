# ADR-005: Atomic Academic Progression and Question Catalog Boundary

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-10 |
| Author | Architect agent |
| GDD Section | `@tag:answer-scoring`, `@tag:question-data`, `@tag:economy`, `@tag:server-authority`, `@tag:guardrails` |
| Extends | ADR-004; does not extend ADR-003 into gameplay access |

> Accepted by the project owner on 2026-08-10 (`LGTM`).

## Context

An accepted answer changes question inventory, hidden audit, Rank, Rank Currency, combat damage, enemy state, and possibly Stage. Implementing these as independent client managers risks partial commits and duplicated rewards. Real Firestore question/video delivery is unavailable, but the project needs testable infrastructure matching the approved `{id, video_link, answer, rank}` request.

## Decision

- Create separate Unity-free Academic Core and Infrastructure assemblies.
- Replace the current narrow combat gateway with one `IAttemptAuthorityGateway` whose idempotent receipts atomically cover question reservation, answer-window opening, answer/timeout resolution, audit, Rank, currency, and combat.
- Lock question ID, Rank, multiplier, answer policy, and presentation descriptor at commit.
- Keep correct answers out of player presentation models.
- Use a scene-memory `LocalDevelopmentAttemptGateway` with deterministic in-memory fixtures only in Editor/development builds.
- Use the active Rank multiplier and approved `+1` matching Rank Currency for correct development answers.
- Use an exact five-result `AuditWindow`; content voids do not advance it, and result five resolves under the old Rank before a new Rank affects the next attempt.
- Maintain independent per-Rank FIFO/cycle state with no repeated question inside one audit and failed-question front requeue in failure order.
- Define `QuestionDocumentDto` with `id`, `video_link`, `answer`, and `rank`, plus an all-or-nothing mapper/validator/catalog builder.
- Do not implement live Firestore reads, rules, indexes, gameplay writes, or persistence in this ADR.
- Do not interpret the existing undocumented `rankProgress`; production persistence later requires explicit audit score/count and inventory state contracts.
- Fail closed in production when no approved remote authority exists.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Independent audit, Rank, wallet, inventory, and combat managers | Small classes and quick local wiring | No atomic boundary; partial mutation and duplicate-retry exploits are likely. |
| Extend `PlayerSessionStore` as a mutable local save | Reuses current snapshot/UI events | Contradicts server authority, pollutes bootstrap state, and creates fake persistence. |
| Add question reads directly to `FirestoreRestClient` now | Fastest route to future content | Exposes answers, mixes authentication with gameplay, requires unapproved rules/security, and cannot own atomic progression. |
| Keep random scores and bolt audit onto results | Minimal combat refactor | Cannot test real correctness, FIFO question lifecycle, incorrect answers, or answer-length policy. |
| **Atomic attempt gateway plus separate Academic Core/catalog infrastructure** | One retry-safe result, pure tests, transport isolation, future remote replacement | Requires a deliberate gateway/combat refactor before adding visible Rank UI. |

## Consequences

### Positive

- One command cannot award currency/audit without its matching combat and question result.
- Rank changes and FIFO behavior are deterministic and fast to test.
- The Firestore-shaped DTO can evolve behind a mapper without contaminating domain/UI code.
- Development can exercise correct, incorrect, timeout, promotion, demotion, and content void without network access.
- Production remains fail-closed and cannot silently trust local educational outcomes.

### Negative / Trade-offs

- Existing combat gateway, coordinator, local engine, presenter, and tests require migration.
- Local progression resets on scene reload because persistence is intentionally absent.
- The requested DTO contains correct answers; it is unsuitable for an untrusted live client without a later security decision.
- `rankProgress` remains unused until its legacy meaning is documented.
- A real remote authority and video adapter remain future work.

### Migration

1. Add Academic Core and question Infrastructure with isolated tests.
2. Implement FIFO/audit/local progression aggregate.
3. Refactor combat resolution to accept explicit academic outcome/multiplier.
4. Replace the gateway contract and migrate existing tests.
5. Add player-safe Rank/currency projections and UI feedback.
6. Keep ADR-004 fail-closed build guard and session-read-only composition.

## Related

- ADR-003: Direct Firestore Prototype Authentication.
- ADR-004: Combat Runtime Boundary and Development Simulation.
- `Docs_PowerMath/3_Outputs/Specs/audit-rank-question-infrastructure-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/audit-rank-question-infrastructure-arch-plan.md`
- `Assets/Project/Script/Gameplay/Combat/Core/ICombatGateway.cs`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
