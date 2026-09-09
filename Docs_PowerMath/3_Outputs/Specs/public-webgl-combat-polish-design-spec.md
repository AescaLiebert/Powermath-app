---
slug: public-webgl-combat-polish
status: approved
source: manual
gdd_tags: [combat-attempt, stage-progression, server-authority, feedback, player-experience, guardrails, playtest]
owner: game-design-agent
human_checkpoint: required
next_agent: implementation-agent
blocked_by: []
---

# Public WebGL combat presentation design

Approval recorded 2026-09-09: the user selected **Approve the proposed design** for implementation and **Keep YouTube with keypad beside/below video**. Apply the documented YouTube options and nonoverlapping responsive layout; do not implement the alternate owned-video path. Final subjective timing/audio/art review remains a playtest checkpoint. Implementation reuses the accepted ADR-012/014 presentation ownership, shared gate, recovery and ScriptableObject boundaries.

Prepared 2026-09-09 from the user's goal objective and `public-webgl-combat-polish-task-card.md`. This is a proposed presentation specification, not accepted architecture or a claim of implementation. It follows `game-design-agent.md`, `/implement-feature`, and the game-design skill's combat, audio, UI/UX, pacing and persistence guidance. It changes no damage, scoring, timer, reward or settlement rules.

## 1. Player experience description

The player enters a layered battle scene through a clean sliding-square transition. The world remains visible behind a small white, ornamental Battle title. The player and enemy arrive while that title plays; when loading and presentation are both finished, the controls become available together. The scene feels composed and elegant rather than delayed by successive title screens.

After answering, a readable success or failure sticker sits beside the result panel. The player has an additional second to understand the result before the character acts. A hit has a short swing followed by a distinct contact sound; an incorrect answer has a gentle failure cue, not a mocking or punitive sound.

When the enemy dies, its entire death motion finishes before the background changes. The outgoing image fades completely away; the new image fades in at its existing size. The biome's display name appears in the same white line-and-star language as Battle. The next enemy then enters and the controls return. Halfway through a biome, a second background provides a sense of travel without changing gameplay rules.

Input context: the user explicitly targets public WebGL and mobile question playback. Specify browser pointer/keyboard behavior first, with touch equivalents below. The actual GDD is `GDD_PowerMathProject.md`; it has no `@tag:platform-input`, and `project-stack.md` is an unfilled template. This draft does not assert an undocumented mobile-first platform policy.

## 2. Grounding in the current implementation

These are observations of the working tree, including pre-existing uncommitted actor and stage changes. Those changes were inspected only. Line numbers may move during parallel repairs.

| Area | Observed implementation | Design consequence |
| --- | --- | --- |
| Entrance | `MainMenuTransitionController.NotifySessionReady` starts `PlayBootstrap`; that routine completes Battle title entry/hold/exit before scene and actor reveal. Its safety routine forces final UI after a timeout. | The requested overlap between actor loading and the title is a new flow. Finishing or cancelling cosmetic UI must never imply that session data is ready. |
| Biome order | `CombatLobbyPresenter.ResolutionRoutine` plays battle feedback before `PlayBiomeTransition`. Inside `CombatFeedbackPlayer.PlayBattleResolution`, the `StageAdvanced` branch already prepares the next encounter and plays `EnemyAppear`. | The next enemy can appear before the subsequent biome transition. The presentation sequence must have a single, unambiguous next-enemy entrance after the background change. |
| Background | `CombatLobbyCompositionRoot.CrossfadeBiomeBackground` raises a second image's opacity while the old image remains visible. | This overlaps images; the request requires outgoing alpha to reach zero before switching and fading in. |
| Biome content | `BiomeDefinition` has one `backgroundSprite` and a `fallbackTitle`. `Biome01.asset` currently uses `Biome 1`. The view reads `BiomeTitle`, with an existing backdrop/card treatment. | A second background and authored Thai/English place names are content inputs. Never show a stable biome ID as the proposed title. |
| Result | Integrated combat uses `CombatFeedbackPlayer.PlayAnswerFeedback`, with different success and failure durations and optional damage-breakdown rows. | Add the requested second after the final result content becomes readable. Do not tune the standalone `QuestionSequenceController` demo and assume production combat changed. |
| Death | `ActorPresentationController` currently uses one Dying duration and drop/rotation/white-fade behavior for both player and enemy. It exposes white-flash and reward presentation signals. | The requested normal/mini-boss and player/big-boss treatments need explicit classes. Preserve presentation signal semantics and existing work; signals must not award currency. |
| Audio | `CombatAudioPlayer` synthesizes tones/chords. No `.wav`, `.ogg` or `.mp3` files were found under `Assets/Project` in this audit. | An authored battle sound pack is outstanding; the design does not claim placeholder replacement is complete. |
| Recovery | `CombatPresentationPlanBuilder.BuildDeathRecovery` explicitly builds PlayerTakeDamage, PlayerDie and ShowStageResult. `CombatInteractionReadinessPolicy` rejects pending required receipts. | Player Death presentation and its reset summary remain required on recovery. Refresh skips optional transition effects only. |

