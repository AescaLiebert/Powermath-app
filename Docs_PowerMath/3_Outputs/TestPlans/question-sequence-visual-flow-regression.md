# Question Sequence Visual Flow Regression

## Automated Coverage

- `DamageCalculator_AppliesResponseScoreMultiplier` verifies the immutable visual breakdown matches the authoritative damage input and result.
- `QuestionPresentationResult_ReportsRetainedSurfaceExplicitly` verifies the provider explicitly declares retained browser content.
- `AnswerFeedback_ReplacesNumpadWithOrderedVisualStack` verifies the retained-video layout, answer/feedback swap, and connected score rows.
- UI entry-asset tests require the answer content, feedback card, score stack, title, and battle banner contracts.
- Targeted EditMode run on 2026-08-27: 45 passed, 0 failed.

## Manual WebGL Sequence

1. Commit one attack with a valid YouTube question.
2. Let the video finish naturally.
3. Verify the ended video docks on the left, remains visible, and no longer intercepts pointer input.
4. Verify the numpad appears to the right, accepts touch/pointer input, waits through preparation, then counts down.
5. Submit a fast correct answer and verify this order: Correct → Base ATK → Rank → Buff → Critical/No Critical → Response Score multiplier → Final Damage.
6. Verify the video and question card close before damage/HP feedback begins.
7. Repeat with an incorrect answer and timeout; verify each has a distinct title/icon and explicit `0 DAMAGE` without multiplier rows.
8. On a lethal correct hit, verify enemy defeat cancels retaliation, Stage advances once, and the next encounter begins at full cooldown.
9. On a surviving enemy with zero cooldown, verify player damage finishes before the enemy counterattack and one heart is consumed.

## Viewport Matrix

| Viewport | Pass condition |
|---|---|
| 16:9 landscape | Retained video and answer panel do not overlap; all numpad targets remain visible. |
| 4:3 landscape | Score tally remains inside the result card with no clipped labels. |
| Narrow supported mobile landscape | Final-damage row and Submit remain visible without scrolling. |

## Accessibility and Stress

- Reduced motion: all tally rows remain present; only reveal intervals and HP interpolation are shortened.
- Muted audio: icons, titles, and text still explain every outcome.
- Rapid Submit/timeout boundary: one result, one close, and one battle presentation.
- Content failure: browser surface closes immediately, committed cooldown is restored, and no incorrect popup appears.

## Known Test-Harness Blocker

The existing `PowerMath.Gameplay.Combat.PlayModeTests` fixture currently supplies `combat-test-player`, while the live-Firebase path requires a `level:student` player identity. With `GameApiSettings.useEditorSampleStudent` disabled, all four tests abort on that pre-existing setup log before their assertions. This UI change does not modify Firebase mode or test-environment policy.

