# DevLog: 2026-08-12 - Response Score Damage Multiplier

## Goal

Make answer speed directly scale combat damage without conflating educational Response Efficiency with combat ATK.

## What I Did

- [x] Added the approved 20%-per-score-point combat multiplier.
- [x] Applied it after ATK, Rank, Buff, and Critical multipliers.
- [x] Exposed the applied multiplier on damage and combat results.
- [x] Added score and percent to correct-answer feedback.
- [x] Added complete score 1-10 damage mapping tests.

## Key Decisions

- Named the combat value `ResponseDamageMultiplier` to keep it distinct from the existing 0%-100% educational `Response Efficiency` metric.
- Kept incorrect and timeout damage at exactly zero outside the correct-answer damage calculator.
- Kept midpoint rounding away from zero after every multiplier is composed.

## Game Feel Notes

The score-to-percent feedback makes speed-based power predictable before the hit resolves. The table is an owner-approved starting curve and should be checked against enemy time-to-defeat during Play Mode balance testing.

## Verification

- Combat core and EditMode test source compile: 0 warnings, 0 errors.
- Unity full-script compilation: succeeded; only the pre-existing LeanTween obsolete-API warning remains.
- Direct regression execution: 29 cases passed, including all ten Response Score mappings and invalid-score rejection.
- Formula smoke check: score 9 maps composed damage 50 to 90; score 10 maps it to 100.

## Next Session

- Verify score-boundary submissions and Firebase attempt persistence in Play Mode.
- Revisit the 200% maximum only if fast answers trivialize Stage pacing.
