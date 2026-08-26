# Math:World Illustration Balance — v8

## Corrected target

The illustration system should sit between smooth youthful anime and friendly shape-driven mobile-game art. It must not reduce scenery to low-poly symbols, and it must not render the heroine more intensely than the rest of the scene.

The intended focus order is:

1. Face, eyes, and expression.
2. Character and pet silhouettes/action.
3. Authentication or loading interaction.
4. Recognizable environment and atmosphere.
5. Small texture and decoration.

## Character rendering

- Use a young, kid-friendly heroine with roughly six-head proportions, a rounded triangular face, large expressive eyes, a small nose, and clear eyebrow acting.
- Keep three readable costume masses: cream jacket, blue hood/capelet, and indigo lower outfit. Small gold or cyan accents are identity anchors, not general decoration.
- Concentrate gradients and detail around eyes, cheeks, front hair, the compass accessory, and one or two metal surfaces.
- Use a single broad hard-edged primary shadow, followed by one soft transition. Avoid stacks of rim light, gloss, tiny specular spots, and sculptural micro-shading.
- Ambient occlusion should be lightly saturated blue-violet in cool areas and muted peach-brown in warm areas. It should separate overlapping forms, not dirty every crease.
- Prefer soft brown, indigo, or local-color linework. Keep the outer contour slightly firmer, internal lines sparse, and allow bright or soft edges to disappear into the light.

## Environment rendering

- Build scenery from medium-sized grouped forms: rolling grass masses, path bands, clustered foliage, readable rocks, flower groups, water shapes, and broad architectural openings.
- Forms must remain recognizable and layered; avoid both realistic texture density and faceted low-poly shorthand.
- Give the environment approximately 50–65% of the character contrast and edge sharpness. Distant forms should merge through atmosphere rather than through aggressive blur.
- Use broad gradients and selective soft brushes across ground and sky. Reserve crisp edges for the foreground narrative zone and important navigation landmarks.
- The background may contain enough information to imply a living world, but it should not compete with faces, gestures, or the central UI panel.

## Screen composition

### Authentication

- Use the sky-dive perspective: a rounded world below, broad cloud arcs, the heroine entering diagonally from the left, and a large pet counterbalancing her on the right.
- Keep the Math:World logo small and centered above a centered sign-in panel. The UI is the stable visual anchor while the characters create motion around it.
- The world needs medium grouped biome, river, academy, and settlement detail—enough to invite exploration without becoming a map illustration.

### Loading

- Use a low, wide pet-garden perspective and a small visual story: the heroine teaches with math tokens while pets misunderstand, help, and react.
- Characters and pets occupy about 65% of the visual attention. Lawn, pond, path, trees, building, benches, and flowers establish place at lower contrast.
- Keep only a small logo and a single loading line; do not cover the narrative with a large panel.

### Main Menu

- Keep the combat UI hierarchy and values unchanged.
- The rear-view heroine replaces a generic attack icon and must still read as the same character through hair silhouette, blue hood/capelet, cream jacket, compass accent, and sword.
- The battlefield uses the same medium-detail environment rule: smooth rolling ground and grouped natural forms, with neither realistic clutter nor low-poly simplification.

## Consistency checks

- Squint test: face/action first, UI second, environment third.
- Grayscale test: the heroine must not have a much wider value range than the environment.
- Edge test: most internal edges are soft or absent; crisp lines cluster at the face, hands, interaction props, and primary silhouettes.
- Light test: one coherent key light; no unrelated glow around every character.
- Distance test: Authentication labels and core Main Menu information remain readable at mobile size.
- Style test: pets may use a slightly firmer cartoony contour than the heroine, but share the same light direction, AO color, and shadow simplification.

## Built-in image-generation prompt set

### Heroine concept

Original 14–15-year-old Wayfinder heroine, six-head youthful proportions, rounded triangular face, wide amber eyes with teal inner ring, open expressive smile, sandy-honey bob with outward tufts, short side braid, crescent cowlick, cyan compass hair clip; cream oversized short jacket, sky-blue hood/capelet, indigo tunic and skirt, opaque navy leggings, cream-blue boots, gloves, compass pouch, short scarf, compact sword. Smooth local-color lineart, slightly firmer silhouette, sparse interior lines, one broad hard cel shadow plus one soft transition, lightly saturated blue-violet and peach AO, limited gradients focused on eyes, face, hair, jacket, and metal. Front three-quarter lively pose with small rear-view inset; clean pale presentation background.

### Authentication scene

16:9 sky-dive authentication illustration with an extreme wide-angle view down toward a rounded fantasy world. Young Wayfinder heroine falls from upper left with foreshortened boot and reaching hand; ice pet tumbles at right; smaller leaf and star pets frame lower edges. Medium-grouped green biomes, rivers, academy cluster, settlement, and broad clouds below. Center a small simple Math:World logo over a translucent sign-in panel using only MATH:WORLD, SIGN IN, EMAIL, PASSWORD, ENTER, GUEST, and 1.0. Smooth anime-to-cartoony rendering, sparse colored lineart, hard primary shadow plus soft transition, light saturated AO, coherent bright sky illumination; no micro-detail, excessive rim light, or low-poly scenery.

### Loading scene

16:9 low wide-angle pet-garden narrative. The young Wayfinder heroine teaches with three glowing abstract token orbs and a compass board; a large ice pet in the left foreground bats the wrong token, a leaf pet carries another, and a star pet guides a third while the heroine reacts with amused surprise. Characters and pets occupy about 65% of attention. Medium-grouped lawn, curved path, small pond, clustered trees, pet-care academy building, bench silhouettes, picnic items, and flower groups at lower contrast. Smooth youthful anime heroine, friendly shape-driven pets with slightly firmer contours, one hard shadow plus one soft transition, lightly saturated AO. Small Math:World logo at top-left and a single bottom line reading LOADING and 72%.

### Main Menu scene

16:9 mobile combat Main Menu preserving the established UI: NOVA profile with rank values 3, 0, 0; STAGE 12; enemy STONEWARD at 850 / 1000; map, leaderboard, and settings; HUB, GACHA, REBIRTH; dashboard HP 780 / 1000 with equipped ice pet, sword, potion 3, and leaf 2. A young rear-view Wayfinder with sandy-honey hair, blue hood/capelet, cream jacket, compass accent, and sword faces the stone golem. Use a medium-detail stylized meadow with rolling gradients, broad grass strokes, dirt path, grouped flowers, rounded foliage, readable rocks, and distant ruins with broad openings. Smooth colored lineart, one hard primary shadow plus soft transition, light saturated AO, limited highlights, no realistic clutter and no faceted low-poly simplification.
