---
slug: webgl-startup-memory-and-question-reliability
status: draft
source: manual
gdd_tags:
  - combat-attempt
  - answer-scoring
  - player-experience
  - guardrails
owner: project-owner
human_checkpoint: required
next_agent: human-qa
blocked_by: []
---

# WebGL Startup and Question Reliability Regression Plan

## Automated checks

- Run `node --test Tools/Tests/web-cache-reload.test.cjs Tools/Tests/youtube-question.test.cjs`.
- Run Unity EditMode tests for `VersionAndMigrationTests` and
  `UiEntryAssetContractTests` after the Editor imports the changed scripts.
- Verify the release preflight accepts matching versions and rejects a player/manifest
  mismatch before a WebGL package is accepted.
- Verify source contracts require `isolateQuestionFallback: true`, simulation fallback
  presentation, and the 12-second startup window.

## Human WebGL cases

1. Open a build whose player and manifest versions match. Confirm there is no forced reload.
2. Open an intentionally mismatched build. Confirm one guarded update/reload attempt and
   a clear incompatible-client state if the old artifact remains.
3. Throttle Firebase so a valid catalog completes after 2.5 seconds but before 12 seconds.
   Confirm the canonical question video is used.
4. Block or corrupt the Rank catalog. Confirm isolated practice mode, simulation
   presentation, explicit unsaved-progress notice, and zero progression writes.
5. Let the Challenge catalog fail while Rank content succeeds. Confirm Rank combat works
   and an Event attempt fails closed without a synthetic question.
6. Block YouTube while canonical content loads. Confirm the iframe is removed and the
   attempt returns to a safe state; the developer sample video must not appear.
7. Repeat startup, question cancellation, scene transitions, and retry. Confirm no stale
   callbacks replace the active catalog or presentation.
8. Repeat the same cases on iOS Safari, Android Chrome, Windows Chrome, and Edge.

## Release gates

- Do not publish until the owner aligns `bundleVersion`, `clientVersion`, and deployed
  artifacts, then approves the WebGL build.
- Do not tune `webGLInitialMemorySize` in this regression pass; use the separate memory
  baseline and Jetsam evidence plan first.
