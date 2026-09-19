---
slug: on-first-enemy-survive-tutorial
status: approved
source: manual
human_checkpoint: required
---

# Design Spec: `OnFirstEnemySurvive`

> Approved by the project owner with `lgtm` on 2026-09-15.

## Authored Flow

| State | Type | Power | Player action |
|---|---|---|---|
| `survival-warning` | Dialogue | scared/alert | Tap anywhere |
| `queue-explanation` | Dialogue | teaching | Tap anywhere |
| `focus-enemy-actions` | Interaction | hidden dialogue | Tap highlighted Enemy Action Queue |
| `final-encouragement` | Dialogue reaction | encouraging | Tap anywhere to complete |

## Proposed Localized Copy

| Key | English | Thai |
|---|---|---|
| `tutorial.onFirstEnemySurvive.warning` | That monster survived! Be careful—strong monsters move closer to attacking every time you commit an action. | มอนสเตอร์ยังไม่ล้ม! ระวังนะ มอนสเตอร์ที่แข็งแกร่งจะขยับเข้าใกล้การโจมตีทุกครั้งที่เธอลงมือ |
| `tutorial.onFirstEnemySurvive.actions` | These boxes are the monster's actions. Each MOVE is one step closer. When the red ATTACK box reaches the front, a surviving monster attacks! | ช่องเหล่านี้คือแอ็กชันของมอนสเตอร์ MOVE แต่ละช่องคือการขยับเข้าใกล้อีกหนึ่งก้าว เมื่อช่อง ATTACK สีแดงมาถึงด้านหน้า มอนสเตอร์ที่ยังอยู่จะโจมตี! |
| `tutorial.onFirstEnemySurvive.encouragement` | Don't give up, adventurer! With the power of mathematics, you can overcome any monster. | อย่ายอมแพ้นะ นักผจญภัย! ด้วยพลังแห่งคณิตศาสตร์ เธอเอาชนะมอนสเตอร์ทุกตัวได้แน่นอน |

## Player-Experience Constraints

- Clarity: warning follows the first observed cooldown consumption, then points at the exact queue.
- Response: only the highlighted queue acknowledgement is accepted during focus.
- Satisfaction: scared warning resolves into encouragement through pose and audio feedback.
- No numbers are introduced or tuned; the tutorial explains the live enemy-authored cooldown.
- A qualifying result that also kills the player finishes death/settlement first and queues this tutorial for the next safe Lobby.
- No legacy backfill is inferred when saved history cannot prove that a specific enemy survived a damaging hit.
