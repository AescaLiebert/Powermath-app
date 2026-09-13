# DevLog: Shared Settings Panel and Admin Test Tools

**Date:** 2026-09-10  
**ADR:** ADR-017  
**Status:** Implemented; awaiting human scene/WebGL QA

## Outcome

- Authentication and Main Menu now instantiate the same `SettingsPanel.uxml` and `SettingsPanel.uss`.
- General exposes the actual fullscreen state and reports browser rejection.
- General also exposes TH/EN language selection and Account Logout. Logout clears remembered/runtime credentials, the hydrated player session, and admin test overrides before loading Authentication, so a later refresh cannot resume the old account.
- Sound applies and locally persists Music and SFX volume across scenes.
- Admin is fail-closed and available only when the stable `playerId` username is an exact normalized `test1`–`test10` match.
- Session combat overrides support ATK, CR, CD, and invincibility, then rebuild the run at a safe scene boundary.
- Quick Test uses synthetic answer-`0` questions, immediate presentation, a practice run ID, and isolated persistence.
- Rank, HP, and wallet commands refresh the Firestore document, require the expected revision/update time, and update the current session only after commit.
- Reset uses one complete-map update mask for the student's `gamedata`, restores opening/player-preparation defaults, preserves sibling `userdata`, and removes the old leaderboard projection.

The previous `AdminPanel` assets/controller remain unreferenced compatibility artifacts because they had pre-existing local edits and tests. No Firebase rules, build settings, dependencies, publishing, or remote account reset were performed.

## Verification

- Unity Roslyn compilation: Combat Core, UI Core, Assembly-CSharp, Assembly-CSharp-Editor, and Combat EditMode test assemblies compiled with zero errors.
- Shared UXML documents parse as XML and Unity generated/imported their `.meta` files.
- `git diff --check` found no whitespace errors in this feature's files (reported pre-existing unrelated asset whitespace only).
- Node WebGL bridge/media suite: 14/14 passed.
- Added UI contract/access-policy/reset-map tests and an invincibility engine test; execution in Unity Test Runner remains a human QA step because the project is open in the interactive Editor.
