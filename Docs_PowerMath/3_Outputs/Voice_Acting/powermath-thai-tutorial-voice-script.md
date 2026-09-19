---
slug: powermath-thai-tutorial-voice-script
status: draft
source: manual
gdd_tags:
  - tutorial-system
  - player-experience
owner: Codex
human_checkpoint: required
next_agent: human
blocked_by: []
---

# PowerMath — Thai Tutorial Voice-Acting Script

**Language:** Thai (`th`)  
**Source:** `Assets/Project/Resources/Localization/UI.json`  
**Scope:** `OnFirstCreate`, `OnFirstRankChange`, and `OnFirstEnemySurvive`  
**Speaker:** Power / tutorial voice (confirm final casting)  
**Revision:** 2026-09-17

## Recording notes

- Record one numbered line per file or take. Keep the localization key in the filename/session log.
- Replace `{0}` with the player display name. Record the name naturally in Thai; do not read `{0}` aloud.
- Text in brackets is a performance cue, not spoken dialogue.
- Ellipses (`...`) indicate a short suspenseful pause; the em dash (`—`) indicates a warm conversational beat.
- Capture a clean take plus one alternate take for lines marked **Alt** if schedule allows.

## Session 01 — OnFirstCreate

| # | Localization key | Thai dialogue | Performance direction |
|---:|---|---|---|
| 01 | `tutorial.onFirstCreate.welcomeNew` | สวัสดี {0}! ขอบคุณที่มานะ เราชื่อพาวเวอร์—ดีใจจริง ๆ ที่ได้เจอกัน! | Warm, bright welcome; smile through the line. Give `{0}` a friendly lift. |
| 02 | `tutorial.onFirstCreate.welcomeReturning` | ยินดีต้อนรับกลับนะ {0}! เราชื่อพาวเวอร์ มาดูกันว่าคณิตศาสตร์จะกลายเป็นพลังต่อสู้ได้อย่างไร! | Familiar and welcoming; build excitement on “พลังต่อสู้”. |
| 03 | `tutorial.onFirstCreate.mathWorld` | ที่นี่คือ Math:World ทุกโจทย์ที่เธอแก้ได้ จะเปลี่ยนความคิดของเธอให้เป็นพลังจริง ๆ! | Wonder and discovery; make the final “จริง ๆ” feel magical. |
| 04 | `tutorial.onFirstCreate.attackPrompt` | มีมอนสเตอร์ขวางทางอยู่ แตะมันเพื่อเริ่มการท้าทายครั้งแรกของเธอเลย! | Energetic instruction; clear emphasis on “แตะมัน”. |
| 05 | `tutorial.onFirstCreate.firstTryCorrect` | ครั้งแรกก็เยี่ยมมาก! คณิตศาสตร์ของเธอกลายเป็นการโจมตีจริง ๆ แล้ว! | Genuine celebration; punch the first sentence, then marvel at the result. |
| 06 | `tutorial.onFirstCreate.firstTryIncorrect` | การลองครั้งแรกยอดเยี่ยมมาก! ทุกครั้งที่ลอง พลังของเธอก็เติบโต ไปต่อกันเลย! | Encouraging, never disappointed; make “ไปต่อกันเลย!” motivating. |
| 07 | `tutorial.onFirstCreate.firstTryTimeout` | การลองครั้งแรกยอดเยี่ยมมาก! ค่อย ๆ คิดได้เลย แล้วเรามาลองอีกครั้งนะ! | Patient and reassuring; soften “ค่อย ๆ คิดได้เลย”. |
| 08 | `tutorial.onFirstCreate.firstTryAbandoned` | กลับมาแล้ว! การลองครั้งแรกของเธอยังมีความหมาย—เราจะอยู่ข้าง ๆ เอง ไปต่อกันนะ! | Relieved, supportive welcome-back; intimate and reassuring. |
| 09 | `tutorial.onFirstCreate.threatHandoff` | ว้าว—มอนสเตอร์ตัวใหม่! ดูน่ากลัวจัง... แต่เธอพร้อมแล้ว การต่อสู้นี้เป็นของเธอ! | Start with surprise, pause after “จัง...”, finish confident and empowering. |

