# DevLog - Audit, Rank, and question infrastructure

Date: 2026-08-10  
GDD: `@tag:answer-scoring`, `@tag:question-data`, `@tag:run-reset`, `@tag:economy`, `@tag:server-authority`, `@tag:feedback`, `@tag:guardrails`

Implemented the approved ADR-005 local-development slice without connecting live Firestore.

- Added Unity-free Academic Core models for `AcademicRank`, five-result audit windows, one-step Rank transitions, separate Rank Currency balances, question IDs/definitions, per-Rank FIFO inventories, cycle history, and atomic progression mutations.
- Added a transport-neutral question repository plus a Firestore-shaped DTO/mapper using exact fields `{id, video_link, answer, rank}`. Documents fail validation before catalog admission, and each Rank requires at least five distinct questions.
- Added deterministic in-memory Silver, Gold, and Diamond fixtures. Their QA-only prompts expose target answers so the full path remains testable while real video content is unavailable.
- Replaced seeded random combat scoring with committed-question correctness, monotonic response timing, locked Rank multipliers, zero damage for incorrect/timeout results, matching `+1` local Rank Currency for correct results, atomic rollback on content failure, and idempotent command receipts.
- Added Rank/currency/question UI, explicit promotion/demotion acknowledgement, transition audio, local-only labels, and coordinated semantic feedback. Hidden audit score/count remains absent from the player-facing visual tree.
- Kept progression scene-memory only and ignored undocumented `rankProgress`. Production continues to fail closed when no remote attempt authority exists.
- Added 48 passing EditMode tests and 4 passing PlayMode scene tests covering correct, incorrect, timeout, promotion, demotion, modal acknowledgement, FIFO/cycle behavior, validation, multipliers, rollback, idempotency, and hidden audit UI.

The game-design constraints shaped two material choices: audits stay invisible during play to avoid test-pressure framing, and demotion uses neutral “Rank adjusted” language while still requiring an explicit Continue action.

No dependency, package, build-setting, Firestore schema/rule/index, credential, persistence, CI, deployment, PR, merge, or official publishing change was made. Human desktop/mobile visual QA and the production Firestore/authority adapter remain follow-up work.
