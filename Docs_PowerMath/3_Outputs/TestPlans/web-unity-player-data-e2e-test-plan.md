---
slug: web-unity-player-data
status: needs-human
source: manual
gdd_tags:
  - server-authority
  - player-experience
  - guardrails
owner: human-tester
human_checkpoint: required
next_agent: code-review-agent
blocked_by:
  - real-web-backend-endpoint
  - valid-one-time-launch-code
---

# Human E2E Test Plan: Web-to-Unity Player Data

## Setup

### Editor Sample Student

1. Select `Assets/Project/Settings/GameApiSettings.asset`.
2. Enable `Use Editor Sample Student`.
3. Enter Play Mode from `BootstrapScene`.
4. Confirm Main Menu shows `Developer Sample Student`, Stage 24, 1,200 Power Coins, `starter-sword`, and `starter-pet`.

The sample path exists only under `UNITY_EDITOR`, has no elevated permissions, and cannot authenticate or access Firestore.

### Real Web/Backend Integration

1. Select `Assets/Project/Settings/GameApiSettings.asset`.
2. Disable `Use Editor Sample Student` before testing the real handoff in Play Mode.
3. Configure `baseUrl` for the real web/game API.
4. Confirm the exchange endpoint returns schema version 1 and the documented session/player payload.
5. For WebGL, listen for `powermath-unity-ready` and call:

```javascript
unityInstance.SendMessage("Bootstrap", "ReceiveLaunchCode", launchCode);
```

6. Start from `BootstrapScene`, not `MainMenuScene`.

## Success Flow

- [ ] Web login succeeds without passing username/password to Unity.
- [ ] `BootstrapScene` shows connecting, session, progress, and opening states.
- [ ] A valid launch code opens `MainMenuScene` exactly once.
- [ ] Display name matches the authenticated student.
- [ ] Stage and stage progress match server data.
- [ ] Power Coins and equipped weapon/pet match server data.
- [ ] Refreshing the page creates a fresh launch-code flow and restores the same authoritative state.

## Failure and Recovery

- [ ] Empty launch code leaves Unity in `BootstrapScene`.
- [ ] Expired/reused launch code requests web reauthentication.
- [ ] Offline or unreachable API shows retry feedback without opening Main Menu.
- [ ] Malformed or missing player fields do not partially render a player.
- [ ] Unsupported `schemaVersion` shows update-required feedback.
- [ ] Repeated host callbacks and retry presses never load multiple menus.
- [ ] Directly playing `MainMenuScene` does not show a believable fake student identity.

## Privacy and Abuse

- [ ] Browser/Unity logs contain no password, launch code, access token, or full player snapshot.
- [ ] Modified player IDs, revisions, wallet values, or ownership payloads are rejected by the backend.
- [ ] Public profile responses omit private educational analytics.

## Result

- Tester:
- Build/environment:
- Pass/fail:
- Notes/screenshots:
