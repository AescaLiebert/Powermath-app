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

## Session 2: Roblox-Grade Privacy Protection, Cross-Language Innuendos & QoL Username Pre-Fill

Date: 2026-09-10

### Summary of Enhancements

1. **QoL Username Pre-Fill in Onboarding (`PlayerPreparationPresenter.cs`)**:
   - `ShowNameEntry()` now auto-populates `_name` from existing student userdata via `ResolveDefaultDisplayName()`:
     - Priority 1: Existing `profile.displayName`.
     - Priority 2: Account username extracted from `playerId` (`{levelId}:{username}`) via `AdminAccountAccessPolicy.TryGetUsername`.
     - Priority 3: Fallback to raw `playerId`.
   - Clamped to `DisplayNamePolicy.MaximumDisplayNameLength` (20 text elements).
   - Students no longer have to think of or type a name if they want to proceed with their account name, but can still customize it freely.

2. **Cross-Language Phonetic & Transliteration Filtering**:
   - **Thai Vulgarities in Romanized/English Script (Karaoke)**:
     - Explicit coverage and letter-boundary matching for `Hum`, `Ham`, `Hee`, `Sus`, `Suss`, `Kuy`, `Kuay`, `Yed`, `Hia`, `Dokthong`, `Meung`, `Goo`, `Garee`, `Ted`, `Jim`.
     - Smart word/letter boundary check (`!char.IsLetter`) ensures names like `Humphrey`, `Sustain`, `Classic`, `Played` remain completely permitted while `Hum99`, `Sus123`, `KuyZa`, `Hee555` are blocked.
   - **English Vulgarities written in Thai Phonetic Script (คำหยาบอังกฤษเขียนไทย)**:
     - Explicit coverage for `ฟัค`, `ฟักยู`, `ฟัคยู`, `ฟักกิ้ง`, `ชิท`, `บิทช์`, `พอร์น`, `เซ็กส์`, `นู้ด`, `ดิก`, `ค็อก`, `พุสซี่`, `แอสโฮล`, `เควายเอส`, `ดิลโด้`, `คัม`, `ฮอร์นี่`, `สลัท`, `เรป`.
     - Scunthorpe allowlisting prevents false positives on innocent Thai words (`ฟักทอง`, `ต้มฟัก`, `แกงฟัก`, `ฟักเขียว`, `ฟักไข่`, `คัมภีร์`, `ดิกชันนารี`, `ค็อกเทล`).

3. **Compound Phrase & Phonetic Innuendo Detection (e.g. "YesMOM")**:
   - Strict multi-word scanner detecting Thai-English phonetic puns and compound insults:
     - `"YesMom"`, `"Yes-Mom"`, `"Yes_Mom"`, `"yes.mom"`, `"YesMae"`, `"YesMother"`, `"YesPor"`, `"YesPed"`, `"YesKae"`.
     - Thai equivalents: `"เยสมัม"`, `"เยส-มัม"`, `"เยสแม่"`, `"เยสพ่อ"`, `"เยสเป็ด"`, `"เยสเข้"`.
     - English and Karaoke compounds: `"YedMom"`, `"FuckMom"`, `"EatShit"`, `"SuckDick"`, `"พ่อมึงตาย"`, `"แม่มึงตาย"`.
   - Checked against raw stripped delimiters, collapsed repeats, and leetspeak-normalized strings.

4. **Roblox-Grade Privacy & COPPA PII Protection**:
   - Upgraded `ContactLinkRegex` and added `SocialHandleRegex`:
     - Blocks all `@` mentions and handles (`@gmail`, `@twitter`, `@instagram`, `@tiktok`, `@somchai`).
     - Blocks social platform prefixes (`ig:`, `fb:`, `line:`, `dc:`, `tt:`, `yt:`, `discord:`).
     - Blocks Discord user tags (`User#1234`) and server invite links.
     - Blocks URLs and common domain TLDs (`.com`, `.net`, `.org`, `.xyz`, `.th`, `.gg`, etc.).
     - Blocks Thai phone numbers (`081-234-5678`, `0812345678`) and international numbers.
   - Updated localization in `Assets/Project/Resources/Localization/UI.json` (`onboarding.nameContactInfo`) for Thai and English.

