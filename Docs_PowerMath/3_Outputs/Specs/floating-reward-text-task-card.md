---
slug: "floating-reward-text"
status: complete
source: manual
gdd_tags:
  - "feedback"
  - "economy"
  - "combat-attempt"
owner: "antigravity"
human_checkpoint: approved
next_agent: none
blocked_by: []
---

# Task Card: Float Reward Text on UGUI Canvas with FDT Styling

## Player-Facing Goal

Every time the player receives a reward—whether Rank Currency (**Silver**, **Gold**, **Diamond**) from correct math answers or **Power Coins** from Challenge Events/settlement—a dedicated Float Reward Text pops and drifts upward on the UGUI Canvas. The text uses the bold FDT font style (`FDT-Font`), its own buoyant celebration animation, and distinct currency colors:
- **Silver:** Metallic Silver-Blue (`#C2D0E2`)
- **Gold:** Radiant Gold (`#FFC641`)
- **Diamond:** Cyan Glow (`#69DFF2`)
- **Power Coin:** Flame Orange (`#FF8C0D`)

## Source

- Origin: User design request
- Requested by: Product Owner / Lead Designer

## GDD Reference

- `@tag:feedback` - Clear visual/audio feedback for currency and reward grants.
- `@tag:economy` - Distinct identities for Silver, Gold, Diamond, and Power Coins.
- `@tag:combat-attempt` - Real-time outcome feedback upon answer resolution.

## Type

- [x] Feature

## Scope

### Systems Affected

- Combat Presentation Unity (`FloatingRewardTextStyleDefinition`, `FloatingRewardTextView`, `FloatingRewardTextPool`, `FloatingRewardTextService`, `FloatingCombatTextStyleDefinition`, `FloatingCombatTextView`, `FloatingCombatTextPool`, `FloatingCombatTextService`)
- Combat Feedback Wiring (`CombatLobbyCompositionRoot`, `CombatFeedbackPlayer`, `RunEconomyPanelController`, `RunSettlementPanelController`)
- Style Assets (`Resources/FloatingRewardTextStyle.asset`, `Resources/FloatingCombatTextStyle.asset`)
- EditMode Unit Tests (`FloatingRewardTextServiceTests`, `FloatingCombatTextServiceTests`)

### Files To Inspect/Modify

- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingRewardTextStyleDefinition.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingRewardTextView.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingRewardTextPool.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingRewardTextService.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingCombatTextStyleDefinition.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingCombatTextView.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingCombatTextPool.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/Presentation/FloatingCombatTextService.cs`
- `Assets/Project/Resources/FloatingRewardTextStyle.asset`
- `Assets/Project/Resources/FloatingCombatTextStyle.asset`
- `Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs`
- `Assets/Project/Script/Gameplay/Combat/Unity/CombatFeedbackPlayer.cs`
- `Assets/Project/Script/UI/MainMenu/RunEconomy/RunEconomyPanelController.cs`
- `Assets/Project/Script/UI/MainMenu/RunEconomy/RunSettlementPanelController.cs`
- `Assets/Project/Tests/EditMode/CombatUnity/FloatingRewardTextServiceTests.cs`
- `Assets/Project/Tests/EditMode/CombatUnity/FloatingCombatTextServiceTests.cs`

## Acceptance Criteria

- [x] `FloatingRewardTextStyleDefinition` ScriptableObject supports 4 currency colors: Silver, Gold, Diamond, and Flame Orange for Power Coin.
- [x] Uses `FDT-Font` for text styling on UGUI canvas.
- [x] Dedicated 3-phase **Pop, Drop & Fade** physical animation:
  - Phase 1: Pop Up (white impact flash + cubic launch + multi-phase squash & stretch + rotational tilt)
  - Phase 2: Gravity Drop & Landing Bounce (accelerates downward $t^2$, stretches on fall, hits cushion with horizontal slap rebound, damped rotation)
  - Phase 3: Settle & Fade Out (non-linear quadratic alpha dissolve + scale shrink)
- [x] Random spawn position scatter (`spawnJitterX`, `spawnJitterY`) prevents number stacking on overlapping events.
- [x] Both **Floating Reward Text** and **FDT (Floating Combat Damage Text)** share identical Pop-Drop-Fade juicy physics and random scatter.
- [x] Full accessibility: reduced motion clamping (0 travel, no jitter, no rotation, clean static fade).
- [x] Spawns on correct academic answer for Silver/Gold/Diamond.
- [x] Spawns on Challenge Monster / Event / settlement for Power Coins.
- [x] EditMode unit test suites (`FloatingRewardTextServiceTests` and `FloatingCombatTextServiceTests`) pass and verify juice parameters, formatting, pooling, and component frame application.
