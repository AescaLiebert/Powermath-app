---
slug: ui-mockup-experience
status: approved
source: manual
gdd_tags:
  - core-loop
  - run-reset
  - gacha
  - leaderboard-profile
  - feedback
  - player-experience
  - guardrails
owner: game-design-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Task Card: UI Mockup Experience Migration

## Player-Facing Goal

Transform the current functional-but-placeholder UI Toolkit prototype into one coherent Math:World experience matching the approved mockup direction across Authentication, Loading/Bootstrap, Main Menu, Navigator/World Map, Player Hub, Rebirth, Pet Gacha, Leaderboard, and Profile Analytics without breaking the existing authentication, persistence, economy, combat, or social-profile behavior.

## Source

- Origin: project-owner request on 2026-08-26.
- Visual references: `Docs_PowerMath/3_Outputs/UI_Mockups/`.
- Visual authority: `master-prompt-guide-art-direction.md`, with the flat Animo Gacha v10 screens as the primary visual anchors.
- Runtime evidence: current UI Toolkit UXML/USS, presenters, and panel controllers under `Assets/Project/UI/` and `Assets/Project/Script/UI/`.

## Current Implementation

- Unity `6000.5.3f1`, current Editor target `StandaloneWindows64`.
- Three build scenes: `BootstrapScene`, `AuthenticationScene`, and `MainMenuScene`.
- UI Toolkit is already the runtime UI framework.
- Authentication, bootstrap/recovery, combat lobby, informational World Map, Rebirth settlement, Weapon Ascension, one-pull Pet Gacha, Leaderboard, Profile Analytics, and logout have working controllers.
- `MainMenuUI.uxml` contains nearly every lobby and modal surface in one document; `CombatLobbyUI.uss` is a single large stylesheet.
- Most art inside the mockup PNGs is not available as separate production-ready UI sprites, backgrounds, portraits, icons, or text-free layers under `Assets/Project/`.

## Target Scope

- Establish one reusable UI visual system: palette, typography, spacing, corner, border, shadow, icon, focus, disabled, busy, success, warning, and error tokens.
- Replace placeholder composition with responsive screen layouts based on the approved mockups.
- Preserve stable UI element names or provide an explicit binding migration for existing controllers.
- Add a single modal/screen arbitration policy, focus restoration, safe close behavior, and input locking during authoritative operations.
- Use actual player/session/catalog data; never bake mockup names, currencies, ranks, pets, stages, or analytics into live screens.
- Add visual and audio feedback hooks, reduced-motion behavior, and non-color-only state communication.
- Validate all important screen states at supported aspect ratios and through the existing gameplay flows.

## Compatibility Constraints

- Authentication remains school username plus six-digit password/PIN. No email or Guest path is introduced by this visual migration.
- Rebirth follows the GDD settlement preview: Stage, Power Coins, Legacy ATK, Prestige/Honor, and explicit KEEP/RESET state.
- Initial Pet Gacha remains one pull for 25 Power Coins with current calculated odds visible before confirmation. x10, guarantee/pity, and summon history require separate approved game-system work.
- Initial Player Hub retains implemented Pet Status and Weapon Ascension behavior. Pet/weapon inventory browsing and equip changes require separately approved scope if not already supported by an authoritative command path.
- Leaderboard content follows `@tag:leaderboard-profile`; its v13 image is an information-architecture reference only, not the final density or art-style reference.
- Profile Analytics uses persisted/GDD-approved metrics only. Mockup-only “Mastery,” “Streak,” or “Focus Next” values must not be invented.
- Navigator means the existing informational World Map unless the project owner approves a separate definition and mockup.

## Out of Scope

- Changes to economy, gacha probability, pity/guarantee, x10 transaction semantics, duplicate handling, ranking, Rebirth rewards, or authentication rules.
- New backend services, Firestore schema/rules publication, secrets, CI/build setting changes, or dependencies.
- Canon approval for generated characters, Animo, names, numbers, or icons shown in mockups.
- Combat-loop redesign, Challenger League, teacher analytics, social messaging, or live-service content.
- Automatic publishing, merge, or deployment.

## Required Human Decisions

