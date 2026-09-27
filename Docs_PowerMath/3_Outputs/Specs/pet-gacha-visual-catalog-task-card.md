---
slug: pet-gacha-visual-catalog
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

# Task Card: Pet Gacha Visual Catalog and Pet Inspection

## Player-Facing Goal

Replace the text-first Pet Gacha details list with a scrollable, visual pet catalog. Players can inspect any pet in a Player Hub style preview panel.

## Approved Direction

The project owner requested this UI direction on 2026-09-26:

- Display pets in a scrollable grid with icon, rarity, name, and current individual pull chance.
- Separate the catalog into rarity groups ordered SSR (`★★★★★`), SR (`★★★★`), then R (`★★★`), with a star divider before each group.
- Remove the `OWNED` tag presentation.
- Open a pet preview popup from a catalog card, reusing the Player Hub preview design.
- Animate preview popup entrance and exit using the existing UI Toolkit motion style.

## Scope

- Pet Gacha details UXML and USS
- Catalog card construction and bindings in `PetGachaPanelController`
- Pet inspection popup presentation and motion

## Acceptance Criteria

- [x] The details panel displays catalog pets in a scrollable visual grid.
- [x] Catalog cards are grouped under star dividers in SSR, SR, then R order.
- [x] Each card shows pet icon, rarity stars, name, and its current individual pull chance.
- [x] No `OWNED` tag or owned/unowned color treatment appears in the catalog.
- [x] Selecting a pet opens a Player Hub style preview with artwork, rarity, name, stat, and passive description.
- [x] The details panel and preview popup have enter and exit animation with reduced-motion behavior.
- [x] Existing gacha pull, odds calculation, and persistence behavior are unchanged.
- [ ] In-Editor visual review.

## Human Checkpoints

- [x] Visual direction supplied directly by the project owner on 2026-09-26.
- [x] Architecture uses the existing Pet Gacha controller and Player Hub preview styles.
- [ ] In-Editor visual review.
- [ ] PR review before merge.
