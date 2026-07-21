# Context Router

> What to load per agent role. For full rules, see `RULES_AND_POLICY.md` §8.

## Quick Reference

| Role | Load | Token Budget |
|------|------|---------|
| 🔀 Orchestrator | project-stack + task card + workflow index | ~2,000 |
| 🎮 Game Designer | project-stack + GDD `@tag:` + agent prompt | ~4,000 |
| 🏗️ Architect | project-stack + GDD `@tag:` + design spec + ADRs + agent prompt | ~6,000 |
| 💻 Implementer | project-stack + specs + affected source files + agent prompt | ~8,000 |
| 👀 Reviewer | project-stack + git diff + review prompt | ~4,000 |
| 🧪 QA | project-stack + design spec + impl summary + QA prompt | ~4,000 |
| 📋 PM Reporter | project-stack + git log + DevLogs + report prompt | ~2,000 |

For multi-agent framework setup (CrewAI, AutoGen, LangGraph): load each `Agent_Prompts/{role}-agent.md` as that agent's system prompt. Inject `project-stack.md` as shared context.
