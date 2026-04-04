# IndyPOS Overhaul - Implementation Plan

**Last Updated:** 2026-04-03
**Progress:** ~98% Complete (Epic L Done, Ready for Pilot)

---

## Epic Overview

| Epic | Name | Status |
|------|------|--------|
| 0 | Extract Business Logic | Complete |
| A | Prepare Codebase | Complete |
| B | Remove Deprecated PG Report | Complete |
| C | StoreHub Service + Aspire | Complete |
| D | Schema Design | Complete |
| E | Outbox + SyncWorker | Complete |
| F | Cloud API | Complete |
| G | Desktop Integration | Complete |
| H | Testing & Rollout | Complete |
| L | Local Deployment Readiness | Complete |
| I | Cloud Infrastructure | Not Started |
| S | Security Hardening | 5/9 Complete |

> For detailed epic history, see `completed/` folder

---

## Recently Completed

### Epic L: Local Deployment Readiness ✅

**Goal:** Get the system ready for local machine deployment and testing

| Task | Description | Priority | Status |
|------|-------------|----------|--------|
| L1 | Add StoreHub config to WinForms appsettings.json | HIGH | ✅ Complete |
| L2 | Create StoreHub appsettings.Production.json | HIGH | ✅ Complete |
| L3 | Create publish script (build release binaries) | MEDIUM | ✅ Complete |
| L4 | Create install-config.ps1 script | MEDIUM | ✅ Complete |
| L5 | End-to-end test: WinForms → StoreHub → PostgreSQL | HIGH | ✅ Complete |
| L6 | Create setup guide: Local Machine Deployment | MEDIUM | ✅ Complete |

**Bonus: Velopack Prep**
| Task | Description | Status |
|------|-------------|--------|
| Version system | `Directory.Build.props`, `AppVersion.cs` | ✅ Complete |
| Version endpoint | `GET /version` in StoreHub | ✅ Complete |
| Bruno request | `get-version.bru` | ✅ Complete |
| Versioning docs | `docs/versioning.md` | ✅ Complete |

**Deployment Scenarios:**
- **Dev/Test (Aspire):** `dotnet run --project src/IndyPOS.AppHost` - API testing with Bruno
- **Local Production:** WinForms + StoreHub + PostgreSQL on same machine

---

#### L1: WinForms appsettings.json - StoreHub Config ✅

**What was done:** Added StoreHub config section to WinForms appsettings.json. Removed the legacy `Enabled` flag since StoreHub is now the only option (SQLite removed). Also removed unused `Database` section.

