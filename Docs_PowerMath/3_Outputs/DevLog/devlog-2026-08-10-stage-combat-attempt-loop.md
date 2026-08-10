# DevLog - Stage combat attempt loop

Date: 2026-08-10  
GDD: `@tag:core-loop`, `@tag:combat-attempt`, `@tag:answer-scoring`, `@tag:combat-stats`, `@tag:question-data`, `@tag:stage-progression`, `@tag:feedback`, `@tag:guardrails`

Implemented the approved ADR-004 development slice for the Main Menu combat loop.

- Added a Unity-free Core assembly for Stage, enemy HP/cooldown, answer buffer, timer coordination, damage calculation, stage progression, and deterministic local resolution.
- Added `ICombatGateway` command receipts so duplicate command IDs return the first immutable result without applying state twice.
- Added an injected `IQuestionPresentation` seam; the current synchronous simulation prompt can later be replaced by an asynchronous VideoPlayer/Firestore-backed adapter without changing combat rules or UI flow.
- Added a development-only seeded fallback that maps response score 1-10 to effective ATK with Rank multiplier 1 and never mutates `PlayerSessionStore`, audit, Rank, wallet, analytics, or persisted progress.
- Added UI Toolkit Stage/enemy HUD, HP/cooldown/hearts, Attack flow, 1-second preparation, 10-second timer, numpad, semantic result states, critical/damage text, enemy reaction, HP interpolation, counterattack/defeat messaging, and generated audio cues.
- Added a Main Menu composition bridge that reuses the existing monster texture and fails closed outside Editor/development builds.
- Added 15 passing EditMode tests and one passing PlayMode scene smoke from hydrated session through keypad submission and visible enemy HP loss.

The approved game-design skill constraints influenced the result sequence: reason first, then damage/enemy reaction, then HP interpolation, followed by defeat or counterattack. Reduced-motion keeps semantic feedback while shortening movement.

No dependency, build-setting, Firestore schema/write, CI, deployment, audit, Rank, PR, merge, or official publishing change was made. Production video/correctness authority and human WebGL/mobile visual QA remain follow-up work.
