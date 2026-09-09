# 2026-09-09 Global Data-Driven SFX Library & Controller Upgrade

## What Was Done
- Upgraded the SFX architecture from hardcoded combat playback to a centralized, data-driven system.
- Created `PowerMath.Audio`:
  - `SfxCueConfig`: Configurable data asset supporting multi-clip variation, volume scaling, and pitch randomness.
  - `SfxLibraryDefinition`: Master ScriptableObject asset containing categorized sound cues for Player, Enemy, Question Sequence, Battle, Character Selection, and USS UI styles, backed by procedural synthesis fallbacks.
  - `ISfxController` & `SfxController`: Persistent singleton managing a multi-voice AudioSource pool with typed playback methods (`PlayPlayer`, `PlayEnemy`, `PlayQuestion`, `PlayBattle`, `PlayCharacterSelection`, `PlayUiStyle`).
  - `UiSfxAudioBinder`: UI Toolkit helper mapping USS style classes to audio cues.
- Implemented `IEnemySfxProfile` and `IEventSfxProfile` on `EnemyDefinition` and `EventDefinition` for custom audio ownership overrides.
- Hooked audio triggers in `ActorPresentationController` (enemy appear/hurt/attack/die; player attack/fail/hurt/die; actor click).
- Hooked audio triggers in `QuestionSequenceController` (popup, keypad tap, keypad clear, submit, countdown tick, score multiplying, and failure).
- Hooked audio triggers in `PlayerPreparationView` (character card hover, click, accept, confirm, and back navigation).
- Created EditMode unit tests in `SfxControllerTests.cs`.
