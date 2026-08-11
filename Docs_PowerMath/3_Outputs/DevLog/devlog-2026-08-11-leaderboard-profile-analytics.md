# DevLog - Grade Leaderboard and Profile Analytics

Date: 2026-08-11  
GDD: `@tag:leaderboard-profile`, `@tag:server-authority`, `@tag:player-experience`, `@tag:guardrails`

Implemented the approved direct-Firestore prototype slice for grade-filtered standings and the private student profile dashboard.

- Added a sanitized `leaderboard-public/{level1|level2|level3}` projection keyed by opaque public player IDs. Existing authenticated `level-1`/`level-2`/`level-3` settings map to the canonical public document IDs without changing the settings asset.
- Added deterministic competition ranking by highest Stage, then checked 64-bit `Silver*5 + Gold*7 + Diamond*10`, including shared ranks for exact score ties.
- Added open/manual-refresh-only leaderboard loading, a pinned green self summary, top-three VIP rows, first-place throne summary, equipment display, cached error handling, and no polling.
- Added private attempt analytics for resolved outcomes, response score, response duration, approved Response Efficiency, overall/per-Rank aggregates, fixed histograms for exact median display, total damage including overkill, recorded focused play time, and the first Stage 200 milestone.
- Added Profile Analytics cards for identity, progression, lifetime activity, Rank Currency, Power Coins, loadout, outcome totals, accuracy, response mean/median, efficiency mean/median, and per-Rank summaries. Hidden audit score/count remains absent.
- Added Display Name validation and optimistic Firestore rename persistence. The first accepted rename is immediately eligible; later changes enforce 168 hours against the server response time. Accepted names are projected to the leaderboard; a projection failure does not roll back private progress.
- Added local Firestore rule/schema documentation for the public projection. Rules were not published.
- Added no automated test scripts and ran no automated tests, per owner direction.

Verification performed: Core gameplay scripts compile with Unity's Roslyn response; UXML parses as XML; `git diff --check` reports no whitespace errors. A complete Unity refresh/compile could not be launched in batch mode because this project is already open in another Unity instance and that instance had not refreshed the new files. Human E2E and Firestore rule publication remain owner checkpoints.
