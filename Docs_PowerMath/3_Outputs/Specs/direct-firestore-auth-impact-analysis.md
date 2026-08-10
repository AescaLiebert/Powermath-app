# Impact Analysis: Direct Firestore Prototype Authentication

## Goal

Replace the Cloudflare session boundary with direct Unity-to-Firestore REST reads while retaining Authentication, Bootstrap, Main Menu, and the Editor sample flow.

## Class Responsibility Table

| Concern | Before | After |
|---|---|---|
| Credential verification | Cloudflare Function HMAC | Unity compares Firestore string fields locally |
| Session restoration | Secure HttpOnly cookie | Runtime credentials or remembered `PlayerPrefs` credentials |
| Player bootstrap | `/api/v1/game/bootstrap` | `gamedata.game1` inside the matching grade document |
| Logout | Server session revocation | Clear local runtime/PlayerPrefs credentials |
| Firebase authorization | Service-account IAM | Anonymous Firestore Security Rules |
| Editor sample | `UNITY_EDITOR` mock services | Unchanged and still `UNITY_EDITOR` only |

## Blast Radius

- Replace `GameApiClient`, REST service adapters, response/session validation, and Firebase setup documentation.
- Authentication UXML changes from access code to an exactly six-digit password.
- Main Menu data binding and `PlayerSnapshot` remain unchanged.
- Build scene order remains Bootstrap, Authentication, Main Menu.
- Cloudflare source is removed because the team explicitly rejected operating it.

## Regression Boundaries

- Production builds cannot reference the sample username, password, or player snapshot.
- Invalid credentials remain a generic error; network/rules failures remain distinguishable.
- Bootstrap never opens Main Menu with missing `game1`; an empty `game1` uses safe presentation defaults.
- Remember unchecked does not write credentials to PlayerPrefs.
- Logout clears both runtime and remembered credentials.

## Player Experience Check

- **Clarity:** The student never chooses a grade; `level-1`, `level-2`, or `level-3` determines Grade 4, 5, or 6 automatically.
- **Response:** Sign-in keeps immediate busy, success, invalid-credential, network, and rules-denied feedback.
- **Motivation/Fit:** The matching PowerMath profile loads without exposing `game2`, `temp`, or leaderboard concepts in the UI.
- **Satisfaction:** No extra celebration is added to a utility login flow; successful loading transitions directly to Main Menu.