5. **Test Results**:
   - Ran `DisplayNamePolicyTests` via Unity MCP test runner in EditMode: **114 / 114 tests passed (0 failures)**.
   - Ran `PlayerLifecycleTests` via Unity MCP test runner in EditMode: **16 / 16 tests passed (0 failures)**.

## Session 3: Variable-Style Toxic Words, Missing Letters, Leetspeak Symbols & Hate/Threats/Assault (TH & EN)

Date: 2026-09-11

### Summary of Enhancements

1. **Shortened Toxic Variations & Missing-Letter Words**:
   - Covered missing-letter and slang toxic words in English:
     - `fuc`, `fck`, `fuk`, `dic`, `dik`, `suc`, `sux`, `bch`, `cnt`, `stfu`, `gtfo`, `wtf`, `kys`.
   - Differentiated unambiguous acronyms/abbreviations (`fck`, `cnt`, `bch`, `kys`, `stfu`, `gtfo`, `kuy`, `kuay`) which are checked unconditionally across substrings, from short ambiguous stems (`fuc`, `suc`, `dic`, `dik`, `ass`, `azz`) which use strict letter boundary checks (`!char.IsLetter(before)` and `!char.IsLetter(after)`) so normal English words (`fuchsia`, `success`, `dictionary`, `classic`, `assistant`) are never falsely denied.

2. **Special Symbol Leetspeak (`A$$`)**:
   - Expanded homoglyph canonicalization mapping:
     - `$` and `§` -> `s`
     - `¢` and `©` -> `c`
     - `€` -> `e`
     - `|` -> `i`
     - In addition to standard `@` -> `a`, `0` -> `o`, `1` -> `i`, `3` -> `e`, `5` -> `s`, `7` -> `t`.
   - Now catches evasions like `A$$`, `a$s`, `a$$hole`, `b!tch`, `d!ck`, `fu¢k`, `5h!t`, `p0rn` across both delimiter-stripped and symbol-mapped representations.

3. **Hate Speech, Threats, Violence & Assault (EN & TH)**:
   - **English Prohibited Terms**:
     - Threats & Self-harm: `kill yourself`, `go die`, `suicide`, `kys`.
     - Sexual Violence: `rape`, `molest`.
     - Slurs & Hate Speech: `nigger`, `faggot`, `retard`.
   - **Thai Gaming Slang & Threats**:
     - Shortened Gaming Slang: `คย` (ค.ย., ค-ย, ค_ย), `เห้` (ไอ้เห้), `พมต` (พ่อมึงตาย), `มมต` (แม่มึงตาย), `พ่องตาย`, `แม่งตาย`, `หาพ่อง`.
     - Death Threats & Violence: `ไปตายซะ`, `กูจะฆ่ามึง`, `ฆ่าตัวตาย`.
     - Sexual Assault: `ข่มขืน`, `รุมโทรม`, `ลวนลามเด็ก`.
     - Ableist Slurs / Toxic Insults: `ปัญญาอ่อน`, `ไอ้เอ๋อ`, `เศษสวะ`.

4. **Scunthorpe Problem Safeguards**:
   - Expanded allowlist with legitimate words containing triggers:
     - `Grape`, `Grapes`, `Grapefruit`, `Drape`, `Scrape` (contains `rape`).
     - `Success`, `Succinct`, `Succulent` (contains `suc`).
     - `Dictionary`, `Predict`, `Addict` (contains `dic`).
     - `Fuchsia` (contains `fuc`).
     - `Asset`, `Assemble` (contains `ass`).
     - `เทคนิค`, `ภาคยานุวัติ` (contains Thai letters `คย`).
     - `ผู้ฆ่ามังกร` (protects `ฆ่า` when part of benign fantasy titles, while banning `กูจะฆ่ามึง`).
     - `ปัญญา` (protects wisdom in Thai, while banning `ปัญญาอ่อน`).

5. **Test Results**:
   - Ran `DisplayNamePolicyTests` via Unity MCP test runner in EditMode: **174 / 174 tests passed (0 failures)**.
   - Ran `PlayerLifecycleTests` via Unity MCP test runner in EditMode: **16 / 16 tests passed (0 failures)**.
   - Total test suite coverage across all display name validation scenarios: **190 tests passed, 0 failures, 0 warnings**.

