---
slug: display-name-policy-word-filter
status: approved
source: manual
gdd_tags:
  - player-experience
  - core-loop
owner: Antigravity
human_checkpoint: required
next_agent: human
blocked_by: []
---

# Task Card: Kid-Friendly Display Name Policy System & Global Word Filter

## Player-Facing Goal

When a student creates or updates their character's display name (up to 20 characters) in the preparation/onboarding screen:
1. The system validates the name against strict kid-friendly appropriateness standards (blocking profanity, vulgarity, toxicity, sexual/nudity references, and contact/PII leaks).
2. If the name violates the policy, the operation is denied and an immediate global UI log message notification (via StatusMessageService & AppLog) alerts the player with a clear, child-friendly localized explanation (in Thai and English), preventing inappropriate names from ever reaching leaderboards or multiplayer records.

## Source

- Origin: Manual user request (2026-09-09)
- Requested by: User
- Requirement: 
  - Display Name: max 20 characters (string:20).
  - Major appropriateness issues: bad words / swear / toxic / nudity.
  - Industry research: Find global word filter projects standard in the industry.
  - Global UI log message: If display name is denied, notify via the global UI log system.

## GDD Reference

- @tag:core-loop — Student onboarding and preparation sequence before entering Lobby and Leaderboard.
- Safe Kid-Friendly Educational Game Guidelines (COPPA compliance, child safety).

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- PowerMath.Session (PlayerLifecyclePolicy, DisplayNamePolicy)
- PowerMath.PlayerLifecycle (PlayerPreparationPresenter, PlayerPreparationView)
- PowerMath.UI.Core (StatusMessageService, StatusToastOverlay)
- PowerMath.Diagnostics (AppLog)
- Assets/Project/Resources/Localization/UI.json
- Assets/Project/Tests/EditMode/Editor/PlayerLifecycleTests.cs
- Assets/Project/Tests/EditMode/Editor/DisplayNamePolicyTests.cs

### Files Modified / Created

- `Assets/Project/Script/Session/DisplayNamePolicy.cs` [NEW]
- `Assets/Project/Script/Session/PlayerLifecycleCommands.cs` [MODIFY]
- `Assets/Project/Script/PlayerLifecycle/PlayerPreparationPresenter.cs` [MODIFY]
- `Assets/Project/Resources/Localization/UI.json` [MODIFY]
- `Assets/Project/Tests/EditMode/Editor/DisplayNamePolicyTests.cs` [NEW]
- `Assets/Project/Tests/EditMode/Editor/PlayerLifecycleTests.cs` [MODIFY]
- `Docs_PowerMath/3_Outputs/DevLog/2026-09-09-display-name-policy-word-filter.md` [NEW]

### Out of Scope

- Real-time chat messaging filter (display name only).
- Paid SaaS moderation API subscriptions requiring monthly per-seat or per-request costs.

## Acceptance Criteria

- [x] Global Word Filter Industry Analysis documented with enterprise and open-source benchmarks.
- [x] Display name length strictly enforced up to 20 text elements (StringInfo Thai/Grapheme compatible).
- [x] Comprehensive kid-friendly filter blocking English and Thai profanity, toxicity, vulgarity, and sexual/nudity content.
- [x] Leetspeak / homoglyph and delimiter evasion detection (f.u.c.k, @ss, 5h1t, repeated letters).
- [x] Scunthorpe problem prevention (allowing harmless words containing innocent substrings like Pass, Classic, Assistant, Hero, Ricko, Stellar).
- [x] Global UI notification (StatusMessageService.ShowWarning / StatusMessageService.ShowError) triggered when a name is denied, accompanied by AppLog logging.
- [x] Localized player feedback for Thai (th) and English (en).
- [x] Comprehensive suite of EditMode automated unit tests verifying all acceptance criteria.

## Human Checkpoints

- [x] Design/game-feel approval
- [x] Architecture approval
- [ ] PR review before merge
- [ ] Team status publish approval

## Router Decision

Recommended workflow: /implement-feature
Status: Implementation completed and ready for review.
