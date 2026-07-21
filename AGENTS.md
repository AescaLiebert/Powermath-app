# Agent Entry Point
Use this file as the always-loaded instruction layer. Load docs on demand only.

## Reusable System Location
- `Docs(Template)/` (or `Docs/` if active)
  - `0_User_Manual/RULES_AND_POLICY.md` & `TEAM_SYNC_POLICY.md`
  - `1_Inputs_Templates/` (GDD, Stack, Brief)
  - `2_System_Files/` (Agent_Prompts, Handoff_Contracts, Workflows)
  - `3_Outputs/` (ADRs, DevLog, Specs)

## Routing Rule
For multi-agent work, route before coding:
1. Read `2_System_Files/Agent_Prompts/orchestrator-agent.md`.
2. Read `2_System_Files/Handoff_Contracts/README.md`.
3. Create/validate task card from `3_Outputs/Specs/{slug}-task-card.md`.
4. Select one workflow from `2_System_Files/Workflows/`.
5. Stop at human checkpoints for design, architecture, PR merge, publishing, build/CI settings, dependencies, secrets, or destructive actions.

## Context Loading Policy
Load ONLY what is needed:
- Stack: `1_Inputs_Templates/project-stack.md`
- Task: `3_Outputs/Specs/{slug}-task-card.md`
- Design: relevant `@tag:` section from GDD
- Workflow: single file from `2_System_Files/Workflows/`
- Prompt: single file from `2_System_Files/Agent_Prompts/`
- Rules: relevant sections of `0_User_Manual/RULES_AND_POLICY.md`

Avoid loading: All agent prompts; full GDD; entire Specs/ADRs/Reports dirs; superseded docs.

## Common Workflows
- New feature: `/implement-feature`
- Bug fix: `/fix-bug`
- Refactor: `/refactor`
- Review: `/code-review`
- Report/sprint summary: PM/report workflow
- New project setup: `/bootstrap-project`

## Team Tools & Safety
- Repo markdown is the source of truth (Notion/Discord are not).
- Never invent project facts. If undocumented, ask for clarification.
- Never auto-merge, publish status, modify CI/build/secrets, or run destructive ops without explicit human approval.

