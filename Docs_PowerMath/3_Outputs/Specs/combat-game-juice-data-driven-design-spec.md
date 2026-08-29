---
slug: combat-game-juice-data-driven
status: approved
source: manual
gdd_tags:
  - core-loop
  - combat-attempt
  - stage-progression
  - run-reset
  - server-authority
  - feedback
  - player-experience
  - guardrails
  - playtest
owner: game-design-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Design Spec: Data-Driven Combat Presentation and Game Juice

## 1. Player Goal and Experience

The player should experience one readable chain of cause and effect:

```text
Solve the math challenge
  -> the player character acts first
    -> the enemy visibly receives the result
      -> the enemy consumes its next planned action
        -> both characters settle
          -> the next choice becomes available
```

Correct answers should feel like the player character caused the damage, rather than like an HP number changed in a menu. Enemy cooldown boxes should behave like a visible plan advancing toward an attack, rather than a static copy of a number. Death, Rebirth, Rank movement, potion use, critical hits, and encounter changes should each have a distinct presentation while remaining consumers of authoritative gameplay data.

The central rule is: **gameplay state decides what happened; presentation data decides how that result is shown; presentation never decides damage, Rank, rewards, cooldowns, or progression.**

This supports the GDD requirements that player damage resolves first, enemy retaliation is predictable, significant actions have visual and audio feedback, refresh cannot reroll outcomes, and the next decision is not presented until the previous result is understandable (`@tag:combat-attempt`, `@tag:server-authority`, `@tag:feedback`, `@tag:player-experience`).

## 2. Scope and Assumptions Requiring Approval

### In scope

- Player presentation states: Idle, Attack, Take Damage, Die, and Rebirth, plus reactions for potion use and Rank movement.
- Enemy presentation states: Appear, Idle, Walk, Attack, Take Damage, and Die.
- A data-driven action-sequence plan that can later express follow-up attacks and counterattacks.
- A combat interaction gate that waits for authoritative resolution, the action queue, actor states, and blocking UI transitions.
- Target-anchored Floating Combat Text (FCT), critical-hit impact impulse, pooling, and authorable style/timing.
- Animated enemy action boxes with initiate, armed, consume, exit, and auto-layout/reflow behavior.
- Visually distinct Death and Rebirth sequences using one shared settlement-result payload.
- A reusable Enter -> Idle -> Exit lifecycle for persistent, modal, and transient combat UI.
- Refresh/reconnect behavior for unresolved presentation, with stricter replay for Death.

### Out of scope for this design checkpoint

- Damage, critical, cooldown, Rank, settlement, or reward-rule changes.
- Final animation clips, final VFX, final audio assets, or final camera tuning.
- Architecture, code, scene edits, build settings, dependencies, publishing, or deployment.
- Skipping, fast-forwarding, or cancelling committed combat presentation.

> [!NOTE]
> **ASSUMPTION:** The existing static uGUI character images may use data-driven tween poses as a first visual implementation before final sprite animation clips exist.
> **IMPACT:** The state and event infrastructure can be validated without waiting for final art.
> **IF WRONG:** Implementation must wait for complete player/enemy animation assets and animator-controller rules.
> **VALIDATE:** At the design checkpoint, approve tween placeholders or supply the intended animation assets.

> [!WARNING]
> **ASSUMPTION:** A potion-use gameplay action is not defined in the current GDD. This spec defines only its presentation hook, not potion inventory, healing, cost, or eligibility rules.
> **IMPACT:** A future authoritative `PotionUsed` result can trigger feedback without redesigning the presentation system.
> **IF WRONG:** Treat the hook as reserved and do not expose potion UI.
> **VALIDATE:** Add potion rules to the GDD before implementing any potion gameplay behavior.

> [!NOTE]
> **ASSUMPTION:** “Block any interaction” means all in-game Lobby, navigation, panel, Attack, and answer controls, except the one control explicitly owned by the active step. Browser controls cannot be blocked.
> **IMPACT:** During a question, only question controls are available; during result presentation, no ordinary game control is available; a required Death result exposes only Retry/Restart.
> **IF WRONG:** Define which non-combat panels may remain usable during combat resolution before architecture begins.
> **VALIDATE:** Test attempts to open Map, Player Hub, Rebirth, Gacha, Leaderboard, and Profile during each lock state.

## 3. Design Principles

