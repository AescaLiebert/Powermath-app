# DevLog: 2026-08-12 - Player Hub and Rebirth Preview

## Goal

Make permanent player power and Rebirth outcomes predictable before the player spends currency or resets a run.

## What I Did

- [x] Replaced the Weapon shortcut with Player Hub.
- [x] Added a structured stat panel and Weapon Ascension section.
- [x] Added current-to-result Rebirth values for Power Coins, Legacy ATK, Effective ATK, and Prestige.
- [x] Unified combat and UI calculations through `PlayerStatProjectionFactory`.
- [x] Kept atomic Firebase commands and death-settlement Retry behavior intact.

## Key Decisions

- Extended ADR-008 instead of creating a competing progression presentation decision.
- Display Pet stats as unconfigured because the current data model has a Pet ID but no Pet stat definition.
- Keep white permanent subtotal and green Legacy contribution in separate UI elements for readable hierarchy.

## Bugs Found

- [x] Death save failure previously allowed a generic modal close path; the settlement screen now remains terminal until Retry succeeds.
- [x] Combat and progression UI previously had separate formula paths; both now use one stat projection.

## Game Feel Notes

The implemented hierarchy prioritizes outcome clarity. Final animation and audio assets remain a later polish pass.

## Verification

- Isolated Unity-assembly compile: 0 warnings, 0 errors.
- UXML XML parse: passed.
- Required binding and duplicate-name checks: passed.

## Next Session

- Owner performs Firebase Play Mode cases from the Player Hub/Rebirth test plan.
- Implement Pet definitions before enabling a Pet stat contribution.

---

## Git Commit Summary

```text
feat(progression): add player hub and rebirth stat previews

- share one player stat projection with combat and UI
- embed weapon ascension in player hub
- show rebirth rewards and effective ATK before reset
```
