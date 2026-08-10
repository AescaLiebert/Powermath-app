# Human E2E: Unity-Owned Custom Authentication

> **Superseded:** Use `direct-firestore-auth-e2e-test-plan.md`; this plan covers the removed host-Function flow.

No automated or scripted gameplay test was run for this change. A human should complete these checkpoints before removing the launch-code rollback files.

## Unity Editor Sample Account

1. Ensure `Use Editor Sample Student` is enabled in `GameApiSettings`.
2. Start Play Mode from `BootstrapScene`.
3. Confirm Bootstrap opens `AuthenticationScene` instead of bypassing login.
4. Submit an incorrect code and confirm the generic error appears and the code field clears.
5. Sign in with `sample-student` / `POWERMATH-EDITOR-2026`.
6. Confirm Bootstrap loads the sample snapshot and Main Menu displays the sample name, stage, wallet, and loadout.
7. Select Log Out and confirm the game returns to Authentication.

## Cloudflare/Firebase WebGL

1. Configure Cloudflare secrets, rate-limit binding, `_routes.json`, fail-closed quota behavior, Firebase IAM, and the documented Firestore schema.
2. Build WebGL and host the static build and `/api/*` Function on the same HTTPS origin.
3. Sign in with Remember This Device unchecked. Refresh Main Menu and confirm no new login; close the entire browser and confirm the session ends according to that browser's session-cookie behavior.
4. Sign in with Remember This Device checked. Close and reopen the browser and confirm the student is restored through Bootstrap.
5. Log out, refresh, and confirm the remembered session cannot restore.
6. Try an invalid username and an invalid code. Confirm both show the same message.
7. Exceed the starting login rate limit. Confirm HTTP 429 becomes a wait-and-retry message without a Firestore bypass.
8. Disconnect during login and Bootstrap. Confirm network errors differ from invalid credentials and Bootstrap Retry recovers.
9. Disable or expire a session document. Confirm Bootstrap routes to Authentication and clears the cookie.
10. Validate a player document with inventory and active-run data; confirm Main Menu receives the correct projection without accepting a client-supplied player ID.
11. Measure Worker CPU for a cold OAuth token creation and normal cached requests against the Workers Free CPU allowance.

## Player-Experience Checks

- New player: identify username, access code, Remember This Device, shared-device warning, and Sign In without explanation.
- Stress: press Sign In repeatedly and press Enter repeatedly; only one request should be active.
- Readability: an observer can distinguish signing in, loading progress, invalid credentials, offline recovery, and successful logout.
- Shared device: Remember This Device starts unchecked and logout removes future restoration.
