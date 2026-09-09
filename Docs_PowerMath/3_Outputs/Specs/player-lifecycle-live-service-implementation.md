---
slug: player-lifecycle-live-service
status: draft
source: manual
gdd_tags: [server-authority, player-experience, leaderboard-profile]
owner: Codex
human_checkpoint: required
next_agent: reviewer
blocked_by: []
---

# Player lifecycle implementation handoff

Design and architecture approved by the user's “LGTM”. Placeholder art and opening content explicitly authorized.

## Implemented locally

| Area | Result |
|---|---|
| Save contract | Durable schema V3; raw schema/type validation before default repair; field-mask migration preserves unknown fields and existing receipts; upgrades increment an existing revision |
| Language | Thai default, English fallback, keyed JSON catalog, UXML bindings, startup/auth state translation, pre-login and in-game language control, cloud preference commands and local PlayerPrefs cache |
| Preparation | New players enter opening; legacy saves bypass it; persist selected character and completion; support Thai names, back navigation, duplicate confirms and lost-response retries |
| Appearance | Shared ricko/stellar catalog resolves selection art, hub, default portrait, combat image and optional Animator controller; distinct neutral placeholders work without supplied art |
| Opening | Localized authored text, optional hosted video URL with an explicit play action, text fallback and skip/continue |
| Updates | Invalid policy fails closed; new lifecycle writes recheck release policy; automatic reload bounded per target version; only owned build caches cleared; explicit manual reload remains available |
| Future authority | Typed lifecycle gateway and mailbox/event/announcement server contracts; direct adapter explicitly reports IsServerAuthoritative = false |
| Tutorial | Independent persisted version/checkpoint fields; authored tutorial progression intentionally unavailable until an approved step catalog is supplied |

The 2026-09-09 public-combat pass also moved the release check in front of direct login/default repair and added raw current-schema plus exact-revision validation before academic progression writes. See `public-webgl-combat-polish-audit.md` for evidence and remaining deployment limits.

## Authoring

- Edit `Assets/Project/Resources/CharacterPresentationCatalog.asset` in Unity. Fill each character's selection art, default profile icon, hub sprite, battle sprite and optional animation controller. Keep IDs `ricko` and `stellar` unchanged.
- Edit `Assets/Project/Resources/OpeningSequence.asset` for English/Thai opening text and optional hosted video URL. Empty fields produce the explicit placeholder flow.
- Video uses a URL because WebGL does not support embedded VideoClip assets. See [Unity's Web video documentation](https://docs.unity.cn/6000.1/Documentation/Manual/webgl-video.html).
- Add text to `Assets/Project/Resources/Localization/UI.json`. Static UXML text uses `class="loc-semantic.key"`; dynamic presenters resolve `LocalizationService.Get(key, args)` and re-render on locale change.
- Catalog coverage currently targets startup, authentication, onboarding and the core static menu labels. Existing dynamic gameplay/economy status copy and content names are not all translated by this pass. Do not bind an initial static key to a live counter: re-render its formatted key from its presenter.
- Name input has a starting maximum of 20 Unicode text elements. Verify the longest Thai names against final profile layouts before release.

## Bootstrap cinematic enhancement (2026-09-09)

The first-run preparation view now follows Figma section `118:479`: centered type-on/fade narrative, full-screen opening video, a looping character-selection video with invisible left/right targets, mirrored Ricko/Stellar detail compositions, separated colored-shadow character motion, a generated dot field, staggered black-square wipes, and an opposite-side name card. Character detail is modeled as explicit Entering, Holding, and Exiting states so input is disabled during transitions and the selected state never disappears on an arbitrary frame.

The existing authoritative checkpoints remain unchanged: `opening` is acknowledged before selection, character choice is saved before name entry, and the completion callback (which routes BootstrapScene to MainMenuScene) only fires after the final save succeeds and the white flash plays. Existing complete players continue to bypass preparation.

Editor/native builds use the checked-in opening and selection MP4 clips. Both definitions expose hosted URL fields for WebGL; if no valid selection URL is authored, the exact character overview sprites provide a usable static fallback. No build profile, dependency, Firebase rule, or scene-list setting was changed.

## Persistence behavior

Missing gamedata after a successful read initializes opening state. Existing gamedata with no onboarding state enters character choice, preserves the existing name, and bypasses opening. Invalid/future documents cannot pass the defaults planner. This prototype's school credential scheme remains unchanged.

Final preparation writes character, name, completion receipt and revision with a document update-time precondition. Retrying an acknowledged completion is a no-op; an incompatible payload or concurrent revision is rejected. Completion is not inferred from a nonempty display name. Preference writes are limited to preference/revision fields. Presentation receipts and economy fields are not cleared.

The raw shared-grade document remains the prototype storage unit, so unrelated students can cause update-time conflicts. A conflict reloads state rather than overwriting another writer. A lost response retries/checks the same preparation operation. The server-time field is left empty by the prototype adapter instead of fabricating server time from the device.

## Validation

- Unity 6000.5.3f1 compiles the implementation. Final focused run: **19 passed, 0 failed**, including both character flows. Browser bridge: **3 passed, 0 failed**.
- Focused suite covers migration guards/preservation, malformed scalar fields, command identity/concurrency, immutable completion retries, Thai input, invalid policies and both character UI flows.
- Browser bridge tests: `node --test Tools/Tests/web-cache-reload.test.cjs`.
- **164 catalog entries and 124 UXML bindings** pass JSON/UTF-8, duplicate-key, Thai/English placeholder parity and XML/key validation.
- UI test renders the actual UI Toolkit panel to `Logs/LifecycleValidation/character-selection.png`; Thai text and both distinct neutral silhouettes visually verified. Explicit Unity null checks ensure unassigned serialized sprite slots use placeholders.
- No live Firebase migration, authentication cutover, rules publication, WebGL deployment or dependency installation was performed.

## Broader regression findings

The first broader EditMode run completed 36 tests with 30 passes and six failures:

- AdminResetTests.AdminPanelController_ResetButton_EntersConfirmationState: expected confirmation text, received initial reset text.
- Four PlayerHubDataTests fail setup because their reflected `version` field is missing from the current pet catalog class.
- RunSettlementPolicyTests.Calculate_AtStage50_ComputesAccurateAward: expected a positive award, received zero.

These failures were outside the focused lifecycle suite. The owning admin, pet catalog and settlement production code was not changed in this implementation. They remain unresolved; a clean full-project test pass is not claimed.

## Before public protected rewards

The new contracts are preparation for server authority, not an implementation of it. The current permissive rules and plaintext prototype login still exist. Choose the backend host and legacy enrollment strategy, then implement authenticated commands, trusted time and transactional receipts, migrate all protected economy/attempt writers, and deploy restrictive client rules together. Merely adding a secure mailbox endpoint while leaving direct wallet writes enabled is insufficient.

Full authored tutorial lessons, final art, hosted opening media, comprehensive dynamic gameplay translation, cross-device Firebase smoke tests and a deployed-browser refresh test remain follow-up work. Existing player progress must not be reset to supply any of these.

Release note: the checked-in Cloudflare manifest still advertises the earlier schema/version. It was not published or edited. A real release must publish the matching minimum-client/schema policy and use hashed or release-pinned build/content URLs; this is part of the pending deployment cutover.
