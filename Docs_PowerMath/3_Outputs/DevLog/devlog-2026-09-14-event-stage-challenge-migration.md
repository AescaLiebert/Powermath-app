# DevLog — Event Stage and Challenge Migration

Date: 2026-09-14

GDD references: `@tag:core-loop`, `@tag:question-data`, `@tag:stage-progression`, `@tag:encounters`, `@tag:pet-system`, `@tag:feedback`, `@tag:guardrails`.

## Outcome

Implemented the approved Event Stage migration. `EventDefinition` is now an independent typed definition with `ChallengeMonster` and `Minigame` variants. A deterministic, persisted per-run scheduler places one guaranteed Challenge and at most one pet-modified bonus Challenge in each 20-Stage block while preserving fixed designer bindings inside the cap.

Challenge content now uses a separate Rank-aware catalog and FIFO reservation state with canonical string IDs such as `cs1`, `cg1`, and `cd1`. `GameApiSettings` centrally maps the physical `challenge-silver`, `challenge-gold`, and `challenge-diamond` documents; Event Definitions no longer carry unused document or handler ownership fields. All Challenge Events use the same FIFO cursor for the active Rank. Ordinary Rank questions remain numeric and Challenge results remain outside the five-question audit.

## Runtime and Persistence

- Challenge Monsters resolve in one committed attempt.
- Correct answers defeat the 1-HP target.
- Incorrect answers and timeouts consume `Flee`, remove the target, advance the Stage, and do not remove a heart.
- Correct rewards use `10 + responseScore`; incorrect/timeout rewards grant 10 Power Coins.
- Wallet delta, Stage result, next encounter, Challenge cursor/reservation, attempt identity, schedule, and presentation/reward receipt share the authoritative gameplay save.
- Attempt presentation receipts were versioned to carry flee and Power Coin data for recovery without replaying state mutation.
- Player schema version advanced to 4 with additive schedule, Challenge sequence, content identity, and reward receipt defaults/reset mappings.
- Unique owned SSR pet collection bonuses are aggregated into the encounter chance multiplier and snapshotted with the run schedule.

## Presentation

- Added the `Flee` enemy action token and `EnemyFlee` presentation action.
- Failure feedback no longer routes through enemy attack or player damage.
- Flee and Power Coin reward feedback can play together, followed by the next Stage encounter.
- Fixed a plan-order regression found during focused verification where Challenge failure could still enter the generic enemy-attack branch.

## Verification

- Ten affected runtime/test assemblies compiled successfully with Unity's generated compiler response files.
- Twelve focused scheduling, identity, FIFO, reward, flee, transaction, and presentation cases passed.
- The complete pure Combat and Academic EditMode assemblies passed through the reflection harness: 66 Combat cases and 48 Academic cases, with zero failures. Targeted lifecycle migration and SSR multiplier cases also passed.
- Seven targeted Firestore Event parser cases passed for the deployed `qN`/`video-url` structure, canonical Rank IDs, and rejection of numeric, uppercase, zero-padded, whitespace-padded, wrong-Rank, or ordinal-mismatched IDs.
- `git diff --check` completed without whitespace errors; line-ending conversion warnings reflect the repository's existing Windows checkout policy.
- Full Unity Test Runner and manual Editor/WebGL playtests remain pending because the project is currently open in the interactive Editor and a second batch process cannot acquire its lock.

## Checkpoints

- Architecture approved by the project owner with `lgtm` on 2026-09-14.
- Implementation review and manual playtest remain required before merge.
- No Firebase publication/deployment, build setting, CI, dependency, secret, or merge action was performed.
