# Patch 1.2 Announcement Specification

This document contains the official Patch 1.2 announcement copy in both English (`bodyEn`) and Thai (`bodyTh`), formatted to match the authoring structure and Markdown renderer specifications established in Patch 1.1 (`Assets/Project/Resources/Announcements/Catalog.json` and `AnnouncementMarkdownRenderer.cs`).

---

## Catalog Metadata Entry (JSON)

```json
{
  "id": "patch-1.2-update",
  "version": "1.2",
  "publishedAtUtc": "2026-09-21T00:00:00Z",
  "featured": true,
  "titleEn": "Pet Gacha & Player Hub Arrival!",
  "titleTh": "ระบบสัตว์เลี้ยงและฮับผู้เล่นมาแล้ว!",
  "bodyEn": "# Patch 1.2 Update\n\nHello, Adventurer! ✨ The wait is over — Patch 1.2 is officially here! We appreciate your patience while we polished the experience, tuned combat balance, and optimized performance across all platforms.\n\nHere are the Patch 1.2 updates:\n\n## New Features\n\n- 🐾 **[ADD]** **Pet Gacha System Unlocked**: The Pet Summoning system is now live! Experience the new cinematic summon sequence with rarity comets, celestial bursts, and dramatic individual reveals (White R, Purple SR, Yellow SSR). Supports both 1x and 10x summons, rapid Skip controls, and account-wide collection passives.\n\n- 🏰 **[ADD]** **Player Hub Unlocked**: The Player Hub is now open! Visit your personal sanctuary to interact with Ricko and Stellar, review your growing pet roster, and prepare for weapon ascension.\n\n- ⚔️ **[ADD]** **Pet Combat Scaling & Counter-Attacks**: Companion pets now actively join the battle! Academic Rank damage multipliers scale Pet Follow-Up and Counter-Attack damage, and pets will automatically counter-attack when you lose hearts from enemy hits.\n\n- 🎓 **Academic Rank Multipliers Rebalanced**: Rebalanced high-rank combat damage multipliers — Gold rank damage multiplier increased to **x1.25** and Diamond rank increased to **x1.50** to make subject mastery even more rewarding!\n\n- 💰 **Economy & Run Settlement Rebalance**: Standardized Pet Gacha summon cost to 180 Power Coins, calibrated starter tutorial rewards, and enhanced dungeon run completion payouts based on cleared stages.\n\n- 🎙️ **[ADD]** **Story Voice Acting & New Music**: Integrated Thai voice acting clips for Story Chapters 2 & 3, alongside the brand-new Biome 5 background music theme (`OST_B5-Theme`).\n\n- 🔄 **Schema V6 & Firestore Modernization**: Upgraded progression storage to per-user collections with atomic cooldown persistence, full challenge scheduling, and automatic recovery of interrupted encounters.\n\n---\n\n## Bug Fixes & Optimizations\n\n- 🚀 **WebGL Memory & Startup Optimization**: Lowered initial heap allocation from 512MB to 256MB and capped maximum heap to 1024MB, resolving startup out-of-memory crashes on mobile devices and iOS Safari.\n\n- 🛡️ **Release Alignment Guard**: Added strict client-manifest version matching to prevent unexpected cache-purge reload loops.\n\n- ⏱️ **Question Catalog Reliability**: Extended question loading deadlines from 2.5s to 12s, eliminating premature network timeouts.\n\n- 📚 **Fail-Safe Practice Mode**: Fixed question network dropouts falling back to developer sample videos; offline questions now safely run in an isolated Practice Session with zero progression risk.\n\n- 🎵 **Audio Ducking Silence Fix**: Fixed background music remaining muted after answering questions or defeating encounter bosses.\n\n- 🎬 **Optimized Streaming Media**: Re-encoded and compressed WebGL streaming videos to reduce bandwidth usage and improve scene transition speeds.\n\n- 👾 **Encounter Attempt Recovery**: Handled interrupted attempts and orphaned presentation states cleanly upon run hydration.\n\n---\n\n## 👀 Update Preview\n\n![Pet Gacha & Collection](resource:Announcements/Images/Pet State)\n\n![Player Hub & Weapon State](resource:Announcements/Images/Weapon State)",
  "bodyTh": "# อัปเดตแพตช์เกม 1.2\n\nสวัสดี นักผจญภัย! ✨ สิ้นสุดการรอคอย — แพตช์ 1.2 เปิดให้อัปเดตอย่างเป็นทางการแล้ว! ขอขอบคุณทุกท่านที่อดทนรอคอยในระหว่างที่เราขัดเกลาระบบ ปรับสมดุลการต่อสู้ และเพิ่มประสิทธิภาพการเล่นเกมให้ลื่นไหลยิ่งขึ้นในทุกแพลตฟอร์ม\n\nปัจจุบันทางเกมขอนำเสนอการอัปเดตแพตช์ 1.2 ดังนี้\n\n## อัปเดตใหม่\n\n- 🐾 **[ADD]** **เปิดระบบสุ่มสัตว์เลี้ยง (Pet Gacha)**: ระบบสุ่มสัตว์เลี้ยงเปิดให้ใช้งานแล้วอย่างเต็มรูปแบบ! สัมผัสประสบการณ์อนิเมชันเปิดตัวสุดอลังการ ทั้งดาวตกนำโชคตามระดับความหายาก (สีขาว R, สีม่วง SR, สีทอง SSR), แสงระเบิดออร่า และฉากเปิดตัวตัวละครสุดตื่นตา รองรับการสุ่มทั้งแบบ 1 ครั้ง และ 10 ครั้ง พร้อมระบบข้าม (Skip) และบัฟสะสมสัตว์เลี้ยงทั้งไอดี (Collection Passives)\n\n- 🏰 **[ADD]** **เปิดระบบฮับผู้เล่น (Player Hub)**: เข้าสู่ศูนย์กลางของผู้เล่นได้แล้ววันนี้! พบกับภาพเคลื่อนไหวตัวละคร Ricko และ Stellar แบบจัดเต็ม จัดการคลังสัตว์เลี้ยง และเตรียมความพร้อมสำหรับการเลื่อนขั้นอาวุธ (Weapon Ascension)\n\n- ⚔️ **[ADD]** **พลังต่อสู้และการสวนกลับของสัตว์เลี้ยง**: สัตว์เลี้ยงคู่หูจะร่วมต่อสู้เคียงข้างคุณในสนามประลอง! บัฟพลังโจมตีตามแรงก์วิชาการจะช่วยเพิ่มความเสียหายให้กับการโจมตีตาม (Follow-Up) และการสวนกลับ (Counter-Attack) ของสัตว์เลี้ยง โดยสัตว์เลี้ยงจะโจมตีสวนกลับทันทีเมื่อคุณเสียหัวใจจากการโจมตีของศัตรู\n\n- 🎓 **ปรับสมดุลตัวคูณพลังโจมตีตามแรงก์**: ยกระดับความสำคัญของแรงก์วิชาการ โดยปรับเพิ่มตัวคูณพลังโจมตีของระดับ Gold เป็น **x1.25** และระดับ Diamond เป็น **x1.50** เพื่อให้ทุกคำตอบที่ถูกต้องทรงพลังยิ่งขึ้น!\n\n- 💰 **ปรับสมดุลค่าเงินและรางวัลจบด่าน**: ปรับต้นทุนการสุ่มสัตว์เลี้ยงเป็น 180 เหรียญ Power Coin ปรับรางวัลสอนเล่นเริ่มต้น และปรับสูตรเหรียญรางวัลเมื่อจบการผจญภัย (Run Settlement) ให้คุ้มค่ากับด่านที่ลุยได้ลึกขึ้น\n\n- 🎙️ **[ADD]** **เพิ่มเสียงพากย์เนื้อเรื่องและเพลงใหม่**: เพิ่มไฟล์เสียงพากย์ภาษาไทยสำหรับเนื้อเรื่องบทที่ 2 และบทที่ 3 พร้อมเพลงประกอบพื้นที่ Biome 5 ใหม่ (`OST_B5-Theme`)\n\n- 🔄 **อัปเกรดฐานข้อมูลสู่ Schema V6**: ปรับระบบบันทึกความคืบหน้าสู่ Firestore แยกรายบุคคล พร้อมระบบบันทึกคูลดาวน์มอนสเตอร์อย่างแม่นยำ และกู้คืนสถานะการต่อสู้ที่ค้างอยู่ได้อย่างราบรื่น\n\n---\n\n## การแก้ไขบัคและการปรับปรุงประสิทธิภาพ\n\n- 🚀 **ปรับลดการใช้หน่วยความจำ WebGL**: ปรับลดขนาดหน่วยความจำเริ่มต้น (Initial Heap) จาก 512MB เหลือ 256MB และจำกัดเพดานสูงสุดที่ 1024MB แก้ไขปัญหาเกมแครชหรือรีโหลดซ้ำบนเบราว์เซอร์มือถือและ iOS Safari\n\n- 🛡️ **ระบบป้องกันการรีโหลดแคชซ้ำซ้อน**: เพิ่มระบบตรวจสอบความสอดคล้องระหว่างเวอร์ชันเกมและ Manifest ป้องกันการรีโหลดตัวเกมแบบไม่รู้จบ\n\n- ⏱️ **ปรับปรุงการโหลดคำถามให้เสถียรขึ้น**: ขยายเวลารอโหลดคำถามจาก 2.5 วินาที เป็น 12 วินาที เพื่อรองรับการเชื่อมต่ออินเทอร์เน็ตที่มีความหน่วง โดยไม่หลุดการเชื่อมต่อ\n\n- 📚 **ระบบฝึกซ้อมฉุกเฉิน (Isolated Practice Mode)**: แก้ไขปัญหาวิดีโอคำถามตัวอย่างของนักพัฒนาหลุดเข้ามาแสดงผล โดยหากโหลดโจทย์ออนไลน์ไม่สำเร็จ ระบบจะสลับเข้าสู่โหมดฝึกฝนชั่วคราวอย่างปลอดภัย พร้อมป้ายแจ้งเตือนชัดเจนว่าจะไม่บันทึกความคืบหน้า\n\n- 🎵 **แก้ไขเสียงเพลงหายหลังตอบคำถาม**: แก้ไขปัญหาเสียงเพลงแบ็กกราวนด์เงียบหลังจากการลดระดับเสียง (Audio Ducking) ในฉากคำถามหรือเมื่อเอาชนะบอส\n\n- 🎬 **บีบอัดและปรับขนาดวิดีโอ**: ปรับลดขนาดไฟล์วิดีโอ MP4 ใน StreamingAssets ช่วยประหยัดแบนด์วิดท์และทำให้ดาวน์โหลดเข้าเกมได้รวดเร็วขึ้น\n\n- 👾 **ระบบกู้คืนสถานะการต่อสู้**: ป้องกันข้อมูลการต่อสู้สูญหายเมื่อเน็ตหลุดหรือปิดเกมกะทันหันขณะกำลังต่อสู้\n\n---\n\n## 👀 ส่องอัปเดต\n\n![Pet Gacha & Collection](resource:Announcements/Images/Pet State)\n\n![Player Hub & Weapon State](resource:Announcements/Images/Weapon State)"
}
```