**Files modified:**
- `src/IndyPOS.Windows.Forms/appsettings.json` - Added StoreHub config
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubOptions.cs` - Removed `Enabled` property
- `src/IndyPOS.Infrastructure/ConfigureServices.cs` - Removed conditional check
- `src/IndyPOS.Windows.Forms/appsettings.README.md` - Created config documentation

---

#### L2: StoreHub appsettings.Production.json ✅

**What was done:** Created production config template with all necessary settings and comprehensive documentation.

**Files created:**
- `src/IndyPOS.StoreHub/appsettings.Production.json` - Production config template
- `src/IndyPOS.StoreHub/appsettings.Production.README.md` - Detailed property documentation

---

#### L3: Publish Script ✅

**What was done:** Created comprehensive publish script that builds self-contained releases for all components.

**Files created:**
- `scripts/publish.ps1` - Builds StoreHub, WinForms, MigrationTool

---

#### L4: install-config.ps1 Script ✅

**What was done:** Created installation script that automates PostgreSQL setup, directory creation, JWT key generation, and config file creation.

**Files created:**
- `scripts/install-config.ps1` - Full installation automation

---

#### L5: End-to-End Test ✅

**What was done:** Created comprehensive smoke test script covering all major API flows.

**Test Coverage:**
| Category | Tests |
|----------|-------|
| **Health** | `/health/live`, `/health/ready`, `/version` |
| **Auth** | Login (valid/invalid), `/auth/me` |
| **Products** | Create, update, search, barcode lookup |
| **Inventory** | Adjust quantity |
| **Sales** | Complete sale, verify inventory deducted, verify in reports |
| **Pay Later** | Create pay later sale, list accounts, record payment |
| **Reports** | Sales summary, invoices, product sales, pay later report |
| **Cleanup** | Delete test product |

**Files created:**
- `scripts/smoke-test.ps1` - Comprehensive E2E smoke test
- `.bruno/StoreHub/health/get-version.bru` - Bruno request for version endpoint

---

#### L6: Setup Guide - Local Machine Deployment ✅

**What was done:** Created comprehensive setup guide with architecture diagrams, step-by-step instructions, and troubleshooting sections.

**Guide Sections:**
1. Overview with architecture diagrams
2. Prerequisites (hardware, software, network)
3. PostgreSQL Setup
4. StoreHub Service Setup (with Windows Service installation)
5. Data Migration (SQLite → PostgreSQL)
6. WinForms Client Setup
7. Multi-Terminal Setup
8. Backup Configuration
9. Maintenance and Troubleshooting

**Files created:**
- `docs/operations/store-installation-guide.md` - Comprehensive store deployment guide

---

### Epic S: Security Hardening (Partial)

**Completed:**
- S1: POS offline authentication (BCrypt, JWT)
- S2: Local user cache (sync from cloud)
- S3: RBAC implementation (capability-based)
- S4: CloudApi user management
- S5: RSA key signing + DPAPI secrets

**Remaining (LOW priority):**
| Task | Description | Priority |
|------|-------------|----------|
| S6 | Key rotation support | LOW |
| S7 | Security audit logging | LOW |
| S8 | Rate limiting | LOW |
| S9 | Secrets management | LOW (covered by S5) |

### Epic I: Cloud Infrastructure (Not Started)

**Prerequisites:** Epic L (Local Deployment) should complete first

| Task | Description |
|------|-------------|
| I1 | Provision DigitalOcean Droplet |
| I2 | Provision DO Managed PostgreSQL |
| I3 | Deploy CloudApi to Droplet |
| I4 | Configure SyncWorker with real CloudApi |
| I5 | Multi-store sync testing |
| I6 | Central reporting dashboard |
| I7 | Create setup guide: Cloud Deployment |

**Cloud Specs:**
- Droplet: Basic Premium AMD (2 GB RAM, 1 vCPU, 50 GB SSD)
- Managed PostgreSQL: Smallest tier (1 GB RAM)
- Region: Singapore
- Monthly cost: ~$20-30 USD

---

#### I7: Setup Guide - Cloud Deployment

**Why:** Document the cloud deployment process for future reference and handoff.

**Audience:** Developer/IT admin setting up cloud infrastructure

**Guide Structure:**
```
docs/operations/setup-cloud.md

1. Overview
   - Architecture diagram (Stores → CloudApi → Central PostgreSQL)
   - Why cloud sync? (central reporting, backup, multi-store)
   - Data flow: Outbox pattern + SyncWorker

2. Cloud Infrastructure
   - DigitalOcean Droplet setup (specs, region, OS)
   - Managed PostgreSQL setup
   - Firewall rules (ports 443, 5432)
   - Domain + SSL certificate

3. CloudApi Deployment
   - Build and publish CloudApi
   - Configure appsettings.Production.json
   - Set up as systemd service (Linux)
   - Health check verification

4. OAuth2 Client Setup
   - Register store clients in CloudApi
   - Generate client credentials
   - Distribute to stores securely

5. Store Configuration for Cloud Sync
   - Configure StoreHub with CloudApi credentials
   - Enable SyncWorker
   - Verify sync status

6. Central Database
   - Schema overview (multi-tenant with StoreId)
   - Querying across stores
   - Reporting queries

