---
slug: on-first-enemy-survive-tutorial
status: approved
source: manual
human_checkpoint: required
---

# Architecture Plan: `OnFirstEnemySurvive`

> Approved by the project owner with `lgtm` on 2026-09-15.

## Current Implementation

- Combat already publishes a post-presentation `CombatTutorialResult` containing outcome, damage/defeat/flee facts, source/destination snapshots, and rank transition.
- The Enemy Action Queue is rendered at `combat-enemy-actions` from authoritative remaining/maximum cooldown.
- `TutorialDirector` currently embeds Rank-change result eligibility, which should not grow into one branch per tutorial ID.

## Target Architecture

1. Add a small data result trigger contract to the tutorial composition boundary: a predicate maps `CombatTutorialResult` to an optional queued tutorial variant.
2. Configure `OnFirstRankChange` with its current Rank predicate and `OnFirstEnemySurvive` with the surviving-hit predicate; keep the reducer tutorial-ID agnostic.
3. Extend `CombatTutorialResult` only if necessary to expose final damage and cooldown consumption explicitly. Eligibility must be derived from persisted receipt facts, not UI animation or HP polling.
4. Register semantic target `combat.enemy-actions` against `combat-enemy-actions` with an acknowledgement callback returning success and no gameplay command.
5. Add the localized ScriptableObject sequence to `TutorialCatalog`; reuse the current overlay, gate, persistence, safe-state, restart, and queue-priority behavior.
6. Add pure predicate/reducer tests and asset-contract checks. Compile Core, Combat Unity, runtime, and Editor test assemblies; leave final Editor/mobile visual validation as a human checkpoint.

## Trigger Predicate

The first result qualifies only when all are true:

```text
standard encounter
AND correct outcome
AND finalDamage > 0
AND enemyDefeated == false
AND enemyFled == false
AND sourceEncounterId == destinationEncounterId
AND destinationRemainingCooldown < sourceRemainingCooldown
```

If the enemy attacks and its cooldown resets in the same authoritative resolution, explicit `cooldownActionConsumed` receipt semantics are required rather than comparing remaining cooldown values.

## Safety

- Queue persistence occurs after truthful combat presentation completes.
- The focus acknowledgement never invokes Attack or writes gameplay state.
- Completed progress cannot reopen; interrupted active presentation restarts without granting rewards.
- No backend rules, schema publication, build settings, CI, dependencies, merge, or release changes are included.