---

## Standalone Markdown Format

### English Version (`bodyEn`)

# Patch 1.2 Update

Hello, Adventurer! ✨ The wait is over — Patch 1.2 is officially here! We appreciate your patience while we polished the experience, tuned combat balance, and optimized performance across all platforms.

Here are the Patch 1.2 updates:

## New Features

- 🐾 **[ADD]** **Pet Gacha System Unlocked**: The Pet Summoning system is now live! Experience the new cinematic summon sequence with rarity comets, celestial bursts, and dramatic individual reveals (White R, Purple SR, Yellow SSR). Supports both 1x and 10x summons, rapid Skip controls, and account-wide collection passives.

- 🏰 **[ADD]** **Player Hub Unlocked**: The Player Hub is now open! Visit your personal sanctuary to interact with Ricko and Stellar, review your growing pet roster, and prepare for weapon ascension.

- ⚔️ **[ADD]** **Pet Combat Scaling & Counter-Attacks**: Companion pets now actively join the battle! Academic Rank damage multipliers scale Pet Follow-Up and Counter-Attack damage, and pets will automatically counter-attack when you lose hearts from enemy hits.

- 🎓 **Academic Rank Multipliers Rebalanced**: Rebalanced high-rank combat damage multipliers — Gold rank damage multiplier increased to **x1.25** and Diamond rank increased to **x1.50** to make subject mastery even more rewarding!

