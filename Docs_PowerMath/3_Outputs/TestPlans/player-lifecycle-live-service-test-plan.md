---
slug: player-lifecycle-live-service
status: draft
gdd_tags: [server-authority, player-experience, leaderboard-profile]
owner: Codex
human_checkpoint: required
next_agent: reviewer
blocked_by: []
---

# Player lifecycle verification

## Automated

Run the Unity EditMode filters:

- PowerMath.Tests.EditMode.PlayerLifecycleTests
- PowerMath.Tests.EditMode.VersionAndMigrationTests
- PowerMath.Tests.EditMode.PlayerPreparationUiTests

The UI test enters an empty Play Mode scene with an Editor-only command adapter. It never authenticates or writes to Firebase. It confirms Ricko and Stellar, including Thai input, through UI Toolkit submit events.

Run `node --test Tools/Tests/web-cache-reload.test.cjs` for cache ownership, retained query parameters, blocked session storage and rejected cache deletion.

Validate localization JSON uniqueness, nonempty Thai/English entries, format-placeholder parity and all UXML loc-key references.

## Manual staging checks before a public release

| Scenario | Expected result |
|---|---|
| Existing save with pending combat presentation | Preserve and replay the receipt; choosing a missing protagonist does not reset progression |
| Confirmed account with absent gamedata | Opening appears, followed by character and display-name preparation |
| Permission/network/parse failure | Retry screen; no new save inferred |
| Future schema or malformed wallet scalar | No repair/write attempted |
| Reload after each onboarding checkpoint | Resume latest acknowledged state |
| Drop response after final confirmation | Reload/retry returns completed profile without duplicate initialization |
| Two tabs confirm different characters | One committed result; conflicting tab must rehydrate |
| Switch locale, refresh, then sign in on another device | Stored account preference restored |
| Switch accounts during preference write | No response from the previous account applied to the active player |
| Long Thai names and narrow aspect ratio | Combining marks intact, name and controls readable |
| Hosted opening video denied/unavailable | Text and skip/continue remain usable |
| Incompatible manifest repeatedly served | Automatic reload stops after the target guard; manual reload available |
| New mailbox/event backend, when integrated | Recipient/time/catalog checks, unique business claim, atomic rewards, and denied direct protected writes |

No live Firebase, hosted video or deployed WebGL checks are claimed in this local verification pass. Record their results against a staging environment before public deployment.

The broader regression failures are recorded in `../Specs/player-lifecycle-live-service-implementation.md`.
