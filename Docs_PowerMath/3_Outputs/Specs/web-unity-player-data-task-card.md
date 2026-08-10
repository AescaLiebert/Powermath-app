---
slug: web-unity-player-data
status: approved
source: manual
gdd_tags:
  - server-authority
  - leaderboard-profile
  - guardrails
owner: codex
human_checkpoint: required
next_agent: code-review-agent
blocked_by: []
---

# Task Card: Web-to-Unity Player Data Bootstrap

## Player-Facing Goal

A student signs in once on the web app, sees a clear Unity loading scene, and enters the Main Menu only after their authoritative profile, progress, wallet, inventory, loadout, and active run are loaded successfully.

## Source

- Origin: Manual prompt in the active Codex session
- Requested by: Project owner
- Approved flow: `BootstrapScene` success-gates `MainMenuScene`

## GDD Reference

- `@tag:server-authority` - The server owns gameplay state and saves after committed actions.
- `@tag:leaderboard-profile` - Public profile fields are separated from private education data.
- `@tag:guardrails` - Persistent and run-reset state must remain distinct.

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- WebGL JavaScript handoff
- Game API session exchange
- Persistent player-session state
- Bootstrap and Main Menu scene flow
- UI Toolkit loading and player-data presentation

### Files To Inspect First

- `Docs_PowerMath/3_Outputs/Specs/web-unity-player-data-arch-plan.md`
- `Docs_PowerMath/3_Outputs/ADRs/001-web-unity-session-and-player-data-boundary.md`
- `Assets/Project/Script/Bootstrap/`
- `Assets/Project/Script/Session/`
- `Assets/Project/Script/PlayerData/`
- `Assets/Project/Script/UI/MainMenu/`

### Out of Scope

- Web/backend endpoint implementation
- Direct Firestore SDK access from Unity
- Deleting deprecated authentication assets
- Automated Unity tests; the project owner requested human E2E validation

## Acceptance Criteria

- [x] `BootstrapScene` exists and is build index 0.
- [x] `MainMenuScene` is build index 1 and cannot open through the bootstrap flow before valid player hydration.
- [x] Unity never receives username/password credentials.
- [x] Player data is exposed to Main Menu through a persistent read-only store.
- [x] Main Menu prototype identity/progress fields are data-bound.
- [x] Failure states remain in `BootstrapScene` with clear retry/sign-in/update feedback.
- [ ] Human E2E test passes against the real web/backend environment.
- [x] Existing unrelated worktree changes remain intact.

## Human Checkpoints

- [x] Design/game-feel approval
- [x] Architecture approval
- [ ] E2E acceptance
- [ ] PR review before merge
- [ ] Team status publish approval

## Router Decision

Recommended workflow: `/implement-feature`

Next artifact:

- `Docs_PowerMath/3_Outputs/Specs/web-unity-player-data-review.md`
