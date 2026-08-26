# Math:World Master Prompt Guide for Art Direction — v14

## Status and authority

This file is the master visual prompt source for future Math:World UI mockups and scene illustrations. It consolidates the approved Pet Gacha direction into one repeatable system.

- Product rules and required fields still come from the GDD and approved design specs.
- This guide controls art style, rendering, composition, UI surface treatment, icon use, hierarchy, mood, and prompt construction.
- When earlier mockup documents conflict with this guide, this guide takes visual precedence.
- `animo-gacha-banner-flat-v10.png` and `animo-gacha-result-flat-v10.png` are the primary visual anchors.
- `animo-gacha-art-direction-v10.md` remains the detailed source for Animo Gacha rendering.
- `leaderboard-pet-gacha-v13.png` is rejected as an art-style and density reference. It may be used only as an information-architecture study.
- External game screenshots are composition or function references only. Never copy their characters, creatures, logos, wording, currencies, modes, or ornamental frame language.
- This document authorizes no Unity implementation, asset replacement, publishing, or dependency change.

## North-star statement

Math:World is a bright, friendly, icon-driven fantasy game presented through shape-driven cartoony UI and Animo, with selective smooth anime rendering only where a human face, gesture, or narrative scene genuinely needs it.

The interface should feel like the Pet Gacha screens expanded into a complete game—not several unrelated anime and mobile-game UI kits placed beside each other.

## Five non-negotiable rules

### 1. Icon first

- Communicate rank, Stage, currency, element, rarity, equipment, status, navigation, and actions with recognizable icons before adding words.
- Pair an icon with a number when the meaning is already established.
- Add a short label only when an icon alone would be ambiguous or unsafe.
- Use tooltips, info popovers, or secondary details for explanations; do not place explanatory sentences in the primary composition.
- Never repeat the same meaning as title, subtitle, label, and icon.

### 2. Shape first

- Begin with large silhouettes, broad color masses, and empty space.
- Every pet, character, panel, badge, and icon must remain readable when reduced to a flat silhouette.
- Interior lines never compensate for a weak silhouette.
- Use two or three dominant color masses before adding gradients or markings.

### 3. Hierarchy before completeness

- The player goal and next decision receive the strongest scale, contrast, edge sharpness, and color accent.
- Reference information is smaller and quieter.
- Decorative graphics occupy only unused negative space.
- A field required by the GDD may be present without receiving equal visual emphasis.
- If everything is detailed, nothing is important.

### 4. One rendering grammar

- Humans, Animo, icons, UI panels, and backgrounds share the same palette, contour family, AO hue, shadow softness, and paper grain.
- Smooth anime rendering means controlled gradients inside simplified shapes; it does not mean many hair strands, costume seams, glossy highlights, or dense lineart.
- Cartoony rendering means strong shape design and economical marks; it does not mean flat, childish, or depthless.

### 5. Every element must have a job

An element is allowed only when it performs at least one function:

- explains the current state;
- enables or confirms an action;
- identifies a character, Animo, item, or location;
- establishes narrative direction;
- creates necessary separation or hierarchy.

Remove elements that exist only to fill space, decorate a corner, repeat a message, or demonstrate rendering skill.

## Screen-mode gate

Choose exactly one mode before writing a prompt. Do not mix the rendering density of one mode into another.

### Mode A — Graphic UI

Use for:

- Pet Gacha;
- Leaderboard;
- results and rewards;
- inventory and progression panels;
- popups, settings, events, and profile summaries.

Rules:

- The background is a flat graphic stage, not a physical location.
- UI and icons carry the experience.
- Human art is limited to an avatar portrait, bust, or small focal sticker unless the screen's only purpose is character celebration.
- Do not place a full-body anime character beside a dense information table.
- No depth-of-field, cinematic environment lighting, or realistic scenery.

### Mode B — Scene UI

Use for:

- Authentication;
- Loading;
- Main Menu combat;
- the grounded Player Hub.

Rules:

- A physical environment is allowed because place and narrative matter.
- UI remains visually derived from the Pet Gacha surface system.
- Characters and scenery use full-CG depth only through scale, overlap, perspective, atmospheric falloff, and controlled gradients.
- Environment detail stays below the active character, gesture, enemy, or UI decision.
- Do not cover a scene with a second unrelated graphic background system.

### Mode C — Character or Animo showcase

Use for:

- character sheets;
- approved promotional art;
- a rare Animo reveal;
- a single cosmetic preview.