## 3. Interaction flow and state rules

### Entrance

```text
Authenticate and resolve saved player state
  -> Complete opening/preparation only when the existing lifecycle requires it
  -> Sliding squares carry the scene transition
  -> Main Menu: actor/session preparation || transparent Battle overlay
  -> Wait for session, actors, required recovery and overlay completion
  -> Enable interaction together
```

An absent local cache is not proof of a first account. This presentation consumes the lifecycle's confirmed opening/preparation decision; it neither creates player data nor marks an opening/tutorial complete. An unresolved save, incompatible version, unavailable session or required Death receipt keeps its own gate after cosmetic presentation ends.

### Answer and combat

```text
Commit the attempt -> play its locked question
  -> existing preparation and answer window
  -> accept one result -> save result
  -> show final result + sticker -> retain for one additional second
  -> execute existing player attack / enemy response order
  -> if enemy died, finish death
  -> optional biome or midpoint fade sequence
  -> next encounter enters -> stable ready state
```

Pointer clicks, touch taps and keyboard activation have identical eligibility. Inputs during a presentation lock do not queue a future Attack, dismiss a title, dismiss a result, or skip death. During an active answer window, valid numpad input is acknowledged immediately and Submit remains single-use. The extra result hold does not extend a deadline or reopen answer submission.

| State | Entry | Exit and next state | Interrupt and recovery | Resource cost |
| --- | --- | --- | --- | --- |
| Scene transition | Lifecycle authorizes Main Menu entry | Square wipe completes; start/continue menu preparation and title | Scene unload/cancel removes cosmetic surfaces; readiness stays governed by the lifecycle | None |
| Entrance overlay | Main Menu presentation root exists; actor loading may still be in progress | Title and actor/session preparation complete; required receipts clear; enter Ready | No player skip. Refresh reconstructs accepted session and can omit optional title. Slow loading retains a plain loading status after title ends | None |
| Question/result | Existing committed attempt; result begins only after accepted resolution | Final content appears, requested added hold finishes, then battle execution | Refresh recovers the same committed attempt/result. It cannot reroll, retry as a new attempt, cancel a consumed cooldown or add a reward | Existing attempt commitment only; presentation consumes nothing |
| Enemy death | Accepted enemy defeat after hit response | Actor reaches hidden; continue stage presentation or terminal result | No input interruption. Refresh loads saved destination; optional enemy motion may be omitted when recovery allows | No separate grant |
| Background transition | Death complete, stage outcome saved, destination background differs | Old alpha zero -> switch -> new alpha full -> title finished -> enemy entrance | No click/tap/key skip. Refresh loads final background from saved stage and skips cosmetic travel | None |
| Enemy entrance | Destination background stable and title finished | Encounter and action queue stable; enter Ready or EventReady | Cancelled scene work may not reveal an obsolete actor. Refresh reconstructs the saved encounter, never rerolls identity or HP | None |
| Player death | Accepted player defeat, or pending required Death receipt | Longer death motion -> required reset summary -> existing acknowledgement/settlement flow | Refresh preserves and replays the existing required Death presentation. Do not acknowledge it merely because cosmetics were skipped | Existing once-per-run settlement only |
| Final boss defeat | Accepted final victory | Longer boss motion -> existing RunComplete flow | No next-biome or next-enemy sequence; refresh restores terminal outcome and required receipts | Existing reward rules only |

