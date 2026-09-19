# AI Assistant Guide

This guide exists so an AI assistant can re-enter the project after a long break, understand the current state, and work without inventing a new architecture each session.

## Source Of Truth

Read these files before planning or coding:

1. `README.md` for product vision and implemented systems.
2. `docs/PROJECT_HEALTH.md` for the latest project snapshot and risks.
3. `docs/ROADMAP.md` for current milestones and next work.
4. `docs/ARCHITECTURE_STANDARDS.md` for code organization and design rules.
5. `DECISIONS.md` for lasting product and architecture decisions.
6. `TASKS.md` for short-term task notes.

If documentation and code disagree, inspect the code and update the relevant doc as part of the handoff.

## Project Check-In Prompt

Use this when returning to the project:

```text
Act as the project maintainer for this Unity VTT repo.

First read README.md, docs/PROJECT_HEALTH.md, docs/ROADMAP.md,
docs/ARCHITECTURE_STANDARDS.md, DECISIONS.md, and TASKS.md.
Then inspect git status and the changed files.

Give me a project check-in with these sections:
- Current progress
- Current git state
- Architecture health against SOLID, MVP, assembly definitions, namespaces, and folder organization
- Test and build health, including what was verified and what is unknown
- Roadmap position
- Recommended next 3 tasks
- Risks or decisions that need my attention

Do not edit files unless I ask.
```

## Feature Implementation Prompt

Use this when starting a new feature:

```text
Implement the next small step for [feature].

Before editing, read docs/ARCHITECTURE_STANDARDS.md, docs/ROADMAP.md,
docs/PROJECT_HEALTH.md, DECISIONS.md, and the existing files in the target area.

Follow the existing architecture:
- Keep MonoBehaviours thin.
- Keep domain and rules logic in plain C# where practical.
- Use MVP for non-trivial UI Toolkit flows.
- Keep networking authority behind interfaces and server validation.
- Add or update EditMode tests for rules, state, validation, services, and regressions.
- Update docs/PROJECT_HEALTH.md, docs/ROADMAP.md, TASKS.md, or DECISIONS.md only when the change affects current state, next work, or a lasting decision.

Start with a short implementation plan, then make the change and run the relevant validation.
```

## Architecture Review Prompt

Use this before merging a larger change or when the code feels hard to follow:

```text
Review the current changes as a senior Unity/C# engineer.

Use docs/ARCHITECTURE_STANDARDS.md as the review standard. Focus on bugs,
regressions, missing tests, unclear ownership, SOLID issues, MVP violations,
assembly-definition problems, namespace/folder drift, and modularity problems.

Return findings first, ordered by severity, with file and line references.
For each finding, explain the concrete risk and the smallest practical fix.
Do not rewrite code unless I ask.
```

## End-Of-Session Handoff Prompt

Use this after a coding session:

```text
Create a handoff update for this session.

Summarize what changed, what was validated, what is still unknown, and the next
small task. Update docs/PROJECT_HEALTH.md and docs/ROADMAP.md if the project
state or roadmap changed. Add to DECISIONS.md only for lasting product or
architecture decisions. Keep updates additive and concise.
```

## AI Behavior Rules

- Prefer the existing project architecture over new patterns.
- Do not introduce a pattern unless it solves a named problem.
- Make the smallest useful change that keeps the next change easier.
- If a task touches UI flow, confirm whether it is View-only or MVP.
- If a task touches gameplay rules, isolate calculations from Unity lifecycle.
- If a task touches networking, identify who owns authority and validation.
- If a task touches reusable systems, define the boundary and tests before generalizing.
- Never mark the project healthy only because files compile locally; record what was actually verified.
