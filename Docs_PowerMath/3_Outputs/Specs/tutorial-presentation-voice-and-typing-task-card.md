---
slug: tutorial-presentation-voice-and-typing
status: draft
source: manual
gdd_tags:
  - tutorial-system
  - audio-controller
  - player-experience
owner: Antigravity
human_checkpoint: required
next_agent: orchestrator-agent
blocked_by: []
---

# Task Card: Tutorial Presentation Improvements — Typewriter Animation and Dedicated Voice Channel

## Player-Facing Goal

When entering any tutorial dialogue/text step, the character's speech animates with a lively, kid-friendly typewriter typing effect rather than displaying the entire block of text abruptly. The speaker's voice plays on its own dedicated Voice audio channel, allowing clear audio delivery that adaptively interrupts and crossfades smoothly whenever a new voice or dialogue step triggers, eliminating awkward cut-offs or sound overlap. Players can tap at any time to instantly finish typing, and tapping again advances the dialogue.

## Source

- Origin: Manual request from user on 2026-09-19.
- Requested by: Project Owner.
- Canonical design: `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md` at `@tag:tutorial-system`.
- Workflow: `/implement-feature`.

## GDD Reference

- `@tag:tutorial-system` — "The tutorial is both story and playable onboarding. Power, the game's character mascot, forms an emotional relationship with the student while teaching the real interface through guided actions... Tutorial presentation uses a full visual-novel-style overlay... Reduced Motion replaces large character entrances or screen movement with fades and pose/emotion changes without removing story information."

## Current State

- `TutorialOverlayView` renders body text instantly via `_body.text = LocalizationService.Get(...)`.
- `TutorialOverlayView` plays dialogue sounds through `SfxController.Instance?.PlayUiStyle(step.AudioCueId, false)` into the generic SFX voice pool without a dedicated Voice channel.
- If steps change or another dialogue triggers, any playing voice or dialogue sound is not managed with adaptive interruption or crossfade.
- `SfxLibraryDefinition` manages music and generic SFX pools, but no dedicated `VoiceController` or Voice channel exists.

## Target State

1. **Typewriter Text Animation**:
   - `TutorialOverlayView` animates body text with a configurable, smooth typewriter effect on dialogue step entry.
   - If user taps during typing, the typing finishes instantly to display the complete line. A subsequent tap advances the dialogue.
   - If `reducedMotion` is enabled, typing animation is bypassed and text appears immediately.
   - Scheduling is safely tied to UI Toolkit and cancelled cleanly on advance, error, hide, or dispose.

2. **Dedicated Voice Channel & Controller**:
   - `VoiceController` (`IVoiceController`) introduced under `PowerMath.Audio` with dedicated `AudioSource` channel separation from Music and SFX.
   - Adaptive interruption: When a new voice line is requested, any currently playing voice is smoothly and rapidly faded down (e.g., 60–100ms) while the new voice cleanly takes over, preventing audio pops, clicks, or overlapping speech cacophony.
   - Graceful stop: Stopping speech fades smoothly without digital cutoff glitches.
   - Fallback voice synthesis: If an authored step doesn't yet have recorded voice audio assets, an adaptive synthesized speaker vocalization plays on the Voice channel, ensuring every text session has an audible, responsive speaker voice.
   - `TutorialOverlayView` triggers voice playback on dialogue step enter and stops/interrupts when dialogue advances or closes.

## Type

- [x] Feature
- [ ] Bug fix
- [x] UI/UX onboarding
- [x] Audio presentation polish
- [ ] Persistence migration
- [ ] Build/CI

## Scope

### Systems Affected

- `PowerMath.Audio`: `VoiceController`, `IVoiceController`
- `PowerMath.UI.MainMenu.Tutorial`: `TutorialOverlayView`, `TutorialDirector`
- Tests: `Assets/Project/Tests/EditMode/Audio/VoiceControllerTests.cs`

### Files To Inspect / Modify

- `Assets/Project/Script/Audio/Voice/IVoiceController.cs` (New)
- `Assets/Project/Script/Audio/Voice/VoiceController.cs` (New)
- `Assets/Project/Script/UI/MainMenu/Tutorial/TutorialOverlayView.cs` (Modify)
- `Assets/Project/Script/UI/MainMenu/Tutorial/TutorialDirector.cs` (Modify if needed)
- `Assets/Project/Tests/EditMode/Audio/VoiceControllerTests.cs` (New)

### Out of Scope

- Recording and authoring real voice actor .mp3/.wav assets for all dialogue lines (infrastructure and playback are created now; audio clip binding is supported via ScriptableObject / Resources).
- Modifying tutorial server persistence or state machine rules.

## Acceptance Criteria

- [ ] Every dialogue step in tutorial animates text via typewriter effect on enter.
- [ ] Tapping during typing instantly displays the full text without skipping to the next step.
- [ ] Tapping after typing has completed advances to the next step as usual.
- [ ] Reduced motion bypasses typing animation and displays full text immediately.
- [ ] Voice playback runs on a dedicated Voice channel separate from SFX pool and Music.
- [ ] Triggering a new voice while one is already playing adaptively interrupts the previous voice smoothly without pops or overlapping audio.
- [ ] Stopping or advancing dialogue cleanly stops the voice.
- [ ] EditMode unit tests cover VoiceController channel playback, volume scaling, adaptive interruption, and stop behavior.

## Human Checkpoints

- [x] Design/game-feel approval
- [ ] PR review before merge
