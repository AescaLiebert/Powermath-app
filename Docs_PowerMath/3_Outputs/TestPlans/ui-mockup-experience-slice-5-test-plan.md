---
slug: ui-mockup-experience-slice-5
status: draft
source: manual
gdd_tags:
  - leaderboard-profile
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: required
next_agent: human-tester
blocked_by:
  - slice-5-human-visual-review
---

# UI Mockup Experience Slice 5 Test Plan

## 1. Scope

Validate the Leaderboard and Profile Analytics visual migration without changing cohort resolution, ranking order, weighted Rank Currency score, Firestore repositories, display-name policy, cooldown authority, or private educational-data boundaries.

## 2. Automated Evidence

- [x] Focused UI and shared panel-host EditMode contracts: 8/8 passed on 2026-08-27.
- [x] The stable Main Menu entry imports and clones focused Leaderboard and Profile Analytics templates.
- [x] Existing controller bindings resolve to their required UI Toolkit types.
- [x] Both panels start hidden, are focusable, and participate in one shared modal host.
- [x] Display-name input retains the authoritative 20-character maximum.
- [x] Grade tabs, mock Mastery/Streak/Focus Next, and hidden audit score/count labels are absent.
- [x] Unity compiled the changed scripts and imported the UI assets without source errors.

The full EditMode project run completed 89 tests: 88 passed and the same pre-existing `PetGachaCoreTests.Roll_UsesCategoryBoundaryThenExactPetWeights` test failed because it expected category `rare` and received `middle`. Slice 5 did not modify Pet Gacha.

## 3. Functional Scenarios

### TEST: Leaderboard is locked to the authenticated cohort

GIVEN: a hydrated student session  
WHEN: Leaderboard opens  
THEN: the cohort is derived from the authenticated level document, no grade selector exists, and only that cohort is requested.  
PRIORITY: P0

### TEST: Ranking and public row content remain authoritative

GIVEN: valid cohort standings  
WHEN: the response renders  
THEN: rows are ordered by Highest Stage then weighted accumulated Silver/Gold/Diamond, exact score ties share rank, and each row shows public name, Current/Best Stage, currencies, Avatar/Pet/Weapon, and Total Damage.  
PRIORITY: P0

### TEST: Refresh remains explicit and cache-aware

GIVEN: previously loaded standings  
WHEN: Leaderboard reopens or Refresh is pressed  
THEN: cached standings remain visible while one manual request runs, the Refresh action is disabled during loading, and failure labels the stale cached state rather than clearing it.  
PRIORITY: P0

### TEST: Profile Analytics exposes only approved student-visible metrics

GIVEN: a hydrated player snapshot  
WHEN: Profile Analytics opens  
THEN: adventure, economy/loadout, persisted learning efficiency, by-rank, and by-question summaries render without login credentials, hidden audit values, recommendations, or invented trends.  
PRIORITY: P0

### TEST: Rename is one authoritative operation

GIVEN: the display name is valid and cooldown permits a change  
WHEN: Save Name is selected repeatedly  
THEN: one save runs, Save and Close are disabled until resolution, accepted state updates the session and leaderboard projection, and failure remains editable with a labeled reason.  
PRIORITY: P0

## 4. Edge and Abuse Cases

- [ ] Spam Leaderboard/Profile openers and verify only one host-managed panel remains open.
- [ ] Close Leaderboard during refresh; verify the read-only request cancels and Refresh is restored.
- [ ] Disconnect during refresh with and without cached standings.
- [ ] Validate exact rank ties and deterministic display-name/player-ID fallback ordering.
- [ ] Validate empty standings and a missing self projection.
- [ ] Spam Save Name and attempt Close/another panel while saving.
- [ ] Reject unchanged, short, long, control-character, and unsupported-bracket display names.
- [ ] Reconnect after accepted rename; the cooldown and public display name remain authoritative.
- [ ] Validate long localized names and very large currency/damage values.

## 5. Game-Experience Evaluation

| Component | Slice 5 evidence |
| --- | --- |
| Clarity | Cohort lock, ranking rule, cache freshness, public/private boundary, and rename cooldown are labeled before action. |
| Motivation | Leaderboard emphasizes persistent Highest Stage and accumulated Rank Currency; Profile connects learning and adventure records without fabricating goals. |
| Response | Manual refresh, cancellable read requests, shared modal exclusivity, and non-cancellable committed rename states match the consequence of each action. |
| Satisfaction | Top-three, self, loading, success, and error states are visually distinct while retaining text labels. |
| Fit | Cream cards, cyan/cobalt fields, coral/gold accents, broad arcs, and simple symbols continue the approved flat Math:World language. |

No new timing, ranking, score, cooldown, or economy values were introduced.

## 6. Visual and Accessibility Review

- [x] Best Stage reads before secondary Rank Currency and Total Damage fields in the layout hierarchy.
- [x] Self and top-three states remain labeled without relying only on color.
- [x] Profile groups authoritative data into Adventure, Economy/Loadout, Learning, and By Rank/Question sections.
- [x] Close and Refresh are icon-only with tooltips; primary rename action retains text.
- [ ] Review populated, empty, loading, cached-error, tie, and long-name Leaderboard states at 1920x1080.
- [ ] Review Profile Analytics with empty, typical, and dense by-question analytics at 1920x1080.
- [ ] Validate keyboard-only focus order and opener-focus restoration in Play Mode.
- [ ] Validate minimum supported window, 16:10, ultrawide, and high-DPI scaling.
- [ ] Confirm final UI audio treatment during Slice 6 polish; no unapproved audio asset was introduced here.
- [ ] Project owner approves the Slice 5 visual direction.

## 7. Acceptance Gate

Slice 5 is accepted when the project owner approves both panel visuals, functional and abuse scenarios pass with a real hydrated session, the viewport matrix is complete, and public/private data boundaries are verified against production-like records.