Rules:

- One character or Animo is the clear subject.
- UI is minimal and secondary.
- Additional characters, pets, props, and lore elements require a narrative reason.
- This mode must never be used to render a leaderboard row, settings menu, or data table.

## Core visual language

### Palette roles

| Role | Color family | Use |
| --- | --- | --- |
| Primary surface | warm cream / ivory | panels, cards, quiet controls |
| Brand field | cyan to cobalt | graphic background, active zones, broad arcs |
| Primary text/contour | deep navy / indigo | typography, cool contours, structural icons |
| Warm action | gold to restrained coral | one primary commit action or featured reward |
| Equipped/success | muted green | selected state, check, completion, self row |
| Danger/missing | restrained coral | enemy health, irreversible action, missing requirement |
| Rank/value | silver, gold, diamond cyan | rank badges and rarity/value states only |

Do not use coral as general decoration. Do not use gold trim around every panel. Do not create a new accent palette for each screen.

### UI surfaces

- Default surface: warm-cream paper card with one thin navy, cool-gray, or muted-brown contour.
- Corners: softly rounded or lightly clipped, consistent within the screen.
- Depth: spacing, overlap, and one soft low-opacity cast shadow.
- Texture: subtle uniform paper grain; never heavy noise or material texture.
- Active state: cyan/azure edge or fill.
- Selected/equipped/self state: green edge plus check, pin, or `YOU` chip; color is never the only signal.
- VIP/rarity state: a compact badge and a restrained edge accent, not a unique ornamental frame.
- Primary action: one warm gold-to-coral surface.
- Secondary actions: cream controls with navy icons.
- Avoid bevels, glass panels, dark translucent slabs, glossy inner highlights, stacked borders, gold filigree, and unique frames for every content block.

### Graphic background pattern

Allowed ingredients:

- two- or three-color cyan/cobalt/cream gradients;
- one or two broad cream, cobalt, or coral arcs;
- oversized cropped low-contrast screen word;
- halftone discs or checker-dot clusters;
- compass diamonds, paw-dot clusters, and thin concentric rings;
- subtle paper grain.

Rules:

- Recompose the same ingredients across screens.
- Keep patterns behind empty space, not beneath dense text or icons.
- Use decoration to guide flow or balance a mass, never to fill every gap.
- A Graphic UI background must still look like graphic design after all panels are removed.

Forbidden ingredients:

- physical scenery in a Graphic UI screen;
- galaxy or particle fields;
- decorative sparkles around every object;
- unrelated symbols from external references;
- multiple overlapping pattern systems;
- dark competitive arena lighting.

## Rendering system

### Human characters

- Full-body scene characters use a youthful 5.5–6-head ratio, slender athletic limbs, natural hands and feet, and large readable costume masses.
- Information UI uses simplified avatar portraits or bust stickers. Do not render full-body heroes unless the screen is a showcase.
- Prioritize eyes, brows, mouth, front-hair silhouette, hands, and one identity accessory.
- Use very thin local-color contours: warm brown on skin/hair, desaturated indigo on blue fabric, pale blue-gray on cream fabric.
- Exterior contour is only slightly firmer than interior lines. Bright edges may lose the contour.
- Use one broad hard cel-shadow shape and one soft gradient transition.
- Use light cyan-violet AO in cool overlaps and muted peach AO in warm overlaps.
- Reserve highlights for eyes, one hair plane, and one focal metal/crystal object.
- Portraits must simplify further: fewer hair locks, fewer costume seams, and fewer gradients than scene art.

Avoid:

- thick black anime lineart;
- individually drawn hair strands;
- many fabric folds and seams;
- glossy skin or plastic hair;
- rim light around the whole silhouette;
- adult glamour or toddler/chibi proportions;
- detailed full-body character art inside dense information UI.

### Animo

- Begin with one unmistakable silhouette and two or three large color masses.
- Use flat graphic markings as cutouts.
- Use a slightly firmer indigo, brown, or local-color outer contour than the human character.
- Use interior marks only for expression, overlap, or one identity feature.
- Apply one hard shadow shape, tiny soft contact/AO, and one selective eye or focal-mark gradient.
- Use expression and pose for appeal instead of fur, armor, particles, or lighting effects.
- Small UI portraits lose secondary markings and gradients before their silhouette changes.

### Icons and equipment

