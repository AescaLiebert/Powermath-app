---
slug: global-data-driven-sfx-library
status: approved
source: manual
gdd_tags:
  - feedback
  - audio
  - core-loop
  - player-experience
owner: implementation-agent
human_checkpoint: not-required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: Global Data-Driven SFX Library & Controller

## Player-Facing Goal

Provide a cohesive, responsive audio experience across the entire game through a centralized, data-driven SFX architecture:
1. **Enemy States**: Appear, Hurt, Attack, and Die. Supports global sound defaults, per-Encounter-Kind sound sets (Normal, Elite, Miniboss, BigBoss, FinalBoss, ChallengeEvent), and custom audio ownership overrides on specific `EnemyDefinition` and `EventDefinition` assets.
2. **Player States**: Attack (swing), Fail Attack, Hit, Critical Hit, Hurt, and Die.
3. **Question Sequence States**: Modal Pop-up, Keypad digits/submit, Countdown timer ticking, Result Success, Result Failure, and Damage Multiplying buildup.
4. **Battle & World System**: Biome transitions, stage advancements, run victory/defeat, and actor interactive click.
5. **Character Selection State**: Card/Button hover, card click, character choose/confirm, and back navigation.
6. **Generic UI Animation SFX**: Reusable data-driven audio cues mapped to UI Toolkit USS style classes (e.g. `.btn-primary:hover`, `.shake-card`, `.modal-popup`, `.tab-switch`) for designers to easily select and customize in the Inspector.

## Source

- Origin: User request on 2026-09-09.
- Key Requirements:
  - Global / Data-driven rather than hardcoded inside specific classes.
  - Enemy all states (appear / hurt / attack / die), with encounter kind uniqueness and individual event/enemy ownership of SFX.
  - PlayerState (hurt / attack / fail / hit / crit hit / die).
  - Question sequence all states (popup / countdown tick / result fail / result success / damage multiplying).
  - Battle system (biome change, stage progress).
  - Character selection state (hover / click).
  - Generic UIAnimation SFX struct (like USS Style) for designers to choose.

## GDD Reference

- `@tag:feedback` - Every user interaction and combat event must provide distinct, satisfying auditory feedback.
- `@tag:audio` - Sound design must be modular, layered, non-repetitive (pitch/variant variation), and easily tunable via ScriptableObjects.
- `@tag:core-loop` - Question answering, attack resolution, and boss encounters must feel impactful.

## Scope & Proposed Changes

- `PowerMath.Audio`:
  - `SfxCueConfig`: Struct/Class for multi-clip randomization, volume scaling, and pitch variation.
  - `IEnemySfxProfile` & `IEventSfxProfile`: Audio profile contracts for individual monster and challenge overrides.
  - `UiAnimationSfxStyle`: Struct for USS style class and animation trigger mapping.
  - `SfxLibraryDefinition`: Master ScriptableObject asset containing categorized sound cues and procedural synth fallbacks.
  - `ISfxController` & `SfxController`: Persistent singleton managing voice pools and typed SFX playback.
  - `UiSfxAudioBinder`: UI Toolkit binder mapping USS classes to UI animation sound cues.
- `PowerMath.Gameplay.Combat.Unity`:
  - `EnemyDefinition`: Implement `IEnemySfxProfile` for custom sound ownership overrides.
  - `EventDefinition`: Implement `IEventSfxProfile` for custom sound ownership overrides.
  - `ActorPresentationController`: Hook enemy appear/hurt/attack/die and player attack/fail/hurt/die audio triggers.
  - `CombatFeedbackPlayer` & `CombatAudioPlayer`: Bridge to `SfxController`.
- `PowerMath.UI.QuestionSequence`:
  - `QuestionSequenceController`: Trigger popup, countdown tick, keypad tap, success, failure, and damage multiplying SFX.
- `PowerMath.PlayerLifecycle`:
  - `PlayerPreparationView`: Register hover and click audio on character cards and action buttons.
