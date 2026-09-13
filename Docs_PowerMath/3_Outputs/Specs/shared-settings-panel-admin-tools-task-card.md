---
slug: shared-settings-panel-admin-tools
status: partially-superseded-by-mobile-webgl-admin-state-regression
source: manual
gdd_tags:
  - player-experience
  - combat-stats
  - answer-scoring
  - economy
  - server-authority
  - guardrails
owner: implementation-agent
human_checkpoint: required
next_agent: human-qa
blocked_by: []
---

# Task Card: Shared Settings Panel and Admin Test Tools

> [!NOTE]
> The hard-coded `test1`–`test10` allowlist, session-only combat overrides, and
> synthetic Quick Test behavior were superseded by ADR-018 and
> `mobile-webgl-admin-state-regression-task-card.md` after deployed testing.

## Player-Facing Goal

The Settings gear in Authentication and Main Menu opens the same child-friendly Settings panel. General and Sound controls stay simple and consistent. Only approved test accounts can see tools that manipulate test progression or reset their own save.

## Source

- Origin: direct product-owner prompt on 2026-09-10.
- Requested behavior: replace `AdminPanel` with a shared `SettingsPanel` used by Authentication and Main Menu.
- Admin allowlist requested for test accounts `test1` through `test10`.

## GDD Reference

- `@tag:player-experience` — protect input response and outcome clarity before spectacle.
- `@tag:combat-stats` — ATK, CR, CD, and HP have explicit meanings and clamps.
- `@tag:answer-scoring` — answers, timers, and the five-question audit resolve deterministically.
- `@tag:economy` — Rank Currency is cumulative achievement data; Power Coins are spendable.
- `@tag:server-authority` — gameplay and progression mutations are authoritative and traceable.
- `@tag:guardrails` — Rank, audit, question identity, HP, and currencies must remain separate concepts.
- The GDD does not define an admin bypass. Admin behavior is therefore a visibly test-only exception requiring explicit approval and architecture boundaries.

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- Authentication and Main Menu UI Toolkit composition.
- Shared device preferences for fullscreen, Music, and SFX.
- Test-account authorization and admin presentation.
- Combat test overrides for ATK, CR, CD, HP/invincibility, and question-video bypass.
- Rank, audit, wallet, player session, leaderboard projection, and full player-save reset commands.

### Files To Inspect First

- `Assets/Project/UI/Authentication/AuthenticationScreen.uxml`
- `Assets/Project/UI/MainMenu/AdminPanel.uxml`
- `Assets/Project/UI/MainMenu/AdminPanel.uss`
- `Assets/Project/UI/MainMenu/LegacyFeaturePanels.uxml`
- `Assets/Project/Script/UI/Authentication/AuthenticationView.cs`
- `Assets/Project/Script/UI/MainMenu/AdminPanelController.cs`
- `Assets/Project/Script/UI/MainMenu/SocialProfile/SocialProfileCompositionRoot.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatRuntimeSettingsDefinition.cs`
- `Assets/Project/Script/Session/FirestorePlayerResetService.cs`
- `Assets/Project/Script/Session/PlayerDefaultsPlanner.cs`

### Out of Scope

- Changing normal student balance, combat formulas, rank thresholds, or question content.
- Granting admin access to ordinary student accounts.
- Modifying Firebase rules, build settings, dependencies, deployment, or CI.
- Deleting `userdata` credentials or another player's data.
- Publishing or merging without human approval.

## Acceptance Criteria

- [x] Both Settings buttons open one reusable `SettingsPanel` template with identical content geometry and shared USS styling.
- [x] Authentication and Main Menu show General and Sound tabs.
- [x] General contains a fullscreen switch with accurate current state and failure feedback.
- [x] General contains persistent TH/EN selection and Account Logout on both shared-panel instances.
- [x] Account Logout clears remembered/runtime credentials, player session, and admin test state before returning to Authentication, so refresh cannot resume the old account.
- [x] Sound contains Music and SFX controls that apply immediately and persist locally across scenes/restarts.
- [x] Admin is absent from Authentication and hidden for every username except normalized exact matches `test1` through `test10`.
- [x] Client presentation gating is backed by command-side authorization checks; hiding a tab alone is not treated as security.
- [x] Admin supports clamped ATK, CR, CD, HP/invincibility, Rank, skip-video/answer-zero, Rank Currency, and Power Coin test commands.
- [x] Rank override explains and resets the partial audit before applying.
- [x] Save reset replaces `gamedata`, resets onboarding/player-preparation, removes the prior leaderboard projection, and preserves sibling login credentials.
- [x] Reset requires explicit destructive confirmation and cannot target a different account.
- [x] All mutation states show pending, success, and failure feedback and prevent duplicate submissions.
- [x] Uses the existing pointer/touch, focus, responsive flex, and Reduced Motion panel infrastructure.
- [x] Compile and contract verification covers authentication, audio, combat, and player-data integrations.
- [x] Follows `RULES_AND_POLICY.md`.

## Human Checkpoints

- [x] Approve the design defaults and admin exception policy (`LGTM`, 2026-09-10).
- [x] Approve architecture and mutation authority before implementation (`lgtm`, 2026-09-10).
- [ ] Approve any Firebase rule or remote command change separately.
- [ ] PR review before merge.
- [ ] Team status publish approval.

## Router Decision

Recommended workflow: `/implement-feature`

Next artifact: human QA using
`Docs_PowerMath/3_Outputs/TestPlans/shared-settings-panel-admin-tools-test-plan.md`.
