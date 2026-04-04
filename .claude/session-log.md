# Session Log

> Recent session history for context handoff

---

## 2026-04-03: Epic L Complete - Local Deployment Readiness

**Epic:** L | **Commits:** `aaea941..9d810ad` (11 commits)

### Summary
Completed Epic L (Local Deployment Readiness) - system is now **ready for pilot deployment**! 🚀

### Work Done

**L1-L6 Core Tasks:**
- L1: WinForms appsettings.json + removed legacy `Enabled` flag (StoreHub is now default)
- L2: StoreHub appsettings.Production.json + README documentation
- L3: `publish.ps1` - builds self-contained releases
- L4: `install-config.ps1` - automates PostgreSQL setup
- L5: `smoke-test.ps1` - comprehensive E2E test (health, auth, products, sales, pay later)
- L6: `store-installation-guide.md` - complete deployment guide

**Velopack Prep (Bonus):**
- `Directory.Build.props` - centralized version (1.0.0)
- `AppVersion.cs` - version helper class
- `/version` endpoint in StoreHub
- `docs/versioning.md` + Bruno request

**Boy Scout Cleanup:**
- Removed outdated `Enabled` flag references from 3 docs
- Renamed `setup-local.md` → `store-installation-guide.md`
- Updated PostgreSQL 16 → 18 across all docs/scripts (10 files)

### Key Files Created
| File | Purpose |
|------|---------|
| `scripts/publish.ps1` | Build release binaries |
| `scripts/install-config.ps1` | PostgreSQL + config setup |
| `scripts/smoke-test.ps1` | E2E API tests |
| `docs/operations/store-installation-guide.md` | Deployment guide |
| `docs/versioning.md` | Version management docs |

### Next Steps
1. Run `publish.ps1` to build release binaries
2. Deploy to pilot store
3. Run `smoke-test.ps1` to verify
4. Monitor and gather feedback

---

## 2026-04-01: Epic G3 SQLite Removal Complete

**Epic:** G3 | **Commit:** `02c35fc`

### Summary
Completed full SQLite removal from main IndyPOS application. WinForms now **requires** StoreHub mode.

### Work Done
- Deleted 9 SQLite repository implementations
- Deleted 8 Pos repository interfaces
- Deleted ~70 legacy Nokpirab handlers
- Deleted legacy services (SaleService, UserLogInService, ReportService, StoreConstants)
- Updated WinForms (MainForm, UserLogInPanel, UsersPanel, AddNewUserForm)
- Removed `System.Data.SQLite.Core` and `Dapper` packages
- Created `LegacyDtos.cs` for backward compatibility

### Metrics
- 174 files changed, ~90 deleted
- 5,699 lines deleted, 236 added
- 298 tests passing

### Technical Debt
`StoreHubReportService` has stub implementations for int-based methods → address during MAUI migration.

---

## 2026-03-31: Epic G3 Phases 0a-0b

**Epic:** G3 (SQLite Removal prep)

### Summary
Created foundation for SQLite removal - HardcodedStoreConstants and StoreHubReportService.

### Work Done
- **Phase 0a:** Created `HardcodedStoreConstants.cs` using enums instead of SQLite lookups
- **Phase 0b:** Created `StoreHubReportService.cs` implementing `IReportService`
- Added legacy report endpoints to StoreHub (`/reports/legacy/sales-summary`, `/reports/legacy/payments-summary`)
- Updated `IStoreHubClient` and `StoreHubHttpClient` with report methods

---

## 2026-03-29: Epic H Complete + Solution Reorganization

**Epic:** H (Testing & Rollout)

### Summary
Completed Epic H with integration tests, migration tests, and operational docs. Reorganized solution folders.

### Work Done
- Created `IndyPOS.StoreHub.IntegrationTests` (49 tests)
- Created `IndyPOS.Migration.Tests` (15 tests)
- Created `IndyPOS.MigrationTool` console app
- Fixed NuGet vulnerabilities (Azure.Identity, KubernetesClient)
- Organized 14 projects into logical solution folders
- Created operational docs (pilot-checklist, smoke-test, rollback-plan, RUNBOOK)

---

## 2026-03-28: Epic G3 Phases 4-7

**Epic:** G3 (SQLite Removal)

### Summary
Implemented IInventoryProductService and migrated WinForms to use StoreHub.

### Work Done
- Created `IInventoryProductService` interface
- Implemented `StoreHubInventoryProductService`
- Updated 4 WinForms inventory forms
- Created initial EF Core migration for StoreHub tables
- Added 10 unit tests for StoreHubInventoryProductService

---

## 2026-03-27: Epic S5 + G1 + Report API

**Epic:** S5 (Security) + G1 (Desktop Integration)

### Summary
Implemented RSA signing, DPAPI secrets, StoreHub client integration, and Report API.

### Work Done
- **S5a:** RSA 2048+ signing for CloudApi JWT tokens
- **S5b:** DPAPI secret storage for StoreHub ClientSecret
- **G1:** StoreHub client integration (HTTP client, product cache, sale/login services)
- Simplified UserId from int to Guid
- Created Report API endpoints (5 endpoints)
- Added E2E tests with WireMock

---

*For older sessions, see `session-log-archive.md`*
