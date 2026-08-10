---
slug: firestore-player-question-youtube-migration
status: implemented-pending-unity-runner
source: manual
gdd_tags:
  - core-loop
  - combat-attempt
  - answer-scoring
  - question-data
  - run-reset
  - server-authority
  - guardrails
owner: orchestrator-agent
human_checkpoint: required
next_agent: game-design-agent
blocked_by: []
---

# Task Card: Firestore Player/Question Migration and YouTube Presentation

## Player-Facing Goal

An existing student can enter PowerMath even when their stored game profile is incomplete: the game adds only missing defaults, then loads the student's saved state. Attack selects a question from the active Rank's independent Firebase catalog, opens its YouTube video, and returns the student to the existing answer/audit/combat flow without confusing Stage, Rank, or question identity.

## Source

- Origin: Manual project-owner request on 2026-08-10.
- Requested changes:
  1. Accept valid YouTube `video_link` values and call YouTube from Unity.
  2. Add a live Firebase question catalog independent from individual player progression.
  3. Replace `competition-2/{level-*}/{student}.gamedata.game1` with `competition/{level*}/{student}.gamedata` and create missing defaults for an existing student.
- Explicit risk decision: direct Unity/Firestore access remains acceptable for the prototype regardless of security.

## Router Decision

- Workflow: `/implement-feature`
- Current artifact: `Docs_PowerMath/3_Outputs/Specs/firestore-player-question-youtube-migration-task-card.md`
- Next artifact: `Docs_PowerMath/3_Outputs/Specs/firestore-player-question-youtube-migration-design-spec.md`
- Human checkpoint: design approval required before architecture work.

## GDD Alignment and Required Reconciliation

- `@tag:core-loop` and `@tag:combat-attempt`: Attack locks a question, presents its video, then opens the answer window.
- `@tag:answer-scoring`: one answer resolves correctness, response score, audit, Rank Currency, and combat.
- `@tag:question-data`: Rank pools retain independent FIFO/cycle history and question IDs remain separate from visual Stage.
- `@tag:run-reset`: academic/player progression survives sessions.
- Current GDD conflicts to update after design approval:
  - direct `.mp4` playback becomes an external valid YouTube URL;
  - question `rank` is inferred from the Silver/Gold/Diamond document rather than repeated per item;
  - direct client authority is an explicitly accepted prototype exception to `@tag:server-authority`.

## Current Implementation

- `FirestoreRestClient` reads only `competition-2/level-1..3`, parses `gamedata.game1`, and never writes defaults.
- Deep `JsonUtility` Firestore wrapper DTOs exceed Unity's serialization-depth limit.
- Academic Core/audit/FIFO behavior exists, but the composition root loads deterministic in-memory questions and keeps progression scene-local.
- `QuestionDocumentDto` requires `{id, video_link, answer, rank}` and validates `.mp4` links.
- `SimulationQuestionPresentation` does not launch or play remote content.

## Target Scope

- Shallow Firestore REST decoding that does not use nested `JsonUtility` wrapper graphs.
- New competition schema with direct `gamedata` under the dynamic existing student map; no `game1` wrapper.
- Missing-field detection and masked PATCH that preserves all existing values and unrelated student/game data.
- Explicit default player, audit, Rank Currency, and Rank question-state values.
- Three Rank catalog documents: Silver, Gold, and Diamond; item Rank inferred from the owning document.
- Stable composite domain identity `QuestionKey(Rank, id)` and numeric ID ordering.
- Valid YouTube URL normalization and embedded WebGL IFrame presentation through Unity.
- Live Firestore question repository behind the existing catalog port; deterministic repository remains test-only/development fallback by explicit configuration.
- Persisted academic state contract separate from shared question content.
- REST contract, mapper, progression, and PlayMode coverage without mutating real Firebase during automated tests.

## Out of Scope

- Firebase Authentication, secure backend authority, hiding answers from the client, rules/index deployment, credential rotation, or production hardening.
- Native mobile WebView/player SDK integration or extracting YouTube media streams.
- Renumbering/migrating existing question IDs or live student data during tests.
- New Unity packages, WebGL templates, build settings, CI, deployment, or secrets.

## Acceptance Criteria

- [x] An existing matching student with complete data is read without any write.
- [x] An existing matching student with missing required fields receives only those defaults; existing fields remain unchanged.
- [x] A missing student is not silently registered.
- [x] A concurrent/stale write conflict reloads before retry and never overwrites newer data.
- [x] The new schema reads `gamedata` directly and contains no `game1` dependency.
- [x] Firestore payload parsing cannot trigger Unity serialization-depth warnings.
- [x] Shared question content is loaded independently from player progression.
- [x] Silver, Gold, and Diamond documents infer Rank and contain question items `{id, video_link, answer}`.
- [x] Numeric IDs sort deterministically and are unique within Rank; identity is composite across Rank.
- [x] Only valid supported YouTube watch/short URLs enter the catalog.
- [x] Attack opens the locked YouTube URL and waits for embedded playback to end before the answer interface.
- [x] Question load/open failure voids the attempt instead of marking the student incorrect.
- [x] Audit, FIFO, Rank, Rank Currency, and question-history state rehydrate from and persist to the student's `gamedata` contract.
- [x] Production does not silently substitute QA questions when live content is unavailable.
- [ ] Automated REST tests use a fake transport and never write real Firebase.

## Human Checkpoints

- [x] Approve embedded YouTube IFrame flow, three-document item shape, and interpretation of the competition schema. Approved by the project owner on 2026-08-10 (`ok LGTM, implementation`).
- [x] Approve architecture and superseding ADR before implementation or any Firebase write path. Approved by the project owner on 2026-08-10 (`LGTM`).
- [ ] Approve any later rules, indexes, secrets, dependency, WebGL-template, or deployment change separately.
- [ ] Review PR before merge.
- [ ] Approve official team-status publishing.
