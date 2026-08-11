# Response Score Damage Multiplier Test Plan

## Rule

GDD references: `@tag:combat-attempt` and `@tag:combat-stats`.

```text
ResponseDamageMultiplier = ResponseScore × 0.20
FinalDamage = round(ComposedCombatDamage × ResponseDamageMultiplier)
```

Incorrect and timeout outcomes remain zero damage.

## Automated cases

- Verify scores 1 through 10 map to 20% through 200% in 20-point steps.
- With composed damage 50, verify resulting damage is 10, 20, 30, 40, 50, 60, 70, 80, 90, and 100.
- Verify score 5 preserves the previous midpoint-rounding test at 100%.
- Verify a preparation-period score 10 applies 200% after the Rank multiplier.
- Verify incorrect and timeout outcomes still produce zero damage and can trigger the enemy counterattack.
- Verify response score outside 1-10 is rejected for a correct result.

## Manual readability cases

1. Submit a correct answer at score 9 and verify feedback shows `SCORE 9 - 180% DAMAGE` before the `90` damage number when composed damage is 50.
2. Submit during preparation and verify score 10 shows `200% DAMAGE`.
3. Submit at score 5 and verify the hit equals the composed damage before response scaling.
4. Submit an incorrect answer and allow timeout; verify neither path shows a response multiplier or deals damage.

## Playtest focus

- New player: ask why two correct answers caused different damage; pass when they identify response speed from the score/multiplier feedback.
- Stress: submit at countdown boundaries; pass when the accepted saved Response Score and displayed damage multiplier always agree.
- Abuse: retry or reconnect after resolution; pass when the multiplier is not applied a second time.
- Readability: an observer can connect `Score`, `% Damage`, and the following damage number without inspecting developer data.
