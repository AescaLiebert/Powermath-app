# Audit, Rank, and Question Infrastructure Test Plan

Date: 2026-08-10  
GDD: `@tag:answer-scoring`, `@tag:question-data`, `@tag:economy`, `@tag:server-authority`, `@tag:feedback`, `@tag:guardrails`

## Automated Evidence

| Layer | Coverage | Result |
| --- | --- | --- |
| EditMode Core | Rank parsing/multipliers, audit clamping and thresholds, floor/ceiling transitions, atomic progression, per-Rank FIFO/cycles, failure requeue, void rollback, catalog validation, real-answer combat resolution, deadlines, and command idempotency | 48 passed, 0 failed |
| EditMode UI asset | Required Rank/currency/question/modal UXML contract, snapshot rendering, and absence of player-facing audit score/count elements | Included in the 48 passed tests |
| PlayMode scene | Correct answer damage/currency, five-correct promotion, five-incorrect demotion, semantic modal acknowledgement, timeout zero-damage path, local-mode labels, and hidden audit UI | 4 passed, 0 failed |

Evidence files:

- `Logs/academic-combat-editmode-results.xml`
- `Logs/audit-rank-playmode-results.xml`

## Manual Release Checklist

- [ ] Inspect Rank HUD, question prompt, currency feedback, and modal layout at desktop and narrow mobile-compatible aspect ratios.
- [ ] Confirm the modal reads clearly over the combat scene and the Continue control has an adequate pointer/touch target.
- [ ] Confirm promotion audio feels rewarding and demotion audio/copy feels neutral rather than punitive.
- [ ] Confirm reduced-motion mode retains semantic text, audio, and HP/currency meaning.
- [ ] Confirm keyboard/numpad input cannot bypass the Rank modal acknowledgement gate.
- [ ] Confirm leaving and re-entering the scene resets the explicitly local audit, queues, and currency projection.
- [ ] Confirm a non-development build fails closed when no remote attempt authority is configured.

## Deferred Firestore Contract Tests

- Document reads using exact wire fields `{id, video_link, answer, rank}`.
- Network unavailable, permission denied, malformed document, duplicate ID, insufficient per-Rank pool, and stale signed-video URL cases.
- Atomic persisted audit/Rank/currency/question inventory transactions, reconnect reconciliation, and remote idempotency receipts.
- Player bootstrap mapping for explicit audit score/count and inventory state; legacy `rankProgress` remains unused until documented.

These cases are deferred because this approved slice defines the repository/DTO/mapper boundary but deliberately adds no live Firestore adapter, rules, indexes, credentials, or deployment.
