---
slug: combat-game-juice-data-driven
status: implemented-awaiting-human-review
source: manual
gdd_tags:
  - core-loop
  - combat-attempt
  - stage-progression
  - run-reset
  - server-authority
  - feedback
  - player-experience
  - guardrails
  - playtest
owner: project-owner
human_checkpoint: required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Data-Driven Combat Presentation and Game Juice

## Player-Facing Goal

Make every accepted combat result play as a readable character-and-UI action sequence: the player acts first, the target reacts, enemy action boxes advance, and interaction returns only after both living actors and all blocking UI are stable. Death and Rebirth share settlement data but remain visually and interactively distinct, with Death replaying after refresh until the forced Restart is acknowledged.

## Source

- Origin: Manual project-owner request on 2026-08-28.
- Requested by: Project owner.
- Design approval: `LGTM` on 2026-08-28.
- Impact-accent extension approval: `LGTM` on 2026-09-15.
- Canonical design artifact: `Docs_PowerMath/3_Outputs/Specs/combat-game-juice-data-driven-design-spec.md`.

## GDD Reference

- `@tag:core-loop` - combat result must lead clearly to the next decision or run reset.
- `@tag:combat-attempt` - player damage resolves before enemy retaliation; lethal damage cancels enemy action.
- `@tag:stage-progression` - the next saved encounter appears before combat becomes ready.
- `@tag:run-reset` - Death and Rebirth share authoritative reset rules while presentation may differ.
- `@tag:server-authority` - attempts, cooldowns, damage, settlement, and reconnect results remain authoritative and idempotent.
- `@tag:feedback` - significant combat, Rank, Death, and Rebirth events require visual and audio feedback.
- `@tag:player-experience` - response and clarity take priority over spectacle.
- `@tag:guardrails` - presentation cannot mutate progression or reopen unsafe combat.
- `@tag:playtest` - refresh, abuse, readability, and Reduced Motion behavior require validation.

## Type

- [x] Feature
- [x] Bug fix: FCT currently has no enemy-relative anchor.
- [x] Refactor: split monolithic combat feedback into semantic orchestration and component presenters.
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

Recommended workflow: `/implement-feature`, with the approved design consumed by the architect-agent.

## Scope

### Systems Affected

- Combat attempt/result orchestration and presentation-completion recovery.
- Player and enemy Canvas presentation state machines.
- Target-anchored pooled FCT/FRT with bounded random spawn and off-screen drop, plus normal/critical impact impulse hierarchy.
- Enemy Walk/Attack action queue UI and animated reflow.
- Main Menu interaction/focus gate and UI lifecycle transitions.
- Death/Rebirth settlement presentation and acknowledgement persistence.
- ScriptableObject presentation profiles and Reduced Motion variants.
- EditMode/PlayMode contract and recovery tests.

### Files To Inspect First

- `Assets/Project/Script/Gameplay/Combat/Core/CombatAttemptCoordinator.cs`
- `Assets/Project/Script/Gameplay/Combat/Core/AttemptAuthorityModels.cs`
- `Assets/Project/Script/Gameplay/Combat/Core/IGameplayPersistence.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatLobbyPresenter.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/AttemptFeedbackSequence.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatFeedbackPlayer.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatLobbyView.cs`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
- `Assets/Project/Script/UI/MainMenu/RunEconomy/RunSettlementPanelController.cs`
- `Assets/Project/Script/Gameplay/Academic/FirestoreGameplayPersistence.cs`
- `Assets/Project/Script/PlayerData/PlayerSnapshot.cs`
- `Assets/Project/Script/PlayerData/PlayerSessionStore.cs`
- `Assets/Project/UI/MainMenu/CombatSurface.uxml`
- `Assets/Project/UI/MainMenu/RebirthPanel.uxml`
- `Assets/Project/Scenes/MainMenuScene.unity`

### Out of Scope

