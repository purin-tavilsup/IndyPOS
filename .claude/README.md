# Claude Workspace

This folder contains workspace metadata for Claude Code sessions.

## Structure

```
.claude/
├── README.md              # This file
├── settings.local.json    # Local settings (not tracked)
└── sessions/              # Session logs (NOT tracked in git)
    └── YYYY-MM-DD-*.md    # Individual session logs
```

## Session Logs

**Tracked in git:** No (in `.gitignore`)

Session logs help Claude resume work across conversations. These are working notes and not meant for version control.

**What's in a session log:**
- Date and summary
- What was accomplished
- Decisions made
- Context needed to resume work

## Related Files

| File | Location | Purpose |
|------|----------|---------|
| Project Context | `../CLAUDE.md` | Generic project info, coding standards |
| Implementation Status | `../.planning/indypos-overhaul/implementation-status.md` | Epic/task tracker |
| Planning Docs | `../.planning/indypos-overhaul/` | Detailed plans, ADRs, specs |
| Architecture Docs | `../docs/` | Public documentation |
