# PowerMath Light-Fantasy UI Direction v2

Status: concept design for human review. This handoff does not authorize implementation or replacement of existing assets.

## Design goal

Blend the instant readability and friendly weight of casual mobile UI with the restraint and splash-art focus of modern anime games. Information should be understood in this order: icon, number, master word, supporting detail only when essential.

## Shared visual language

- Ivory content panels, deep navy structure, sky-blue active states, teal secondary states, and warm gold primary actions.
- Coral is semantic: warning, destructive reset, threat, or `NEW`. It is not a general accent.
- Rounded geometric panels with soft shadows and thin high-contrast borders.
- Decoration comes from flat, low-contrast paw, star, leaf, and elemental patterns rather than additional objects or text.
- Selected, equipped, locked, and dangerous states must differ by shape/icon as well as color.
- Original pet silhouettes only. References guide composition and mood, not character identity.

## Screen hierarchy

### Main menu / battle HUD

1. Stage and enemy intent occupy the top-center combat focus.
2. Profile and currencies stay compact at top left.
3. `PETS`, `SUMMON`, and `REBIRTH` form the only word-labeled navigation rail.
4. Map, rank, and settings remain icon-only at top right.
5. Player, equipped pets, and attack are grouped into one bottom-center action tray.
6. Power-up slots flank the action tray and do not compete with the primary attack.

### Rebirth

1. Use a before-to-after transformation, not explanatory sentences.
2. Show power gain first, stage reset second, and coin reward third.
3. Put the warning icon directly on the reset row.
4. Keep one dominant confirmation action: `REBIRTH`.

### Pet summon banner

1. Feature one dominant original pet and two supporting silhouettes.
2. Reserve the left third for title, pity badge, and banner tabs.
3. Keep summon-one and summon-ten actions adjacent so cost comparison is immediate.
4. Communicate premium rarity through lighting, pose, and framing rather than dense copy.

### Gacha result

1. Enlarge the rare/new result while keeping the remaining results in a quiet grid.
2. Show `NEW` once, pet name once, and rarity through stars.
3. Offer only the immediate decisions: `EQUIP`, `AGAIN`, share, or close.

### Player Hub / pet wardrobe

1. Left: avatar and currently equipped pet team.
2. Center: pet roster grid.
3. Right: selected-pet preview, accessory slots, and accessory carousel.
4. The roster and wardrobe are separate panels so selecting a pet never looks like equipping an accessory.
5. The gold `EQUIP` button commits the pending selection.

## Interaction states

- Selected: gold outline plus a small corner marker.
- Equipped: checkmark badge plus gold slot frame.
- Locked: lock icon plus reduced saturation.
- Available empty slot: plus icon.
- Destructive reset: coral warning triangle; confirmation remains warm gold so the screen stays friendly.
- Primary tap acknowledgment: panel compresses slightly, highlight sweeps once, and a short sound confirms the input.

## Player-experience check

- Clarity: major outcomes are icon-led and grouped by decision.
- Motivation: rare pet, permanent power gain, and team changes each receive a distinct focal moment.
- Response: every screen has one obvious primary action and an always-visible close/back action.
- Satisfaction: rare pulls and Rebirth should combine visual burst and audio confirmation without persistent particle clutter.
- Fit: playful pets and chunky controls remain approachable; negative space, typography, and splash framing add anime-game polish.

## Quick playtest

1. New player: after five seconds on each screen, ask what the primary action does. Pass if the answer is correct without reading instructions.
2. Readability: show the HUD during a busy attack effect. Pass if stage, enemy intent, and attack control remain identifiable.
3. State test: ask players to identify selected, equipped, locked, and empty pet slots without reading labels.
4. Rebirth safety: ask what will increase and what will reset before confirmation.
5. Gacha flow: ask players to equip the new pet, then summon again, without guidance.

## Final image-generation prompt set (normalized)

- Main menu: shippable 16:9 battle HUD; original ice-crystal creature; profile/currency cluster; stage and intent strip; three master-word side tabs; icon-only utilities; player/pet attack tray; minimal light-fantasy casual/anime hybrid.
- Rebirth: centered 16:9 modal; icon-led before/after rows for power, stage reset, and coins; progress bar; one warning icon; one `REBIRTH` confirmation.
- Summon banner: 16:9 premium pet splash; original moon-fox, ember ram, and sprout-dragon; title and pity at left; currency and close at top right; summon-one and summon-ten at bottom right.
- Gacha result: 16:9 ten-pull reveal; one enlarged five-star new pet; nine quiet result cards; only `RESULT`, `NEW`, `MOONPAW`, `EQUIP`, and `AGAIN` as words.
- Player Hub: 16:9 three-zone management layout; avatar/team preview, pet roster, and separate pet wardrobe/accessory panel; only `HUB`, `PETS`, and `EQUIP` as words.

All prompts constrained output to original pets and UI assets, no logos, no trademarks, no watermarks, no annotation arrows, no copied characters, no paragraphs, and no tiny text.
