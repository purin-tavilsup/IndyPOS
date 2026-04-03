# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 6 (complete) |
| **Phase** | Post-MVP Polish |
| **Blocked?** | No |

## Recent Completion (2026-04-01)

**Epic G3: SQLite Removal** - COMPLETE
- Removed all SQLite dependencies from main app
- 174 files changed, 5,699 lines deleted
- MigrationTool kept for store migrations
- 298 tests passing, 0 errors

## Next Actions (Priority Order)

1. [ ] **Epic I: Cloud Infrastructure** (LOW - when multi-store sync needed)
   - Provision DigitalOcean droplet + managed PostgreSQL
   - Deploy CloudApi
   - Configure SyncWorker with real CloudApi

2. [ ] **Epic S (remaining)** (LOW priority)
   - S6: Key rotation support
   - S7: Security audit logging
   - S8: Rate limiting
   - S9: Secrets management

3. [ ] **Backlog: Migration --sync-to-cloud flag**
   - Create outbox events for migrated invoices

## Key Files

| Purpose | Path |
|---------|------|
| Full plan & epics | `.planning/indypos-overhaul/PLAN.md` |
| Session history | `.claude/session-log.md` |
| Completed epics | `.planning/indypos-overhaul/completed/` |
| Security spec | `.planning/indypos-overhaul/security/` |

## Quick Context

IndyPOS is a Point-of-Sale system migrating from SQLite to PostgreSQL (StoreHub). The main MVP is ~95% complete. Desktop app now uses StoreHub API exclusively. Cloud sync infrastructure exists but isn't deployed yet.

## Stats

- **Tests:** 298 passing
- **Build:** 0 errors, 55 warnings
- **Progress:** ~95% complete

---
*Last updated: 2026-04-03*
