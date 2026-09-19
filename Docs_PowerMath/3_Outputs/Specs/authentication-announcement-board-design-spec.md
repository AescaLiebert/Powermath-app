---
slug: authentication-announcement-board
status: approved
source: manual
gdd_tags:
  - feedback
  - player-experience
  - guardrails
owner: game-design-agent
human_checkpoint: not-required
next_agent: architect-agent
blocked_by: []
---

# Design Spec: Authentication Announcement Board

## Player Goal and Context

Before signing in, a player can quickly understand what changed in the current and earlier patches without leaving the game. The board must remain optional and must never obstruct authentication after dismissal or content failure.

## Interaction Rules

1. Authentication ready loads the bundled catalog and auto-opens the board unless suppressed for the current local date.
2. The featured patch, or the first authored patch, is selected.
3. Selecting a left-column tab replaces only the right document and resets its scroll offset.
4. Close dismisses the board for the current application session.
5. “Don't show again today” opens a confirmation. Confirming stores a non-critical local preference and closes the board.
6. The manual announcement button always opens the board, even when automatic display is suppressed.
7. Authentication busy/success and scene exit force a stable hidden state and stop announcement media.

## Landscape Composition

- Permanent two-column layout.
- Left: vertically scrollable patch tabs.
- Right: fixed document header and independently scrollable content.
- No portrait or narrow-screen stacking behavior.
- Safe-area and landscape aspect-ratio differences may change available dimensions without changing information architecture.

## Presentation State

`Hidden → Opening → Browsing → ConfirmingSuppression → Closing → Hidden`

Catalog/media failure returns to a non-blocking hidden or readable fallback state. Escape closes the confirmation first, then the board. Closing restores login focus.

## Content Rules

- Raw authored tags are escaped before approved inline Markdown is converted to UI Toolkit rich text.
- Supported blocks: H1-H3, paragraphs, bullet items, callouts, dividers, image, frames and video.
- Thai content falls back to English when absent.
- Missing media produces readable localized/fallback text and never removes the rest of the document.

## Five-Component Evaluation

| Component | Design Response |
|---|---|
| Clarity | Selected tab, patch title/version/date, two explicit close choices and confirmation copy |
| Motivation | Featured patch immediately communicates relevant changes without requiring sign-in |
| Response | Tab selection and close actions update immediately; content failure cannot block login |
| Satisfaction | Shared motion plus UI SFX acknowledge opening, selection and dismissal |
| Fit | Reuses the Authentication navy/gold/cream visual language and existing localization |

## Numbers Policy

- Modal lifecycle uses the existing shared `UiMotionProfileDefinition` starting values and its existing repeated-navigation test criteria.
- Frame animation uses a starting value of 100 ms per frame. Validate that authored motion reads clearly without distracting from text; increase the interval if it competes with reading or decrease it only when source animation appears visibly choppy.
- Video presentation uses a starting RenderTexture size of 960×540. Profile WebGL memory and readability; reduce resolution if transient memory or upload cost is excessive.

## Risks and Abuse Cases

- Raw markup injection: escaped before formatting.
- Duplicate patch identity: catalog validation rejects the catalog.
- Local clock changes: can alter only a convenience suppression preference; no rewards or authority are affected.
- Large media: authoring/profile responsibility; failure remains non-blocking.
- Rapid patch tapping: renderer clears previous media before drawing the next document.

## Playtest Scenarios

- New player understands the two columns and closes without instruction.
- Rapidly alternate patches while scrolling both panes.
- Switch TH/EN while open and inspect Thai combining marks, emoji and fallback glyphs.
- Confirm suppression, reopen manually, restart same day, then simulate date rollover.
- Test missing catalog, malformed content, missing image and failed video.
- Test keyboard focus, Escape behavior, reduced motion and muted SFX.

## Approval

Project owner approved the design and landscape-only constraint on 2026-09-17.
