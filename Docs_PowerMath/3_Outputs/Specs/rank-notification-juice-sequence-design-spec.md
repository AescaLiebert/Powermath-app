# Design Spec: Rank Notification Game Juice & Sequence Refactor

## 1. Overview & Player Experience
When a player achieves a rank promotion or experiences a rank adjustment, the previous prototype modal box is replaced by a game juice winning-screen style overlay.
- Visuals: Prominent themed rank notification banner with glowing aura and bold rank badges.
- Fluid sequence:
  1. Rank Banner pops up with punchy scale bounce and audio fanfare.
  2. Route transition text fades in with slide-up showing `[Previous Rank Icon] ➔ [Destined Rank Icon]`.
  3. Holds on screen with breathing glow, waiting for player tap anywhere.
  4. Upon tap, text immediately clears and the banner slides away smoothly off-screen.

## 2. Visual & Juice Details
- **Promotion Theme**: Radiant gold/amber aura, sparkling golden header ("RANK UP"), celebratory fanfare arpeggio.
- **Adjustment/Demotion Theme**: Cool steel/frost aura, polished silver-blue header ("RANK ADJUSTED"), soft resonant chime tone.
- **Route Display**:
  - Previous Rank: Compact icon badge (e.g. `[S] SILVER`, `[G] GOLD`, `[D] DIAMOND`)
  - Arrow: Glowing connector `➔`
  - Destined Rank: Highlighted icon badge with glowing aura
- **Tap to Continue Prompt**: Pulsing subtitle "TAP ANYWHERE TO CONTINUE".
- **Dismissal Animation**: Immediate route fade-out + upward slide-away (`translate: 0 -120px; opacity: 0;`).
