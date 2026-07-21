# {Project Name} — Project Stack & Context (For AI Assistants)

> **Copy-paste this into any AI chatbot's system prompt or first message to bootstrap context.**

## Project Identity
- **Game:** "{Project Name}" — {genre description}
- **Engine:** {engine and version}
- **Platform:** {primary}-first, {secondary} port secondary
- **Core Fantasy:** {one-sentence core fantasy}

## Tech Stack
- {Input system}
- {Camera system}
- {UI framework}
- {Rendering/lighting approach}
- {Monetization if any}
- {Language / architecture constraints}

## Core Systems (Current State)
| System | Location | State |
|--------|----------|-------|
| {System Name} | `Assets/{path}` | {Prototype / Works / Needs refactor} |
| {System Name} | `Assets/{path}` | {state} |
| {System Name} | `Assets/{path}` | {state} |

## Architecture Rules (From GDD)
1. {Rule 1 — e.g., "No jump, no vertical platforming"}
2. {Rule 2 — e.g., "Mobile-first input"}
3. {Rule 3 — e.g., "Game feel > clean code"}
4. {Rule 4 — e.g., "Don't grow the god-object"}
5. {Rule 5 — e.g., "Data-driven content"}
6. {Rule 6 — e.g., "English-only identifiers, PascalCase"}

## Key Docs
- GDD: `Docs/1_Inputs_Templates/GDD.md`
- ADRs: `Docs/3_Outputs/ADRs/` — check before making architecture decisions
- DevLog: `Docs/3_Outputs/DevLog/` — check recent entries for current state
- Rules: `Docs/0_User_Manual/RULES_AND_POLICY.md` — file org, naming, optimization, agent behavior

## Commit Convention
```
type(scope): description

Types: feat, fix, refactor, docs, style, test, chore, juice
Scopes: {your project scopes}
```

## Context Manifest
 
> For full loading rules, see `Docs/0_User_Manual/context-router.md`.
 
| File | Role | Load When |
|------|------|-----------|
| `Docs/1_Inputs_Templates/project-stack.md` | Universal bootstrap | Every agent call |
| `Docs/0_User_Manual/context-router.md` | Context loading guide | Setting up agents or unsure what to load |
| `Docs/0_User_Manual/prompt-cookbook.md` | Human quick-start recipes | Starting a task without reading full workflow docs |
| `Docs/1_Inputs_Templates/project-context-brief.md` | Bootstrap input | `/bootstrap-project` only |
| `Docs/1_Inputs_Templates/GDD.md` | Design truth (use `@tag:` sections) | When design intent is needed |
| `Docs/2_System_Files/Handoff_Contracts/README.md` | Artifact contracts | When producing or consuming artifacts |
| `Docs/0_User_Manual/RULES_AND_POLICY.md` | Conventions + safety | Reference specific §sections only |
