---
slug: pet-gacha-system
status: draft
source: manual
gdd_tags:
  - economy
  - gacha
  - server-authority
  - feedback
owner: qa-agent
human_checkpoint: required
next_agent: human-tester
blocked_by:
  - production-pet-catalog
---

# Pet Gacha System Test Plan

## 1. Summary

| Test area | Priority | Type | Platform |
| --- | --- | --- | --- |
| Probability conservation and redistribution | P0 | Automated | Pure EditMode |
| Spend, ownership, duplicate, and receipt idempotency | P0 | Automated + manual Firebase | Unity Editor/WebGL |
| Refresh/reconnect recovery | P0 | Manual network fault injection | WebGL |
| Probability and duplicate disclosure | P1 | UI automated + manual | WebGL/mobile viewport |
| New/duplicate feedback and Reduced Motion | P1 | Manual | WebGL/mobile viewport |
| Economy and profile regressions | P1 | Automated + manual | Unity Editor/WebGL |

GDD references: `@tag:economy`, `@tag:gacha`, `@tag:server-authority`, and `@tag:feedback`. ADR reference: ADR-010.

## 2. Functional Tests

### P0 - Exact probability model

**GIVEN** a rarity total `R`, `N` pets, and no owned pets  
**WHEN** probabilities are calculated  
**THEN** every pet receives `R/N` and the rarity subtotal remains exactly `R`.

**GIVEN** the GDD example of five 3%-rarity pets with one owned  
**WHEN** probabilities are calculated  
**THEN** the owned pet is `0.3%`, each unowned pet is `0.675%`, and the subtotal is exactly `3%`.

**GIVEN** every pet in a rarity is owned  
**WHEN** probabilities are calculated  
**THEN** equal original odds return and every result is eligible as an empty duplicate.

**GIVEN** ownership changes in one rarity  
**WHEN** probabilities are recalculated  
**THEN** all other rarity totals and individual distributions remain unchanged.

### P0 - Atomic pull

**GIVEN** exactly 25 Power Coins, a valid preview revision, and no unresolved attempt  
**WHEN** the player confirms  
**THEN** the result saves once and the accepted balance becomes zero.

**GIVEN** fewer than 25 Power Coins  
**WHEN** the gacha panel opens or a stale request reaches persistence  
**THEN** confirmation is unavailable, no spend occurs, and the exact shortfall is shown.

**GIVEN** an unowned rolled pet  
**WHEN** the PATCH is accepted  
**THEN** exactly one `{itemId, owned:true, upgradeLevel:0}` entry is appended and the receipt says `wasNew=true`.

**GIVEN** an owned rolled pet  
**WHEN** the PATCH is accepted  
**THEN** 25 Coins are spent, inventory remains byte-for-byte equivalent, and no compensation or level change is saved.

### P0 - Idempotency and recovery

**GIVEN** an accepted pull receipt  
**WHEN** the same transaction ID is submitted again  
**THEN** the saved pet, new/duplicate flag, cost, and resulting balance are returned without a roll or write.

**GIVEN** a network interruption after confirmation  
**WHEN** the client retries recovery  
**THEN** it retains the transaction ID and either returns the saved receipt or commits one result; it never exposes two results.

**GIVEN** another client changes revision after preview  
**WHEN** confirmation refreshes saved state  
**THEN** no spend/roll occurs, odds and balance refresh, and the player must reconfirm.

### P1 - Player-facing disclosure

**GIVEN** a valid catalog and mixed ownership  
**WHEN** the panel opens  
**THEN** it shows current/after balance, fixed cost, every pet's current chance, rarity subtotals, owned labels, and the empty-duplicate warning before `Pull`.

**GIVEN** the confirmation overlay  
**WHEN** it appears  
**THEN** it repeats the exact cost, projected balance, and no-compensation duplicate rule; Cancel spends nothing.

**GIVEN** a saved new or duplicate result  
**WHEN** reveal completes  
**THEN** icon, rarity, pet name, balance, and explicit `NEW` or `DUPLICATE - NO PET CHANGES` text remain readable for at least 600 ms.

## 3. Edge and Abuse Cases

