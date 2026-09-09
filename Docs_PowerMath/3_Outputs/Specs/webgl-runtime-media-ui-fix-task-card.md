---
slug: webgl-runtime-media-ui-fix
status: implemented-awaiting-webgl-review
source: manual
gdd_tags:
  - combat-attempt
  - answer-scoring
  - feedback
  - player-experience
  - guardrails
owner: project-owner
human_checkpoint: required
next_agent: qa-agent
blocked_by: []
---

# WebGL runtime media and UI fix

## Approved intake

On 2026-09-09 the project owner approved implementing the diagnosed WebGL fixes for missing Unicode symbols, embedded local video failures, the YouTube question layout/lifecycle, and missing result stickers. Publishing, deployment, build-profile changes, new secrets, and PR merge remain separate human checkpoints.

## Required behavior

- Runtime UI must not depend on Editor-only or host-OS glyph fallback.
- Local MP4/MOV presentation must use browser-streamable URL sources in WebGL and retain static-art failure fallbacks.
- Question playback begins fitted to the game viewport. After playback ends, answer and result UI remain centered; the complete question surface exits only after result feedback finishes.
- YouTube autoplay rejection, load failure, and stale callbacks must fail closed without leaving an opaque browser overlay over the game.
- Correct/failure stickers must be serialized or otherwise included in player builds without `UnityEditor.AssetDatabase`.

## State contract

`Lobby -> Committed -> WatchingVideo -> Answering -> ShowingResult -> BattleFeedback -> Lobby/Terminal`

- Video completion is the only ordinary entry to `Answering`.
- Submit or timeout is the only entry to `ShowingResult`.
- Result feedback completion dismisses the video/question surface before battle feedback.
- Content failure restores the attempt and removes every browser overlay.
- Duplicate/late browser events cannot advance a newer attempt.

## Compatibility boundary

ADR-006 remains the current live-question boundary. A true last-frame freeze with Unity UI layered over the image requires project-owned direct video media; a YouTube iframe cannot be captured into Unity and YouTube policy does not permit application overlays on the player. The fix may center Unity answer/result UI after hiding a completed YouTube iframe, while direct hosted question media remains a separately authored migration input.

## Acceptance

- [x] WebGL-visible UI uses packaged glyphs or image/ASCII equivalents for all required icons.
- [x] Authentication, opening, selection, and hub videos use URL sources in WebGL.
- [x] Missing URLs show stable fallback art and do not hang.
- [x] YouTube begins viewport-fitted and never leaves a blank navy overlay after blocked/error playback.
- [x] Answer/result panel remains centered and closes after result feedback.
- [x] Main Menu result stickers are direct build references.
- [ ] Browser bridge and focused Unity tests pass.
- [ ] Human WebGL visual/playback review is completed before publishing.

## Scope exclusions

- No deployment, CDN upload, Firestore content rewrite, build/CI setting, dependency, secret, merge, or release.
