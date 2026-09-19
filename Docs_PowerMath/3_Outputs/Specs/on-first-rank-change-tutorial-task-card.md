---
slug: on-first-rank-change-tutorial
status: approved
source: manual
gdd_tags:
  - tutorial-system
  - server-authority
  - feedback
  - player-experience
owner: Codex
human_checkpoint: required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: `OnFirstRankChange` Tutorial

## Player-Facing Goal

After the first authoritative Rank promotion or demotion has fully resolved, Power explains permanent Rank Currency and guides the child through Profile Analytics and the Leaderboard without replaying rewards or interrupting combat.

## Source

- Request: project owner, 2026-09-15.
- Canonical design: `GDD_PowerMathProject.md`, `@tag:tutorial-system`, lines 148-157.
- Governing architecture: ADR-020 generic tutorial map and semantic adapters.
- Workflow: `/implement-feature`.

## Current State

- The generic reducer, localized overlay, focus proxy, interaction gate, and V5 tutorial map exist.
- Only `OnFirstCreate` is catalogued and the scene currently constructs one tutorial director.
- Rank transition feedback exists, but there is no durable tutorial eligibility signal containing promotion/demotion context.
- Profile Analytics and Leaderboard controllers do not expose semantic opened/closed events or tutorial-authorized open commands.

## Target State

- `OnFirstRankChange` is a separately authored localized sequence in the shared catalog.
- Its eligibility is recorded from an authoritative Rank transition or preserved legacy Rank-change history.
- Queue selection prevents it from overlapping `OnFirstCreate` or unsafe combat/settlement presentation.
- Guided panel actions invoke the same production panel-host path as ordinary input.
- Promotion and demotion use truthful, distinct Power emotion/copy.
- Completion persists only after both panels were opened and the final explanation advanced.

## Out of Scope

- Rank calculation changes, currency awards, leaderboard scoring changes, backend/rules deployment, tutorial rewards, `OnFirstRebirth`, and PR/merge/publishing.

## Acceptance Criteria

- [ ] First persisted promotion/demotion queues exactly one tutorial entry.
- [ ] Existing Rank-history accounts remain eligible without resetting Rank.
- [ ] Presentation waits for attempt feedback, counterattack/death, and settlement completion.
- [ ] Promotion and demotion opening copy are distinct and truthful.
- [ ] Only Profile Analytics is actionable at its focus step.
- [ ] The matching Rank Currency is highlighted inside the open profile panel.
- [ ] Only Leaderboard is actionable at its focus step.
- [ ] Rank Currency contribution is highlighted in the open leaderboard.
- [ ] Refresh resumes a saved step without reopening a panel command twice.
- [ ] `OnFirstCreate` wins when older/active; `OnFirstRankChange` remains queued.
- [ ] English/Thai and Reduced Motion remain supported.
- [ ] Tests cover queue ordering, rank direction, panel events, reconnect, and completion.

## Human Checkpoints

- [x] Approve copy, emotion mapping, guided panel flow, and visual emphasis (`lgtm`, 2026-09-15).
- [x] Approve the architecture extension to ADR-020 before implementation (`lgtm`, 2026-09-15).
- [ ] Approve final Editor/mobile playtest and PR merge separately.