7. Monitoring & Maintenance
   - Health check endpoints
   - Log aggregation
   - Database backups (managed PostgreSQL snapshots)
   - Scaling considerations

8. Troubleshooting
   - Sync failures
   - Authentication issues
   - Network connectivity
```

**Files:**
- Create: `docs/operations/setup-cloud.md`

---

## Backlog

| Item | Description | Priority |
|------|-------------|----------|
| Migration `--sync-to-cloud` | Create outbox events for migrated invoices | LOW |
| **Auto-Update System** | Remote update capability for StoreHub + WinForms | Future |
| MAUI Migration | Replace WinForms with MAUI | Future |
| Legacy Report Cleanup | Remove int-based report methods | MAUI Migration |

---

## Future: Auto-Update System

**Goal:** Enable remote updates for StoreHub and WinForms without manual intervention.

### Why Auto-Update?

- 3 stores × 1-2 terminals = 5-6 machines to update
- Manual updates require physical access or remote desktop
- Minimize downtime during business hours
- Ensure all stores run consistent versions

### Architecture Options

#### Option A: CloudApi as Update Server (Recommended)

```
┌─────────────┐     Check for updates     ┌─────────────┐
│  StoreHub   │ ──────────────────────────▶│  CloudApi   │
│  WinForms   │                            │             │
└─────────────┘                            │  /updates   │
       │                                   │  /download  │
       │         Download new version      └─────────────┘
       ▼                                          │
┌─────────────┐                            ┌──────▼──────┐
│  Local      │                            │   Azure     │
│  Installer  │                            │   Blob /    │
└─────────────┘                            │   S3 / DO   │
                                           └─────────────┘
```

**Pros:** Centralized control, version tracking per store, rollback support
**Cons:** Requires CloudApi to be deployed first

#### Option B: GitHub Releases + Squirrel

```
┌─────────────┐     Check releases        ┌─────────────┐
│  StoreHub   │ ──────────────────────────▶│   GitHub    │
│  WinForms   │                            │  Releases   │
└─────────────┘                            └─────────────┘
       │                                          │
       │         Download .nupkg                  │
       ▼                                          │
┌─────────────┐                            ┌──────▼──────┐
│  Squirrel   │◀───────────────────────────│   Assets    │
│  Installer  │                            │  (.nupkg)   │
└─────────────┘                            └─────────────┘
```

**Pros:** Simple, works without CloudApi, familiar tooling
**Cons:** Less control over which stores get updates

### Components Needed

| Component | Description |
|-----------|-------------|
| **Version Endpoint** | CloudApi endpoint to check latest version |
| **Update Package** | Signed .nupkg or .zip with new binaries |
| **Update Service** | Background service in StoreHub to check/apply updates |
| **Update UI** | WinForms notification + manual trigger option |
| **Rollback** | Keep previous version, restore on failure |

### Implementation Tasks (Epic U)

| Task | Description | Complexity |
|------|-------------|------------|
| U1 | Add version endpoint to CloudApi (`GET /updates/latest`) | Low |
| U2 | Add update check to StoreHub (background, configurable interval) | Medium |
| U3 | Create update download + extract logic | Medium |
| U4 | Handle StoreHub self-update (stop service, replace, restart) | High |
| U5 | Add update notification to WinForms | Low |
| U6 | Create signed update packages in CI/CD | Medium |
| U7 | Add rollback capability | Medium |
| U8 | Admin UI in CloudApi to manage rollouts | Medium |

### Update Flow (StoreHub)

```
1. StoreHub checks CloudApi every N hours
2. CloudApi returns: { version: "1.2.0", url: "...", hash: "sha256:..." }
3. If newer version available:
   a. Download to temp directory
   b. Verify hash
   c. Schedule update (next restart or off-hours)
