# Math:World Pet Gacha Scene Adaptation — v12

## Status and scope

This pass redraws Authentication, Loading, Player Hub, and Main Menu around the Animo Gacha visual system. It supersedes v11 for these four scene mockups only.

The supplied Ragnarok-like image is a composition and action-flow reference only. Its characters, pets, logo, clothing, and world are not project assets.

## Shared rendering grammar

- Start with large, shape-driven silhouettes and two or three dominant color masses.
- Use strong but soft colored exterior contours: warm brown on skin and hair, indigo on blue cloth, pale blue-gray on cream cloth, and local-color contours on Animo.
- Remove interior linework when a color boundary, overlap, gradient, or shadow already explains the form.
- Model volume with one broad hard-edged shadow, one soft transition, light cyan-violet or muted-peach AO, and narrow selective highlights.
- Keep shadows shallow and saturated. Avoid black occlusion, broad rim lights, bloom, particle haze, and glossy over-rendering.
- Spend the richest gradients, sharpest edges, and strongest contrast only on the primary face, hand, interaction object, or combat decision.
- Lower-hierarchy people and Animo lose hair strands, interior contours, eye gradients, material highlights, and texture as they recede.
- Every character, Animo, prop, landmark, and motion line must either explain the action, establish place, or support UI hierarchy.

## Scene decisions

### Authentication

The heroine's reaching hand and face lead into a stable centered sign-in panel. The red-haired boy returns the gaze across the panel, the teal-haired girl points toward the academy, and four geometric Animo complete one circular S-flow. Full-CG perspective is allowed, but distant characters, pets, and the curved world receive progressively less detail.

### Loading

The final cyan puzzle tile and socket are the focal pair. The heroine places the tile, the teal-haired girl verifies the circle-diamond-star order, the red-haired boy handles the wrong star, and four Animo each move or project one relevant piece. The academy garden stays readable but subordinate.

### Player Hub

The heroine, equipped aqua Animo, and sword stand directly on the inlaid wardrobe floor outside every panel. ATK, CR, CD, and equipped slots remain lightweight chips. The arched window, mirror, garments, boots, weapons, folded fabrics, shelves, and trunks remain legible with only mild atmospheric softening. The right side is one PETS/WEAPON workspace with an immediate selected state and no Equip button.

### Main Menu

The UI first communicates Stoneward health and player health. The second read is the heroine-versus-golem silhouette and shared sight line. The equipped Animo appears only in the dashboard portrait. Meadow flowers, trees, rocks, path, hills, and two ruin arches use broad grouped forms and never compete with combat.

## Final built-in image-generation prompt set

### Authentication

Redraw the curved-world sign-in scene in the Animo Gacha visual family: shape-driven almost-vector anime characters, soft colored contours, sparse interior lines, gradient-led volume, one hard shadow plus one soft transition, light saturated AO, and selective face/hand detail. Preserve three established gang members, four to five purposeful geometric pets, centered sign-in UI, and an S-shaped gaze and motion loop. Keep distant cast and world visibly simpler.

### Loading

Redraw the academy-garden puzzle relay in the Animo Gacha visual family. Preserve the heroine placing the final cyan diamond, teal companion verifying the symbol order, red companion holding the wrong star, and four pets moving relevant tiles. Converge every gaze and gesture on the final socket. Keep the garden readable but low-detail, with minimal Loading UI.

### Player Hub

Rebuild the PETS hub with the heroine, equipped aqua Animo, and sword standing on the actual wardrobe-room floor outside all panels. Use three small stat chips, two equipped-slot chips, and one right-side PETS/WEAPON workspace with a 3x3 grid, selected check state, preview, rarity, and two concise abilities. Match Animo Gacha cream/navy/cyan/coral surfaces and colored contour rendering. Do not blur away the wardrobe.

### Main Menu

Redraw the heroine-versus-Stoneward combat dashboard with Animo Gacha shape language and UI surfaces. Preserve all functional labels, health values, navigation, and inventory slots. Keep exactly one heroine and one golem in the battlefield, with the equipped Animo only in the dashboard portrait. Simplify scenery into grouped forms and reserve detail for combat state and silhouettes.

## Validation checklist

- The primary decision reads at thumbnail size before scenery or decoration.
- Humans and Animo share key light, AO hue, and atmospheric falloff.
- Human contours are softer and thinner than Animo outer contours.
- Lower-hierarchy subjects visibly contain fewer edges, gradients, and highlights.
- No object exists only to fill an empty space.
- Player Hub reads immediately as a wardrobe/equipment room.
- Main Menu contains no extra battle pet or enemy.
- Authentication and Loading retain depth without drifting into high-line-count anime rendering.