## Session 02 — OnFirstRankChange

| # | Localization key | Thai dialogue | Performance direction |
|---:|---|---|---|
| 10 | `tutorial.onFirstRankChange.promotion` | {0} ก้าวถึงเป้าหมายการเรียนรู้ใหม่แล้ว! ทุกความท้าทายกำลังช่วยให้แรงก์ของเธอเติบโต | Proud congratulations; replace `{0}` with the player name. |
| 11 | `tutorial.onFirstRankChange.demotion` | แรงก์เปลี่ยนไป แต่สิ่งที่ {0} เรียนรู้ไม่ได้หายไปนะ มาดูสิ่งที่เธอสะสมไว้กัน | Gentle reassurance; never sound alarming. Replace `{0}` naturally. |
| 12 | `tutorial.onFirstRankChange.openProfile` | เปิดสถิติโปรไฟล์ แล้วเราจะพาไปดูประวัติการเรียนรู้ของเธอ | Helpful guide; clear action cue on “เปิดสถิติโปรไฟล์”. |
| 13 | `tutorial.onFirstRankChange.rankCurrency` | นี่คือสกุลเงินแรงก์ เธอไม่ต้องใช้มัน เพราะมันจะอยู่เป็นบันทึกเกียรติยศและประสบการณ์การเรียนรู้ตลอดไป | Calm explanation; emphasize that it is a permanent record. |
| 14 | `tutorial.onFirstRankChange.closeProfile` | ปิดสถิติโปรไฟล์ก่อน ต่อไปเราจะพาไปดูว่าบันทึกการเรียนรู้ของเธอเปล่งประกายตรงไหน | Smooth transition; upbeat curiosity on the final phrase. |
| 15 | `tutorial.onFirstRankChange.openLeaderboard` | ตอนนี้เปิดกระดานอันดับได้เลย | Short, crisp instruction; friendly confidence. |
| 16 | `tutorial.onFirstRankChange.leaderboardCurrency` | สกุลเงินแรงก์ที่สะสมไว้ช่วยแสดงความก้าวหน้าของเธอท่ามกลางผู้เรียนคนอื่น | Informative and encouraging; avoid competitive pressure. |
| 17 | `tutorial.onFirstRankChange.final` | เรียนรู้ต่อไปนะ เรื่องราวและบันทึกของเธอยังเติบโตได้อีก! | Warm send-off; hopeful lift on “เติบโตได้อีก”. |

## Session 03 — OnFirstEnemySurvive

| # | Localization key | Thai dialogue | Performance direction |
|---:|---|---|---|
| 18 | `tutorial.onFirstEnemySurvive.warning` | มอนสเตอร์ยังไม่ล้ม! ระวังนะ มอนสเตอร์ที่แข็งแกร่งจะขยับเข้าใกล้การโจมตีทุกครั้งที่เธอลงมือ | Alert but not frightening; clear warning, then explain calmly. |
| 19 | `tutorial.onFirstEnemySurvive.actions` | ช่องเหล่านี้คือแอ็กชันของมอนสเตอร์ MOVE แต่ละช่องคือการขยับเข้าใกล้อีกหนึ่งก้าว เมื่อช่อง ATTACK สีแดงมาถึงด้านหน้า มอนสเตอร์ที่ยังอยู่จะโจมตี! | Teaching cadence; distinguish “MOVE” and “ATTACK” clearly. |
| 20 | `tutorial.onFirstEnemySurvive.encouragement` | อย่ายอมแพ้นะ นักผจญภัย! ด้วยพลังแห่งคณิตศาสตร์ เธอเอาชนะมอนสเตอร์ทุกตัวได้แน่นอน | Rallying encouragement; heroic, optimistic finish. |

## Pickup / QA checklist

- [ ] Player-name substitution is tested with a short and long Thai display name.
- [ ] All 20 keys resolve to Thai text in `UI.json`.
- [ ] Correct, incorrect, timeout, abandoned, rank-change, and enemy-survive branches each have a distinct take.
- [ ] “MOVE”, “ATTACK”, and “Math:World” pronunciation is approved by the director.
- [ ] Final takes preserve the written pauses and do not add claims that change gameplay meaning.

