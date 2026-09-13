---
slug: mobile-webgl-admin-state-regression
status: implemented-awaiting-mobile-review
source: manual
gdd_tags:
  - combat-attempt
  - player-experience
  - server-authority
  - guardrails
owner: project-owner
human_checkpoint: required
next_agent: human-qa
blocked_by: []
---

# Mobile WebGL and Admin State Regression

## Required behavior

- Enter and player/enemy clicks do not request fullscreen.
- Video remains visible in explicit fullscreen, autoplays inline on Android/iOS,
  does not interrupt game music, and cannot strand combat after a tap, pause, end,
  error, or late callback.
- Admin authorization and combat tuning come from Firebase.
- No synthetic session or answer-zero Quick Test is created.
- Each successful admin command rehydrates Firebase state; non-reset commands
  preserve the current Stage.

## Acceptance

- [x] Browser bridge regression tests pass.
- [x] Unity runtime and Editor test assemblies compile.
- [x] Shared Settings contract reflects Firebase-backed admin behavior.
- [ ] Android Chrome deployed playback/fullscreen review.
- [ ] iOS Safari deployed playback/fullscreen review.
- [ ] Disposable Firebase admin-account command sequence review.

No deployment, build-setting, CI, secret, dependency, merge, or Firebase rules
publication is included.
