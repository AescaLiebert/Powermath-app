# Math:World Scene Art Adaptation — v11

## Status and scope

This set adapts Authentication, Loading, Player Hub, Main Menu, and Rebirth into one visual system.

- The corrected Wayfinder heroine from v10 is the character identity anchor.
- The red-haired boy and teal-haired girl are **provisional gang concepts**, created only because approved companion designs were not supplied. They are not canon until approved.
- The geometric Animo are **non-canon visual placeholders**. They demonstrate how shape-driven cartoony pets can stand beside smooth anime characters; replace them with approved pet assets later.
- Supplied illustrations are used as composition and art-style references only. Their characters, pets, clothing, logos, and worlds are not project assets.

## Core rendering system

### Hierarchy first

Every illustration assigns detail, contrast, edge sharpness, gradient complexity, and highlight count according to narrative priority.

1. Primary face, gesture, or gameplay decision.
2. Directly related UI or interaction object.
3. Supporting character or Animo that continues the action flow.
4. Environment that establishes place.
5. Distant decoration.

Lower-priority elements must not receive the same eye detail, hair separation, contour strength, material highlights, or texture density as the lead.

### Characters

- Human characters use 5.5–6-head proportions, thin local-color contours, sparse interior linework, and shape-driven costume masses.
- The lead receives the clearest eyes, brows, mouth, hands, compass accessory, and selective hair strands.
- Companions use fewer interior lines, fewer gradients, softer edges, and slightly reduced contrast as they recede.
- Use one broad hard shadow and one soft transition. Additional gradients are reserved for face, front hair, major fabric rolls, and one metal focal object.
- AO is lightly saturated cyan-violet in cool overlaps and muted peach in warm overlaps.
- Use narrow highlight bands rather than rim-lighting the whole silhouette.

### Animo

- Animo remain more shape-driven and cartoony: unmistakable silhouette, two or three color masses, slightly firmer colored contour, minimal interior marks, and one simple shadow.
- They share the scene's key light, AO hue, and atmospheric falloff so they do not look pasted beside the anime characters.
- Closer story-critical Animo may receive eye gradients and one highlight; distant Animo lose those refinements.

### Environments

- Use broad grouped forms and readable landmarks rather than realistic microtexture or low-polygon shorthand.
- Authentication and Loading may use full-CG depth and perspective, but the environment remains lower contrast than the cast.
- Avoid deep shadows, excessive bloom, particle haze, sharp detail across the entire frame, and blur that erases location identity.

## Authentication

### Narrative hierarchy

1. Lead heroine's face and foreshortened reaching hand.
2. Stable centered sign-in panel and small Math:World logo.
3. Red-haired companion returning his gaze toward the heroine.
4. Teal-haired companion pointing toward the destination below.
5. Geometric Animo extending the characters' motion arcs.
6. Rounded archipelago and academy.

The cast forms an S-shaped circular flow around the panel. No person or Animo exists merely to fill a corner.

### Composition

- Extreme wide-angle sky-dive over a curved world.
- Heroine and red-haired companion occupy opposite foreground diagonals.
- Teal-haired companion anchors the upper-middle distance.
- The sign-in panel remains readable and visually stable while the illustration moves around it.

## Loading

### Narrative hierarchy

1. Heroine's hand and the final empty puzzle socket.
2. Heroine's focused expression and correct cyan tile.
3. Teal companion verifying the symbol order.
4. Red companion reacting to the wrong tile.
5. Four Animo carrying, projecting, or removing specific pieces.
6. Academy garden.

All gazes, hands, ribbons, projected symbols, and moving pieces converge on the compass board. Remove any prop that does not explain or advance the puzzle relay.

## Player Hub

- The heroine, equipped Animo, and weapon stand directly on the wardrobe-room floor outside all panels.
- Stats are separate lightweight chips, not a summary card.
- The wardrobe remains readable through the mirror, garment rack, boot shelf, weapon stand, folded fabric, window light, and floor inlay.
- Use only mild atmospheric softening; do not blur the room into a generic backdrop.
- Inventory and preview share one workspace. Selecting a pet card equips immediately through the green outline/check state; no Equip button.
- PETS and WEAPON remain the only major tabs.

## Main Menu

### Priority

