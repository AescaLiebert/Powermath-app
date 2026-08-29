# DevLog — 2026-08-27 — Question Sequence Visual Flow

## Goal

Keep the ended YouTube question visible through answering and replace the template result sentence with an ordered, mobile-game-style score tally before battle feedback begins.

## Implemented

- Added an explicit retained-presentation flag and dismiss lifecycle to `IQuestionPresentation`.
- Changed the WebGL YouTube bridge to dock the ended iframe at the left, disable its pointer interception, and keep it visible until the presenter dismisses it.
- Split answer feedback from battle feedback so the video closes between those phases.
- Added an immutable authoritative damage breakdown: Base ATK, Rank, Buff, Critical, Response Score multiplier, and Final Damage.
- Added correct, incorrect, and timeout visual feedback cards with sequential tally-row animation and reduced-motion timing.
- Added a separate battle banner so the question panel can close before damage, defeat/Stage, and enemy-action feedback.
- Added Mermaid flow/state documentation and a dedicated regression plan.

## Verification

- Changed-script validation: no diagnostics on presentation, view, feedback, and model files; the validator reported a known false-positive duplicate `CreateSnapshot` signature on two existing engine classes, while Unity compilation succeeded.
- Targeted EditMode suites: 45 passed, 0 failed.
- Runtime UI preview staged in `MainMenuScene` and captured at 2048×1536; score hierarchy, connectors, final-damage emphasis, and retained-video companion layout were visually readable.
- PlayMode suite: 0/4 completed because the legacy fixture enters live Firebase with an invalid fake player identity; recorded in the regression plan rather than changing environment policy.

## Game-Design Evaluation

- Clarity: outcome and formula order are visible without relying on animation or color alone.
- Response: retained video becomes non-interactive, leaving the Unity numpad as the sole active input surface.
- Satisfaction: correct answers build toward a strongly emphasized final damage value; failures use a concise zero-damage popup.
- Fit: math performance visibly converts into combat power before the combat impact occurs.

## Human Follow-Up

- Validate the real WebGL iframe at 16:9, 4:3, and the narrow supported mobile landscape viewport.
- Tune the starting 0.25-second header, 0.16-second row, and 0.75-second failure holds only after observer readability testing.

