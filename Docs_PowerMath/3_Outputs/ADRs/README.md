# Architecture Decision Records Index

## Dependency Order

```mermaid
graph TD
    ADR001["ADR-001: Web-Owned Authentication and Server-Owned Player Data"]
    ADR002["ADR-002: Unity-Owned Custom Authentication"]
    ADR003["ADR-003: Direct Firestore Prototype Authentication"]
    ADR004["ADR-004: Combat Runtime Boundary and Development Simulation"]
    ADR005["ADR-005: Atomic Academic Progression and Question Catalog Boundary"]
    ADR006["ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary"]
    ADR007["ADR-007: Direct Firestore Social Projection and Private Analytics Boundary"]
    ADR008["ADR-008: Atomic Run Settlement and Weapon Ascension"]
    ADR009["ADR-009: Data-Driven Stage Map and Encounter Runtime"]
    ADR010["ADR-010: Direct-Firestore Pet Gacha Prototype"]
    ADR011["ADR-011: Version Control, Browser Cache Management, and Data Migration"]
    ADR012["ADR-012: Persisted Combat Presentation Receipts and Orchestration"]
    ADR013["ADR-013: Canonical Pet Definitions, Loadout, and Shared Main Menu Feedback"]
    ADR014["ADR-014: Scene-Scoped UI Composition and Shared Motion"]
    ADR015["ADR-015: Isolated Question Fallback Practice Session"]
    ADR002 -- "supersedes" --> ADR001
    ADR003 -- "supersedes" --> ADR002
    ADR004 -- "extends prototype composition" --> ADR003
    ADR005 -- "extends attempt authority" --> ADR004
    ADR006 -. "proposed schema/live adapter supersession" .-> ADR003
    ADR006 -. "proposed live persistence extension" .-> ADR005
    ADR007 -- "extends public/private prototype data" --> ADR006
    ADR008 -- "extends atomic persistence/economy" --> ADR006
    ADR008 -- "updates public weapon/reset projection" --> ADR007
    ADR009 -- "replaces single-enemy run ownership" --> ADR004
    ADR009 -- "adds separate Event question path" --> ADR005
    ADR009 -- "extends encounter persistence" --> ADR006
    ADR009 -- "preserves run settlement contract" --> ADR008
    ADR010 -- "extends direct persistence" --> ADR006
    ADR010 -- "shares atomic Power Coin boundary" --> ADR008
    ADR011 -- "extends bootstrap & persistence" --> ADR003
    ADR011 -- "protects schema hydration" --> ADR006
    ADR012 -- "extends combat presentation completion" --> ADR004
    ADR012 -- "extends live presentation receipt persistence" --> ADR006
    ADR012 -- "extends settlement acknowledgement" --> ADR008
    ADR012 -- "reuses encounter identity for recovery" --> ADR009
    ADR012 -- "requires schema V2 migration" --> ADR011
    ADR013 -- "extends private loadout persistence" --> ADR006
    ADR013 -- "extends shared stat and weapon presentation" --> ADR008
    ADR013 -- "canonicalizes pet content and ownership" --> ADR010
    ADR013 -- "keeps feedback subordinate to receipts" --> ADR012
    ADR014 -. "proposed shared presentation foundation" .-> ADR012
    ADR014 -. "proposed consolidation of shared UI feedback" .-> ADR013
    ADR015 -- "supersedes real-save fallback only" --> ADR006
    ADR015 -- "protects pending presentation artifacts" --> ADR012
```

Update this diagram as ADRs are added. Show which decisions depend on others.

## ADR Registry