All UI gates must cover Attack, actor click actions, menus, map, modal shortcuts and keyboard bindings during the biome sequence. A visual blocker alone is insufficient evidence. If presentation loses its scene or an art reference, restore deterministic final visuals where possible; never fabricate successful saving or clear another system's lock. Authoritative errors retain a clear retry/reconnect state.

## 4. Juice specification

All values labelled **Starting value** below are proposals for playtesting, not accepted tuning. Structural requirements such as zero/full opacity and the user-requested additional second are not tuning estimates. Use elapsed real time so a low frame rate does not change authoritative timing.

### Battle and biome titles

- Exact composition: `──── ✦ Battle! ✦ ────`. For a biome, substitute its localized display name. The user's `──── × Battle × ────` example defines the ornamental arrangement; use a small star/cross ornament with equivalent visual weight.
- White lettering, fine symmetrical rules, a small ornament on each side, ample spacing and the existing bilingual HYWenHai font. Center the composition over the battle scene, clear of health and action controls. No title background, panel, card, full-screen color fill or old kicker. The scene remains visible.
- Rules extend outward, ornaments fade in, the title settles with slight vertical motion, and the whole composition gently fades out. No bounce, repeated flashes or camera zoom. Screen shake and particles: none for these titles. A restrained airy shimmer supports the visual entrance.
- Starting value: title entry **0.25 s**, readable hold **0.80 s**, exit **0.30 s**; vertical settle **8 reference-layout pixels**. Test Thai and English titles with new players. Starting pass target: correct title recall in at least **8 of 10** observations, with no clipped glyphs or obstructed controls. If unreadable, increase hold by **0.15 s** or shorten ornamental lines; if perceived as a delay, reduce hold after confirming loading overlap. Fit ornaments to long names rather than shrinking text below the established UI text size.
- Starting value: scene squares cover in **0.35 s** and reveal in **0.35 s**, eased without overshoot. Use the current palette, with squares translating as a mask rather than scaling the background. Test slow/fast loads and viewport changes. Pass: no exposed unprepared frame, no stale cover, and no clickable underlying game during the handoff; lengthen coverage only if the handoff is visibly discontinuous. A load stall displays loading feedback; it does not repeat the wipe.
- Starting value: actor entrance **0.45 s**, running alongside the title as actors become ready. Preserve authored stage depth and actor positions. No battle camera movement is added. Pass: the player can identify both actors before controls unlock; if not, reduce entrance travel or extend the final settle rather than add another blocking title.

### Biome and midpoint transition

- Strict order: finish enemy death; fade old background alpha **100% -> 0%**; switch while invisible; fade new alpha **0% -> 100%**; finish title; enter the next enemy; unlock once stable. Never overlap old and new visible background images. **Never animate background scale, position, rotation or camera zoom.** Preserve its authored framing throughout.
- Starting value: fade out **0.70 s**, fade in **0.70 s**. Begin the biome title at the invisible switch and run its entry/hold/exit alongside the new background's fade-in; enemy entrance waits for both. Pass: frame capture shows outgoing alpha zero before the new sprite becomes visible, no flicker, and no early actor. If abrupt, lengthen each fade by **0.10 s**; if players call it slow after several clears, reduce each by **0.10 s** while preserving the strict order.
- Proposed midpoint rule, requiring approval: for inclusive range `[firstStage, lastStage]`, `length = lastStage - firstStage + 1`; the second visual starts at `firstStage + ceil(length / 2)`. For a 30-stage biome this is its sixteenth stage; for stages 181-200 it is stage 191. For an odd length, the first visual receives the extra stage. A one-stage biome has no midpoint transition. This is a deterministic content rule, not a timing value.
- The midpoint variant changes only background presentation. Keep the same biome identity, encounter pool, progression and name. Use the same biome title treatment without inventing a new region name or awarding anything. Show it only when crossing the midpoint during active play. At reload, directly select the background for the saved destination stage.
- Authoring fallback: if no second background is assigned, retain the first background and omit an empty midpoint ceremony. Missing optional art must not permanently lock the game. Use the biome's existing readable fallback name when a translation is missing; do not fall back to its ID. Proper Thai/English biome names and second backgrounds remain authoring inputs.

