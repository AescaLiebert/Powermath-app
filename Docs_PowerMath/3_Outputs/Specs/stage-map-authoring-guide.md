# Stage Map Authoring Guide

GDD references: `@tag:stage-progression`, `@tag:encounters`, `@tag:server-authority`.

## Root Catalog

Edit `Assets/Project/Resources/StageMapDefinition.asset`. The combat composition root
loads this exact Resource automatically. If the reference is missing, runtime retains
the complete code-defined development map as a safe fallback.

The root owns global HP tuning, exactly seven ordered biome references, and fixed Event
bindings. Biome ranges must cover Stage 1 through Stage 200 continuously.

## Biomes

Each `BiomeDefinition` owns its display title, Stage range, background, map landmark,
normal-monster pool, and protected boss bindings. The supplied prototype ranges are
1-30, 31-60, 61-90, 91-120, 121-150, 151-180, and 181-200.

Assign artwork directly to `backgroundSprite` and `mapLandmarkSprite`. Add normal
monster assets only when their Encounter Kind is `NormalMonster` and their Biome ID
matches the owning biome.

## Enemies and Bosses

Edit the `EnemyDefinition` assets under `Assets/Project/Resources/StageMapContent`.
Normal monsters may be added to a biome pool. Boss assets may be reused at multiple
protected Stages within their biome.

- Every Stage divisible by 5 requires a `MiniBoss`, unless a higher-priority rule applies.
- Every Stage divisible by 30 requires a `BigBoss`.
- Stage 200 requires the `FinalBoss`.
- Encounter Kind and Biome ID must match the binding.

`hpMultiplierBasisPoints` uses 10000 as 100%, 15000 as 150%, and so on. These prototype
values come from the approved architecture and remain tuning inputs, not final balance.

## Events

Create Event assets through `Assets > Create > PowerMath > Stage Map > Event Definition`.
Bind Events only to ordinary Stages; protected boss Stages and Stage 200 reject Events.
`questionDocumentId` identifies the Event document in Question Firebase. If Question
Firebase is unavailable, the explicit development-question fallback serves the Event
while player progression continues to save to the player Firebase project.

## Validation

Selecting or editing the root catalog runs `StageMapDefinition.OnValidate`. Entering the
Main Menu maps and validates it again before combat begins. Invalid authoring reports a
specific Console error and does not become runtime encounter state.
