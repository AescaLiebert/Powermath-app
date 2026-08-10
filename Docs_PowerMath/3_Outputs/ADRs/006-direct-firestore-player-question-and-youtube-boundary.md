# ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-10 |
| Author | Architect agent |
| GDD Section | `@tag:core-loop`, `@tag:question-data`, `@tag:run-reset`, `@tag:server-authority` |
| Supersedes on acceptance | ADR-003 schema/path decision; ADR-005 no-live-adapter restriction |

> Accepted by the project owner on 2026-08-10 (`LGTM`).

## Context

The direct-Firestore prototype now needs to initialize missing fields for existing students, load three shared Rank question documents, persist individual academic state, and display YouTube content inside the WebGL game. The current recursive `JsonUtility` DTO graph exceeds Unity's serialization depth, the player schema contains a removed `game1` wrapper, and ADR-005 intentionally stopped before live content/persistence.

## Decision

- Keep direct anonymous Firestore REST access and its accepted prototype security limitations.
- Configure player/progression Firestore and shared-question Firestore as independent Firebase projects with separate project IDs, database IDs, and Web API keys.
- Use `competition/{level1|level2|level3}` shared documents with dynamic student maps and direct `gamedata`.
- Initialize only missing leaves for an existing credential-matched student using update masks and an update-time precondition; never auto-register a missing student.
- Replace recursive Firestore wrapper serialization with a shallow raw-JSON navigator and explicit typed leaf mapping; add no JSON dependency.
- Use `question/{silver|gold|diamond}` with `items: [{id, video_link, answer}]`; Rank comes from the document and runtime identity is `(Rank, numeric id)`.
- Keep question content shared and player audit/FIFO/history state inside the student's `gamedata.academic` map.
- Gate each local attempt checkpoint with Firestore persistence and revisioned transaction receipts. Content, answer input, result feedback, and the next Attack remain blocked until the corresponding mutation is acknowledged or fails closed.
- Embed the official YouTube IFrame Player over the Unity WebGL canvas through a project-owned `.jslib`; use Editor mocks and no third-party package.
- Do not silently substitute development fixtures in production.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Continue `JsonUtility` wrapper DTOs | Minimal code change | Already exceeds Unity depth; brittle for questions/academic maps |
| Add Newtonsoft JSON | Mature parser, less custom code | New dependency and approval surface for a small REST subset |
| One Firestore document per question | Scales independently | Contradicts the approved exactly-three-document content layout |
| Native Unity `VideoPlayer` with YouTube watch URL | Unity-rendered texture | Watch URLs are webpages, not direct media resources |
| External YouTube tab | Simplest and Editor-compatible | Breaks flow and cannot receive reliable ended/error events |
| **Shallow REST adapter plus IFrame bridge** | No new package; exact schema; in-game video events | WebGL-specific DOM overlay and custom bridge tests required |

## Consequences

### Positive

- Existing students self-heal missing game fields without losing stored values.
- Question content and individual progression have explicit independent ownership.
- Numeric Rank-scoped IDs match the approved content authoring model.
- YouTube completion/error can participate in the committed attempt state machine.
- Parser, mapper, and patch policy are deterministic and fake-transport testable.

### Negative / Trade-offs

- Direct clients can read answers and tamper with requests; intentionally accepted for the prototype.
- Shared level documents create contention; precondition retry mitigates but does not remove it.
- Three Rank documents have Firestore document-size limits and whole-document read cost.
- The YouTube DOM element is visually over the Unity canvas, not a Unity texture, and Editor cannot render it.
- Attempt gateway/presenter contracts require asynchronous migration.
- Legacy `game1` data remains until a separately approved cleanup.

### Migration

1. Add parser/writer/default planner with fake REST tests.
2. Cut reads to direct `gamedata`; copy valid legacy leaves only when direct leaves are absent.
3. Change settings collection/doc defaults without changing project ID/API key.
4. Migrate question identity, repository, and academic persistence.
5. Add IFrame bridge and WebGL human E2E.
6. Mark ADR-003 schema and ADR-005 live-adapter restriction superseded only after this ADR is accepted.

## Related

- ADR-003: Direct Firestore Prototype Authentication.
- ADR-004: Combat Runtime Boundary and Development Simulation.
- ADR-005: Atomic Academic Progression and Question Catalog Boundary.
- `Docs_PowerMath/3_Outputs/Specs/firestore-player-question-youtube-migration-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/firestore-player-question-youtube-migration-arch-plan.md`
