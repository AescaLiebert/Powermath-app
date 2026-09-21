---
slug: webgl-startup-memory-and-question-reliability
status: needs-human
source: manual
gdd_tags:
  - combat-attempt
  - answer-scoring
  - player-experience
  - guardrails
owner: project-owner
human_checkpoint: required
next_agent: project-owner
blocked_by: []
---

# Task Card: WebGL Startup, Memory, and Question Reliability Fix

## Player-Facing Goal

Players can open the WebGL game without an unexplained reload, enter combat without
seeing YouTube's developer sample video, and recover clearly from slow or unavailable
question content. Low-memory mobile devices use a measured memory configuration rather
than a guessed heap size, and fallback play never changes canonical Firebase progress.

## Source

- Origin: Manual player feedback received 2026-09-20.
- Affected clients: Reported primarily on iOS, with additional PC reports.
- Reported symptoms: startup reload/out-of-memory behavior and the YouTube
  "Embedded Web Player Customization" video replacing the intended question video.

## GDD Reference

- `@tag:combat-attempt` — the committed attempt must lock one valid question and a
  confirmed system/content failure must void rather than reroll or corrupt it.
- `@tag:answer-scoring` — the video and answer window must resolve deterministically.
- `@tag:player-experience` — outcome and failure reasons must remain clear.
- `@tag:guardrails` — content failure is the only permitted exception to an
  irreversible committed attempt.
- ADR-011 — release version negotiation and cache invalidation must stay coherent.
- ADR-015 — bundled fallback questions run only in an isolated, unsaved practice session.
- ADR-018 — mobile YouTube playback remains inline, muted-first, and fail-closed.

## Type

- [x] Bug fix
- [ ] Feature
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [x] Tooling / release validation

## Confirmed Current-State Findings

1. `PlayerSettings.bundleVersion` is `1.1`, while the checked-in release policy requires
   `clientVersion` and `minSupportedVersion` `1.2.0.0`. Bootstrap therefore invokes the
   cache-purge reload path for a hard update.
2. Question startup has a hard `2.5 s` total deadline even though the configured request
   timeout is `30 s`.
3. Three Rank documents load sequentially. Only after they finish do three Challenge
   documents load sequentially, but all six operations share the `2.5 s` total deadline.
4. Deadline expiry aborts the live requests and builds a development catalog whose video
   URL is the YouTube sample ID `M7lc1UVf-VE`.
5. The live fallback currently passes `isolateQuestionFallback: false`, contradicting
   ADR-015 and the existing isolation contract test. Development content can therefore
   enter canonical Firebase progression.
6. Web initial heap is `512 MB`, maximum heap is `2048 MB`, geometric growth is `20%`,
   and the maximum growth step is `512 MB`. These values have not been justified by a
   current cold-start/mobile memory capture.
7. The mobile template already caps device pixel ratio at `1.3`; retain this protection.

## Root-Cause Statements

- The startup reload occurs when a `1.1` player evaluates a release manifest whose
  minimum supported version is `1.2.0.0`; this is an application-requested reload and
  must be separated from a WebKit/Jetsam termination during diagnosis.
- The wrong video occurs because the catalog deadline cancels slow but potentially valid
  Firebase requests after `2.5 s`, then routes production presentation to a hardcoded
  YouTube developer sample.
- Mobile memory failure remains a strong risk, not yet a confirmed single cause: Safari
  must reserve the Unity heap alongside the unpacked Web data, browser runtime, graphics
  allocations, media decoding surfaces, and the YouTube iframe.

## Delivery Plan

### PR 1 — Release Consistency and Reload Diagnostics

Scope: release-policy validation only; no deployment.

- Add an editor or packaging preflight that normalizes and compares:
  - `PlayerSettings.bundleVersion`;
  - `version.json.clientVersion`;
  - `version.json.minSupportedVersion`;
  - expected R2 build filenames or release directory.
- Fail packaging when the manifest requires a build that is not the build being packaged.
- Replace hardcoded `MathWorld-1.1` deployment-test paths with values derived from the
  packaged release manifest/build output.
- Log a structured reload reason before `PowerMathPurgeCacheAndReload`.
- Preserve the session loop guard and test that one mismatch cannot reload repeatedly.
- Do not change or publish `version.json`, CDN content, or R2 objects in this PR.

