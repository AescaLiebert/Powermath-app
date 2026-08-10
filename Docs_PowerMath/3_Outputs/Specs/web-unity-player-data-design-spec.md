---
slug: web-unity-player-data
status: approved
source: manual
gdd_tags:
  - server-authority
  - player-experience
  - guardrails
owner: game-design-agent
human_checkpoint: required
next_agent: code-review-agent
blocked_by: []
---

# Web-to-Unity Player Data Design Spec

## Player Experience

The student authenticates on the web app and Unity immediately acknowledges that it is connecting. The loading screen explains whether Unity is checking the session, loading progress, opening the game, waiting for a new sign-in, recovering from a connection problem, or blocked by an incompatible build.

`MainMenuScene` is never shown with anonymous, cached, or partially loaded player data.

## Interaction Flow

```text
Web login success
-> BootstrapScene: connecting
-> Session exchange
-> Player snapshot validation
-> Persistent state hydration
-> MainMenuScene
```

Failure branches remain on the loading scene and expose either retry, return-to-login, or update-required guidance.

## Five-Component Evaluation

| Component | Implementation requirement |
|---|---|
| Clarity | Each bootstrap state uses distinct status and detail text. |
| Motivation | Successful loading restores the student's persistent progress and ownership. |
| Response | Duplicate launch codes and retry presses cannot create duplicate Main Menu loads. |
| Satisfaction | Loading visibly advances through semantic milestones before revealing the populated menu. |
| Fit | The loading UI uses the PowerMath title and gold-on-navy visual language. |

## Risks and Abuse Cases

- Empty, reused, expired, or modified launch codes never open the Main Menu.
- Unsupported schemas never partially hydrate player state.
- Directly opening `MainMenuScene` produces an unavailable state instead of fake identity data.
- Tokens and snapshots are not written to `PlayerPrefs` or logs.

## Human Validation

Use `Docs_PowerMath/3_Outputs/TestPlans/web-unity-player-data-e2e-test-plan.md`. Automated script tests are intentionally excluded per project-owner direction.
