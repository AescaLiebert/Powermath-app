# Test Plan: Event Stage and Challenge Migration

Date: 2026-09-14

## 1. Test Summary

| Area | Priority | Type | Current result |
|---|---:|---|---|
| 20-Stage scheduling and fixed cap | P0 | EditMode | Focused cases pass |
| Challenge identity and per-Rank FIFO | P0 | EditMode | Focused cases pass |
| One-trial defeat/flee resolution | P0 | EditMode + manual | Focused cases pass; manual pending |
| Atomic Power Coin reward and replay | P0 | EditMode + Firestore manual | Domain case passes; live retry pending |
| Presentation action order | P1 | EditMode + manual | Focused case passes; visual QA pending |
| Pet encounter multiplier | P1 | EditMode | Assembly compiles; full suite pending |
| Existing combat/progression regression | P0 | Full suite + manual | Pending |

## 2. Automated Verification Completed

The following Unity-generated response-file assemblies compiled successfully on 2026-09-14:

- `PowerMath.Gameplay.Academic.Core`
- `PowerMath.Gameplay.Combat.Core`
- `PowerMath.Gameplay.Combat.Presentation.Core`
- `PowerMath.Gameplay.Pets.Core`
- `PowerMath.Gameplay.Pets.Unity`
- `PowerMath.Gameplay.Combat.Unity`
- `PowerMath.Gameplay.Academic.EditModeTests`
- `PowerMath.Gameplay.Combat.EditModeTests`
- `Assembly-CSharp`
- `Assembly-CSharp-Editor`

Twelve focused runtime assertion cases passed against the compiled EditMode assembly:

- schedule at 0% base chance: exactly one Challenge per block;
- schedule at 100% base chance: exactly two Challenges per block;
- generated Challenges exclude protected boss Stages;
- fixed Challenges count inside the one-to-two block cap;
- canonical `csN`/`cgN`/`cdN` validation;
- independent Silver/Gold/Diamond Challenge FIFO state;
- shared per-Rank FIFO cursor across different Event catalog keys;
- correct reward at response scores 10, 5, and 0;
- incorrect and timeout reward floor;
- failure flees, advances, and preserves hearts;
- Challenge transaction grants Power Coins without changing the Rank audit;
- presentation consumes `Flee`, emits no enemy attack, and introduces the next encounter afterward.

The same reflection harness then executed every public pure test case in the two directly affected EditMode assemblies:

- Combat EditMode: 66 passed, 0 failed.
- Academic EditMode: 48 passed, 0 failed.

Two targeted `PlayerLifecycleTests` cases also passed for schema migration and unique-owned-SSR pet multiplier aggregation. Seven Firestore Event parser cases passed for the three Rank documents using top-level `q1: { id, video-url, answer }`: canonical `cs1`/`cg1`/`cd1` documents plus rejection of numeric, uppercase, zero-padded, whitespace-padded, wrong-Rank, and `qN`-mismatched IDs. These results supplement, but do not replace, execution through Unity Test Runner.

The full Unity Test Runner was not started because this project is already open in the interactive Unity Editor and a second process cannot acquire the project lock.

## 3. P0 Manual Scenarios

- [ ] Start a fresh run and record every scheduled Challenge across all 20-Stage blocks; confirm one or two per block and none on Mini-Boss, Big-Boss, or Final-Boss Stages.
- [ ] Put a fixed Challenge in a block and confirm it keeps its authored Stage and counts toward the same cap.
- [ ] Change owned SSR pet bonuses before a new run and confirm the saved multiplier affects only that new run.
- [ ] Change pet ownership after a schedule is saved and confirm the active run does not reroll.
- [ ] Refresh before Challenge commit, during video preparation, during the answer window, after submit, during Flee, and during reward feedback.
- [ ] Confirm a restored unfinished Challenge keeps the same committed attempt ID and reserved Challenge content ID.
- [ ] Submit correct answers at response scores 10, 5, and 0; confirm wallet deltas 20, 15, and 10.
- [ ] Submit an incorrect answer and allow a timeout; confirm `+10 PC`, no heart loss, no enemy attack, and Stage advance.
- [ ] Retry an already accepted Firestore attempt after a simulated lost response; confirm wallet, Stage, question cursor, and analytics do not change twice.
- [ ] Confirm Challenge attempts do not increment or reorder the five-question Rank audit.

## 4. Presentation and Platform Scenarios

- [ ] Verify the enemy visibly disappears on Flee and the next encounter appears only afterward.
- [ ] Verify Power Coin feedback can run with Flee without delaying or duplicating the authoritative result.
- [ ] Confirm Challenge copy explains one attempt, flee-on-failure, and the 10-20 PC reward before commit.
- [ ] Verify correct, incorrect, and timeout feedback in Editor and mobile WebGL at supported aspect ratios.
- [ ] Verify Reduced Motion preserves the same semantic result and readable reward feedback.
- [ ] Verify missing video/content cleanly voids the commit and returns the same Challenge reservation to ready state.

## 5. Regression Checklist

- [ ] Run all EditMode tests in the open Editor after script refresh.
- [ ] Run relevant PlayMode tests.
- [ ] Verify normal, Mini-Boss, Big-Boss, and Final-Boss correct/incorrect/timeout flows.
- [ ] Verify ordinary numeric Rank question FIFO and promotion/demotion audit behavior.
- [ ] Verify Death/Rebirth settlement and presentation receipt recovery.
- [ ] Verify default/reset/migration paths from schema versions 1, 2, and 3 to schema version 4.
- [ ] Verify `GameApiSettings` resolves all three production Challenge document URLs and Firestore content rejects uppercase, zero-padded, wrong-Rank, duplicate, `qN`-mismatched, malformed URL, and negative-answer content.

## 6. Release Gates

- Implementation review is required before merge.
- Firebase rule publication or deployment requires separate owner approval.
- Build/CI or dependency changes are not part of this task.