- 💰 **Economy & Run Settlement Rebalance**: Standardized Pet Gacha summon cost to 180 Power Coins, calibrated starter tutorial rewards, and enhanced dungeon run completion payouts based on cleared stages.

- 🎙️ **[ADD]** **Story Voice Acting & New Music**: Integrated Thai voice acting clips for Story Chapters 2 & 3, alongside the brand-new Biome 5 background music theme (`OST_B5-Theme`).

- 🔄 **Schema V6 & Firestore Modernization**: Upgraded progression storage to per-user collections with atomic cooldown persistence, full challenge scheduling, and automatic recovery of interrupted encounters.

---

## Bug Fixes & Optimizations

- 🚀 **WebGL Memory & Startup Optimization**: Lowered initial heap allocation from 512MB to 256MB and capped maximum heap to 1024MB, resolving startup out-of-memory crashes on mobile devices and iOS Safari.

- 🛡️ **Release Alignment Guard**: Added strict client-manifest version matching to prevent unexpected cache-purge reload loops.

- ⏱️ **Question Catalog Reliability**: Extended question loading deadlines from 2.5s to 12s, eliminating premature network timeouts.

- 📚 **Fail-Safe Practice Mode**: Fixed question network dropouts falling back to developer sample videos; offline questions now safely run in an isolated Practice Session with zero progression risk.

