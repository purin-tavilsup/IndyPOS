# Claude Session Context

This folder contains session context for quick handoffs and resumption.

## Files

| File | Purpose | When to Read |
|------|---------|--------------|
| `STATUS.md` | Quick checkpoint (~50 lines) | **Always first** |
| `session-log.md` | Recent session history | When resuming work |
| `session-log-archive.md` | Older sessions | For deep historical context |

## Session Workflow

### Starting a Session
1. Read `STATUS.md` - Current state + next actions
2. If needed, read `session-log.md` - Recent context
3. Start working on next action

### Ending a Session / Compact
1. Update `STATUS.md` with current state + next actions
2. Add session summary to `session-log.md`

## Related Documentation

| Doc | Location |
|-----|----------|
| Project context | `../CLAUDE.md` |
| Full plan | `../.planning/indypos-overhaul/PLAN.md` |
| Completed epics | `../.planning/indypos-overhaul/completed/` |
| Architecture docs | `../docs/` |