- Build icons from the same broad shapes and colored contours as Animo.
- Use a cream or pale-blue interior, navy/local-color outline, one hard shadow, tiny AO, and at most one highlight.
- Keep silhouettes recognizable without a label.
- Standardize camera angle within an icon family.
- Do not use photorealistic materials, micro-engraving, dense cracks, or unrelated outline weights.

### Environments

- Environments are allowed only in Scene UI.
- Use broad grouped landmarks, foliage masses, rocks, paths, windows, and furniture.
- Show location identity through a few readable landmarks, not many small props.
- Reduce edge density, texture, saturation, and gradient complexity with distance.
- Avoid realistic grass density, repeated windows, individual leaves, masonry microtexture, heavy blur, and uniform sharpness.

## Detail hierarchy

Assign every visible element to one tier before generation.

### Primary

The current decision, active subject, or narrative gesture.

- strongest silhouette;
- sharpest important edge;
- richest controlled gradient;
- clearest expression;
- strongest semantic accent.

### Secondary

Information or subjects directly needed to understand the primary element.

- simpler gradients;
- fewer internal marks;
- lower contrast;
- smaller scale or quieter surface.

### Reference

Supporting stats, distant actors, environment identity, and optional context.

- icon plus number where possible;
- minimal linework;
- flat fills;
- no decorative highlight.

### Decorative

Background arcs, halftones, grain, and low-contrast motifs.

- never sharper than content;
- never carry essential information;
- disappear first when the screen becomes crowded.

## Icon-driven information architecture

### Text must earn its place

Keep text only when it performs one of these jobs:

- screen title;
- proper name;
- ambiguous primary action;
- short state or warning;
- accessibility-critical explanation that cannot be expressed by icon and value.

Convert these to icons:

- currency type;
- Stage/current/best distinction;
- rank/rarity;
- pet, weapon, avatar, element, health, attack, defense, cooldown, refresh, close, back, settings, map, and leaderboard;
- selected, equipped, locked, completed, missing, or new state.

Move these out of the primary screen:

- formulas and scoring explanations;
- long subtitles;
- repeated category names;
- paragraphs, tips, and instructional prose;
- labels already established by a strong icon;
- secondary analytics not needed for the immediate decision.

### Copy hierarchy

- One clear screen title.
- One compact context chip when needed, such as the locked grade cohort.
- One short state label for the currently selected or pinned item.
- Icon-plus-number groups for repeatable stats.
- One labeled primary action only when the action is not safely understood through an icon.
- Explanations belong behind a compact info icon.

### Row design

A dense list row should read in this order:

1. rank/state badge;
2. avatar and display name;
3. primary ranking value;
4. compact secondary icon-value groups;
5. pet and weapon icons;
6. optional reference statistic.

Do not label every column inside every row. Establish the icon grammar once in the header or an info popover.

## Mood and tone

The intended mood is:

- bright;
- optimistic;
- curious;
- supportive;
- lightly celebratory;
- adventurous without aggression;
- polished without luxury ornament.

Use airy cream and cyan space, rounded graphic motion, friendly expressions, and restrained warm accents.

Avoid:

- dark esports or casino atmosphere;
- intimidating throne rooms;
- aggressive versus framing outside combat;
- ridicule or failure language;
- excessive gold, crowns, flames, particles, or victory glow;
- school-report or corporate-dashboard tone.

## Reference-image protocol

Every image supplied to generation must receive exactly one explicit role:

- **visual authority** — palette, contour, rendering, texture, and UI surfaces;
- **layout reference** — spatial organization only;
- **character identity anchor** — face, outfit, proportions, and approved accessories;
- **content reference** — exact object or pet that must be preserved;
- **mood reference** — energy and tone only.

The prompt must say what must not be copied from each reference.

Never let an external screenshot become an implicit visual authority. For Math:World UI, the Pet Gacha banner/result references remain the visual authority unless the project owner explicitly replaces them.

## Prompt construction order

Write prompts in this order:

1. use case and asset type;
2. screen mode;
3. reference roles;
4. player goal and single primary decision;
5. visual hierarchy;
6. minimum required content from GDD/spec;
7. icon-first conversion;
8. shared UI/background style lock;
9. human/Animo/icon rendering lock;
10. mood and lighting;
11. literal text inventory;
12. invariants;
13. avoid list.

Do not begin with a long inventory of features. Beginning with content encourages the generator to give every item equal weight.

## Copy/paste master prompt

