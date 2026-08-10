# ADR-001: Web-Owned Authentication and Server-Owned Player Data

| Field | Value |
|---|---|
| Status | **Superseded by ADR-002** |
| Date | 2026-08-08 |
| Author | Architect agent |
| GDD Section | `@tag:server-authority`, `@tag:leaderboard-profile`, `@tag:guardrails` |

## Context

The Unity authentication prototype is being deprecated because students authenticate in the containing web application. Unity still needs trustworthy player identity and per-player inventory, progression, wallet, loadout, and save state. The GDD requires server authority and privacy separation, while the account database intentionally uses Firestore without Firebase Authentication.

## Decision

The web backend owns username/password verification and the browser session. It issues an opaque, single-use Unity launch code. Unity exchanges that code through a game API for a game-scoped session and versioned player bootstrap projection.

The game API—not Unity—reads and writes authoritative Firestore data. Unity submits idempotent domain commands with a unique ID and expected state revision. A persistent in-memory `PlayerSessionStore` exposes immutable state to scene presenters. UI Toolkit views render that state and emit intent but never authenticate, query Firestore, or own gameplay state.

`BootstrapScene` is the mandatory build entry and loading scene. It exchanges the session and fully hydrates `PlayerSessionStore` before loading `MainMenuScene`. Any authentication, transport, validation, or scene-load failure remains in `BootstrapScene`; the Main Menu has no anonymous or partial-data fallback.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Direct Firestore access from Unity | Fewer endpoints; quick prototype | Cannot safely establish per-student authority without trusted identity; exposes schema; conflicts with server authority |
| Pass username/password to Unity | Simple handoff | Duplicates authentication and exposes credentials to WebGL memory/logging |
| Pass web session token directly | Avoids exchange endpoint | Broad credential enters a less trusted runtime and couples Unity to web session internals |
| **Single-use launch code + game API** | Narrow trust boundary, revocable game scope, server authority, testable contracts | Requires backend endpoints and explicit expiry/retry flow |

## Consequences

### Positive

- Students sign in once and Unity receives the correct stable identity.
- Passwords and Firestore server credentials never enter Unity.
- Saves follow the GDD's idempotency and reconnect rules.
- UI Toolkit stays decoupled from transport and Firestore schemas.
- Public/private projections are enforced server-side.

### Negative / Trade-offs

- The backend team must implement and operate launch-code exchange and game API endpoints.
- Local Unity development needs fake handoff/API adapters.
- Session expiry, schema compatibility, and revision conflicts need explicit UI states.
- Backend and Unity contracts require coordinated versioning.

### Migration

- Add modular Unity seams with fakes first.
- Integrate backend session and WebGL bridge after security review.
- Bind `MainMenuScene` to the read-only store.
- Create `BootstrapScene`, then change Build Settings only after the separate Build Settings checkpoint.
- Retire `AutheticationScene` after refresh, reconnect, privacy, and duplicate-command tests pass.

## Related

- `Docs_PowerMath/3_Outputs/Specs/web-unity-player-data-arch-plan.md`
- `Assets/Project/Scenes/AutheticationScene.unity`
- `Assets/Project/Scenes/MainMenuScene.unity`
- `Assets/Project/UI/MainMenuUI.uxml`

> [!NOTE]
> The launch-code source files were removed during the ADR-003 cutover on 2026-08-09.
