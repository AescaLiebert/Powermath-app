---
slug: firebase-backed-admin-and-mobile-media-recovery
status: accepted
source: manual
gdd_tags:
  - combat-attempt
  - player-experience
  - server-authority
  - guardrails
owner: project-owner
human_checkpoint: passed
next_agent: human-qa
blocked_by: []
---

# ADR-018: Firebase-Backed Admin and Mobile Media Recovery

| Field | Value |
| --- | --- |
| Status | **Accepted** |
| Date | 2026-09-11 |
| Decision owner | Project owner |
| Supersedes | ADR-017 session-only overrides, synthetic Quick Test, username allowlist |
| Extends | ADR-006 embedded YouTube lifecycle |

## Context

Deployed Android/iOS browser testing found that implicit fullscreen hid the DOM
video outside the fullscreen canvas, video focus/pause could strand the committed
attempt, audible autoplay was unreliable, and question-time audio ducking sounded
like the music had stopped. Admin combat/Quick Test controls also created
session-only state while persisted commands reloaded against stale local data.

## Decision

- Login, Enter, and actor input never request fullscreen. Only the Settings toggle does.
- Browser fullscreen targets `unity-container`; the YouTube overlay is hosted in
  that same subtree.
- Question video starts muted and inline for mobile autoplay. Once playback begins,
  iframe input is locked and focus returns to Unity. Pause/stall and error paths
  close the overlay and report one terminal result.
- Browser-to-Wasm callbacks are generation-checked, runtime-checked, and caught so
  late callbacks cannot escape as an unhandled WebAssembly trap.
- Question presentation does not duck game music.
- Admin visibility comes from the signed-in student's Firebase
  `userdata.admin` boolean, managed manually by the project owner.
- Combat tuning is stored in `gamedata.adminTuning`. Synthetic Quick Test state is removed.
- After every successful admin patch, Unity reloads and rehydrates the full player
  snapshot from Firebase before any scene reload or subsequent command.
- Rank, HP, currency, and combat-setting actions preserve the saved active Stage.
  Full Reset remains the only command that intentionally returns the account to Stage 1.

## Consequences

- Mobile autoplay is reliable without competing for OS audio focus.
- A stalled or tapped video cannot leave an opaque overlay or permanent combat lock.
- Admin state and authorization survive refresh and match Firebase.
- The direct-Firestore prototype remains client-visible and is not production-grade authorization.
- Deployed Android/iOS review remains required before publishing.
