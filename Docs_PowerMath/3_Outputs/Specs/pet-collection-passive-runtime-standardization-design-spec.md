---
slug: pet-collection-passive-runtime-standardization
status: approved
source: manual
gdd_tags:
  - combat-stats
  - economy
  - pet-system
  - gacha
  - server-authority
  - feedback
owner: game-design-agent
human_checkpoint: required
next_agent: implementation-agent
blocked_by:
  - final-pet-follower-animation-brief
---

# Design Spec: Pet Collection and Passive Runtime Standardization

## 1. Player Goal and Experience

The player spends 180 PC knowing that every result is permanent growth. A duplicate never creates a second inventory row: the existing card counts up from `x1` to `x2`, its total contribution updates, and the reveal communicates a positive collection gain. The player may equip any owned pet for appearance without changing account power.

During combat, the equipped cosmetic follower idles beside the player. When an unlocked collection passive produces a Follow-Up or Counter-Attack, the visible follower represents the collection action: it leaves idle, charges toward the current enemy, resolves one authoritative damage event, then returns. The visible pet is an avatar for the account-wide passive and is not the source of eligibility.

Final animation timing, trajectory, art, audio, VFX, and heart feedback remain intentionally unspecified until the owner supplies the promised visual brief.

## 2. System Rules

### Collection and Equip

- A valid pull spends exactly 180 PC.
- Ownership is a positive integer copy count keyed by stable pet ID.
- Every authored per-copy stat stacks by count using checked arithmetic and a data-defined clamp.
- The inventory renders one card per pet ID with `xN`; it never renders duplicate rows.
- Equip accepts only an owned, catalog-valid pet ID and changes presentation only.
- Corrupt, negative, zero, unknown, case-conflicting, or duplicate canonical records fail closed and enter migration/repair; they never silently grant one copy.

### Passive Stack Rule

- Each SSR passive definition includes an explicit stack rule.
- Shipping rule: `UniquePerDefinition`. One or more copies activate one passive instance; extra copies still stack the pet's authored Main Stat.
- `PerCopy` and `CappedCopies` may exist as future schema values but are rejected by production catalog validation until separately approved.
- Multiple different SSR passives responding to one trigger combine same-family numeric results into one readable sequence, as required by the GDD.

### Trigger and Resolution Order

```text
Authoritative gameplay event
  -> verify owned collection and catalog version
  -> match generic passive trigger/filter
  -> advance scoped passive progress
  -> produce deterministic effect command(s)
  -> apply combat/economy state once
  -> persist state + idempotency receipt atomically
  -> emit semantic presentation event
  -> presentation animates equipped cosmetic follower
```

Pet actions never trigger other pet actions. Power Rescue resolves before a queued enemy attack; an eligible pet Counter-Attack resolves after actual heart loss and only if the player remains alive.

### State Machine

| State | Entry | Exit | Interruptibility | Next |
| --- | --- | --- | --- | --- |
| `Idle` | Valid equipped pet is visible and no pet action is pending | Authoritative pet action receipt appears | Cosmetic equip change may replace the view | `Windup` |
| `Windup` | Receipt identifies Follow-Up/Counter-Attack and target | Brief-approved telegraph completes | Reduced Motion may collapse movement, not resolution | `Strike` |
| `Strike` | Target is still valid or receipt contains resolved target outcome | One damage/heart/effect presentation completes | Cannot change gameplay result | `Return` |
| `Return` | Strike feedback completes | Pet reaches idle anchor or reduced-motion snap completes | Cosmetic equip change may replace the returning sprite | `Idle` |

If the player attack kills the current enemy, pending Follow-Up value is saved and targets the next valid combat enemy. Reconnect restores from the authoritative receipt/state and must not apply damage twice.

## 3. Formula Contract

The architecture must centralize these operations; presentation code may only display the result.

