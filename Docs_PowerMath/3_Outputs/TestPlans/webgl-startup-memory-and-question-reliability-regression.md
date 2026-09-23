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
- Run `unity test . --mode EditMode --output
  TestResults/development-memory-profiler-results.xml` for the affected Unity tests after
  the Editor imports the changed scripts. The current attempt is blocked because the
  project is already open in Unity Editor and the existing project compilation currently
  reports `RunSettlementPanelController.cs(245,17): CS0103`.
- Verify the release preflight accepts matching versions and rejects a player/manifest
  mismatch before a WebGL package is accepted.
- Verify source contracts require `isolateQuestionFallback: true`, simulation fallback
  presentation, and the 12-second startup window.
- Build a Development WebGL player from the same revision and confirm that
  `DevelopmentMemoryProfiler` emits `player-ready`, scene, checkpoint, and peak samples.
- Confirm a release WebGL player does not create the profiler object or emit
  `[MemoryProfile]` logs.

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

## Memory profiling procedure

1. Run the Development player with a cold browser cache and capture the console from
   `player-ready` through Main Menu, first video, first YouTube question, and combat.
2. Call `DevelopmentMemoryProfiler.MarkCheckpoint("name")` at any product-specific
   lifecycle boundary that needs a named sample.
3. Collect the JSON report from `Application.persistentDataPath` where the platform
   exposes a writable filesystem. WebGL may only provide console output; do not treat
   report-file failure as a gameplay failure.
4. Record the highest Unity, system, GC, video, and graphics-driver values. Compare
   cold-start and warm-cache runs separately.
5. Repeat on one lower-memory iOS device, one current iOS device, and Windows Chrome or
   Edge. Use the results to set a startup and peak budget before changing WebGL heap
   settings.

The `SystemInfo.systemMemorySize` value in the report is a device-reported hint, not a
Safari/WebKit memory limit. Apple does not publish a fixed WebGL quota per iPad or iPhone;
Jetsam decisions depend on iOS version, foreground/background state, browser process
sharing, graphics allocations, media decoder surfaces, and current system pressure.

## Release gates

- Do not publish until the owner aligns `bundleVersion`, `clientVersion`, and deployed
  artifacts, then approves the WebGL build.
- Do not tune `webGLInitialMemorySize` in this regression pass; use the separate memory
  baseline and Jetsam evidence plan first.
