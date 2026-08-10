---
slug: web-unity-player-data
status: approved
source: manual
gdd_tags:
  - server-authority
  - player-experience
  - guardrails
owner: code-review-agent
human_checkpoint: required
next_agent: human-tester
blocked_by: []
---

# Review: Web-to-Unity Player Data Implementation

## Summary

Approved for human E2E. The implementation follows ADR-001: the web/backend owns authentication, Unity accepts only an opaque launch code, authoritative state enters a persistent store, and `BootstrapScene` gates `MainMenuScene`. No project-owned compile error or warning remains.

## Issues Found

No blocker or warning was found in the implementation scope.

The Unity Console still reports one pre-existing LeanTween example deprecation warning, and UnityMCP emitted one transport warning during its own domain reload. Neither originates from the PowerMath implementation.

## Review Checklist

- [x] **Architecture** - Follows ADR-001 and the approved scene gate.
- [x] **Naming** - C# and asset identifiers follow project conventions.
- [x] **Performance** - No `Update()` polling; references are cached; allocations occur only on state transitions/network calls.
- [x] **Hierarchy** - `BootstrapScene` follows separator ordering.
- [x] **Optimization** - UI and session changes are event/coroutine driven.
- [x] **Events** - Session store and handoff changes are event based.
- [x] **God-object check** - Transport, storage, bootstrap, scene flow, view, and presenter responsibilities are separated.
- [x] **Data-driven** - API and scene configuration live in `GameApiSettings`.
- [x] **Platform** - WebGL handoff uses a `.jslib` host event bridge.
- [x] **Edge cases** - Empty/reused/expired codes, network errors, invalid schema/data, duplicate loads, and direct Main Menu entry are guarded.
- [x] **Security** - No password, launch code, access token, or player snapshot is logged or persisted locally.

## Human Gate

Execute `Docs_PowerMath/3_Outputs/TestPlans/web-unity-player-data-e2e-test-plan.md` against the real web/backend environment before merge.
