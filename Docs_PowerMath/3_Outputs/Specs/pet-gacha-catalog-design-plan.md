---
slug: pet-gacha-catalog
status: approved
source: manual
gdd_tags:
  - core-loop
  - combat-stats
  - economy
  - gacha
  - feedback
owner: game-design-agent
human_checkpoint: required
next_agent: implementation-agent
blocked_by: []
---

# Pet Gacha Catalog Design Plan

> Prototype content proposal requested by the project owner on 2026-08-13. Every rate, name, role, color, and presentation note below is a **starting value** for testing, not locked production content.

> Implementation of this prototype catalog was authorized by the project owner on 2026-08-13. The shared icon is intentionally temporary and does not constitute final art approval.

## 1. Player Goal and Context

The catalog should give a student three immediately understandable reasons to pull:

1. Complete a compact fantasy pet collection.
2. Find a pet whose identity expresses their preferred play style.
3. Chase rare showcase pets without hiding the exact odds or the empty-duplicate risk.

The first implementation remains ownership-only. Pet equipping and combat stats stay in a later slice. Until equipping exists, role labels are promises about design direction rather than active benefits; the UI must not imply that a collected pet currently grants stats.

## 2. Catalog Shape and Starting Rates

Use three rarity categories with five pets in each. Equal category sizes make the redistribution rule easier to inspect and explain.

| Rarity | Stable ID | Category rate | Basis points | Base chance per pet | Visual direction |
| --- | --- | ---: | ---: | ---: | --- |
| Rare | `rare` | **70% starting value** | 7000 | 14% | Bronze leaf frame plus one diamond pip |
| Super Rare | `super_rare` | **27% starting value** | 2700 | 5.4% | Violet rune frame plus two diamond pips |
| Super Super Rare | `ssr` | **3% starting value** | 300 | 0.6% | Gold celestial frame plus three diamond pips |

Rates total exactly 10,000 basis points. Text labels and pip count carry rarity information so color is never the only signal.

### Rate Micro-Test

- **Test:** Give ten new players the odds panel, then ask which rarity and individual pet are hardest to obtain.
- **Pass:** At least 8 of 10 answer correctly and can state that owning a pet changes only the distribution inside its rarity.
- **Fail response:** Expand the rarity subtotal explanation and show `category rate ÷ five pets` beside the default state; do not solve confusion with more animation.
- **Balance direction:** If SSR acquisition feels irrelevant over the tested progression window, raise SSR in 0.5 percentage-point steps by moving probability from Rare. If SSR stops feeling exceptional, reverse that adjustment. Preserve the 10,000-point total.

## 3. Prototype Pet Roster

### Rare — Familiar Companions

| Pet ID | Display name | Identity | Future role direction | Art silhouette |
| --- | --- | --- | --- | --- |
| `ember_fox` | Ember Fox | Curious fire guide | Steady Pet ATK | Large tail with ember tip |
| `moss_turtle` | Moss Turtle | Patient forest guardian | Max-hearts defense | Round shell with sprout |
| `cloud_finch` | Cloud Finch | Fast sky scout | Critical-rate setup | Small bird with cloud wings |
| `pebble_golem` | Pebble Golem | Loyal stone helper | Reliable Pet ATK | Blocky body and bright core |
| `moon_bunny` | Moon Bunny | Calm lunar companion | Critical-damage setup | Long ears and crescent charm |

### Super Rare — Arcane Beasts

| Pet ID | Display name | Identity | Future role direction | Art silhouette |
| --- | --- | --- | --- | --- |
| `prism_owl` | Prism Owl | Scholar of patterns | Balanced CR/CD | Wide geometric wings |
| `rune_lynx` | Rune Lynx | Agile spell hunter | Critical-rate specialist | Tufted ears and rune bands |
| `tide_serpent` | Tide Serpent | Flowing water spirit | Pet ATK specialist | Coiled wave body |
| `gear_griffin` | Gear Griffin | Clockwork protector | ATK/heart hybrid | Beaked profile and gear wings |
| `bloom_stag` | Bloom Stag | Keeper of renewal | Max-hearts specialist | Branch antlers with flowers |

