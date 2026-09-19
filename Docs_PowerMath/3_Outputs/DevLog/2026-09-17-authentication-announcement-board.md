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
next_agent: human
blocked_by: []
---

# DevLog: Authentication Announcement Board

## Implemented

- Added a bundled, schema-validated announcement catalog with stable patch IDs and TH/EN content.
- Added a fixed landscape two-column modal with independently scrollable patch and content regions.
- Added a sanitized Markdown renderer for headings, paragraphs, emphasis, inline code, lists, callouts, dividers and media directives.
- Added Resources image, frame-sequence animation and StreamingAssets/hosted video presentation with lifecycle cleanup.
- Added manual open, session close, daily suppression confirmation, Escape handling and login-focus restoration.
- Reused shared `UiPanelLifecycle`, `UiMotionDriverProvider` and dynamic `UiSfxAudioBinder` integration.
- Kept local dismissal separate from future authenticated announcement acknowledgement.

## Verification

- Unity 6000.5.3f1 script compilation: passed.
- UXML/USS imports: passed with no import errors.
- Catalog JSON and UXML XML parsing: passed.
- `AnnouncementBoardTests`: 5 passed, 0 failed, 0 skipped.
- `git diff --check`: passed; only existing line-ending conversion warnings were reported.

## Follow-Up Manual QA

- Inspect final typography and two-column proportions in supported landscape WebGL resolutions.
- Verify production font fallback for Thai combining marks, emoji and unusual symbols.
- Profile authored announcement videos on WebGL before shipping large media.
