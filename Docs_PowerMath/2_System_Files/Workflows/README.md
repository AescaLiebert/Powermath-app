---
description: Agent workflow index and router entrypoint
---

# Agent Workflows

IDE workflow automation files for AI-assisted game development.

## Available Workflows

| Workflow | Trigger | Description |
| --- | --- | --- |
| [bootstrap-project.md](bootstrap-project.md) | `/bootstrap-project` | One-time setup: project brief -> filled GDD + project stack |
| [implement-feature.md](implement-feature.md) | `/implement-feature` | Full pipeline: GDD -> Design -> Architecture -> Code -> Review -> QA |
| [fix-bug.md](fix-bug.md) | `/fix-bug` | Investigate -> Fix -> Regression check |
| [code-review.md](code-review.md) | `/code-review` | Review a file, diff, or PR against project conventions |
| [refactor.md](refactor.md) | `/refactor` | Impact analysis -> safe refactor -> regression check |
| [report.md](report.md) | `/report` | Generate a weekly or sprint PM report from git logs and DevLogs |

## Common Step 0: Validate Task Card

All workflows except `/code-review` and `/report` start with this step:

1. If intake is from Discord/Notion/GitHub/direct prompt, convert to `Docs/3_Outputs/Specs/{slug}-task-card.md`
2. Validate against `Docs/2_System_Files/Handoff_Contracts/README.md`
3. Route with `orchestrator-agent.md` and confirm the workflow selection
4. If the task card is missing, create from `Docs/1_Inputs_Templates/Task_Card_Template.md`
