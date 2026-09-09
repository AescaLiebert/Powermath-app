---
slug: public-webgl-combat-polish
status: implemented-awaiting-playtest
source: manual
gdd_tags: [combat-attempt, stage-progression, server-authority, feedback, player-experience, guardrails, playtest]
owner: Codex
human_checkpoint: required
next_agent: human-playtest
blocked_by: []
---

# Public WebGL lifecycle and combat implementation audit

Design approval was recorded on 2026-09-09. The user selected YouTube with the keypad beside or below the video on narrow screens. This audit distinguishes implemented code from content, deployment, and trusted-backend work.

## Implemented and repaired

| Area | Evidence and resulting behavior |
| --- | --- |
| Login release guard | `DirectFirestoreAuthenticationService` fetches and evaluates the release manifest before direct Firestore authentication can run its defaults repair. Hard-update, maintenance, invalid-policy, and future-schema states fail before player mutation; update-recommended remains usable. |
| Gameplay save protection | `FirestoreAcademicProgressionStore` re-reads the raw document, validates current schema and exact revision, then applies its masked PATCH with `currentDocument.updateTime`. Stale tabs, malformed/legacy data, future schema, and lost-response retries require reload rather than replaying an old reward snapshot. Unknown fields, wallet state, and pending presentation data remain outside the patch unless intentionally updated. |
| Browser refresh/cache | Existing schema V3, raw pre-repair inspection, scoped cache deletion, target-version loop guard, pending attempt/presentation receipts, and onboarding checkpoints remain the recovery boundary. Browser bridge tests cover owned-cache cleanup and bounded reload. |
| Localization/preparation | Existing Thai-default/English-fallback catalog, UI Toolkit semantic bindings, local locale cache plus cloud preference, opening/character/name preparation, and independent tutorial checkpoint remain in place. Dynamic gameplay and content-name translation is incomplete and must not be described as comprehensive. |
| Future live service | Existing typed mailbox/event/announcement/release contracts remain transport boundaries. The direct Firestore adapter explicitly reports that it is not server authoritative. No protected live reward was enabled. |
| Battle entrance | A bootstrap-wide interaction lock is acquired before the Main Menu appears. Nine alternating square panels reveal the scene while player/enemy canvas entrances and the transparent `———— × BATTLE! × ————` title run concurrently. Input unlocks only after session readiness and both presentation branches complete. Pending-presentation recovery skips the ordinary entrance and retains its own lock. The timeout reveals a stable screen but cannot unlock an unresolved session. |
| Question video | The WebGL bridge invalidates stale iframe callbacks, destroys replaced players, handles API failure once, disables supported YouTube controls, keyboard shortcuts, fullscreen, and pointer interaction, and uses inline playback. Wide screens dock the video at left; narrow/portrait screens put it above a full-width answer panel so the keypad stays usable without covering the iframe. |
| Result feedback | Success/failure uses an absolute round sticker beside the result copy. Every result keeps its existing buildup and then holds for one additional real-time second before battle execution. |
| Biome sequence | Destination visuals are no longer bound before the outgoing background transition. After death, the current background fades to zero, the sprite switches while invisible, and the new background fades to full opacity. No background scale is animated. All seven biomes reference existing `_2` art from their deterministic midpoint onward; stages 1-30 switch at stage 16. Refresh renders the accepted destination directly and skips cosmetic replay. Biome titles use the same transparent white ornament treatment and read `BiomeTitle`, not `BiomeId`. |
| Interaction ordering | The presenter owns destination visuals until the presentation-completed acknowledgement saves. The shared gate blocks lobby, navigation, modal dismissal, and actor pointer juice during bootstrap, result, death, biome fade, and enemy entrance. |
| Death hierarchy | Normal and mini-boss enemies drop 4% of their displayed height, flash white, and fade in 0.70 s. Big/final bosses and the player use the major 1.60 s red drop, local shake, pulses, and fade. Reduced Motion removes shake/flicker and keeps 0.50 s/1.00 s durations. Actor rotation is held at the authored pose. |
| Battle sound | `BattleSfxLibraryDefinition` and `Resources/BattleSfxLibrary.asset` provide slots for repeated swing/hit/click arrays and distinct outcome/death/transition cues. Missing authored clips use deterministic synthesized sweep/transient/chord cues; swing, hit, and click each cycle three variants. Missing audio never blocks combat. |

## Confirmed limits and follow-up boundaries

- YouTube supports hiding standard controls and disabling keyboard/fullscreen parameters, but the embedded provider still owns playback policy. Its terms prohibit covering the player with application controls. The accepted layout therefore places the keypad beside/below the iframe. Actual iOS/Android autoplay behavior remains a deployed-browser test; an application Play button may be needed if a target browser blocks delayed autoplay.
- Biome assets still use generic authored fallback titles such as `Biome 1`. The runtime now displays the title field rather than the stable ID, but final Thai/English place names remain a content input and should be supplied before calling the presentation final.
- The SFX asset is ready for licensed authored clips. Procedural fallbacks make the behavior functional; final mix, three authored repeated-cue variants, and the 20-attempt fatigue check remain an audio-authoring/playtest checkpoint.
- The tutorial contract deliberately rejects advancement until an approved step catalog exists. Opening video/text and tutorial lessons remain content inputs.
- Mailbox claims, timed-event eligibility, announcement acknowledgement, trusted time, transaction receipts, Firebase Authentication, and restrictive Firestore rules require a real backend cutover. Client contracts alone are not server authority.
- The checked-in public release manifest, WebGL build, Cloudflare content, Firebase rules, and live data were not changed or published. Their build/publish/security checkpoints remain required.

## Verification on 2026-09-09

- `node --test Tools/Tests/youtube-question.test.cjs Tools/Tests/web-cache-reload.test.cjs`: **10 passed, 0 failed**.
- Unity 6000.5.3f1 imported the changed scripts/assets and completed two domain reload compilations with no recent C# compiler, UXML, or USS import errors in `Editor.log`.
- Added focused EditMode coverage for raw academic-save schema/revision guards, exact midpoint background selection, global actor-click gating, major-death classification, and death-duration hierarchy. The Unity tests were not executed in this pass because the project was already open in the Unity Editor; run the filters in the test plan before merging or publishing.
- `git diff --check` reports only pre-existing whitespace findings in unrelated AuthenticationScene and LeaderboardPanel edits. No scoped combat/lifecycle patch introduced a whitespace error.

## Review decision

Code is ready for the required human game-feel and real-browser checkpoint. A public build is not ready until focused Unity tests pass, final biome names/audio are accepted, target mobile browser playback is verified, and the release manifest/build deployment is approved as one matching release.
