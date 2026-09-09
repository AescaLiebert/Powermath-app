---
slug: public-webgl-combat-polish
status: implemented-awaiting-playtest
source: manual
gdd_tags: [combat-attempt, stage-progression, server-authority, feedback, player-experience, guardrails, playtest]
owner: project-owner
human_checkpoint: required
next_agent: human-playtest
blocked_by: []
---

# Public WebGL lifecycle and combat polish

## Source and routing

Source: the user's goal-objective.md attachment, read on 2026-09-09. Workflow: `/implement-feature`. Existing approved lifecycle, version/cache/migration, combat presentation and question-flow plans remain implementation authority within their accepted scope. New design and architecture decisions require the repository's human checkpoints; do not infer approval of a new proposal from an older approval.

The stack template is unfilled. Determine implementation facts from the current project and tagged GDD; do not invent project decisions. Preserve existing uncommitted work, especially actor presentation, stage definitions and authentication UI.

## Requested outcome

Design/implementation authorization: on 2026-09-09 the user approved the proposed design for implementation and selected YouTube with keypad beside/below the iframe. Reuse accepted presentation and lifecycle architecture; no new backend, deployment or dependency decision is included.

- Thai/English UI Toolkit localization with persisted language and the existing bilingual font.
- Safe public WebGL updates and refresh recovery that preserve Firebase player data; extend the existing manifest/schema migration boundaries.
- First confirmed new-player opening and preparation, resumable tutorial infrastructure, and future server-authoritative mailbox/events/announcements contracts.
- Sliding-square scene transition; replace the old full-screen Battle title with a transparent white line/star Battle overlay. Main Menu loads actors concurrently with the overlay; interaction waits for both.
- Prevent player control of question video playback/settings; keep numeric input usable above/around video on mobile; absolute-position success/failure sticker and one additional second of result display before battle execution.
- After enemy death completes, fade old biome background fully out, switch, fade new background in without scaling; show the localized biome display name using Battle-style typography. Include an inner-biome background change halfway through its configured length. Lock interaction until the next enemy enters; refresh safely skips cosmetic transitions.
- A small battle SFX library for swing, hit, actor clicks, success/failure; differentiated death motion for normal/mini-boss versus player/big boss, with the latter longer.

## Acceptance and evidence

Every requested area must be classified against actual code as implemented, a confirmed defect, a new design choice, or an external authoring/integration input. Verify save preservation, unsupported schemas, duplicate operations, pending receipts and refresh behavior with focused tests where feasible. Do not label a direct Firestore client server authoritative. Verify browser iframe constraints against official provider documentation before promising an absolute control restriction.

New timings are starting values with playtest criteria. New audio, imagery, tutorial content and biome assets must use actual project resources or be identified as authoring inputs. No publishing, live Firebase writes, dependency or build/CI changes, merge, or destructive actions are authorized by this card.

## Required outputs

- `public-webgl-combat-polish-design-spec.md`: game-design-agent's reviewable presentation specification.
- `public-webgl-combat-polish-audit.md`: implementation evidence, compatibility constraints and bounded follow-up slices.
- Focused repairs only where covered by current authorization and accepted boundaries; record exact checks and remaining limitations.
