# DevLog: 2026-08-12 - Question Fallback with Real Persistence

## Goal

Keep authenticated Firebase gameplay testable when the independent Question Firebase
project is unavailable, without replacing the real player-data persistence path.

## What I Did

- [x] Moved player progression-store creation ahead of shared question loading.
- [x] Added local question and simulated-video fallback for Question Firebase failures.
- [x] Kept Firestore attempt, combat, academic, run-settlement, economy, and analytics saves active.
- [x] Preserved persisted question IDs when constructing fallback catalogs.
- [x] Added a player-visible fallback notice and diagnostic warnings.
- [x] Added event-question fallback keyed to every event document used by the Stage map.

## Key Decisions

- Player Firebase availability and Question Firebase availability are independent runtime concerns.
- Fallback content is explicit and uses a disposable/prototype academic history; it is never labeled as live content.
- ADR-006 now records the owner-approved exception to the earlier fail-closed content policy.

## Bugs Found

- [x] Live initialization previously created the real persistence adapter only after all question documents loaded, so a missing content project disabled unrelated player saves.
- [x] A fixed local catalog could reject an existing player's saved FIFO IDs during rehydration.

## Game Feel Notes

The startup notice makes the lower-fidelity content state clear before the first Attack.
The combat, numpad, timer, feedback, and progression cadence remain unchanged.

## Verification

- Roslyn compiled `Assembly-CSharp` successfully with the Unity-generated response file.
- `git diff --check` passed for the modified code and ADR.
- Live Firebase fallback and restart persistence remain human E2E steps in the regression plan.

## Next Session

- Run the disposable-user fallback E2E after leaving Play Mode so Unity imports the latest script revision.

---

## Git Commit Summary

```text
fix(firebase): keep player saves active during question fallback

- use local simulated questions when the content project is unavailable
- preserve authoritative player Firestore persistence
- document and test the split-backend fallback path
```
