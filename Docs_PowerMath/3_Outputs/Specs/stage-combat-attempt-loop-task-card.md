---
slug: stage-combat-attempt-loop
status: approved
source: manual
gdd_tags:
  - core-loop
  - combat-attempt
  - answer-scoring
  - combat-stats
  - question-data
  - stage-progression
  - feedback
  - player-experience
  - guardrails
owner: orchestrator-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Task Card: Stage Combat Attempt Loop

## Player-Facing Goal

The student sees the current Stage and enemy state, commits one attack, watches a question video when content is available, enters one integer answer through a responsive numpad under the documented timer, and receives an unmistakable result that flows into damage, enemy response, or stage advancement.

## Source

- Origin: Manual Codex goal submitted on 2026-08-10.
- Requested outcome: Stage progress, enemy entity and UI, attempt sequence, numpad, timer, score-to-damage resolution, and high-clarity damage feedback.
- Temporary integration need: Firestore question/video delivery is unavailable during development.

## GDD Reference

- `@tag:core-loop` - Attack is a committed combat attempt; defeating an enemy advances the visual stage.
- `@tag:combat-attempt` - Player damage resolves before a possible enemy counterattack.
- `@tag:answer-scoring` - Integer-only numpad, one submission, 1-second preparation, and 10-second countdown.
- `@tag:combat-stats` - Correct attempts use the documented damage formula; incorrect or timed-out attempts deal zero damage.
- `@tag:question-data` - Missing/corrupt production content voids the attempt and restores enemy cooldown.
- `@tag:stage-progression` - Stage, World Level, Rank, and `question.id` remain independent.
- `@tag:feedback` - Significant actions require visual and audio feedback.
- `@tag:guardrails` - Attack is irreversible except for confirmed system/content failure; stage advances only after enemy defeat.

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Current Implementation

- `MainMenuScene` and `MainMenuUI.uxml` show player identity, Stage progress, wallet/loadout summary, and an unbound attack image.
- `MainMenuPresenter`, `MainMenuView`, and `MainMenuViewModel` establish the current UI Toolkit presenter/view pattern.
- `PlayerSnapshot.ActiveRunData` exposes `runId`, `currentStage`, and `committedAttemptId`, but no enemy or active-attempt projection.
- `PlayerSessionStore` exposes a hydrated snapshot and change event.
- Direct Firestore REST is currently limited to prototype authentication/bootstrap under accepted ADR-003; gameplay writes remain out of scope.
- No current enemy domain, combat-attempt coordinator, question/video adapter, numpad flow, damage resolver, gameplay feedback controller, or gameplay tests were found.
- The project is on Unity `6000.5.3f1` and already includes UI Toolkit, Input System `1.19.0`, Video, Accessibility, URP `17.6.0`, and Test Framework `1.7.0`; this slice requires no new package dependency.
- `MainMenuScene` currently mixes a UI Toolkit `UIDocument` for player/menu data with a legacy uGUI `Canvas` for its enemy and background images.
- The scene already contains a `monsterPrefab`-named uGUI image using `Assets/Project/Material/Test-ปกคลิปสำหรับงานเเข่ง (1).png`; it is a scene placeholder, not a reusable enemy prefab or data definition.
- No project-owned gameplay test assembly was found; existing test-named assets belong to the bundled LeanTween examples.

## Target Scope

### Systems Affected

- Stage and ephemeral run-state projection.
- Enemy definition, spawned enemy state, health, cooldown, and presentation.
- Combat-attempt state machine and deterministic resolution order.
- Question acquisition/video seam with production failure and development simulation adapters.
- Integer answer input, preparation period, countdown, submission, and timeout.
- Response score and damage calculation seam.
- Damage, critical, enemy defeat, counterattack, and stage-progress feedback.
- UI Toolkit Main Menu combat panel and mobile-compatible input.

### Files To Inspect First

