# DevLog — Pet Gacha Catalog Design — 2026-08-13

## Outcome

Created a prototype catalog design plan for the approved pet gacha system. The proposal defines three rarity tiers, 15 pseudo-authored pets, starting category rates, probability examples, accessibility requirements, future role directions, risks, and playtests.

## Decisions Preserved

- Pull cost remains 25 Power Coins.
- Ownership weighting and empty duplicates remain unchanged from `@tag:gacha`.
- The first implementation remains ownership-only.
- Pet equip/loadout and combat stat values remain a separate design and implementation slice.
- All catalog numbers remain starting values pending human approval and playtest evidence.

## Artifact

- `Docs_PowerMath/3_Outputs/Specs/pet-gacha-catalog-design-plan.md`

## Next Checkpoint

The project owner authorized implementation on 2026-08-13. `prototype-v1` is now authored as `Assets/Project/Resources/PetGachaCatalog.asset` with the approved rates, roster, and stable IDs. All 15 pets temporarily share the existing project attack sprite so catalog validation remains fail-closed until distinct art is approved.

An EditMode asset contract test verifies that Unity can load and validate the catalog, the tier rates remain 7000/2700/300 basis points, every tier contains five pets, all 15 stable IDs retain their approved ordering, and representative entries resolve a non-null placeholder icon.
