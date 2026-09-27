---
slug: pet-gacha-result-grid-redesign
status: approved
source: manual
gdd_tags:
  - gacha
  - feedback
owner: codex
human_checkpoint: required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Pet Gacha Full-Screen Result Grid

## Player-Facing Goal

Make the final Pet Gacha result easy to review at a glance with an elegant full-screen summary for both individual and 10x pulls.

## Approved Direction

The project owner requested this direction on 2026-09-26:

- Use the same resolved-result presentation for individual and 10x pulls.
- Use a full-screen, non-scrollable grid that fits all result tiles inside the available result viewport.
- Enlarge and center the result grid.
- Show the rarity as stars on each tile.
- Give SR and SSR stronger premium borders and glow.
- Remove the Power Coin balance from the result summary.
- Keep the existing `NEW!` and `DUPE` tags.
- Fade cards in with ease-out timing, in pull order.

## Scope

- Pet Gacha result-screen UXML and USS
- Result-card icon presentation and ordered entrance timing in `PetGachaPanelController`

## Acceptance Criteria

- [x] A single pull and a 10x pull use the same full-screen result summary.
- [x] Result tiles use the Player Hub inventory's square pet-tile layout and resize to fit the result viewport without scrolling.
- [x] The larger grid is centered and wraps for narrower screens.
- [x] SR and SSR cards have distinct colored borders and rarity glow.
- [x] The result summary omits the Power Coin balance.
- [x] Each card shows the pet icon, rarity stars, name, and existing `NEW!`/`DUPE` tag.
- [x] Cards fade and ease into place in receipt order.
- [x] Continue and close input stay locked until the final card entrance settles.
- [x] Reduced motion presents every result immediately with the same information.
- [x] Result identity, order, `NEW!`/`DUPE` outcome labels, and transaction behavior are unchanged.
- [ ] In-Editor visual review.

## Human Checkpoints

- [x] Visual direction supplied directly by the project owner on 2026-09-26.
- [x] Existing gacha receipt and result flow retained.
- [ ] In-Editor visual review.
- [ ] PR review before merge.
