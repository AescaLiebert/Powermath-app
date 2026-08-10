# DevLog — Direct Firestore prototype authentication

Date: 2026-08-09

Replaced the Cloudflare Function/HMAC/cookie architecture with an intentionally low-security direct Firestore REST prototype, per team direction and ADR-003.

- Authentication searches `competition-2/level-1..3`, extracts the dynamic username map, and compares `userdata` strings in Unity.
- Bootstrap revalidates credentials and maps `gamedata.game1`; the containing document derives Grade 4, 5, or 6.
- Remember-device uses PlayerPrefs/browser IndexedDB; unchecked credentials stay in runtime memory only.
- The Editor-only sample remains compiled behind `UNITY_EDITOR` and uses `sample-student` / `123456`.
- Removed host Function and WebGL launch-code bridge code.
- Added intentionally public read-only Firestore rules and a console/schema setup guide.
- Kept all Firestore writes denied; save commands are out of scope.

Unity's Roslyn compilation completed with no PowerMath errors. Human E2E remains required using the direct Firestore test plan; Unity MCP was unavailable.
