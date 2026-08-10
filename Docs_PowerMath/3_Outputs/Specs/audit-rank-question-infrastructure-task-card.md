---
slug: audit-rank-question-infrastructure
status: approved
source: manual
gdd_tags:
  - answer-scoring
  - question-data
  - run-reset
  - economy
  - server-authority
  - feedback
  - player-experience
  - guardrails
  - playtest
owner: orchestrator-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Task Card: Audit, Rank, and Question Infrastructure

## Player-Facing Goal

The student completes non-overlapping five-question audits without seeing the hidden calculation. An actual promotion or demotion produces a clear, non-punitive Rank transition; correct answers use the active Rank's combat multiplier and award its matching Rank Currency. Questions remain independent from visual Stage and are selected from the active Rank's FIFO inventory.

## Source

- Origin: Manual Codex request on 2026-08-10.
- Requested outcome: Implement the Audit system and Rank system described by the GDD.
- Question-data constraint: prepare infrastructure for a later Firestore API using `{id, video_link, answer, rank}` across Silver, Gold, and Diamond; do not connect live Firestore gameplay content yet.

## Router Decision

- Workflow: `/implement-feature`
- Current artifact: `Docs_PowerMath/3_Outputs/Specs/audit-rank-question-infrastructure-task-card.md`
- Next artifact: `Docs_PowerMath/3_Outputs/Specs/audit-rank-question-infrastructure-design-spec.md`
- Human checkpoint: required before architecture work.

## GDD Reference

- `@tag:answer-scoring`: five resolved questions, maximum 50 points, thresholds `>= 40`, `26-39`, and `<= 25`; audit details stay hidden.
- `@tag:question-data`: independent per-Rank FIFO queues, no question repeated within one audit, failed-question requeue, cycle restart, and content-failure rollback.
- `@tag:run-reset`: active Rank, partial audit, queue state, cycle history, and Rank Currency persist across death/rebirth.
- `@tag:economy`: correct answers award the active Rank Currency; balances are permanent and independent.
- `@tag:server-authority`: server owns attempts, answers, audit, Rank, damage, currency, persistence, timestamps, and idempotency.
- `@tag:feedback`: actual promotion/demotion requires a Rank popup and transition audio.
- `@tag:guardrails`: Stage, World Level, Rank, and `question.id` remain separate; one attempt per question per audit.

## Current Implementation

- `PlayerSnapshot.ProgressionData` already exposes `activeRank` and an undocumented `rankProgress` integer; it does not expose explicit partial-audit score/count or per-Rank question inventory state.
- `PlayerSnapshot.WalletData` exposes separate `silver`, `gold`, and `diamond` balances.
- `FirestoreRestClient` maps those bootstrap fields for authentication/player loading only under ADR-003.
- ADR-004 deliberately excludes audit, Rank changes, Rank Currency, authoritative question content, and gameplay persistence.
- The current combat simulation generates a seeded response score for any non-empty answer, fixes Rank multiplier to 1, and does not validate a real question answer.
- `IQuestionPresentation` exists, but the only implementation is a synchronous development simulation prompt.
- Combat command receipts prevent duplicate local mutations inside one scene-scoped gateway.
- No `AcademicRank` value object, five-question audit aggregate, Rank transition projection, FIFO question inventory, question validator/repository, Rank HUD, transition popup, or audit tests currently exist.
- No new Unity dependency is required; UI Toolkit, Video, audio, and Test Framework are already installed.

## Target Scope

### Functional Slice

- Pure `AcademicRank` model with Silver, Gold, and Diamond ordering and GDD multipliers.
- Pure five-result `AuditWindow` aggregate and one-step promotion/demotion rules.
- Explicit partial-audit score and resolved-count state; do not reinterpret `rankProgress` without a documented mapping.
- Pure question definition and validation for `id`, video link, answer, and Rank.
- Per-Rank FIFO inventory with audit-window uniqueness, correct removal, failed requeue in failure order, and cycle counters.
- Repository/catalog contracts that a future Firestore adapter can implement.
- In-memory development question catalog and progression authority so the full flow is testable without network or persistence.
- Correct/incorrect resolution using a locked question answer and real response timing in development mode.
- Rank multiplier and development-only Rank Currency delta integrated into combat results.
- Rank HUD plus promotion/demotion popup and audio; hidden audit score/count remains absent from player UI.
- Fail-closed behavior in non-development builds when no authoritative gameplay gateway exists.