- 🎵 **Audio Ducking Silence Fix**: Fixed background music remaining muted after answering questions or defeating encounter bosses.

- 🎬 **Optimized Streaming Media**: Re-encoded and compressed WebGL streaming videos to reduce bandwidth usage and improve scene transition speeds.

- 👾 **Encounter Attempt Recovery**: Handled interrupted attempts and orphaned presentation states cleanly upon run hydration.

---

## 👀 Update Preview

![Pet Gacha & Collection](resource:Announcements/Images/Pet State)

![Player Hub & Weapon State](resource:Announcements/Images/Weapon State)

---

### Thai Version (`bodyTh`)

# อัปเดตแพตช์เกม 1.2

สวัสดี นักผจญภัย! ✨ สิ้นสุดการรอคอย — แพตช์ 1.2 เปิดให้อัปเดตอย่างเป็นทางการแล้ว! ขอขอบคุณทุกท่านที่อดทนรอคอยในระหว่างที่เราขัดเกลาระบบ ปรับสมดุลการต่อสู้ และเพิ่มประสิทธิภาพการเล่นเกมให้ลื่นไหลยิ่งขึ้นในทุกแพลตฟอร์ม

ปัจจุบันทางเกมขอนำเสนอการอัปเดตแพตช์ 1.2 ดังนี้

## อัปเดตใหม่

- 🐾 **[ADD]** **เปิดระบบสุ่มสัตว์เลี้ยง (Pet Gacha)**: ระบบสุ่มสัตว์เลี้ยงเปิดให้ใช้งานแล้วอย่างเต็มรูปแบบ! สัมผัสประสบการณ์อนิเมชันเปิดตัวสุดอลังการ ทั้งดาวตกนำโชคตามระดับความหายาก (สีขาว R, สีม่วง SR, สีทอง SSR), แสงระเบิดออร่า และฉากเปิดตัวตัวละครสุดตื่นตา รองรับการสุ่มทั้งแบบ 1 ครั้ง และ 10 ครั้ง พร้อมระบบข้าม (Skip) และบัฟสะสมสัตว์เลี้ยงทั้งไอดี (Collection Passives)

- 🏰 **[ADD]** **เปิดระบบฮับผู้เล่น (Player Hub)**: เข้าสู่ศูนย์กลางของผู้เล่นได้แล้ววันนี้! พบกับภาพเคลื่อนไหวตัวละคร Ricko และ Stellar แบบจัดเต็ม จัดการคลังสัตว์เลี้ยง และเตรียมความพร้อมสำหรับการเลื่อนขั้นอาวุธ (Weapon Ascension)

- ⚔️ **[ADD]** **พลังต่อสู้และการสวนกลับของสัตว์เลี้ยง**: สัตว์เลี้ยงคู่หูจะร่วมต่อสู้เคียงข้างคุณในสนามประลอง! บัฟพลังโจมตีตามแรงก์วิชาการจะช่วยเพิ่มความเสียหายให้กับการโจมตีตาม (Follow-Up) และการสวนกลับ (Counter-Attack) ของสัตว์เลี้ยง โดยสัตว์เลี้ยงจะโจมตีสวนกลับทันทีเมื่อคุณเสียหัวใจจากการโจมตีของศัตรู

