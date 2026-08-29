# ADR-011: Version Control, Browser Cache Management, and Client Data Migration

| Field       | Value                    |
|-------------|--------------------------|
| Status      | **Accepted**             |
| Date        | 2026-08-28               |
| Author      | Antigravity Orchestrator |
| GDD Section | Platform Architecture & WebGL Delivery |

## Context

Power!-Math is deployed as a Unity WebGL application on Cloudflare Edge CDN backed by Firestore. As new features, stage maps, and balance updates are published, browsers can serve stale `.wasm` or asset files due to aggressive HTTP and IndexedDB caching. Furthermore, schema additions to `PlayerSnapshot` require forward migration so existing student accounts are updated cleanly without data loss or downtime.

## Decision

We establish a 3-part system for version negotiation, cache invalidation, and data migration:

1. **Version Negotiation (`version.json`)**:
   - Host `version.json` on Cloudflare with `Cache-Control: no-cache, no-store, must-revalidate`.
   - Client fetches this manifest during `GameBootstrapper` before player authentication.
   - Evaluates `minSupportedVersion`, `clientVersion`, `schemaVersion`, and `maintenance.isActive`.

2. **CDN & WebGL Browser Cache Invalidation**:
   - Cloudflare `_headers` configure immutable caching (`max-age=31536000, immutable`) for hashed build files (`/Build/*.unityweb`) and strictly no-cache for entry points (`index.html`, `version.json`, `sw.js`).
   - WebGL bridge (`PowerMathWebBridge.jslib` & `WebCacheBridge.cs`) provides `PowerMathPurgeCacheAndReload()` to clear IndexedDB `UnityCache` and Cache Storage when a hard update is required.

3. **Client User Data Migration Pipeline**:
   - `PlayerSchemaMigrator` provides sequential forward migrations (`MigrateV0ToV1`, etc.) and default field baseline guarantees.
   - `PlayerSessionStore.TryHydrate` accepts schemas where `schemaVersion <= SupportedSchemaVersion`, running automatic forward migrations.

## Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| **A: Full CDN Purge on Every Release** | Simple | Causes massive bandwidth spikes; doesn't clear client IndexedDB `UnityCache`. |
| **B: Enforced App Store / Package Updates Only** | Traditional | Not applicable to WebGL browsers. |
| **C: Three-Tier Version Manifest + JSLib Purge + Schema Migrator (Selected)** | Zero-downtime, granular control, protects student save data | Requires maintaining `version.json` in deploy pipeline. |

## Consequences

### Positive
- Prevents stale cache crashes and WebGL version mismatch.
- Allows instant emergency maintenance banners without code deployment.
- Seamlessly migrates student profiles to new data schemas upon login.

### Negative / Trade-offs
- An initial HTTP request to `version.json` is added to the bootstrap sequence (mitigated with 3-second timeout and cache-busting timestamp).

### Migration
- Added `WebCacheBridge.cs`, `GameVersionChecker.cs`, `GameVersionManifest.cs`, and `PlayerSchemaMigrator.cs`.
- Updated `GameBootstrapper.cs`, `BootstrapView.cs`, and `BootstrapState.cs`.
- Created `Cloudflare/public/_headers` and `Cloudflare/public/version.json`.

## Related
- ADR-003, ADR-006, ADR-008
- Scripts affected: `GameBootstrapper.cs`, `BootstrapView.cs`, `PlayerSessionStore.cs`, `GameApiSettings.cs`, `PowerMathWebBridge.jslib`
