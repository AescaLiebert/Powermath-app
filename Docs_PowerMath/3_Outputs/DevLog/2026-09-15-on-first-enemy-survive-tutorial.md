# DevLog — `OnFirstEnemySurvive` Tutorial

Date: 2026-09-15

GDD references: `@tag:tutorial-system`, `@tag:combat-attempt`, `@tag:feedback`, `@tag:player-experience`.

## Outcome

Implemented a four-state localized tutorial that observes the first authoritative damaging standard-combat hit that leaves the same enemy alive and consumes its action. Power warns the player, explains the live Enemy Action Queue, guides an acknowledgement-only queue focus, and returns with encouragement.

## Architecture

- Extended the post-presentation tutorial result with persisted damage, defeat, flee, and enemy-action-consumption facts.
- Extracted eligibility into `TutorialCombatTriggerPolicy` with focused exclusion tests.
- Replaced Rank-specific result observation inside `TutorialDirector` with serialized, configured result-driven bindings in composition.
- Multiple tutorials triggered by the same receipt now save sequentially against the shared player revision.
- Registered `combat.enemy-actions` as a semantic UI Toolkit focus target whose action only acknowledges the tutorial.

## Content

- Added the `OnFirstEnemySurvive` ScriptableObject sequence and catalog entry.
- Added approved English/Thai warning, action-queue explanation, and encouragement copy.
- Reused scared, teaching, and cheerful Power emotion resources plus Reduced Motion behavior.

## Verification

- The sequence asset, transitions, localization keys, and catalog reference pass static validation.
- Combat Unity, runtime, Combat Unity EditMode tests, and Editor assemblies compile successfully.
- Final Unity Editor/mobile visual, reconnect, and simultaneous-trigger playtests remain a human checkpoint.
