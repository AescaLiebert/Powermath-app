---
slug: on-first-create-tutorial
status: needs-human
source: manual
gdd_tags:
  - tutorial-system
  - combat-attempt
  - server-authority
  - feedback
  - player-experience
  - guardrails
  - playtest
owner: Codex
human_checkpoint: required
next_agent: reviewer-agent
blocked_by:
  - implementation-review-and-manual-playtest
---

# Task Card: State-Driven `OnFirstCreate` Tutorial

## Player-Facing Goal

Introduce Power and Math:World through the real first combat interaction. The tutorial must celebrate the child's first attempt without misrepresenting an incorrect answer, disappear for the timed question and battle resolution, wait for authoritative state, and hand control back when the next standard enemy is ready.

## Source

- Origin: project-owner request on 2026-09-14.
- Requested by: project owner.
- Canonical design: `Docs_PowerMath/1_Inputs_Templates/GDD_PowerMathProject.md` at `@tag:tutorial-system`.
- Workflow: `/implement-feature`.

## Current State

- `PlayerSnapshot.TutorialData` stores only `version` and `checkpointId`; it does not implement the GDD `tutorialMap` contract.
- `PlayerLifecycleCommands.AdvanceTutorial` deliberately throws because no authored tutorial catalog exists.
- `LocalizationService` already supplies Thai/English keyed text from `Resources/Localization/UI.json`.
- `MainMenuInteractionGate` already owns scoped UI locks.
- `CombatLobbyPresenter` already waits for committed attempts, presentation completion, and persisted acknowledgement, but exposes no generic tutorial-facing combat signals.
- No tutorial overlay, definition asset, state engine, progress store, or tutorial tests exist.

## Target State

- A reusable, Unity-free tutorial state engine evaluates a data-authored sequence against semantic game signals.
- Tutorial content, localized keys, emotions, presentation cues, focus targets, wait conditions, and completion policy are authored outside presenter code.
- Firebase stores one durable entry per stable tutorial ID; missing entries remain eligible for legacy players.
- Only `OnFirstCreate` is authored and enabled in this delivery. Other sequences remain future catalog entries, not switch branches or placeholder runtime behavior.
- Guided Attack invokes the same `CombatLobbyView.RequestAttack()` / combat coordinator path as normal input.
- The overlay and focus layer use the existing shared interaction gate and never cover a video, answer window, result sequence, settlement, or other unsafe state.

## Type

- [x] Feature
- [x] Persistence migration
- [x] UI/UX onboarding
- [ ] Build/CI
- [ ] Dependency change
- [ ] Publishing/deployment

## Scope

### Systems Affected

- Tutorial core state machine and authored definitions.
- Tutorial progress mapping, defaults, migration, reset, and Firestore commands.
- Main Menu composition, interaction gate, combat semantic events, and localized overlay.
- Thai/English tutorial copy and accessibility behavior.
- EditMode state/persistence/contract tests and PlayMode interaction/recovery tests.

### Files To Inspect First

- `Assets/Project/Script/PlayerData/PlayerSnapshot.cs`
- `Assets/Project/Script/PlayerData/PlayerSchemaMigrator.cs`
- `Assets/Project/Script/Session/FirestoreRestClient.cs`
- `Assets/Project/Script/Session/PlayerDefaultsPlanner.cs`
- `Assets/Project/Script/Session/PlayerLifecycleCommands.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatLobbyPresenter.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatLobbyView.cs`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/MainMenuInteractionGate.cs`
- `Assets/Project/Script/Localization/LocalizationService.cs`
- `Assets/Project/Resources/Localization/UI.json`
- `Assets/Project/UI/MainMenu/MainMenuShell.uxml`

### Out of Scope

- `OnFirstRankChange`, `OnFirstRebirth`, tutorial rewards, forced gacha, pet demonstration, and Power Rescue.
- New gameplay rules, altered math timing, automatic answers, manufactured success, or direct tutorial writes to combat/economy/Rank state.
- New packages, CI/build settings, Firebase deployment, hosted schema publication, PR creation/merge, or final production art/audio acceptance.

## Acceptance Criteria

- [x] Missing or incomplete `OnFirstCreate` progress becomes eligible after character creation, including legacy accounts.
- [x] Eligibility queues durably and starts only at a safe Lobby with a standard enemy.
- [x] One tutorial runs at a time; reconnect resumes the saved incomplete step without replaying accepted gameplay commands.
- [x] Welcome, Math:World, first-try result branches, and next-enemy handoff use localization keys, not literal presenter strings.
- [x] The focus layer exposes exactly one required target and blocks unrelated UI.
- [x] The guided enemy tap calls the ordinary Attack request once; repeated taps cannot duplicate the attempt.
- [x] Tutorial presentation is absent through video, preparation, answer timing, save, and combat result presentation.
- [x] Correct, incorrect, timeout, and abandoned first attempts receive truthful effort-aware copy.
- [x] If the enemy survives, normal battle input is restored until authoritative encounter advancement.
- [x] Completion is persisted only after the next standard enemy is ready and the final handoff dialogue is advanced.
- [x] Changing text, sprites, or motion data cannot replay the tutorial or reapply an interaction.
- [x] Thai/English switching refreshes visible tutorial text; reduced motion preserves all story information.
- [x] Unknown tutorial status/version fails closed with recovery UI and does not unlock unsafe interactions.
- [ ] Existing combat recovery, interaction gate, session migration, and localization tests remain green.

## Human Checkpoints

- [x] Approve exact English/Thai copy and emotion/cue mapping in the design spec (`lgtm`, 2026-09-14).
- [x] Approve the generic tutorial-map/state-machine architecture and schema migration (`lgtm`, 2026-09-14).
- [ ] Approve final visual motion, Power art, audio, contrast, and mobile target sizing after PlayMode review.
- [ ] Approve hosted schema/rules deployment separately if production publication is requested.
- [ ] Approve PR merge and team-status publication.

## Router Decision

- Workflow: `/implement-feature`.
- Draft design: `Docs_PowerMath/3_Outputs/Specs/on-first-create-tutorial-design-spec.md`.
- Draft architecture: `Docs_PowerMath/3_Outputs/Specs/on-first-create-tutorial-arch-plan.md`.
- Current checkpoint: implementation review plus manual Editor/mobile WebGL visual and recovery playtest.

## Implementation Result

Implemented on 2026-09-15. The delivery includes the Unity-free reducer, authored `OnFirstCreate` asset, semantic focus-target registry, localized overlay, combat persistence checkpoints, V5 dynamic `tutorialMap`, interrupted-attempt recovery, reset/default mapping, and focused tests. See the companion test plan and DevLog for automated validation and remaining manual gates.
