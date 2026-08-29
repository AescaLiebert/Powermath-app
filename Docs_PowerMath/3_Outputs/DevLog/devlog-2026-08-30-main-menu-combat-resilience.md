# DevLog — Main Menu and Combat Resilience

Date: 2026-08-30

GDD references: `@tag:core-loop`, `@tag:combat-attempt`, `@tag:question-data`, `@tag:stage-progression`, `@tag:feedback`, `@tag:player-experience`.

## Bugs found and fixed

- Panel close events were still subscribed to the retired session-return transition. Removed that transition path while preserving the one-time bootstrap entrance.
- Enemy action rendering rebuilt the full maximum cooldown and styled consumed entries as spent, causing a removed token to return transparently. Rendering now contains only future actions.
- Lethal feedback serialized queue cancellation, enemy death, reward motion, clear feedback, and an extra hold. The independent visual paths now run concurrently while accepted combat data and persistence remain authoritative before presentation.
- Invalid development question fallback called only the combat view error state and returned while bootstrap visuals remained prepared/hidden. It now uses the shared unavailable handler, which restores final Main Menu presentation state.
- Question fallback reused real persisted FIFO IDs and Firebase persistence, allowing prototype content to collide with or alter the canonical player artifact. Fallback is now an isolated practice session with a fixed catalog, fresh local queues/run ID, in-memory checkpoints, disabled persistent economy controls, and an explicit unsaved-progress notice.

## Verification

- Runtime `Assembly-CSharp` compiled successfully with Unity's generated response files.
- Combat Unity EditMode test assembly compiled successfully; only two pre-existing unused-local warnings remain in `ActorPresentationControllerTests`.
- Targeted EditMode assertions were updated for future-only enemy actions, retired panel-close session returns, and question-fallback bootstrap recovery.
- Full Unity Test Runner execution remains pending because the project is already open in the interactive Editor.