- `Assets/Project/Script/UI/MainMenu/MainMenuPresenter.cs`
- `Assets/Project/Script/UI/MainMenu/MainMenuView.cs`
- `Assets/Project/Script/UI/MainMenu/MainMenuViewModel.cs`
- `Assets/Project/Script/PlayerData/PlayerSnapshot.cs`
- `Assets/Project/Script/PlayerData/PlayerSessionStore.cs`
- `Assets/Project/UI/MainMenuUI.uxml`
- `Docs_PowerMath/3_Outputs/ADRs/003-direct-firestore-prototype-authentication.md`

### Out of Scope

- Five-question audit calculation, audit persistence, promotion/demotion, and Rank changes.
- Rank inventory FIFO implementation and mastery analytics.
- Rank Currency or other reward persistence.
- Production Firestore gameplay writes, security-rule changes, backend authority, CI/build settings, and deployment.
- Final enemy art/audio content, balance values, shops, minigames, RNG cards, death rewards, and Rebirth.

## Acceptance Criteria

- [x] The HUD clearly shows Stage, enemy identity/visual, current/max HP, and remaining/max attack cooldown.
- [x] Attack can commit only from the enemy-ready state and cannot double-commit.
- [x] A question provider is injected behind an interface; UI and combat logic do not know Firestore details.
- [x] Content failure can void the attempt and restore consumed cooldown, matching the future production adapter contract and GDD.
- [x] Explicit development simulation mode can exercise the attempt-to-damage flow without Firestore and without writing audit, Rank, wallet, analytics, or persisted run progress.
- [x] The numpad accepts digits, Backspace, Clear, and Submit; empty Submit is disabled; answer length is enforced; one submission ends the attempt.
- [x] The GDD-defined 1-second preparation and 10-second countdown behave correctly at zero and under low frame rates.
- [ ] Incorrect and timeout outcomes deal zero damage. Timeout is implemented and tested; incorrect-answer authority awaits the production question/result gateway.
- [x] Simulated success resolves score, damage, HP, enemy death, and possible counterattack in the documented order.
- [x] Enemy defeat advances the visual Stage exactly once and spawns the next enemy with full cooldown.
- [x] Normal damage, critical damage, zero-damage timeout, enemy defeat, and imminent counterattack are distinguishable without relying on color alone.
- [x] Significant results use visual and audio feedback; reduced-motion mode shortens movement without hiding semantic information.
- [x] Existing authentication, bootstrap, logout, audit, and Rank behavior remain untouched.
- [x] EditMode tests cover pure state, score/damage, stage, content recovery, and idempotency; PlayMode covers real-scene UI input through visible HP loss.
- [ ] Human WebGL and narrow mobile-aspect verification remains before release; the controls use UI Toolkit pointer events and keyboard input without platform-specific APIs.
- [x] Follows `RULES_AND_POLICY.md` and `unity-conventions.md` for this implementation slice.

## Implementation Status

- Current implementation: development-only, seeded, scene-memory simulation with a persistent `SIMULATION - NOT SAVED` badge.
- Current implementation: pure Core rules, injected authority/question ports, UI Toolkit presentation, generated feedback audio, and a runtime Main Menu composition bridge.
- Current implementation: 15 EditMode tests and one end-to-end PlayMode scene smoke pass.
- Target architecture remaining: Firestore/video question adapter, authoritative correct/incorrect result adapter, saved scene/data-asset wiring, and human WebGL/mobile visual QA.
- Review/merge checkpoint remains open; no PR, merge, build-setting, dependency, Firestore, or publishing action was performed.

## Human Checkpoints

- [x] Approve design/game-feel and the development-only random-score fallback policy. Approved by project owner on 2026-08-10.
- [x] Approve architecture and ADR-004 before code implementation. Approved by project owner on 2026-08-10.
- [ ] Approve any later build-setting, dependency, Firestore, or deployment change separately.
- [ ] Review the PR before merge.
- [ ] Approve official team-status publishing.

## Router Decision

- Workflow: `/implement-feature`
- Current artifact: `Docs_PowerMath/3_Outputs/Specs/stage-combat-attempt-loop-task-card.md`
- Next artifact: `Docs_PowerMath/3_Outputs/Specs/stage-combat-attempt-loop-design-spec.md`
- Human checkpoint: required before architecture work
