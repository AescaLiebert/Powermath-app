---
slug: leaderboard-profile-analytics
status: implementation-ready-for-e2e
source: manual
gdd_tags:
  - leaderboard-profile
  - server-authority
  - player-experience
  - guardrails
owner: game-design-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Task Card: Grade-Filtered Leaderboard and Profile Analytics

## Player-Facing Goal

A signed-in Grade 4, 5, or 6 student can open a polished leaderboard from `MainMenuScene`, compare progress only with peers in the same grade/level cohort, understand exactly why players are ranked, and always find their own standing. The same student can expand `ProfilePanel` into a private analytics dashboard that presents progression, ownership, play history, and educational performance without leaking private analytics to other players.

## Source

- Origin: Manual Codex goal submitted on 2026-08-11.
- Visual references: four supplied Cookie Run-style leaderboard and parchment-profile images.
- Requested leaderboard: cohort-only query, open/manual refresh, weighted sort, top-three VIP treatment, pinned self row, first-place throne, avatar/pet/weapon presentation.
- Requested profile: GDD-aligned owner-only analytics opened from `ProfilePanel` with clear charts and polished graphics.
- Validation direction: project owner will run human E2E; this slice must not create automated test scripts.

## GDD References

- `@tag:leaderboard-profile`: fun leaderboard, public-safe progression/loadout data, private educational analytics, and Challenger League separation.
- `@tag:server-authority`: every saved state change is authoritative and idempotent.
- `@tag:player-experience`: response and clarity take priority over spectacle.
- `@tag:guardrails`: private educational analytics stay out of public profiles.

## Current Implementation

- Firestore uses `competition/level1`, `competition/level2`, and `competition/level3`, corresponding to Grade 4, Grade 5, and Grade 6.
- Authentication already derives the student's grade cohort from the level document containing the account; no student-selectable grade is required.
- `PlayerSnapshot` currently exposes basic profile, progression, wallet, inventory, loadout, active-run, and partial academic state.
- Most GDD analytics are not currently persisted: totals by result/question/Rank, response-duration and efficiency distributions, aggregate damage, Rank-change history, cycle history, registration date, and play time.
- `MainMenuUI.uxml` already contains a `leaderboard` button and `profile-panel`, but neither has this feature's modal behavior or data presentation.
- The accepted direct-Firestore prototype stores credentials, private state, and public-facing game state together inside shared grade documents. A leaderboard must not bind or log raw peer student maps.
- An earlier approved academic spec requires audit score/count to remain absent from normal student UI.

## Target Scope

### Leaderboard

- Open/close modal behavior in safe Main Menu states.
- Cohort lock: Level 1 only sees Grade 4, Level 2 only Grade 5, Level 3 only Grade 6.
- Public-safe leaderboard projection and deterministic rank semantics.
- Sort by historical Highest Stage, then weighted `Silver × 5 + Gold × 7 + Diamond × 10`; shared ranks for exact score ties.
- Pinned `YOUR STANDING` row, green `YOU` treatment, full scroll-list self highlight, top-three VIP banners, and first-place throne showcase.
- Public row data: rank, display name, avatar, current and best Stage, three Rank Currency balances, total damage, equipped pet, and equipped weapon.
- Fetch-on-open and explicit manual refresh only, with stale-cache/error/empty states and update acknowledgement; no polling or automatic while-open refresh.
- Responsive WebGL/mobile layout and non-color-only rank/self identification.

### Private Profile Analytics

- Expand `ProfilePanel` into a private owner-only modal/dashboard.
- Allow the owner to edit `displayName` immediately when eligible, then enforce a server-authoritative seven-day cooldown after every accepted change.
- Identity, cohort, leaderboard standing, current/best Stage, Prestige/Honor, first Stage 200 milestone, total damage, currencies, Power Coins, and equipped loadout.
- Questions cleared and correct/non-correct outcome counts overall, by Rank, and by question.
- Mean/median response score, response duration, and approved Response Efficiency (`correct ? responseScore × 10 : 0`).
- Rank-change and question-cycle history without exposing hidden live audit score/count.
- Registration date and total play time visible only to the owner and authorized education views.
- Overview, progress, and learning visualizations with supportive language and privacy-safe empty states.

### Data and Authority

- Separate public leaderboard data from credentials and private educational analytics.
- Update aggregates and public ranking inputs from the same accepted attempt/run result, idempotently.
- Never let the client choose another grade cohort or submit an authoritative rank value.
- Resolve avatar/pet/weapon IDs through the content catalog; never use raw IDs as final player-facing labels.

## Out of Scope

- Challenger League and its separate pure-mathematics leaderboard.
- Viewing another student's private educational analytics.
- Friend, guild, global, seasonal, or cross-grade leaderboards.
- Leaderboard rewards, rank decay, manual moderation tooling, or chat/social messaging.
- Teacher/administrator analytics UI.
- Architecture, implementation, Firestore rule publication, dependencies, build settings, deployment, automated tests, or E2E execution in this design slice.

## Approved Human Decisions

1. Grade 4 / Grade 5 / Grade 6 map to Level 1 / Level 2 / Level 3 and each student is locked to their authenticated cohort.
2. Historical `highestStage` is the primary ranking key; rows show both Current and Best Stage.
3. Weighted Rank Currency is the secondary key. Silver ×5, Gold ×7, and Diamond ×10 reuse the GDD's established `0.5 / 0.7 / 1.0` relative values without decimals; Power Coins are excluded.
4. Raw/current audit score and position stay hidden from the student ProfilePanel; mean/median audit analytics remain reserved for a future authorized education view.
5. Public identity uses mutable `displayName`, never the login username or credentials. The first accepted change is immediately available, then each accepted change starts a seven-day server-authoritative cooldown.
6. The supplied visual direction, responsive first-place throne, and restrained motion/audio are approved.
7. The leaderboard fetches on open and on explicit manual Refresh only; no polling or automatic refresh runs while the modal remains open.

Approved by the project owner on 2026-08-11 (`LGTM`) with weighted currency, seven-day Display Name editing, and manual-only refresh amendments.

## Human Checkpoints

- [x] Approve the amended player-facing design and companion design spec. Approved on 2026-08-11.
- [x] Approve the architecture plan, ADR-007, Firestore schema/rules direction, and Response Efficiency formula. Approved on 2026-08-11 (`LGTM`).
- [ ] Manually publish any approved Firestore rules/indexes; automation must not publish them.
- [ ] Project owner performs human E2E; no automated test scripts are requested.
- [ ] Review the implementation PR before merge.
