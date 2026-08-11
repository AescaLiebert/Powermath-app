# ADR-009: Data-Driven Stage Map and Encounter Runtime

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-11 |
| Author | Architect agent |
| GDD Section | `@tag:stage-progression`, `@tag:encounters`, `@tag:server-authority`, `@tag:feedback` |
| Extends | ADR-004 combat runtime; ADR-005 academic atomicity; ADR-006 direct Firestore persistence; ADR-008 run settlement |

## Context

The current run engine assumes one enemy definition for all 200 Stages and one Rank-audit question path for every attempt. The approved design requires seven biome monster/background sets, protected Mini/Big/Final Boss Stages, deterministic saved encounter selection, and independent Events using separate question documents without default Rank-audit mutation.

Adding Events as fake enemies or placing biome switches in UI would duplicate Stage authority, enable reconnect rerolls, and couple future Slot/RNG mechanics to monster-only fields.

## Decision

Accepted by the product owner on 2026-08-11 (`LGTM`).

- Add one validated `StageMapDefinition` root referencing seven `BiomeDefinition` assets, monsters/bosses, and fixed MVP Event bindings.
- Map ScriptableObjects once into immutable Unity-free `StageMapData`; core runtime never reads authoring assets directly.
- Resolve Stage type with strict priority: Stage 200 Final Boss, `%30` Big Boss, `%5` Mini-Boss, otherwise Normal/Event candidate.
- Replace single-definition `LocalCombatEngine` ownership with `LocalRunEncounterEngine`, the sole owner of Stage, hearts, encounter, HP/cooldown/Event state, and Stage advancement.
- Select normal identity/HP variation using a stable run ID/catalog/Stage hash, then persist the encounter snapshot atomically with Stage advancement.
- Keep normal HP independent of monster art/name; apply boss modifiers after the global Stage HP curve.
- Model Events with `EventDefinition` and tagged Event runtime/results, never fake `MonsterDefinition` values.
- Generalize transactions into explicit Rank-question and Event-question paths; Event questions do not mutate Rank audit by default.
- Generate one attempt ID at commit and reuse it across all save points and receipts.
- Load validated Event question documents from the existing shared question-project configuration.
- Generalize private active-run persistence to canonical encounter fields with a legacy enemy-field fallback.
- Persist next Stage/encounter before cosmetic biome transition; presentation never owns progression/rewards.
- Keep the pseudo-map read-only and derived from local map data plus saved current Stage.

## Alternatives Considered

| Alternative | Advantage | Rejected because |
| --- | --- | --- |
| Put biome switches in `CombatLobbyPresenter` | Small initial change | Makes UI a second Stage/encounter authority |
| Add sprite arrays to current `EnemyDefinition` | Minimal classes | Cannot express fixed bosses, Events, map landmarks, or route validation |
| Treat Challenge as a 1-HP Monster | Reuses combat | Requires fake cooldown/critical/Rank behavior |
| Persist only Stage and reroll on load | Smaller schema | Enables refresh rerolls and content-drift changes |
| Store full ScriptableObject data in Firestore | Exact recovery | Duplicates local content and increases migration/payload cost |
| Random Event chance for MVP | More surprise | Hurts classroom reproduction, tuning, and E2E before economy is known |
| Separate Event scene | Strong isolation | Breaks the requested same Attack-entry flow and adds scene complexity |

## Consequences

### Positive

- Stage/biome/boss/Event rules have one deterministic source.
- Random monster identity cannot reroll through refresh or change HP difficulty.
- Future Event handlers can differ without polluting monster data.
- Harder Event questions remain measurable without unfair Rank changes.
- Map/transitions remain presentation consumers rather than progression authority.
- Stable attempt IDs strengthen standard-attempt idempotency too.

### Negative / Trade-offs

- Significant refactor of snapshots, transactions, presenter branching, and persistence mapping.
- Active-run schema grows and temporarily synchronizes legacy enemy fields.
- Event preload adds one document read per unique Event question document per Main Menu session.
- Live combat requires a complete validated seven-biome content catalog.
- Mini-Boss reuse and final art remain content decisions.
- Anonymous direct Firestore remains prototype-only authority.

## Migration

1. Add canonical encounter fields without deleting legacy enemy leaves.
2. Map existing saved enemy state as Monster when canonical fields are absent.
3. Write canonical plus legacy Monster leaves for one compatibility release; Events use canonical fields.
4. Replace per-save transaction IDs with the committed attempt's stable ID.
5. Keep leaderboard schema unchanged.

## Related

- ADR-004: Combat Runtime Boundary and Development Simulation.
- ADR-005: Atomic Academic Progression and Question Catalog Boundary.
- ADR-006: Direct Firestore Player, Question, and YouTube Boundary.
- ADR-008: Atomic Run Settlement and Weapon Ascension.
- `Docs_PowerMath/3_Outputs/Specs/stage-map-biomes-enemies-events-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/stage-map-biomes-enemies-events-arch-plan.md`
