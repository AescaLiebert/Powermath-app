---
slug: authentication-announcement-board
status: approved
source: manual
gdd_tags:
  - feedback
  - player-experience
  - guardrails
owner: implementation-agent
human_checkpoint: not-required
next_agent: implementation-agent
blocked_by: []
---

# Task Card: Authentication Announcement Board

## Player-Facing Goal

Show localized patch announcements over the Authentication screen in a landscape-only, two-column board. The left patch list and right content document scroll independently. Players can close the board, suppress automatic display for the current local day, or reopen it manually.

## Approved Scope

- Bundled, data-driven patch catalog updated with each client patch.
- Stable patch IDs connect tabs to localized content.
- Sanitized Markdown subset: headings, paragraphs, bold, italic, inline code, lists, callouts, dividers and authored media.
- Thai and English content with English fallback.
- Static Resources images, Resources frame animations and StreamingAssets/hosted video.
- Shared UI motion, reduced-motion handling and UI SFX binding.
- Local device daily suppression only; no account/server acknowledgement.
- Landscape-only two-column composition. Narrow-screen stacking is explicitly excluded.

## Source and Approval

- Requested by the project owner on 2026-09-17.
- Initial design approved with “lgtm”.
- Landscape-only revision approved with “lgtm”.
- Implementation authorized with “lgtm implementation”.

## Non-Goals

- Remote publishing or Cloudflare/Firebase deployment changes.
- Arbitrary HTML, CSS, scripts or unrestricted CommonMark.
- Protected rewards, authenticated announcement receipts or gameplay state.
- Native runtime GIF decoding dependency. Animated media uses frame sequences or video.

## Acceptance Criteria

- Automatic board display occurs when Authentication is ready unless suppressed today.
- Manual open remains available while daily suppression is active.
- Patch and content panels scroll independently.
- Patch selection updates the localized document and selected state.
- Closing restores login focus; authentication busy/success hides the board.
- Daily suppression expires on local date change.
- Invalid catalog or media fails closed without blocking login.
- Dynamic patch buttons receive UI SFX binding.
