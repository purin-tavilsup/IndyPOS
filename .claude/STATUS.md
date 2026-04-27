# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 7 |
| **Phase** | Aspire Local Testing ✅ |
| **Blocked?** | No |

## Recent Session (2026-04-27)

### Aspire Local Testing - COMPLETE ✅

Fixed all blockers for running IndyPOS locally with Aspire:

| Fix | Description |
|-----|-------------|
| global.json | SDK version 7.0.0 → 10.0.107 |
| AppHost postgres | Added WithDataVolume + WithLifetime for persistence |
| WinForms URL | Port 5000 → 5012 for StoreHub |
| LocalTokenOptions | Moved to Application layer with defaults |
| Login | Removed password encryption (API expects plaintext) |
| Products | Fixed StoreId in seeder, singleton HttpClient for auth |
| Reports | Added ReportErrorHandler for 403/error handling |
| Logging | Added debug logging to all command/query handlers |
| OpenTelemetry | Updated to 1.15.x for CVE-2026-40894 fix |
| WinForms in Aspire | Added with WithExplicitStart() |
| dbgate | Removed (pgAdmin sufficient) |
| **Timezone** | Added store timezone support for reports (defaults to Thailand) |

**Test Results:** All 306 tests passing (219 + 15 + 23 + 49)

### Timezone Support
Reports now use store-configured timezone (default: "SE Asia Standard Time" for Thailand).
This allows testing from any location (e.g., Canada) while reports use Thai local time for date calculations.

### Test Accounts (seeded)

| Username | Password | Role |
|----------|----------|------|
| admin | admin123 | SystemAdmin |
| manager | manager123 | StoreManager |
| cashier | cashier123 | Cashier |

## In Progress

### Epic M: Multi-Store Type Support 🟡 IN PROGRESS

**Completed:** M1-M6 (core domain)
**Pending:** M7-M13 (UI, installer, CloudApi, migrations, docs)

## Next Actions (Priority Order)

### 1. VM Testing for Epic V Installer
- [ ] Create Hyper-V VM with Windows 11
- [ ] Run `scripts\publish.ps1` to create Velopack packages
- [ ] Run `installer\build-installer.ps1` to build bootstrapper
- [ ] Test full installation in VM

### 2. Continue Epic M (M7-M13)
- M7: Update First-Run Wizard (store type selection)
- M8: Update WinForms UI to respect feature flags
- M9: Update CloudApi for store type routing
- M10-M13: Installer, migrations, docs

## Key Files

| Purpose | Path |
|---------|------|
| Full plan | `.planning/indypos-overhaul/PLAN.md` |
| Epic M draft | `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md` |
| Session log | `.claude/session-log.md` |

## Quick Commands

```bash
# Run Aspire (http profile)
dotnet run --project src/IndyPOS.AppHost --launch-profile http

# Run tests
dotnet test

# Build
dotnet build
```

---
*Last updated: 2026-04-27*
