---
slug: pet-gacha-system
status: draft
source: manual
gdd_tags:
  - economy
  - gacha
  - server-authority
  - feedback
owner: implementation-agent
human_checkpoint: required
next_agent: human-reviewer
blocked_by:
  - production-pet-catalog
  - unity-editor-test-run
---

# DevLog: 2026-08-13 - Pet Gacha System

## Goal

Implement the approved ownership-only Pet Gacha slice from `@tag:gacha`: transparent current odds, exact duplicate redistribution, a 25-Power-Coin idempotent pull, permanent ownership, recovery, and Main Menu feedback.

## What I Did

- [x] Added exact, pure probability and two-stage roll logic with deterministic test injection.
- [x] Added versioned `PetGachaCatalogDefinition` validation without inventing production content.
- [x] Added cryptographic prototype RNG and ADR-010's explicit authority limitation.
- [x] Added a direct-Firestore command that refreshes saved state and atomically writes spend, optional ownership, revision, and result receipt.
- [x] Extended player defaults/REST mapping with the last pull receipt.
- [x] Added Main Menu preview, confirmation, recovery, new/duplicate result, audio hooks, and Reduced Motion handling.
- [x] Added pure EditMode and UI contract tests plus the QA plan.
- [x] Preserved the deferred boundary: pulling does not equip a pet or change combat stats.

## Key Decisions

- Used integer basis-point rarity bands and exact within-rarity weights to preserve probability totals.
- Kept production gacha unavailable until rates totaling 10,000 basis points, pets, names, and icons are human-authored.
- Recorded client-side cryptographic RNG as a prototype-only limitation in ADR-010.
- Reused the existing inventory item shape and Power Coin persistence conventions.

## Bugs Found

- [x] Close could bypass the result reading hold - fixed inline.
- [x] Gacha modal defaulted visible before controller composition - fixed inline.
- [ ] Full batch Test Runner stalled during Unity asset import/Package Manager activity while other Unity Editor instances were active; re-run from the active project Editor before PR approval.

## Game Feel Notes

The compact interaction now protects cost/odds clarity before spectacle, and new versus duplicate results have distinct text, styling, timing, and audio hooks. Actual reveal feel cannot be judged until production icons, rarity colors, and audio are authored and playtested.

## Verification

- Unity Roslyn compilation: core, Unity content/RNG, Assembly-CSharp integration, and test assembly passed; only an existing LeanTween obsolete-API warning appeared.
- Core execution harness: passed redistribution, total conservation, completion reset, new/duplicate outcomes, exact spend, and insufficient funds.
- UXML: valid XML with unique element names.
- Git whitespace validation: passed.

## Next Session

- Author and approve the production pet catalog asset.
- Run EditMode tests and WebGL recovery/E2E cases from Unity.
- Playtest compact reveal readability with sound, mute, Reduced Motion, and mobile-sized viewport.

## Git Commit Summary

```text
feat(pets): implement idempotent pet gacha prototype

- preserve exact rarity probabilities with owned-pet redistribution
- save 25-Coin pulls and ownership with recoverable receipts
- add transparent Main Menu preview and result feedback
```
