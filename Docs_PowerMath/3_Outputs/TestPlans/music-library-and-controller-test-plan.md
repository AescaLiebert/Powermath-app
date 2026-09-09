---
slug: music-library-and-controller
status: approved
source: manual
gdd_tags:
  - feedback
  - player-experience
  - core-loop
owner: qa-agent
human_checkpoint: not-required
next_agent: human
blocked_by: []
---

# Test Plan: Music Library and Controller

## 1. Test Plan Summary

| Test Area | Priority | Type | Platform |
| --- | --- | --- | --- |
| AuthenticationScene login music loop | P0 | Functional + Audio verification | Windows Editor / WebGL |
| Character Selection Static replay/continuation | P0 | Functional + Lifecycle verification | Windows Editor / WebGL |
| MainMenuScene battle music per biome | P0 | Functional + Audio verification | Windows Editor / WebGL |
| Big Boss battle interruption channel separation | P0 | Functional + Automated EditMode | Windows Editor / WebGL |
| Continuous battle music playback without retrack | P0 | Functional + Automated EditMode | Windows Editor / WebGL |
| Question Sequence & YouTube ducking (-80% to -90%) | P0 | Functional + Automated EditMode | Windows Editor / WebGL |
| Smooth lerped volume transitions & crossfades | P1 | Automated + Audio verification | Windows Editor / WebGL |
| Fallback procedural synthetic audio generation | P1 | Automated EditMode | Windows Editor / WebGL |

---

## 2. Functional Tests

### TEST-01: Login Looping Music on AuthenticationScene
- **GIVEN**: Player launches the game and opens `AuthenticationScene`.
- **WHEN**: Scene initializes.
- **THEN**: `MusicController.Instance` starts looping `LoginMusic` on `ThemeChannel` with a smooth crossfade in from volume 0 to target volume (~0.85f).
- **PRIORITY**: P0.

### TEST-02: Character Selection Static Replay
- **GIVEN**: Player completes authentication and enters onboarding character selection (`PlayerPreparationPresenter` at `CharacterSelection`).
- **WHEN**: Character selection UI is shown (even after opening video/narrative).
- **THEN**: `MusicController.Instance.PlayLoginMusic()` ensures the login theme is playing smoothly, without abrupt pops or clipping.
- **PRIORITY**: P0.

### TEST-03: MainMenuScene Battle Music Playback & Biome Mapping
- **GIVEN**: Player enters `MainMenuScene`.
- **WHEN**: `CombatLobbyCompositionRoot` renders the current biome (e.g. `snapshot.BiomeId`).
- **THEN**: `MusicController.Instance.PlayBattleMusic(biomeId)` resolves the appropriate biome track (or default battle track) and smoothly fades in while fading out the theme channel.
- **PRIORITY**: P0.

### TEST-04: Big Boss Interruption Channel Separation
- **GIVEN**: Normal Battle music is actively playing in `MainMenuScene`.
- **WHEN**: A Big Boss encounter appears (`StageEncounterKind.BigBoss` or `FinalBoss`).
- **THEN**:
  1. `BossChannel` starts playing the Boss Battle theme and smoothly fades in to full volume (~0.95f) over `BossInterruptDuration` (0.8s).
  2. `BattleChannel` smoothly fades volume down to 0 over `BossInterruptDuration`.
  3. **Crucial**: `BattleChannel.Stop()` is **NOT** called; its playback timeline continues running silently in the background.
- **PRIORITY**: P0.

### TEST-05: Resumption Without Retracking
- **GIVEN**: Big Boss was interrupting, and BattleChannel has been playing silently at 0 volume.
- **WHEN**: Big Boss is defeated or replaced by a normal/event encounter.
- **THEN**:
  1. `BossChannel` smoothly fades volume down to 0 and stops.
  2. `BattleChannel` smoothly fades volume back up to full target volume over `BossInterruptDuration`.
  3. `BattleChannel` resumes playing at its current elapsed timeline position (without retracking or restarting from 0:00).
- **PRIORITY**: P0.

### TEST-06: Question Sequence & YouTube Volume Ducking
- **GIVEN**: Battle or Boss music is playing at normal volume in combat.
- **WHEN**: Player initiates an attack attempt (`CombatLobbyPresenter.OnAttack()`) and Question Sequence / YouTube is presented.
- **THEN**:
  1. Music ducking activates (`SetDucking(true)`).
  2. Combat music volume smoothly lerps down by 85% (volume multiplier = 0.15f) over `DuckFadeDuration` (0.4s).
  3. Music continues playing at the reduced volume throughout the question solving and video playback.
- **WHEN**: Question is answered and resolution completes (`ResolutionRoutine`), or presentation fails/dismisses.
- **THEN**:
  1. Music ducking deactivates (`SetDucking(false)`).
  2. Combat music volume smoothly lerps back up to normal 100% volume over `DuckFadeDuration`.
- **PRIORITY**: P0.

---

## 3. Automated Unit Tests (`MusicControllerTests.cs`)

- `MusicLibraryDefinition_FallbackClips_GenerateValidLoopingAudio`: Validates that zero-audio-asset fallback procedural clips are synthesized with valid lengths and amplitudes.
- `MusicLibraryDefinition_ResolveBattleTrack_ResolvesBiomeOrFallback`: Validates biome track matching and fallback resolution.
- `MusicController_PlayLoginMusic_SmoothlyFadesIn`: Validates volume progression during smooth crossfade.
- `MusicController_BossInterruption_SeparatesChannelsWithoutRetracking`: Validates channel separation, volume fade down to 0, no `Stop()` called, and resumption without timeline reset.
- `MusicController_SetDucking_ReducesCombatVolumeBy80To90Percent`: Validates that ducking factor drops volume by ~85% and restores accurately.
- `MusicController_Mute_SilencesActiveChannels`: Validates mute behavior.
