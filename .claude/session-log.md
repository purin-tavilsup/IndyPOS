# Session Log

> Recent session history for context handoff

---

## 2026-04-27: Aspire Local Testing Complete ✅

**Focus:** Fix all blockers for running IndyPOS locally with Aspire

### Summary
Fixed all issues preventing team from testing IndyPOS with Aspire. Login, products, reports, and sales all working. Added comprehensive logging for debugging.

### Fixes Applied

| Area | Issue | Fix |
|------|-------|-----|
| global.json | SDK version 7.0.0 invalid | Changed to 10.0.107 |
| AppHost | PostgreSQL containers not persisting | Added WithDataVolume + WithLifetime |
| WinForms | Wrong StoreHub port (5000) | Changed to 5012 |
| CloudApi | LocalToken:SecretKey config error | Moved LocalTokenOptions to Application with defaults |
| Login | Password encrypted before sending | Removed encryption (API expects plaintext) |
| Products | Not showing after login | Fixed StoreId in seeder, singleton HttpClient |
| Reports | 403 errors not handled | Added ReportErrorHandler for graceful handling |
| Tests | Flaky SyncWorkerTests | Used TaskCompletionSource pattern |
| Tests | Handler tests missing logger | Added NullLogger |
| Tests | ReportsEndpointTests timezone | Use DateTime.Now not UtcNow |
| Security | OpenTelemetry CVE-2026-40894 | Updated to 1.15.x |
| Aspire | WinForms not in dashboard | Added with WithExplicitStart() |
| Aspire | dbgate redundant | Removed (pgAdmin sufficient) |
| Reports | Date picker not initialized | Added DateTime.Today initialization |
| Reports | Testing from Canada shows wrong dates | Added store timezone support (defaults to Thailand) |

### Timezone Support
- Added `TimeZone` property to `IStoreIdentityService`
- Added `TimeZoneId` config option to `StoreIdentityOptions` (default: "SE Asia Standard Time")
- Updated all 5 report handlers to pass store timezone to `ReportDateRange.ToUtcRange()`
- Reports now correctly use Thai local time regardless of where app is running

### Logging Added
- CompleteSaleCommandHandler
- IngestEventsCommandHandler
- All 7 report query handlers

### Test Results
- **306 tests passing** (219 + 15 + 23 + 49)
- All integration tests green ✅

### Commits (12 total)
1. `fix(aspire): correct SDK version, postgres persistence, and StoreHub URL`
2. `fix(auth): move LocalTokenOptions to Application layer and remove password encryption`
3. `feat(logging): add debug logging to command and query handlers`
4. `fix(ui): improve reports, inventory, and error handling`
5. `test: fix tests for logging changes and flaky behavior`
6. `feat(aspire): add WinForms app to AppHost with explicit start`
7. `fix(security): update OpenTelemetry packages for CVE-2026-40894`
8. `chore(aspire): remove dbgate, pgAdmin is sufficient`
9. `feat(reports): add timezone support for store-specific date calculations`

### Test Accounts
| Username | Password | Role |
|----------|----------|------|
| admin | admin123 | SystemAdmin |
| manager | manager123 | StoreManager |
| cashier | cashier123 | Cashier |

---

## 2026-04-05: Epic M Progress (M1-M6) + VM Testing Plan

**Epic:** M | **Tasks:** M1-M6 complete | **Commits:** 9

### Summary
Continued Epic M (Multi-Store Type Support). Completed M1-M6 (core domain work). Created VM testing documentation and automation plan.

### Key Decision
**StoreId Strategy:** Root entities only (Product, StoreSetting). Child entities (Payment, InvoiceLine, PayLater) inherit via JOIN to Invoice.

### Work Done

**M3: StoreId on Entities**
- Added StoreId to Product and StoreSetting entities
- Updated repositories with IStoreIdentityService injection
- Created composite unique index (StoreId, Barcode) for Product
- Created EF Core migration: `AddStoreIdToProductAndStoreSetting`

**M4: Repository Updates**
- PayLaterRepository now filters by StoreId via Invoice JOIN
- Maintains store isolation without adding StoreId to child entities

