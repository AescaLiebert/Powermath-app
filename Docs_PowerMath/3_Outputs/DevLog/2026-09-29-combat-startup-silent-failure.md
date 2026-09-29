# Combat startup silent failure

## Findings

The Event question repository invoked the completion callback inside its catalog
validation catch. If combat initialization threw, it called that callback again
with a null catalog. The consumer had already marked loading complete and ignored
the second callback. This swallowed the original exception and disabled the
startup deadline while leaving combat uninitialized. Main Menu transition gate
release is independent of combat initialization.

An EnemyDefinition maximum cooldown reduction from 3 to 2 reproduced a startup
exception when restoring remaining cooldown 3. The production restore constructor
used the new maximum with the old remaining value and EnemyState rejected it.
The B1/B2 boss assets read 2 when initially inspected, but were changed back to 3
during investigation. No claim is made that this is the current account's failure.

## Changes

- Restrict the repository catch to catalog construction, invoking the callback outside it.
- Clamp restored remaining cooldown to the new definition maximum, preserving HP,
  hearts, stage, and phase. Negative/corrupt cooldowns still fail validation.
- Catch and log combat initialization exceptions at the composition root, including
  saved stage/phase/encounter, then use the existing unavailable-state handler.
- Remove the earlier speculative RunComplete auto-resume helper. No evidence
  supported advancing the player's saved stage to recover this startup failure.
- Preserve the authored biome references, including Biome 7's intentional reuse.

## Verification

- Standalone .NET harness using production core code reproduced the original
  `ArgumentOutOfRangeException` in EnemyState before the cooldown fix.
- Four restore cases pass after the fix. Nonzero cooldown cases also commit,
  resolve an incorrect answer, complete presentation, and return to EnemyReady.
- A repository harness with fake platform/network responses and real parsing
  reproduces the old callback swallowing: two callbacks, no propagated exception.
  The changed repository invokes once and propagates the original exception.
- Unity's C# compiler, using copies of existing Editor response files with outputs
  redirected under ignored Logs/CombatStartupRegression, successfully compiled
  Combat.Core, Combat.EditModeTests, and Assembly-CSharp. This is compilation only.
- Unity EditMode execution is unavailable: `unity` is not on PATH. Intended command:
  `unity test . --mode EditMode --filter PowerMath.Gameplay.Combat.Tests.CombatCoreTests.LocalRunEncounterEngine_RestoresAfterEnemyCooldownChange --output TestResults/combat-startup-regression.xml`.
  No Unity test results report was produced; the Editor Test Runner was not used.
- Live Editor/current-account recovery still needs a new Play run. If startup
  still fails, the full startup exception is now observable rather than swallowed.
