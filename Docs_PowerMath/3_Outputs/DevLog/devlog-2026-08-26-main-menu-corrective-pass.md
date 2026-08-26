# DevLog: 2026-08-26 — Main Menu Corrective Pass

## Goal

Correct the non-clickable Main Menu and replace the oversized placeholder composition with the annotated encounter-first HUD layout.

## Implemented

- Marked every full-screen UI Toolkit template container as pass-through so empty template space no longer intercepts pointer hits.
- Rebuilt the persistent HUD into the requested profile, encounter, three-icon navigator, collapsible Player Menu, attack control, and bottom dashboard zones.
- Added separate live Silver/Gold/Diamond Rank Currency values, Power Coin value, loadout labels, and projected current ATK.
- Added a cooldown action grid that consumes from the left and reserves the final slot for the enemy Attack icon.
- Removed the UI Toolkit biome background and enemy image. Runtime encounter sprites now update the existing `Canvas/bg` and `Canvas/monsterPrefab` `Image` GameObjects.
- Kept unsupported Power-up cards visibly unavailable instead of inventing items or effects.
- Preserved controller element names and existing Profile, Leaderboard, Navigator, Player Hub, Pet Gacha, Rebirth, combat, and modal-host behavior.
- Compacted Enemy Name and remaining HP into one overlaid HP strip, reducing the encounter panel's vertical footprint while retaining outlined, non-color-only text.

## Verification

- Unity compilation: no errors.
- Main Menu/Combat contract tests: 9/9 passed after the compact HP-strip update.
- Full EditMode: 87/88 passed. The only failure remains the pre-existing Pet Gacha boundary test (`rare` expected, `middle` returned).
- Game View review confirmed the Canvas background/enemy remain visible behind the transparent UI Toolkit HUD at 16:9.

## Human Checkpoint

- Project-owner visual and real-session click-through review is pending.
