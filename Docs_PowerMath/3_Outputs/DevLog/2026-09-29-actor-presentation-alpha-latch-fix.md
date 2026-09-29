# Actor presentation alpha latch fix

## Findings

In `MainMenuScene.unity`, `playerPresentation` and `monsterPrefab` lack an authored `CanvasGroup` component. 
During the main menu entrance transition, `MainMenuTransitionController.CacheAuthoredCanvasState()` cached `playerCanvasGroup` and `enemyCanvasGroup` as `null`.

When `SetCanvasAlpha()` ran during `AnimateCanvasEntrance()`, it fell back to mutating `graphic.color.a` directly on `UnityEngine.UI.Image`. While this 0.45s cubic tween was fading in, `CombatLobbyCompositionRoot` finished loading and called `ActorPresentationController.Initialize()`.

At that moment ($\approx 0.14\text{s}$ into the fade), `graphic.color.a` was $\approx 0.6706$ (byte value 171). `ActorPresentationController.Initialize()` captured `_authoredColor = _graphic.color`. Because $0.67 > 0.01$, the guard `if (_authoredColor.a <= 0.01f)` did not trigger, permanently latching 171 as the actor's baseline color. Subsequent visual state transitions called `RestoreAuthoredPose()`, continually forcing the player and enemy sprites back to alpha 171.

`petPresentation` was unaffected because `MainMenuTransitionController` does not target the pet.

## Changes

- **ActorPresentationController.cs**:
  - In `Initialize()`, force `_authoredColor`'s alpha to `1f`: `_authoredColor = new Color(_graphic.color.r, _graphic.color.g, _graphic.color.b, 1f);`.
  - Restore `_graphic.color = _authoredColor;` immediately to clear any in-flight transition fade alpha from the graphic.
  - Added optional `bool resetAlpha = true` parameter to `Initialize()`. When `resetAlpha` is false, `_canvasGroup.alpha = 1f` is not executed, preventing mid-transition opacity pops while an entrance fade is in flight.
- **CombatLobbyCompositionRoot.cs**:
  - In `EnsureActorPresenters()`, determine `resetAlpha = transition == null || !transition.IsPlaying;` and pass it to `_playerActor.Initialize()` and `_enemyActor.Initialize()`. This preserves smooth fade-in curves during main menu entrance.
- **MainMenuTransitionController.cs**:
  - In `CacheAuthoredCanvasState()`, dynamically ensure a `CanvasGroup` exists on `playerArt` and `enemyArt` if missing: `playerArt.GetComponent<CanvasGroup>() ?? playerArt.gameObject.AddComponent<CanvasGroup>()`.
  - In `SetCanvasAlpha()`, check `target.GetComponent<CanvasGroup>()` before mutating `graphic.color`.
  - Add `EnsureGraphicOpaque()` in `CacheAuthoredCanvasState()` and `ApplyCanvasFinalState()` to guarantee `Graphic.color.a` is restored to `1f`.
- **ActorPresentationControllerTests.cs**:
  - Added EditMode unit test `Initialize_WhenGraphicAlphaIsFractional_ForcesAuthoredAlphaToOpaque`.
  - Added EditMode unit test `Initialize_WhenResetAlphaIsFalse_PreservesCanvasGroupAlpha`.