1. **One result, one presentation plan.** Each accepted attempt/result has a stable presentation ID derived from its attempt or settlement transaction.
2. **Semantic events, not animation commands.** Gameplay emits meanings such as Player Primary Attack, Enemy Took Damage, Enemy Advance, Enemy Attack, Player Defeated, or Rank Changed. Local presentation data maps those meanings to animation, FCT, audio, VFX, and UI profiles.
3. **Actor-first readability.** A number never appears without a visible source and target. Player attack reaches its impact marker before enemy HP/FCT response; enemy attack reaches its impact marker before heart loss/player reaction.
4. **Player action first.** After the math result is accepted, the player primary action always presents before any surviving enemy Walk/Attack action. A failed answer uses a readable no-damage attempt/miss action rather than silently jumping to the enemy.
5. **Barriers, not guessed delays.** Sequence progress waits for named completion signals from actors/UI. Authored durations are presentation profiles and fail-safe bounds, not gameplay authority.
6. **No early unlock.** Non-terminal combat unlocks only when the plan is empty, the player and enemy are Idle, blocking UI is Idle/Hidden as appropriate, and the authoritative phase is ready.
7. **Terminal states are explicit exceptions.** A dead actor never has to return to Idle. Death proceeds to the mandatory Death result; enemy defeat proceeds through the next encounter's Appear -> Idle before combat unlocks.
8. **Accessible semantics survive reduced motion.** Reduced Motion replaces translation, scale punches, and impact impulse with short fades, color/outline changes, readable labels, and audio where enabled.

## 4. Design-Facing Data Model

This section describes the information the later architecture must support; it does not prescribe C# types.

### 4.1 Combat Presentation Plan

Every resolved attempt produces or reconstructs a plan containing:

| Field | Design purpose |
|---|---|
| `presentationId` | Stable attempt/settlement-linked ID used for replay and completion acknowledgement. |
| `authoritativePhase` | Prevents presentation from reopening interaction against unsafe saved state. |
| `steps[]` | Ordered semantic actions and reactions. |
| `requiredPresentation` | Marks flows that must replay until acknowledged; always true for Death. |
| `resultPayload` | Immutable before/after values for HP, hearts, Stage, Rank, and settlement summary. |
| `accessibilityMode` | Selects normal or reduced-motion presentation profile without changing the result. |

Each step contains a stable action ID, actor, target, semantic action type, outcome payload, local presentation-profile key, blocking policy, and completion barrier. A step may start parallel feedback at an impact marker, but its ordering against other gameplay actions remains deterministic.

### 4.2 Semantic Action Types

| Actor | Current semantic actions | Reserved extension points |
|---|---|---|
| Player | PrimaryAttack, FailedAttack, TakeDamage, Die, Rebirth, PotionUsed, RankUp, RankDown | FollowUpAttack, CounterAttack, BuffReaction |
| Enemy | Appear, Walk, Attack, TakeDamage, Die | FollowUpAttack, CounterAttack, Stagger, Enrage |
| UI/FX | ShowFct, UpdateHp, ConsumeActionToken, InitiateActionQueue, ShowRankResult, ShowRunResult | ComboCounter, StatusFct, BossPhaseBanner |

Future follow-up/counter mechanics append semantic steps to the authoritative result. They must not bypass the same ordering, actor-state, target, and interaction-gate rules.

### 4.3 Authorable Presentation Profiles

The later architecture should expose local data assets for:

- `ActorPresentationProfile`: animation/tween reference, impact marker, recovery marker, interrupt policy, audio cue, VFX cue, and reduced-motion variant per semantic action.
- `FloatingCombatTextStyle`: text component/prefab, font, material, size, outline, colors, prefixes, spawn offset, spread, pop/hold/exit curves, sorting, pool capacity, and reduced-motion variant.
- `ActionQueuePresentationProfile`: Walk/Attack visuals, initiate/consume/reflow curves, danger treatment, spacing, and audio cues.
- `ImpactImpulseProfile`: strength, duration, direction/noise curve, affected visual roots, critical-only rule, and reduced-motion behavior.
- `UiTransitionProfile`: Enter/Exit duration and curve per UI category; Idle owns visibility and interaction semantics.

Gameplay definitions select semantic/profile keys; they do not store animation frames or transient UI state in authoritative combat data.

## 5. Interaction and Locking Model

### 5.1 Interaction scopes

| Scope | When it is enabled |
|---|---|
| Lobby/navigation controls | Only in EnemyReady/EventReady or allowed RunComplete state, with no action plan, actor transition, blocking modal transition, or required presentation. |
| Question controls | Exclusively during Preparation/Answering according to existing answer rules. Lobby/navigation remains locked. |
| Combat presentation | No ordinary player input. It advances from completion events. |
| Rank modal | Continue becomes enabled only after modal Enter completes and the Rank reaction reaches its stable pose/Idle. |
| Rebirth preview | Confirm/Cancel are available only while the preview is Idle and the run remains safe. |
| Death result | Only Retry after settlement failure or Restart after accepted settlement; backdrop, Escape, navigation, and ordinary Close do nothing. |

### 5.2 Unlock predicate

For ordinary combat, interaction may reopen only when all conditions are true:

```text
Authoritative state is EnemyReady or EventReady
AND presentation plan has no unconsumed step
AND Player presentation state is Idle
AND Enemy presentation state is Idle
AND enemy action queue has no Armed/Consuming/Reflowing box
AND every blocking UI is either Idle in its intended visible state or fully Hidden
AND no required presentation receipt is awaiting acknowledgement
```

