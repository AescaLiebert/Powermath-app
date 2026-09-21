---
slug: pet-gacha-animation-sequence
status: implemented
source: manual
gdd_tags:
  - gacha
  - feedback
owner: codex
human_checkpoint: required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Pet Gacha Animation Sequence

## Player-Facing Goal

Make Pet Gacha feel premium from panel entry through reward review without changing the resolved reward, currency cost, probability, pity, or persistence behavior.

## Approved Direction

The project owner requested this direction on 2026-09-21:

- Clicking Pet Gacha eases the Main Menu UI out, fades through black, then opens the panel. Panel copy fades and rises while featured pets ease in from the right.
- A committed pull uses a standard anticipation stage and communicates rarity as white `R`, purple `SR`, and yellow `SSR`.
- Every resolved pet receives an individual Genshin-inspired reveal: silhouette drops in, the pet is revealed, copy rises, and rarity stars land one by one.
- `Result` means the final summary containing every pull in one UI. The individual screen is named and treated as `Reveal`.

The approved v10 flat graphic art direction remains in force. No transaction or catalog architecture changes are required.

## Scope

- `PetGachaPanelController` presentation state machine and animation scheduling
- Pet Gacha UXML reveal/transition/result structure
- Pet Gacha USS motion and rarity language
- 1x and existing 10x command paths ending on the same result grid
- Reduced-motion equivalents and skip behavior

## Acceptance Criteria

- [x] Panel entry animates topbar, text, featured pet preview, and actions.
- [x] Main Menu exit, black handoff, and panel entrance use simple cubic ease-out LeanTween motion without back/punch overshoot.
- [x] Rewards remain resolved before the presentation begins; Skip cannot reroll or change them.
- [x] Anticipation follows the reference rhythm: vortex, rarity-colored comet flight, white burst, then reveal.
- [x] Transition tokens match pull count and use white `R`, purple `SR`, and yellow `SSR`.
- [x] Each pull uses the reference rhythm: oversized silhouette settles into a radial burst, a white flash reveals the pet, copy rises, and stars stagger in.
- [x] Entrance content defaults to alpha zero and ignores input until its scheduled entrance.
- [x] The individual Reveal has no `NEXT` button; the settled full-screen Reveal advances on tap/click.
- [x] Advancing clears the previous pet copy to alpha zero before assigning the next pet text.
- [x] Sequential reveals hard-hide the pet-info container between pulls, then prime it invisibly before its entrance to prevent frame leaks.
- [x] Hidden reveal copy preserves its flex allocation, and both pet images remain visibility-gated until the black silhouette is ready.
- [x] Rarity stars retain the source PNG's white rendering instead of rarity tinting.
- [x] `SKIP ALL` remains a separate control and advances directly to the final summary.
- [x] Both 1x and 10x show every resolved pull in the final grid.
- [x] Animation callbacks are invalidated between states to prevent stale input or visual updates.
- [x] Reduced motion preserves quantity, rarity, pet identity, and result information.
- [x] Runtime and EditMode test assemblies compile.

## Human Checkpoints

- [x] Design/game-feel direction supplied by the project owner on 2026-09-21
- [x] Existing pet-gacha architecture retained; no ADR required
- [ ] In-Editor visual timing review
- [ ] PR review before merge
