---
slug: display-name-policy-word-filter
status: approved
source: manual
gdd_tags:
  - player-experience
  - core-loop
owner: Antigravity
human_checkpoint: required
next_agent: human
blocked_by: []
---

# Task Card: Kid-Friendly Display Name Policy System & Global Word Filter

## Player-Facing Goal

When a student creates or updates their character's display name (up to 20 characters) in the preparation/onboarding screen:
1. The system validates the name against strict kid-friendly appropriateness standards (blocking profanity, vulgarity, toxicity, sexual/nudity references, cross-language transliterations, innuendo compounds like "YesMOM", and contact/PII leaks like @handles, emails, and phone numbers).
2. The name input field automatically pre-fills with the student's existing account username from userdata, saving them the effort of coming up with a name while allowing customization.
3. If the name violates the policy, the operation is denied and an immediate global UI log message notification (via StatusMessageService & AppLog) alerts the player with a clear, child-friendly localized explanation (in Thai and English), preventing inappropriate names from ever reaching leaderboards or multiplayer records.

## Source

- Origin: Manual user request (2026-09-09 & 2026-09-10)
- Requested by: User
- Requirement: 
  - Display Name: max 20 characters (string:20).
  - Major appropriateness issues: bad words / swear / toxic / nudity.
  - Industry research: Find global word filter projects standard in the industry.
  - Global UI log message: If display name is denied, notify via the global UI log system.
  - Auto pre-fill username from userdata.
  - Cross-language: Thai in EN (Hum, Hee, Sus, Kuy) and EN in Thai (ฟัค = fuck, ชิท, บิทช์, พอร์น).
  - Strict compound phrases / innuendos (YesMOM, YesMae, เยสมัม, YedMom, EatShit).
  - Roblox-grade privacy: No @gmail, @twitter, third-party social handles (@), platform prefixes (ig:, fb:, line:, dc:), links, or phone numbers.

## GDD Reference

- @tag:core-loop — Student onboarding and preparation sequence before entering Lobby and Leaderboard.
- Safe Kid-Friendly Educational Game Guidelines (COPPA compliance, Roblox child safety standards).

## Type

- [x] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] Code review
- [ ] Report / PM update
- [ ] Tooling / CI

## Scope

### Systems Affected

- PowerMath.Session (PlayerLifecyclePolicy, DisplayNamePolicy)
- PowerMath.PlayerLifecycle (PlayerPreparationPresenter, PlayerPreparationView)
- PowerMath.UI.Core (StatusMessageService, StatusToastOverlay)
- PowerMath.Diagnostics (AppLog)
- Assets/Project/Resources/Localization/UI.json
- Assets/Project/Tests/EditMode/Editor/PlayerLifecycleTests.cs
- Assets/Project/Tests/EditMode/Editor/DisplayNamePolicyTests.cs

### Files Modified / Created

- `Assets/Project/Script/Session/DisplayNamePolicy.cs` [NEW]
- `Assets/Project/Script/Session/PlayerLifecycleCommands.cs` [MODIFY]
- `Assets/Project/Script/PlayerLifecycle/PlayerPreparationPresenter.cs` [MODIFY]
- `Assets/Project/Resources/Localization/UI.json` [MODIFY]
- `Assets/Project/Tests/EditMode/Editor/DisplayNamePolicyTests.cs` [NEW]
- `Assets/Project/Tests/EditMode/Editor/PlayerLifecycleTests.cs` [MODIFY]
- `Docs_PowerMath/3_Outputs/DevLog/2026-09-09-display-name-policy-word-filter.md` [NEW]

### Out of Scope

- Real-time chat messaging filter (display name only).
- Paid SaaS moderation API subscriptions requiring monthly per-seat or per-request costs.

## Acceptance Criteria