### SSR — Mythic Wisdom Keepers

| Pet ID | Display name | Identity | Future role direction | Art silhouette |
| --- | --- | --- | --- | --- |
| `infinity_dragon` | Infinity Dragon | Master of endless paths | Premium balanced offense | Figure-eight flying pose |
| `chrono_phoenix` | Chrono Phoenix | Keeper of second chances | Critical-rate specialist | Clock-halo wings |
| `astral_kirin` | Astral Kirin | Guide through star maps | Critical-damage specialist | Horn and constellation mane |
| `crown_sphinx` | Crown Sphinx | Guardian of riddles | Offense/heart hybrid | Crown, wings, and seated pose |
| `wisdom_leviathan` | Wisdom Leviathan | Ancient sea of knowledge | Max-hearts specialist | Crescent body around an orb |

Names and silhouettes are deliberately distinct at small mobile sizes. The themes combine fantasy ownership with pattern, rune, time, constellation, and riddle motifs that fit PowerMath without turning the pets into lesson UI.

## 4. Ownership Weight Examples

The authoritative algorithm remains the GDD rule: an owned pet receives half its equal base share while at least one pet in that rarity remains unowned; removed probability is shared equally across unowned pets.

With one owned pet in a five-pet category:

| Rarity | Owned pet chance | Each of four unowned pets | Rarity subtotal |
| --- | ---: | ---: | ---: |
| Rare | 7% | 15.75% | 70% |
| Super Rare | 2.7% | 6.075% | 27% |
| SSR | 0.3% | 0.675% | 3% |

When all five pets in a rarity are owned, their chances return to the equal base values in Section 2 and every result from that rarity is an empty duplicate.

## 5. Future Stat Plan — Deferred Slice

Do not add combat values to the first catalog asset. The existing catalog schema needs only identity, rarity, icon, and presentation assets. A later equip/stat design should add a separate balance table so content identity is not coupled to live combat math.

Starting constraints for that later design:

- One equipped pet at a time.
- Every pet within a rarity receives the same total power budget; identity changes the distribution among Pet ATK, CR, CD, and max hearts, not its total value.
- Rare pets must remain usable choices rather than automatic trash.
- SSR pets may be broader or more specialized, but must not invalidate Weapon Ascend or trivialize stage/boss pacing.
- Challenger League pet effects remain disabled as required by the GDD.
- All values must use the existing clamped effective-stat pipeline.

### Stat Micro-Test Before Activation

- **Test:** Compare no pet, each role, and Weapon Ascend alternatives at early, middle, and late progression snapshots.
- **Pass:** No pet produces an unintended difficulty wall; no single pet role dominates every snapshot; testers can explain why they spent 25 Coins on collection versus saving for Weapon Ascend.
- **Fail response:** Adjust pet power budgets first, then role distribution. Do not alter pull odds to repair combat balance.

## 6. Presentation Plan

### Catalog Cards

Each card shows icon, name, full rarity label, pip count, current exact probability, and `Owned` or `Not owned`. Future role text is hidden until stats actually ship or explicitly labeled `Planned role` in development builds.

### Reveal Assets

For the prototype, each pet needs:

- One square icon readable at small mobile size.
- One transparent reveal illustration or a clean enlarged icon fallback.
- Rarity frame inherited from its category rather than bespoke UI per pet.
- A one-line identity phrase from the roster.

Shared audio is sufficient for the prototype: one commit cue, one new-pet reveal cue per rarity tier, one duplicate cue, and one error cue. Missing audio must not block catalog activation if all outcomes remain readable.

### Reduced Motion

Use opacity and a static rarity frame instead of scale bursts or particles. Keep the same readable result hold as the standard presentation.

## 7. Five-Component Evaluation

