# ADR-020: Generic Tutorial Map, Pure Reducer, and Semantic UI Adapters

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-09-15 |
| Author | Codex |
| GDD Section | `@tag:tutorial-system`, `@tag:combat-attempt`, `@tag:server-authority`, `@tag:feedback`, `@tag:guardrails` |
| Extends | ADR-006, ADR-011, ADR-012, ADR-014, ADR-016 |

## Context

The existing lifecycle model had one legacy tutorial checkpoint and deliberately disabled tutorial advancement because no authored catalog existed. `OnFirstCreate` needs durable recovery, truthful first-attempt branching, localization, exact interaction ownership, and synchronization with the real combat transaction without embedding one-off step branches in presenters.

## Decision

- Use a Unity-free, immutable `TutorialStateMachine` that reduces a versioned sequence, durable progress, and semantic signal into the next progress state.
- Author steps, transitions, outcome predicates, localization keys, Power sprite/emotion/audio cues, primary target IDs, and fallback target IDs in ScriptableObject assets. Enable only `OnFirstCreate` in this delivery.
- Store progress as a dynamic V5 `tutorialMap.{tutorialId}` map. Keep the legacy tutorial record readable but no longer authoritative.
- Persist status, stable step ID, trigger/completion time, reward flag, operation/attempt ID, legacy flag, guided encounter ID, and first-attempt outcome. Never persist localized copy, scene references, or focus geometry.
- Reuse revision-checked lifecycle commands for the direct-Firestore prototype, with idempotency by operation ID and a guard that prevents reopening a completed tutorial.
- Keep the live `PlayerSnapshot` object identity when merging a successful tutorial write so already-composed combat persistence observes the new shared revision.
- Add semantic combat checkpoints after attempt persistence and after truthful result presentation persistence. Tutorial code observes these checkpoints and never mutates combat state directly.
- Resolve authored focus IDs through a scene-scoped `TutorialTargetRegistry`. A resolved overlay action invokes the ordinary combat request once while all unrelated UI remains gated.
- Show dialogue/focus only in the safe standard-enemy Lobby predicate. Release the tutorial lease for the timed question, combat feedback, encounter-clear wait, and terminal flows.
- Treat scene/process lifetime as a presentation boundary: if persisted progress is `Active` during initialization, durably re-queue it and restart from the authored safe entry. Preserve the original trigger, variant, and one-time `rewardClaimed` state.
- Keep trusted backend commands, Firebase-rule publication, future tutorials, rewards, final art/audio, and release publication outside this slice.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Hardcode `OnFirstCreate` in the Main Menu presenter | Fast initial implementation | Couples copy, branching, input, persistence, and combat; later tutorials multiply conditionals |
| Store one global checkpoint string | Small schema | Cannot queue/version multiple tutorials or retain truthful recovery context |
| Drive progression from animation timing | Easy to demo | Reconnect unsafe and can advance before gameplay persistence succeeds |
| Create a separate fake first battle | Full tutorial control | Does not teach the production interaction and can diverge from real combat rules |
| Give tutorial code direct combat-engine access | Fewer adapters | Breaks authority boundaries and makes duplicate commands likely |

## Consequences

### Positive

- Later sequences can reuse the reducer, persistence contract, overlay, safe-state policy, and target registry.
- Copy and presentation content can change without replaying completed progression.
- Combat remains the source of truth for attempt identity and outcome.
- Shared revision ownership prevents tutorial saves from making the live gameplay adapter stale.
- The overlay is localizable and can focus scene or UI Toolkit targets through the same semantic contract.

### Negative / Trade-offs

- Schema V5 adds a dynamic map and contextual fields to each player save.
- The direct-Firestore adapter remains client-authoritative prototype infrastructure; it is not a production security boundary.
- UI Toolkit asset import and visual behavior still require manual Editor/mobile WebGL validation.
- Only one catalog entry is enabled, so multi-tutorial priority ordering remains future work even though the storage and reducer support multiple stable IDs.

### Migration

1. V4 saves gain an empty `tutorialMap`; no existing player is silently marked complete.
2. Character-complete accounts without an entry queue `OnFirstCreate`; legacy accounts use the returning welcome step and keep their current run.
3. Full admin reset clears the map. Death, Rebirth, content changes, and locale changes do not.
4. Unknown future schema/status/version values fail closed and require an updated client.
5. Interrupted active tutorials restart at their authored entry after the authoritative combat receipt/settlement becomes safe; completed tutorials never reopen and claimed rewards never reset.

## Approval

Accepted by the project owner with `lgtm` on 2026-09-14. Implementation completed on 2026-09-15; implementation review, visual playtest, and any Firebase/release publication remain separate checkpoints.

## Related

- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary
- ADR-011: Version Control, Browser Cache Management, and Data Migration
- ADR-012: Persisted Combat Presentation Receipts and Orchestration
- ADR-014: Scene-Scoped UI Composition and Shared Motion
- ADR-016: Player Lifecycle and Live Service Boundaries
- `Docs_PowerMath/3_Outputs/Specs/on-first-create-tutorial-arch-plan.md`
