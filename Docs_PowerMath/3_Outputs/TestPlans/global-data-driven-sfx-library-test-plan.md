# Test Plan: Global Data-Driven SFX Library & Controller

## Objective
Validate that all game sound effects (Player states, Enemy states with encounter kind and individual ownership, Question sequence states, Battle/Biome system, Character selection, and generic UI animation USS styles) trigger reliably and are tunable via ScriptableObjects without null reference exceptions or missing assets.

## Test Matrix

### 1. Automated EditMode Unit Tests (`SfxControllerTests.cs`)
- `SfxLibraryDefinition_GeneratesProceduralFallbacks_ForAllCues`: Verifies procedural audio generation for all sound keys when audio files are unassigned.
- `SfxCueConfig_PicksNonNullVariant_AndResolvesPitchRange`: Verifies randomized variant selection and pitch modulation within configured ranges.
- `SfxController_PlayPlayer_DoesNotThrow_AndPlaysExpectedCues`: Validates player Attack, Fail, Hit, Crit, Hurt, and Die cues.
- `SfxController_PlayEnemy_RespectsCustomProfileAndEncounterKind`: Validates enemy Appear, Attack, Hurt, and Die cues using encounter kinds and custom `IEnemySfxProfile` overrides.
- `SfxController_PlayQuestion_PlaysStateCuesWithoutThrowing`: Validates Question Sequence PopUp, KeypadTap, CountdownTick, ResultSuccess, ResultFail, and DamageMultiplying.
- `SfxController_PlayBattleAndCharacterSelection_PlaysCuesWithoutThrowing`: Validates BiomeTransition, StageAdvance, ActorClick, CardHover, CardClick, and CharacterAccepted cues.
- `SfxController_UiStyles_ResolvesConfiguredStyles`: Validates USS style class resolution and playback.

### 2. Manual PlayMode Verification
- Enter Authentication & Character Selection: Hover over Ricko & Stellar buttons to hear hover tick, click to hear selection confirmation.
- Enter Combat: Observe enemy appear audio, player sword swing on answer, hit/crit sound on impact, and enemy death sound.
- Open Question Sequence: Verify popup cue, keypad digit taps, countdown second ticks, success chime, and damage multiplying chord.
