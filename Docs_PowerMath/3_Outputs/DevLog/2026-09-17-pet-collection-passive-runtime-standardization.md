# DevLog — Pet Collection and Passive Runtime Standardization

Date: 2026-09-17  
GDD: `@tag:combat-stats`, `@tag:economy`, `@tag:pet-system`, `@tag:gacha`, `@tag:server-authority`, `@tag:feedback`  
ADR: ADR-021 (accepted by project owner with `LGTM`)

## Implemented

- Standardized ownership around stable pet ID plus positive integer `count`; legacy duplicate rows aggregate into one inventory entry.
- Made inventory show one pet tile with an `xN` badge and removed display-name identity fallback.
- Projected all owned copies into collection-wide stats; cosmetic equip is validated but no longer gates power.
- Added generic passive effect, stack rule, reset scope, and encounter filter definitions with shipping SSR assets using `UniquePerDefinition`.
- Replaced Sapphire/Auregriff/GoldenCrane/Lunamoth/Lumirin booleans and pet-name enum branches with semantic passive queries.
- Applied the supplied CSV values to the 14-pet catalog, including Furbo +7 Player ATK per copy and the five SSR passive parameters.
- Added approved 1x/10x products at 180/1,800 PC, an SR-or-better guarantee per 10x, and SSR hard pity on the 90th individual pull.
- Added an ordered multi-result receipt with per-result copy deltas and pity before/after values; natural or guaranteed SSR results reset pity immediately.
- Hardened direct-Firestore prototype reads/writes against invalid counts, case duplicates, unchecked overflow, and count/upgrade-level ambiguity.
- Preserved combat passive progress (`stageAttackCount`, Big Boss defeats, pending follow-up damage) in the existing active-run snapshot.
- Added generic heart-change reason/event coverage and deterministic passive combat fixtures.

## Verification

- Unity script/asset import and domain reload completed successfully after changes.
- Latest `Assembly-CSharp` compilation succeeded using Unity's generated compiler response.
- 16 targeted pet-domain smoke tests passed, including 10x guarantees, strict receipt recovery, and SSR pity reset behavior.
- 5 targeted combat/passive smoke tests passed.
- The scoped pet/combat implementation diff passed whitespace validation; unrelated pre-existing art metadata still fails a repository-wide check.

## 2026-09-18 Runtime Collection Injection

- Routed the Pet catalog into both Rebirth preview and authoritative settlement so stacked Power Coin percentages and generic Rebirth grants use one calculation path.
- Split player and Follow-Up hits into ordered, persisted combat actions and bound the existing `petPresentation` image to the equipped follower actor.
- Added active-lobby collection hot reload: an authoritative pet acquisition now recomputes player/pet ATK, critical stats, maximum hearts, and unlocked passives without recreating the current encounter.
- If collection data changes during a committed/resolving attempt, the engine queues it until presentation completion so a hit cannot change formula halfway through resolution.
- Future encounter-schedule slots are regenerated with the new collection multiplier while reached stages remain unchanged, preventing refresh/reroll abuse.
- Verification: application and combat assemblies compile; 8 focused pet-combat smoke tests pass, including immediate Follow-Up and heart-stat activation plus encounter-schedule preservation.

## Remaining Human-Gated Work

- Production exploit resistance still requires the separately approved trusted authenticated Firebase command backend and rule deployment; direct anonymous Firestore remains prototype-only.
- Counter-Attack and final heart loss/gain motion detail still await the owner's remaining visual brief.
- Full Unity Test Runner, Firebase emulator, mobile WebGL, and visual QA remain required before merge/release.

## Notes

- No Firebase deployment, security-rule publication, package/dependency change, build/CI setting change, merge, or team-status publication was performed.
- The repository contained extensive pre-existing modified and untracked work; this session preserved it and did not reset unrelated files.