**M5-M6: Feature Validation**
- CompleteSaleCommand blocks PayLater for non-GeneralHardware stores
- GetPayLaterQuery/GetPayLaterByIdQuery validate `Features.PayLaterEnabled`
- RecordPayLaterPaymentCommand validates before processing

**Testing**
- Created `MockStoreIdentityService` in IndyPOS.Mock project
- Updated all PayLater tests to use mock
- Fixed namespace conflicts (IndyPOS.Mock vs Moq.Mock)
- All 301 tests passing ✅

**Documentation**
- Created `docs/development/vm-testing-guide.md` - Hyper-V setup guide
- Created `.planning/indypos-overhaul/drafts/vm-installer-testing-plan.md`
  - Phase 1 (Quick Win): Script-driven with pre-installed Windows
  - Phase 2 (Full Auto): Unattended Windows install + complete pipeline

### Commits
1. `feat(domain): add StoreType enum and StoreTypeFeatures value object`
2. `feat(infrastructure): add StoreId to Product and StoreSetting entities`
3. `feat(infrastructure): filter PayLater by StoreId via Invoice JOIN`
4. `feat(application): add store type feature validation for PayLater`
5. `test: add MockStoreIdentityService and update tests for StoreId`
6. `docs: add VM testing guide and installer testing plan`
7. `feat(installer): add Velopack bootstrapper and publish script`
8. `feat(winforms): add first-run setup wizard foundation`
9. `docs: update store installation guide and plan`

### Files Created
| File | Purpose |
|------|---------|
| `tests/IndyPOS.Mock/MockStoreIdentityService.cs` | Test mock for store types |
| `docs/development/vm-testing-guide.md` | Hyper-V testing guide |
| `.planning/.../vm-installer-testing-plan.md` | VM automation plan |

### Next Session
- Implement Phase 1 VM testing scripts (Quick Win)
- Continue Epic M (M7-M13): UI, installer, CloudApi updates
- Test Epic V installer in Hyper-V VM

---

## 2026-04-05: Epic M Started - Multi-Store Type Support

**Epic:** M | **Tasks:** M1-M2 complete

### Summary
Finalized Epic M decisions and began implementation. Completed M1 (Domain types) and M2 (config schemas).

### Decisions Finalized
| Question | Answer |
|----------|--------|
| StoreHub Location | Local per store (most stores = 1 POS machine) |
| Offline Support | Local PostgreSQL required (offline-first POS) |
| StoreId Generation | Manual UUID by System Admin (avoids ID mismatch) |
| Migration | 1 existing hardware store → migrate to `generalHardware` DB |

Architecture unchanged - still Local PG → SyncWorker → CloudApi → Central PG.

### Work Done

**M1: Domain types**
- Created `src/IndyPOS.Domain/Enums/StoreType.cs` (GeneralHardware, Minimart, CoffeeShop)
- Created `src/IndyPOS.Domain/ValueObjects/StoreTypeFeatures.cs` (PayLaterEnabled, MultipleProductTypesEnabled)

**M2: Config schemas**
- Updated `StoreIdentityOptions` - added `Type` property
- Updated `IStoreIdentityService` - added `StoreType` and `Features` properties
- Updated `StoreIdentityService` - implements new interface members
- Marked `Code` as obsolete (use UUID StoreId instead)

### Files Created/Modified
| File | Change |
|------|--------|
| `Domain/Enums/StoreType.cs` | Created |
| `Domain/ValueObjects/StoreTypeFeatures.cs` | Created |
| `Application/Common/Models/StoreIdentityOptions.cs` | Added Type |
| `Application/Common/Interfaces/IStoreIdentityService.cs` | Added StoreType, Features |
| `Infrastructure/Services/StoreIdentityService.cs` | Implemented new members |
| `.planning/indypos-overhaul/drafts/epic-m-multi-store-type.md` | Updated with decisions |

### Open Decision for Next Session
**M3: StoreId on entities** - Should we add StoreId to ALL entities or only root entities?
- Currently 4 have it: Invoice, StoreUser, OutboxEvent, InventoryMovement
- Missing 5: Product, Payment, PayLater, InvoiceLine, StoreSetting
- Child entities (Payment, InvoiceLine, PayLater) could inherit via JOIN to Invoice

### Build Status
✅ 0 errors, 62 warnings (including expected obsolete warnings)

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
