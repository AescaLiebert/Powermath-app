# Unity Conventions — File Organization, Hierarchy, and Optimization

> Extracted from `RULES_AND_POLICY.md` §3-§5. Reference this file for Unity-specific conventions.

---

## 3. File & Folder Organization

### Project Folder Structure (Target)
```
Assets/_Project/
  Core/                  # Bootstrap, game flow, save/profile, services
    Bootstrap/
    GameFlow/
    SaveProfile/
    Services/
  Gameplay/              # Runtime gameplay systems
    Player/
    Interaction/
    Inventory/
    {GameSpecificSystem}/
  Content/               # ScriptableObject definitions and authored data
    Items/
    Enemies/
    Levels/
    Encounters/
  UI/                    # All UI logic and prefabs
    HUD/
    Menus/
    Shop/
  Audio/
  Art/
  Tools/                 # Editor tools, debug utilities
  Tests/                 # Edit-mode and play-mode tests

Docs/                    # Outside Unity Assets, version-controlled
  0_User_Manual/         # Guides, prompt cookbook, context router, rules
  1_Inputs_Templates/    # Configuration brief, stack, GDD template
  2_System_Files/        # Agent prompts, workflows, contracts, scripts
  3_Outputs/             # Active specs, ADRs, test plans, dev logs, reports
```

### File Naming Rules
| Type | Pattern | Example |
|------|---------|---------|
| MonoBehaviour | `{SystemName}.cs` | `PlayerMovement.cs` |
| ScriptableObject | `{Name}Definition.cs` | `ItemDefinition.cs` |
| Interface | `I{Name}.cs` | `IInteractable.cs` |
| Editor Script | `{Name}Editor.cs` | `ItemDefinitionEditor.cs` |
| ADR | `{NNN}-{kebab-case-title}.md` | `001-docs-structure.md` |
| DevLog | `{YYYY-MM-DD}-{kebab-case}.md` | `2026-05-11-phase1-cleanup.md` |

---

## 4. In-Scene Hierarchy Organization

### Separator Convention
Use **empty GameObjects** as visual separators in the hierarchy. Name them with `=` padding to create clear visual sections:

```
=======System=======
  EventSystem
  AudioListener
  InputManager
=======Manager=======
  GameFlowManager
  SessionManager
  SceneLoader
=======Camera=======
  MainCamera
  CinemachineVCam
=======Lighting=======
  GlobalLight2D
  AmbientLight
=======Environment=======
  Background
  Foreground
  Tilemap_Ground
  Tilemap_Walls
=======Player=======
  Player
=======Enemies=======
  EnemySpawner
  (runtime spawned enemies)
=======UI=======
  Canvas_HUD
  Canvas_Menus
  Canvas_Overlay
=======Debug=======
  DebugConsole
  TestSpawner
```

### Separator Rules
1. **No parenting** — separators are sorting markers, not parent objects
2. **Disable all components** — separator objects should have no components (or just a disabled `MonoBehaviour` tag script)
3. **Static flag** — mark separators as `EditorOnly` or strip them in builds
4. **Order matters** — follow the convention: System → Manager → Camera → Lighting → Environment → Player → Enemies → UI → Debug
5. **Scene-specific sections** — add custom sections as needed (e.g., `=======Interactables=======`, `=======VFX=======`)

### Prefab Hierarchy
Inside prefabs, use logical grouping with actual parenting:
```
Player (root)
  ├── Model/
  │   ├── Sprite
  │   └── Animator
  ├── Collision/
  │   ├── BodyCollider
  │   └── InteractionTrigger
  ├── Audio/
  │   ├── FootstepSource
  │   └── VoiceSource
  └── FX/
      ├── DamageFlash
      └── HealParticle
```

---

## 5. Game Optimization Policy

### Performance Budgets
| Metric | Mobile Target | PC Target |
|--------|--------------|-----------|
| Frame rate | 30 FPS stable | 60 FPS stable |
| Draw calls | < 50 per frame | < 100 per frame |
| Memory | < 512 MB | < 1 GB |
| Scene load time | < 3 seconds | < 2 seconds |
| GC allocations | 0 per frame in gameplay | 0 per frame in gameplay |

### Code Optimization Rules
1. **No `Update()` polling** when events or coroutines suffice
2. **Cache component references** — never use `GetComponent<T>()` in `Update()`
3. **Use object pooling** for frequently spawned/destroyed objects (bullets, particles, enemies)
4. **Avoid string operations** in hot paths — use `StringComparison.Ordinal`, `StringBuilder`, or pre-hashed values
5. **Use `CompareTag()`** instead of `gameObject.tag ==`
6. **Minimize `Find*()` calls** — use direct references, events, or service locators
7. **Profile before optimizing** — never optimize without data from Unity Profiler

### Art & Asset Optimization Rules
1. **Texture atlasing** — combine small sprites into atlases
2. **Appropriate texture sizes** — mobile sprites rarely need > 1024x1024
3. **Audio compression** — use Vorbis for music, ADPCM for short SFX
4. **Prefab variants** — use variants instead of duplicating prefabs
5. **Addressables or AssetBundles** for large content sets that can be loaded on demand

### Scene Optimization Rules
1. **Minimize root-level objects** — use the separator convention above, but keep total root object count reasonable
2. **Use static batching** for non-moving environment art
3. **Culling** — use sorting layers and camera bounds to avoid rendering off-screen objects
4. **Additive scenes** for large game worlds — don't put everything in one scene

### Build Optimization Rules
1. **Strip unused engine modules** in Player Settings
2. **IL2CPP** for production builds (better performance than Mono)
3. **Managed code stripping** — set to Medium or High
4. **Compress build** — LZ4 for faster load, LZ4HC for smaller size
