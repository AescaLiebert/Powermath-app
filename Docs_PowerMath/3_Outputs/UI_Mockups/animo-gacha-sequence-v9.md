# Animo Gacha Sequence — v9

## Player goal

The player should understand the featured Animo, guarantee, currency cost, and available record information before committing. After summoning, they should recognize quantity and rarity before the reveal, enjoy new or rare outcomes, then regain control quickly.

The four visual states are:

1. `Banner` — choose x1 or x10; inspect Details or History.
2. `Summon Transition` — confirm quantity and foreshadow rarity through ten paw-stars.
3. `Rare Reveal` — pause for a new or high-rarity Animo.
4. `Result` — scan all ten outcomes and either close, share, or repeat.

## Visual consistency

- The entire flow remains in the same moonlit observatory garden. Camera angle and contrast change, but the location does not.
- Reuse the Math:World v8 cream surfaces, navy typography, cyan interaction light, restrained gold primary action, thin colored outlines, and consistent corner radii.
- Gold has only three jobs: primary summon action, rare-result cue, and currency. It is not a universal frame color.
- Coral is reserved for `NEW` and must not become a second action color.
- Animo portraits use a slightly firmer cartoony silhouette than the environment, with one broad hard shadow, one soft transition, and lightly saturated blue-violet ambient occlusion.
- Avoid paragraphs on the banner. Detailed rates and rules belong behind `DETAILS`.

## Banner hierarchy

### Primary

- Featured Animo splash art.
- `ANIMO CALL`.
- x1 / 25 and x10 / 250 summon choices.

### Secondary

- One short guarantee line: `3★+ within 10`.
- Currency balance and close control.

### Reference

- `DETAILS` opens the exact published rate table, guarantee rules, duplicate conversion, and featured availability.
- `HISTORY` opens a reverse-chronological summon log.

Do not invent probability values in the interface. The exact rates must come from the approved economy specification.

## Interaction state machine

### Banner idle

- Entry: open Gacha from Main Menu or return from Result.
- Player can: close, open Details, open History, summon x1, summon x10.
- A summon button displays quantity and full cost in the same hit target.
- If currency is insufficient, do not begin the animation or subtract currency. Pulse only the balance pill and open the approved currency-source path.

### Commit

- On valid summon input, acknowledge the pressed button immediately, lock both summon buttons, deduct the validated cost once, and move to Transition.
- Ignore repeat presses until Result. Never queue a second purchase from input spam.
- If the transaction fails, restore Banner idle and show a concise error toast; do not consume currency.

### Summon transition

- x10 displays exactly ten paw-stars in one orbit. x1 displays one.
- Cyan-white indicates ordinary unresolved outcomes; restrained gold foreshadows at least one rare outcome.
- `SKIP` goes directly to Result after rewards have been resolved server-side. It never changes the reward.
- Reduced-motion mode replaces the orbit and camera motion with a short crossfade while preserving the quantity and rarity cues.

### Reveal queue

- Standard duplicate outcomes do not require individual full-screen reveals.
- New or high-rarity Animo enter the reveal queue and appear one at a time.
- Tap the arrow or screen-safe area to continue. `SKIP` resolves the remaining reveal queue and opens Result.
- Rarity is encoded redundantly through diamond count, halo shape, sound tier, and color—not color alone.

### Result

- Show ten equal cards in a 5×2 grid. The featured/new card remains equal in size and gains only a gold outline plus `NEW` ribbon.
- The displayed currency reflects the completed transaction.
- Close returns to the previous game screen. Repeat uses the same quantity/cost and performs a fresh affordability check.
- Share captures the result grid without currency, close, or repeat controls.

## Starting timing values and tests

These are starting values, not final balance claims.

| Event | Starting value | Test and adjustment |
|---|---:|---|
| Button acknowledgment | 0.10 s | New players should see the selected quantity before transition in 9/10 trials. If presses feel ignored, strengthen the visual press state before extending duration. |
| Summon transition | 1.50 s | Observers should identify x1 versus x10 and notice a rare cue in 8/10 trials. If unclear, slow the paw-star formation by 0.15 s; if repetitive, shorten by 0.15 s. |
| Rare reveal minimum | 1.20 s | Players should identify the Animo, `NEW`, and rarity before advancing in 8/10 trials. If missed, delay input acceptance by 0.10 s; if frustrating, reduce the minimum by 0.10 s. |
| Result input unlock | 0.20 s after layout settles | Spam test must never trigger repeat before cards are visible. If accidental repeats occur, increase only this lock by 0.10 s and retest. |

## Five-component check

| Component | Design response |
|---|---|
| Clarity | Cost and quantity share one button; paw-star count previews quantity; rarity has shape and color cues. |
| Motivation | Featured splash, guarantee line, `NEW`, and visible rarity communicate potential value without long copy. |
| Response | Immediate button acknowledgment, always-visible Skip, fast reveal advance, and direct repeat/close choices. |
| Satisfaction | Water ripple, halo/star shapes, pet expression, rarity sound tier, and card outline scale with outcome. |
| Fit | Compass and observatory imagery connect Animo summoning to Math:World instead of using unrelated generic portals. |

## Required playtests

1. New player: identify Details, History, x1 cost, x10 cost, and guarantee without instruction.
2. Readability: observer explains quantity and whether a rare result is coming before the reveal.
3. Stress: spam summon, Skip, close, and repeat; currency is deducted exactly once per successful transaction.
4. Reduced motion: all reward and rarity information remains understandable without orbit or camera movement.
5. Economy failure: insufficient currency and transaction error return control without losing currency or showing false results.

## Built-in image-generation prompt set

### Banner

16:9 Math:World `ANIMO CALL` gacha banner using the v8 cream, navy, cyan, and restrained-gold UI. Calm title and `3★+ within 10` on the left; original featured white moon-fox, teal leaf-drake, and ember ram in a medium-detail moonlit observatory garden on the right. Currency 2,350 and close at top-right. Labeled `DETAILS` and `HISTORY` secondary buttons at bottom-left. Cream x1 / 25 and gold x10 / 250 summon buttons at bottom-right. Smooth colored lineart, hard primary shadow plus soft transition, lightly saturated AO, limited gradients, no paragraph or ornamental clutter.

### Summon transition

16:9 view slightly downward over the observatory reflecting pool. The young Wayfinder's cream-and-blue gloved hand releases a compass token into the water. One cyan-gold ripple, exactly ten orbiting paw-stars with nine cyan-white and one gold, and a translucent moon-fox silhouette beginning to form. Only a small `SKIP` control. Restrained bloom and coherent moonlight; no explosion or unrelated panels.

### Rare reveal

16:9 joyful featured moon-fox `LUNARA` leaping toward the viewer above the pool, preserving white fur, blue eyes, blue-tipped ear fins, crescent mark, tail, and compass charm. One restrained compass halo and four large gold diamond stars. Minimal left title block with `NEW` and `LUNARA`, small `SKIP`, and arrow-only continue control. Expression and silhouette carry the reveal; no card frame or excessive effects.

### Result

16:9 `RESULT` screen over the softly darkened observatory. Exactly ten equal cream cards in a 5×2 grid with portrait, element chip, and two-to-four diamond marks. Lunara first with one coral `NEW` ribbon and restrained gold outline; nine original supporting Animo with distinct silhouettes. Currency 2,100, close, share icon, and one x10 / 250 repeat button. No names, paragraphs, uneven card sizes, or heavy gold glow.
