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
**Scope:** `OnFirstCreate`, `OnFirstRankChange`, `OnFirstEnemySurvive`, `OnFirstRebirthUnlock`, and `OnFirstRebirth`
**Speaker:** Power / tutorial voice (confirm final casting)  
**Revision:** 2026-09-22

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

## Sessions 04 to 06 — First Rebirth Sequence

The following is one uninterrupted post-Stage-30 onboarding arc. Session 04 is the pre-settlement explanation; Sessions 05 and 06 continue only after the authoritative death/Rebirth settlement has returned the player to Stage 1. The death route reuses lines 22 and 23, so those lines need only one recorded take.

### Session 04 — OnFirstRebirthUnlock

#### Route A — first death opens the Rebirth Die window

| Runtime order | Recording # | Localization key | Thai dialogue | Required presentation / action |
|---:|---:|---|---|---|
| 1 | 21 | `tutorial.onFirstRebirthUnlock.defeat` | โอ๊ะ! หัวใจหมดแล้ว... แต่ไม่เป็นไรนะ นักผจญภัย | `S4_01_VA` — show immediately when the Rebirth/Die window appears, before the player confirms restart. Power is briefly startled but stays bright and reassuring. |
| 2 | 22 | `tutorial.onFirstRebirthUnlock.rebirthExplanation` | เวลาสู้ต่อไม่ไหว เราสามารถ เกิดใหม่ และสืบทอดพลังเพื่อกลับมาแข็งแกร่งกว่าเดิมได้! | `S4_02_VA` — advance from line 21. Explain Rebirth with a hopeful, teaching tone. |
| 3 | 23 | `tutorial.onFirstRebirthUnlock.rebirthBenefits` | ยิ่งเธอผจญภัยมาไกล พอเกิดใหม่ก็จะได้รับพลังเพิ่มขึ้น และได้ Power Coin มากขึ้นด้วยนะ | `S4_03_VA` — spotlight the complete Legacy ATK and Power Coin panel while this line plays. |
| 4 | 24 | `tutorial.onFirstRebirthUnlock.rebirthPrompt` | พร้อมแล้วก็กด เกิดใหม่ กันเลย | `S4_04_VA` — focus `RebirthBtn`; accept only the valid Rebirth/restart command. |
| 5 | 25 | `tutorial.onFirstRebirthUnlock.rebirthResult` | ตอนนี้เธอแข็งแกร่งขึ้นแล้ว แถมยังได้ Power Coin มาด้วยนะ! | `S4_05_VA` — wait for the authoritative settlement and Stage 1 return, then play this line before Session 05 begins. |

#### Route B — the player opens Rebirth after beating Stage 30 (at Stage 31 or later)

| Runtime order | Reused recording # | Thai dialogue | Required presentation / action |
|---:|---:|---|---|
| 1 | 22 | เวลาสู้ต่อไม่ไหว เราสามารถ เกิดใหม่ และสืบทอดพลังเพื่อกลับมาแข็งแกร่งกว่าเดิมได้! | Trigger when the eligible player opens the Rebirth panel. |
| 2 | 23 | ยิ่งเธอผจญภัยมาไกล พอเกิดใหม่ก็จะได้รับพลังเพิ่มขึ้น และได้ Power Coin มากขึ้นด้วยนะ | Spotlight the complete Legacy ATK and Power Coin panel. The player retains the normal choice to confirm Rebirth. |

### Session 05 — OnFirstRebirth Pet Gacha

**Trigger:** the player has pressed Rebirth from either Session 04 route, the normal settlement has returned them to Stage 1, and the prior Session 04 dialogue has finished. The death route therefore flows directly into this session.

| Runtime order | Recording # | Localization key | Thai dialogue | Required presentation / action |
|---:|---:|---|---|---|
| 1 | 26 | `tutorial.onFirstRebirth.gachaInvite` | อืม.. แล้ว Power Coin ใช้ทำอะไรได้บ้างน่ะเหรอ? ตามฉันมาเลย! | `S5_01_VA` — focus `PlayerMenu/Pet-GachaBtn` and require the Pet Gacha panel to open. |
| 2 | 27 | `tutorial.onFirstRebirth.gachaIntroduction` | ที่นี่คือ แท่นอัญเชิญสัตว์เลี้ยง! เหล่าสัตว์เลี้ยงจะมาเป็นคู่หูผจญภัยของเธอ! | `S5_02_VA` — play as the Pet Gacha panel opens. |
| 3 | 28 | `tutorial.onFirstRebirth.petBenefits` | สัตว์เลี้ยงจะช่วยเพิ่มพลังให้เธอ และบางตัวยังช่วยโจมตีมอนสเตอร์ได้ด้วยนะ! | `S5_03_VA` — teach warmly and clearly. |
| 4 | 29 | `tutorial.onFirstRebirth.petRarity` | สัตว์เลี้ยงมีตั้งแต่ระดับ ธรรมดา หายาก ไปจนถึง หายากสุดๆ ยิ่งระดับสูงก็ยิ่งเก่ง! | `S5_04_VA` — continue the rarity explanation; make the rarity ladder easy to follow. |
| 5 | 30 | `tutorial.onFirstRebirth.gachaGrant` | ฉันจะให้ Power Coin เพิ่มนิดหน่อย ลองอัญเชิญคู่หูตัวแรกของเธอกันเลย! | `S5_05_VA` — trigger the one-time +180 Power Coin animation, then focus one pull and its confirmation. |
| 6 | 31 | `tutorial.onFirstRebirth.gachaResult` | ว้าว! เธอได้สัตว์เลี้ยงระดับสูงด้วยล่ะ คู่หูตัวนี้จะช่วยให้เธอแข็งแกร่งขึ้นแน่นอน! | `S5_06_VA` — wait for the authoritative Pet Reveal, then celebrate the result. Wait for the player to return to the Main Menu before Session 06. |

