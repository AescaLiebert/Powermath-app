# DevLog: Question Panel Centering & Removed Battle Banners

- **Date:** 2026-09-10
- **Author:** Antigravity Pairing Assistant
- **Status:** Complete

## Summary

1. **Removed Combat Battle Banners**:
   - In `CombatFeedbackPlayer.cs`, removed the `_view.ShowBattleBanner(...)` calls:
     - `"STAGE {stage} CLEARED"` / `"FINAL ENEMY DEFEATED - RUN COMPLETE"` (stage cleared message).
     - `"ENEMY COUNTERATTACK - LOST 1 HEART"` / `"CHALLENGE FAILED - LOST 1 HEART"` ("enemy attack player" message).
   - Combat now directly flows into the enemy attack/defeat animations, SFX, and heart damage without displaying the disruptive overlay banner messages.

2. **Question Panel Centering Fix**:
   - Replaced unsupported `margin-left: auto;` in Yoga layout with the established negative margin centering standard (`left: 50%; right: auto; margin-left: -230px;`).
   - The question panel is now dead-centered on any screen resolution.

## Verification

- Automated tests: `Tools/Tests/youtube-question.test.cjs`, `webgl-glyph-policy.test.cjs`, `web-cache-reload.test.cjs` all passed (14/14).
