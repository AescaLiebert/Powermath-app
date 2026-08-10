---
slug: unity-owned-custom-auth
status: superseded
source: manual
gdd_tags:
  - server-authority
  - player-experience
  - guardrails
owner: codex
human_checkpoint: required
next_agent: architect-agent
blocked_by:
  - cloudflare-secrets-and-deployment
  - human-webgl-e2e
---

# Task Card: Unity-Owned Custom Authentication

> **Superseded:** Replaced by `direct-firestore-auth-task-card.md` on 2026-08-09.

## Goal

Replace the web-app launch-code handoff with a self-contained WebGL flow where Unity owns the login and loading screens, a same-origin host function owns authentication and authorization, and the function reads authoritative Firestore data without Firebase Authentication.

## Important Boundary

Unity must not connect directly to Firestore with privileged credentials. A WebGL build is public client code, so any embedded service-account credential or unrestricted database path is compromised by design.

## Target Scene Flow

```text
BootstrapScene
  -> valid browser session -> hydrate player -> MainMenuScene
  -> no browser session -> AuthenticationScene

AuthenticationScene
  -> valid credentials -> BootstrapScene -> MainMenuScene
  -> invalid credentials -> remain on AuthenticationScene
```

`BootstrapScene` is also the loading intermediary for later scene transitions.

## Scope

- Restore `AuthenticationScene` as a supported Unity UI Toolkit scene.
- Replace launch-code exchange with cookie-backed session restoration.
- Host the WebGL build and `/api/**` function under the same Cloudflare Pages or Netlify site origin.
- Support an explicit Remember This Device option with a revocable persistent cookie.
- Keep `PlayerSnapshot`, `PlayerSessionStore`, and Main Menu data binding.
- Retain an Editor-only mock authentication/data path.
- Supersede ADR-001 only after this proposal is approved.

## Out of Scope

- Direct privileged Firestore access from Unity/WebGL.
- Firebase Authentication.
- Account registration, access-code recovery/reissue UI, MFA, and educator administration UI.
- Choosing production session-expiry durations without school/device policy.
- Implementation before design and architecture approval.

## Acceptance Criteria

- [ ] Human WebGL: refreshing a valid session restores the student without another login.
- [ ] Human WebGL: Remember This Device restores after browser close/reopen until expiry or revocation.
- [ ] Human WebGL: unchecked login ends when the browser session ends.
- [x] Invalid or expired sessions route to Authentication in the implemented scene flow.
- [x] Network failures remain on Bootstrap with Retry and are distinct from invalid credentials.
- [x] Credentials are sent only to the same-origin custom login endpoint in player builds.
- [x] Access codes, session IDs, and service credentials are not written to Unity logs or local storage.
- [x] Firestore access remains behind the trusted Cloudflare boundary.
- [x] The Editor mock exercises Authentication -> Bootstrap -> Main Menu without Firebase.
- [x] Main Menu keeps its existing player-data presentation and adds explicit logout.

## Human Checkpoints

- [x] Use a same-origin Cloudflare Pages Function/Worker boundary.
- [x] Remember This Device may survive browser close through a persistent cookie.
- [x] Architecture and ADR-002 approved by the project owner.
- [x] Build-scene implementation approved by the project owner.
- [ ] Human WebGL E2E before removing the launch-code implementation.
