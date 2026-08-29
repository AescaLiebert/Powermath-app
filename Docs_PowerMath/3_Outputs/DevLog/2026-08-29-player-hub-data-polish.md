# DevLog: 2026-08-29 — Player Hub Data and Presentation Polish

## Outcome

Implemented the approved full-screen Player Hub slice with canonical pet data, validated owned inventory, authoritative instant equip, shared Main Menu utility/notifications, Pet ATK projection, and visual-first Weapon Ascend/Pet feedback.

## What Changed

- Migrated the 15 approved gacha pets into reusable `PetDefinition` assets without changing IDs, rarity rates, or probability behavior.
- Added immutable Pet ATK content, a fail-closed equipped-pet stat policy, and threaded one runtime pet catalog through combat, settlement, and Player Hub projections.
- Added `PlayerOwnedPetInventory` over the existing saved inventory and an idempotent conditional Firestore equip command with receipt recovery, stale-state refresh, and rollback-safe UX.
- Rebuilt Player Hub as an environment-left/full-workspace destination with image-only equipped anchors, Weapon/Pet tabs, scrollable icon/rarity pet tiles, and a large rich-text-ready preview.
- Added a shared title/currency/back overlay for Player Hub, Biome Map, Profile Analytics, Pet Gacha, and Leaderboard plus semantic transient notifications.
- Added USS interaction transitions, LeanTween anticipation/bounce/nudge/pulse sequences, a preallocated UI particle burst, milestone feedback from authored weapon tiers, optional audio hooks, and Reduced Motion behavior.

## Implementation Notes

- The shared overlay is consolidated into one idempotent scene component and markup embedded in `MainMenuUI.uxml`; this keeps the approved reusable boundary while avoiding extra composition objects.
- Production Pet ATK remains zero by design. Test-only catalog data proves non-zero projection without inventing balance values.
- The hidden legacy close buttons remain as controller busy-state signals; the shared back control respects their enabled state so ambiguous writes cannot be closed out.

## Verification

- Compilation: zero errors.
- Combat Unity EditMode: 25/25 passed.
- Pet catalog Unity EditMode: 5/5 passed.
- Focused Pet ATK/ownership policy: 2/2 passed.
- Focused shared navigation/Player Hub UI contracts: 2/2 passed.
- Editor UI Toolkit captures completed for Weapon and Pet layouts at 1280×720.
- Targeted whitespace check passed.

Two unrelated baseline issues remain: one existing pet-roll boundary expectation and the live-Firebase readiness setup used by all four Combat PlayMode tests. Details are in `TestPlans/player-hub-data-polish-test-plan.md`.

## Human Follow-ups

- Author non-zero Pet ATK and ability copy only after content/balance approval.
- Assign final avatar, pet preview, weapon, particle, and audio assets.
- Complete the manual delayed-save, Reduced Motion, narrow-layout, and five-destination shared-bar pass before merge.
