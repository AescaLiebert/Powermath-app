---
slug: webgl-startup-memory-and-question-reliability
status: implemented-awaiting-review
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

# 2026-09-21 — WebGL Startup and Question Reliability P1–P2

## Bugs Found

- The WebGL player can be `1.1` while the release manifest requires `1.2.0.0`,
  causing the intentional hard-update cache purge/reload path to run.
- Question startup used a hard `2.5 s` total deadline while Rank and Challenge
  documents were fetched sequentially. The configured 30-second request timeout was
  effectively ignored by per-request three-second caps.
- Catalog timeout fallback used development questions with live player persistence and
  mounted the live YouTube presentation, exposing the developer sample video.
- Event-catalog failure synthesized event content from the Rank catalog and continued
  with live persistence.

## Changes

- Added `GameVersionChecker.ValidateReleaseAlignment` plus Cloudflare packaging
  preflight validation so a package cannot be prepared with a manifest targeting a
  different client version.
- Increased the question startup window to 12 seconds and removed the three-second
  per-request cap in both question repositories; the existing configured request timeout
  remains the upper request limit.
- Changed question timeout fallback to an isolated practice session with an explicit
  `PRACTICE QUESTIONS ACTIVE; PROGRESS IS NOT SAVED` notice and simulation presentation.
- Changed Challenge catalog failure to leave ordinary Rank combat available while event
  attempts fail closed instead of synthesizing event content.
- Added EditMode contract coverage for release alignment, isolated fallback, simulation
  presentation, and removal of the 2.5-second deadline.

## Verification

- `node --test Tools/Tests/web-cache-reload.test.cjs Tools/Tests/youtube-question.test.cjs`: **17 passed**.
- The combined browser command also included `webgl-glyph-policy.test.cjs`; its single
  failure reports four unrelated pre-existing files and is outside this change.
- Unity EditMode tests were not executable because no Unity or .NET command-line runner
  is available in the current environment.
- No WebGL heap setting, version manifest, Firebase rule, R2 object, CDN deployment,
  dependency, or merge was changed.

## Review Checkpoint

- The current checked-in `bundleVersion`/`version.json` mismatch is intentionally still
  visible; the new postprocessor will reject a WebGL build until the owner aligns and
  approves those release values.
- Memory measurement, heap tuning, texture variants, Addressables, and deployment remain
  P3–P5 work from the task card.
