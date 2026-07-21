# {Project Name} - Project Documentation Hub

All documentation lives here, outside Unity Assets, version-controlled alongside the codebase.

## Folder Structure

| Folder | Purpose | Key Files |
|--------|---------|----------|
| `0_User_Manual/` | Policies, rules, cookbook | `RULES_AND_POLICY.md`, `context-router.md`, `prompt-cookbook.md` |
| `1_Inputs_Templates/` | Configure design and inputs | `project-context-brief.md`, `GDD.md`, `project-stack.md`, templates |
| `2_System_Files/` | Agent prompts, workflows, contracts | `Agent_Prompts/`, `Workflows/`, `Handoff_Contracts/` |
| `3_Outputs/` | Generated development artifacts | `Specs/`, `TestPlans/`, `ADRs/`, `DevLog/`, `PM_Reports/` |
| `.github/` | GitHub templates and actions | `COMMIT_CONVENTION.md`, `PULL_REQUEST_TEMPLATE.md` |

## Quick Start

1. Bootstrap: fill `1_Inputs_Templates/project-context-brief.md`, run `/bootstrap-project`
2. Start tasks from `3_Outputs/Specs/{slug}-task-card.md` using `1_Inputs_Templates/Task_Card_Template.md`
3. Route with `2_System_Files/Agent_Prompts/orchestrator-agent.md`
4. Run workflow: `/implement-feature`, `/fix-bug`, `/refactor`, `/code-review`, or `/report`
5. Stop at human checkpoints for design feel, architecture, PR merge, and team publishing

## Context & Token Policy

See `0_User_Manual/RULES_AND_POLICY.md` §1 (GDD as source of truth) and §8 (token budget).
See `0_User_Manual/context-router.md` for per-role loading tables.

Agents load only: `project-stack.md` + current task card + relevant GDD `@tag:` section + current agent prompt.