4. On scheduled update:
   a. Stop StoreHub service
   b. Backup current binaries
   c. Extract new binaries
   d. Start StoreHub service
   e. Health check - if fails, rollback
5. Report update status to CloudApi
```

### Update Flow (WinForms)

```
1. On startup, check StoreHub for available updates
2. If update available:
   a. Show notification to user
   b. "Update available (v1.2.0) - Install now?"
3. If user accepts:
   a. Download update package
   b. Close WinForms
   c. Run installer/updater
   d. Restart WinForms
```

### Configuration

```json
// StoreHub appsettings.json
{
  "AutoUpdate": {
    "Enabled": true,
    "CheckIntervalHours": 6,
    "UpdateWindowStart": "02:00",
    "UpdateWindowEnd": "05:00",
    "AutoInstall": true
  }
}
```

### Security Considerations

- [ ] Sign update packages (code signing certificate)
- [ ] Verify package hash before applying
- [ ] HTTPS only for downloads
- [ ] Rate limiting on update endpoints
- [ ] Audit log of all updates

### Libraries to Consider

| Library | Purpose |
|---------|---------|
| [Squirrel.Windows](https://github.com/Squirrel/Squirrel.Windows) | WinForms auto-updater (mature, widely used) |
| [Velopack](https://github.com/velopack/velopack) | Modern Squirrel fork, cross-platform |
| [NetSparkle](https://github.com/NetSparkleUpdater/NetSparkle) | .NET updater framework |
| Custom | Roll your own for full control |

### Rollout Strategy

1. **Canary** - Update one store first, monitor for issues
2. **Staged** - Roll out to remaining stores over days
3. **Emergency** - Force update all stores (security patches)

### Dependencies

- Requires Epic I (Cloud Infrastructure) for Option A
- Can start with Option B (GitHub) while waiting for CloudApi

---

## Technical Debt

### StoreHubReportService Legacy Methods
Stub implementations return empty collections. Address during MAUI migration:
- `GetInvoicesByPeriodAsync()`, `GetInvoicesByDateRangeAsync()`
- `GetPayLaterPaymentsByPeriodAsync()`, `GetPayLaterPaymentsAsync()`
- `GetInvoiceProductsByDateAsync()`, `GetInvoiceProductsByDateRangeAsync()`
- `GetInvoiceProductsByInvoiceIdAsync(int)`, `GetPaymentsByInvoiceIdAsync(int)`
- `GetInvoiceInfoAsync(int)`

---

## Statistics

| Metric | Value |
|--------|-------|
| Total Epics | 10 |
| Completed Epics | 9 |
| Total Tests | 298 |
| Build Status | 0 Errors, 55 Warnings |

---

## Reference Documentation

| Doc | Location |
|-----|----------|
| Current status | `.claude/STATUS.md` |
| Session history | `.claude/session-log.md` |
| Completed epics | `.planning/indypos-overhaul/completed/` |
| Security spec | `.planning/indypos-overhaul/security/` |
| Diagrams | `.planning/indypos-overhaul/diagrams/` |
| Operations docs | `docs/operations/` |

---

## Solution Structure

```
src/
  Core/
    IndyPOS.Domain
    IndyPOS.Application
    IndyPOS.Infrastructure
  DesktopApp/
    IndyPOS.Windows.Forms
  Services/
    IndyPOS.StoreHub
    IndyPOS.CloudApi
  DevAppHost/
    IndyPOS.AppHost
    IndyPOS.ServiceDefaults
  Tools/
    IndyPOS.MigrationTool

tests/
  Core/ -> IndyPOS.Application.Tests
  DesktopApp/ -> IndyPOS.Windows.Forms.Tests
  Services/ -> IndyPOS.StoreHub.IntegrationTests
  Tools/ -> IndyPOS.Migration.Tests, IndyPOS.MigrationTool.Tests
```

---

## Quick Commands

```bash
# Run tests
dotnet test

# Build
dotnet build

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222
```
