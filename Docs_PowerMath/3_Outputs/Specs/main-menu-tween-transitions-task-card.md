---
slug: main-menu-tween-transitions
status: approved
source: manual
gdd_tags:
  - core-loop
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human
blocked_by: []
---

# Task Card: Main Menu Tween Transitions

## Player-Facing Goal

Make entry into the Main Menu feel like the start of a battle, then make every return from Leaderboard, Pet Gacha, Profile Analytics, and Navigator feel like the lobby arrives as one cohesive motion instead of appearing instantly.

## Source

- Origin: project-owner prompt on 2026-08-27.
- Bootstrap request: a Pokemon-inspired `BATTLE START` beat, screen reveal, Player/Enemy entrance, then lobby-session reveal.
- Session request: after closing Leaderboard, Pet Gacha, Profile Analytics, or Navigator, slide the top HUD down, Player Menu from the left, and Player Dashboard up simultaneously with one shared duration.
- Visual constraint: use the original Math:World art direction and hierarchy; do not reproduce Pokemon branding, typography, or copyrighted effects.

## GDD Reference

- `@tag:core-loop` - the lobby must immediately expose the current encounter, Stage, HP/rules, hearts, and cooldown.
- `@tag:feedback` - significant actions require visual and audio feedback.
- `@tag:player-experience` - protect input response and outcome clarity before spectacle.
- `@tag:guardrails` - presentation must not mutate authoritative combat, encounter, or progression state.

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- UI Toolkit Main Menu shell and combat surface.
- Main Menu panel close lifecycle.
- Main Menu scene-entry/session-ready presentation.
- Existing Canvas-owned Player and Enemy images.
- UI audio feedback hooks and reduced-motion presentation.

### Files To Inspect First

- `Assets/Project/UI/MainMenu/MainMenuShell.uxml`
- `Assets/Project/UI/MainMenu/CombatSurface.uxml`
- `Assets/Project/UI/MainMenu/MainMenuExperience.uss`
- `Assets/Project/Script/Gameplay/Combat/Unity/MainMenuPanelHost.cs`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
- `Assets/Project/Scenes/MainMenuScene.unity`
- `Docs_PowerMath/3_Outputs/Specs/ui-mockup-experience-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/ui-mockup-experience-arch-plan.md`

### Out of Scope

- Changes to combat, session bootstrap, authentication, scene routing, persistence, economy, or panel contents.
- New packages or third-party tween dependencies.
- Final/canon audio or visual asset creation.
- Copying Pokemon logos, fonts, branded battle graphics, or exact animation assets.
- Build settings, CI, publishing, merge, or deployment.

## Acceptance Criteria

- [ ] First Main Menu entry plays `BATTLE START`, reveals the scene, brings Player and Enemy art into place, then reveals the interactive lobby HUD.
- [ ] Closing Leaderboard, Pet Gacha, Profile Analytics, or Navigator reveals top HUD, Player Menu, and Player Dashboard simultaneously with one shared duration.
- [ ] Rebirth and any future blocking/transaction panel do not become cancellable because of presentation logic.
- [ ] Repeated close/open, disable, scene unload, and low-frame-rate conditions settle into one valid visible state with no stranded input lock.
- [ ] The sequence uses live UI and Canvas elements; animation never changes gameplay state.
- [ ] A reduced-motion path communicates the same state change without large translation or scale motion.
- [ ] Works with mouse and keyboard on the current Windows Editor target and remains compatible with the GDD's Unity WebGL/mobile direction.
- [ ] Existing Main Menu binding and panel-host tests remain green.
- [ ] Follows `RULES_AND_POLICY.md`.

## Human Checkpoints

- [x] Design/game-feel approval (`lgtm`, 2026-08-27).
- [ ] Architecture approval.
- [ ] Human visual and audio playtest approval.
- [ ] PR review before merge.
- [ ] Team status publish approval.

## Implementation Progress

- [x] Architecture approved (`lgtm`, 2026-08-27).
- [x] UI Toolkit/Canvas transition controller implemented with the project's existing LeanTween dependency.
- [x] Bootstrap sequence wired after live/fallback combat presentation readiness.
- [x] Session-return sequence wired for Navigator, Pet Gacha, Leaderboard, and Profile Analytics accepted closes.
- [x] Reduced-motion and deterministic cancellation/final-state paths implemented.
- [x] Temporary original non-canon Player cutout generated and wired as replaceable Canvas art.
- [x] Focused Main Menu panel/binding/transition contracts passed 11/11.
- [x] Direct off-screen/final-state/reduced-motion verification passed 3/3.
- [x] Added cold/repeated Play Mode LeanTween driver recovery without resetting global tween state.
- [x] Unified session HUD motion into one simultaneous shared-duration tween.
- [x] Affected Combat Unity EditMode assembly passed 15/15.
- [x] Live state probe confirmed input lock during return and complete cleanup/unlock afterward.
- [ ] Human game-feel review of timing, scale, and temporary Player art.

## Router Decision

- Workflow: `/implement-feature`
- Current artifact: `Docs_PowerMath/3_Outputs/Specs/main-menu-tween-transitions-design-spec.md`
- Next agent: human
- Human checkpoint: implementation game-feel/visual review required
- Blocker: final acceptance requires human review; final/canon Player art remains separate.
