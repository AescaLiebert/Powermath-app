# figma-codex-plugin

Local, user-initiated bridge between Codex and the currently open Figma Design
file. Canvas operations are limited to the whitelisted commands implemented in
`code.ts`; the bridge never evaluates arbitrary JavaScript.

## Setup

1. Install dependencies and compile the plugin:

   ```powershell
   cd "Figma\figma-codex-plugin"
   npm install
   npm run build
   ```

2. Start the local bridge with a private token:

   ```powershell
   $env:POWER_MATH_FIGMA_TOKEN = "replace-with-a-long-random-secret"
   npm run bridge
   ```

3. In the Figma desktop app, open the target Design file and run
   **Plugins → Development → figma-codex-plugin**.

4. Paste the same token into the plugin window and select **Connect**. Keep the
   plugin window open while commands are running.

## Smoke test

In a second PowerShell terminal:

```powershell
$token = "replace-with-a-long-random-secret"
$headers = @{ Authorization = "Bearer $token" }

$job = Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:3847/jobs" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body (@{ type = "ping" } | ConvertTo-Json)

Start-Sleep -Milliseconds 750

Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:3847/jobs/$($job.id)" `
  -Headers $headers
```

The result should contain the open Figma file key, current page ID, page name,
and selection count.

## Supported commands

- `ping`
- `inspect-selection`
- `inspect-node`
- `create-frame`
- `create-text`
- `set-properties`
- `set-all-text-fonts`
- `set-progress`
- `rebuild-main-menu-ui`
- `polish-main-menu-icons`
- `flatten-main-menu-icons`
- `design-main-menu-vector-icons`
- `build-player-hub-ui`
- `export-node-preview`
- `restyle-player-hub-wireframe`
- `recompose-player-hub-reference-v16`
- `build-rebirth-wireframe`
- `finalize-player-hub-interactions`

Example frame command:

```json
{
  "type": "create-frame",
  "name": "Bridge Test",
  "width": 400,
  "height": 240,
  "x": 200,
  "y": 200,
  "fill": "#DCEEFF"
}
```

## Security

- The server listens only on the local `localhost` interface.
- Every request requires the bearer token.
- Request bodies are capped at 1 MB.
- The pending queue is capped at 100 commands.
- Commands are explicitly whitelisted; there is no `eval` or raw-script API.
- Delete operations are intentionally not implemented.
