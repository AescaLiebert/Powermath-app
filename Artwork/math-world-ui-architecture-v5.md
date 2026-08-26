# Math:World UI Architecture v5

Status: design concept for human review. No Unity implementation, asset replacement, publishing, or dependency change is authorized by this document.

All values shown in the mockups are content examples inherited from the concepts, not balance recommendations.

## Consistency without identical frames

Consistency comes from shared tokens, while silhouette follows function.

| Component role | Shape language | Visual weight |
|---|---|---|
| Profile and combat information | Elegant floating capsule or thin-line glass panel | Quiet |
| Biome, leaderboard, settings | Compact circular icon discs | Medium |
| Hub, Gacha, Rebirth windows | Chunkier vertical icon tiles with a close handle | Medium-high |
| Dashboard and inventory | Low grouping surface plus compact cards | Medium |
| Preview and progression | Spacious light panel with one focal illustration | Quiet around vivid content |
| Commit actions | Warm gold-to-coral button | Highest UI emphasis |

Shared tokens:

- cream-white or pale-blue panel bodies;
- one cool-gray border family;
- indigo typography;
- cyan/azure active state;
- green equipped/completed state;
- gold rarity/value state;
- coral threat, missing requirement, or commitment accent;
- consistent icon lighting and spacing.

Patterns appear only in unused negative space, never across every panel.

## Main Menu information architecture

### Profile Panel

- Profile icon.
- Display name.
- Rank currencies only.

### Navigation

- Biome map.
- Leaderboard.
- Settings.

These are icon-only circular utilities at top right.

### Player Menu

- Player Hub.
- Pet Gacha.
- Rebirth.
- Collapsible through the side chevron handle.

### Active combat

- Stage and monster name/HP.
- Monster intent icons.
- Player character is the attack control.
- Player Dashboard contains HP, equipped pet, current weapon, and four power-up card slots.
- Equipped pet uses a green outline plus check.

## Player Hub tab states

The left Player Summary persists across both tabs:

- player appearance;
- equipped pet;
- current weapon appearance;
- `ATK`, `CR`, and `CD` summary;
- equipped pet and weapon portraits.

### Pet Inventory

- Scrollable four-column pet grid.
- Each card shows portrait, element, and star rarity.
- Clicking an owned card immediately equips that pet.
- Equipped state uses green outline plus check; there is no Equip button.
- Preview panel shows pet art, name, rarity, and two concise ability rows.

### Weapon Ascend

- Large current weapon illustration.
- Rarity and ascent nodes.
- Current stat to next stat.
- Upgraded weapon thumbnail.
- Material requirements.
- Required power coins.
- One `ASCEND` action.

## Interaction state definitions

### Immediate pet equip

- Entry: tap an owned pet card; scrolling gesture must not count as a tap.
- Resolution: new card receives green outline/check; previous equipped card loses them; Player Summary updates.
- Cost: none.
- Exit: selection persists when leaving the Hub after save acknowledgement.
- Invalid states: locked or unowned cards do not equip and must acknowledge the tap without changing the summary.
- Edge cases: rapid repeated taps, save failure, card removed from inventory, filter or scroll refresh.

### Weapon ascend

- Entry: tap `ASCEND` while materials and power coins are sufficient.
- Resolution: consume requirements once, advance the node, update weapon appearance/stat, and confirm through visual plus audio feedback.
- Invalid state: insufficient requirements keep the action unavailable and emphasize only missing values.
- Edge cases: double tap, interrupted save, inventory value changing during confirmation, maximum tier.

### Authentication and loading

- Valid credentials or Guest transitions from Authentication to Loading.
- Authentication failure keeps the form visible and identifies the failed field/action without clearing unrelated input.
- Loading displays one authoritative progress value and transitions only after account and scene data are ready.
- Settings and language remain available before authentication.

## Authentication Scene

- Cinematic floating-world background establishes the fantasy.
- Math:World logo occupies the upper-left brand zone.
- One compact sign-in card contains email, password, Enter, and Guest.
- Provider branding and dense legal copy are intentionally absent from this concept.

## Loading Scene

- Flatter, lighter illustration than Authentication.
- Player and pets run toward the academy along a glowing number route.
- One route-style progress line and one percentage.
- No tips paragraph or competing panel.

## Validation script

1. Ask a new player to identify profile, utilities, closable windows, and active combat without annotation labels.
2. Ask where to find Player HP, equipped pet, weapon, and power-ups.
3. In Pet Inventory, ask the player to equip a pet without instruction. Pass if they tap a card and understand the green checked state.
4. In Weapon Ascend, ask what will improve, what is missing, and what currency will be spent.
5. On Authentication, ask how to sign in and how to continue as Guest.
6. On Loading, ask whether progress and destination are identifiable within three seconds.

## Final built-in image-generation prompt set

- Logo: original transparent `MATH:WORLD` wordmark with orbiting world, compass star, and restrained math-symbol accents.
- Main Menu: preserve the user's revised battlefield layout; separate profile, utilities, closable player windows, combat header, and Player Dashboard using role-based shapes.
- Pet Inventory: persistent Player Summary, scrollable pet grid, immediate green checked equip state, and concise selected-pet preview.
- Weapon Ascend: same Hub shell, focused weapon appearance, current-to-next stat, requirements, power coins, and one Ascend action.
- Authentication: cinematic floating-world background, original logo, compact sign-in card, Enter and Guest.
- Loading: flatter journey illustration, original logo, and one route-style progress indicator.
