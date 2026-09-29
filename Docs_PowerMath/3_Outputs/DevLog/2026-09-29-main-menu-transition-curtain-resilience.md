# Main Menu Transition Curtain Resilience, Synchronized Reveal, and UI State

## Summary

1. **Curtain Masking & Synchronized Reveal:**
   - The blue square curtain covers the screen from frame 0 during scene load, keeping everything masked while Firebase question catalogs, Addressables enemy sprites, and player session hydration complete in the background.
   - Once all presentation and session assets are ready (or upon reaching a 4.0s deadline), the curtain peels open (`_view.RevealScene()`) and the punchy "BATTLE!" title sequence (`_view.ShowBattleTitle()`) triggers **simultaneously**, accompanied by the battle impact sound effect and actor slide-in (`AnimateCanvasEntrance`).
   - After the "BATTLE!" banner holds and exits, the session HUD (top profile, player loadout, navigator) slides into place, followed by clean release of the bootstrap input lock.

2. **Revert of Cascading UI Disable:**
   - Reverted all `enabledSelf` / `enabled="false"` modifications on popups/modal panels (`PlayerHubPanel`, `PetGachaPanel`, `RunSettlementPanel`, `TutorialOverlay`, etc.).
   - Setting `enabled="false"` on parent visual elements cascaded down the hierarchy, disabling all child interactive elements (buttons, tabs, inputs, inventory slots) with `.unity-disabled`.
   - Modals and popups now use standard display-level toggling (`is-hidden` / `DisplayStyle.None` / `DisplayStyle.Flex`), ensuring sub-content is never disabled upon opening.

## Flow & Timing Details

- **Phase 0 (Background Loading / Curtain Solid):**
  - Frame 0: `main-menu-transition-layer` is `display: flex; is-bootstrap-active` and `is-transition-blocking`. `main-menu-transition-cover` is opaque solid blue (`rgb(13, 49, 94)`).
  - Background coroutines load Addressables enemy sprite, hydrate Firestore player session, and fetch question catalogs.
  - Controller synchronizes on `(!_presentationReady || !_sessionReady)` with a 4.0s safety deadline.
- **Phase 1 (Synchronized Curtain Open + BATTLE! Impact):**
  - `_view.RevealScene()`: Cover fades out (0.42s) and 9 transition squares slide outward (0.35s).
  - `_view.ShowBattleTitle()`: "BATTLE!" title punches in with scaling and opacity (0.24s).
  - `PlayClip(settings.BattleStartImpact)`.
  - `AnimateCanvasEntrance()`: Player and Enemy canvas actors slide into their rest positions.
- **Phase 2 (Hold & Exit Title):**
  - Title holds for `settings.TitleHoldSeconds` (0.75s).
  - `_view.HideBattleTitle()` (0.22s).
- **Phase 3 (HUD Entrance & Interactive Unlock):**
  - Actors finish entering canvas.
  - `AnimateSessionUi`: Top bar, Player Menu, and Dashboard slide smoothly into view via `UiMotionChannel.Lifecycle`.
  - `_view.ApplyFinalState()`: Releases transition layer and restores interaction.

## Verification
- Verified clean compilation in Unity.
- Confirmed `UiEntryAssetContractTests`, `ActorPresentationControllerTests`, and `MainMenuPanelHostTests` contracts pass without any disabled sub-content or transition regressions.
- Verified all 11 panel UXML files and panel controllers are restored to standard `is-hidden` display management.

