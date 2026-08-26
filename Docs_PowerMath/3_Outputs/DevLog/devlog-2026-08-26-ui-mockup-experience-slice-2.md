# DevLog: 2026-08-26 — UI Mockup Experience Slice 2

## Goal

Migrate the Main Menu shell, combat surface, and existing informational World Map/Navigator into the approved Math:World visual system without changing combat, travel, persistence, or economy behavior.

## What I Did

- [x] Replaced the monolithic Main Menu entry layout with shell, combat, Navigator, and compatibility feature-panel templates while retaining `MainMenuUI.uxml` as the scene entry asset.
- [x] Added a focused Main Menu stylesheet that reuses the Slice 1 palette, typography, controls, and semantic states.
- [x] Removed placeholder portrait and Attack bitmap dependencies from the migrated shell in favor of native UI Toolkit geometry.
- [x] Added `MainMenuPanelHost` in the existing Combat Unity assembly and routed World Map open/close through it with one-panel exclusivity and focus restoration.
- [x] Preserved all existing controller element names and authoritative presenters/gateways.
- [x] Added Main Menu binding-contract and panel-host tests and replaced the brittle Pet Gacha inline-display assertion with the semantic `is-hidden` contract.
- [x] Reviewed the shell, combat-ready, answer-entry, and Navigator compositions at 1920x1080.

## Key Decisions

- Navigator remains the current informational Stage Map. It explicitly says `NO FAST TRAVEL` and exposes no selectable travel command.
- The combat view retains authoritative snapshot projection and attempt commitment. The panel host owns visibility/focus only.
- Unmigrated Rebirth, Player Hub, Pet Gacha, Profile, and Leaderboard markup moved intact into a compatibility template so later slices can replace one panel at a time.
- The host lives inside the existing Combat Unity asmdef because that view cannot reference the default Assembly-CSharp layer. No assembly definition or dependency was added.

## Bugs Found

- [x] The old Pet Gacha test depended on an inline `display:none` value that nested template cloning returned as `Flex`; migrated it to the stable semantic hidden-class contract.
- [ ] All four existing combat PlayMode tests stop on `Player ID cannot identify the Firestore level and student fields.` before their combat assertions. This is recorded in the Slice 2 test plan for fixture repair.

## Game Feel Notes

The 5-component review prioritized Response and Clarity: Attack remains the dominant commitment, Stage/biome/cooldown information is glanceable, Navigator states its read-only role before interaction, and a competing panel cannot silently replace the current one. The flat compass/orbit language keeps Fit consistent with Authentication and Bootstrap without importing unapproved art.

## Next Session

- Complete project-owner visual review for Slice 2.
- Repair or re-baseline the combat PlayMode player-ID fixture.
- After approval, migrate Slice 3: Player Hub and Rebirth.

---

## Git Commit Summary

```text
feat(ui): migrate main menu combat and navigator experience

- split the stable Main Menu entry into focused UI Toolkit templates
- add presentation-only panel exclusivity and focus restoration
- preserve combat and map authority with binding/state regression coverage
```
