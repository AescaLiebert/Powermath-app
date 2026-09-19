# DevLog — Story Tutorial and Power Rescue GDD

**Date:** 2026-09-14  
**Work type:** Game-design documentation  
**Primary artifact:** `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md`

## Summary

Expanded the canonical GDD with a story-driven tutorial system featuring Power, a persistent Firebase `tutorialMap`, legacy-account eligibility, forced guided interactions synchronized with authoritative Battle State, and the “Power Won't Let You Down!” rescue mechanic.

## GDD Changes

- Added the `@tag:tutorial-system` and `@tag:power-rescue` design section.
- Defined the visual-novel presentation: animated Power sprite, emotion states, transparent black scrim, localized dialogue box, focus layer, audio feedback, and Reduced Motion behavior.
- Defined tutorial queue entry/exit rules, safe-state constraints, refresh/reconnect recovery, and idempotent forced actions.
- Defined server-authoritative `tutorialMap` progress that allows accounts created before tutorial implementation to experience newly released tutorials without resetting gameplay state.
- Specified `OnFirstCreate`, `OnFirstRankChange`, and `OnFirstRebirth` sequences, including the one-time 25-PC grant, forced gacha pull, guaranteed starter Follow-Up SSR contract, pet demonstration, and Player Hub introduction.
- Defined the rescue as a dedicated `PowerRescueCounterAttack` Battle State triggered after three consecutive non-void failures against the same standard enemy.
- Set Starting `PowerRescueDamage` to 5,000 fixed support damage and documented its resolution before a pending enemy attack.
- Prevented rescue damage from granting correctness, Rank Currency, audit credit, pet/player hit triggers, or `TotalDamageDealt` credit.
- Defined serial ordering with enemy attacks and pet Counter-Attacks so counter-attack-family states cannot overlap or recurse.
- Updated combat flow, state persistence, server authority, required feedback, player-experience requirements, guardrails, playtests, and tunable values.

## Safety and Migration Decisions

- Tutorials never cover or pause an active answer timer.
- Forced interactions use the same validated commands as normal play and cannot write game state directly.
- Tutorial rewards and gacha actions are transaction-idempotent.
- Missing tutorial entries keep legacy accounts eligible; completed tutorials do not replay merely because presentation content changes.
- Existing owners of the starter Follow-Up SSR receive no unintended free duplicate; the tutorial uses their owned pet for the demonstration.
- Power Rescue is limited to once per enemy and excluded from Events, Math Minigames, and Challenger League.

## Validation

- Verified all new tutorial and rescue terms are cross-referenced in the GDD.
- Verified the combat state diagram routes failed attempts through `PowerRescueCounterAttack` before enemy attack resolution.
- Ran `git diff --check`; no whitespace errors were reported.
- No source code, Firebase rules, dependencies, build settings, or prior analysis artifacts were modified.

## Next Human Checkpoint

Approve the story copy/emotion list and confirm whether Starting 5,000 Power Rescue damage should remain available against protected bosses after abuse and pacing playtests.
