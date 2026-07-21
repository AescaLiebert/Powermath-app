# Prompt Cookbook — Quick-Start Recipes

> Minimal structured inputs for each workflow. For full pipeline details, see `Docs/2_System_Files/Workflows/`.

---

## Recipe: New Feature
**Workflow:** `/implement-feature` — see `Workflows/implement-feature.md`

### Minimal Input
```
Feature: {one sentence — what should the player experience?}
GDD Section: @tag:{relevant-tag}
Priority reason: {what does this unblock?}
Systems affected: {from project-stack.md system table}
Files to read first: {specific paths from project-stack.md}
Platform constraint: {primary platform input model}
```

---

## Recipe: Bug Fix
**Workflow:** `/fix-bug` — see `Workflows/fix-bug.md`

### Minimal Input
```
Bug: {what's broken — one sentence}
Severity: Critical / Major / Minor / Cosmetic
Repro steps:
  1. {step}
  2. {step}
  3. {step}
Expected: {what should happen per GDD @tag:{section}}
Actual: {what happens instead}
Platform: {which platform, which input method}
Frequency: Always / Sometimes / Rare
```

---

## Recipe: Refactor
**Workflow:** `/refactor` — see `Workflows/refactor.md`

### Minimal Input
```
Goal: {one-sentence refactor goal — be specific}
Motivation: @tag:{section} or ADR-{NNN}
Scope: {which files / systems}
Not in scope: {explicit non-goals}
Behavior change: None (refactor only)
```

---

## Recipe: Code Review
**Workflow:** `/code-review` — see `Workflows/code-review.md`

### Minimal Input
```
PR / Diff: {reference or branch name}
Task card: {slug of the original task}
Focus: Architecture / Performance / Conventions / All
```

---

## Recipe: Sprint Report
**Workflow:** `/report` — see `Workflows/report.md`

### Minimal Input
```
Period: {start date} to {end date}
Source: git log + Docs/3_Outputs/DevLog/{relevant entries}
Audience: team / stakeholder / personal
```

---

## When NOT to Use These Recipes

| Situation | Do This Instead |
|-----------|----------------|
| Setting up a multi-agent pipeline | Load `Agent_Prompts/{role}-agent.md` as system prompts; see `context-router.md` |
| First time bootstrapping | Run `/bootstrap-project` with filled `project-context-brief.md` |
| GDD doesn't exist yet | Fill `project-context-brief.md` first, then `/bootstrap-project` |

## Quick Reference

| You Have | Workflow | First Agent |
|----------|----------|-------------|
| A feature idea | `/implement-feature` | 🎮 Game Designer |
| A bug report | `/fix-bug` | 🔀 Orchestrator → implementation |
| Code needs restructuring | `/refactor` | 🏗️ Architect |
| A diff or PR to evaluate | `/code-review` | 👀 Reviewer |
| End of sprint | `/report` | 📋 PM Reporter |
| Empty project | `/bootstrap-project` | Fill `project-context-brief.md` first |