The Attack button acknowledges immediately on press and closes Lobby interaction before the commit request can be repeated. If authority rejects or voids the attempt, a neutral recovery presentation returns both actors/UI to Idle before reopening interaction.

### 5.3 Fail-safe rule

Missing animation markers or interrupted coroutines must not deadlock the game. A presentation profile has a completion bound. If it is exceeded, the affected visual snaps to its semantic end state, logs the presentation ID/action ID, and continues only if authoritative state says that continuation is safe. It never grants, repeats, or changes gameplay data.

## 6. Actor State Machines

### 6.1 Player presentation state

| State | Entry | Visible behavior | Exit / chained state | Interruptibility |
|---|---|---|---|---|
| Hidden | Scene/encounter unavailable | Not presented | Appear or Idle during scene bootstrap | System recovery only |
| Idle | Actor ready and no exclusive action | Breathing/stance loop or static idle pose | Attack, FailedAttack, TakeDamage, Rebirth, queued reaction | May yield to authoritative action |
| Attack | Player PrimaryAttack step | Wind-up -> impact marker -> recovery | Idle, or next Player FollowUpAttack | Cannot be cancelled by UI/input |
| FailedAttack | Incorrect/timeout accepted | Readable fizzle/miss/no-damage pose | Idle, then surviving enemy action | Cannot be cancelled by UI/input |
| TakeDamage | Enemy impact applies heart loss | Hit flash/recoil plus heart loss | Idle if alive; Die if defeated | Die has priority after lethal result |
| Die | Lethal TakeDamage completed/marker reached | Collapse/fade that is visually distinct from Rebirth | Death result flow; never Idle in the defeated run | Terminal; scene disable only |
| Rebirth | Safe Rebirth confirmed | Voluntary empowered dissolve/ascend, not collapse | Fresh-run appear/Idle after accepted settlement | Cannot be cancelled after authority accepts |

PotionUsed, RankUp, and RankDown are queued **reaction cues**, not new combat authority states. They may use an additive overlay or briefly own the player's exclusive presentation channel according to the profile. They never interrupt Attack/TakeDamage/Die. Rank reactions occur after battle resolution and before the Rank modal becomes actionable.

### 6.2 Enemy presentation state

| State | Entry | Visible behavior | Exit / chained state | Interruptibility |
|---|---|---|---|---|
| Hidden | No active encounter or previous enemy exited | Not presented | Appear | Encounter replacement only |
| Appear | New saved encounter becomes active | Spawn/reveal, settle into stance | Idle | Cannot attack or be interacted with |
| Idle | Enemy is ready between steps | Idle loop/static pose | Walk, Attack, TakeDamage | Yields to scheduled authoritative step |
| Walk | Consumed Walk action token after player action | Pseudo-step/advance with weight | Idle | Cancelled if player attack defeated enemy |
| Attack | Consumed Attack action token and enemy survived | Wind-up -> impact marker -> recovery | Idle after player reaction, or terminal Death flow | Cannot be cancelled after authoritative attack result |
| TakeDamage | Player impact applies damage | Flash/recoil/stagger | Idle if alive; Die if defeated | Die has priority after lethal result |
| Die | Enemy HP reaches zero | Defeat animation and visual exit | Hidden, then next encounter Appear; or RunComplete | Terminal for that encounter |

The actor state is presentation-only. HP zero, hearts zero, cooldown state, and encounter identity remain authoritative.

## 7. Action Sequence Rules

### 7.1 Correct, enemy survives, Walk token

```text
Answer result accepted
-> Result UI exits
-> Player Attack begins
-> Impact marker: Enemy TakeDamage + HP interpolation + anchored FCT + hit audio
-> Player Attack and Enemy TakeDamage both settle to Idle
-> Leftmost Armed Walk box Consumes/Exits
-> Remaining boxes reflow left
-> Enemy Walk pseudo-action
-> Enemy returns Idle
-> presentation completion saved
-> interaction gate opens
```

### 7.2 Correct, enemy survives, Attack token

```text
Player Attack -> enemy damage response -> both settle
-> Attack box consumes/exits -> queue reaches empty
-> Enemy Attack -> impact marker: player heart loss + TakeDamage
-> enemy and living player return Idle
-> a fresh cooldown queue initiates and settles
-> presentation completion saved -> interaction opens
```

### 7.3 Incorrect or timeout

The player presents FailedAttack first with no damage/FCT. The Armed enemy token then resolves as Walk or Attack. Failure audio remains non-punitive per the GDD. A confirmed system/content failure instead voids the attempt, restores the cooldown, disarms the token, and plays no enemy action.

### 7.4 Enemy defeated

At player impact, enemy TakeDamage chains to Die. Any Armed enemy token exits with a cancelled/dissolve treatment and does not perform Walk or Attack. If the run continues, Stage/encounter data is already authoritative; the old enemy exits, the queue initiates for the new enemy, the new enemy plays Appear -> Idle, and only then may interaction reopen.

### 7.5 Future follow-up and counterattacks

