# ADR-015: Isolated Question Fallback Practice Session

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-30 |
| Author | Codex |
| GDD Section | `@tag:question-data`, `@tag:server-authority`, `@tag:guardrails`, `@tag:player-experience` |
| Supersedes | ADR-006 local-content/real-save fallback only |

Accepted by the project owner on 2026-08-30: fallback questions must retain their own artifact and remain separate from real progression.

## Context

ADR-006 allowed bundled development questions to update the authenticated player's real combat and academic state. When the live catalog became malformed or changed IDs, fallback rehydration collided with persisted FIFO IDs and could also contaminate the canonical player artifact with prototype content.

## Decision

- Treat a shared-question catalog load failure as entry into an isolated practice session.
- Build practice content only from the bundled fixed catalog; never derive it from persisted player question IDs.
- Start fresh in-memory FIFO, audit, and reservation state using the authenticated Rank and balances only as read-only starting projections.
- Clone combat from the saved Stage into a new practice run ID. Do not restore, acknowledge, replace, or clear a real pending presentation artifact.
- Route every practice combat checkpoint to immediate in-memory persistence. Do not write combat, academic, Rank currency, analytics, active-run, settlement, or leaderboard changes to Firebase.
- Disable persistent run-economy controls during practice so settlement or reset cannot write fallback catalog IDs.
- Display an explicit `PROGRESS IS NOT SAVED` notice. Reload after the live catalog is repaired to exit practice and resume the untouched Firebase artifact.

## Consequences

- A malformed or unavailable question catalog no longer blocks Main Menu rendering or corrupts real academic queues.
- Practice progress is intentionally temporary and resets on reload.
- Pet Gacha, Player Hub progression commands, Death settlement, and Rebirth are unavailable in the isolated session.
- A real pending combat presentation remains untouched until the canonical catalog is available again.

## Related

- ADR-005: Atomic Academic Progression and Question Catalog Boundary.
- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary.
- ADR-012: Persisted Combat Presentation Receipts and Orchestration.
