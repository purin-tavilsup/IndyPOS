# IndyPOS Agent Instructions

Start here when working in this repository with Codex or another coding agent.

## Required Context

Read these files first:

1. `.claude/STATUS.md` for current branch, active epic, and next actions.
2. `CLAUDE.md` for project structure, architecture, conventions, and workflow.

If the task touches active planning or architecture decisions, also read the relevant file under `.planning/indypos-overhaul/`.

## Project Defaults

- Main branch: `development`
- Current active branch may differ; check `.claude/STATUS.md`
- Architecture: Clean Architecture with CQRS and DDD
- Backend: C# .NET 10
- UI: Windows Forms
- Database: PostgreSQL

## Working Rules

- Keep Domain free of external dependencies.
- Keep Application dependent on Domain only.
- Keep Infrastructure implementations behind Application interfaces.
- Keep UI thin and focused on presentation concerns.
- Follow existing naming patterns from `CLAUDE.md`.
- Prefer small, testable changes over broad refactors.
- Validate input at system boundaries.
- Use async/await for I/O work.
- Do not introduce SQLite-based solutions; PostgreSQL is the active direction.

## Current Priorities

Unless the user redirects, prefer work that aligns with the active status in `.claude/STATUS.md`, especially:

- Epic M follow-up work
- VM installer testing scripts
- Installer validation and deployment flow hardening

## Codex Behavior

- Before editing, gather enough context to avoid conflicting with current architecture decisions.
- Before changing behavior, inspect nearby tests and update or add tests when practical.
- Favor targeted fixes over speculative cleanup.
- Preserve unrelated user changes in the worktree.
- When tradeoffs exist, choose the option that is simplest to maintain in a small multi-store retail deployment.

## Response Style

- Be casual and use simple words.
- Address the user as `Pond`.
- Codex may have opinions, but facts come first.
- Keep replies concise and practical.
- State assumptions clearly when needed.
- Summarize risks and verification performed.

