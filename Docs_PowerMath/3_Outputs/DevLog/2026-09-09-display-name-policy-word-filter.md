# DevLog — Kid-Friendly Display Name Policy System, Global Word Filter & UI Log Notification

Date: 2026-09-09

GDD references: `@tag:player-experience`, `@tag:core-loop`.

## Summary of Changes

1. **Global Word Filter Industry Research**:
   - Researched enterprise gaming and platform standards (Microsoft Two Hat / Community Sift, Roblox Sentinel, Microsoft Azure AI Content Safety, OpenAI Moderation API, ToxMod).
   - Evaluated open-source projects including ComputerysProfanityFilter (.NET / Unity C#), LDNOOBW (Shutterstock multilingual datasets), bad-words-thai (Thai swear, karaoke, leetspeak), and censor-text.
   - Identified core defense patterns: Unicode FormC normalization, leetspeak mapping, delimiter stripping, repeat character collapse, Scunthorpe allowlisting, and COPPA PII prevention.

2. **Thai Profanity Standard Format (มาตรฐานนโยบายคำหยาบภาษาไทย)**:
   - Structured Thai filter terms across 4 official industry tiers (based on ETDA, Royal Society of Thailand, and Thai game publishers such as Garena/Playpark):
     - **Tier 1 (Sexual Anatomy & Acts)**: `หี`, `แตด`, `จิ๋ม`, `ช่องคลอด`, `ควย`, `ดอ`, `กระดอ`, `หำ`, `จู๋`, `เจี๊ยว`, `ลึงค์`, `เย็ด`, `เด้า`, `เงี่ยน`, `ร่าน`, `ชักว่าว`, `น้ำแตก`, `อมนกเขา`, `ดูดควย`, `เลียหี`, `กะหรี่`, `ซ่อง`
     - **Tier 2 (Severe Vulgarity & Curse Words)**: `เหี้ย`, `เชี่ย`, `สัส`, `สัด`, `สึด`, `สาส`, `สาซ`, `ดอกทอง`, `อีดอก`, `ชาติหมา`, `จัญไร`, `อัปปรีย์`, `ระยำ`, `สันดาน`, `หน้าด้าน`, `ตอแหล`, `บัดซบ`, `เสือก`, `ส้นตีน`, `กวนตีน`, `แม่ง`, `มึง`, `กู`, `ห่า`, `เปรต`, `ไอ้ควาย`, `อีควาย`, `พ่อมึงตาย`, `แม่มึงตาย`
     - **Tier 3 (Prefix Combinations & Evasion)**: `ไอ้...`, `อี...`, `หัว...`, `ลูก...`, `โคตร...`, `ยัด...` combined with vulgar roots.
     - **Tier 4 (Phonetics & Karaoke)**: `kuy`, `kuay`, `hee`, `heee`, `hiea`, `hia`, `chia`, `sat`, `sud`, `yed`, `ted`, `mung`, `dorkthong`, `karhee`.
   - **Thai Canonicalization Pipeline (`CanonicalizeThai`)**:
     - Strips delimiter punctuation (`.`, `_`, `-`, `/`, `\`, `*`, `~`, `|`, `^`, `+`, `@`, `#`, `$`, `!`, `:`, `;`, `,`) and all whitespace so evasions like `"ควย / หี"`, `"ค ว ย"`, `"ค-ว-ย"`, `"ค.ว.ย"`, `"ห ี"`, `"ห/ี"`, `"สั ส"`, `"ไอ้ เหี้ย"` collapse directly into root profanities.
     - Collapses consecutive repeating characters (`สัสสส` -> `สัส`).
   - **Scunthorpe Whitelist for Thai**:
     - Protects innocent compound words like `"หีบ"` (`"หีบสมบัติ"`, `"หีบเพลง"`), `"กูเกิล"`, `"กูรู"`, `"หอยทาก"`, `"ตีนเขา"`, `"ผู้กล้า"`.

3. **DisplayNamePolicy Engine (`PowerMath.Session`)**:
   - Created and upgraded `DisplayNamePolicy.cs`:
     - **Length Constraint**: Strictly checks max 20 characters (`StringInfo.LengthInTextElements`) ensuring proper counting of Thai multi-codepoint vowel and tone mark clusters.
     - **Character Whitelist/Sanitization**: Blocks control characters and HTML markup (`<`, `>`).
     - **Contact / PII Detection**: Blocks phone numbers (Thai 08x/09x/06x and international patterns) and URLs/Discord tags to protect young students.
     - **Scunthorpe Problem Protection**: Curated allowlist ensuring harmless names are never falsely blocked.
     - **Validation Result**: Returns `DisplayNameValidationResult` with explicit `DisplayNameDenialReason` and localization key.

4. **Player Preparation UI & Global Logging Integration**:
   - Updated `PlayerPreparationPresenter.cs`:
     - Proactive validation in `OnNameChanged`: displays inline error warning immediately when inappropriate words are entered.
     - In `OnNameConfirmed()`: Triggers `StatusMessageService.ShowWarning(message)`, surfacing the animated on-screen toast (`StatusToastOverlay`), broadcasting `StatusMessageService.MessagePublished`, and logging to `AppLog.Warning("DisplayNamePolicy", ...)` and `AppLog.Warning("StatusUI", ...)`.
   - Updated `PlayerPreparationView.cs`:
     - Registered `KeyDownEvent` on `_nameField` to trigger `OnNameConfirmed()` on Enter/Return key.

5. **Automated Testing**:
   - Updated `DisplayNamePolicyTests.cs`:
     - Tested direct words: `ควย`, `หี`, `สัส`, `ไอ้เหี้ย`, `เชี่ย`, `เย็ด`, `ดอกทอง`, `ชาติหมา`
     - Tested evasions: `ควย / หี`, `ค ว ย`, `ค-ว-ย`, `ค.ว.ย`, `ห ี`, `ห/ี`, `ไอ่เหี้ย`, `ไอ้ เหี้ย`, `สั ส`, `สัสสส`, `kuy`, `hiea`, `hee`
     - Tested Scunthorpe safe words: `หีบ`, `หีบสมบัติ`, `กูเกิล`, `ผู้กล้า`, `Pass`, `Classic`, `Assistant`, `Butter`, `Grass`, `Titan`, `Hello`, `Ricko`, `Stellar`
   - Verified zero compilation errors in Unity.