- Player PrimaryAttack remains first.
- Player FollowUpAttack steps execute in their authoritative order before the enemy's scheduled cooldown action unless a future rule explicitly emits a different order.
- A CounterAttack is a distinct semantic step with a trigger/source action ID; it is never inferred from animation state.
- Each impact owns its own damage payload and FCT request. Presentation cannot combine damage values unless the result explicitly identifies an aggregate hit.
- The interaction gate waits for the expanded plan and all involved actor barriers, so future actions do not need new ad-hoc button locks.

## 8. Enemy Action Queue as Pseudo-Animation

### 8.1 Meaning

For a standard enemy with maximum cooldown `N`, the initial visible queue is:

```text
[Walk] x (N - 1) -> [Attack]
```

The leftmost box is always the next enemy action. The list is predictive, not decorative: each committed valid attempt arms exactly one box. The authoritative cooldown is still consumed at Attack commit as required by the GDD, but the box remains visible in an **Armed** state during the question so the player can see what was committed. It visually consumes only after the player action presents.

### 8.2 Box lifecycle

```text
Entering -> Idle -> Armed -> Consuming -> Exiting -> removed
                                  |
                                  -> CancelledExit if enemy dies/attempt is voided
```

- `Entering`: new queue boxes appear with a short stagger.
- `Idle`: stable and readable; the first box has a “next” emphasis that does not depend on color.
- `Armed`: commit acknowledgement; input is already locked and the box cannot be armed twice.
- `Consuming`: squash/dissolve/slide treatment synchronized to the represented enemy action.
- `Exiting`: removal completes before layout is declared stable.
- `CancelledExit`: distinct neutral removal for enemy defeat or void recovery; it must not resemble an executed Attack.

### 8.3 Auto-layout and sorting

- Spent boxes are removed; no faded placeholder remains.
- After the first box exits, every surviving box animates from its previous screen position to the auto-layout result, preserving order.
- Reflow must finish before the queue reports Idle.
- If the consumed box was Attack, the enemy attacks and then the full reset queue plays Initiate.
- A new enemy creates a new queue only after the enemy begins Appear; both queue Initiate and enemy Appear must finish before combat unlocks.
- Events use an event-specific action/risk token rather than pretending to have a standard Walk/Attack queue.
- If loaded authoritative cooldown data and visual boxes disagree, rebuild from authority, show a short synchronization transition, and remain locked until stable.

## 9. Floating Combat Text and Critical Impact

### 9.1 Targeting rule and current bug

FCT spawns from a **target anchor supplied by the affected actor**, never from the player stats/menu, the combat root's default layout position, or a hard-coded screen coordinate. Enemy damage uses the visible enemy sprite bounds plus an authorable offset; player damage/healing uses the player sprite bounds.

The current damage label is a root-level UI Toolkit label in `CombatSurface.uxml`; `.combat-damage-label` supplies a fixed `top` but no enemy-relative anchor, while the actual enemy is a separate uGUI Image (`monsterPrefab`). That ownership split explains why the label cannot reliably follow the enemy.

### 9.2 FCT lifecycle

```text
Acquire pooled view at target anchor
-> Pop in (scale/opacity overshoot)
-> Hold for readability
-> Slide upward while fading
-> Return to pool
```

- Normal damage displays the exact accepted damage value.
- Critical damage uses a distinct style and `CRITICAL` semantic label, not color alone.
- Zero damage does not create enemy FCT; Incorrect/Timeout communicates through result UI and FailedAttack.
- Multiple simultaneous hits receive deterministic horizontal/spawn-order offsets so values remain readable.
- FCT follows the spawn anchor only for the initial placement; it does not chase a recoiling target during its slide.
- FCT is transient and never blocks action-sequence completion or interaction by itself.

### 9.3 Critical impact impulse

At a critical impact marker, pulse only the combat-world presentation layer, leaving HUD text and answer/result UI readable. The effect is a short directional impulse toward/away from the target plus critical audio and stronger target reaction. Because the current characters live on a Screen Space Overlay Canvas, architecture must implement this as a combat-presentation-root impulse or deliberately migrate the affected visuals; moving the Unity camera alone would not move the current overlay characters.

Reduced Motion disables translation/noise impulse and uses a brief contrast/outline pulse plus the critical label/audio.

### 9.4 Where FCT is changed today and after the planned refactor

Current touchpoints:

- `Assets/Project/UI/MainMenu/CombatSurface.uxml`: `combat-damage-label` and `combat-critical-label` elements.
- `Assets/Project/UI/CombatLobbyUI.uss`: `.combat-damage-label`, `.combat-damage--animate`, and `.combat-critical-label` font/color/motion styling.
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatLobbyView.cs`: `ShowDamage`/`HideDamage` sets text and classes.
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatFeedbackPlayer.cs`: decides when the text appears and disappears.

Planned authoring boundary:

