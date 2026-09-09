# 2026-09-09 — WebGL runtime media and UI fix

## Bugs found

- UI Toolkit used `zh-cn.ttf` for global text, while several visible icons were emoji or symbols absent from that font. Editor OS fallback concealed the missing packaged glyphs; WebGL had no equivalent fallback.
- Local presentation code selected serialized `VideoClip` assets, but Unity Web does not support embedded clips. The authored hosted URL fields were empty, so browser playback had no valid source.
- The YouTube bridge intentionally created an inset/docked iframe with an opaque navy background and lacked bounded handling for autoplay rejection or startup failure.
- Main Menu result sticker fields were unassigned. Editor-only `AssetDatabase` lookup concealed that `Resources.Load` could not find those sprites in a player build.

## Fix

- Replaced unsupported text icons with glyph-safe labels and added a source-policy regression test.
- Copied the six local H.264 MP4 assets to `StreamingAssets/Videos`, added a shared URL resolver, and selected URL playback for authentication, opening, character selection, and hub presentation on WebGL.
- Made the YouTube layer canvas-fitted, black, stale-callback-safe, autoplay-aware, and startup-time-bounded. Completion hides the cross-origin iframe and leaves the Unity answer/result panel centered.
- Serialized correct/failure sticker sprite references into both question hosts.

## Verification

- Browser regression suite: 13 passed, 0 failed.
- Streaming copies: six of six SHA-256 comparisons matched their authored sources.
- `git diff --check` reports existing whitespace issues in unrelated dirty files; the touched opening-sequence whitespace was corrected.
- Unity completed a successful WebGL build earlier in the same Editor session, but the final post-change Unity compile and browser visual/playback pass remain human QA checkpoints because native Editor control is unavailable in this environment.

No publishing, deployment, dependency, build-profile, secret, merge, or destructive action was performed.
