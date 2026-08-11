---
slug: run-settlement-weapon-ascend
status: approved
source: manual
gdd_tags:
  - combat-stats
  - encounters
  - run-reset
  - economy
  - server-authority
  - feedback
  - guardrails
owner: game-design-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Task Card: Persistent Run Settlement and Weapon Ascend

## Goal

Turn death and optional Stage 50+ Rebirth into safe, understandable persistent settlements: reward Power Coins, grant the same Stage-based permanent ATK increase, preserve the current Rank, reset audit/question runtime, and protect account progression. Replace the weapon shop with one kid-friendly Level 0–100 Weapon Ascend path purchased only with Power Coins.

## Current State

- Combat already reaches `RunDefeat` and `RunComplete`, but neither state has a persistent settlement command or Stage 1 recovery flow.
- Firestore gameplay persistence saves attempts, wallet Rank Currency, Rank/audit state, active run state, and profile analytics.
- The current snapshot does not track currency earned during the current run, last-settled run identity, permanent Legacy ATK, or a canonical Weapon Ascension level.
- `inventory[].upgradeLevel` exists but no Weapon Ascend domain policy or atomic Power Coin spending command exists.
- Main Menu UI styling will be briefed separately; this slice should expose behavior and a functional entry point without treating current visuals as final.

## Requested Scope

- Atomic death and optional Rebirth settlement persisted to Firebase.
- Power Coin reward calculated from currency earned during the current run, never lifetime balances.
- Stage 1 reset of enemy, hearts, temporary buffs/cards, and run-only counters.
- Preservation of active Rank, Rank Currency, Power Coins, pets, inventory/upgrades, historical analytics, Highest Stage, leaderboard snapshots, and profile state.
- Reset of partial audit score/count and per-Rank question queue runtime to canonical fresh cycles after either death or Rebirth.
- Rebirth-only Prestige/Honor increment.
- Rebirth eligibility at Stage 50 or later from a safe state with no unresolved question.
- Identical death/Rebirth Legacy ATK formula: additive `+0.1%` per Stage reached.
- Main Menu Weapon Ascend Level 0–100, Power Coin spending, nonlinear ATK/cost curves, CR/CD milestones, persistent saved upgrades, and clear failure states.
- Ordered Weapon Ascension ScriptableObject catalog containing tier IDs, display names, icons, unlock levels, appearances, and milestone feedback keys.
- Rank Currency becomes accumulation/leaderboard-only and is never spent.

## Out of Scope

- Final visual art, weapon sprites, animation assets, sound assets, or final UI polish.
- Pet Gacha implementation or balance changes beyond confirming its only shared currency is Power Coins.
- Challenger League account-power behavior; it continues to strip Weapon/Legacy ATK bonuses.
- Firestore rules publication, deployment, build settings, dependencies, automated tests, or human E2E execution.

## Human Decision Needed

1. Approve the starting Weapon formulas and transformation names in the GDD/design spec.
2. Approve additive Legacy ATK: both death and Rebirth use `StageReached × 10` basis points (`0.1%` per Stage), with no Rebirth-only ATK grant.
3. Confirm that repeated completed runs may keep adding Legacy ATK, with a cap/diminishing-return rule deferred until playtest data indicates it is needed.

## Checkpoints

- [x] Approve amended GDD and player-facing design. Approved 2026-08-11 (`LGTM`).
- [x] Approve architecture/Firestore transaction schema before implementation. Approved 2026-08-11 (`LGTM`).
- [x] Approve Player Hub and structured Rebirth result preview. Approved 2026-08-12 (`LGTM`).
- [ ] Manually publish any later Firestore rule change.
- [ ] Owner performs E2E; no automated test scripts requested.
- [ ] Review before merge.
