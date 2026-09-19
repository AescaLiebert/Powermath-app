---
slug: r2-streaming-assets-and-audio-ducking-fix
status: complete
source: manual
gdd_tags:
  - combat-attempt
  - video-playback
  - audio-controller
  - webgl-stability
owner: project-owner
human_checkpoint: completed
next_agent: none
blocked_by: []
---

# R2 StreamingAssets Video Deployment & Combat Audio Ducking Silence Fix

## Problem Statement

1. **Missing MP4 Videos in Player Builds (R2 Issue)**:
   - In the Unity Editor, `VideoPlayer` instances directly play embedded `VideoClip` assets.
   - In WebGL player builds, videos are dynamically streamed via `StreamingVideoPath.TryResolve(...)` from `Application.streamingAssetsPath`, configured in `index.html` as `https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/StreamingAssets`.
   - The `Build/` folder was uploaded to Cloudflare R2, but `StreamingAssets/` was omitted, causing HTTP 404 on all 6 `.mp4` video files (`V2_LogInLive2D.mp4`, `V2_LogInLive2D_Reverse.mp4`, `V_Hub_Stand_Ricko.mp4`, `V_Hub_Stand_Stellar.mp4`, `V_SelectCharacterScene.mp4`, `WelcomeToWorldVideo.mp4`).

2. **Music Going Silent After YouTube Question Ducking**:
   - `CombatLobbyPresenter.BeginQuestionRoutine()` activates volume/pitch ducking via `SetMusicDucked(true)`.
   - When the question sequence finishes and an enemy is defeated, `CombatFeedbackPlayer.PlayBattleResolution()` checks `IsBossActive || IsEncounterOverrideActive` and calls `_audio?.FadeOutBossMusic(1.2f)`.
   - `MusicController.FadeOutBossMusic()` fades out `_bossChannel` to 0, but omits restoring `_battleChannel.TargetBaseVolume = 1f`. Both channels end up at volume 0.0, silencing game music until a new biome or clip change occurs.
   - `PowerMathYouTube.jslib`'s `PowerMathYouTubeHide()` destroys the YouTube player iframe without refocusing `unity-canvas`, risking canvas blur and `AudioListener.pause = true` in WebGL.

## Acceptance Criteria

- [x] `StreamingAssets/` folder deployment instructions and automated verification tool (`Test-CloudflareR2Deployment.ps1`) provided.
- [x] `MusicController.FadeOutBossMusic()` smoothly restores underlying `_battleChannel` volume to 1.0.
- [x] `PowerMathYouTube.jslib` ensures `focusUnityCanvas()` and `resumeUnityAudio()` are called when hiding the player overlay.
- [x] Automated EditMode and Node.js test contracts pass.
