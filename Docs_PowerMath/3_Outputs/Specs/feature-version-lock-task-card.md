---
slug: feature-version-lock
status: ready-for-review
source: manual
gdd_tags:
  - core-loop
  - economy
  - gacha
  - server-authority
  - player-experience
owner: Antigravity
human_checkpoint: required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: Version 1.0 Feature Locking (Biome Map, PlayerHub, Pet Gacha)

## Player-Facing Goal

Present a clean, unmistakable "Chain Lock" visual on the **Biome Map**, **Player Hub**, and **Pet Gacha** lobby buttons, disabling click interactions during the initial Version 1.0 release while preserving the active, unchained Rebirth button and core combat progression.

## Source

- Origin: Manual prompt on 2026-09-08.
- Requested by: Project owner.

## Target Slice

### Systems Affected

- `GameVersionManifest` & `GameVersionChecker`: Version-gated feature flags (`biomeMap`, `playerHub`, `petGacha`) defaulting to false for Version 1.0.
- `version.json`: Deployment manifest explicitly declaring feature flags.
- `PlayerSessionStore`: Exposing active `GameVersionManifest` for lobby UI controllers.
- `ChainLockVectorElement`: Scale-independent vector element drawing crossed chains and a central padlock with keyhole via `Painter2D`.
- `MainMenuShell.uxml` & `PlayerMenuPanel.uxml`: Declarative chain lock overlay elements.
- `MainMenuExperience.uss` & `PlayerMenuPanel.uss`: Locked styling, disabled pointer behavior, and muted icon presentations.
- `CombatLobbyView`: Biome Map button disabling, locked tooltip, and click-guarding.
- `PlayerHubPanelController` & `PlayerHubView`: Player Hub button disabling, locked tooltip, and click-guarding.
- `PetGachaPanelController`: Pet Gacha button disabling, locked tooltip, and click-guarding.

## Non-Goals

- Do not lock or alter `rebirth-button` (Rebirth is active in v1.0).
- Do not alter `leaderboard` or `setting` buttons.
- Do not introduce binary texture assets if vector/UI Toolkit scale-independent rendering satisfies the visual requirement.