### Session 06 — Player Hub and Weapon Ascend

This is the final chapter of `OnFirstRebirth`, not a competing root tutorial. It starts only after Session 05 has reached the Main Menu.

| Runtime order | Recording # | Localization key | Thai dialogue | Required presentation / action |
|---:|---:|---|---|---|
| 1 | 32 | `tutorial.onFirstRebirth.playerHubInvite` | ยังมีอีกที่หนึ่งที่จะช่วยให้เธอเก่งขึ้นนะ ไปที่ Player Hub กัน! | `S6_01_VA` — focus `PlayerMenu/PlayerHubBtn`; require Player Hub to open. |
| 2 | 33 | `tutorial.onFirstRebirth.weaponAscendExplanation` | ตรงนี้เธอสามารถใช้ Power Coin เพื่ออัปเกรดดาบให้แข็งแกร่งขึ้นได้ | `S6_02_VA` — play in Player Hub. |
| 3 | 34 | `tutorial.onFirstRebirth.weaponAscendPrompt` | ดูจำนวน Power Coin ที่ต้องใช้ แล้วกด อัปเกรด ได้เลย | `S6_03_VA` — focus `AscendBtn` and permit the normal upgrade command. The tutorial budget ensures sufficient Power Coin; no insufficient-funds branch is required. |
| 4 | 35 | `tutorial.onFirstRebirth.weaponAscendResult` | สำเร็จ! ดาบของเธอแรงขึ้นแล้ว แบบนี้สู้มอนสเตอร์ได้ง่ายขึ้นแน่นอน! | `S6_04_VA` — wait for authoritative upgrade success before playing. |
| 5 | 36 | `tutorial.onFirstRebirth.petTabPrompt` | แล้วก็ถ้าอยากดูคู่หูของเธอ ก็กดที่เมนู สัตว์เลี้ยง ได้เลยนะ! | `S6_05_VA` — focus `PetTabMenu`; require the Pet tab to open. |
| 6 | 37 | `tutorial.onFirstRebirth.duplicatePetBenefit` | สัตว์เลี้ยงที่สุ่มได้ซ้ำมาจะช่วยเพิ่มค่าสถานะให้ด้วยนะ | `S6_06_VA` — explain the account-wide benefit of duplicates. |
| 7 | 38 | `tutorial.onFirstRebirth.final` | พร้อมกว่าเดิมแล้วใช่ไหม? งั้นกลับไปลุยกันต่อเลย นักผจญภัย! | `S6_07_VA` — complete `OnFirstRebirth` only after this dialogue advances. |

## Sessions 04 to 06 recording notes

- Record lines 21–38 in numerical order. Lines 22 and 23 are shared by both Session 04 entry routes and must use the same approved take.
- `+180 Power Coin`, one pull, Pet Reveal, and Weapon Ascend are gameplay waits and focus actions, not additional spoken lines.
- The imported cue IDs map exactly to `S4_01_VA` through `S4_05_VA`, `S5_01_VA` through `S5_06_VA`, and `S6_01_VA` through `S6_07_VA` in `Assets/Project/Resources/Voice/`.
- Proposed localization keys must be added and verified before implementation; the existing `UI.json` currently contains only Sessions 01–03 keys.

## Pickup / QA checklist

- [ ] Player-name substitution is tested with a short and long Thai display name.
- [ ] All 20 existing keys resolve to Thai text in `UI.json`.
- [ ] Proposed keys for recordings 21–38 resolve to the exact approved Thai text before the new sequence ships.
- [ ] Correct, incorrect, timeout, abandoned, rank-change, and enemy-survive branches each have a distinct take.
- [ ] “MOVE”, “ATTACK”, and “Math:World” pronunciation is approved by the director.
- [ ] Final takes preserve the written pauses and do not add claims that change gameplay meaning.

