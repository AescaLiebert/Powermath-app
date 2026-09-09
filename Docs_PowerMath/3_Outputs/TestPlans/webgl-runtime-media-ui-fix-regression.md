# WebGL runtime media and UI regression plan

## Automated checks

- Run `node --test Tools/Tests/web-cache-reload.test.cjs Tools/Tests/youtube-question.test.cjs Tools/Tests/webgl-glyph-policy.test.cjs`.
- Confirm all six files under `Assets/StreamingAssets/Videos/` are byte-identical to their authored source MP4 files.
- Run the focused Unity EditMode suites for player lifecycle, combat lobby view, and status toast after Unity imports the new assets.

## Human WebGL checkpoint

Use a clean browser profile and test desktop plus one mobile-sized viewport.

1. Authenticate and verify the login background video plays forward and reverse without a missing-video icon.
2. Verify the opening video and character-selection video play. Enter the hub and verify both Ricko and Stellar hub presentations play or show stable fallback art if media loading is intentionally blocked.
3. Inspect authentication, bootstrap, combat, leaderboard, pet, player-hub, admin, and question UI. Confirm no tofu squares or missing-glyph artifacts appear.
4. Start a YouTube-backed combat question. Confirm the player fits the entire Unity canvas and no navy/blue layer covers it.
5. Let the video finish. Confirm the iframe disappears and the answer panel appears at the center without moving to a side rail.
6. Submit a correct answer and an incorrect answer. Confirm the matching sticker appears, result feedback completes, and only then the question surface exits.
7. Block YouTube autoplay or network access and retry. Confirm the attempt fails closed, the opaque overlay is removed, and the player can continue.
8. Rapidly start/cancel or transition away from a question. Confirm late iframe callbacks do not open or complete a newer question.

## Expected compatibility boundary

YouTube playback cannot be captured as a frozen last frame behind Unity UI. The compliant behavior is to hide the completed iframe and center the Unity answer/result surface. A real last-frame freeze requires project-owned direct question-video files.
