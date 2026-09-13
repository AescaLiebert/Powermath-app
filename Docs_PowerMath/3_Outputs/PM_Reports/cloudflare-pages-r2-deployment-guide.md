# MathWorld WebGL Deployment Guide: Cloudflare Pages + R2

## Deployment layout

- Cloudflare Pages serves the PWA shell, release policy, icons, and service worker.
- Cloudflare R2 serves the large Unity `Build/` and `StreamingAssets/` files.
- `MathWorld_BurstDebugInformation_DoNotShip/` is never deployed.

The project WebGL template already points Unity content requests to:

```text
https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev
```

For production, replace this development URL with an R2 custom domain.

## 1. Build from Unity

1. Open `File > Build Profiles`.
2. Select the Web profile.
3. Confirm the Web Template is `PROJECT:MathWorldPWA`.
4. Build to a new empty folder, for example `MathWorld Pre-Alpha 1.0`.
5. Confirm Unity reports that the WebGL Cloudflare postprocessor copied
   `version.json` and `_headers` into the output.

## 2. Upload large content to R2

Upload these local folders while preserving their names and internal paths:

```text
Build/
StreamingAssets/
```

They must become these R2 object paths:

```text
Build/<Unity build files>
StreamingAssets/<streaming asset files>
```

Do not upload `MathWorld_BurstDebugInformation_DoNotShip/`.

Verify these URLs return HTTP 200 before deploying Pages:

```text
https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/Build/MathWorld%20Pre-Alpha%201.0.loader.js
https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/Build/MathWorld%20Pre-Alpha%201.0.data
https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/Build/MathWorld%20Pre-Alpha%201.0.framework.js
https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/Build/MathWorld%20Pre-Alpha%201.0.wasm
```

## 3. Prepare the Pages upload

From the PowerMath repository, run:

```powershell
.\Tools\Prepare-CloudflarePagesPackage.ps1 `
  -BuildRoot "E:\My Project\Power Wisdom Project\PowerMath app Project\MathWorld Pre-Alpha 1.0"
```

The script creates a sibling folder named:

```text
MathWorld Pre-Alpha 1.0-PagesUpload
```

It includes only:

```text
index.html
version.json
_headers
manifest.webmanifest
ServiceWorker.js
TemplateData/
```

## 4. Deploy the Pages shell

1. Open the Cloudflare dashboard.
2. Open `Workers & Pages`, then the `wisdom-mentalgame` Pages project.
3. Create a new deployment using direct upload.
4. Upload the contents of the generated `-PagesUpload` folder.
5. Do not upload the large `Build/` or `StreamingAssets/` folders to Pages.
6. Promote and install from the stable production hostname. Do not install a hash-prefixed preview deployment, because each preview hostname has separate PWA storage and credentials.

## 5. Verify the release

Open the new Pages deployment URL and verify:

```text
https://<deployment>.wisdom-mentalgame.pages.dev/version.json
```

Required result:

- HTTP status `200`.
- Content type contains `application/json`.
- The response body starts with `clientVersion`, `minSupportedVersion`, and
  `schemaVersion`; it must not return `index.html`.

Then verify:

1. The loading screen reaches 100%.
2. An existing account can log in.
3. An account without game data reaches player preparation/default creation.
4. Add to Home Screen uses the MathWorld icon.
5. The installed app launches in landscape where the browser and operating system support the manifest orientation preference.

Also open the stable production worker directly:

```text
https://wisdom-mentalgame.pages.dev/ServiceWorker.js
```

It must contain `-shell-v3` and must not list `Build/*.data`, `Build/*.wasm`, or
other Unity build files in `contentToCache`. If it still contains
`PowerWisdom-MathWorld-1.0` with the Unity build files, Pages is serving the old
deployment and an installed app can remain pinned to an old WASM file.

## Installed-app cache recovery

After the corrected Pages deployment is live:

1. Close the installed MathWorld app completely.
2. Uninstall the existing MathWorld PWA.
3. In Chrome, clear site data for `wisdom-mentalgame.pages.dev`.
4. Open `https://wisdom-mentalgame.pages.dev/` (not a hash-prefixed preview URL).
5. Load it once online, close and reopen Chrome, then install it again.
6. Test the existing Firebase account first with the normal Chrome tab closed so
   another session cannot update the same player revision during bootstrap.
