---
slug: patch-1-22-announcement-board
status: approved
source: manual
gdd_tags:
  - announcement-board
  - stage-progression
  - localization
owner: game-design-agent
human_checkpoint: not-required
next_agent: qa-agent
blocked_by: []
---

# Task Card: Patch 1.22 Announcement Board & Biome Map Date Update

## Goal

Publish the Patch 1.22 in-game announcement ("AquaMarine Biome here!") to the Announcement Board catalog in both English and Thai, and postpone the locked Biome Map (1.3 update) warning notification date from `27/09/26` to `31/09/26`.

## Scope

1. **Announcement Board Catalog (`Assets/Project/Resources/Announcements/Catalog.json`):**
   - Add new featured Patch 1.22 entry with title "AquaMarine Biome here!" / "ไบโอม AquaMarine มาแล้ว!".
   - Content covers:
     - Adding Biome 6 "Aquamarine" (deep sea zone, aquatic monsters, deep sea bosses).
     - Realigned Stage Map (dynamic biome count architecture, endgame stage route).
     - Balancing Power Coins (PC run rewards, flat stage coins, depth bonuses).
     - Improved Gacha Sequence (redesigned result grid, polished animation, visual catalog).
     - Bug Fixes (combat startup recovery, actor presentation alpha latch, localization completion).
   - Bump `catalogRevision` to `"2026.09.29.1"`.
   - Update previous Patch 1.2 `featured` flag to `false`.

2. **Biome Map (1.3) Warning Date (`Assets/Project/Resources/Localization/UI.json`):**
   - Update `menu.lockedFeatureUpdate` from `27/09/26` to `31/09/26` in both English and Thai.

3. **Tests (`Assets/Project/Tests/EditMode/Editor/`):**
   - Synchronize `FeatureVersionLockTests.cs` to assert `31/09/26`.
   - Ensure `AnnouncementBoardTests.cs` queries the new active patch tab.

## Checkpoints
- [x] Catalog.json updated with Patch 1.22 in EN and TH
- [x] UI.json updated with 31/09/26 date
- [x] EditMode tests updated