- [ ] Spam Pull/Confirm/Cancel/Continue and verify only one coroutine/transaction ID becomes active.
- [ ] Close, refresh, navigate back, and terminate the browser before confirm, during GET, during PATCH, and after PATCH.
- [ ] Test empty inventory, one owned pet, one unowned pet, and every rarity complete.
- [ ] Test duplicate, unowned, `owned=false`, negative level, duplicate ID, and removed-catalog pet records.
- [ ] Test missing/invalid catalog, missing icon, duplicate catalog IDs, empty rarity, zero rate, and totals below/above 10,000.
- [ ] Test Power Coin balances `0`, `24`, `25`, `26`, and supported maximum values.
- [ ] Test revision near overflow and malformed negative saved currency; mutations must fail closed.
- [ ] Test active question and `RunDefeat`; gacha must remain unavailable.
- [ ] Test `RunComplete`; gacha remains available when no attempt is unresolved.
- [ ] Test public projection failure after a private accepted pull; ownership/balance stay accepted.

## 4. Performance

- [ ] Opening/rebuilding the odds list produces no ongoing `Update()` work.
- [ ] Profile the largest approved catalog; panel open and refresh must not cause visible input hitch on the mobile target.
- [ ] Maintain the project target of stable 30 FPS mobile and 60 FPS PC during reveal.
- [ ] Verify repeated panel opens release dynamically-created UI rows when cleared and do not grow retained memory.
- [ ] Confirm pet icons follow the mobile texture guidance and are atlas-ready.

## 5. Platform and Accessibility

| Test | Unity WebGL desktop | Mobile-compatible viewport |
| --- | --- | --- |
| Open/close/confirm | Pointer clicks acknowledge immediately | Touch targets remain readable and do not overlap |
| Odds list | Mouse wheel/drag scroll works | Touch drag scroll does not trigger Pull |
| Reduced Motion | Static/opacity result remains clear | Same result text and hold, no scale dependency |
| Muted audio | All outcomes remain unambiguous | All outcomes remain unambiguous |
| Low frame rate | Saved text state resolves independent of animation | Same; input remains locked during commitment |

## 6. Regression Checklist

- [ ] Weapon Ascend still spends the exact authored cost and receives session updates.
- [ ] Run settlement still grants/preserves inventory and Power Coins correctly.
- [ ] Combat composition still reports Pet ATK as unconfigured and does not apply newly owned pets.
- [ ] `loadout.petId` remains unchanged after pulling.
- [ ] Leaderboard/profile projection does not expose private gacha receipts.
- [ ] Login/default repair preserves existing wallet and inventory while adding only missing receipt leaves.
- [ ] Main Menu profile, leaderboard, Player Hub, Rebirth, and combat overlays retain initial visibility and input.

## 7. Prototype Authority Warning

ADR-010 accepts client-side cryptographic RNG for the direct-Firestore prototype. Before production launch or consequential purchases/rewards, repeat all P0 tests against the required trusted backend and add modified-client tamper tests. Passing this plan does not turn the prototype into production-grade server authority.

## 8. Verification Completed This Session

- Core, Unity catalog/RNG, Assembly-CSharp integration, and EditMode test assemblies compiled with Unity's Roslyn inputs.
- A standalone core harness passed exact GDD redistribution, 100% total conservation, rarity-complete restoration, new/duplicate selection, exact 25-Coin spend, and insufficient-funds rejection.
- `MainMenuUI.uxml` parsed as XML and all named UI elements were unique.
- Full Unity Test Runner execution was attempted but the batch Editor stalled during asset import/Package Manager activity while other Unity 6000.3 Editor instances were active; no results XML was produced. Run the listed Unity tests from the project Editor before PR approval.

## 9. Bug Report Template

```text
BUG: Pet Gacha - {short title}
SEVERITY: Critical / Major / Minor / Cosmetic
CATALOG VERSION: {value}
TRANSACTION ID: {redacted diagnostic value}
PREVIEW REVISION: {value}
REPRO STEPS:
  1. {step}
  2. {step}
  3. {step}
EXPECTED: {expected result}
ACTUAL: {actual result}
PLATFORM: {WebGL browser/device or Editor}
FREQUENCY: Always / Sometimes / Rare
SCREENSHOT/VIDEO: {link}
```