- Edit font, font material, outline, alignment, and base hierarchy on a reusable component/prefab under a dedicated combat-FX overlay.
- Edit normal/critical/heal style, spawn offset, spread, curves, timing, pool size, and impact-impulse linkage in a `FloatingCombatTextStyle` presentation asset.
- Runtime code supplies only semantic type, accepted value, target anchor, and presentation ID.

FCT therefore does not need to remain a UI Toolkit element. The architect may choose uGUI/TMP or another component-driven view already supported by the project, without changing this design contract or adding an unapproved dependency.

## 10. Death and Rebirth: Shared Data, Different Presentation

### 10.1 Shared settlement result payload

Death and Rebirth use the same result structure for Stage reached, Power Coins before/after/gain, Legacy ATK before/after/gain, Effective ATK before/after, Prestige before/after, and Keep/Reset lists. `cause = Death | Rebirth` selects copy, art, animation, audio, confirmation, and interaction rules. Death does not add Prestige; Rebirth does, according to the existing GDD.

### 10.2 Death sequence

```text
Enemy impact accepted -> heart reaches zero / RunDefeat saved
-> hard interaction lock; all reset/result panels remain hidden
-> Player TakeDamage -> Player Die
-> settlement authority resolves or returns Retry state
-> Death Result panel Enter -> Idle
-> accepted result exposes only Restart / Begin Again
-> acknowledgement saved -> fresh Stage 1 presentation -> unlock
```

Rules:

- The Die animation completes before the Death Result panel begins Enter.
- Death is a collapse/defeat treatment; it must never reuse the voluntary Rebirth animation.
- The result panel cannot be dismissed by Close, backdrop, Escape, navigation, or another Main Menu panel.
- If settlement fails, the player remains locked in the terminal run and the panel exposes Retry. Restart cannot begin and rewards cannot duplicate.
- A successful settlement may prepare authoritative Stage 1 data, but Stage 1 combat remains presentation-locked until the required Death result is acknowledged.
- The final defeated-run presentation snapshot or equivalent receipt must remain available until acknowledgement so refresh can reconstruct the correct Stage/result context.

### 10.3 Mandatory refresh/reconnect replay

Death owns a persisted **required presentation obligation**, keyed by the defeated run/settlement ID. If the browser closes or refreshes before acknowledgement:

1. Bootstrap detects the unacknowledged Death presentation before enabling any game UI.
2. The Lobby loads in a hard-locked recovery presentation.
3. The player TakeDamage/Die sequence replays from its defined beginning; saved animation frames are not resumed.
4. The Death Result panel appears only after Die completes and authoritative settlement state is known.
5. Only a successful acknowledgement followed by Restart clears the obligation and exposes Stage 1 interaction.

Replaying presentation cannot repeat heart loss, settlement, analytics, rewards, or Stage reset. Those remain idempotent authoritative results.

### 10.4 Rebirth sequence

```text
Safe Lobby -> Rebirth Preview panel Enter/Idle
-> Cancel returns panel Exit -> Lobby Idle
OR Confirm -> interaction hard lock -> preview Exit
-> authority accepts settlement -> Player Rebirth animation
-> Rebirth Result panel Enter/Idle using shared payload
-> Continue acknowledges -> fresh Stage 1 Appear/Idle -> unlock
```

- Rebirth is voluntary, bright, upward/renewal-focused, and never uses Die.
- Cancel is available only before the Rebirth request is accepted.
- After acceptance, refresh uses the same required-presentation principle so the player cannot skip the result or access an incompletely presented fresh run.
- The Rebirth result may use the same content layout as Death, but its title, color, animation, audio, explanatory copy, and Prestige row are cause-specific.

## 11. UI Lifecycle Contract

Every combat UI unit, including persistent HUD groups, dynamic action boxes, transient FCT, banners, and modals, must have an explicit lifecycle. Directly toggling visible/hidden without a lifecycle is not a completed implementation.

### 11.1 Base lifecycle

```text
Hidden -> Entering -> Idle -> Exiting -> Hidden
```

- **Entering:** visible, non-interactive unless the component explicitly owns a permitted control after Enter completes.
- **Idle:** stable visual state; only this state may expose its intended interaction.
- **Exiting:** non-interactive; completion is part of any blocking sequence barrier.
- **Hidden:** removed from picking/focus and reset for deterministic reuse.
- A re-show request during Exiting either completes Exit then Enters or uses an explicitly authored reversible transition; it never leaves mixed classes/picking state.
- Scene disable cancels transitions and resets views to a deterministic semantic end state.

### 11.2 Required UI states

