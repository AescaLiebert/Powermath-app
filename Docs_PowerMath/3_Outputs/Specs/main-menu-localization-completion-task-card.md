---
slug: main-menu-localization-completion
status: ready-for-review
source: manual
gdd_tags:
  - stage-progression
  - pet-system
  - gacha
  - player-experience
owner: implementation-agent
human_checkpoint: required
next_agent: code-review-agent
blocked_by: []
---

# Main Menu Localization Completion

## Player-Facing Goal

Stage Status, Player Hub, Rebirth, Pet Gacha, and weapon and pet names display in the player's selected English or Thai language.

## Source

Direct request on 2026-09-28.

## GDD Reference

- `@tag:player-experience` calls for clear stage, weapon, pet, and reset feedback.
- `@tag:stage-progression` covers visible stage progression.
- `@tag:pet-system` and `@tag:gacha` cover collection and summon presentation.

## Type

Bug fix.

## Acceptance Criteria

- [x] Stage number and listed panel copy use the selected locale.
- [x] All authored pet and weapon display names have Thai values with English fallback.
- [x] Gacha collection, inspection, reveal, and Hub pet preview use localized names.
- [x] Static UXML labels and tooltips have localization keys.
- [ ] Verify both languages visually in the Unity Editor and run EditMode tests through the Unity CLI.

## Human Checkpoint

Review Thai wording and the implementation before merge. No merge or publication was performed.
