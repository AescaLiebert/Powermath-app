# Stage Combat Attempt Loop Test Plan

Date: 2026-08-10  
GDD: `@tag:core-loop`, `@tag:combat-attempt`, `@tag:answer-scoring`, `@tag:combat-stats`, `@tag:stage-progression`, `@tag:feedback`, `@tag:guardrails`

## Automated Evidence

| Layer | Coverage | Result |
| --- | --- | --- |
| EditMode Core | Stage/world mapping, answer normalization, damage rounding, content rollback, lethal ordering, timeout, deadline one-shot, gateway idempotency | 15 passed, 0 failed |
| EditMode UI asset | Required UXML contract, ten numpad buttons, snapshot-to-HUD rendering | Included in the 15 passed tests |
| PlayMode scene | Hydrated Main Menu, simulation badge, enemy art, Attack, numpad input, Submit, feedback completion, visible HP loss | 1 passed, 0 failed |

Evidence files:

- `Logs/combat-all-editmode-results.xml`
- `Logs/combat-playmode-results.xml`

## Manual Release Checklist

- [ ] Verify desktop Game view readability at the project target resolution.
- [ ] Verify narrow mobile-compatible aspect ratio, pointer/touch hit targets, and no clipped numpad controls.
- [ ] Verify normal, critical, lethal, timeout, counterattack, and three-heart defeat feedback by selecting deterministic seeds.
- [ ] Verify reduced-motion mode removes large motion while retaining text, HP, and audio meaning.
- [ ] Verify keyboard digits, keypad digits, Backspace, Delete/Clear, Enter, and keypad Enter.
- [ ] Verify logout and scene re-entry restore the bootstrapped Stage and do not persist simulated combat.
- [ ] Verify a non-development WebGL build shows unavailable state and cannot silently enable random simulation.
- [ ] Re-run authentication/bootstrap/logout regression checks from their existing plans.

## Deferred Production Contract Tests

- Firestore/video content success, corrupt URL, playback error, and cancellation.
- Authoritative correct/incorrect result handling; incorrect must return zero damage.
- Remote idempotency receipts and retry behavior.
- Persisted Stage/run reconciliation after reconnect.

These cases are deferred because the production gameplay gateway and question-video service are outside the approved slice.