### Question result and media boundary

- Add a success/failure sticker as a decorative, absolute-position element beside the result panel. It must not move the panel, equation, keypad or buttons. On narrow screens, tuck it into the panel's upper trailing margin while leaving all text readable. The sticker cannot intercept input. Success uses a check/star gesture; failure uses a neutral cross or thoughtful expression. Keep explicit localized `Correct`, `Incorrect` and `Time expired` text; color and artwork alone do not explain outcomes.
- Starting value: sticker entry **0.18 s** with a small scale settle, no screen shake. Pass: result text is read correctly in at least **8 of 10** new-player observations and neither Thai glyphs nor answer content overlap at the narrowest supported viewport. If distracting, remove the scale motion before changing the information hierarchy.
- **Fixed user requirement:** retain the completed result **1.00 additional second** before battle execution, after the existing result buildup/final row is visible. Apply to correct, incorrect and timeout results, including Reduced Motion. Do not replace all existing durations with a single second. Verification passes only if recorded execution starts no earlier than the old completed-result point plus that second; media dismissal cannot expose an actionable battle frame early.
- The exact requested over-video keypad and absolute playback-control restriction require a media-provider decision. Supported YouTube parameters hide selected controls and keyboard/fullscreen affordances but do not promise absolute restriction. YouTube also prohibits overlays covering its embedded player. See [supported player parameters](https://developers.google.com/youtube/player_parameters) and [required minimum functionality](https://developers.google.com/youtube/terms/required-minimum-functionality). Do not add iframe-cover hacks.
- **Proposed option A:** use an owned/licensed hosted video adapter for production questions, with application-controlled playback and a keypad rendered above the video surface. This supports the intended product interaction, subject to browser verification; it does not promise control over the user's browser itself. Requires media rights, assets and hosting/provider approval before architecture and implementation.
- **Option B:** retain YouTube and dock the keypad in a separate non-overlapping area. Hide only officially supported controls; accept provider limitations. When autoplay requires a gesture, use a clear Play button outside the iframe, then begin the existing validated completion flow. On touch devices, the keypad and visible player must not overlap. This does not meet the exact over-video requirement and needs explicit acceptance of the tradeoff.

### Death hierarchy

- **Normal and mini-boss:** slight downward slide, a single white wash on the actor, then fade to hidden. No tumble/rotation. The outgoing background remains stable until the actor has disappeared. Starting value: **0.70 s** total and a drop of **4% of the actor's displayed height**. Start fading after the white wash becomes readable. Pass: observers distinguish death from a hit in at least **8 of 10** clips; if ambiguous, strengthen the white wash or lower movement before extending the duration.
- **Player and big boss:** slower downward slide, red tint, visibly stronger actor-local shake/flicker, then fade. Keep the camera and background still; this conveys weight without shaking the entire reading surface. Starting value: **1.60 s** total, drop **8% of displayed actor height**, local shake bounded to **1.5% of actor width**, with **two** soft brightness pulses across the motion. Pass: observers identify this as more consequential than normal death in at least **8 of 10** paired clips, while the reset/boss result stays legible. If weak, increase actor-local shake within the readable area; if harsh, remove pulses first. The long class must remain longer than the normal class.
- Proposed classification: Final Boss uses the big-boss treatment and then RunComplete. Confirmation is part of design approval, since the user names Player/Big Boss but not Final Boss explicitly.
- Preserve the existing required Death receipt, reset summary and acknowledgement behavior. The new motion replaces the visual routine, not the reason it must replay. Reward drops remain visual representations of an already accepted grant. If a presentation cue currently depends on the white-flash milestone, preserve its once-per-presentation semantics even when a red death variant has no white flash.

### Small battle SFX library

| Cue | Audible intent | Trigger and priority |
| --- | --- | --- |
| Swing | Short light blade/air motion | At attack motion's release; precedes contact |
| Hit / critical hit | Clean contact; critical adds a short accent | Accepted visible contact, paired with HP/damage feedback |
| Player / enemy click | Soft distinct acknowledgement matching the clicked actor | Only an eligible actor interaction; locked spam is silent |
| Correct | Brief bright resolved phrase | With the accepted success sticker/result |
| Incorrect / timeout | Gentle unresolved phrase; no ridicule | With the explicit failure reason |
| Normal death / major death | Light dissolve versus weightier low decay | Death begins; major death has higher mix priority |
| Battle / biome title | Airy shimmer with clear arrival | Title entrance; subordinate to required death/reset information |

Scope the library to these battle cues and existing keypad/commit acknowledgement. No dynamic music system or haptics are proposed. Retain the user's sound/mute preference; playback failure cannot affect combat. Asset selection is outstanding. Provide at least **three authored variants** for frequently repeated swing/hit/click cues per the skill's repetition guidance; this is an authoring target, to be verified in listening tests. Starting pitch variation: **±3%** for swing/hit only; result and title phrases keep their authored pitch. Starting click retrigger interval: **0.10 s**. Listen through **20 consecutive attempts**: pass if result/death cues remain identifiable, no clipping occurs, and click spam cannot bury an outcome. If fatiguing, improve source variation and reduce click gain before increasing processing.

No added gameplay hitstop, input buffer or coyote time: this is a committed turn-based question loop. Recovery means returning to an eligible stable state, not opening a cancel window or altering the answer deadline.

### Reduced Motion

Keep informational holds and all outcome rules. Remove square travel, actor shake, death flicker, sticker bounce and title vertical movement. Use static composition plus opacity. Starting value: **0.20 s** per transition fade, normal death **0.50 s**, major death **1.00 s** with stable white/red tint. Pass: testers can still identify title, defeat class and failure reason, and major death remains longer; if unreadable, extend static hold rather than restore motion. The no-scaling background rule applies in every mode.

## 5. Five-component evaluation

| Component | Intended improvement | Guardrail / failure signal |
| --- | --- | --- |
| Clarity | Shared Battle/biome title language, display names, explicit result reasons, death before environmental change | Failure: observers cannot tell which encounter died or why the scene changed. Fix sequence/labels before stronger effects. |
| Motivation | Midpoint scenery communicates progress; boss weight marks meaningful milestones | No new currency, multiplier or reward is attached to the ceremony. |
| Response | Loading overlaps title; one readiness gate; keypad remains reachable; spam cannot skip or prequeue actions | Failure: controls look ready while blocked, or become actionable too early. Fix eligibility and media layout before timings. |
| Satisfaction | Sticker plus cue, swing plus hit, differentiated death, elegant region arrival | Effects must reflect the accepted result and never imply a second reward. |
| Fit | Layered actor scene, fine white ornaments and restrained sound support the user's elegant 2.5D direction | References to Persona, Genshin Impact and RPG Maker are user-supplied mood directions, not copied assets or source-backed timings. |

Tuning priority: Response, then Clarity, then Satisfaction, Fit and Motivation. Longer animations cannot repair missing input eligibility, an unreadable result, or incorrect recovery.

## 6. Edge cases and playtest acceptance

These are scripts for a future browser/Unity playtest, not executed results. Starting human-readability targets use the GDD's 8-of-10 guidance; adjust only after recording observations. Mechanical invariants require every tested case to pass.

| Test | Script | Pass/fail and response to failure |
| --- | --- | --- |
| New player | Enter, complete a correct and incorrect answer, cross a biome; repeat in Thai and English | At least 8/10 observations explain the result and destination correctly. If failed, simplify labels and staging before adding time. |
| Stress | Repeated click/tap, hold activation keys, open-menu shortcuts throughout title, result, death, fade and entrance | No skip, next-attempt prequeue, duplicate sound storm or unlocked frame. Any occurrence fails and requires gate/order repair. |
| Skill | Compare correct-fast, correct-slow, incorrect and timeout using existing rules | Same scores, damage, cooldown and deadlines with effects enabled or reduced. Any difference fails; presentation must not own those rules. |
| Abuse / refresh | Refresh before/after result save, during death, each background fade, title, enemy entrance and reset summary | Same saved attempt, selected encounter, HP and stage; no extra grant. Required Player Death and reset summary still replay. Any lost required receipt or duplicated outcome fails. |
| Background | Capture every frame at biome and midpoint crossings, including boundary stages 15/16, 30/31 and 190/191 for current ranges | Outgoing reaches zero before switch; no scaling or moving background; one incoming enemy after transition. Any flicker/order violation fails. |
| Low frame rate / hidden tab | Throttle frames, hide/resume tab, resize/orient during each state | Deterministic final visuals and no locked abandoned overlay or extended authoritative deadline. Restore from current accepted state; do not replay grants. |
| Mobile media | Actual supported iOS/Android browsers, portrait/landscape, autoplay blocked, repeat video completion callbacks | Keypad stays usable; no overlapping YouTube player if that option is chosen; one answer window. Verify the selected provider's real constraints before claiming completion. |
| Readability / motion | Watch normal, mini-boss, big-boss, player and final-boss deaths with sound/motion on and off | Major class is longer; cause and final result stay clear; Reduced Motion has no shake/flicker. Simplify effects first if failed. |
| Missing content | Remove optional SFX, second biome sprite or translation; simulate delayed session load | No permanent input lock, no incorrect biome ID title, no fake successful load. Report missing content and retain deterministic fallback. |

## 7. GDD alignment and approval record

The current `GDD_PowerMathProject.md` motivates this work:

- `@tag:combat-attempt`: "The question is locked to that attempt; refreshing cannot reroll it." The proposal preserves commitment and player-damage-before-counterattack order.
- `@tag:stage-progression`: "Transition timing is presentation tuning, not authoritative gameplay state." The request replaces its older crossfade/slow-shift visual direction with strict opacity-only switching and adds a proposed midpoint variant; neither grants progress.
- `@tag:feedback`: "Significant actions require at least visual and audio feedback." Each significant new treatment pairs visible feedback with a scoped battle cue.
- `@tag:player-experience`: "protect input response and outcome clarity before increasing spectacle or reward size." The gate, result legibility and media choice take priority over animation.
- `@tag:server-authority` and `@tag:guardrails`: retain the accepted attempt/result and once-only settlement. This document does not label the present client implementation server authoritative.
- `@tag:playtest`: repeat input, refresh, boundary-stage and Reduced Motion checks are included above.

Human design checkpoint approved on 2026-09-09: the user accepted the title/entrance timings, deterministic midpoint rule, death hierarchy including Final Boss, and implementation of the proposed design. For media, the user selected option B: retain YouTube and keep the keypad beside or below the iframe on narrow screens. The implementation therefore uses only supported YouTube player parameters and does not overlay the iframe.

Approval basis: `2_System_Files/Workflows/implement-feature.md` explicitly states, "Design/game-feel approval is required before architecture work." `2_System_Files/Handoff_Contracts/README.md` assigns design approval to humans. That checkpoint is recorded above. The remaining checkpoint is a human game-feel/browser playtest after implementation; publishing and live-service configuration remain separately gated.

Validation completed for this artifact: requested prompt/skill read; targeted GDD sections and runtime paths inspected; state entry/exit/interrupt/recovery, five-component evaluation and playtest criteria documented. Runtime implementation and its automated checks are recorded in `public-webgl-combat-polish-audit.md`.