| Component | Catalog requirement | Validation signal |
| --- | --- | --- |
| Clarity | Three tiers, five pets per tier, visible subtotals, exact per-pet odds, text-plus-pip rarity encoding | New players correctly compare odds and duplicate risk |
| Motivation | Each pet has a distinct fantasy identity, collection slot, and future role direction | Players name at least one desired pet and why they want it without being told which is strongest |
| Response | Catalog content does not add steps to the approved confirm-and-reveal flow | Selection, result, and refreshed odds remain immediate and unambiguous |
| Satisfaction | Strong silhouette progression from familiar to arcane to mythic, with tiered reveal intensity | Observers distinguish rarity and new/duplicate outcomes with sound muted |
| Fit | Math-adjacent fantasy motifs support Challenge, Expression, and Ownership | Pets feel native to PowerMath rather than imported from an unrelated theme |

Priority if requirements conflict: Response, Clarity, Satisfaction, Fit, then Motivation.

## 8. Risks and Abuse Cases

- **Empty duplicates feel punitive:** Repeat the no-reward warning before confirmation; measure behavior and sentiment. Do not quietly add shards, pity, or conversion outside a GDD change.
- **SSR chase overwhelms the educational loop:** Keep summon access in the Main Menu and avoid urgent timers, streak pressure, or paid currency messaging.
- **Catalog is mistaken for active combat power:** Mark ownership-only status clearly until equip/stat support ships.
- **Rare pets feel disposable:** Give every pet a distinct identity and later balance equal power budgets within each rarity.
- **Color-only rarity:** Always pair color with the written label, pip count, and frame shape.
- **Small-screen visual collision:** Test all 15 silhouettes and names at the minimum supported mobile-compatible WebGL viewport.
- **ID instability:** Treat the stable IDs in this document as save-data keys; display names may change, IDs must not after ownership data exists.

## 9. Playtest Plan

### New Player

Show the unopened catalog and give the player 50 Power Coins. Pass when 8 of 10 testers identify cost, rarest tier, an owned marker, and the consequence of a duplicate without instruction.

### Collection Pacing

Simulate and manually play 10, 25, 50, and 100 pulls across empty, half-complete, and nearly complete collections. Record unique pets, duplicate streaks, Coins spent, and category completion. This establishes the baseline; it does not assume a target collection time that the GDD has not defined.

### Stress and Abuse

Spam confirm, refresh during reveal, retry a transaction ID, change the active catalog version, and complete a rarity. Pass only if odds remain conserved, save IDs remain stable, and no action creates a free reroll or double charge.

### Readability

Test muted audio, Reduced Motion, color-vision filters, low frame rate, and the minimum mobile viewport. Pass when 8 of 10 observations correctly identify pet, rarity, probability, owned state, and result consequence.

### Economy Choice

Present gacha and Weapon Ascend at early, middle, and late snapshots. Record the reason for each choice. Before pet stats ship, pass only if players understand that the pull is currently a collection purchase; after stats ship, rerun using the separate stat balance plan.

## 10. Implementation Sequence and Checkpoints

1. **Human content checkpoint:** Approve or revise the three rates, 15 names, stable IDs, rarity membership, and visual direction.
2. Create `PetGachaCatalogDefinition` version `prototype-v1` with 10,000 total basis points and five entries per rarity.
3. Produce placeholder icons with distinct silhouettes; final art is a separate approval checkpoint.
4. Assign shared prototype reveal/audio assets and verify readable fallbacks.
5. Run catalog validation plus probability unit tests for empty, partial, and complete ownership states.
6. Run the playtests in Section 9 and record results before calling the rates balanced.
7. Keep pet equip/stat work in a separate task card and design checkpoint.

## 11. Decision Requested

Approve this as the prototype catalog direction, or revise any of these content groups before an asset is created:

- Rates: Rare 70%, Super Rare 27%, SSR 3%.
- Roster size: five pets per rarity, 15 total.
- Stable IDs and display names in Section 3.
- Ownership-only launch, with all combat roles explicitly deferred.
