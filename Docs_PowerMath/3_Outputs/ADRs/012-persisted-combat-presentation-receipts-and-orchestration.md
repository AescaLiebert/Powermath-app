# ADR-012: Persisted Combat Presentation Receipts and Orchestration

| Field       | Value                    |
|-------------|--------------------------|
| Status      | **Accepted**             |
| Date        | 2026-08-28               |
| Author      | architect-agent          |
| GDD Section | `core-loop`, `combat-attempt`, `stage-progression`, `run-reset`, `server-authority`, `feedback`, `player-experience`, `guardrails`, `playtest` |

Accepted by the project owner on 2026-08-28 (`LGTM`).

## Context

Combat authority saves an accepted result before scene-only feedback and records presentation completion afterward, but the accepted result payload is not persisted. Reload can therefore lose an unfinished hit sequence, while Death settlement can expose the reset run before the mandatory defeat presentation is acknowledged. Actor, FCT, enemy-action queue, and blocking UI behavior also lack a common semantic state and interaction-readiness contract.

## Decision

1. Persist a versioned semantic attempt outcome receipt at `activeRun.pendingPresentation` from `AttemptResolved` until the matching `PresentationCompleted` acknowledgement succeeds.
2. Persist a versioned Death or Rebirth presentation receipt under `lastRunSettlement`, with explicit `Pending` and `Acknowledged` status. Death and Rebirth may share authoritative settlement data, but they remain distinct presentation modes.
3. Rebuild presentation plans locally from semantic receipts. Do not persist animation frames, tween progress, Animator state names, scene references, or other Unity implementation keys.
4. Add a Unity-free assembly for semantic plan building, actor/UI state, and readiness evaluation. A scene-scoped combat presentation director coordinates specialized actor, FCT, action-queue, impact, and result-panel presenters.
5. Use one shared, scoped interaction gate for combat, panels, terminal results, and existing Main Menu transitions. Interaction unlocks only when authority is ready, the action plan is empty, both living actors are Idle, the enemy-action queue is stable, and all blocking UI is stable.
6. Use the existing LeanTween and TextMeshPro support. FCT is pooled uGUI/TMP content anchored to the affected actor and follows Pop, Hold, then Slide/Fade. Do not add Cinemachine or another presentation dependency.
7. Apply critical-hit impulse to a dedicated combat-world Canvas root because the current world art is Screen Space Overlay. Reduced Motion suppresses the impulse.
8. Store settlement Effective ATK before/after only as immutable display-receipt values. Presentation may read them, but combat authority must never consume them as gameplay inputs.
9. Migrate the player schema from V1 to V2 for the new receipts. Unknown receipt versions fail closed so interaction cannot resume from an uninterpretable pending result.

## Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Keep coroutine-only feedback | Smallest code change | Refresh can skip or lose an accepted result and the Death obligation |
| Persist current animation state or frame | Could attempt an exact visual resume | Couples saves to clips and tweens and is not version-safe |
| Persist the entire rendered action list | Direct replay input | Stores presentation implementation instead of authoritative meaning |
| Delay settlement mutation until the panel closes | Avoids an acknowledgement field | Mixes presentation with settlement eligibility and makes terminal rewards/reset vulnerable to disconnects |
| Let each presenter toggle controls independently | Locally simple | Creates transient unlocks and untraceable lock ownership |
| Add Cinemachine or another presentation package | Provides ready-made effects | Does not solve overlay-Canvas movement and adds an unnecessary dependency |
| Keep FCT in the root UI Toolkit document | Avoids a component migration | Cannot reliably anchor to separate uGUI actor bounds |

## Consequences

### Positive

- Refresh and reconnect can replay accepted combat and mandatory Death/Rebirth results without duplicating gameplay.
- Future follow-up and counter attacks extend semantic data rather than accumulating presenter conditionals.
- Actor, FCT, enemy-action queue, and UI lifecycle behavior become authorable and testable.
- A single interaction gate makes lock ownership and readiness traceable.
- Animation and art can change without a persistence migration.

### Negative / Trade-offs

- Player schema and Firestore mapping/patch surfaces grow.
- The pure planning assembly and focused presenters increase the initial file count.
- Result saves carry a larger fixed receipt, and settlement acknowledgement adds one write before Restart.
- Hybrid UI coordinate conversion and queue geometry require targeted EditMode and PlayMode tests.
- Display-only Effective ATK values are intentionally redundant and must never become combat inputs.

### Migration

1. Add V2 receipt fields and safe defaults while retaining the legacy feedback path.
2. Write receipts at `AttemptResolved` and settlement, but keep recovery disabled until composition validation passes.
3. Cut presentation playback to the new director.
4. Enable recovery and settlement acknowledgement.
5. Remove legacy visual paths only after regression parity.
6. Update the hosted schema manifest only under its separate publication approval.

## Related

- ADR-004: Combat Runtime Boundary and Development Simulation.
- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary.
- ADR-008: Atomic Run Settlement and Weapon Ascension.
- ADR-009: Data-Driven Stage Map and Encounter Runtime.
- ADR-011: Version Control, Browser Cache Management, and Client Data Migration.
- Architecture plan: `Docs_PowerMath/3_Outputs/Specs/combat-game-juice-data-driven-arch-plan.md`.
- Task card: `Docs_PowerMath/3_Outputs/Specs/combat-game-juice-data-driven-task-card.md`.