| UI unit | Enter | Idle | Exit | Blocking behavior |
|---|---|---|---|---|
| Combat HUD / encounter header | Scene/encounter reveal | Stable HP, Stage, queue | Scene/encounter replacement | Blocks Attack until new encounter HUD is Idle |
| Question panel | Committed question opens | Preparation/Answering owns question controls | Submission/timeout accepted | Lobby remains locked throughout |
| Answer result card | Correct/Incorrect/Timeout reveal | Readable result/build-up | Before actor action begins | Blocks sequence until Exit completes |
| Enemy action queue | New encounter/reset initiation | Predictive list | Encounter death/replacement | Blocks unlock during initiate/reflow/exit |
| Individual action box | Entering | Idle/Armed | Consuming/Cancelled exit | First box and reflow participate in barriers |
| Battle/Stage banner | Outcome reveal | Short readable hold | Before next encounter becomes actionable | Blocks progression presentation only |
| Rank modal | Rank result reveal | Continue available | Continue acknowledged | Blocks Lobby and actor plan |
| Death result | After Die and settlement | Retry or Restart only | Accepted acknowledgement | Hard terminal block |
| Rebirth preview/result | Request/result reveal | Context-specific controls | Cancel/confirm/continue | Modal block; hard block after acceptance |
| FCT | Pop | Hold | Slide/fade | Non-blocking |
| Global interaction shield | Immediate activation | Captures picking/focus | After unlock predicate | Must prevent pointer and keyboard leakage |

## 12. Juice and Starting Tuning Values

All values below are **starting values**, not standards or final balance. They require the listed micro-tests.

| Element | Starting value | Micro-test / pass condition | Adjustment if it fails |
|---|---:|---|---|
| Standard player Attack, start to settled Idle | 0.55 s; impact at 0.22 s | Observer identifies attacker and impact order in 9/10 mixed clips without the sequence feeling delayed | Move impact earlier in 0.03 s steps if laggy; lengthen recovery only if weight is unclear |
| Enemy Walk pseudo-action | 0.32 s | Player notices one plan step advanced in 9/10 turns | Increase displacement/contrast before increasing duration |
| Enemy Attack, start to settled Idle | 0.60 s; impact at 0.28 s | Player predicts heart loss before impact in 8/10 first encounters | Lengthen wind-up in 0.05 s steps; do not delay heart/FCT after impact |
| Take Damage reaction | 0.30 s | Normal hit reads without obscuring the next action | Increase visual contrast first; lengthen by 0.05 s only if missed |
| Enemy Appear | 0.45 s | New encounter identity is recognized before Attack enables in 9/10 transitions | Increase hold/name clarity rather than spectacle if identity is missed |
| Player Die before panel Enter | 0.90 s | Death is recognized before summary in 10/10 lethal sequences without feeling stalled | Adjust impact/pose hold in 0.10 s steps |
| Player Rebirth | 0.85 s | Observers distinguish Rebirth from Death in 10/10 unlabeled clips | Change direction/color/silhouette before changing duration |
| FCT pop / hold / exit | 0.12 / 0.24 / 0.34 s | Exact value and critical status are read in 9/10 mixed hits | Increase hold by 0.05 s; reduce travel speed if digits blur |
| Critical combat-root impulse | 14 reference pixels over 0.18 s, 2 directional oscillations | Critical is distinguished from normal in 9/10 hits and causes no discomfort report | Reduce amplitude first; then duration; disable fully in Reduced Motion |
| Action box consume / reflow | 0.16 / 0.20 s | Observer can point to the consumed first box and new first box in 9/10 turns | Increase consume contrast; then add 0.04 s to reflow |
| Queue initiate stagger | 0.05 s per box, capped by profile | Full order reads without making long cooldowns slow | Reduce stagger as queue length grows |
| Standard UI Enter / Exit | 0.18 / 0.14 s | UI feels responsive and no control is clickable while visually absent | Shorten by 0.03 s if input acknowledgement feels delayed |

Audio feedback must accompany significant actions: player Attack impact, enemy Attack impact, normal/critical hit distinction, enemy defeat, Rank movement, Death, Rebirth, and action-queue Attack warning/consume. Frequently repeated cues should have variation before final content approval.

## 13. Five-Component Evaluation

| Component | Design response | Acceptance signal |
|---|---|---|
| Clarity | Actor-first sequencing, anchored FCT, predictive action queue, Armed commit state, distinct Death/Rebirth, explicit UI lifecycles | New observer explains source, target, result, and next enemy action in at least 8/10 resolutions |
| Motivation | Correct math visibly powers the character; Rank and settlement changes receive distinct character/UI reactions | Players notice progression events without reading raw saved data |
| Response | Immediate Attack acknowledgement, exclusive question controls, deterministic barriers, no accidental unlock gap | Spam/double input causes one commit and one presentation; valid numpad input still acknowledges immediately |
| Satisfaction | Attack/reaction pairing, HP/FCT/audio, enemy defeat, critical impulse, queue motion | Normal and critical outcomes are distinguishable without reading damage values |
| Fit | Mathematics becomes character power; enemy plan creates a readable turn rhythm; voluntary Rebirth feels empowering while Death feels terminal | Players correctly label Death vs Rebirth and understand why the enemy acted |

Conflict resolution follows the project/game-design priority: Response -> Clarity -> Satisfaction -> Fit -> Motivation. Shorten or simplify spectacle before accepting sluggish input or ambiguous outcomes.

## 14. Edge Cases, Risks, and Abuse Cases

