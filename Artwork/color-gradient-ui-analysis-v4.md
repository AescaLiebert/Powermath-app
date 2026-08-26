# PowerMath Color-Gradient UI Analysis v4

Status: design concept for human review. No Unity implementation is authorized by this document.

## What changed from v3

The v3 hierarchy was clean, but one material treatment was applied to every layer. Beige paper, brown pencil contours, and flat shading made the UI, characters, pets, weapons, and environment feel equally quiet. That removed the visual distinction between collectible fantasy art and interface containers.

V4 keeps the layout and flat UI discipline while separating the rendering layers:

- Characters: polished anime cel shading, colored rim light, layered hair and fabric tones.
- Pets and monsters: soft dimensional gradients, clean colored contours, glossy focal highlights.
- Weapons and collectible icons: brighter material gradients and controlled sparkle.
- Environment: cyan-to-green atmospheric depth with warm sunlight and violet-blue shadows.
- UI containers: calm cream-white translucent planes with a consistent cool-gray border.

## Reference analysis

- The creature-focused reference uses friendly silhouettes and concentrated color around faces, clothing, and pets.
- The ensemble references use warm/cool contrast: blue skies and shadows against orange, gold, coral, or lime focal accents.
- The anime reference gives characters more dimensionality than the UI through cel-shaded planes, rim light, hair gradients, and selective highlights.
- None of these references require every panel to be colorful. Neutral framing protects the vivid artwork.

## Selective saturation rule

Use strongest saturation on:

- player and featured pet;
- enemy or summon focal art;
- HP/resource fills;
- active tab and selected state;
- weapon and rarity materials;
- primary action.

Use lower saturation on:

- panel bodies;
- inactive tabs;
- secondary equipment slots;
- distant environment;
- typography and borders.

This avoids both muddy earth tones and rainbow interface noise.

## Frame and color system

- Panel body: cream white or very pale blue-violet translucency.
- Border: one thin cool-gray/blue line across every panel family.
- Active edge: restrained cyan-to-violet highlight.
- Primary action: warm gold-to-coral gradient.
- Player health: turquoise-to-green gradient.
- Enemy health/threat: coral-to-red gradient.
- Ascended state: blue-violet diamond or rim light.
- Missing requirement: coral numeral, without recoloring the entire card.
- Shadow: soft and shallow; no thick navy slab or heavy gold bevel.

## Main-menu rendering split

- The girl remains the attack control and receives the richest character shading.
- Her cyan ground ring is the only persistent attack affordance.
- Player HP stays beside the foreground character; enemy HP remains top-center.
- The ice monster uses translucent blue crystal highlights and a soft body gradient.
- Power-up cards retain neutral bodies while their icons carry color.

## Player Hub rendering split

- Character, pet, portraits, sword, materials, and active ascent nodes are vivid.
- Overall and Weapon Ascend panels remain pale and quiet.
- The warm Ascend button is the only large warm-gradient UI object.
- Wardrobe remains available through the inactive paw/hanger tab.

## Quick validation

1. Blur the screen slightly. The player, monster/pet, and primary action should remain the strongest color masses.
2. Convert the screen to grayscale. Health ownership, selected state, and hierarchy must still read through position and shape.
3. Check that inactive panels do not compete with character faces or weapon art.
4. During combat effects, verify that the cyan character ring and both HP bars remain distinguishable.
5. In Player Hub, verify that the eye moves character -> overall power -> weapon -> Ascend button.

## Final built-in image-generation prompt set

- Main menu: preserve the v3 composition and character-based attack; replace uniform paper rendering with anime cel-shaded character art, dimensional pet/monster gradients, luminous cyan-green environment, and calm cream-white flat UI with consistent cool borders.
- Player Hub: preserve the v3 information architecture and identities; render character, pet, and sword with premium anime-fantasy color depth; retain neutral translucent Overall and Weapon Ascend panels; reserve vivid gradients for active states, materials, and Ascend.
