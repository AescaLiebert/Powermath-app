# DevLog: Centralized On-Screen Status Notification Toast System

**Date**: 2026-09-08
**Author**: Antigravity Pair Programmer
**Component**: UI / Core / Diagnostics / Feedback

---

## 1. Problem Statement & Motivation
Previously, player feedback across the application was handled through fragmented, static `<ui:Label>` elements embedded inside forms and modals (e.g. `auth-status` in `AuthenticationScreen`, `admin-reset-status` in `AdminPanel`, `pet-gacha-status` in `PetGachaPanel`, etc.).
These legacy messages had several limitations:
1. They occupied fixed layout space or caused visual jumps when text changed.
2. They lacked consistent visual hierarchy, animations, or standard durations.
3. They were isolated to specific screens and lacked a centralized notification pipeline connected to telemetry and diagnostics (`AppLog`).

The goal of this task was to introduce an industry-standard, centralized on-screen status toast notification system featuring:
- An absolute, floating notification banner overlay.
- An animated routine (Enter -> Hold -> Exit).
- Full integration with global logging (`PowerMath.Diagnostics.AppLog`).
- Replacement and centralization of legacy status feedback across UI Toolkit screens.

---

## 2. Architecture & Implementation

### 2.1 Core Components (`PowerMath.UI.Core`)
- **`StatusSeverity`**: `Info`, `Success`, `Warning`, `Error`.
- **`StatusToastOverlay`**:
  - UI Toolkit visual element tree consisting of `status-toast-container`, `status-toast-banner`, `status-toast-badge`, and `status-toast-text`.
  - Positioned absolutely at top-center (`top: 24px; left: 50%; translate: -50% -26px; scale: 0.94 0.94; opacity: 0;`).
  - Set to `picking-mode: Ignore` and `pointer-events: none` so it never blocks gameplay or UI interactions.
  - Lifecycle routine:
    - **Enter**: Sets `display = Flex`, applies `.is-visible` CSS transition (`opacity: 1; translate: -50% 0; scale: 1 1;`).
    - **Hold**: Remains visible for `durationMilliseconds` (default 2600ms; 3200ms for errors). Token-guarded to prevent race conditions when consecutive messages arrive.
    - **Exit**: Clears `.is-visible` (animates back to `opacity: 0; translate: -50% -26px; scale: 0.94 0.94;`), and sets `display = None` after the transition completes.
- **`StatusMessageService`**:
  - Global static facade (`Show`, `ShowSuccess`, `ShowError`, `ShowWarning`, `ShowInfo`).
  - Automatically records structured diagnostics into `AppLog` under the `"StatusUI"` category.
  - Broadcasts to all registered active overlays (`_overlays`) and event subscribers (`MessagePublished`).

### 2.2 Theme & USS (`MathWorldControls.uss`)
Added standard CSS transitions and severity variants matching the MathWorld palette:
- `.status-toast--info`: Deep Astral Blue (`rgba(20, 48, 91, 0.97)`) with Celestial Cyan border (`rgb(95, 211, 225)`).
- `.status-toast--success`: Forest Emerald (`rgba(18, 99, 68, 0.98)`) with Mint border (`rgb(128, 242, 171)`).
- `.status-toast--warning`: Amber Gold (`rgba(109, 70, 17, 0.98)`) with Bright Gold border (`rgb(255, 202, 84)`).
- `.status-toast--error`: Crimson Carmine (`rgba(128, 43, 51, 0.98)`) with Coral border (`rgb(255, 148, 130)`).

### 2.3 Integration Across UI Screens
- **`AuthenticationView.cs`**:
  - Attaches `StatusToastOverlay` to root upon element binding.
  - Replaced inline error reliance with `StatusMessageService.ShowError(playerMessage)` on `RenderFailure`.
  - Added `StatusMessageService.ShowSuccess(...)` on `RenderSuccess`.
  - Added `StatusMessageService.ShowInfo(...)` on language toggle.
- **`PlayerLifecycleRuntime.cs`**:
  - Discovers all scene `UIDocument`s and attaches `StatusToastOverlay`.
  - Dispatches `common.saving`, `common.saved`, and `errors.save` via `StatusMessageService`.
- **`MainMenuSharedOverlayController.cs`**:
  - Forwards `Publish(...)` calls to `StatusMessageService.Show(...)` for unified notifications.

---

## 3. Verification
- **Compilation**: Verified via Unity Editor log (`Logs/Editor.log`); all scripts compiled cleanly with zero errors (`CompileScripts: 6551.150ms`).
- **Tests Added**: `Assets/Project/Tests/EditMode/Editor/StatusToastTests.cs` (6 test cases covering facade dispatch, severity helpers, overlay attachment, display transitions, immediate hiding, and authentication view integration).
