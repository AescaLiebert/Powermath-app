# DevLog — Worldwide Language Switcher & Global Industry-Standard Logging

Date: 2026-09-08

GDD references: `@tag:player-experience`, `@tag:core-loop`.

## Summary of Changes

1. **Restricted Language Toolbar from All Scenes**:
   - Removed the runtime `language-toolbar` creation loop in `PlayerLifecycleRuntime.cs` (`BindDocuments()`) that was previously injecting TH / EN buttons into every `UIDocument` across all loaded scenes.
   - Preserved `LocalizedDocument` and `CharacterPresentationBinding` bindings across all scenes so UI text responds cleanly to locale changes without scene reloads.
   - Added `PlayerLifecycleRuntime.SetLocaleAndSave(...)` for persistent locale management.

2. **AuthenticationScene Worldwide Button & Animated Flyout**:
   - Updated `AuthenticationScreen.uxml`: Configured the top-right button as a Worldwide button (`worldwide-button`, `🌐`) and added `auth-language-flyout` with `language-button-th` ("ไทย") and `language-button-en` ("EN").
   - Updated `AuthenticationScreen.uss`: Added warm MathWorld themed styling for the Worldwide button and language flyout, incorporating smooth CSS transforms (`transition-property: opacity, scale, translate;`) and `.is-open` / `.is-active` states.
   - Updated `AuthenticationView.cs`: Added responsive toggle animations, locale switching through `LocalizationService.SetLocale(...)`, dynamic button active state updates, and global logging.

3. **Industry-Standard Global Logging Architecture (`PowerMath.Diagnostics`)**:
   - Created standalone assembly `PowerMath.Diagnostics`:
     - `LogLevel`: Verbose, Debug, Info, Warning, Error, Fatal.
     - `LogMessage`: Immutable structured record containing UTC timestamp, frame count, log level, subsystem category, formatted text, optional exception with full stack trace, and Unity Object context.
     - `ILogSink` & `UnityConsoleSink`: Formats console output with rich text color-coded tags in the Unity Editor and standard ISO formatting in standalone builds.
     - `AppLog`: High-performance static facade with thread-safe ring buffer history (last 128 entries), `MessageLogged` event broadcaster, configurable `MinimumLevel`, and formatted logging methods.

4. **Codebase Migration (Elimination of Individual Logs)**:
   - Updated ASMDEF dependencies (`PowerMath.UI.Core`, `PowerMath.Gameplay.Combat.Unity`, `PowerMath.Gameplay.Pets.Unity`, and corresponding test assemblies) to reference `PowerMath.Diagnostics`.
   - Replaced all 44 raw `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls across 26 files with structured `AppLog.*` calls categorized by domain (`Auth`, `Combat`, `Localization`, `Lifecycle`, `Bootstrap`, `Version`, `Progression`, `SocialProfile`, `Pets`, `SafeArea`, `UI`, `WebCache`, `Session`).

5. **Testing**:
   - Added `AppLogTests.cs`: Verified minimum log level filtering, structured formatting, exception capture, event dispatching, and ring-buffer history.
   - Added `AuthenticationLanguageUiTests.cs`: Verified Worldwide button presence, flyout toggling, TH/EN locale switching, active styling, and absence of accidental toolbar.
