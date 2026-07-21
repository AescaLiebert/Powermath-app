# Agent System Prompts

Ready-to-use system prompts for each role. Run `/bootstrap-project` first to fill `project-stack.md`.

## How to Use

1. Run `/bootstrap-project` to fill in your GDD and project stack.
2. Start multi-agent work with `orchestrator-agent.md`.
3. Load `project-stack.md` as universal context for every agent call.
4. Load the current task card or artifact.
5. Load the prompt for the current role only.
6. Load the relevant GDD `@tag:` section, not the full GDD.

## Agent Roster

| Agent | File | Input | Output | Saves To |
| --- | --- | --- | --- | --- |
| Router | [orchestrator-agent.md](orchestrator-agent.md) | Task, issue, artifact status | Workflow route, context pack | `Specs/` |
| Game Designer | [game-design-agent.md](game-design-agent.md) | Approved task card | Design spec with juice params | `Specs/` |
| Architect | [architect-agent.md](architect-agent.md) | Approved design spec | Tech plan, class diagrams, ADR | `Specs/` |
| Implementer | [implementation-agent.md](implementation-agent.md) | Approved arch + design spec | Production C# code | `Assets/` |
| Reviewer | [code-review-agent.md](code-review-agent.md) | Git diff or code file | Actionable review feedback | PR comment |
| QA | [qa-agent.md](qa-agent.md) | Feature spec + impl summary | Test plan + edge cases | `TestPlans/` |
| PM Reporter | [pm-report-agent.md](pm-report-agent.md) | Git log + DevLogs | Sprint report | `PM_Reports/` |
