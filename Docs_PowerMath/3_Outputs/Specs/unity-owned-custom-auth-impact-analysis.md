# Impact Analysis: Unity-Owned Custom Authentication

> **Superseded:** Replaced by `direct-firestore-auth-impact-analysis.md` on 2026-08-09.

## Refactor Goal

Replace the web-app launch-code handoff with Unity-owned sign-in and a same-origin Cloudflare session API while retaining the existing player snapshot and Main Menu presentation.

## Blast Radius

- Authentication, Bootstrap, Main Menu logout, session transport, build-scene order, and the Firestore projection contract change.
- Player gameplay DTOs and Main Menu display mapping remain compatible.
- The former WebGL launch-code bridge is detached but retained until human WebGL E2E passes.
- Production deployment and secrets are not changed by the repository implementation.

## Class Responsibility Table

| Concern | Before | After |
|---|---|---|
| Student credential entry | Containing web app | Unity `AuthenticationView` and `AuthenticationPresenter` |
| Credential verification | Unimplemented external backend | Cloudflare `/api/v1/auth/login` with server-only HMAC |
| Browser persistence | One-time launch code | Secure HttpOnly session cookie managed by the browser |
| Startup identity | `WebGlSessionHandoff` | `IPlayerBootstrapService` and `/api/v1/game/bootstrap` |
| Loading/routing | Launch-code-driven `GameBootstrapper` | Cookie-session `GameBootstrapper` |
| Editor development | Automatic Bootstrap bypass | Explicit Editor-only sample login using the production scene flow |
| Runtime player state | `PlayerSessionStore` with access token | `PlayerSessionStore` with snapshot, CSRF token, and session metadata |
| Logout | Not implemented | Main Menu logout through `IAuthenticationService` |
| Firestore access | Planned game API | Cloudflare Function using OAuth service-account access held in secrets |

## Regression Boundaries

- Main Menu view-model output is unchanged.
- `PlayerSnapshot` schema version remains 1.
- Bootstrap remains build index 0 and success-gates Main Menu.
- No production code path uses the Editor sample account because all mock implementations are guarded by `UNITY_EDITOR`.
- No launch-code file is deleted before WebGL E2E.
