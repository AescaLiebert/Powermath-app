---
slug: patch-1-22-announcement-board-and-biome-map-date
status: ready-for-review
gdd_tags:
  - announcement-board
  - stage-progression
  - localization
owner: implementation-agent
human_checkpoint: not-required
next_agent: code-review-agent
blocked_by: []
---

# DevLog: Patch 1.22 Announcement Board & Biome Map Date Update

**Date:** 2026-09-29  
**Status:** Implemented  

## Context

Release Patch 1.22 announcement titled **"AquaMarine Biome here!"** across the sign-in screen Announcement Board in both English and Thai. In addition, postpone the locked Biome Map (Patch 1.3) warning date string shown in the combat lobby from `27/09/26` to `31/09/26`.

## Changes

1. **Announcement Catalog (`Assets/Project/Resources/Announcements/Catalog.json`):**
   - Added featured Patch 1.22 update entry (`patch-1.22-update`) at the top of the patch list.
   - Set `catalogRevision` to `"2026.09.29.1"`.
   - Set Patch 1.2 `featured` to `false`.
   - Outlined 5 core update areas in both English and Thai:
     - **Adding Biome 6 "Aquamarine"**: New deep sea encounters, monsters, and bosses.
     - **Realigned Map**: Dynamic biome support and expanded 280-stage progression route.
     - **Balancing PC**: Recalibrated completion rewards, stage coins, and depth scaling.
     - **Improve Gacha Sequence**: Redesigned result grid, polished animation flow, and visual catalog integration.
     - **Bug Fixes**: Resolved combat startup silent failures, cooldown restoration clamps, actor presentation alpha latch, and localization polish.

2. **Biome Map (1.3) Warning Notification (`Assets/Project/Resources/Localization/UI.json`):**
   - Updated `menu.lockedFeatureUpdate`:
     - EN: `"Wait for 1.3 Update (31/09/26) sorry!"`
     - TH: `"รออัปเดต 1.3 (31/09/26) ขออภัยด้วยนะ!"`

3. **EditMode Tests:**
   - Synchronized `FeatureVersionLockTests.cs` to test the new `31/09/26` date strings.
   - Updated `AnnouncementBoardTests.cs` to query the latest active patch tab button.

## Verification

- Validated JSON syntax for both `Catalog.json` and `UI.json` with Node parser.
- Ran `git diff --check` to ensure no trailing whitespace or format issues.
