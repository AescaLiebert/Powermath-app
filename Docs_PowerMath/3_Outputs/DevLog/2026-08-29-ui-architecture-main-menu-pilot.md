# DevLog: 2026-08-29 — UI Architecture Core and Main Menu Pilot

## Outcome

Implemented accepted ADR-014 as a small scene-scoped UI foundation and migrated the approved Main Menu pilot. UI lifecycle ownership is now deterministic, feature code no longer owns LeanTween mechanics, and the pilot preserves existing gameplay and transaction boundaries.

## What Changed

- Added `PowerMath.UI.Core` with one shared motion profile, LeanTween execution driver, motion channels, lifecycle state machine, interaction binder, subtree query helper, and read-only scene context.
- Added a LeanTween assembly boundary so UI Core can depend on LeanTween without moving feature code into the default assembly.
- Adapted `MainMenuPanelHost` to asynchronous enter/exit, sampled reversal, input-safe picking, focus-after-entry, synchronous force-close, and callback-once behavior.
- Migrated Main Menu bootstrap/session-return and Canvas character tween execution to the shared driver while preserving sequence and authored transition settings.
- Migrated Player Hub idle, pulse, shake, particle, and milestone tween execution to the shared driver while keeping feature/audio policy local.
- Removed transform-transition ownership from migrated Player Hub feedback targets and preserved unrelated color/border transitions.
- Constructed one `UiSceneContext` in the existing Main Menu composition root and supplied the shared driver explicitly to the pilot feature.

## Architecture Notes

- Kept the existing feature-owned Main Menu panel-host interface; a new generic global UI manager or dependency-injection framework would be unnecessary indirection at this stage.
- Kept the driver primitive and channel-based. High-level `Enter`, `Pulse`, and `Idle` methods were not duplicated in the driver because lifecycle and feature classes already own those meanings.
- Priority is `Lifecycle > Feedback > Interaction > Ambient`; higher motion cancels lower conflicts, while lower requests cannot overwrite active higher motion.
- Broader subtree extraction and remaining screen migrations are intentionally deferred until the human pilot checkpoint.

## Verification

- Unity 6000.5.3f1 batch compilation passed with zero compiler errors.
- UI Core EditMode: 7/7 passed.
- Combat Unity EditMode: 28/28 passed.
- Direct LeanTween ownership scan passed: runtime UI usage exists only in `LeanTweenUiDriver`.
- Targeted diff whitespace check found no new pilot-file errors; repository-wide `git diff --check` still reports pre-existing trailing whitespace in unrelated scene/TMP assets.

## Human Follow-up

- Review Main Menu panel enter/exit, bootstrap/session return, and Player Hub feedback in normal and reduced motion.
- Exercise pointer and keyboard focus, rapid reopen during exit, supported aspect ratios, and twenty open/close cycles.
- Approve the pilot before Phase C expands subtree/view cleanup to the remaining feature panels.
