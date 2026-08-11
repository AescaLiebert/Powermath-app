# ADR-007: Direct Firestore Social Projection and Private Analytics Boundary

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-11 |
| Author | Architect agent |
| GDD Section | `@tag:leaderboard-profile`, `@tag:server-authority`, `@tag:guardrails` |
| Extends | ADR-006 direct Firestore prototype boundary |

## Context

The approved Grade 4-6 leaderboard and owner Profile Analytics introduce public discovery, competitive state, private education data, and a seven-day Display Name mutation. ADR-003 names competitive/public state as an authentication migration trigger, but the current project direction still rejects Firebase Authentication and a hosted backend and requests low-read, manual-only refresh behavior.

## Decision

- Continue the accepted direct-Firestore prototype without a new backend or package.
- Store private owner analytics inside the authenticated student's existing `competition/{levelId}.{username}.gamedata` map.
- Create one pre-provisioned, sanitized `leaderboard-public/{level1|level2|level3}` document containing opaque-public-ID entries and no credentials/private analytics.
- Fetch that one public document only when the leaderboard opens or the student explicitly presses Refresh; never poll.
- Derive shared ranks in Unity using Highest Stage then the GDD-aligned `Silver ×5 + Gold ×7 + Diamond ×10` score.
- Persist an opaque `publicPlayerId`; never publish the credential username.
- Serialize gameplay and Display Name mutations through a revisioned player-data coordinator.
- Update private player state first, then publish an idempotent revisioned public projection. Public projection lag is repairable and cannot roll back accepted private state.
- Use a Firestore server timestamp for accepted Display Name changes and derive the seven-day eligibility date from it.
- Keep student audit values out of Profile presentation and calculate Response Efficiency as `correct ? responseScore × 10 : 0`.
- Require human approval before creating/migrating public documents or publishing rule changes.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Read `competition/{levelId}` directly for leaderboard | One read; no duplicate projection | Downloads credentials/private analytics and violates the approved public projection boundary |
| One sanitized public document per level **(chosen)** | One read per open/Refresh; complete shared ranking; fits current dynamic-map prototype | Shared document size/contention; anonymous writes are not ownership-secure |
| One public document per player | Scalable writes and query ordering | One read per entry/page; extra index/rank/self-query complexity and higher read cost |
| Trusted backend/Firebase Authentication with materialized leaderboard | Real ownership, cooldown enforcement, secure private analytics | Contradicts current no-backend decision; requires deployment/secrets/auth migration |
| Automatic listener/polling | Fresher ranks | Unrequested recurring reads and quota/cost risk |

## Consequences

### Positive

- Opening or manually refreshing a cohort costs one leaderboard document read.
- Leaderboard payloads are public-safe and cannot accidentally bind credentials or education analytics into UI.
- Domain ranking and analytics remain deterministic and transport-independent.
- Main Menu, combat, social UI, and persistence responsibilities remain separated.
- Display Name state, analytics, and progression survive sessions and revision conflicts.
- No new runtime dependency, backend worker, listener, or Firestore index is required.

### Negative / Trade-offs

- Direct anonymous Firestore still cannot prove caller ownership or provide tamper-proof competitive integrity.
- Existing login continues to download the shared private grade document under ADR-006's accepted risk.
- The cohort document has Firestore document-size and shared-write limits.
- Public projection is eventually consistent with private player state rather than one atomic cross-document transaction.
- Server timestamps make the normal client's cooldown clock-independent but do not stop a modified anonymous client.
- Bounded histories are necessary inside the shared document; unbounded attempt history requires a future storage/auth migration.
- Response Efficiency is intentionally a normalized correctness-and-speed value and remains separate from hidden audit state.

### Migration

1. Add opaque IDs and owner analytics leaves without overwriting existing data.
2. Manually pre-create/migrate the three sanitized public documents.
3. Replace the academic-only store with the shared mutation coordinator behind an adapter.
4. Add social/profile repositories and presentation.
5. Publish reviewed Firestore rules only after explicit project-owner approval.
6. Migrate to trusted authentication/per-player private documents before the leaderboard controls rewards, purchases, Challenger League, or other consequential outcomes.

## Related

- ADR-003: Direct Firestore Prototype Authentication and its competitive/public migration trigger.
- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary.
- `Docs_PowerMath/3_Outputs/Specs/leaderboard-profile-analytics-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/leaderboard-profile-analytics-arch-plan.md`

> [!NOTE]
> Accepted by the project owner on 2026-08-11 (`LGTM`).
