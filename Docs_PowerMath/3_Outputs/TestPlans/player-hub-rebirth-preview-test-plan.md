# Player Hub and Rebirth Preview Test Plan

## Scope

GDD references: `@tag:combat-stats`, `@tag:run-reset`, `@tag:economy`, `@tag:feedback`, and `@tag:server-authority`.

## Automated/static verification

- Compile `PlayerStatProjection`, combat adapter, and both UI controllers with Unity project assemblies.
- Parse `MainMenuUI.uxml` as XML.
- Verify every controller-required UI Toolkit name exists exactly once.

## Manual functional cases

1. Open Player Hub at Weapon Level 0 and verify Effective ATK matches combat damage before Rank/critical multipliers.
2. Verify the white line equals Base ATK + Weapon ATK and the green line reports the saved Legacy percentage plus its rounded ATK contribution.
3. Verify Pet reads `NO STAT CONFIGURED`; an equipped Pet ID must not silently change ATK.
4. With insufficient Power Coins, verify Upgrade is disabled and the exact shortfall is shown.
5. With sufficient Power Coins, upgrade once and verify level, current/next stats, cost, balance, and Effective ATK refresh from the saved Firebase result.
6. Re-enter Play Mode and verify the accepted weapon level and Power Coin balance reload from Firebase.
7. At Stage 50+, open Rebirth and verify current/result Power Coins, Legacy ATK, Effective ATK, and Prestige before confirming.
8. Confirm Rebirth and verify the accepted screen retains those values, Firebase contains them, and Stage returns to 1.
9. Simulate a settlement save failure and verify the death screen cannot close and offers Retry without granting twice.
10. At Weapon Level 100, verify the panel reports maximum level and performs no write.

## Experience checks

- New player: without explanation, ask what each number will become after Rebirth; pass when the player predicts all four result values.
- Stress: repeatedly press Upgrade/Rebirth; pass when only one Firebase command is accepted and controls immediately show busy state.
- Abuse: reopen/retry after a network failure; pass when currency, Legacy, Prestige, and weapon level never duplicate.
- Readability: an observer should identify white subtotal, green permanent boost, final ATK, and exact upgrade cost without opening developer tools.

## Known limitation

Pet combat stats are not implemented. This plan validates the explicit zero/unconfigured presentation only.
