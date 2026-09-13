# WebGL Fullscreen and Installed-PWA Test Plan

## Player goal and context

The player stays in the browser's current display mode during login and combat.
Fullscreen is an explicit choice in Settings and must include browser-hosted video.

## System rules

- Login, Enter, and player/enemy pointer interaction never request fullscreen.
- Fullscreen is changed only from the explicit Settings toggle.
- No request is issued when browser fullscreen, installed-PWA display mode, or a
  previous fullscreen request is already active.
- A successful browser fullscreen request targets `unity-container` and attempts a
  landscape orientation lock.
- The installed PWA declares landscape as its preferred orientation in the manifest.
- An eligible Android browser visit shows the in-page install card while the app is
  not installed; the native browser prompt opens only after the player taps Install.
- Dismissing the install card lasts only for the current page visit.
- Browser and operating-system accessibility or orientation restrictions take priority.

## State transitions

| Current state | Input | Result |
|---|---|---|
| Browser windowed | Login/actor pointer gesture | Continue without requesting fullscreen |
| Browser windowed | Settings fullscreen toggle | Request the complete Unity container fullscreen |
| Fullscreen request pending | Another Settings toggle | Ignore the duplicate request |
| Browser fullscreen | Login/actor pointer gesture | Continue without a display-mode change |
| Browser denies request | Settings fullscreen toggle | Remain windowed and report the rejection |
| Fullscreen exited by user | Settings fullscreen toggle | A new fullscreen request is allowed |

## Five-component evaluation

- Clarity: fullscreen is coupled only to the labeled Settings control.
- Motivation: immersive mode remains available without surprising the player.
- Response: fullscreen failure never consumes or suppresses login/combat input.
- Satisfaction: installed mode removes browser chrome and prefers the game layout.
- Fit: landscape matches the authored 1920x1080 presentation.

## Risks and abuse cases

- iOS or an accessibility configuration may ignore orientation locking.
- Device rotation lock may override the PWA preference.
- Installing a hash-prefixed Pages preview creates a separate origin and storage silo.
- Repeated taps during a pending request must not create multiple browser prompts.

## Playtest scenarios

1. Open the stable production URL on Android Chrome, tap Enter and both actors,
   and confirm the browser remains windowed.
2. Use the Settings toggle; confirm the game and question-video layer share the
   same fullscreen surface.
3. Exit fullscreen and tap player/enemy rapidly; confirm no fullscreen request and
   no duplicated gameplay command.
4. Deny fullscreen permission from Settings; confirm the game remains interactive.
5. Readability: install from the stable production hostname and launch while holding
   the phone in portrait; expect landscape where supported, otherwise a usable responsive view.
6. Persistence: close and reopen the installed app; expect account storage to remain
   available on the same production origin.
7. Upgrade: install the previous release, deploy the `shell-v4` worker, and reopen
   online; expect the old build cache to be deleted and the new WebGL binary to load.
8. Existing account with an empty/legacy locale: temporarily reject or conflict the
   locale PATCH; expect the authenticated player to continue into preparation or the
   main menu while the precise save failure is written to diagnostics.
9. Android install prompt: visit the stable production URL with Chrome while the PWA
   is not installed; expect the branded install card on every fresh page visit. Tap
   Not now, reload, and expect it again. Tap Install and expect Chrome's native prompt.
10. Installed detection: launch the installed PWA and confirm the install card does
    not appear. Uninstall it, revisit the stable URL, and confirm it becomes eligible
    again after Chrome emits `beforeinstallprompt`.
11. Icon fidelity: Add to Home Screen on Android Chrome and older iOS Safari; confirm the
    custom MathWorld icon appears on the device home screen without transparent corner blackouts,
    generic browser badges, or screenshot thumbnails.
12. Social share preview: Share the production URL in iOS/Android native share sheets, LINE,
    WhatsApp, or Discord; confirm the preview card displays the 512px MathWorld icon, product
    title, and description.
