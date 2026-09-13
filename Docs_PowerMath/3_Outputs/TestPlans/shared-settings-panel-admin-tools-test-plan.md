# Shared Settings Panel and Admin Tools — Human QA

## Authentication

1. Open Settings with pointer and keyboard; verify General and Sound only.
2. Change TH/EN in General; verify Settings and the underlying Authentication screen update immediately and the choice persists after refresh.
3. Use Account Logout from Authentication after a remembered login; verify saved credentials clear and refresh remains on Authentication.
4. Change Music/SFX, switch scenes, return, and confirm the values remain applied.
5. Toggle fullscreen in WebGL; verify enter, exit, and blocked-request correction.

## Main Menu

1. Verify the panel geometry/content matches Authentication and closes back to the gear.
2. Use Account Logout; verify runtime/remembered credentials, player session, and test overrides clear, then verify refresh remains on Authentication.
3. Sign in as an account whose Firebase `userdata.admin` is absent/false; verify Admin is absent.
4. Set `userdata.admin` to true manually for a test account, sign in again, and verify Admin appears.
5. Verify a display-name spoof and malformed player ID do not receive Admin.

## Admin Safety and Function

1. Apply ATK/CR/CD and invincibility; verify `gamedata.adminTuning` is written,
   the reloaded run stays on the same saved Stage, damage changes, and enemy attacks do not reduce hearts.
2. Restore HP; verify Firebase and the reloaded run show maximum hearts without changing Stage.
3. Change Rank; verify the selected Rank applies and partial audit score/count clear.
4. Verify there is no synthetic Quick Test/answer-zero control and all questions continue to use the Firebase catalog and normal persistence.
5. Set each Rank Currency and Power Coin; verify HUD/session and Firebase values.
6. Execute two different commands consecutively; verify both use the refreshed Firebase revision and neither creates a temporary session.
7. Cause a concurrent revision update and verify the command rejects instead of overwriting it.
8. Verify reset needs the second click within five seconds. On a disposable test account only, confirm fresh opening/player preparation, fresh `gamedata`, preserved login credentials, and removal of the old leaderboard entry. Reset is the only admin action expected to return to Stage 1.

## Regression

- Run EditMode suites for Combat Core, UI contract/access policy, player reset, audio/music, and lifecycle.
- Smoke authentication, logout, normal combat, boss/enemy music override, and Reduced Motion panel transitions.