| Case | Required behavior |
|---|---|
| Attack double-click / keyboard plus pointer | One commit, one Armed box, one presentation ID. |
| Content/system failure after commit | Void authority restores cooldown; Armed box uses CancelledExit; no enemy action or damage feedback. |
| Incorrect/timeout | FailedAttack first, then surviving enemy token; zero-damage FCT does not spawn. |
| Critical lethal hit | One critical impulse/FCT, enemy TakeDamage -> Die, Armed enemy token cancels, no retaliation. |
| Follow-up attack kills enemy | Remaining emitted enemy actions cancel according to authoritative plan; no inferred attack. |
| Scene disabled during animation | Presentation stops safely; authority is unchanged; unresolved required plan reconstructs on return. |
| Refresh during ordinary unresolved result | Reconstruct from accepted result/presentation save point and replay deterministically or complete via approved recovery path; do not silently unlock. |
| Refresh during Death | Always replay TakeDamage/Die before mandatory result until acknowledgement. |
| Settlement succeeds but acknowledgement write fails | Remain locked; duplicate acknowledgement/restart is idempotent. |
| Settlement fails | Terminal Retry; no Stage 1 interaction and no duplicated result arithmetic. |
| Missing animation/profile | Visible fallback pose/fade and logged configuration error; semantic sequence still completes safely. |
| Low frame rate | Markers cannot be skipped; elapsed-time evaluation reaches the semantic end state exactly once. |
| Multiple FCT events | Pool expands only to approved cap; deterministic offset/queue prevents overlap; no gameplay event is dropped. |
| Very long cooldown queue | Layout remains within available width using authored compression/scale policy; order and Attack token remain readable. |
| Reduced Motion | No shake/impulse or large translation; labels, outlines, fades, audio, and order preserve meaning. |
| Focus/keyboard leakage behind modal | Global shield and focus ownership prevent hidden Lobby buttons from receiving input. |

Primary risks:

- **Presentation deadlock:** mitigated by explicit barriers, completion bounds, deterministic cleanup, and logging by presentation/action ID.
- **Mixed uGUI/UI Toolkit coordinates:** mitigated by a target-anchor conversion contract and one combat-FX overlay owner.
- **Visual state contradicts authority:** mitigated by rebuilding from authoritative snapshots/receipts and never mutating gameplay from presentation callbacks.
- **Refresh duplicates effects or rewards:** presentation replay is cosmetic; authority and acknowledgement are transaction/idempotency keyed.
- **Too much juice obscures learning feedback:** answer result remains readable before action; intensity scales with significance; FCT/impulse never covers the question result.
- **God-object growth:** the later architecture must keep action planning, actor presentation, FCT, queue UI, input gating, and settlement presentation as separate responsibilities.

## 15. Playtest Plan

### New player test

- Complete attacks without explaining the action queue.
- Ask what the first box means, when the enemy will attack, who acted first, and why damage did/did not occur.
- Starting pass: correct explanations in at least 8/10 observed resolutions.

### Stress and recovery test

- Spam Attack, navigation, modal buttons, numpad, Submit, and keyboard shortcuts at every transition boundary.
- Refresh during answer result Exit, player Attack, enemy reaction, Walk, enemy Attack, queue reflow, enemy Appear, Die, Death panel Enter, settlement Retry, and Restart acknowledgement.
- Pass: one authoritative result, one action token per attempt, no unlocked gap, no duplicated reward, and mandatory Death replay until acknowledged.

### Skill test

- Mix fast correct, slow correct, incorrect, timeout, critical, enemy defeat, and Rank movement.
- Pass: players distinguish result types without relying only on color or raw numbers.

### Abuse test

- Repeatedly refresh to attempt to skip Die, reopen combat, duplicate settlement, reroll an encounter, or avoid an armed enemy Attack.
- Pass: the saved result/action remains fixed and interaction stays locked until the required presentation/acknowledgement completes.

### Readability test

- Show unlabeled clips of normal hit, critical hit, enemy Walk, enemy Attack, enemy defeat, player Death, and Rebirth.
- Starting pass: observers identify at least 9/10 normal-vs-critical outcomes and 10/10 Death-vs-Rebirth clips.

### Motion/accessibility test

- Repeat all significant flows with Reduced Motion.
- Pass: cause/order/result remain understandable, no combat-root impulse occurs, and no control becomes available earlier than in the full-motion path.

## 16. Acceptance Criteria

