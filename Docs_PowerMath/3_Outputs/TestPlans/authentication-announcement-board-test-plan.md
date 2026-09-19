---
slug: authentication-announcement-board
status: approved
source: manual
gdd_tags:
  - feedback
  - player-experience
  - guardrails
owner: qa-agent
human_checkpoint: not-required
next_agent: human
blocked_by: []
---

# Test Plan: Authentication Announcement Board

## Automated EditMode

- Reject duplicate patch IDs.
- Verify local-date suppression expires after date rollover and is board-wide for the day.
- Verify raw rich-text tags are escaped before Markdown formatting.
- Verify Authentication UXML contains two independent ScrollViews and all required controls.
- Verify manual opening displays the board and selects catalog content.

## Manual Editor

- Launch Authentication without remembered credentials; board opens above login.
- Select every patch and independently scroll both columns.
- Close; confirm login focus and interaction are restored.
- Open manually, invoke daily suppression, cancel, then confirm.
- Verify manual open still works after suppression.
- Switch Thai/English while the board and confirmation are open.
- Verify missing media fallback and media disposal on patch change/close.
- Enable reduced motion and mute SFX; confirm functionality remains clear.

## WebGL Landscape

- Test supported landscape aspect ratios and safe areas without layout stacking.
- Refresh after daily suppression and confirm auto-open remains disabled for that local day.
- Verify StreamingAssets/hosted video load, failure handling and transient memory.
- Verify Thai combining marks, emoji and symbols render with the production font fallback configuration.

## Regression

- Login submission and Enter key.
- Language flyout.
- Shared Settings overlay.
- Status toast messages.
- Remembered-credential auto-login does not flash or block on the announcement board.
