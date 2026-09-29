---
slug: main-menu-localization-completion
status: ready-for-review
gdd_tags:
  - stage-progression
  - pet-system
  - gacha
  - player-experience
owner: implementation-agent
human_checkpoint: required
next_agent: code-review-agent
blocked_by: []
---

# Main Menu Localization Completion

Date: 2026-09-28

## Changes

- Localized the stage number, Player Hub, weapon ascension, Rebirth panel, and Pet Gacha copy through `LocalizationService`.
- Added Thai display names to all 25 pet definitions and 23 weapon tiers. SSR pet passive descriptions also have Thai text. Missing Thai fields fall back to English.
- Added static UXML localization and tooltip bindings. Runtime labels refresh when the locale changes.

## Verification

- Parsed `UI.json` and affected UXML files; checked localization keys and English/Thai format placeholders.
- `git diff --check` passed for the changed files.
- `unity test . --mode EditMode --output TestResults/localization-editmode-results.xml` could not run because the `unity` CLI is not installed on `PATH`; no results report was produced. The project is open in the Unity Editor, so a second batch Editor process was not started against that checkout.

## Review Notes

- Check Thai text fitting in the Hub, Gacha, and Rebirth panels in the Unity Editor.
- Existing server and validation errors may still arrive as English text; this change covers the authored UI and the common local interaction messages for the requested surfaces.
