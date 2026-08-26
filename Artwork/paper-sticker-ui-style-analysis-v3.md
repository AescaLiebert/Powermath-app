# PowerMath Paper-Sticker UI Style Analysis v3

Status: design concept for human review. No Unity implementation is authorized by this document.

## Reference analysis

### Flat popup reference

- Warm ivory is the dominant plane; color accents are small and semantic.
- Frames use one fine muted-brown line with tiny clipped-corner details.
- Depth comes from spacing and a soft cast shadow, not bevels or thick borders.
- Large empty areas and faint tone-on-tone patterns keep the panel friendly without adding information.
- Typography is dark brown and visually quieter than the former outlined display text.

### Golem reference

- The outer contour is slightly irregular, like graphite or pencil rather than a perfect vector stroke.
- Shapes are large, simple, and readable as a sticker silhouette.
- Materials use flat base color plus one restrained shadow tone.
- Fine paper grain unifies the object without adding surface detail.

### Conflict in the prior HUD

- Navy slabs, gold trim, hexagons, circles, bevels, and heavy shadows created several competing frame languages.
- The attack tray, portraits, navigation, and utility buttons looked like separate UI kits.
- The sword icon described an action but did not show the player fantasy of confronting the monster.
- Player HP was absent even though health is critical combat information.

## Consolidated visual system

- Frame: warm ivory paper card, clipped corners, one graphite-brown outline.
- Illustration: flat paper sticker with a slightly irregular pencil contour and subtle grain.
- Accent colors: powder blue, muted teal, sage, mustard, and limited coral.
- Coral: enemy health, danger, or missing requirements only.
- Shadow: one soft low-opacity shadow; no bevel or inner gloss.
- Patterns: paw, star, leaf, or weapon motifs at very low contrast.
- Shape rule: cards and small rectangular chips dominate; circles are reserved for character focus rings or compact status markers.

## Main-menu decisions

- The girl character replaces the entire attack button and sword icon.
- A pale hand-drawn ground ring signals that the character is tappable.
- Her back-facing fighting pose supplies direction: player in foreground, monster in the distance.
- Player HP sits immediately beside/above the foreground player zone with a heart icon and teal fill.
- Enemy HP stays top-center with coral fill, preventing ownership confusion.
- Equipped pet portraits remain beside Player HP as supporting combat participants.
- Power-ups use four vertical cards, two on each side, with icon and count only.

### Attack clarity requirement

When the character is actionable, the ground ring should breathe or brighten subtly. On press, acknowledge the input immediately through character compression/anticipation plus a short UI sound. During cooldown or lockout, desaturate the ring and show the remaining state on the character zone rather than reintroducing an attack button.

## Player Hub decisions

- Keep the known character and pet preview at left for continuity.
- `OVERALL` summarizes power, HP, attack, defense, speed, and equipment through icons and values.
- `WEAPON ASCEND` is a single focused task: current weapon, ascent nodes, before/after power, materials, and one `ASCEND` action.
- The previous pet wardrobe remains available through the paw/hanger tab; it is not mixed into weapon progression.
- Missing material uses coral numerals while sufficient materials remain neutral.

## Quick validation

1. Show the battle HUD without instruction and ask where to tap to attack.
2. Ask which health bar belongs to the player and which belongs to the monster.
3. During a busy combat effect, verify that both health bars, turn intent, and the player character remain identifiable.
4. In Player Hub, ask the player to identify current power, the weapon's next value, and the missing material.
5. Ask how to return to pet wardrobe; the inactive paw/hanger tab should be found without a text label.

## Final built-in image-generation prompt set

- Main menu: revise the previous meadow HUD into a flat paper-sticker and pencil-line-art system; retain the ice monster and navigation; replace the attack tray with a back-facing sword girl; add teal Player HP near the foreground player; change four power-ups into vertical cards; prohibit glossy navy, gold bevels, mixed frames, attack text, and attack icons.
- Player Hub: preserve the known adventurer and ice-pet preview; replace the roster and accessory panels with Player Overall and Weapon Ascend; keep wardrobe as an inactive paw/hanger tab; use one warm-ivory clipped-corner frame family and restrained pencil-sticker rendering.