### PR 2 — Canonical Question Loading and Safe Fallback

Scope: question correctness and recovery only; no build-setting changes.

- Remove the production path to `M7lc1UVf-VE` and prevent any developer sample URL from
  being presented by a non-Editor build.
- Restore ADR-015 by passing `isolateQuestionFallback: true` for every catalog failure,
  using fresh in-memory attempt/audit state and `ImmediateGameplayPersistence`.
- Use `SimulationQuestionPresentation` for isolated practice so it cannot mount a fake
  YouTube question video.
- Display the existing explicit `PRACTICE QUESTIONS ACTIVE; PROGRESS IS NOT SAVED`
  notice throughout the isolated session.
- Separate loading UX from failure policy:
  - `2.5 s`: show a non-blocking "Still loading questions" state;
  - configurable network deadline: default proposal `12 s` pending measured latency;
  - one bounded retry with jitter for transient read failures;
  - after final failure, enter isolated practice or show Retry if practice validation fails.
- Do not cancel a valid request merely because the loading indicator threshold elapsed.
- Load the canonical Rank catalog first. Do not block ordinary combat startup on the
  Challenge catalog; load Challenge content in the background or at the first point it
  is actually required.
- Preserve generation checks so late callbacks cannot replace a newer catalog/session.
- Record structured failure information: phase, elapsed time, request result, HTTP code,
  rank/document identifier, retry number, and fallback mode. Never log API keys or player
  credentials.

### PR 3 — Memory Baseline and Asset Budget

Scope: measurement and report only; no Player Settings or dependency changes.

- Produce release and development Web builds from the same revision.
- Record build-report totals for `.data`, `.wasm`, framework code, textures, audio,
  fonts, and `Resources` content.
- Capture cold and warm runs on at least:
  - one lower-memory supported iPhone/iPad Safari device;
  - one current iPhone/iPad Safari device;
  - Windows Chrome and Edge.
- Capture checkpoints at loader start, Unity ready, authentication, Main Menu, first
  local video, first YouTube question, post-video cleanup, combat, and repeated scene
  transitions.
- Collect iOS Jetsam reports where available. Classify each failure as application reload,
  Unity heap exhaustion, WebKit process termination, GPU/media pressure, or unknown.
- Establish measured budgets for startup heap, runtime peak, GPU textures, `.data` size,
  and video-decoder overlap. Do not select a smaller heap until this report is accepted.

### PR 4 — Approved Mobile Memory and Payload Reduction

Scope: only the changes approved after PR 3; separate build/dependency checkpoint.

- Set initial heap to measured typical peak plus `20–30%` headroom, rounded to a `16 MB`
  boundary; retain geometric growth.
- Set maximum heap from measured worst-case usage and reduce the maximum geometric growth
  step from `512 MB` to a measured bounded value.
- Keep the `1.3` mobile pixel-ratio cap and verify framebuffer size on Retina devices.
- Ensure only one local `VideoPlayer` or YouTube presentation owns decode/playback state;
  stop, detach, and release media resources during dismissal and scene transitions.
- Remove large startup-only content from `Resources` in descending measured impact.
- Create capability-selected Web texture payloads: ASTC for supported mobile devices and
  DXT for desktop, with a verified fallback.
- Enable release compression and verify the real R2 response headers, MIME types, CORS,
  cache policy, and decompression behavior before disabling Unity's fallback decompressor.
- Consider Addressables only if the accepted build report shows that asset deferral is
  needed. A new package requires a separate dependency/ADR checkpoint.

### PR 5 — Atomic Release and Device QA

Scope: owner-operated publishing after code and build review.

- Upload versioned or content-hashed R2 artifacts before changing the release manifest.
- Verify all artifact URLs and response headers from the public endpoint.
- Publish the matching Pages shell and `version.json` atomically.
- Run the regression matrix below with cold browser storage, warm storage, throttled
  networking, blocked Firebase, blocked YouTube, and repeated reloads.
- Observe staged telemetry before broad release and retain the previous release for
  rollback.

## Files To Inspect First