```text
Use case: ui-mockup
Asset type: shippable 16:9 Math:World game UI concept
Screen mode: [Graphic UI | Scene UI | Character/Animo showcase]. Obey only this mode's density and background rules.

Reference roles:
- Image 1: visual authority for the Pet Gacha palette, flat graphic stage, cream panels, navy typography, colored contours, paper grain, shape-driven Animo, and restrained gradients.
- Image 2: [layout/content/identity reference only]. Preserve [specific invariant]. Do not copy [brand, characters, species, logo, wording, ornamental frame, lighting, or unrelated features].

Player goal: [one sentence describing what the player is trying to understand or do].
Primary focal point: [one decision, subject, gesture, or state].
Hierarchy: [primary] -> [secondary] -> [reference] -> [decorative]. Lower tiers use less scale, contrast, edge sharpness, linework, gradient complexity, texture, and highlight.

Required content: [only GDD/spec-required fields and controls].
Icon-first conversion: communicate [currencies/stats/states/navigation] through established icons plus values. Keep text only for [title, proper names, ambiguous primary action, short warning]. Move explanations behind one info icon. No repeated labels.

UI style: warm-cream paper cards; one thin navy/cool-gray contour; shallow soft shadow; cyan/cobalt active field; green selected/equipped state; restrained gold/coral semantic accent; subtle paper grain. Use broad arcs, low-contrast cropped typography, halftone clusters, compass diamonds, paw dots, and thin rings only in unused negative space. Reuse one frame family.

Human rendering: shape-driven youthful anime; simplified large hair/costume masses; very thin local-color contours; sparse interior lines; selective eye/front-hair/hand detail; one broad hard cel shadow; one soft gradient transition; lightly saturated cyan-violet or muted-peach AO; narrow highlights. For information UI, use portraits or bust stickers rather than detailed full-body art.

Animo/icon rendering: unmistakable silhouette; two or three large color masses; slightly firmer local-color contour; minimal interior marks; one hard shadow; tiny soft AO/contact shadow; one selective eye or focal-mark gradient. Equipment icons use the same grammar.

Mood: bright, optimistic, friendly, lightly celebratory, airy, and supportive. No dark competitive atmosphere.

Text (verbatim): [list the complete, minimal text inventory]. Render no other words.

Constraints: every element must explain state, enable/confirm an action, identify content, establish narrative direction, or create necessary hierarchy. Remove decorative filler. Preserve clear empty space. Ensure the primary decision reads at thumbnail size.

Avoid: thick black lineart; many hair strands or costume seams; glossy/plastic rendering; heavy shadow; full-silhouette rim light; bloom or particle spam; equal detail everywhere; ornate gold frames; bevels; glass panels; dark translucent slabs; mixed frame languages; scenery in Graphic UI; paragraphs; repeated labels; tiny text; copied reference branding or characters; extra features; watermark.
```

## Corrected Leaderboard prompt module

The v13 Leaderboard attempted to show too many labels and used a detailed full-body anime showcase. Future Leaderboard concepts must use this module.

```text
Screen mode: Graphic UI.
Player goal: compare friendly same-grade progression and find my own standing immediately.
Primary focal point: the pinned green self row and its Best Stage.
Hierarchy: self row -> top-three rank badges and Best Stage -> compact currency/loadout icons -> decorative graphic stage.

Build the Leaderboard on the same flat cyan/cobalt/cream pattern stage as the Pet Gacha result screen. Use one large title, one locked grade chip, manual Refresh, Close, and one info icon for the ranking explanation. Do not print the ranking formula or a long subtitle on the main screen.

Use a compact first-place portrait card, not a full-body character illustration or throne scene. Show one simplified avatar portrait, rank badge, display name, Best Stage icon/value, three Rank Currency icons/values, pet icon, and weapon icon.

Pin the player's self row above the list with a green edge, pin icon, and small YOU chip. Each list row contains: rank badge; avatar and name; Best Stage icon/value as the primary score; Current Stage icon/value smaller; three currency icons/values; pet icon; weapon icon; optional damage icon/value at the quiet end. Establish icon meaning once; do not repeat CURRENT, BEST, TOTAL DMG, or currency names inside every row.

Top three use a compact Diamond, Gold, or Silver badge and restrained edge accent. Do not create a separate ornamental banner style for each row. Use avatar portraits with the same simplified colored-contour rendering as Animo cards. No full-body character art, no environment, no throne, no crowns larger than the ranking content, no paragraphs, no reward road, and no arena controls.

Text (verbatim): "LEADERBOARD", "GRADE 4 · LEVEL 1", "YOU", and placeholder display names only. Render no other words; use icons plus values for every repeated field.
```

