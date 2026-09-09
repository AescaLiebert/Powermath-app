---
slug: public-webgl-combat-polish
status: ready
owner: Codex
human_checkpoint: required
---

# Public WebGL combat polish verification

## Automated checks

Run the browser contracts:

`node --test Tools/Tests/youtube-question.test.cjs Tools/Tests/web-cache-reload.test.cjs`

Run these Unity EditMode fixtures from Test Runner:

- `PowerMath.Gameplay.Combat.Unity.Tests.CombatLobbyViewTests`
- `PowerMath.Gameplay.Combat.Unity.Tests.ActorPresentationControllerTests`
- `PowerMath.Tests.EditMode.FirestoreAcademicSaveGuardTests`
- existing lifecycle/version/migration fixtures listed in `player-lifecycle-live-service-test-plan.md`

The focused checks cover stages 15/16 midpoint selection, deferred destination binding, recovery skip, actor gate behavior, death hierarchy, stale/future/malformed save rejection, hidden YouTube controls, stale iframe callbacks, and bounded cache reload.

## Unity playtest

| Scenario | Expected result |
| --- | --- |
| Fresh session | Squares, title, player, and enemy enter together; all UI remains locked until both title and actor loading complete. |
| Pending receipt refresh | Ordinary Battle entrance is skipped; accepted required presentation replays once and unlocks only after its acknowledgement saves. |
| Correct/incorrect/timeout | A distinct sticker and cue appear; the completed result remains for one extra second; scoring and answer deadline are unchanged. |
| Spam during locked states | Lobby, navigation, modal dismiss, keypad, and actor click juice cannot prequeue or skip actions. |
| Stages 15 to 16 | Enemy death completes, background fades fully out, `_2` art switches while invisible, fades fully in, then one enemy enters. No scale or flicker. |
| Stages 30 to 31 | Same ordering across biome boundary; title shows destination display title and white ornament style. |
| Normal/mini death | Slight drop, white flash, no rotation, 0.70 s class. |
| Player/big/final death | Longer red drop with local shake/pulses, no camera/background shake, 1.60 s class. |
| Reduced Motion | Opacity-only title/background behavior; no square travel or death shake/flicker; major death remains longer. |
| Missing SFX clip | Procedural cue plays or combat continues silently; no gameplay failure. |

## Deployed browser checkpoint

Test actual supported desktop, iOS, and Android browsers in portrait and landscape. Confirm the YouTube iframe never overlaps the keypad, no standard player controls or pointer interaction are available, resize/orientation does not strand the iframe, completion fires once, and a replaced/cancelled video cannot complete the current question. Record whether delayed autoplay is blocked; if it is, add and verify an application-owned Play Video action outside the iframe.

Refresh during committed question, answer window, saved result, each death, both halves of background fade, enemy entrance, and terminal reset. Each case must restore the accepted attempt/stage/HP/receipt without duplicate rewards or rerolling content.

Do not publish until the focused Unity fixtures pass and the checked-in public manifest advertises the same client/schema policy as the built player.
