# DevLog: 2026-08-27 — UI Mockup Experience Slice 5

## Goal

Migrate Leaderboard and Profile Analytics from compatibility panels into the approved Math:World experience while preserving ranking, cohort isolation, manual refresh, profile privacy, display-name persistence, and cooldown authority.

## What I Did

- [x] Added focused `LeaderboardPanel.uxml/.uss` and `ProfileAnalyticsPanel.uxml/.uss` assets.
- [x] Preserved every existing controller-facing UI element name and native control type.
- [x] Reframed Leaderboard around the locked cohort, ranking explanation, first-place summary, pinned self standing, and structured public rows.
- [x] Added Avatar to the existing Pet/Weapon loadout presentation so each row matches the approved public entry fields.
- [x] Reframed Profile Analytics into private Adventure, Economy/Loadout, Learning, and By Rank/Question groups using only persisted metrics.
- [x] Added an explicit public-display-name area with authoritative cooldown copy.
- [x] Introduced one scene-scoped `MainMenuPanelHostProvider` shared by combat, economy, Leaderboard, and Profile Analytics panels.
- [x] Added semantic loading/cache/error states for Leaderboard and busy/success/error states for rename.
- [x] Prevented close and competing-panel input while a rename mutation is active.
- [x] Extended UI contracts for privacy, unsupported mockup metrics, focusability, and cohort-lock structure.

## Authority Preserved

- `LeaderboardRanking`, cohort resolution, weighted score, Firestore mapping, manual refresh, and cached standings behavior remain unchanged.
- `DisplayNamePolicy`, server-time cooldown verification, optimistic concurrency, session update, and leaderboard projection remain unchanged.
- Login credentials, online status, hidden audit score/count/thresholds, mock Mastery/Streak/Focus Next, and grade selection remain absent.
- No new ranking, analytics, cooldown, or economy value was introduced.

## Verification

- Focused UI and panel-host EditMode contracts: 8/8 passed.
- Full EditMode run: 88/89 passed; the only failure is the existing Pet Gacha category-boundary expectation (`rare` expected, `middle` returned), outside Slice 5.
- Unity compilation/import: no source errors.
- Active scene remained `AuthenticationScene`; its unsaved project-owner changes were preserved.

## Game Feel Notes

Clarity and Response were prioritized. The leaderboard explains its ranking key and locked cohort before standings, keeps stale data visible during manual refresh, and treats the player's own row as a stable landmark. Profile Analytics separates private learning evidence from the one public mutation. Read-only refresh can be cancelled on close; a committed rename locks conflicting input until the server outcome is known.

## Next Session

- Complete project-owner visual review for Slice 5.
- Validate populated/empty/cache-error/long-name and dense-analytics states with a real session.
- After approval and authorization, run Slice 6 compatibility cleanup, viewport/audio polish, and final regression evidence.

---

## Git Commit Summary

```text
feat(ui): migrate leaderboard and profile analytics

- split social and private analytics into focused templates
- share modal arbitration across all main-menu panels
- preserve ranking, privacy, refresh, and rename authority
```
