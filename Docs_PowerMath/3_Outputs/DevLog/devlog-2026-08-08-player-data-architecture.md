# DevLog — Web-to-Unity Player Data Architecture

| Field | Value |
|---|---|
| Date | 2026-08-08 |
| Status | Architecture approved; implementation not started |
| GDD references | `@tag:stage-progression`, `@tag:economy`, `@tag:leaderboard-profile`, `@tag:server-authority`, `@tag:player-experience`, `@tag:guardrails` |

## Work completed

- Inspected current authentication/Main Menu scenes, UXML, Build Settings, packages, and relevant documentation.
- Documented modular web handoff, Unity bootstrap, player state, save command, and UI Toolkit architecture.
- Drafted ADR-001 for the web/Unity trust boundary and server-owned Firestore access.
- Recorded migration phases, error states, privacy rules, and validation scenarios.

## Current-state findings

- `AutheticationScene` is the only enabled build scene and is deprecated by the new product direction.
- `MainMenuScene` is not enabled in Build Settings.
- `MainMenuUI.uxml` contains hard-coded prototype identity/stage values and has no data presenter.
- No authentication, Firestore, inventory, progress, or save service exists under `Assets/Project` yet.
- The worktree already contained user changes; no existing Unity asset or GDD content was modified in this session.

## Checkpoint

Architecture was approved with one explicit addition: `BootstrapScene` must exist as the loading scene and must report successful session exchange and player-state hydration before `MainMenuScene` loads. Build Settings, dependencies, security integration, and deletion of deprecated assets retain their separate human checkpoints.