### Firestore Infrastructure Only

- Define transport-neutral contracts and a Firestore-shaped DTO/mapper boundary.
- Validate documents before they enter a live question pool.
- Do not call Firestore, add collections/rules/indexes, modify secrets, or persist gameplay state in this slice.
- Keep the wire-field decision explicit: the request says `video_link`; the current GDD says `source_video_link`.

## Out of Scope

- Live Firestore question reads/writes, listeners, security rules, indexes, credentials, deployment, or schema migration.
- Authoritative cloud transactions, reconnect reconciliation, cross-device persistence, educator analytics, or mastery dashboards.
- Production video download/caching/streaming and signed-URL refresh.
- Weapon unlock thresholds, Power Coin conversion, death reward, Rebirth, gacha, and leaderboard work.
- Editing question content or inventing real educational questions/videos.
- CI/build-setting/package changes.

## Design Ambiguities Requiring Approval

1. **Wire field:** use the request's `video_link` at the future Firestore boundary while keeping a neutral domain `VideoUri`; this intentionally differs from GDD `source_video_link` until the GDD is reconciled.
2. **Currency amount:** the GDD states that a correct answer awards Rank Currency but does not explicitly state the unit delta. Recommended development starting value: `+1` matching active Rank per correct answer; future authority returns the actual delta.
3. **Existing `rankProgress`:** its persistence meaning is undocumented. Recommended behavior: read only `activeRank`; start an empty local audit in development and require explicit `auditScore` plus `auditResolvedCount` in a later backend contract.
4. **Development question visibility:** without real videos, local fixtures must show an unmistakable QA prompt containing the target answer so correctness, incorrectness, FIFO, and Rank transitions can be exercised.

## Acceptance Criteria

- [x] Exactly five resolved, non-void questions form one audit; score is clamped to 0-50.
- [x] The fifth result evaluates exactly one Rank step and opens a fresh audit window.
- [x] Silver cannot demote and Diamond cannot promote; no popup appears when the effective Rank does not change.
- [x] Audit score/count are not shown in normal player UI.
- [x] An actual promotion or demotion blocks the next Attack until a semantic popup is acknowledged and transition audio plays.
- [x] Active Rank multiplier is Silver `1.0`, Gold `1.5`, Diamond `2.0` and is applied only through returned combat-authority results.
- [x] Correct development answers award the matching local Rank Currency projection; incorrect/timeout/void results award zero.
- [x] Stage, World Level, Rank, and question ID use separate types/state.
- [x] Question documents reject empty/duplicate IDs, invalid Rank, non-HTTPS/non-video links, negative/unsupported answers, and catalogs with fewer than five questions per Rank.
- [x] Each Rank has an independent FIFO/cycle position; Rank changes preserve inactive queues.
- [x] No question repeats within one five-question audit.
- [x] Correct questions leave the current cycle; failed questions return to the front after audit in failure order.
- [x] Pool exhaustion increments a cycle and retains historical state without violating audit uniqueness.
- [x] Content failure voids the committed attempt and does not alter audit, Rank, currency, or question outcome history.
- [x] Command retries are idempotent and cannot double-score, double-promote, double-award currency, or resolve a question twice.
- [x] Local development mode is visibly labelled and scene-memory only; production remains unavailable without remote authority.
- [x] EditMode tests cover all thresholds, boundaries, FIFO rules, validation, multipliers, and idempotency.
- [x] PlayMode tests cover correct, incorrect, timeout, promotion, demotion, Rank popup acknowledgement, and hidden audit UI.

## Human Checkpoints

- [x] Approve player-facing design, non-punitive transition language, development fixtures, and the four ambiguity decisions above. Approved by the project owner on 2026-08-10 (`LGTM!`).
- [x] Approve architecture and ADR-005 before implementation. Approved by the project owner on 2026-08-10 (`LGTM`).
- [ ] Approve any later Firestore schema/rules/indexes, secrets, deployment, dependency, or build-setting change separately.
- [ ] Review the PR before merge.
- [ ] Approve official team-status publishing.
