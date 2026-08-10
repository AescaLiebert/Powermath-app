# ADR-003: Direct Firestore Prototype Authentication

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-09 |
| Author | Codex with project-owner approval |
| Supersedes | ADR-002 |

## Context

The team does not want to operate a host Function, custom backend, or Firebase Authentication. The project owner explicitly accepts the security limitations of a Unity WebGL client reading Firestore directly for the current prototype.

## Decision

Unity calls the public Cloud Firestore REST API directly. `AuthenticationScene` checks the three documents `competition-2/level-1`, `level-2`, and `level-3`, locates the dynamic `{normalizedUsername}` map, and compares its `userdata.username` and plaintext six-digit `userdata.password` locally. Bootstrap repeats the check and maps `gamedata.game1` into `PlayerSnapshot` before opening Main Menu. The containing level determines Grade 4, 5, or 6.

When Remember This Device is selected, Unity stores the username and plaintext password using `PlayerPrefs`; in WebGL this persists in browser IndexedDB. When it is not selected, credentials remain only in process memory. The Editor sample remains compiled exclusively under `UNITY_EDITOR`.

The Firebase API key is project identification, not authorization. These unauthenticated REST calls are authorized only by permissive Firestore Security Rules.

## Accepted Risks

- Anyone able to obtain or guess an account document ID can read its plaintext password.
- A six-digit password has only 1,000,000 possible values and there is no server-side rate limit.
- WebGL source, project ID, API key, collection names, requests, responses, and remembered credentials are inspectable by the browser user.
- Without trusted identity, Firestore Rules cannot enforce that the caller owns the requested dynamic student map.
- Remember This Device stores credentials without encryption and shared-device users can recover them.
- Audience size and technical skill do not prevent automated scanning or quota abuse.

These risks are intentionally accepted for the prototype and must not be presented as secure authentication.

## Prototype Schema

```text
competition-2/{level-1 | level-2 | level-3}
  {normalizedUsername}: map
    userdata: map
      username: string
      password: string   // exactly six ASCII digits
    gamedata: map
      game1: map         // PowerMath PlayerSnapshot data
      game2: map         // ignored
```

## Consequences

- Cloudflare Function code, cookies, HMAC, OAuth service credentials, CSRF state, and server sessions are removed.
- Bootstrap remains build index 0 and Main Menu still receives the same `PlayerSnapshot` projection.
- Logout clears runtime and remembered credentials locally.
- Each grade GET exposes every student map in that document; Firestore cannot return only the requested dynamic map field.
- `temp`, `game2`, leaderboards, and gameplay writes remain out of scope.

## Migration Trigger

Replace this ADR when the project handles meaningful purchases, personal data, competitive state, public discovery, or a larger audience, or when account abuse occurs.
