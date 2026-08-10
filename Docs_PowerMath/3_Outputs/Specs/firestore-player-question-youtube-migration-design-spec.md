---
slug: firestore-player-question-youtube-migration
status: approved
source: manual
gdd_tags:
  - core-loop
  - combat-attempt
  - answer-scoring
  - question-data
  - run-reset
  - server-authority
  - guardrails
owner: game-design-agent
human_checkpoint: required
next_agent: architect-agent
blocked_by: []
---

# Design Spec: Firestore Player/Question Migration and YouTube Presentation

## 1. Player Goal and Context

The student should reach the Lobby with a usable saved profile even when an older account is missing newly required PowerMath fields. Pressing Attack should clearly lock one Rank-appropriate question and play its YouTube lesson/problem inside the WebGL game surface before the existing answer flow begins.

The student must never be told that a storage migration is happening. They see a normal loading state; on recoverable failure they see a specific retry message rather than entering a partially initialized Lobby.

## 2. Approved-Intent Assumptions Requiring Confirmation

### A. YouTube presentation

**Approved interpretation:** the WebGL build creates an official YouTube IFrame Player over the Unity canvas through a project-owned `.jslib` bridge. Unity receives ready, ended, and error callbacks. `ENDED` automatically begins the existing one-second preparation and ten-second answer flow.

- No external browser/tab and no third-party or Asset Store package.
- The Unity Editor uses a deterministic mock because an HTML iframe cannot render inside the Editor Game view.
- Playback controls remain available so browser autoplay blocking never traps the attempt.
- An embed-disabled, removed, invalid, or failed video is a content failure and voids the attempt.

### B. Competition schema

**Recommended interpretation:** `student-name` remains a dynamic map field inside each level document, not another Firestore collection/path segment.

```text
competition/{level1 | level2 | level3}
  {normalizedStudentName}: map
    userdata: map
      username
      password
    gamedata: map
      revision
      profile
      progression
      wallet
      inventory
      loadout
      activeRun
      academic
```

There is no `game1` wrapper. A student absent from all three level documents is still an invalid login and is not created.

### C. Three-document question schema

**Recommended interpretation:** one `question` collection contains exactly three Rank documents, each with an `items` array.

```text
question/silver
  items: [ { id, video_link, answer }, ... ]

question/gold
  items: [ { id, video_link, answer }, ... ]

question/diamond
  items: [ { id, video_link, answer }, ... ]
```

- `rank` is inferred from the document ID and is not present in an item.
- `id` is a non-negative integer, sorted numerically, unique only within its Rank.
- Runtime identity is `(Rank, id)`, so Silver `1` and Gold `1` are distinct.
- IDs must remain stable after publishing; reordering by renumbering would corrupt player question history.

## 3. Player Interaction Flow

```text
Student submits existing credentials
  -> PowerMath reads the matching level document
    -> Complete gamedata: continue without a write
    -> Missing required fields: show normal loading, add missing defaults, re-read
      -> Valid snapshot: enter Lobby
      -> Conflict/network/permission failure: remain in Bootstrap with Retry

Student presses Attack
  -> Lock active-Rank QuestionKey and consume enemy cooldown
    -> Show question ID/Rank and embedded YouTube player
      -> Player starts/watches the video inside the game surface
        -> YouTube ENDED callback
          -> Existing preparation, timer, numpad, resolution, audit, and combat flow
```

## 4. State Rules

### Bootstrap/default creation

- Entry: valid username/password already exists in exactly one configured level document.
- Mutation: only missing required leaf fields receive defaults; existing values are never reset merely because another field is missing.
- Exit success: a second read maps a complete supported snapshot.
- Exit failure: Bootstrap remains visible with Retry/Logout; Main Menu is not loaded.
- Interruptibility: cancel scene/service disposal safely aborts the request; no local success is published.
- Concurrency: a stale document version causes reload/re-evaluation before one bounded retry.
- Missing student: invalid credentials; do not register a new account.

### YouTube question presentation

- Entry: an attempt and QuestionKey have been committed and the URL passed validation.
- Exit: official YouTube IFrame `ENDED` callback.
- Interruptibility: content/open failure before confirmation voids the attempt and restores cooldown/question reservation.
- Abandonment: refresh/close after valid playback begins remains an incorrect committed attempt under the GDD.
- Chained state: committed -> embedded player loading -> playing -> preparation -> answering -> resolution.

## 5. Default `gamedata` Contract

Defaults apply only when the corresponding field is missing:

