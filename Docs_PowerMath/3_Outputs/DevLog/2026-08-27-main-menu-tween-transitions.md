# DevLog: 2026-08-27 — Main Menu Tween Transitions

## Goal

Add a professional, presentation-only Main Menu bootstrap and panel-return sequence while preserving combat, session, focus, and transaction authority.

## What I Did

- [x] Added an original Math:World `BATTLE START` cover/title/accent layer.
- [x] Added scene-ready bootstrap orchestration for Canvas Player/Enemy art and UI Toolkit HUD groups.
- [x] Added accepted panel-close notification and return sequencing for Navigator, Gacha, Leaderboard, and Profile Analytics.
- [x] Added deterministic cancellation/final-state cleanup, input blocking, and reduced-motion behavior.
- [x] Generated and imported a transparent temporary non-canon Player cutout.
- [x] Wired Main Menu scene references and a data-driven settings asset.
- [x] Added panel-host and UXML binding regression coverage.

## Key Decisions

- Used the project's existing LeanTween integration for unscaled-time UI Toolkit and Canvas motion; no new dependency was added.
- Stage UI groups at full viewport offsets before animation so the Top HUD enters from above, Player Menu from the left, and Dashboard from below without flashing at rest.
- Kept `ForceCloseAll` silent so scene cleanup and terminal settlement cannot masquerade as a normal session return.
- Kept the existing combat runtime reduced-motion setting as the current Main Menu accessibility source.
- Made Player/Enemy/audio references optional and fail-soft so missing presentation content cannot strand input.
- Marked the generated Player as temporary and replaceable; it is not final/canon content.

## Bugs Found

- [x] Direct Main Menu entry without a session initially risked leaving the bootstrap cover active; unavailable paths now cancel and apply the stable final state.
- [x] Live state probing confirmed the return blocker clears and all entering classes are removed after completion.
- [x] Fixed a cold/repeated Play Mode lifecycle failure where LeanTween static state survived while its hidden Update driver did not; Main Menu now installs a scene-local driver only when no active driver exists.
- [x] Changed Top HUD, Player Menu, and Dashboard to share one simultaneous session tween.
- [ ] The Unity screenshot helper captured the active high-DPI Game View with a cropped/offset composition; human review should use the actual Game View or a fixed 1920×1080 capture profile.

## Game Feel Notes

- Starting bootstrap timing remains approximately two seconds and should be judged after repeated entries.
- The revised return sequence stages every group fully outside the viewport and moves all three UI groups simultaneously over the shared starting duration of 0.28 seconds.
- The Player and Enemy read clearly as opposing sides, but temporary Player size/position needs human visual approval.
- Audio hooks are implemented but clips are intentionally unassigned pending approved content.

## Verification

- Focused Main Menu panel/binding/transition tests: 11/11 passed.
- Direct off-screen/final-state/reduced-motion checks: 3/3 passed.
- Affected Combat Unity EditMode assembly: 15/15 passed.
- Unity console: no C# errors; unrelated LeanTween example obsolescence warning remains.
- Live bootstrap completed and restored all HUD/input final states.
- Live accepted Profile Analytics close entered and then cleared the expected transition states.

## Next Session

- Human game-feel review of timing and temporary Player presentation.
- Tune settings asset values from playtest notes.
- Assign approved audio clips and replace temporary Player art when canon content is ready.

---

## Git Commit Summary

```text
feat(ui): add Main Menu tween transitions

- sequence bootstrap title, scene art, and lobby HUD reveals
- animate safe session return after reference-panel closes
- add reduced-motion, cancellation, tests, and temporary Player art
```
