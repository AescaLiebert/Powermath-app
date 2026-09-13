# Leaderboard List Layout Regression

## Player goal and system rules

Players can compare standings in the right-hand scrolling list while the first-place
throne remains independent. Ordinary rows use the editable `LeaderboardListRow.uxml`
template, and all three Rank Currency icons remain visible.

## Five-component check

- Clarity: the pinned self row and scrollable standings stay in one list column.
- Response: wheel and touch scrolling affect the standings list, not the header.
- Satisfaction and fit: Gold, Silver, and Diamond values use their authored game icons.

## Regression scenarios

1. Open Leaderboard at 1920×1080 and confirm the list is inside the right-hand card.
2. Test an empty result and ten or more rows; the header and throne must not move.
3. Scroll with mouse wheel and Android touch; the list remains clipped by its card.
4. Confirm Gold uses the full `coin_gold_1` sprite in the pinned and ordinary rows.
5. Open `LeaderboardListRow.uxml` in UI Builder and confirm the Gold icon is selectable.
6. Refresh repeatedly and jump to the authenticated player's row; no duplicate rows appear.
