# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 7 |
| **Phase** | Local Deployment Readiness |
| **Blocked?** | No |

## Recent Completion (2026-04-03)

- Restructured session context docs (STATUS.md, PLAN.md, completed/)
- Updated Bruno collection (25 requests, 100% endpoint coverage)

## Next Actions (Priority Order)

### Epic L: Local Deployment Readiness

1. [ ] **L1: WinForms appsettings.json** (HIGH)
   - Add `StoreHub` section with `Enabled: true`, `BaseUrl`

2. [ ] **L2: StoreHub appsettings.Production.json** (HIGH)
   - Connection string for local PostgreSQL
   - JWT signing key config

3. [ ] **L3: install-config.ps1 script** (MEDIUM)
   - PostgreSQL setup automation
   - Referenced in pilot-checklist but missing

4. [ ] **L4: Publish script** (MEDIUM)
   - Build release binaries for StoreHub + WinForms

5. [ ] **L5: End-to-end test** (HIGH)
   - WinForms → StoreHub → PostgreSQL full flow

## Key Files

| Purpose | Path |
|---------|------|
| Full plan | `.planning/indypos-overhaul/PLAN.md` |
| Pilot checklist | `docs/operations/pilot-checklist.md` |
| StoreHub options | `Infrastructure/Services/StoreHub/StoreHubOptions.cs` |
| WinForms config | `src/IndyPOS.Windows.Forms/appsettings.json` |

## Quick Context

IndyPOS StoreHub migration is ~95% code complete. Now focusing on deployment readiness - config files, scripts, and end-to-end testing before pilot rollout.

## Stats

- **Tests:** 298 passing
- **Build:** 0 errors, 55 warnings
- **Bruno:** 25 requests (100% coverage)

---
*Last updated: 2026-04-03*
