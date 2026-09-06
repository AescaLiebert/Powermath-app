# Main Menu and Combat Resilience Regression

GDD references: `@tag:core-loop`, `@tag:combat-attempt`, `@tag:question-data`, `@tag:stage-progression`, `@tag:feedback`, `@tag:player-experience`.

## Automated coverage

- `Render_ProjectsCombatSnapshotIntoPlayerFacingUi` verifies the action queue contains only future enemy actions and never reconstructs spent actions as transparent tokens.
- `MainMenuTransition_UsesViewportStagingAndSharedMotionDriver` verifies panel-close events are not connected to a session-return transition.
- `InvalidQuestionFallback_ReleasesBootstrapPresentation` verifies an invalid local question fallback uses the unavailable-state handler that restores final Main Menu visibility.

## Manual smoke test

1. Open and close World Map, Pet Gacha, Leaderboard, Profile Analytics, and Player Hub. Each panel must close directly without replaying Main Menu session-entry animation.
2. Collapse the Player Menu; confirm its reopen handle remains fully visible and clickable
   at the left viewport edge, then confirm one click restores the complete menu.
3. Commit attempts through an enemy cooldown. Each consumed leftmost action must exit once, survivors must reflow, and the consumed action must not return faded after result completion.
4. Defeat a normal enemy. The clear banner/audio, enemy death, reward motion, and action cancellation should begin together; the next encounter must appear only after the outgoing enemy is hidden.
5. Repeat at a biome boundary and confirm the accepted Stage changes once, the biome transition remains readable, and the new encounter is interactive only after presentation stability.
6. Supply empty or entirely invalid `items` arrays for the shared question catalog, then make the local fallback invalid. The Main Menu must show an unavailable message with the HUD and scene sprites visible instead of remaining behind the bootstrap cover.
7. Repeat steps 2-6 with Reduced Motion enabled.
8. During fallback practice, confirm the Firebase player revision and all academic/run
   fields remain unchanged; reload after repairing Question Firebase and confirm the
   canonical pending artifact resumes.

## Pass/fail tuning metric

- Pass: during ten consecutive lethal attempts, no consumed action reappears, no duplicate Stage advance occurs, and the next encounter begins after only the longest overlapping defeat feedback path rather than the sum of every path.
- If the clear still feels slow, measure the longest remaining actor or reward routine before changing authored timing values; do not delay authoritative resolution or saving to extend presentation.
