# ADR-002: Unity-Owned Custom Authentication with a Hosting-Adjacent REST Boundary

| Field | Value |
|---|---|
| Status | **Superseded by ADR-003** |
| Date | 2026-08-08 |
| Author | Architect agent |
| GDD Section | `@tag:server-authority`, `@tag:player-experience`, `@tag:guardrails` |

## Context

ADR-001 assigns authentication to a containing web application and uses a one-time launch code. The project owner now wants Unity to own the student login experience and the deployment to operate without that external web-app handoff while continuing to avoid Firebase Authentication.

## Decision

Unity owns `AuthenticationScene`, `BootstrapScene`, and all player-facing authentication/loading states. A same-origin Cloudflare Pages Function verifies a username and long generated access code, creates an opaque browser session cookie, and uses Worker-held service credentials to read/write Firestore.

Unity does not access Firestore directly. On every WebGL start or refresh, Bootstrap calls the API with the browser-managed cookie and loads Main Menu only after a versioned player snapshot is validated. An optional Remember This Device selection issues a revocable persistent cookie that survives browser close without storing credentials in Unity. State-changing game actions remain server-authoritative, revision-checked, and idempotent.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Direct Firestore REST from Unity without Firebase Auth | Few components | Cannot securely establish per-student authorization; WebGL credentials are public. |
| Firebase Authentication | Strong client/rules integration | Explicitly excluded by project intent. |
| Keep external web login and launch code | Already implemented in Unity | Conflicts with the new self-contained Unity experience. |
| **Unity UI + same-origin host function + Firestore server access** | Self-contained UX, refresh/device restoration, preserves server authority | Requires a small serverless function, cookie/CSRF security, and account-support ownership. |

## Consequences

### Positive

- Students authenticate entirely inside the Unity experience.
- Page refresh can restore a valid session without resending credentials.
- Access codes, HMAC secrets, and session secrets remain outside Unity storage and logs.
- Cloudflare can deploy the WebGL site and its small authentication API together on the Workers Free plan while usage remains inside its limits.
- Remember This Device can survive browser close while remaining revocable.
- Existing player snapshot/store and Main Menu bindings remain reusable.

### Negative / Trade-offs

- A trusted REST backend is still mandatory even though the external web app is removed.
- Custom authentication requires credential provisioning/reissue, throttling, session revocation, logout, recovery, and operational support.
- Cookie-authenticated commands require CSRF protection and same-origin deployment validation.
- The Workers Free CPU limit must be measured for cold service-account signing and normal requests before production cutover.
- Editor development needs mock authentication and command gateways.

### Migration

- Implement the host-function API and persistent-cookie spike before changing Unity build flow.
- Restore and consolidate `AuthenticationScene` using UI Toolkit.
- Generalize Bootstrap for session restore and scene transitions.
- Keep the launch-code implementation until human WebGL E2E passes.
- If this ADR is accepted, mark ADR-001 `Superseded by ADR-002` during cutover.

## Related

- ADR-001: Web-Owned Authentication and Server-Owned Player Data
- `Docs_PowerMath/3_Outputs/Specs/unity-owned-custom-auth-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/unity-owned-custom-auth-arch-plan.md`

## Implementation Status

Accepted by the project owner on 2026-08-08. The Unity scene flow and Cloudflare Pages Function source are implemented. Production deployment, secrets, IAM assignment, and human WebGL E2E remain checkpoints. The former launch-code assets are retained as a rollback seam until that E2E passes, but they are no longer attached to the active Bootstrap scene.

> [!NOTE]
> On 2026-08-09 the project owner explicitly replaced this boundary with the intentionally insecure direct-Firestore prototype documented by ADR-003.
