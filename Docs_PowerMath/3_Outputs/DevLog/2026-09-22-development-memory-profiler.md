---
slug: development-memory-profiler
status: implemented-awaiting-review
source: manual
gdd_tags:
  - guardrails
  - player-experience
owner: project-owner
human_checkpoint: required
next_agent: project-owner
blocked_by: []
---

# Development Memory Profiler

## Current implementation

Added `DevelopmentMemoryProfiler` to the existing diagnostics assembly. It auto-creates
only in a Unity Development Player, records periodic and scene-loaded samples, exposes
`MarkCheckpoint(string)`, reports Unity/system/managed/GC/video/graphics counters, and
writes a JSON report when the platform provides a writable persistent path.

The profiler does not modify Player Settings, install packages, or run in release Players.
If WebGL cannot write a report file, console peak samples remain available.

## Validation

- Source-level review completed for release gating and no-op behavior outside Development
  Players.
- Node regression checks passed: 17 passed, 0 failed for the Web cache and YouTube
  lifecycle suites.
- Unity CLI validation was attempted with
  `unity test . --mode EditMode --output TestResults/development-memory-profiler-results.xml`
  but Unity refused to run while the project Editor was already open. The open Editor's
  current compilation also reports the unrelated existing
  `RunSettlementPanelController.cs(245,17): CS0103` error, so no Unity results report was
  produced.
- Human validation required on Development WebGL and iOS Safari using the linked
  regression test plan.

## Device-memory limitation

The tool records `SystemInfo.systemMemorySize` as a device hint. It cannot discover a
fixed iOS Safari/WebKit memory quota because Apple does not publish one and the actual
termination threshold varies with OS and process pressure. Jetsam evidence and measured
peaks remain the source of truth.