## Screen-specific style modules

### Pet Gacha, results, and rewards

- Graphic UI only.
- Use the approved flat pattern stage.
- One title, one featured state, one primary action.
- Equal cards remain equal; selection uses edge/check/ribbon, not enlarged clutter.
- No scenic location or full-body human art.

### Inventory and Player Hub panels

- Use one main workspace rather than nested summary frames.
- Character and equipped Animo may stand on the physical wardrobe floor only when the screen uses Scene UI.
- Repeatable stats use icons plus values.
- Selected/equipped state uses green outline plus check.
- Preview uses one focal illustration and no ornamental filler.

### Main Menu combat

- Scene UI.
- Player HP and enemy HP own the first information tier.
- Heroine-versus-enemy silhouettes own the second tier.
- Navigation and equipped dashboard own the third tier.
- One enemy, one heroine, no extra pet in the combat focal zone unless mechanically active and required.
- Environment uses grouped forms and low detail.

### Authentication and Loading

- Scene UI.
- Cinematic depth is allowed, but line count and material detail remain Pet Gacha-simple.
- Every person, Animo, prop, gaze, and motion line must support the sign-in flow or loading narrative.
- UI stays stable and quiet while illustration movement flows around it.

### Popups and confirmations

- Graphic UI layered over the current screen.
- One cream decision surface, one title, icon-led before/after or consequence rows, one short warning, one primary action, and one cancel/close control.
- No paragraph warning when icons and before/after values already explain the consequence.

## Prompt-time restraint audit

Before generation, answer:

- What is the one player goal?
- What is the one primary focal point?
- Which screen mode is active?
- Which reference is the visual authority?
- Which required fields can become icon plus value?
- Which explanation can move behind an info icon?
- Which element can be removed without losing meaning?
- Does any detailed human art compete with information?
- Does every panel use the same surface and contour family?
- Is decoration confined to negative space?

If these questions cannot be answered, the prompt is not ready.

## Output validation

### Thumbnail test

At reduced size, the player must find the primary decision or state before decorative art.

### Squint test

The composition must collapse into a few large masses with a clear focal order.

### Text-deletion test

Temporarily ignore all repeated labels. The screen should remain understandable through icons, grouping, and values.

### Contour test

- No uniform black outline.
- Human contours are thinner/softer than Animo outer contours.
- Icon contours belong to the same family as Animo.
- Bright edges may lose contour.

### Render-budget test

- Richest gradients appear only on the primary face, gesture, selected content, or decision.
- Secondary portraits visibly contain fewer hair locks, folds, highlights, and texture.
- UI does not contain detailed full-body character art unless it is a showcase screen.

### Surface test

Panels use one cream-paper family, one contour family, one shallow shadow language, and semantic accent colors. Reject mixed bevel, glass, navy-slab, and gold-frame systems.

### Pattern test

Removing the UI from a Graphic UI screen reveals the same Pet Gacha family of arcs, halftones, cropped words, compass/paw motifs, and grain—not scenery or a new ornamental theme.

### Mood test

The screen feels bright, supportive, adventurous, and lightly celebratory rather than dark, luxurious, aggressive, or academically punitive.

### Purpose test

Point to every visible object. If its function cannot be explained in one short sentence, remove or simplify it.

## Rejection triggers

Reject and regenerate when any of these appear:

- more text than icons in a repeatable information region;
- a long subtitle or formula visible on the main screen;
- a full-body anime character beside dense rows or cards;
- realistic or high-line-count human rendering;
- Animo and humans lit or outlined as if from different games;
- a unique ornamental frame for each rank, card, or screen;
- dark arena scenery behind a non-combat menu;
- equal sharpness and detail across the frame;
- decorative elements with no narrative or interface function;
- the Pet Gacha references are no longer recognizable as the visual parent.

## Iteration protocol

When an output drifts, change one dimension at a time and repeat the critical invariants.

Recommended correction order:

1. remove unnecessary text and panels;
2. restore icon-first hierarchy;
3. remove full-body or over-detailed character art from information UI;
4. restore Pet Gacha cream-panel and flat-pattern background grammar;
5. reduce lineart and rendering detail;
6. correct semantic color use;
7. remove decorative filler;
8. refine spacing and minor styling only after the hierarchy passes.

Do not attempt to fix hierarchy with more glow, contrast, ornament, or detail.
