# Firebase Split Backend and Persistence Regression

## Automated / Offline

1. Compile Academic Core/Infrastructure, Combat Core/Unity, Editor default, and WebGL player assemblies.
2. Verify a committed save request contains the locked question, reserved FIFO entry, and decremented cooldown.
3. Verify combat restore preserves Stage, enemy HP/cooldown, and player hearts.
4. Verify same numeric question IDs remain valid across different Rank documents.
5. Do not load the serialized live API keys in automated network tests.

## Human Live E2E

1. In `GameApiSettings`, set player Firebase fields to the competition project.
2. Set Question Project ID, Question Database ID, and Question API Key to the separate content project.
3. Disable `Use Editor Sample Student` and authenticate an existing disposable student.
4. Confirm missing fields are created under direct `gamedata`.
5. Press Attack; confirm `activeRun`/`activeAttempt`, reserved question, and cooldown write before content begins.
6. Resolve one correct answer; confirm currency, audit, FIFO, enemy HP, Stage, and revision update before feedback continues.
7. Stop and restart Play Mode; confirm the same completed state rehydrates.
8. Repeat with incorrect answer, timeout, and content failure.
9. Disconnect during each save checkpoint; confirm input stays locked and a save error is shown.

## Question Firebase Fallback E2E

1. Keep the player Firebase project valid and disable `Use Editor Sample Student`.
2. Clear or invalidate only the Question Firebase configuration, then authenticate an
   existing disposable student.
3. Confirm the combat lobby remains playable and shows the development-question/
   Firebase-save notice instead of the unavailable screen.
4. Press Attack; confirm the simulated presentation exposes its QA target and the
   numpad/timer/answer sequence resolves normally.
5. Confirm every acknowledged attempt checkpoint increments the real player document
   revision and persists combat, audit, Rank currency, analytics, and active-run state.
6. Stop and restart Play Mode with Question Firebase still unavailable; confirm the
   saved Stage, HP, cooldown, hearts, Rank, currency, audit, and FIFO state rehydrate.
7. Die during fallback play; confirm death settlement writes once and reloads Stage 1.
8. Restore Question Firebase and confirm its canonical catalog loads instead of the
   fallback. Use a disposable account because fallback question IDs are prototype data.

## Expected Limitation

Stopping during an unacknowledged request cannot guarantee a client write. Once a checkpoint is acknowledged and the UI advances, restart must reproduce that saved state.
