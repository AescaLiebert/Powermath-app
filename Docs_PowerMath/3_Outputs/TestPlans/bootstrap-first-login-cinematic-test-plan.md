---
slug: bootstrap-first-login-cinematic
status: draft
gdd_tags: [player-experience, server-authority]
owner: Codex
human_checkpoint: required
next_agent: human
blocked_by: [final-copy-and-hosted-webgl-video]
---

# Bootstrap first-login cinematic verification

## Automated coverage

Run `PowerMath.Tests.EditMode.PlayerPreparationUiTests`. The test drives both Ricko and Stellar from the opening checkpoint through CharacterSelection, CharacterSelectedHolding, NameEntry, and completion. It asserts the mirrored theme, committed character/name data, and produces 1280x720 captures in `Logs/LifecycleValidation/`.

Run `PowerMath.Tests.EditMode.PlayerLifecycleTests`. The name policy accepts exactly 20 Unicode text elements, rejects 21, preserves Thai text, rejects control characters/markup, and keeps first-run versus existing-save routing intact.

## Starting motion values

These are starting values, not final tuning claims:

| Moment | Value | Intended player read |
|---|---:|---|
| Narrative glyph reveal | 0.025 s per text element | Deliberate visual-novel cadence without feeling stalled |
| Narrative line hold | 1.15 s | Enough time to finish a short line |
| Narrative line fade | 0.42 s | Soft chapter-like handoff |
| Square wipe cover | 0.34 s | Immediate selection acknowledgement |
| Square wipe reveal | 0.48 s | Graphic Persona-inspired reveal |
| Character enter | 0.66 s | Character and colored shadow separate clearly |
| Character exit | 0.38 s | Back/commit feels faster than entry |
| Hold pulse | 1.65 s ping-pong | Visible life without stealing focus from copy |
| Completion flash | 0.68 s | Clear transition punctuation before MainMenuScene |

Reduced-motion mode keeps the same state order and persistence gates, shortens lifecycle transitions to 0.06 s, and removes the ambient hold pulse.

## Player-experience playtest

Test with at least five first-time players at 16:9 plus one narrow layout, in Thai and English.

1. Ask players to select each side without instruction. Target: 4/5 identify both character hit regions on the first screen; if not, strengthen hover/focus affordance or the bottom prompt without making visible button boxes part of the composition.
2. Ask what changed after selection. Target: all players name the chosen character and notice that art/copy swap sides; if not, increase the enter separation or accent contrast before lengthening the transition.
3. Measure selection-to-Select availability. Target perceived response under one second, with no accepted double input during Entering/Exiting. If it feels slow, reduce reveal first, then enter, in 0.08 s increments.
4. Leave the detail screen open for 15 seconds. Target: no discomfort and copy remains primary; if pulse distracts, lower amplitude before changing duration.
5. Reload at `opening`, `character`, and `name`. Each must resume the latest acknowledged checkpoint with no duplicate initialization or lost selection.
6. Simulate video prepare failure. Opening retains Skip; character selection falls back to static art with both invisible hit regions usable.
7. Simulate save failure/conflict on character and confirmation. The screen must remain recoverable and must not enter MainMenuScene until the authoritative completion result succeeds.
8. Enter Thai combining marks, 20 text elements, 21 text elements, whitespace-only, and markup-like input. Count and validation must agree; final white flash occurs only for valid input.

Publishing a hosted WebGL video URL and changing build settings remain human checkpoints.
