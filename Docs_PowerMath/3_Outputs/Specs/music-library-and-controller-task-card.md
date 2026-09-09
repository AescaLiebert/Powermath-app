---
slug: music-library-and-controller
status: approved
source: manual
gdd_tags:
  - feedback
  - player-experience
  - core-loop
owner: implementation-agent
human_checkpoint: not-required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: Music Library and Controller

## Player-Facing Goal

Provide a rich, responsive, and seamless musical experience across game phases:
1. Looping Login music on `AuthenticationScene` that continues or replays smoothly upon reaching Character Selection (Static).
2. Continuous Battle music in `MainMenuScene`, with separated audio channels so that when a Big Boss appears, the Boss Battle theme smoothly takes over while the regular Battle music continues playing silently in the background, allowing the battle music to resume without restarting/retracking when the Boss encounter ends.
3. Automatic, smooth volume ducking (-80% to -90%) during Question Sequences and embedded YouTube video playback, returning smoothly to full volume once the question window closes.
4. Smooth, lerped volume and crossfade transitions across all track and state changes.

## Source

- Origin: User request on 2026-09-09.
- Requirements:
  - Login looping music on `AuthenticationScene`, replayed on Character Selection Static.
  - Battle music on `MainMenuScene`, Boss battle music triggers only on Big Boss appearance.
  - Seamless channel separation: Battle music continues playing in the background without retracking when interrupted by Boss Battle music.
  - Support future per-biome battle music in the Music Library.
  - Question sequence and YouTube appearance ducks all battle music by -80% to -90% (without stopping) until the question sequence window ends.
  - All volume changes and transitions must be smoothly lerped.

## GDD Reference

- `@tag:feedback` - Ambient and interactive audio must provide clear, cohesive sensory feedback.
- `@tag:player-experience` - Background music transitions must be seamless and never jarring.
- `@tag:core-loop` - Combat flow transitions between normal encounters, boss encounters, and question solving must feel continuous.

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- `PowerMath.Audio`: New `MusicLibraryDefinition`, `MusicTrackDefinition`, `IMusicController`, and `MusicController`.
- `PowerMath.UI.Authentication.AuthenticationPresenter`: Hook login music playback.
- `PowerMath.PlayerLifecycle.PlayerPreparationPresenter`: Hook login music playback on Character Selection.
- `PowerMath.UI.MainMenu.CombatLobbyCompositionRoot`: Hook battle music (per-biome) and Big Boss appearance.
- `PowerMath.Gameplay.Combat.Unity.CombatLobbyPresenter`: Hook question sequence and YouTube volume ducking.
- `PowerMath.UI.QuestionSequence.QuestionSequenceController`: Hook standalone/modal question sequence volume ducking.

### Files Created / Modified

- `Assets/Project/Script/Audio/Music/MusicTrackDefinition.cs` [NEW]
- `Assets/Project/Script/Audio/Music/MusicLibraryDefinition.cs` [NEW]
- `Assets/Project/Script/Audio/Music/IMusicController.cs` [NEW]
- `Assets/Project/Script/Audio/Music/MusicController.cs` [NEW]
- `Assets/Project/Resources/MusicLibrary.asset` [NEW]
- `Assets/Project/Script/UI/Authentication/AuthenticationPresenter.cs` [MODIFY]
- `Assets/Project/Script/PlayerLifecycle/PlayerPreparationPresenter.cs` [MODIFY]
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs` [MODIFY]
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatLobbyPresenter.cs` [MODIFY]
- `Assets/Project/Script/UI/QuestionSequence/QuestionSequenceController.cs` [MODIFY]
- `Assets/Project/Tests/EditMode/Audio/MusicControllerTests.cs` [NEW]

### Acceptance Criteria

- [x] `MusicLibraryDefinition` ScriptableObject exposes Login, Default Battle, Boss Battle, and per-Biome battle tracks.
- [x] Fallback synthetic procedural music is generated if AudioClips are unassigned, preventing runtime errors.
- [x] `AuthenticationScene` starts looping login music with a smooth fade-in.
- [x] Character Selection (Static) onboarding step plays/resumes login music smoothly.
- [x] `MainMenuScene` starts battle music matching the current Biome (or fallback).
- [x] When Big Boss appears, Boss Battle music fades in on a dedicated channel; normal Battle music fades to 0 but keeps running (no Stop(), no retracking).
- [x] When Big Boss encounter finishes, Boss Battle music fades out; normal Battle music fades back in at its current timeline position.
- [x] Question Sequence / YouTube triggers smooth volume ducking by 80%-90% (-14dB to -20dB), and restores smoothly upon completion.
- [x] All volume transitions (fade in, fade out, crossfade, ducking) use smooth unscaled time lerping.
- [x] EditMode unit tests validate state transitions, volume calculation, channel separation, and ducking logic.
