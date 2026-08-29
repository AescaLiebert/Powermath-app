---
slug: ui-architecture-centralization
status: implemented-awaiting-human-review
source: manual
gdd_tags:
  - feedback
  - player-experience
  - guardrails
  - playtest
owner: implementation-agent
human_checkpoint: required
next_agent: human
blocked_by: []
---

# Task Card: UI Architecture Centralization

## Player-Facing Goal

Every screen and panel should open, respond, idle, and close with predictable motion and without unrelated UI breaking when one feature changes. Input response and outcome clarity remain more important than decorative motion.

## Source

- Origin: Manual prompt on 2026-08-29.
- Requested work: centralize UI dependencies and lifecycle, remove hard-coded/duplicated behavior, and provide one consistent LeanTween-backed UI animation driver.

## GDD Reference

- `@tag:feedback` - significant actions require clear visual feedback, with audio where the action is significant.
- `@tag:player-experience` - protect response and clarity before spectacle.
- `@tag:guardrails` - UI presentation must not take ownership of gameplay authority.
- `@tag:playtest` - stress repeated inputs and reduced-motion behavior.

## Type

- [x] Refactor
- [x] Player-facing presentation feature
- [ ] Bug fix
- [ ] Tooling / CI

## Scope

### Systems Affected

- UI Toolkit entry documents for Bootstrap, Authentication, and Main Menu.
- Main Menu navigation, modal visibility, focus restoration, and interaction blocking.
- Existing Main Menu transition and Player Hub feedback LeanTween implementations.
- Combat UI lifecycle elements currently owned by `UiToolkitLifecycleController`.
- Shared USS motion rules and reduced-motion behavior.
- Scene composition and explicit UI dependency wiring.

### Files To Inspect First

- `Assets/Project/Script/UI/`
- `Assets/Project/Script/Gameplay/Combat/Unity/MainMenuPanelHost.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/UiToolkitLifecycleController.cs`
- `Assets/Project/UI/`
- `Docs_PowerMath/3_Outputs/Specs/main-menu-tween-transitions-arch-plan.md`

### Out of Scope

- Changing gameplay, persistence, economy, or combat authority.
- Rebuilding UI Toolkit screens as runtime-generated C# UI.
- Adding a dependency-injection framework or replacing LeanTween.
- Reauthoring visual art, typography, layout, or audio content.
- Changing build settings, CI, packages, or deployment.
- Migrating all UI atomically in one change.

## Acceptance Criteria

- [x] A scene-scoped UI composition boundary supplies explicit shared UI dependencies.
- [ ] Feature views query only their own root/subtree; cross-feature queries are removed.
- [x] One panel lifecycle owner controls display, picking, focus handoff, enter, and exit completion.
- [x] Direct `LeanTween.*` calls exist only inside the shared driver; feature feedback composes driver primitives.
- [x] Enter, exit, hover/focus, press, optional idle, cancellation, and reduced motion use one motion profile.
- [x] Rapid reversal applies a documented final state and never leaves input blocked or UI partially transformed.
- [x] Semantic state (`Ready`, `Busy`, `Success`, `Error`, `Blocked`) stays separate from motion state.
- [ ] Main Menu transition, Player Hub feedback, combat lifecycle, Authentication, and Bootstrap migrate incrementally with regression coverage.
- [x] No per-frame polling or recurring allocation is added after motion completes.
- [x] Existing gameplay, transaction, focus, and panel-exclusivity behavior remains intact under automated pilot coverage.
- [x] Human approves motion feel and architecture before runtime implementation.

## Human Checkpoints

- [x] Design/game-feel approval for the shared motion profile.
- [x] Architecture and ADR approval.
- [ ] Review after the Main Menu pilot migration before broader rollout.
- [ ] PR review before merge.

## Router Decision

Workflow: `/refactor`, with a player-facing design checkpoint because motion behavior changes.

Current artifacts:

- `Docs_PowerMath/3_Outputs/Specs/ui-architecture-centralization-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/ui-architecture-centralization-impact-analysis.md`
- `Docs_PowerMath/3_Outputs/Specs/ui-architecture-centralization-arch-plan.md`
- `Docs_PowerMath/3_Outputs/ADRs/014-scene-scoped-ui-composition-and-shared-motion.md`
- `Docs_PowerMath/3_Outputs/TestPlans/ui-architecture-centralization-test-plan.md`

Next agent: human pilot reviewer. ADR-014 and the Main Menu pilot were approved by the project owner on 2026-08-29; implementation and automated pilot verification completed the same day.

## Pilot Implementation Status

- Added the gameplay-independent UI Core assembly, authored shared motion profile, LeanTween driver, panel lifecycle, interaction binder, binding helper, and read-only scene context.
- Migrated Main Menu panel lifecycle, bootstrap/session-return tween execution, and Player Hub feedback execution without moving gameplay or transaction policy.
- Removed duplicate LeanTween ownership and conflicting transform transitions from migrated Player Hub targets.
- Unity compilation passed; UI Core EditMode passed 7/7 and Combat Unity EditMode passed 28/28.
- Broader feature-subtree extraction, combat lifecycle, Authentication, and Bootstrap migration remain intentionally gated behind the Main Menu pilot review.