| Area | Defaults |
| --- | --- |
| Revision | `revision = 0` |
| Profile | `displayName = username`, `iconId = avatar-default` |
| Progression | `currentStage = 1`, `highestStage = 1`, `activeRank = Silver`, `prestige = 0`, `firstStage200Reached = false` |
| Wallet | `silver/gold/diamond/powerCoins = 0` |
| Inventory | empty array |
| Loadout | empty pet/weapon/avatar IDs |
| Active run | empty run ID/attempt ID, `currentStage = 1` |
| Audit | score `0`, resolved count `0` |
| Rank inventories | cycle `0`; pending IDs initialized lazily from the loaded catalog; empty failed/current-audit/cleared history |

`rankProgress` is not created or interpreted because its semantics remain undocumented; explicit audit fields replace that ambiguity.

## 6. Feedback Loops

| Trigger | Visual | Audio | Timing |
| --- | --- | --- | --- |
| Missing defaults detected | Existing Bootstrap loading state; no alarming migration language | Existing loading ambience | Until write and verification finish |
| Default creation fails | Specific retry message: network, permission, conflict exhaustion, or invalid data | Existing error cue | Persistent until Retry/Logout |
| Question committed | Rank and question ID remain visible; embedded player loads above the combat surface | Existing Attack acknowledgement | Immediate |
| YouTube ready | Standard YouTube controls remain visible; answer controls stay hidden | Native YouTube audio | Until video ends |
| URL cannot open | Neutral content-unavailable message; attempt restored | Existing content-error cue | Return to EnemyReady |
| YouTube ends | Player overlay closes; numpad appears and existing preparation/timer begins | Existing answer-ready cue | GDD-defined timing |

No new timing or balance number is introduced. The one-second preparation and ten-second timer remain source-backed by `@tag:answer-scoring`.

## 7. Five-Component Evaluation

| Component | Design response | Acceptance signal |
| --- | --- | --- |
| Clarity | Embedded video occupies the question panel; answer controls appear only after `ENDED` | New player can explain why the answer UI has not started while video is active |
| Motivation | Answer still affects persistent audit, Rank Currency, Rank, and combat | Re-entering the game restores the same academic state |
| Response | Native YouTube controls acknowledge input; callbacks advance exactly once | Spam cannot create multiple players, callbacks, committed questions, or resolutions |
| Satisfaction | Existing correct/damage/currency/rank feedback remains unchanged after the new presentation step | Observer can distinguish content launch from answer success |
| Fit | YouTube is treated as educational content delivery, not as combat feedback | Stage, Rank, and question identity remain visually separate |

## 8. Risks and Abuse Cases

- Direct Firestore content exposes answers and permits client tampering; explicitly accepted for this prototype but must remain documented.
- Browser autoplay can be blocked; standard controls remain available and `onAutoplayBlocked` is non-terminal.
- YouTube owners may disable embedding; error codes are content failures, never incorrect answers.
- Three large Rank documents are subject to Firestore document-size limits; catalog activation must fail visibly when payload/validation is invalid.
- Numeric IDs used as both identity and order cannot be safely renumbered after player history exists.
- Updating a dynamic student field inside a shared level document creates contention; version-precondition retry is required.
- Missing-field initialization must never replace existing arrays/maps wholesale.
- Firebase unavailable during Bootstrap must not produce unsaved local defaults that look persistent.

## 9. Playtest and Verification Scenarios

- New player: play the embedded video and reach answering without verbal instruction.
- Stress: spam Attack/player controls and confirm only one iframe, attempt, and completion callback exist.
- Recovery: block autoplay, deny embedding, lose network, reload during video, and retry Bootstrap.
- Persistence: initialize an incomplete user, restart Unity, and confirm the same completed state loads without a second write.
- Preservation: seed custom stage/currency/inventory values with one missing field and confirm they remain byte-for-byte equivalent after initialization.
- Rank data: same numeric ID in two Rank documents remains two distinct histories.
- Readability: observer can distinguish Stage, Rank, question ID, and video state without relying on color.

## 10. GDD/ADR Reconciliation Required After Approval

- Update `@tag:core-loop` and `@tag:question-data` from direct `.mp4` playback to validated embedded YouTube presentation.
- Remove per-item `rank`; document Rank inference from `question/{rank}`.
- Record numeric per-Rank IDs and composite runtime identity.
- Supersede ADR-003's collection/path and `game1` schema.
- Supersede ADR-005's infrastructure-only/no-live-Firestore restriction while retaining the atomic domain boundary.
- Record the explicit direct-client answer/write risks as project-owner accepted prototype risks.

## 11. Human Design Checkpoint

Approved by the project owner on 2026-08-10 (`ok LGTM, implementation`):

1. YouTube uses an embedded official IFrame Player through a project-owned WebGL bridge; no external tab or third-party package.
2. `competition/{level}` remains a shared level document whose dynamic `{student-name}` map contains `userdata` and direct `gamedata`.
3. `question/{silver|gold|diamond}` contains `items: [{id, video_link, answer}]`; IDs are numeric, stable, and scoped by Rank.
