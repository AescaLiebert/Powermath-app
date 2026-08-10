# ADR-004: Combat Runtime Boundary and Development Simulation

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-10 |
| Author | Architect agent |
| GDD Section | `@tag:combat-attempt`, `@tag:combat-stats`, `@tag:question-data`, `@tag:stage-progression`, `@tag:server-authority`, `@tag:guardrails` |
| Extends | ADR-003 for prototype composition; does not authorize gameplay writes |

> Accepted by the project owner on 2026-08-10 (`LGTM`).

## Context

PowerMath needs its Stage/enemy/attempt/damage loop before Firestore question-video delivery or an authoritative gameplay API is available. The GDD requires server authority and content-failure rollback, while the approved design also needs a random-score fallback that lets developers exercise the complete client loop without modifying audit or Rank systems.

## Decision

Create a ports-and-adapters combat feature with these boundaries:

- `PowerMath.Gameplay.Combat.Core` contains pure domain rules, immutable projections, the attempt coordinator, and authority/presentation interfaces.
- `ICombatGateway` is the only state-changing authority port consumed by the coordinator.
- `LocalSimulationCombatGateway` is scene-scoped, seeded, non-persistent, visibly labelled, and available only in Editor/development builds.
- `UnavailableCombatGateway` fails closed when no production authority is available.
- A future `RemoteAuthoritativeCombatGateway` may replace local simulation without changing combat UI or sequencing.
- The current simulation maps its generated response score to `SimulationBaseATK`, uses Rank multiplier 1, and runs the GDD damage formula with explicit midpoint rounding away from zero.
- UI Toolkit owns the combat presentation and reacts to immutable results; it does not calculate damage, mutate HP/cooldown/Stage, or call Firestore.
- `PlayerSessionStore` supplies the initial Stage but receives no simulation mutation.
- Audit, Rank changes, currencies, analytics, and authoritative persistence remain outside this feature.

Simulation selection is compile/build guarded. If a non-development build requests simulation, the runtime constructs the fail-closed gateway instead.

## Alternatives Considered

| Option | Pros | Cons |
|---|---|---|
| Add all logic to `MainMenuPresenter` | Fastest initial wiring | Creates a god-object, couples UI to rules/data, makes future authority replacement expensive, and is difficult to test. |
| Call Firestore gameplay documents directly from combat UI | Reuses current REST client | Gameplay schema/authority is not approved by ADR-003, leaks transport concerns, cannot safely satisfy idempotent server authority, and blocks offline development. |
| Automatically randomize results whenever Firebase fails | Demo continues in every build | Silently invents educational/combat outcomes and conflicts with production content-failure rules. |
| Duplicate local and remote flows | Simple per-mode code | Causes state-machine and feedback drift, doubles testing, and makes bug fixes mode-specific. |
| **Authority port with guarded local adapter** | Testable, replaceable, deterministic, fail-closed, and keeps one UI/application flow | Adds explicit contracts and composition work before visible features. |

## Consequences

### Positive

- The complete attempt/numpad/timer/damage/Stage loop can be developed without Firebase.
- Simulation cannot corrupt audit, Rank, currencies, analytics, or persisted run data.
- The future backend can become authoritative behind one gateway contract.
- Domain rules are fast EditMode tests rather than scene-dependent tests.
- UI feedback consumes before/after values and cannot become accidental authority.
- Timer and duplicate-command behavior are deterministic and testable.

### Negative / Trade-offs

- The simulation Stage resets to the bootstrapped snapshot after scene reload.
- Local response-score-to-Base-ATK mapping is scaffolding and is intentionally not a production combat rule.
- The new assembly split requires a small bridge in predefined `Assembly-CSharp` because it owns the existing `PlayerSnapshot`.
- The current hybrid uGUI/UI Toolkit Lobby needs a controlled visual migration.
- Remote gameplay integration still requires a later ADR, API contract, and authority/recovery E2E tests.

### Migration

1. Add and test the pure combat Core assembly.
2. Add the guarded local gateway and ScriptableObject definitions.
3. Add UI Toolkit combat presentation and the composition bridge.
4. Move Stage/enemy presentation ownership out of the current menu view and legacy Canvas.
5. Validate simulation and fail-closed modes.
6. Add a remote gateway only after separate backend/authority approval.

## Related

- `Docs_PowerMath/3_Outputs/ADRs/003-direct-firestore-prototype-authentication.md`
- `Docs_PowerMath/3_Outputs/Specs/stage-combat-attempt-loop-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/stage-combat-attempt-loop-arch-plan.md`
- `Assets/Project/Script/UI/MainMenu/MainMenuPresenter.cs`
- `Assets/Project/Script/PlayerData/PlayerSessionStore.cs`
- `Assets/Project/UI/MainMenuUI.uxml`
