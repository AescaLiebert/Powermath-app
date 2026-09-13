# 2026-09-11 — Mobile WebGL and Firebase Admin State Regression

## Bugs found

- Authentication Enter/login pointer and both actor pointer paths explicitly requested fullscreen.
- The YouTube overlay lived outside the fullscreen canvas subtree.
- Autoplay requested audible media, iframe focus could pause Unity, and a paused
  player had no bounded terminal recovery.
- Combat music was ducked to 15% throughout question presentation.
- Admin combat/Quick Test state was static and session-only; persisted commands
  locally mutated an old snapshot before reloading.

## Fix

- Removed implicit fullscreen entry and made explicit fullscreen own the complete
  Unity container, including the YouTube layer.
- Added muted inline autoplay, focus return, pause recovery, bounded failure,
  overlay-first cleanup, and guarded browser-to-Wasm callbacks.
- Removed question-time music ducking.
- Replaced username-derived admin access and temporary combat/Quick Test state
  with Firebase `userdata.admin` and `gamedata.adminTuning`.
- Every successful admin patch now reloads and rehydrates the canonical Firebase
  player before continuing. Only full Reset intentionally returns to Stage 1.

## Verification

- Node WebGL bridge/media suite: 20 passed, 0 failed.
- Unity `Assembly-CSharp` Roslyn compile: passed.
- Unity `Assembly-CSharp-Editor` Roslyn compile: passed.
- Deployed Android/iOS and disposable Firebase-account checks remain human QA checkpoints.

No publishing, build-setting, Firebase rules, dependency, secret, merge, or destructive operation was performed.
