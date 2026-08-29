# ADR-014: Scene-Scoped UI Composition and Shared Motion

| Field | Value |
| --- | --- |
| Status | **Accepted** |
| Date | 2026-08-29 |
| Author | architect-agent |
| GDD Section | `@tag:feedback`, `@tag:player-experience`, `@tag:guardrails` |

Approved by the project owner on 2026-08-29 for additive foundation work and the Main Menu pilot migration.

## Context

PowerMath UI currently combines direct element queries, immediate display writes, scheduled USS lifecycle classes, feature-local LeanTween code, runtime dependency fallbacks, and repeated motion values. These overlapping owners make isolated UI changes capable of breaking visibility, focus, input, or unrelated feature presentation.

## Decision

Adopt a scene-scoped UI architecture:

- Add a gameplay-independent `PowerMath.UI.Core` assembly.
- Construct one `LeanTweenUiDriver` and one read-only `UiSceneContext` per `UIDocument` scene.
- Use one `UiMotionProfileDefinition` for shared enter, exit, hover/focus, press, optional idle, cancellation, and reduced-motion tokens.
- Make `UiPanelLifecycle` the sole owner of panel display, picking, lifecycle state, and motion finalization.
- Keep navigation/panel identity and focus in the scene panel host.
- Restrict views to binding/rendering their own subtree and exposing user intents.
- Keep feature use-case policy, gameplay authority, persistence, and transaction behavior outside UI Core.
- Route feature-specific feedback through shared motion primitives; direct `LeanTween.*` calls are confined to the driver.
- Migrate incrementally, beginning with the Main Menu transition and Player Hub feedback that already use LeanTween.

## Alternatives Considered

| Option | Pros | Cons |
| --- | --- | --- |
| Global persistent `UIManager` singleton | One apparent access point | Hidden dependencies, scene lifetime bugs, god-object growth, difficult tests |
| USS-only motion | Native UI Toolkit styling and low C# surface | Does not unify Canvas targets, sequence cancellation, sampled reversals, or feature feedback |
| LeanTween in each feature | Maximum local freedom | Continues duplicated timings, cancellation, reduced motion, and channel conflicts |
| Replace UI Toolkit or add a third-party UI framework | Could impose a new model | Large rewrite, new dependency, invalidates active work, unnecessary for the current problems |
| Scene-scoped shared driver and lifecycle | Explicit lifetime, testable boundaries, supports UI Toolkit and Canvas, incremental migration | Requires careful adapter migration and removal of conflicting USS transforms |

## Consequences

### Positive

- One motion/cancellation implementation creates consistent behavior.
- Scene lifetime and disposal are explicit; no cross-scene singleton state.
- Views, navigation, lifecycle, and gameplay authority have separate owners.
- Existing Main Menu sequence and feature feedback can migrate without rewriting their policies.
- Reduced motion is enforced through one execution path.

### Negative / Trade-offs

- A shared UI Core assembly and explicit scene wiring add initial migration work.
- Migrated elements cannot keep independent USS transform transitions on the same channels.
- Existing runtime fallback wiring must remain temporarily until scene assets are validated and resaved.
- Subjective motion values still require human playtesting; centralization does not make them automatically correct.

### Migration

- Add the foundation and tests without changing callers.
- Pilot on Main Menu navigation, transition, and Player Hub feedback.
- Migrate panels individually, then combat lifecycle and entry screens.
- Split scene/UI wiring out of `CombatLobbyCompositionRoot` only after behavior is protected.
- Remove deprecated lifecycle helpers, direct LeanTween calls, hard-coded discovery, and duplicate transform rules after no callers remain.

## Related

- `Docs_PowerMath/3_Outputs/Specs/main-menu-tween-transitions-arch-plan.md`
- `Docs_PowerMath/3_Outputs/ADRs/012-persisted-combat-presentation-receipts-and-orchestration.md`
- `Docs_PowerMath/3_Outputs/ADRs/013-canonical-pet-definitions-loadout-and-shared-main-menu-feedback.md`
- `Docs_PowerMath/3_Outputs/Specs/ui-architecture-centralization-impact-analysis.md`
- `Docs_PowerMath/3_Outputs/Specs/ui-architecture-centralization-arch-plan.md`