- [ ] Player, enemy, action box, FCT, and blocking UI state transitions have defined entry, exit, interrupt, and chain rules.
- [ ] Player action presents first after every accepted math result.
- [ ] Interaction cannot reopen until the authoritative ready state, empty action plan, both living actors Idle, stable action queue, and stable blocking UI all agree.
- [ ] Lethal exceptions route to enemy replacement/RunComplete or mandatory Death result without waiting for a dead actor to become Idle.
- [ ] FCT spawns from the affected enemy/player anchor and never from the player-menu/stat position.
- [ ] FCT performs Pop -> Hold -> Slide/Fade -> Pool return and exposes authorable text/style/timing data.
- [ ] Critical hits add a distinct label/audio/target response and combat-root impulse; Reduced Motion removes impulse.
- [ ] Enemy action boxes Enter, Arm, Consume, Exit, and reflow; the first-left box is removed every accepted turn.
- [ ] New enemy/reset queues animate Initiate and settle before input unlocks.
- [ ] Death and Rebirth share settlement data but have distinct animation, copy, audio, interaction, and persistence behavior.
- [ ] Die completes before the Death Result panel enters.
- [ ] Refresh before Death acknowledgement replays the required death sequence and keeps the game locked.
- [ ] Every combat UI unit follows Hidden -> Entering -> Idle -> Exiting -> Hidden, with explicit focus/picking rules.
- [ ] Follow-up attacks and counterattacks can be expressed as additional semantic action steps without presenter-specific lock code.
- [ ] The 5-component and required playtest checks pass at their stated starting targets.

## 17. Tuning Priority

If the sequence feels wrong, tune in this order:

1. **Response:** verify immediate acknowledgement, exclusive input ownership, and no unnecessary lock after semantic completion.
2. **Clarity:** verify action order, impact markers, queue meaning, FCT anchor, and Death/Rebirth distinction.
3. **Satisfaction:** tune recoil, FCT curves, audio layers, queue motion, and critical impulse.
4. **Fit:** tune pose weight, color language, Rebirth mood, and enemy-specific profile variation.
5. **Motivation:** only then adjust how progression outcomes are emphasized; do not change rewards in a juice pass.

## 18. GDD Alignment

- `@tag:core-loop`: strengthens Commit attack -> Combat result -> next decision.
- `@tag:combat-attempt`: preserves player damage before enemy retaliation and cancels enemy action on lethal damage.
- `@tag:stage-progression`: waits for new encounter Appear/Idle and queue initiation after saved Stage advancement.
- `@tag:run-reset`: preserves shared Death/Rebirth settlement rules while separating their visual experiences.
- `@tag:server-authority`: presentation consumes saved outcomes and cannot reroll or duplicate them.
- `@tag:feedback`: supplies visual/audio feedback for damage, critical, enemy defeat, player damage, Rank movement, Death, and Rebirth.
- `@tag:player-experience`: protects response and clarity before spectacle.
- `@tag:guardrails`: keeps progression authoritative and combat locked after terminal states.
- `@tag:playtest`: extends refresh, abuse, readability, and Reduced Motion validation.

No GDD mechanic is intentionally changed. The new mandatory presentation obligation is a recovery/presentation requirement layered on the existing idempotent attempt and settlement transactions. Architecture must document any persistence-schema change in an ADR before implementation.

## 19. Current-System Findings for the Architect

- The visible enemy and player are legacy uGUI Images named `monsterPrefab` and `playerPresentation` in `MainMenuScene.unity`.
- The existing FCT is a root-level UI Toolkit Label, so it has no target-relative relationship to the uGUI enemy.
- `CombatLobbyView.RenderEnemyActions` currently clears/recreates the complete list and retains spent boxes with opacity; it does not animate consumption or reflow.
- `CombatFeedbackPlayer` currently changes the enemy HUD card class for hit reaction, not the actual enemy sprite, and presents enemy retaliation through a banner rather than actor state.
- `AttemptFeedbackSequence` already provides a useful high-level order: answer feedback -> battle feedback -> Rank transition.
- `GameplaySavePoint` already includes `AttemptResolved` and `PresentationCompleted`, which can support replayable presentation, but restored `PresentingResult` state currently normalizes to a ready state.
- `RunSettlementPanelController` currently begins Death settlement immediately when it observes `RunDefeat`; the Die-before-panel and mandatory acknowledgement gate do not yet exist.
- Cinemachine is not present. The current Screen Space Overlay character Canvas means a Unity-camera-only impulse would not move the visible combat characters.

These findings describe the current workspace and do not authorize implementation.

## 20. Human Design Checkpoint

> [!NOTE]
> Approved by the project owner on 2026-08-28 (`LGTM`).

Approval covers:

1. The semantic action-plan model and “player primary action first” order, including FailedAttack on incorrect/timeout.
2. The strict unlock predicate and terminal-state exceptions.
3. `[Walk] x (cooldown - 1) -> [Attack]` as the enemy queue meaning, with Armed state at commit and visual consumption after player presentation.
4. Component-driven target-anchored FCT, authorable profile/prefab boundary, and critical combat-root impulse.
5. Mandatory replayable Death presentation and forced Restart, even when settlement already prepared Stage 1.
6. Voluntary Rebirth animation/result flow that shares settlement data but never reuses Die.
7. Tween/static-pose fallback until final character animation assets exist.
8. The starting timing values and playtest thresholds.

The next artifact is an architecture plan. Implementation, dependencies, scene changes, persistence-schema changes, build settings, publishing, PR merge, and release remain separate human checkpoints.
