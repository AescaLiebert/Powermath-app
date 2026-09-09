---
slug: bootstrap-first-login-cinematic
status: draft
source: user-request-and-figma
gdd_tags: [player-experience, server-authority]
owner: Codex
human_checkpoint: required
next_agent: human
blocked_by: [final-copy-and-hosted-webgl-video]
---

# Bootstrap first-login cinematic

Converted Figma section `118:479` into the existing BootstrapScene UI Toolkit preparation overlay. Reused the checked-in opening/selection MP4s, separated character sprites, overview art, and shared scene motion driver. Added explicit narrative, opening video, character-selection, selected Enter/Hold/Exit, name-entry, confirming, and completing states with mirrored Ricko/Stellar layouts and a reduced-motion path.

Persistence order remains checkpointed and resumable: opening completion, character selection, then final character/name completion. The MainMenu callback remains behind successful persistence and the final white flash. Existing completed players are unaffected.

Added 20-text-element validation coverage, state-aware dual-character UI coverage, deterministic 1280x720 capture points, WebGL URL/fallback authoring fields, and a player-experience tuning plan. No dependency, scene list, build profile, CI, Firebase rule, or publishing change was made.

Verification completed in Unity 6000.5.3f1 against the workspace sources: the cinematic UI test passed 1/1 and the lifecycle regression suite passed 14/14. Captures and NUnit XML are retained under `Logs/LifecycleValidation/bootstrap-cinematic-2026-09-09/` for the design checkpoint.
