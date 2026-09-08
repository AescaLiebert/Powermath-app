---
slug: player-lifecycle-live-service
status: draft
source: manual
gdd_tags: [server-authority, player-experience, leaderboard-profile]
owner: Codex
human_checkpoint: required
next_agent: human
blocked_by: [design-and-architecture-approval]
---

# Player lifecycle architecture preparation

Inspected bootstrap, schema migrator, snapshot, default planner, credential persistence, browser cache bridge, repository Firestore rules, character UI references, and related ADRs/GDD sections.

Produced task card, design proposal, architecture proposal, and proposed ADR-016. Identified client-stamped schema/time, default repair preceding new-player detection, permissive prototype writes, and unbounded/scoped-too-broad cache reload as gaps relevant to the request.

Validation: source inspection and documentation consistency checks only. No Unity tests, live database access, implementation, build, or deployment performed. Existing Figma plugin edits and generated utility assets were left untouched.

Pending human design/architecture review. The opening script/video, final character assets, tutorial content, backend host and legacy enrollment policy remain explicit follow-up inputs.


## Implementation after approval

User approved design/architecture and authorized placeholders. Implemented schema V3 guards, typed lifecycle commands, persisted locale, resumable opening/preparation UI, character presentation catalog/placeholders, scoped bounded reload, and future server command contracts. Added Unity policy/UI tests and Node browser bridge tests. See the implementation handoff for coverage, residual prototype trust limitations and broader regression failures.

Focused verification reached 19/19 passing Unity tests and 3/3 passing browser bridge tests. Actual WebGL/Firebase deployment was not attempted. Runtime copy conversion remains incremental; tutorial lessons and backend integration require their authored/service inputs.

Final visual review confirmed Thai text and both colored silhouettes. Replaced C# null-coalescing of serialized Unity sprite references with explicit Unity null checks; the UI regression now checks the assigned sprite as well as completion behavior. Final delivery suite: 19/19 Unity and 3/3 Node tests.