- Combat formula, question timing, Rank, cooldown, settlement-reward, or economy-rule changes.
- Potion gameplay rules; only a reserved presentation semantic is allowed until the GDD defines potions.
- Final/canon character animation, VFX, FCT font, sound, or art production.
- New third-party dependencies, Cinemachine, build/CI settings, deployment, publishing, merge, or release.
- Automatically skipping or fast-forwarding committed combat presentation.

## Acceptance Criteria

- [x] Semantic action plans reconstruct deterministically from accepted attempt/settlement results and support future follow-up/counter steps.
- [x] Player and enemy presentation states enforce the approved entry, exit, interrupt, and chaining rules.
- [x] Player Attack/Enemy TakeDamage and Enemy Attack/Player TakeDamage execute as synchronized impact-marker pairs rather than serial actor actions.
- [x] Enemy impact updates hearts immediately and adds a light combat-world impulse plus the existing player knockback; Reduced Motion removes only the translation.
- [x] Normal, critical, and player-damage contacts locally hold attacker/reactor poses without pausing global time, authority, UI, or audio.
- [x] Player attacks use positional anticipation without silhouette squash/stretch; Enemy actors may use restrained deformation. Target reactions use one directional knockback and small recovery overshoot instead of repeated oscillation.
- [x] Normal, critical, and player-damage hits create distinct reusable target-anchored starbursts, and a lost heart receives a short punch reaction.
- [x] Normal/critical post-hit tails are shortened to 0.20/0.35 seconds while interaction remains locked through settlement.
- [x] Ordinary game interaction stays locked until authority is ready, the plan is empty, both living actors are Idle, action boxes are stable, and blocking UI is stable.
- [x] FCT/FRT spawn within a bounded random area around their source anchor and follow Pop/60-ms-apex-hold/gravity-drop-below-screen lifecycle with random rotation direction.
- [x] Normal hits trigger a restrained combat-root impulse; critical hits use a stronger distinct FCT/reaction/impulse tier. Reduced Motion removes translation. Production audio remains an asset-wiring checkpoint.
- [x] The first-left Walk/Attack box arms at commit, consumes after player presentation, is removed, and remaining boxes animate reflow.
- [x] New/reset enemy queues initiate and new enemies reach Idle before Attack unlocks.
- [x] Die completes before the mandatory Death result panel enters.
- [x] Refresh before Death acknowledgement replays the Death presentation and cannot expose Stage 1 interaction early.
- [x] Rebirth never uses Die and retains confirmation/cancel only before authoritative acceptance.
- [x] All combat UI units implement Hidden/Entering/Idle/Exiting lifecycle and deterministic cleanup.
- [x] Presentation callbacks never calculate or mutate authoritative gameplay values.
- [ ] Existing attempt idempotency, content-failure rollback, Stage progression, Rank transition, and settlement tests remain valid.
- [ ] Works with mouse/keyboard in Editor and remains WebGL/mobile-compatible.
- [ ] Follows `RULES_AND_POLICY.md` and existing ADRs.

## Human Checkpoints

- [x] Design/game-feel approval (`LGTM`, 2026-08-28).
- [x] Architecture/ADR approval (`LGTM`, 2026-08-28).
- [ ] Human timing, motion, FCT, audio, and Death/Rebirth visual review.
- [x] Persistence migration approval included with architecture and implementation approval (`LGTM`, 2026-08-28).
- [ ] PR review before merge.
- [ ] Team-status publish approval.

## Router Decision

- Workflow: `/implement-feature`
- Current artifact: `Docs_PowerMath/3_Outputs/Specs/combat-game-juice-data-driven-arch-plan.md`
- Next agent: `qa-agent` / project owner visual review.
- Required output: execute `Docs_PowerMath/3_Outputs/TestPlans/combat-game-juice-data-driven-test-plan.md` after leaving Play Mode.
- Human checkpoint: implementation is compile-validated; retain timing/motion/FCT/audio/Death-Rebirth, PR, and publishing checkpoints.
- Blockers: focused runtime and EditMode assemblies compile cleanly; executing the Unity Test Runner and judging motion feel remain open-Editor human checkpoints.
