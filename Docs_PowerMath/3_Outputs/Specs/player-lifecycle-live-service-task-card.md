---
slug: player-lifecycle-live-service
status: approved
source: manual
gdd_tags: [server-authority, player-experience, leaderboard-profile]
owner: Codex
human_checkpoint: required
next_agent: implementation
blocked_by: []
---

# Player lifecycle and public update protection

## Request and source

User request, 2026-09-08: Thai/English localization, persisted language, safe WebGL updates, browser refresh recovery, first-login opening followed by Ricko/Stellar and display name selection, shared character presentation, and an architecture for future server-owned mailbox/events/announcements.

The four supplied JPGs are visual references only. Placeholder descriptions, titles, and button copy are not additional requirements. “PlayerPrep” is interpreted as player preparation/onboarding; Unity PlayerPrefs is separately discussed for local language preferences.

## Evidence and scope

The active GDD is `1_Inputs_Templates/GDD_PowerMathProject.md`; the stack file is still an unfilled template. GDD server-authority requires: “Every state-changing action uses a unique transaction ID and is idempotent.” Player experience requires clear outcomes and reconnect recovery. The new opening, language policy, and named protagonists come from this user request and are not yet documented in the GDD.

Workflow: implement-feature, preparation and human review only. Draft design and architecture accompany this card; neither is accepted implementation authority yet.

## Deliverables

- Design spec: `player-lifecycle-live-service-design-spec.md`.
- Architecture proposal and evidence: `player-lifecycle-live-service-arch-plan.md`.
- Proposed ADR-016, extending ADR-011 and proposing replacement of the relevant ADR-003 prototype trust boundary.
- Implementation in separate slices after approval; no live Firebase, dependencies, build settings, or deployment changes in this preparation pass.

## Acceptance criteria

- Thai/English switching updates active and subsequently opened UI; language survives refresh and follows the account after sign-in.
- Only confirmed absent game data creates a new-player flow; errors never create or reset a save.
- Opening and preparation resume from persisted checkpoints; duplicate confirms have one result.
- Ricko/Stellar selection consistently drives combat, hub, and default portrait presentation.
- Existing saves retain progression, inventory, wallet, analytics, active attempts, and pending presentation receipts.
- Stored schema version is read before repair; unknown newer schema is rejected before any write.
- Old tabs, conflicting writes, lost responses, and incompatible releases have defined recovery.
- Future claims use trusted identity/time and atomic reward receipts; a client-only adapter is never called server authoritative.

## Review decisions

Approve the proposed defaults together, or specify changes: Thai default; cloud language with local cache; skippable opening with text fallback; existing players bypass opening but may complete missing character choice without resetting progress; character immutable after confirmation in this first slice; introductory display name does not consume the existing rename cooldown.

Backend recommendation is Firebase Authentication plus a trusted service using Firestore transactions. Host choice and legacy identity enrollment need explicit follow-up approval before integration/deployment. Full tutorial content, opening script/video, and production character assets remain authoring inputs.


Approval: user accepted design and architecture with “LGTM” on 2026-09-08. Placeholder character/opening assets explicitly authorized. Backend hosting/enrollment and live deployment remain separate integration inputs.
