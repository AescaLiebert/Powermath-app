---
slug: auth-language-worldwide-and-global-logging
status: approved
source: manual
gdd_tags: [player-experience, core-loop]
owner: Antigravity
human_checkpoint: required
next_agent: implementation
blocked_by: []
---

# AuthenticationScene Worldwide Language Switcher & Global Structured Logging

## Request and Context

User request (2026-09-08):
1. Remove accidental language toolbar (TH / EN button) injected across all scenes; restrict language change access strictly to `AuthenticationScene`.
2. Add Worldwide button (`🌐`) in top right corner of `AuthenticationScene` with UI animation revealing `TH` / `EN` language selection buttons.
3. Implement a global industry-standard Log Message system (`PowerMath.Diagnostics.AppLog`) and eliminate all individual `Debug.Log*` calls across the codebase to exclusively use this global logging framework.

## Deliverables

- Task Card: `Docs_PowerMath/3_Outputs/Specs/auth-language-worldwide-and-global-logging-task-card.md`
- DevLog Entry: `Docs_PowerMath/3_Outputs/DevLog/2026-09-08-auth-language-worldwide-and-global-logging.md`
- Diagnostics Framework: `PowerMath.Diagnostics` assembly containing `AppLog`, `LogMessage`, `LogLevel`, `ILogSink`, `UnityConsoleSink`.
- UI & Scene updates: `AuthenticationScreen.uxml`, `AuthenticationScreen.uss`, `AuthenticationView.cs`, `PlayerLifecycleRuntime.cs`.
- Codebase refactor: Complete migration of all 26 files with raw `Debug.Log*` to `AppLog.*`.
- Automated test coverage: `AppLogTests.cs` and `AuthenticationLanguageUiTests.cs`.