- [x] Global Word Filter Industry Analysis documented with enterprise and open-source benchmarks.
- [x] Display name length strictly enforced up to 20 text elements (StringInfo Thai/Grapheme compatible).
- [x] Comprehensive kid-friendly filter blocking English and Thai profanity, toxicity, vulgarity, and sexual/nudity content.
- [x] Leetspeak / homoglyph and delimiter evasion detection (f.u.c.k, @ss, 5h1t, repeated letters).
- [x] Scunthorpe problem prevention (allowing harmless words containing innocent substrings like Pass, Classic, Assistant, Hero, Ricko, Stellar, Humphrey, Sustain, ผู้กล้า, หีบ, หีบสมบัติ, กูเกิล, ฟักทอง, คัมภีร์, ดิกชันนารี, ค็อกเทล).
- [x] QoL Username Pre-Fill: Automatically populates student's username from `PlayerSnapshot` in onboarding name entry.
- [x] Cross-Language Romanized/Karaoke Thai Vulgarity Filtering: Blocks `Hum`, `Hee`, `Sus`, `Kuy`, `Kuay`, `Yed`, `Hia`, `Dokthong`, `Meung` with smart letter boundary checks.
- [x] English Vulgarities written in Thai script: Blocks `ฟัค`, `ฟักยู`, `ฟัคยู`, `ชิท`, `บิทช์`, `พอร์น`, `เซ็กส์`, `นู้ด`, `ดิก`, `ค็อก`.
- [x] Strict Compound Innuendo & Phrase Detection: Blocks `YesMOM`, `YesMae`, `เยสมัม`, `YedMom`, `EatShit`, `SuckDick`, `พ่อมึงตาย`.
- [x] Roblox-Grade Privacy & COPPA PII Protection: Blocks `@` handles, emails (`@gmail`, `@twitter`), social prefixes (`ig:`, `fb:`, `line:`, `dc:`), Discord tags, URLs, and phone numbers under `DisplayNameDenialReason.ContactInformation`.
- [x] Global UI notification (StatusMessageService.ShowWarning / StatusMessageService.ShowError) triggered when a name is denied, accompanied by AppLog logging.
- [x] Localized player feedback for Thai (th) and English (en).
- [x] Variable Style Toxic Words & Shortened Abbreviations: Blocks missing-letter variations (`fuc`, `fck`, `fuk`, `dic`, `dik`, `suc`, `sux`, `bch`, `cnt`, `stfu`, `gtfo`, `wtf`, `kys`) with letter-boundary checks for ambiguous terms.
- [x] Special Symbol Leetspeak: Maps symbol homoglyphs (`A$$`, `a$s`, `a$$hole`, `b!tch`, `d!ck`, `fu¢k`, `5h!t`, `p0rn`, `§`, `$`, `¢`, `€`, `|`).
- [x] Hate Speech, Violence, Threats & Assault (EN & TH): Blocks death threats, suicide encouragement, sexual violence, racial/ableist slurs (`kill yourself`, `go die`, `suicide`, `rape`, `molest`, `nigger`, `faggot`, `retard`, `ไปตายซะ`, `กูจะฆ่ามึง`, `ฆ่าตัวตาย`, `ข่มขืน`, `รุมโทรม`, `ปัญญาอ่อน`, `ไอ้เอ๋อ`, `เศษสวะ`).
- [x] Thai Gaming Slang & Abbreviations: Blocks `คย` (ค.ย., ค-ย), `เห้` (ไอ้เห้), `พมต`, `มมต`, `พ่องตาย`, `แม่งตาย`, `หาพ่อง` while preserving harmless Thai words (`เทคนิค`, `ภาคยานุวัติ`).
- [x] Expanded Scunthorpe Allowlist: Preserves legitimate names containing sensitive stems (`Grape`, `Grapes`, `Success`, `Dictionary`, `Fuchsia`, `Asset`, `Assemble`, `เทคนิค`, `ผู้ฆ่ามังกร`, `ปัญญา`).
- [x] Comprehensive suite of EditMode automated unit tests (174 tests in `DisplayNamePolicyTests`, 16 tests in `PlayerLifecycleTests` — 100% passing).

## Human Checkpoints

- [x] Design/game-feel approval
- [x] Architecture approval
- [ ] PR review before merge
- [ ] Team status publish approval

## Router Decision

Recommended workflow: /implement-feature
Status: Implementation completed, verified (174 tests passed), and ready for review.
