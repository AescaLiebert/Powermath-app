# DevLog: 2026-08-12 - Stage Map Authoring Repair

## Goal

Repair the Main Menu UI contract and make the approved Stage/Biome/Enemy/Event
ScriptableObject architecture usable from Unity.

## What I Did

- [x] Restored the missing Main Menu Stage, progress, and wallet UI elements.
- [x] Split `BiomeDefinition` and `EventDefinition` into Unity-compatible files.
- [x] Repaired the missing-script references on the existing Biome and Event assets.
- [x] Authored a valid seven-biome Stage 1-200 prototype catalog.
- [x] Added normal, miniboss, big-boss, final-boss, and fixed Event references.
- [x] Made `CombatLobbyCompositionRoot` load the root Stage Map from Resources.
- [x] Added an Inspector authoring guide.

## Key Decisions

- Reused one miniboss definition across matching protected Stages within each biome,
  as permitted by the approved architecture.
- Kept sprites empty as explicit art placeholders instead of inventing final assets.
- Preserved the code-defined development map as a missing-resource fallback.

## Bugs Found

- [x] `MainMenuView` required three UXML names that were absent.
- [x] Multiple public ScriptableObject types in `StageMapDefinition.cs` produced
  missing-script Biome and Event assets.
- [x] The root map asset had no biomes or events and was never loaded at runtime.

## Game Feel Notes

The Main Menu now exposes player Stage and wallet information before combat. Biome and
encounter artwork remains an authoring task; runtime currently uses safe visual fallbacks.

## Verification

- Unity-generated Roslyn configurations compiled Combat Unity and Assembly-CSharp.
- Asset validation covered seven continuous biomes, all 40 protected boss bindings,
  Stage 200 Final Boss, and resolvable Resource references.
- `git diff --check` passed for the repaired files.

## Next Session

- Assign final biome, landmark, monster, boss, and Event sprites.
- Human-playtest Stage boundaries and the Stage 7 Event after Unity reimports assets.
