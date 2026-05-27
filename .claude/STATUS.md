# IndyPOS - Current Status

> Quick checkpoint for session start / context handoff

## Current State

| Field | Value |
|-------|-------|
| **Branch** | `indypos-overhaul` |
| **Sprint** | Sprint 7 |
| **Phase** | Installer Side-by-Side — Stages 1 + 2 ✅, Stage 3 🟡 cleanup-v4 + manifest done · verify-install next |
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
**Tasks:** 8 stages tracked (Stages 0, 1, 2 ✅ · Stages 3–7 pending)

**Stage 0 ✅** — Discovery: dev box clean, 3.7.0 footprint mapped, no conflicts.

**Stage 1 ✅** (2026-05-02) — `InstallationConfig` version-aware (9 computed properties from assembly version).

**Stage 2 ✅** (2026-05-03) — Build pipeline + version alignment:
- `vpk` CLI installed globally (v0.0.1298)
- `build-installer.ps1` now stages `Resources/` from `publish/Releases/` before `dotnet publish` (globs vpk Setup.exe pattern, renames to canonical names)
- Repo-wide version bump in `Directory.Build.props`: `1.0.0` → `4.0.0` (Stage 1 leak — D.B.props was clobbering bootstrapper's local `<Version>`)
- `WinForms/Properties/AssemblyInfo.cs`: `3.7.0` → `4.0.0` (legacy `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` bypassed D.B.props; vpk auto-detects pack version here)
- `Resources/` gitignored
- **Verified:** Bootstrapper `AssemblyVersion 4.0.0.0`, both manifest resources embedded with expected names, final `IndyPOS-Setup.exe` 189 MB v4.0.0.0, vpk packs `IndyPOS.POS.v4` v `4.0.0`

**Stage 3 🟡 in progress** (2026-05-26):
- ✅ `scripts/cleanup-v4.ps1` (~250 lines) — reverses install in 5 steps (service → Velopack → DB → dirs → opt-in Postgres uninstall). QA-reviewed; P0+P1 hardening shipped: `DbConnectionStringBuilder` for password parsing, `Assert-V4Path` regex safety guard, `Wait-ServiceGone` polling, Velopack process wait + shortcut sweep, `DROP OWNED BY` before `DROP ROLE`, `-Force` actually skips Read-Host.
- ✅ **Install manifest infrastructure** — bootstrapper writes `$SystemRoot\install-manifest.json` (camelCase JSON, no secrets, ManifestVersion=1) as post-health-check step in `InstallationOrchestrator`. Cleanup script glob-discovers `v*\install-manifest.json` and overrides hardcoded defaults — cleanup is now genuinely version-agnostic (synthetic v9.9.9 test passed). Kills the `# must match InstallationConfig.cs` lockstep coupling the architecture reviewer flagged.
- ⏳ `scripts/verify-install.ps1` (next) — install-artifact audit; sister script to cleanup, consumes same manifest.
- ⏳ First real smoke-test cycle: build installer → manual click-through → verify → cleanup.

**Verified:** `dotnet build` 0/0 · `dotnet test` 3/3 new manifest tests passing · cleanup no-op on clean box ✅ · synthetic v9.9.9 manifest discovered + targeted correctly ✅.

**Decided (option A):** smoke-test on dev box with real Postgres 18 install — full path coverage, VM will catch any remaining gaps.

**Stage 4 (parked, plan saved):** VM smoke-test via Hyper-V. Hyper-V confirmed enabled on dev box. Practical commands + simplified scope (just 2 scripts, skip Phase 2 unattend) appended to `vm-installer-testing-plan.md` § Hyper-V Quick-Start. Pick up after Stage 3 passes.

**Follow-up (modernization, not blocking):** Delete legacy `Properties/AssemblyInfo.cs` from Application/Infrastructure/WinForms; flip `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` → default. Would let D.B.props drive every assembly's version uniformly.

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
*Last updated: 2026-05-26 — Stage 3 cleanup-v4.ps1 + install-manifest infrastructure shipped (3 commits). Cleanup is version-agnostic via manifest glob-discovery; synthetic v9.9.9 test passed. Next: verify-install.ps1, then first real smoke-test cycle.*
