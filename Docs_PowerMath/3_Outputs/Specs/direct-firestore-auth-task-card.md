# Task Card: Direct Firestore Prototype Authentication

**Status:** implemented; awaiting human E2E  
**Approved:** 2026-08-09 by project owner  
**ADR:** ADR-003

## Acceptance Criteria

- [x] Production Authentication searches `competition-2/level-1..3` for the `{normalizedUsername}` map.
- [x] Username is a non-empty string and password is exactly six ASCII digits.
- [x] Successful login derives Grade 4/5/6 from the containing level document.
- [x] Bootstrap maps `gamedata.game1` before Main Menu.
- [x] Remember This Device persists credentials locally; unchecked login remains memory-only.
- [x] Logout clears all local credentials.
- [x] Editor sample code and credentials exist only inside `UNITY_EDITOR` compilation guards.
- [x] Cloudflare Function runtime is no longer required.
- [ ] Human Editor and WebGL E2E passes.

## Out of Scope

- Secure authentication or authorization.
- Firebase Authentication, host Functions, rate limiting, session revocation, password recovery, or multiplayer authority.
- Firestore gameplay writes.
- `temp`, `game2`, and leaderboard behavior.