| # | Title | Status | Phase | Current Note |
|---|---|---|---|---|
| [001](001-web-unity-session-and-player-data-boundary.md) | Web-Owned Authentication and Server-Owned Player Data | **Superseded by ADR-002** | Data architecture | Launch-code source removed during ADR-003 cutover |
| [002](002-unity-owned-custom-authentication.md) | Unity-Owned Custom Authentication with a Hosting-Adjacent REST Boundary | **Superseded by ADR-003** | Authentication migration | Host Function rejected by the team |
| [003](003-direct-firestore-prototype-authentication.md) | Direct Firestore Prototype Authentication | **Partially superseded by ADR-006** | Prototype authentication | Risk acceptance retained; schema/path decision replaced |
| [004](004-combat-runtime-boundary-and-development-simulation.md) | Combat Runtime Boundary and Development Simulation | **Accepted** | Stage combat prototype | Response Score now applies an explicit 20%-200% final damage multiplier |
| [005](005-atomic-academic-progression-and-question-catalog-boundary.md) | Atomic Academic Progression and Question Catalog Boundary | **Partially superseded by ADR-006** | Academic progression infrastructure | Atomic domain retained; no-live-adapter restriction replaced |
| [006](006-direct-firestore-player-question-and-youtube-boundary.md) | Direct Firestore Player/Question and Embedded YouTube Boundary | **Partially superseded by ADR-015** | Live prototype data/content | Direct GET/PATCH, live Rank catalog, embedded YouTube |
| [007](007-direct-firestore-social-profile-projection.md) | Direct Firestore Social Projection and Private Analytics Boundary | **Accepted** | Social/profile data | One-read cohort projection, private owner analytics, manual refresh |
| [008](008-atomic-run-settlement-and-weapon-ascension.md) | Atomic Run Settlement and Weapon Ascension | **Accepted** | Run reset/economy | Shared stat projection, Player Hub, and structured Rebirth preview implemented |
| [009](009-data-driven-stage-map-and-encounter-runtime.md) | Data-Driven Stage Map and Encounter Runtime | **Accepted** | Stage/encounter infrastructure | Architecture approved; implementation in progress |
| [010](010-direct-firestore-pet-gacha-prototype.md) | Direct-Firestore Pet Gacha Prototype | **Accepted** | Pet ownership/economy | Client RNG limitation accepted for prototype; production catalog pending |
| [011](011-version-control-cache-management-and-data-migration.md) | Version Control, Browser Cache Management, and Data Migration | **Accepted** | WebGL / Cache & Migration | Three-tier version manifest, Cloudflare headers, JSLib cache purge, schema migrator |
| [012](012-persisted-combat-presentation-receipts-and-orchestration.md) | Persisted Combat Presentation Receipts and Orchestration | **Accepted** | Combat presentation infrastructure | Versioned attempt/settlement receipts, shared interaction gate, refresh-safe Death/Rebirth replay |
| [013](013-canonical-pet-definitions-loadout-and-shared-main-menu-feedback.md) | Canonical Pet Definitions, Loadout, and Shared Main Menu Feedback | **Accepted** | Player Hub data and presentation | Canonical pet assets, idempotent equip, shared utility/notification/FX overlay |
| [014](014-scene-scoped-ui-composition-and-shared-motion.md) | Scene-Scoped UI Composition and Shared Motion | **Accepted** | UI architecture | Additive foundation and Main Menu pilot approved on 2026-08-29 |
| [015](015-isolated-question-fallback-practice-session.md) | Isolated Question Fallback Practice Session | **Accepted** | Question fallback resilience | Bundled fallback is local-only and preserves Firebase artifacts |

## Phase Mapping

| GDD Phase | ADRs | Current Status |
|---|---|---|
| **Phase 1** - Stabilize Prototype | ADR-001, ADR-002, ADR-003 | ADR-003 is current |
| **Authentication migration** | ADR-003 | Accepted; direct Firestore refactor in implementation |
| **Phase 2** - Split Core Runtime | ADR-001 | Ready for task breakdown |
| **Phase 3** - Data-Drive Content | ADR-001 | Not started |

## How to Use

1. **Before making an architecture decision** - Check this index for existing decisions.
2. **To create a new ADR** - Copy `Docs_PowerMath/1_Inputs_Templates/ADR_TEMPLATE.md` and name it `{NNN}-{kebab-case-title}.md`.
3. **After creating** - Add a row to the registry table above.
4. **When superseding** - Update the old ADR's status to `Superseded by ADR-{NNN}`.
