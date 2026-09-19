# DevLog — State-Driven `OnFirstCreate` Tutorial

Date: 2026-09-15

GDD references: `@tag:tutorial-system`, `@tag:combat-attempt`, `@tag:server-authority`, `@tag:feedback`, `@tag:player-experience`, `@tag:guardrails`, `@tag:playtest`.

## Outcome

Implemented the first complete tutorial sequence as reusable tutorial infrastructure. `OnFirstCreate` now queues after character creation, waits for a safe standard-enemy Lobby, introduces Power and Math:World, guides one production combat action, hides throughout the real timed question and result presentation, reacts to the persisted first-attempt outcome, waits for a different standard encounter, and persists completion after the final handoff.

Only `OnFirstCreate` is authored and enabled. Future tutorials can add catalog definitions, localization, and semantic targets without adding tutorial-ID branches to the reducer or presenter.

## Runtime and Content

- Added a Unity-free immutable tutorial sequence/progress/signal model and pure reducer.
- Added a ScriptableObject sequence/catalog with stable step IDs, outcome rules, localization keys, emotion/sprite/audio cues, and primary/fallback target IDs.
- Added a scene-scoped `TutorialDirector`, localized UI Toolkit overlay, and `TutorialTargetRegistry` supporting scene `RectTransform` and UI Toolkit targets.
- Added semantic combat commit/result checkpoints and stable-Lobby notification. The guided target calls the ordinary combat coordinator path once.
- Tutorial interaction owns the shared gate only for dialogue/focus. It releases before question input, result presentation, and normal encounter-clear combat.
- A tutorial found `Active` on a fresh scene load is durably re-queued and restarts from its authored safe entry. Stale focus targets and panel ownership are never resumed across process lifetime.

## Persistence

- Advanced the player schema from V4 to V5 with a dynamic Firebase `tutorialMap` mapped to deterministic client entries.
- Added empty-map patch support, defaults validation, migration, save contract, and full-reset behavior.
- Added revision-checked/idempotent tutorial lifecycle patches and prevented completed tutorials from reopening.
- Merged tutorial save revision/progress into the existing live snapshot object so the active gameplay persistence adapter observes the same revision.
- Retained the legacy tutorial DTO for compatibility; it is no longer the `OnFirstCreate` source of truth.

## Localization and Presentation

- Added approved English/Thai speaker, welcome, Math:World, attack prompt, correct, incorrect, timeout, abandoned, and handoff copy.
- Locale changes refresh the visible step without mutating tutorial state.
- Added responsive narrow layout and Reduced Motion styling. Current approved Power sticker resources are loaded by authored resource path.

## Bugs Found and Fixed

- Fixed a live playtest blocker where the tutorial could acquire the global interaction gate before its overlay was proven visible. Presentation now renders first, moves to the front with explicit full-screen visibility, and acquires the gate only afterward. Missing/detached presentation fails open with localized recovery messaging.
- Releasing the tutorial gate on target acceptance could synchronously re-enter the director and reacquire it, blocking question input. Target-acceptance suppression and release ordering now prevent reacquisition.
- Replacing the player snapshot after a tutorial write would leave combat persistence with a stale revision. Tutorial writes now merge into the shared live object.
- Startup notices could prevent the initial stable-Lobby signal. The signal now publishes whenever no pending combat presentation owns recovery.
- Empty legacy outcome strings could make otherwise valid tutorial progress unreadable. They now map to `None`.
- Full player reset initially left tutorial entries in memory. Reset now clears both the persisted map and local entries.
- The visible overlay previously ignored pointer picking, allowing transparent regions to pass clicks into unrelated UI. It now owns the entire pointer surface while visible, with only its authored focus proxy or dialogue action enabled.
- Persisted focus steps could resume without the panel/scene state they depended on and hard-lock interaction. Active tutorials now restart on launch while preserving `rewardClaimed`, variant, and original trigger data so future reward tutorials cannot be farmed by quitting.

## Verification

- Tutorial Core, modified Combat Unity, full runtime Assembly-CSharp, full Editor tests, and tutorial EditMode tests compile with Unity's compiler response-file references.
- Eight focused tutorial state/persistence test methods passed with zero failures.
- A standalone pure reducer scenario passed through queue, commit, transaction guard, outcome branch, encounter handoff, completion, and no-replay assertions.
- Localization JSON is valid with unique tutorial keys; modified UXML files parse as XML.
- Scoped `git diff --check` passed without whitespace errors.
- Full Unity Test Runner and manual Editor/mobile WebGL playtests remain pending because the project is open in the interactive Editor and a second process cannot safely take the project lock.

## Checkpoints

- Design and architecture were approved by the project owner with `lgtm` on 2026-09-14.
- ADR-020 records the accepted generic tutorial boundary.
- Implementation review and visual/recovery playtest remain required before merge.
- No Firebase rules, hosted schema manifest, build setting, CI, dependency, secret, deployment, merge, or publication was changed.

## Git Commit Summary

```text
feat(tutorial): implement state-driven OnFirstCreate flow

- add authored localized tutorial reducer, overlay, and semantic targets
- persist V5 tutorialMap progress at combat-safe checkpoints
- cover migration, idempotency, recovery, and truthful outcome branches
```
