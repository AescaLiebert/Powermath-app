# Project Context Brief

> **Instructions:** Fill in this file with your project's real details. This is the ONLY file you need to write manually.
> Then run the `/bootstrap-project` workflow — AI will use this to generate your filled `GDD.md` and `project-stack.md`.
>
> **Token optimization:** Keep answers concise. This brief should be ~500-800 tokens total. Don't write essays — write facts.

---

## 1. Identity

| Field | Your Answer |
|-------|-------------|
| Project Name | |
| Genre | |
| Core Fantasy (1 sentence) | |
| Tone keywords | |
| Primary Platform | |
| Secondary Platform | |
| Camera Style | |

## 2. Tech Stack

| Area | Value |
|------|-------|
| Engine + Version | |
| Render Pipeline | |
| 2D or 3D | |
| Input System | |
| Camera System | |
| UI Framework | |
| Networking | |

## 3. Scenes in Build

List every scene in your Build Settings, one per line:
```
- SceneName → purpose (e.g., "MainMenu → entry point, settings, play button")
- SceneName → purpose
```

## 4. Core Systems — File Map

> **This is the most important section.** List every major system, its script path, and its current state.
> AI agents use this to know **which files to read** and **which files to ignore**.

| System | Script Path | State | Notes |
|--------|------------|-------|-------|
| | `Assets/` | Prototype / Works / Needs refactor | |
| | `Assets/` | | |
| | `Assets/` | | |
| | `Assets/` | | |
| | `Assets/` | | |
| | `Assets/` | | |

## 5. Architecture Reality

### Current Pattern (be honest)
_How are things actually wired right now? Singletons? God-objects? ScriptableObjects? Events?_

```
(Describe in 2-3 sentences)
```

### Known Tech Debt
- 
- 
- 

## 6. Design Boundaries

### Experience Pillars (3-5 max)
1. **Pillar Name** — one-sentence description
2. **Pillar Name** — one-sentence description
3. **Pillar Name** — one-sentence description

### Hard Rules (things AI must never break)
- 
- 

### Architecture Rules (from your design)
1. 
2. 
3. 

## 7. Commit Scopes

List the scope names for your conventional commits (these become the vocabulary for all agents):
```
player · inventory · enemy · ui · audio · scene · build · docs · {add your own}
```

---

> **After filling this out**, run `/bootstrap-project` to generate your GDD and project-stack.
> Total expected fill time: **15-30 minutes** for a solo dev who knows their project.
