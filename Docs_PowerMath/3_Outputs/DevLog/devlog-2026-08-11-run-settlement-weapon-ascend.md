# DevLog: Persistent Run Settlement and Weapon Ascend

Date: 2026-08-11  
Status: implementation complete; awaiting human E2E

## Delivered

- Added a run ID settlement receipt so death and Rebirth cannot reward the same run twice.
- Added identical Stage-based Legacy ATK rewards for death/Rebirth, Rebirth from Stage 50, and Rebirth-only `+1` Prestige.
- Added current-run Rank Currency counters while retaining lifetime wallet values for leaderboard ordering.
- Settlement resets Stage, combat recovery, audit, active question, and all question queues; active Rank, lifetime Rank Currency, highest Stage, analytics, inventory, and loadout remain.
- Added persistent Level 0-100 Weapon Ascend using only Power Coins, with formula-derived ATK/CR/CD and a ScriptableObject tier catalog for name/icon milestones.
- Integrated saved Weapon and Legacy bonuses into authoritative local combat-stat composition.
- Added functional temporary Main Menu controls and settlement summary. Final visual styling remains deferred to the product owner's UI brief.

## Verification boundary

- Combat Core compiled with Unity's generated response file.
- Main assembly compiled with Unity's generated response file after including the newly added UI controller source.
- No automated tests were authored or run. Human E2E is the approved verification path.
- Firebase rules were not published and no build/deployment settings were changed.
