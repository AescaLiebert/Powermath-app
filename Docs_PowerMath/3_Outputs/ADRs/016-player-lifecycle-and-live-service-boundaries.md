---
slug: player-lifecycle-live-service
status: approved
source: manual
gdd_tags: [server-authority, player-experience, leaderboard-profile]
owner: Codex
human_checkpoint: required
next_agent: human
blocked_by: []
---

# ADR-016: Player lifecycle and live service boundaries

Status: **Accepted direction; implementation in progress**. Date: 2026-09-08.

## Context

The requested Thai/English UI, first-login preparation, public patching and future server-owned mailbox/events span presentation, persistence and trust. ADR-003 accepts a direct-Firestore prototype, ADR-011 supplies client version/migration helpers, and ADR-012 persists combat presentation receipts. Current bootstrap does not derive its response schema from a durable save version.

## Proposed decision

Use the contracts and rollout in `../Specs/player-lifecycle-live-service-arch-plan.md`: account language plus local preference cache; explicit resumable onboarding; shared protagonist presentation catalog; durable schema migration independent of release/content versions; trusted idempotent commands for protected state. Reuse existing receipt and UI composition patterns.

Propose Firebase Authentication and a trusted Firestore transaction service as the production trust boundary. Backend host, legacy identity enrollment, dependency and deployment changes require their own concrete approval.

## Alternatives

| Alternative | Trade-off |
|---|---|
| Keep direct client Firestore permanently | Least operational work; cannot deliver the requested trusted rewards with current public writes |
| Add interfaces now, defer backend | Useful incremental preparation; explicitly remains prototype authority |
| Introduce trusted commands incrementally (proposed) | More operational work; enforceable identity, time, ownership and atomic rewards after all protected writers cut over |

For localization, a lightweight keyed catalog avoids a new dependency for text-focused UI; Unity Localization remains an option if production authoring requirements justify it.

## Consequences

Migration fixtures, concurrency tests, Thai rendering QA and legacy onboarding policy are required. Preserve critical receipts and unknown fields. Never downgrade saves for client rollback. Publishing new content does not initialize existing players again.

User approved this direction. The local implementation extends ADR-011; ADR-003 still describes the running prototype trust boundary until the backend/authentication cutover is deployed. See the implementation handoff for current coverage.