- 🎓 **ปรับสมดุลตัวคูณพลังโจมตีตามแรงก์**: ยกระดับความสำคัญของแรงก์วิชาการ โดยปรับเพิ่มตัวคูณพลังโจมตีของระดับ Gold เป็น **x1.25** และระดับ Diamond เป็น **x1.50** เพื่อให้ทุกคำตอบที่ถูกต้องทรงพลังยิ่งขึ้น!

- 💰 **ปรับสมดุลค่าเงินและรางวัลจบด่าน**: ปรับต้นทุนการสุ่มสัตว์เลี้ยงเป็น 180 เหรียญ Power Coin ปรับรางวัลสอนเล่นเริ่มต้น และปรับสูตรเหรียญรางวัลเมื่อจบการผจญภัย (Run Settlement) ให้คุ้มค่ากับด่านที่ลุยได้ลึกขึ้น

- 🎙️ **[ADD]** **เพิ่มเสียงพากย์เนื้อเรื่องและเพลงใหม่**: เพิ่มไฟล์เสียงพากย์ภาษาไทยสำหรับเนื้อเรื่องบทที่ 2 และบทที่ 3 พร้อมเพลงประกอบพื้นที่ Biome 5 ใหม่ (`OST_B5-Theme`)

- 🔄 **อัปเกรดฐานข้อมูลสู่ Schema V6**: ปรับระบบบันทึกความคืบหน้าสู่ Firestore แยกรายบุคคล พร้อมระบบบันทึกคูลดาวน์มอนสเตอร์อย่างแม่นยำ และกู้คืนสถานะการต่อสู้ที่ค้างอยู่ได้อย่างราบรื่น

---

## การแก้ไขบัคและการปรับปรุงประสิทธิภาพ

- 🚀 **ปรับลดการใช้หน่วยความจำ WebGL**: ปรับลดขนาดหน่วยความจำเริ่มต้น (Initial Heap) จาก 512MB เหลือ 256MB และจำกัดเพดานสูงสุดที่ 1024MB แก้ไขปัญหาเกมแครชหรือรีโหลดซ้ำบนเบราว์เซอร์มือถือและ iOS Safari

- 🛡️ **ระบบป้องกันการรีโหลดแคชซ้ำซ้อน**: เพิ่มระบบตรวจสอบความสอดคล้องระหว่างเวอร์ชันเกมและ Manifest ป้องกันการรีโหลดตัวเกมแบบไม่รู้จบ

- ⏱️ **ปรับปรุงการโหลดคำถามให้เสถียรขึ้น**: ขยายเวลารอโหลดคำถามจาก 2.5 วินาที เป็น 12 วินาที เพื่อรองรับการเชื่อมต่ออินเทอร์เน็ตที่มีความหน่วง โดยไม่หลุดการเชื่อมต่อ

- 📚 **ระบบฝึกซ้อมฉุกเฉิน (Isolated Practice Mode)**: แก้ไขปัญหาวิดีโอคำถามตัวอย่างของนักพัฒนาหลุดเข้ามาแสดงผล โดยหากโหลดโจทย์ออนไลน์ไม่สำเร็จ ระบบจะสลับเข้าสู่โหมดฝึกฝนชั่วคราวอย่างปลอดภัย พร้อมป้ายแจ้งเตือนชัดเจนว่าจะไม่บันทึกความคืบหน้า

- 🎵 **แก้ไขเสียงเพลงหายหลังตอบคำถาม**: แก้ไขปัญหาเสียงเพลงแบ็กกราวนด์เงียบหลังจากการลดระดับเสียง (Audio Ducking) ในฉากคำถามหรือเมื่อเอาชนะบอส

- 🎬 **บีบอัดและปรับขนาดวิดีโอ**: ปรับลดขนาดไฟล์วิดีโอ MP4 ใน StreamingAssets ช่วยประหยัดแบนด์วิดท์และทำให้ดาวน์โหลดเข้าเกมได้รวดเร็วขึ้น

- 👾 **ระบบกู้คืนสถานะการต่อสู้**: ป้องกันข้อมูลการต่อสู้สูญหายเมื่อเน็ตหลุดหรือปิดเกมกะทันหันขณะกำลังต่อสู้

---

## 👀 ส่องอัปเดต

![Pet Gacha & Collection](resource:Announcements/Images/Pet State)

![Player Hub & Weapon State](resource:Announcements/Images/Weapon State)
