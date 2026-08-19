---
slug: pet-gacha-system
status: approved
source: manual
gdd_tags:
  - economy
  - gacha
  - server-authority
  - feedback
owner: code-review-agent
human_checkpoint: required
next_agent: qa-agent
blocked_by: []
---

# Pet Gacha System - Self Review

## Summary

Approved for human review with the production catalog intentionally absent and fail-closed. The implementation follows ADR-010's prototype authority boundary, separates exact probability logic from Unity content and Firestore persistence, and does not activate deferred pet equip/combat behavior. No blocker or warning remains in the reviewed diff.

## Issues Found and Resolved

### Result readability could be bypassed through Close

- **Severity:** Warning, resolved inline.
- **File:** `PetGachaPanelController.cs`
- **Issue:** Continue respected the 600 ms result hold, but Close initially became available as soon as the receipt resolved.
- **Fix:** Keep both Continue and Close disabled until the minimum readable hold completes.
- **Rule:** Design spec Response/Clarity priority and `@tag:feedback`.

### Main Menu could briefly show the gacha modal before composition

- **Severity:** Suggestion, resolved inline.
- **File:** `MainMenuUI.uxml`
- **Issue:** The modal was authored with `display:flex` and depended on controller initialization to hide it.
- **Fix:** Author the modal hidden by default; the controller explicitly opens it.
- **Rule:** Primary-platform clarity and safe initial UI state.

## Review Checklist

- [x] Architecture - matches the approved design, architecture plan, and ADR-010.
- [x] Naming - project C# conventions followed.
- [x] Performance - no `Update()` polling; UI references cached; rows rebuild only on preview refresh.
- [x] Hierarchy - no scene objects added; existing UIDocument composition retained.
- [x] Optimization - exact integer hot logic; no per-frame allocations introduced.
- [x] Events - accepted state broadcasts through `PlayerSessionStore.Changed`.
- [x] God-object check - probability, rolling, catalog, persistence, composition, and presentation remain separated.
- [x] Data-driven - production identities/rates/icons/audio require a validated ScriptableObject.
- [x] Platform - UI Toolkit pointer/touch flow and Reduced Motion behavior specified.
- [x] Edge cases - stale revision, insufficient funds, corruption, repeated input, ambiguous transport, missing content, new and duplicate results handled.

## Positive Findings

- The integer weight transformation implements the GDD formula exactly without floating-point roll drift.
- A result is never revealed before the atomic receipt is acknowledged.
- Retry uses one transaction ID and a matching saved receipt, preventing double spend and visible rerolls.
- Empty duplicates remain mechanically empty and are disclosed twice before confirmation.
- Production gacha cannot accidentally ship with invented placeholder rates or pets.