- `ProjectSettings/ProjectSettings.asset`
- `Cloudflare/public/version.json`
- `Assets/Project/Script/Bootstrap/GameBootstrapper.cs`
- `Assets/Project/Script/Bootstrap/GameVersionChecker.cs`
- `Assets/Plugins/WebGL/PowerMathWebBridge.jslib`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
- `Assets/Project/Script/Gameplay/Academic/FirestoreQuestionCatalogRepository.cs`
- `Assets/Project/Script/Gameplay/Academic/FirestoreEventQuestionCatalogRepository.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/QuestionPresentations.cs`
- `Assets/Plugins/WebGL/PowerMathYouTube.jslib`
- `Assets/WebGLTemplates/MathWorldPWA/index.html`
- `Tools/Prepare-CloudflarePagesPackage.ps1`
- `Tools/Test-CloudflareR2Deployment.ps1`

## Required Automated Coverage

- Release preflight accepts equal normalized versions and rejects every mismatch direction.
- Reload guard permits one hard-update reload and prevents a loop for the same target.
- A question response completing after `2.5 s` but before the network deadline remains live.
- Timeout, HTTP error, malformed catalog, and cancellation enter isolated practice exactly once.
- Isolated practice cannot create Firestore progression, settlement, economy, leaderboard,
  pending-presentation, or analytics writes.
- Non-Editor fallback cannot invoke the YouTube bridge with `M7lc1UVf-VE`.
- Late callbacks from an aborted generation cannot replace the current catalog.
- Ordinary combat can start without waiting for Challenge catalog completion.
- YouTube start, error, autoplay-blocked, cancel, and ended paths release their overlay/player.

## Human Regression Matrix

| Case | Expected result |
| --- | --- |
| Matching release, cold cache | One load; no application-requested reload |
| Deliberate version mismatch | One guarded reload; clear update state if still incompatible |
| Firebase latency of 3–8 seconds | Loading status appears; canonical question video is used |
| Firebase unavailable | Explicit unsaved practice or Retry; no Firebase progression writes |
| YouTube unavailable | Attempt fails closed; overlay is removed; no sample substitution |
| iOS cold start | No Jetsam/WebContent termination within the accepted memory budget |
| Repeated local and YouTube videos | No accumulating decoder, texture, iframe, or heap growth |
| Windows Chrome/Edge | Canonical question and release behavior remain unchanged |

## Acceptance Criteria

- [ ] Matching deployed versions never trigger the hard-update reload path.
- [ ] Packaging rejects a manifest/build mismatch before publication.
- [ ] The YouTube developer sample video cannot appear in a production fallback.
- [ ] Canonical requests are not cancelled at `2.5 s`.
- [ ] Question fallback complies with ADR-015 and performs zero canonical writes.
- [ ] Slow, failed, and blocked content states give the player an explicit reason and recovery.
- [ ] Real-device evidence distinguishes intentional reloads from confirmed Jetsam events.
- [ ] The accepted mobile heap values are derived from recorded typical and peak usage.
- [ ] No regression in attempt identity, audit FIFO, answer scoring, event questions, or
  YouTube lifecycle behavior.
- [ ] Automated tests and the human regression matrix pass before publication.

## Out of Scope

- Replacing the direct-Firestore prototype with a trusted authenticated command backend.
- Re-authoring question videos or changing mathematical question content.
- Migrating YouTube question media to project-owned direct video files.
- Unrelated combat, economy, pet, stage, or UI redesign.
- Automatic merge, Firebase rule publication, CI changes, R2 upload, or production deploy.

## Human Checkpoints

- [ ] Approve PR 1 release-validation behavior.
- [ ] Approve the proposed `12 s` question deadline after latency evidence.
- [ ] Accept the PR 3 memory/build report and target budgets.
- [ ] Approve all Web Player/Build Profile changes.
- [ ] Approve any Addressables dependency and supporting ADR.
- [ ] Review PRs before merge.
- [ ] Approve R2/Cloudflare publication and staged rollout.

## Router Decision

- Workflow: `/fix-bug`
- Recommended sequence: PR 1, PR 2, PR 3, human memory checkpoint, PR 4, human
  release checkpoint, PR 5.
- Next artifact after approval: `Docs_PowerMath/3_Outputs/TestPlans/webgl-startup-memory-and-question-reliability-regression.md`.
