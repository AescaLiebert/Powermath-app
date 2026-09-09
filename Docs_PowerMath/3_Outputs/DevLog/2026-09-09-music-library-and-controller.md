# DevLog: 2026-09-09 — Music Library & Controller

## Goal

Implement a unified Music Library and Controller for the game, featuring smooth configurable crossfading, channel-separated uninterrupted battle music during Big Boss appearances, and smooth volume ducking during Question Sequence / YouTube playback.

## What I Did

- [x] Implemented `MusicTrackConfig` and `BiomeMusicBinding` in `MusicTrackDefinition.cs`.
- [x] Implemented `MusicLibraryDefinition` ScriptableObject with core tracks (Login, Battle, Boss), biome mapping, transition duration tuning, and procedural harmonic fallback audio synthesis.
- [x] Created `Resources/MusicLibrary.asset` with default tuning for immediate designer inspection.
- [x] Implemented `IMusicController` and persistent `MusicController` MonoBehaviour with 3 dedicated channels (`ThemeChannel`, `BattleChannel`, `BossChannel`).
- [x] Implemented seamless channel separation: when Big Boss appears, `BossChannel` fades in while `BattleChannel` fades to volume 0 without calling `Stop()` or resetting `time`, allowing it to resume seamlessly when the boss encounter concludes.
- [x] Implemented smooth volume ducking (-85% volume reduction, configurable -80% to -90%) via `SetDucking(bool)` with continuous unscaled time lerp.
- [x] Hooked login music into `AuthenticationPresenter` on startup and `PlayerPreparationPresenter` on Character Selection Static.
- [x] Hooked battle music and Big Boss detection into `CombatLobbyCompositionRoot`.
- [x] Hooked Question Sequence & YouTube ducking into `CombatLobbyPresenter` and `QuestionSequenceController`.
- [x] Created assembly definition files `PowerMath.Audio.asmdef` and `PowerMath.Audio.EditModeTests.asmdef`.
- [x] Wrote automated EditMode unit tests in `MusicControllerTests.cs`.

## Key Decisions

- **Dedicated Channels for Zero-Retrack Boss Battles**: Instead of pausing or restarting battle music, `BattleChannel` and `BossChannel` run concurrently. During a Big Boss battle, `BattleChannel` volume fades to 0 but its playback timeline keeps advancing, ensuring that when the boss is defeated, the regular battle theme returns without jarring timeline rewinds.
- **Continuous Unscaled Time Lerping**: All volume transitions and crossfades are updated in `MusicController.Update()` using `Time.unscaledDeltaTime`, guaranteeing smooth transitions even if game pauses or timescale is adjusted.
- **Fail-Soft Procedural Synthesizer**: To ensure zero runtime exceptions and full testability before commercial audio assets are imported, `MusicLibraryDefinition` automatically generates harmonic waveform loops if inspector audio clips are null.
- **Assembly Partitioning**: Placed the audio system in a dedicated `PowerMath.Audio` assembly with clean dependencies referenced by `PowerMath.Gameplay.Combat.Unity` and `Assembly-CSharp`.

## Verification

- `MusicControllerTests.cs`: 6 tests covering procedural audio synthesis, biome track mapping, login fade-in, boss channel separation, volume ducking, and muting.
- Unity compilation clean with no C# compile errors.

## Next Session

- Assign production music tracks (`.mp3`/`.ogg`) into `Assets/Project/Resources/MusicLibrary.asset`.
- Tune crossfade and ducking durations during onboarding and combat playtesting.
