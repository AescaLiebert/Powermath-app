# DevLog: 2026-08-12 - Configurable Base Weapon ATK

## Goal

Replace the fixed Level-0 Weapon ATK with one serialized combat-content setting.

## What I Did

- [x] Added `baseWeaponAttack` to `CombatRuntimeSettingsDefinition`.
- [x] Loaded `CombatRuntimeSettings.asset` automatically from Resources.
- [x] Applied the configured value to combat stats and the Weapon Ascension curve.
- [x] Applied the same value to upgrade previews and authoritative command responses.
- [x] Preserved the existing value of 5 as the serialized prototype default.

## Key Decision

The configured value is threaded through all Weapon-stat consumers rather than stored
as mutable global state. Changing the asset intentionally recalculates every Weapon
level from the same base on the next runtime initialization.

## Verification

- Combat Unity and Assembly-CSharp compiled with Unity's generated Roslyn configuration.
- Existing one-argument `GetStats` and four-argument combat-stat calls retain the value
  of 5 for compatibility.
