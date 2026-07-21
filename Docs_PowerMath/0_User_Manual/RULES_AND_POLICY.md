# Rules & Policy — AI-Assisted Game Development

> **Purpose:** Standardize how AI agents (IDE copilots, CLI tools, multi-agent pipelines) interact with this project. Copy into your AI tool's system prompt, `.cursorrules`, `CLAUDE.md`, or equivalent.

---

## 1. Context Policy — GDD as Single Source of Truth

### Rule
All `.md` files in this project get context **ONLY** from `1_Inputs_Templates/GDD.md` and its `@tag:` markers. AI agents must not invent context, assume features, or reference external knowledge about the game.

### Tag System
The GDD uses `@tag:section-name` markers to enable precise cross-referencing:

```markdown
<!-- @tag:core-loop -->
## Core Game Loop
...

<!-- @tag:enemies -->
## Enemies and Threats
...
```

Other documents reference GDD sections with:
```
GDD Section: @tag:core-loop
```

### Context Hierarchy
```
1_Inputs_Templates/GDD.md      ← Authoritative source of truth
  ├── 0_User_Manual/            ← Policies, cookbooks, rules
  ├── 1_Inputs_Templates/       ← Derived stack, context brief, templates
  ├── 2_System_Files/           ← Agent roles, workflows, scripts, contracts
  └── 3_Outputs/                ← Specs, ADRs, DevLogs, PM reports
```

> [!IMPORTANT]
> If a fact is not in the GDD, it is not a project fact. AI agents must say "this is not documented in the GDD" rather than guessing.

### Bootstrap Exception

`1_Inputs_Templates/project-context-brief.md` is allowed as an input only during `/bootstrap-project`.
After bootstrap, `1_Inputs_Templates/GDD.md` and `1_Inputs_Templates/project-stack.md` become the source of truth.
Do not use the brief as a parallel truth source for normal feature, bugfix, refactor, review, QA, or report work.

---

## 2. Output Rules — Code & Documentation Style

### Naming Conventions
| Element | Convention | Example |
|---------|-----------|---------|
| C# Class | PascalCase | `PlayerMovement`, `InventoryManager` |
| C# Method | PascalCase | `TakeDamage()`, `GetCurrentFloor()` |
| C# Field (private) | camelCase with `_` prefix | `_currentHealth`, `_isHidden` |
| C# Field (public/serialized) | camelCase | `moveSpeed`, `maxHealth` |
| C# Interface | `I` prefix + PascalCase | `IInteractable`, `IDamageable` |
| ScriptableObject | PascalCase + `Definition` suffix | `ItemDefinition`, `EnemyDefinition` |
| Folder | PascalCase | `Scripts/`, `Gameplay/`, `Core/` |
| Scene | PascalCase | `MainMenu.unity`, `FloorRuntime.unity` |
| Commit | Conventional commits | `feat(player): add sprint mechanic` |

### Code Response Format
When writing code, AI agents must:
1. **State which file** is being created or modified (full path)
2. **State the GDD reference** that motivates the change
3. **Separate current state from target state** — never hallucinate completed architecture
4. **Include only relevant code** — no boilerplate dumps or unrelated files
5. **Add comments only for WHY**, never for WHAT

### Documentation Response Format
- Use markdown with proper heading hierarchy
- Use tables for structured comparisons
- Use mermaid diagrams for architecture/flow visualization
- Use `> [!NOTE]`, `> [!WARNING]`, `> [!CAUTION]` for callouts
- Keep bullet points concise — one line per point

---

## 3-5. Unity Conventions

See `Docs/0_User_Manual/unity-conventions.md` for file organization (§3), in-scene hierarchy (§4), and game optimization (§5).

---

## 6. Agent Behavior Rules

### Must Do
- ✅ Always reference the GDD section that motivates a change
- ✅ Explicitly state whether code is **current implementation** or **target architecture**
- ✅ Write ADRs for any significant architecture decisions
- ✅ Write DevLog entries after each work session
- ✅ Follow existing patterns in the codebase before introducing new ones
- ✅ Keep changes small and incremental — one system per PR

### Must Not Do
- ❌ Never auto-merge to main — always require human approval
- ❌ Never skip the spec/design phase — code-first leads to architectural debt
- ❌ Never grow a god-object just because it's faster short-term
- ❌ Never hallucinate completed architecture — say what exists vs. what's planned
- ❌ Never fully automate game feel — always keep human-in-the-loop for subjective quality
- ❌ Never use one mega-prompt — specialized agents with focused system prompts outperform generalists
- ❌ Never feed the entire repo to an agent — use targeted file selection or RAG