1. Combat status and player HP.
2. Heroine-versus-Stoneward silhouette and gaze.
3. Equipped dashboard state and navigation.
4. Meadow and ruins.

The battlefield contains only the heroine and one golem. The equipped Animo appears in the dashboard portrait rather than competing inside the combat focal zone.

The meadow uses grouped flowers, broad tree masses, readable rocks, path, hills, and ruin arches. It must feel inhabited without realistic grass density or uniform sharpness.

## Rebirth

- Use one warm-cream comparison overlay rather than multiple nested frames.
- Preserve the live battlefield behind a uniform translucent scrim with almost no blur.
- Align sword, stage, and coin comparisons into three shared rows.
- Use coral only for the irreversible reset warning; reserve the strongest coral-to-gold surface for the primary action.
- Keep warning copy to `STAGE RESET`; the before/after stage row already communicates the consequence.
- Avoid a separate glossy header frame, repeated paw motifs, ornamental corners, and dark-blue framing around every section.

## Validation checklist

- Squint: can the primary face/gesture or decision be found immediately?
- Eye-flow: does every companion, pet, prop, and motion line lead to the narrative focal point?
- Edge hierarchy: are background and distant-character edges softer and less numerous than the lead?
- Gradient hierarchy: are the richest gradients concentrated on lead faces, hands, and interaction objects?
- Shadow test: does each form read with one hard shadow and one soft transition without deep black regions?
- Pet integration: do Animo share the same light and AO while keeping simpler shape-driven rendering?
- Player Hub location: can the wardrobe still be identified when the UI is ignored?
- Main Menu clarity: can HP, enemy state, heroine, and golem be understood before noticing scenery?
- Rebirth decision: can the player explain every before/after consequence without reading a paragraph?

## Built-in image-generation prompt set

### Authentication

16:9 extreme curved-world sky-dive Authentication scene. Preserve the corrected Wayfinder heroine at 5.5–6 heads in the left foreground, reaching toward camera; provisional red-haired boy opposes her on the right; provisional teal-haired girl points toward the academy from upper-middle distance. Geometric Animo continue an S-shaped motion loop around a stable centered sign-in panel and small Math:World logo. Use shape-driven almost-vector forms, soft colored contours, gradient-led volume, one hard shadow plus one soft transition, lightly saturated AO, selective facial detail, and progressively lower detail toward pets and world.

### Loading

16:9 low wide-angle academy-garden puzzle relay. Heroine places the final cyan diamond into a compass board; teal companion verifies a three-symbol ribbon-map; red companion catches the wrong star tile; four geometric Animo each carry, project, or remove one purposeful tile. Every gaze and gesture converges on the final socket. Use full-CG depth with grouped scenery, strong detail hierarchy, thin colored contours, gradient-led shader, hard-plus-soft shadow, and minimal bottom loading UI.

### Player Hub

16:9 wardrobe-room PETS tab. The corrected heroine, equipped geometric Animo, and sheathed sword stand directly on a readable inlaid floor outside all panels. Show mirror, arched window, garment rack, boots, weapon stand, shelves, and fabrics with mild atmospheric softening. Use separate ATK/CR/CD chips and equipped-slot chips. Right side contains one cream workspace with PETS/WEAPON tabs, 3×3 inventory grid, selected green outline/check, preview, rarity, and two concise ability rows. No player-summary frame or Equip button.

### Main Menu

16:9 combat screen preserving NOVA profile, STAGE 12, STONEWARD 850 / 1000, navigation, HUB/GACHA/REBIRTH, and dashboard HP 780 / 1000 with equipped Animo, sword, potion 3, leaf 2, and two empty slots. Corrected rear-view heroine faces one mossy block golem. Use a medium-detail meadow with grouped forms and lower contrast. Apply thin local-color heroine contours, gradient-led volume, restrained shadows/highlights, simpler golem planes, and flat-elegant cream UI.

### Rebirth

16:9 centered warm-cream decision overlay over the mildly dimmed live Main Menu. Show REBIRTH title, sword 25 → 31, stage 3 → 1 with caution, coin 0 → 12, progress 3 / 50, compact `STAGE RESET`, and one coral-to-gold REBIRTH button. Use one shared comparison surface, thin icons, minimal shadow, no glossy blue outer frame, repeated decorative patterns, heavy blur, or paragraph warning.
