---
slug: on-first-enemy-survive-tutorial
status: approved
source: manual
gdd_tags:
  - tutorial-system
  - combat-attempt
  - feedback
  - player-experience
owner: Codex
human_checkpoint: required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: `OnFirstEnemySurvive` Tutorial

## Player-Facing Goal

After the first real attack damages but does not defeat a standard enemy, Power teaches the Enemy Action Queue and makes the approaching counterattack understandable without changing combat state.

## Source

- Request: project owner, 2026-09-15.
- Canonical design: `GDD_PowerMathProject.md`, `@tag:tutorial-system`.
- Governing architecture: ADR-020 generic tutorial map and semantic UI adapters.
- Workflow: `/implement-feature`.

## Acceptance Criteria

- [x] Eligibility requires a persisted correct standard-combat hit with positive damage, surviving enemy, no flee, and one consumed cooldown action.
- [x] Trigger waits through cooldown animation, enemy attack/death, settlement, and any older tutorial.
- [x] Scared warning, Enemy Action Queue explanation, focus acknowledgement, and final encouragement are separate authored states.
- [x] Queue focus calls an acknowledgement action only; it never attacks or mutates cooldown.
- [x] Dialogue advances by screen tap; focus advances only by tapping the queue highlight.
- [x] English and Thai copy, emotion assets, Reduced Motion, reconnect restart, and queue ordering remain data-driven.
- [x] Completion persists exactly once after the final dialogue.
- [ ] Automated predicate/asset checks pass; final queue-priority and reconnect behavior require Editor playtest.

## Human Checkpoint

- [x] Approved localized copy and architecture plan (`lgtm`, 2026-09-15).
