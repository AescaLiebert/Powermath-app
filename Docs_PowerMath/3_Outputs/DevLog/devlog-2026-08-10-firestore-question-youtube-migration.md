# DevLog — Firestore Player/Question Migration and Embedded YouTube

Date: 2026-08-10

## Outcome

- Removed recursive `JsonUtility` Firestore DTO deserialization and replaced it with a shallow JSON navigator.
- Migrated configured player paths to `competition/{level1|level2|level3}/{student}.gamedata` with no runtime `game1` dependency.
- Existing authenticated students receive an exact update-mask PATCH for missing default leaves; missing students are not registered.
- Added version-precondition PATCH and one reload/replan retry for initialization conflicts.
- Added independent live reads for `question/{silver|gold|diamond}` with `items: [{id, video_link, answer}]`.
- Changed question identity to numeric `QuestionId` plus composite `QuestionKey(Rank,id)` and deterministic per-Rank ordering.
- Added YouTube URL normalization and an embedded WebGL IFrame Player bridge. Playback end opens the answer window; player/content errors void the attempt.
- Added academic audit/FIFO snapshot import/export, player mapping, and resolved-attempt persistence to the student document.
- Kept deterministic questions and simulated presentation confined to Editor composition; production fails closed when live content is unavailable.

## Verification

- Academic Core, Infrastructure, Combat Core, Combat Unity WebGL, default Editor, and default player assemblies compile through isolated Roslyn checks.
- Academic EditMode test assembly compiles after migration.
- Firestore foundation harness: PASS (37 missing default leaves, escaped dynamic field path, no `game1`, valid deep JSON and PATCH JSON).
- Question contract harness: PASS (composite IDs, per-Rank uniqueness, numeric ordering, YouTube normalization).
- Academic persistence harness: PASS (audit/FIFO export-import round trip).
- No automated request used the live Firebase settings.
- Unity Test Runner could not be launched from the CLI because the project was already open in another Unity Editor instance. Full EditMode/PlayMode execution and WebGL browser playback remain the final local verification step.

## Follow-up

- Run the Unity EditMode and PlayMode suites after closing the other Editor instance.
- Perform the approved human-only Firebase E2E with a disposable existing student.
- Verify iframe sizing, mobile orientation, embed permissions, and autoplay behavior in a WebGL build.

## 2026-08-11 Live Firebase Bugfix

- Root cause: Editor combat composition always selected simulation, even when real authentication/sample mode was disabled.
- Split the question catalog onto independent Firebase project/database/API-key settings.
- Replaced fire-and-forget result saving with persistence-gated commit, answer-window, resolution, content-void, and presentation checkpoints.
- Expanded saved `activeRun` state to Stage, enemy HP/cooldown, hearts, phase, and committed attempt identity.
- Added stable completed-run rehydration and fail-closed handling for unfinished committed attempts.
- Added authoritative session projection updates after each acknowledged write.
