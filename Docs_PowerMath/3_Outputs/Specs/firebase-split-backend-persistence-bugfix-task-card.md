---
slug: firebase-split-backend-persistence-bugfix
status: implemented-pending-live-verification
source: manual
gdd_tags:
  - combat-attempt
  - question-data
  - run-reset
  - server-authority
owner: implementation-agent
human_checkpoint: required
blocked_by: []
---

# Task Card: Split Firebase Backends and Persistence-Gated Gameplay

## Bug

1. Shared questions live in a different Firebase project, but `GameApiSettings` generated question URLs from the player project credentials.
2. Editor Play Mode always composed the simulation path, so real authenticated users never received a live progression store.
3. Resolved-attempt persistence was fire-and-forget; Play Mode or the app could continue/stop before Firestore acknowledged the write.

## GDD Contract

- `@tag:combat-attempt`: Attack commits cooldown and question identity before content begins.
- `@tag:run-reset`: Stage/run state, academic state, currencies, and permanent progression survive restart according to ownership rules.
- `@tag:server-authority`: every committed state-changing action is acknowledged before the next player-facing transition.
- `@tag:question-data`: shared content is independent from per-student history.

## Implemented Scope

- Independent question Firebase project ID, database ID, and Web API key.
- Live Editor composition when `useEditorSampleStudent` is disabled; Editor presentation remains simulated because the YouTube iframe is WebGL DOM.
- Persistence gates for attempt commit, answer-window open, result resolution, content-failure void, and presentation completion.
- Save payload includes revision, Stage/highest Stage, enemy HP/cooldown, hearts, combat phase, active attempt, Rank, audit/FIFO state, and Rank currencies.
- Stable completed run state rehydrates from `activeRun`; an unfinished committed attempt fails closed pending a dedicated recovery resolver.

## Acceptance

- [x] Question URLs use only the question Firebase credentials.
- [x] Player authentication/defaults/progression continue using only the player Firebase credentials.
- [x] Real-user Editor Play Mode composes live catalog and live writes when sample mode is off.
- [x] Video/result transitions do not continue before Firestore acknowledges the relevant state.
- [x] Resolved Stage, enemy, heart, currency, Rank, audit, and FIFO data map back into the player session and Firestore.
- [x] Automated compilation/tests do not contact live Firebase.
- [ ] Human configures the three question Firebase fields and repeats live E2E.
- [ ] Human verifies a completed answer, Play Mode restart, and exact Firestore values.

## Authority Note

This remains the previously approved direct-client REST prototype. Firebase is the durable persistence authority, but the Unity client still calculates results and can be tampered with. True secure server authority requires a separately approved backend/Cloud Function and rules deployment.
