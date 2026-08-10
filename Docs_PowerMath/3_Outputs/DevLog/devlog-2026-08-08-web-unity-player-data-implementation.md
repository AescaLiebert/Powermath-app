---
slug: web-unity-player-data
status: needs-human
source: manual
gdd_tags:
  - server-authority
  - leaderboard-profile
  - guardrails
owner: implementation-agent
human_checkpoint: required
next_agent: code-review-agent
blocked_by:
  - human-e2e
---

# DevLog - Web-to-Unity Player Data Implementation

## Implemented

- Added `BootstrapScene` with loading UI, safe-area handling, session handoff, scene gate, and persistent player store.
- Added the WebGL ready/reauthentication bridge without passing account credentials into Unity.
- Added a configurable game API session-exchange client and schema-1 player snapshot DTOs.
- Added UI Toolkit Main Menu presenter/view bindings for identity, stage, Power Coins, weapon, and pet.
- Changed Build Settings to `BootstrapScene` index 0 and `MainMenuScene` index 1.
- Removed the deprecated authentication scene from the build entry flow without deleting its assets.
- Added an opt-in `UNITY_EDITOR` sample student that uses the production hydration and scene-flow path without contacting the web host or Firestore.
- Removed the Main Menu presenter/view lifecycle-order dependency by binding UI Toolkit elements lazily on first render.

## Verification

- Unity 6000.5.3f1 compilation completed with no project-owned warnings or errors.
- UnityMCP scene validation reported zero missing scripts or broken prefabs in both affected scenes.
- UnityMCP confirmed both UI Toolkit visual trees and the approved build order.
- No automated script tests were created or run, per project-owner direction.
- The Editor sample is enabled in `GameApiSettings.asset`; disable it when manually testing the real backend handoff in Play Mode.

## Human E2E Required

Configure `Assets/Project/Settings/GameApiSettings.asset`, connect the real web host/backend, and execute `Docs_PowerMath/3_Outputs/TestPlans/web-unity-player-data-e2e-test-plan.md`.
