---
slug: firestore-per-user-document-migration
status: approved
source: manual
gdd_tags:
  - backend
  - session
  - persistence
owner: implementation-agent
human_checkpoint: not-required
next_agent: human
blocked_by: []
---

# DevLog: Firestore Per-User Document Migration

## Implemented

- Refactored Firestore database document hierarchy: each level is now a collection (`level-1`, `level-2`, etc.), document ID is the user ID/username, and document fields contain `userdata` and `gamedata` directly.
- Updated `GameApiSettings`:
  - Added `levelCollections` with `[FormerlySerializedAs("levelDocumentIds")]` to preserve asset serialization.
  - Added `TryGetPlayerDocument` overloads to build per-player Firestore document URLs directly.
  - Preserved backward-compatible methods (`TryGetLevel`, `TryGetLevelDocument`, `TryGetLevelDocumentById`).
- Updated `FirestoreRestClient`:
  - Query single-user documents on authentication rather than pulling monolithic level documents.
  - Added `TryGetStudent` with automatic detection and synthetic mapping for direct document fields with legacy fallback.
- Refactored patch planners and builders:
  - `PlayerDefaultsPlanner`, `PlayerLifecycleCommands`, and `PlayerResetPayloadBuilder` now target `gamedata` directly instead of `{ username, "gamedata" }`.
- Refactored progression, pet, and economy stores:
  - `FirestoreProgressionCommandStore`, `FirestorePetEquipCommandStore`, `FirestorePetGachaCommandStore`, `FirestoreAcademicProgressionStore`, `FirestorePlayerResetService`, `FirestoreAdminTuningService`, and `ProfileAnalyticsRuntime` now address per-player documents and use `gamedata` root.
- Updated EditMode tests:
  - `PlayerLifecycleTests`: updated expected patch paths to `gamedata`.
  - `AdminResetTests`: updated expected patch paths to `gamedata`.
  - `FirestoreAcademicSaveGuardTests`: updated test fixtures to test direct document fields.

## Verification

- Unity 6000.5.3f1 script compilation: passed with 0 compiler errors.
- Backward compatibility: transparent fallback for legacy nested format tested and supported.
