# Math:World Leaderboard UI Direction — v13

> [!CAUTION]
> Superseded for visual art direction by `master-prompt-guide-art-direction-v14.md`. The v13 mockup and this document may be used only as information-architecture studies. Do not use the v13 character rendering, text density, first-place showcase, panel treatment, or background treatment as future art references.

## Status and source

This mockup visualizes the approved `@tag:leaderboard-profile` GDD rules and the approved `leaderboard-profile-analytics-design-spec.md` wide WebGL layout.

- The Animo Gacha banner and result screens are the visual-system anchors.
- The supplied arena, event, and medal-road screenshots are layout/function references only. Their brands, characters, currencies, labels, modes, and rewards are not Math:World content.
- `MIRA`, `LEO`, `ARIN`, `KAI`, `LUNA`, portraits, loadouts, balances, Stages, and damage values in the mockup are non-canon presentation placeholders.
- The geometric Animo remain non-canon placeholders until approved pet designs are supplied.

## GDD rules represented

- The leaderboard is a friendly social showoff feature.
- The authenticated cohort is locked to `GRADE 4 · LEVEL 1`; there are no grade tabs.
- Ranking is `Best Stage` descending, then weighted accumulated Rank Currency descending.
- Silver, Gold, and Diamond use the approved relative weights `5 / 7 / 10`; Power Coins do not contribute.
- `YOUR STANDING · YOU` is pinned above the ranked scroll list and uses a green treatment.
- Rows show display rank, public avatar/name, Current Stage, Best Stage, Silver/Gold/Diamond balances, equipped pet, equipped weapon, and Total Damage.
- Rank 1 uses Diamond VIP treatment, rank 2 Gold, and rank 3 Silver. Badge shape and number keep the states readable without color.
- The list fetches on open and through explicit manual Refresh only. `Updated 12:42` communicates freshness without implying polling.
- Login usernames, educational analytics, hidden audit score/count, and audit thresholds never appear.

## Visual hierarchy

1. `LEADERBOARD`, the locked cohort, and the ranking explanation.
2. The pinned green self row.
3. Rank badges, display names, and Best Stage for the top three.
4. Rank Currency, loadout tiles, and Total Damage.
5. Graphic arcs, halftones, paper grain, and other background decoration.

## Rendering balance

- Showcase characters and avatars use smooth youthful anime gradients, simplified 5.5–6-head proportions, large costume/hair masses, soft colored contours, and selective facial detail.
- Animo use two or three color masses, firmer local-color outlines, minimal interior marks, and one simple shadow.
- Both share one hard shadow, one soft transition, lightly saturated cyan-violet or muted-peach AO, and narrow highlights.
- UI surfaces inherit the Gacha system: cream cards, navy typography, cyan/cobalt fields, restrained coral, broad arcs, halftone dots, subtle paper grain, and shallow shadows.
- No physical arena scenery is used. The leaderboard sits on the flat graphic stage so information remains dominant.

## Final built-in image-generation prompt

Create a shippable 16:9 Math:World Fun Leaderboard using the Animo Gacha cream/cyan/cobalt/coral graphic system. Show `LEADERBOARD`, locked `GRADE 4 · LEVEL 1`, `Best Stage first, then weighted Rank Currency`, an Updated timestamp, manual Refresh, and Close. Place a Diamond first-place showcase with an original youthful anime adventurer, equipped geometric Animo, sword, name, Best Stage, and three Rank Currency values at left. At right, pin a green `YOUR STANDING · YOU` row above a scroll list with Diamond/Gold/Silver VIP rows for ranks 1–3 and quieter later rows. Every row shows rank, avatar, public display name, Current and Best Stage, Silver/Gold/Diamond values, distinct pet and weapon tiles, and Total Damage. Balance smooth anime character gradients with shape-driven almost-vector pets and UI: colored contours, sparse interior lines, one hard shadow plus one soft gradient, lightly saturated AO, selective highlights, and hierarchical detail. Do not include grade tabs, Power Coin scoring, educational analytics, hidden audit information, arena battles, event tabs, reward roads, branded reference content, dark competitive scenery, heavy lineart, or ornamental clutter.

## Readability checks

- A new observer should identify the locked cohort, ranking key, and self row within one glance.
- Best Stage must read before currency and Total Damage.
- The green self treatment must remain distinct if the player is also top three.
- VIP rank must remain understandable without color through badge number and silhouette.
- Long localized names must not displace rank, Current/Best Stage, or currency fields.
- Freshness must read as manual/static; no animation may imply automatic polling.
