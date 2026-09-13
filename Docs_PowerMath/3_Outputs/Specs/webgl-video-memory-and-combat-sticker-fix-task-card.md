---
slug: webgl-video-memory-and-combat-sticker-fix
status: completed
source: manual
gdd_tags:
  - combat-attempt
  - feedback
  - video-playback
  - performance
  - webgl-stability
owner: project-owner
human_checkpoint: cleared
next_agent: none
blocked_by: []
---

# WebGL Video Memory Crash & Combat Sticker Fix

## Problem Statement

1. **Memory Access Out of Bounds / Freezing During Video Playback**:
   - WebGL builds crash with `RuntimeError: memory access out of bounds` in browsers (desktop and mobile).
   - In-game video assets are authored at **2612x1440** (1440p / 2.5K) and **1440x2162** at **14–15 Mbps**, totaling ~46 MB in `StreamingAssets/Videos/`. Decoding and uploading 15 MB per frame via `gl.texSubImage2D` exhausts WebGL memory and triggers WASM heap detachment/traps.
   - `PingPongVideoPlayer` switches `_player.url` at runtime during playback, triggering race conditions in WebGL video stream resets.
   - `webGLMemorySize: 32` in `ProjectSettings.asset` vs `webGLInitialMemorySize: 256` can cause memory growth buffer detachment.

2. **Result-Sticker in Combat-Feedback-Card Missing in Player Builds**:
   - In `CombatSurface.uxml`, `combat-result-sticker-correct` and `combat-result-sticker-fail` used `project://database/` URIs which are stripped in player builds.
   - `CombatLobbyCompositionRoot.ResolveResultStickers()` attempts fallback to `Resources.Load<Sprite>("Character/Sticker_Power_Correct")`, but the sprites were not located in any `Resources` folder.
   - `CombatLobbyView.ShowAnswerFeedback()` hid the stickers because the loaded sprites were null, and hid the correct/fail elements whenever `_resultSticker != null`.
   - `.combat-result-sticker` in USS had self-transition properties (`scale, opacity`) interfering with parent card transitions.

3. **Video Player Architecture & Optimization**:
   - The project uses Unity's built-in `UnityEngine.Video.VideoPlayer` for internal videos and `PowerMathYouTube.jslib` for combat YouTube questions.
   - Downscaling videos to 720p at ~1.5–2.0 Mbps will reduce file size by ~85% (from 46 MB to ~7 MB) and texture memory bandwidth by ~75%, preventing crashes without needing third-party plugin overhead.

## Acceptance Criteria

- [x] Video assets in `Assets/Project/Art/...` and `Assets/StreamingAssets/Videos/` re-encoded to 720p (1280x720 landscape, 720x1080/720x1082 portrait) with H.264 web-optimized bitrates (1.5–2.0 Mbps).
- [x] `Sticker_Power_Correct.png` and `Sticker_Power_Fail.png` added to `Assets/Project/Resources/Character/` so `Resources.Load<Sprite>` is 100% reliable in player builds.
- [x] `CombatSurface.uxml` cleaned of `project://database/` URIs for stickers.
- [x] `.combat-result-sticker` in USS has self-transitions removed; stickers behave cleanly as direct child elements of `combat-feedback-card`.
- [x] `CombatLobbyView.cs` provides a clean method (`SetResultStickerVisibility` / `ShowAnswerFeedback`) to explicitly display either "success" or "failure" visual element without null-checks failing in builds.
- [x] `PingPongVideoPlayer.cs` guards against mid-stream URL swapping traps and null URL dereferences in WebGL.
- [x] `webGLInitialMemorySize` in `ProjectSettings.asset` set to 512 MB to prevent WebAssembly memory resizing buffer detachment during texture uploads.
- [x] Existing node tests and EditMode contracts pass.