### Error Handling
When an AI agent encounters ambiguity:
1. State the ambiguity explicitly
2. Reference what the GDD says (or that it's silent on the topic)
3. Propose 2-3 options with trade-offs
4. Wait for human decision — do not proceed with assumptions

### Logic Orchestration
For multi-agent work:
1. Start from a task card that follows `Docs/2_System_Files/Handoff_Contracts/README.md`
2. Use `Docs/2_System_Files/Agent_Prompts/orchestrator-agent.md` to route ambiguous work before selecting a workflow
3. Stop when an artifact has `status: needs-human`, `status: blocked`, or `human_checkpoint: required`
4. Treat Discord and Notion discussion as intake only until converted into a tracked repo artifact
5. Use `Docs/0_User_Manual/TEAM_SYNC_POLICY.md` before posting status to team channels or Notion

---

## 7. Safety Guardrails

### Code Safety
- All gameplay state changes must be traceable (events, logs, or state machines)
- No silent side effects — if a method changes state outside its scope, document it
- Destructive operations (delete, reset, overwrite) require explicit confirmation
- No `PlayerPrefs` for critical game state — use proper save systems

### AI Safety
- AI-generated code must be reviewed before merging
- AI must not generate content that contradicts the GDD's tone, theme, or design pillars
- AI must not introduce dependencies without documenting them in an ADR
- AI must not modify build settings, CI/CD pipelines, or deployment configs without human approval
- AI must not publish official Discord or Notion status without human approval and a link to the repo artifact or PR

---

## 8. Token Budget Policy — AI Context Loading

### Per-Agent Context Budget

| Agent Role | Max Context | Loading Strategy |
|-----------|-------------|-----------------|
| Orchestrator | ~2,000 tokens | `Docs/1_Inputs_Templates/project-stack.md` + task card/current artifact + workflow index |
| 🎮 Game Design | ~4,000 tokens | `Docs/1_Inputs_Templates/project-stack.md` + GDD `@tag:` section + agent prompt |
| 🏗️ Architect | ~6,000 tokens | `Docs/1_Inputs_Templates/project-stack.md` + GDD `@tag:` section + relevant ADRs + agent prompt |
| 💻 Implementer | ~8,000 tokens | Specs + affected source files + agent prompt |
| 👀 Reviewer | ~4,000 tokens | Git diff + agent prompt (not full files) |
| 🧪 QA | ~4,000 tokens | Design spec + implementation summary + agent prompt |
| 📋 PM Reporter | ~2,000 tokens | Git log + report template + agent prompt |

### Context Loading Rules

- Always load `project-stack.md` first (~400 tokens)
- Load task card + relevant GDD `@tag:` section only
- Load current agent prompt only, not all prompts
- Prefer references over inline content

### Per-Role Context Pack (What to Load)

| Role | Load | Skip |
|------|------|------|
| 🔀 Orchestrator | project-stack + task card + workflow index | GDD sections, source files, ADRs |
| 🎮 Game Designer | project-stack + GDD `@tag:` section + agent prompt | Source code, ADRs, other agent prompts |
| 🏗️ Architect | project-stack + GDD `@tag:` + design spec + relevant ADRs + agent prompt | Full GDD, all ADRs, source files |
| 💻 Implementer | project-stack + arch spec + design spec + affected source files + impl prompt | Full GDD, unrelated systems |
| 👀 Reviewer | project-stack + git diff + review prompt | Full files (unless diff is insufficient) |
| 🧪 QA | project-stack + design spec + impl summary + QA prompt | Source code (unless verifying behavior) |
| 📋 PM Reporter | project-stack + git log + DevLogs + report prompt | GDD, source code |

### Context Resolution Order
1. `Docs/1_Inputs_Templates/project-stack.md`
2. `Docs/3_Outputs/Specs/{slug}-task-card.md`
3. `Docs/1_Inputs_Templates/GDD.md @tag:{relevant-tag}`
4. `Docs/2_System_Files/Agent_Prompts/{role}-agent.md`
5. Affected source files, specs, or ADRs — only when needed

### Anti-Patterns

- ❌ Loading the full GDD (7,000+ tokens) when only one `@tag:` section is relevant
- ❌ Loading all agent prompts into one session (only one agent role is active at a time)
- ❌ Loading `RULES_AND_POLICY.md` in full (10,000+ tokens) — load only the relevant §sections (from `Docs/0_User_Manual/RULES_AND_POLICY.md`)
- ❌ Duplicating conventions inline instead of referencing the canonical source

---

## Quick Reference

- 📋 Before coding → Read GDD section + relevant ADRs
- 🏗️ Before deciding → Check existing ADRs, write new one if needed
- 📝 After session → Write DevLog entry
- 📊 Weekly → DevLogs feed into PM reports
- 🎮 Game feel → ALWAYS human-in-the-loop
- 🔒 Merge → ALWAYS human approval
- 💰 Token budget → Load ONLY what the current agent needs (§8)
