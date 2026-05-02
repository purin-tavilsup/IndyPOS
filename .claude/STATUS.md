# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 7 |
| **Phase** | Installer Side-by-Side — Stage 1 ✅ done, Stage 2 next |
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

### 1. Installer Side-by-Side (v3.7.0 ↔ v4.0.0) — ACTIVE 📋
**Plan:** `.planning/indypos-overhaul/drafts/installer-side-by-side-plan.md`
**Tasks:** 8 stages tracked (Stages 0, 1 ✅ · Stages 2–7 pending)

**Stage 0 ✅** — Discovery: dev box clean, 3.7.0 footprint mapped, no conflicts.

**Stage 1 ✅** (2026-05-02) — `InstallationConfig` is now version-aware:
- Bootstrapper csproj: `<Version>4.0.0</Version>` → `InstallVersion` derives from assembly
- `InstallationConfig` exposes 9 computed properties (paths, service name, Velopack app ID, health-check port)
- StoreHubInstaller / DatabaseSetup / VelopackLauncher dropped consts → instance methods reading `Config.*`
- `appsettings.Production.json` template pins `Urls: http://localhost:5000`
- Orchestrator health check uses `config.HealthCheckPort` (no magic number)
- `publish.ps1` → `--packId "IndyPOS.POS.v4"`
- Build: clean, 0 warnings, 0 errors

**Stage 2 (next)** — Build pipeline:
- Install `vpk` CLI tool
- Run `publish.ps1` to produce `IndyPOS.POS.v4-Setup.exe` + `StoreHub.zip`
- Embed both into `installer/IndyPOS.Bootstrapper/Resources/`
- Verify resource lookup paths match (`IndyPOS.Bootstrapper.Resources.StoreHub.zip`, `IndyPOS.Bootstrapper.Resources.IndyPOS.POS.v4-Setup.exe`)

**Decided (option A):** smoke-test on dev box with real Postgres 18 install — full path coverage, VM will catch any remaining gaps.

### 2. Epic I: Cloud Infrastructure
- [ ] I0: Create Dockerfile for CloudApi
- [ ] I1-I2: Provision DigitalOcean (Droplet + PostgreSQL)
- [ ] I3-I4: Deploy CloudApi, configure SyncWorker
- Full plan in `.planning/indypos-overhaul/PLAN.md`

### 3. Continue Epic M (M7-M13)
- M7: Update First-Run Wizard (store type selection)
- M8: Update WinForms UI to respect feature flags
- M9: Update CloudApi for store type routing

## Key Files

| Purpose | Path |
|---------|------|
| Full plan | `.planning/indypos-overhaul/PLAN.md` |
| Installer side-by-side plan | `.planning/indypos-overhaul/drafts/installer-side-by-side-plan.md` |
| VM testing plan | `.planning/indypos-overhaul/drafts/vm-installer-testing-plan.md` |
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
*Last updated: 2026-05-02*