1. Confirm the first shipping target and aspect-ratio matrix. The current Editor target is Windows, while some older specs mention WebGL/mobile responsiveness.
2. Confirm `authentication-pet-gacha-blend-v12.png`, `loading-pet-gacha-blend-v12.png`, and `main-menu-pet-gacha-blend-v12.png` as composition references, subject to the v14 master guide.
3. Choose whether the first pass may use temporary flattened mockup crops or must wait for separate approved backgrounds, characters, Animo, portraits, and icons.
4. Confirm whether Player Hub is visual parity with current Weapon Ascension/Pet Status or includes new inventory/equip functionality.
5. Confirm whether Gacha is visual parity with the current x1 flow or a separate expansion to x10, guarantee, Details, and History.
6. Define Navigator beyond the current informational World Map, or approve that interpretation.

## Human Checkpoints

- [x] Approve the companion design spec and the six decisions above. Approved by the project owner on 2026-08-26 (`lgtm`).
- [x] Approve the architecture plan after design approval. Approved by the project owner on 2026-08-26 (`lgtm`).
- [ ] Approve any new final/canon artwork before importing it as production content.
- [ ] Approve any dependency, Firestore, build-setting, publishing, or merge change separately.
- [ ] Complete human game-feel and visual-parity review before acceptance.

## Implementation Progress

- [x] Slice 1 implemented: shared UI foundation, Authentication, and Bootstrap/Loading.
- [x] Slice 1 binding-contract tests passed (2/2) on 2026-08-26.
- [x] Authentication and Bootstrap reviewed at the 1920x1080 reference resolution.
- [x] Project owner visual approval for Slice 1 (`lgtm`, 2026-08-26).
- [x] Slice 2 authorization: Main Menu shell and Navigator/World Map (`lgtm`, 2026-08-26).
- [x] Slice 2 implemented: Main Menu shell, combat surface, panel host, and informational Navigator.
- [x] Slice 2 affected EditMode assembly passed (9/9) on 2026-08-26.
- [x] Project owner visual approval for Slice 2 (`lgtm`, 2026-08-26).
- [x] Slice 2 corrective pass implemented from the annotated Main Menu reference: click-through layering, compact HUD zones, collapsible Player Menu, enemy-action sequence, and Canvas-owned biome/enemy art.
- [x] Slice 2 corrective binding and interaction contracts passed (8/8); full EditMode remained 87/88 with the pre-existing Pet Gacha category-boundary failure.
- [ ] Project owner visual approval for the Slice 2 corrective pass.
- [x] Slice 3 authorization: Player Hub and Rebirth (`lgtm`, 2026-08-26).
- [x] Slice 3 implemented: focused Player Hub and Rebirth templates, shared panel-host routing, and semantic operation states.
- [x] Slice 3 affected EditMode assembly passed (11/11) on 2026-08-26.
- [x] Player Hub and Rebirth reviewed at the 1920x1080 reference resolution.
- [ ] Project owner visual approval for Slice 3.
- [x] Slice 4 authorization: current one-pull Pet Gacha experience (`continue on next Slice`, 2026-08-27).
- [x] Slice 4 implemented: focused one-pull Pet Gacha template, live odds preview, confirmation, committed/busy recovery, and distinct new/duplicate result states.
- [x] Slice 4 focused EditMode contracts passed (16/16) on 2026-08-27.
- [x] Slice 4 full EditMode regression remained 87/88 with the pre-existing Pet Gacha category-boundary expectation failure.
- [x] Project owner visual approval for Slice 4 (`LGTM`, 2026-08-27).
- [x] Slice 5 authorization: Leaderboard and Profile Analytics (`LGTM`, 2026-08-27).
- [x] Slice 5 implemented: focused Leaderboard and Profile Analytics templates, one shared panel host, manual-refresh states, safe rename states, and public/private information boundaries.
- [x] Slice 5 focused UI and panel-host contracts passed (8/8) on 2026-08-27.
- [x] Slice 5 full EditMode regression remained 88/89 with the pre-existing Pet Gacha category-boundary expectation failure.
- [ ] Project owner visual approval for Slice 5.
- [ ] Slice 6 authorization: compatibility cleanup, viewport/audio polish, and final regression evidence.
