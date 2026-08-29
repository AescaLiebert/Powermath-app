# ADR-013: Canonical Pet Definitions, Loadout, and Shared Main Menu Feedback

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-29 |
| Author | Architect agent |
| GDD Section | `@tag:core-loop`, `@tag:combat-stats`, `@tag:gacha`, `@tag:server-authority`, `@tag:feedback` |
| Extends | ADR-006, ADR-008, ADR-010, ADR-012 |

> Accepted by the project owner on 2026-08-29 (`approve architecture`).

## Context

Pet Gacha already persists permanent pet ownership, but pet identity and presentation are nested in the gacha catalog, Player Hub cannot equip pets, and the shared stat projection intentionally contributes zero Pet ATK. Player Hub also needs visual-first equipment presentation, reusable destination utility controls, universal semantic notifications, and data-driven interaction feedback without adding another animation dependency.

## Decision

- Create canonical `PetDefinition` ScriptableObject assets for identity, icon/preview, approved Pet ATK, and rich-text presentation.
- Make `PetGachaCatalogDefinition` reference those assets while retaining rarity membership and probability rates.
- Keep `PlayerSnapshot.inventory` as the only persistent ownership collection and derive `PlayerOwnedPetInventory` from it.
- Persist equipped pet through `loadout.petId` using an idempotent direct-Firestore prototype command with update-time preconditions and a last-equip receipt.
- Resolve Pet ATK only when the equipped pet is owned and catalog-valid, through the existing shared player-stat projection.
- Keep production Pet ATK at zero until human-authored balance content is approved.
- Add one transient Main Menu overlay for Power Coins/back utility, semantic notifications, and pooled UI feedback.
- Reuse UI Toolkit transitions, the installed LeanTween layer, optional `AudioSource` cues, and Reduced Motion behavior. Do not add a second tween/notification dependency or custom `Update()` polling.
- Detect Weapon Ascend milestones from authored `WeaponAscensionCatalogDefinition` tier transitions and `milestoneFeedbackKey`, never from a second hard-coded milestone list.

## Alternatives Considered

| Option | Advantage | Rejected because |
|---|---|---|
| Keep pet data nested in gacha | Smallest diff | Duplicates or blocks inventory/stat/preview content reuse |
| Add a second pet inventory document | Pet-specific schema | Drifts from gacha ownership and creates migration/authority conflicts |
| Optimistically mutate saved loadout locally | Instant appearance | Presentation could claim persistence after a failed write |
| Add equip to the progression god-object | Reuses its PATCH helper | Expands an already broad store and couples pet validation to unrelated progression |
| New animation/notification package | More features | Adds dependency and build risk when UI Toolkit and LeanTween already cover the slice |
| Hard-code milestone levels in UI | Quick | Drifts from authored weapon tiers and feedback keys |

## Consequences

### Positive

- Gacha, inventory, preview, and stat projection share stable pet identity.
- Equip is responsive, recoverable, and cannot grant power from corrupt/unowned IDs.
- Weapon and pet feedback stay tunable without changing authority or formulas.
- Five Main Menu destinations share one currency/back and notification language.
- Missing audio or cancelled animation cannot block an acknowledged state change.

### Negative / Trade-offs

- Fifteen existing gacha entries require a one-time asset migration.
- The direct-client prototype remains tamperable until a trusted backend replaces it.
- A focused Pet Equip store duplicates a small amount of conditional GET/PATCH plumbing to avoid destabilizing existing stores.
- Immediate provisional equip requires explicit pending and rollback presentation.
- Non-zero Pet ATK, passives, stars, and elements remain content/design dependencies.

## Migration

1. Create 15 canonical pet assets preserving existing IDs, names, icons, rarity membership, and rates.
2. Add missing-safe equip receipt leaves without replacing existing inventory/loadout/economy values.
3. Wire catalog validation and stat resolution with production Pet ATK set to zero.
4. Add the shared overlay before hiding destination-local close controls.
5. Enable instant tile equip only after persistence, rollback, and UI tests pass.

## Related

- `Docs_PowerMath/3_Outputs/Specs/player-hub-data-polish-design-spec.md`
- `Docs_PowerMath/3_Outputs/Specs/player-hub-data-polish-arch-plan.md`
- ADR-006: Direct Firestore Player/Question and Embedded YouTube Boundary.
- ADR-008: Atomic Run Settlement and Weapon Ascension.
- ADR-010: Direct-Firestore Pet Gacha Prototype.
- ADR-012: Persisted Combat Presentation Receipts and Orchestration.