```text
StackedModifier(stat) = Clamp_stat(Sum(PerCopyValue × EffectiveCopyCount))

CollectionPlayerFlatATK = StackedModifier(PlayerFlatAttack)
CollectionPlayerATKPercent = StackedModifier(PlayerAttackPercent)
CollectionPetFlatATK = StackedModifier(PetFlatAttack)
CollectionPetATKPercent = StackedModifier(PetAttackPercent)

EffectivePetATK = RoundAwayFromZero(
    CollectionPetFlatATK × (1 + CollectionPetATKPercent / 100)
)
```

The exact placement of `CollectionPlayerATKPercent` relative to Legacy ATK must be reconciled with the GDD before implementation; current code multiplies `(WeaponATK + CollectionPlayerFlatATK)` by both collection percent and Legacy percent, while the canonical GDD formula names only the Legacy multiplier.

`PlayerHeartUnit` increases maximum hearts per copy. Current hearts clamp to the new maximum. Gain/loss must emit reason-coded semantic events so later animation is presentation-only.

## 4. Content Conflict Requiring Approval

The GDD currently says every copy has `MainStatATK` and that encounter/reward multipliers come only from SSR passives. The supplied CSV instead assigns these main stats:

| Pet examples | CSV stat | Conflict |
| --- | --- | --- |
| Capybara | Encounter Luck +25% | Non-SSR behavioral modifier |
| Mizu / Trippi Troppi | Power Coin Bonus +1% / +5% | Non-SSR run-reward modifier |
| Cozy / Sapphire | Pet ATK% / Pet ATK | Not universal Player ATK |
| Lumirin | Player Heart Unit +1 | Survivability main stat |

Recommended decision: rename the GDD concept from `MainStatATK` to `MainStatEffect`, approve the CSV as the current content source, and retain the generic formula/clamp system. This preserves the owner's explicit “all pet stat and passive are stacking” direction while keeping equip cosmetic.

## 5. Five-Component Evaluation

| Component | Requirement |
| --- | --- |
| Clarity | Inventory shows one tile, copy count, per-copy value, total contribution, active passive, and stack rule. Combat identifies Follow-Up/Counter-Attack source and result. |
| Motivation | Every pull visibly increases a persistent collection value; duplicates use positive growth language. |
| Response | Pull/equip input acknowledges immediately but reveals power only after an authoritative receipt. Pet animation never delays or changes accepted state. |
| Satisfaction | Copy count and stat total count up together; pet actions later use distinct motion plus audio; heart changes later use visual plus audio. |
| Fit | Favorite pet remains cosmetic while the collection acts as one supportive account-wide team. |

## 6. Risks and Abuse Cases

- Reusing a transaction ID with different command content.
- Refresh after the server commits but before the reveal.
- Forged count, wallet, catalog version, passive key, or progress sent by a modified client.
- Duplicate legacy inventory rows or pet IDs differing only by case.
- Unbounded pull count or integer overflow.
- Catalog hot-change between preview and commit.
- Follow-Up carried across a killed target, reconnect, Event boundary, death, or run settlement.
- Counter-Attack triggered by shielded/prevented damage or after death.
- Passive recursion from pet damage.
- Cosmetic pet missing while account-wide passive remains valid.

## 7. Playtest Scenarios

- New player: predict cost, copy-count result, total stat change, and whether equip changes power.
- Stress: double tap pull/equip, reconnect at every receipt boundary, and spam panel open/close.
- Skill: observe correct-answer player damage separately from pet Follow-Up damage.
- Abuse: modify client-side count/passive/pull-count payloads and verify the trusted command rejects them.
- Readability: in at least 8 of 10 observations, a child identifies whether the player, pet, enemy, or Power caused the last damage/heart change.

## 8. Tuning Priority

No new timing or magnitude is proposed. All current numeric values come from the GDD or supplied CSV. After the animation brief, tune in this order: input response, action source clarity, impact readability, fantasy fit, then pacing.
