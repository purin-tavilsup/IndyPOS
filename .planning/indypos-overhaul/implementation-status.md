# IndyPOS Overhaul - Implementation Status

**Last Updated:** 2026-03-08
**Current Sprint:** Sprint 1
**Current Epic:** Epic 0, A, B, D Complete - Ready for Epic C
**Docs Version:** v1.4.0 (with .NET Aspire support)

---

## Sprint Overview

| Sprint | Focus | Status |
|--------|-------|--------|
| Sprint 1 | **Epic 0** ✅ + **Epic A** ✅ + **Epic B** ✅ + **Epic D** ✅ | 🟢 Complete |
| Sprint 2 | Epic C (StoreHub Service) | Not Started |
| Sprint 3 | Epic E (Outbox + Sync) | Not Started |
| Sprint 4 | Epic F (Cloud API) | Not Started |
| Sprint 5 | Epic G (Desktop Integration) | Not Started |
| Sprint 6 | Epic H (Testing & Rollout) | Not Started |

---

## Epic 0: Extract Business Logic ✅ COMPLETE

**Goal:** Extract business logic from UI codebehind to Application/Domain layers
**Status:** 🟢 Complete
**Completed:** 2026-03-06

### Summary

After auditing the codebase, we found it was already well-structured with CQRS (Nokpirab) and service patterns. Only minor extractions were needed:

| Task | Description | Status |
|------|-------------|--------|
| 0.1 | Add `GetTotal()` to Product/InvoiceProductDto | 🟢 Complete |
| 0.2 | Move PayLater completion logic to entity/DTO | 🟢 Complete |
| 0.3 | Extract CashFlow calculator to CashFlowData model | 🟢 Complete |
| 0.4 | Add unit tests (24 total, all passing) | 🟢 Complete |
| 0.5 | Document PayLater business rule | 🟢 Complete |

### Files Modified:
- `Product.cs` - Added `GetTotal()`
- `InvoiceProductDto.cs` - Added `GetTotal()`
- `PayLaterPayment.cs` - Added `RecordPayment()`, `RemainingAmount`, `WouldBeCompletedWith()`
- `PayLaterPaymentDto.cs` - Added `WouldBeCompletedWith()`, `RemainingAmount`
- `CashFlowData.cs` - Added `CalculateExpectedCash()`, `CalculateActualCash()`, `CalculateCashDifference()`
- 8 UI files updated to use new methods (removed duplicate logic)

### Tests Added:
- `ProductTests.cs` (4 tests)
- `CashFlowDataTests.cs` (8 tests)
- `InvoiceProductDtoTests.cs` (3 tests)
- `PayLaterPaymentDtoTests.cs` (5 tests)
- Fixed missing `xunit.runner.visualstudio` package

### Key Business Rule Documented:
- **PayLater Exclusive Payment Type**: If an invoice has PayLater payment, it CANNOT have other payment types combined

**Documentation:** See `diagrams/07-business-logic-layers.md` and `PAYLATER-FEATURE.md`

---

## Epic B: Remove Deprecated PG Report Feature ✅ COMPLETE

**Goal:** Clean slate - remove old PostgreSQL report writer
**Status:** 🟢 Complete
**Completed:** 2026-03-06
**Commit:** `dab05ac`

### Summary

Removed all deprecated PostgreSQL report writer code to provide a clean slate for the new architecture.

### Tasks

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| B1 | Remove infrastructure registrations | 🟢 Complete | Updated `ConfigureServices.cs` |
| B2 | Delete/archive PostgreSQL repository code | 🟢 Complete | Removed `Persistence/Repositories/PostgreSql/` folder |
| B3 | Remove Npgsql package | 🟢 Complete | Removed from Infrastructure.csproj |
| B4 | Remove report write use cases | 🟢 Complete | Removed `SalesReports/` and `PaymentsReports/` |
| B5 | Delete config keys | 🟢 Complete | Removed `CloudDatabaseEnabled` from config and UI |

### Files Removed:
- `Persistence/Repositories/PostgreSql/` (entire folder)
- `Abstractions/Reports/Repositories/` (interfaces)
- `UseCases/SalesReports/` (commands and handlers)
- `UseCases/PaymentsReports/` (commands and handlers)
- Related models, extensions, and constants

**Deliverable:** Build passes; no PG report references remain ✅

---

## Epic A: Prepare the Codebase ✅ COMPLETE

**Goal:** Add foundational concepts (StoreId, docs, dev environment)
**Status:** 🟢 Complete
**Completed:** 2026-03-06
**Commit:** `aa84aee`

### Summary

Added foundational infrastructure for multi-store support and development environment.

### Tasks

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| A1 | Create architecture docs folder | 🟢 Complete | Created `docs/` with ASCII diagrams |
| A2 | Add StoreId concept | 🟢 Complete | `IStoreIdentityService` + `StoreIdentityService` |
| A3 | Local hub DB setup | 🟢 Complete | `docker-compose.yml` + `scripts/init-db.sql` |

### Files Created:
- `IStoreIdentityService.cs` (interface)
- `StoreIdentityOptions.cs` (config model)
- `StoreIdentityService.cs` (implementation)
- `docs/architecture/overview.md`
- `docs/architecture/store-identity.md`
- `docs/development/docker-setup.md`
- `docs/diagrams/architecture-overview.md` (ASCII)
- `docs/diagrams/data-flow.md` (ASCII)
- `docker-compose.yml`
- `scripts/init-db.sql`

### Tests Added:
- `StoreIdentityServiceTests.cs` (8 tests, all passing)

**Deliverable:** StoreId concept available; dev environment ready ✅

---

## Epic D: Schema Design ✅ COMPLETE

**Goal:** Define PostgreSQL schema with PublicId + StoreId
**Status:** 🟢 Complete
**Completed:** 2026-03-06

### Summary

Created new Core domain entities with UUID-based IDs and EF Core configurations for PostgreSQL.

### Tasks

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| D1 | Create Core entities with UUID Id | 🟢 Complete | Invoice, InvoiceLine, Payment, Product, PayLater |
| D2 | Create OutboxEvent entity | 🟢 Complete | For reliable cloud sync |
| D3 | Create InventoryMovement entity | 🟢 Complete | Movement-based tracking |
| D4 | Create EF Core configurations | 🟢 Complete | StoreHubDbContext + all entity configs |

### Files Created:
**Domain (Entities/Core/):**
- `Invoice.cs`, `InvoiceLine.cs`, `Payment.cs`, `Product.cs`, `PayLater.cs`
- `OutboxEvent.cs`, `InventoryMovement.cs`

**Infrastructure (Persistence/StoreHub/):**
- `StoreHubDbContext.cs`
- `Configurations/` - 7 entity configuration files

### Architecture Decision:
- See `ADR-002-entity-organization.md` for entity organization strategy
- Core entities coexist with legacy entities until Epic G cleanup

**Deliverable:** Schema defined; EF Core ready for StoreHub ✅

---

## Epic C: Create StoreHub Service + Aspire Foundation

**Goal:** New StoreHub Windows Service project with .NET Aspire dev orchestration
**Status:** 🔴 Not Started
**Target:** Sprint 2
**Priority:** HIGH

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| C0a | Add IndyPOS.ServiceDefaults project | 🔴 Not Started | - | Shared health checks, OpenTelemetry, conventions |
| C0b | Add IndyPOS.AppHost project | 🔴 Not Started | - | Aspire orchestrator for dev environment |
| C1 | Add IndyPOS.StoreHub project | 🔴 Not Started | - | ASP.NET Core + Windows Service template |
| C2 | Add local Postgres persistence | 🔴 Not Started | - | EF Core + connection string |
| C3 | Expose minimal endpoints | 🔴 Not Started | - | POST /sales/complete, GET /products, etc. |
| C4 | Move selling logic to hub | 🔴 Not Started | - | Reuse Application services initially |
| C5 | Add StoreHub to AppHost | 🔴 Not Started | - | Wire up Postgres + StoreHub in Aspire |

### Aspire Setup Details

**IndyPOS.ServiceDefaults** provides:
- Health check endpoints
- OpenTelemetry tracing/metrics
- Shared service conventions

**IndyPOS.AppHost** orchestrates:
- PostgreSQL container (dev)
- StoreHub API
- Future: CloudApi, SyncWorker

**Developer workflow:**
```bash
dotnet run --project src/IndyPOS.AppHost
```

### Implementation Notes

**Terminal Concurrency Strategy** (from v1.4.0 docs):
- Both terminals MUST call the same StoreHub API (not separate DBs)
- StoreHub generates final invoice numbers (not terminals)
- PostgreSQL transactions provide row locking for concurrent sales
- Use inventory movements, not "set quantity = X"

**Sales Transaction Sequence:**
1. Begin PostgreSQL transaction
2. Generate invoice number
3. Lock/check stock rows
4. Insert: invoice → lines → payments → inventory movements → outbox
5. Commit (all or nothing)

**Deliverable:** Hub runs locally via Aspire; can complete a sale into local Postgres

---

## Epic E: Outbox + SyncWorker

**Goal:** Reliable sync from StoreHub to Cloud
**Status:** 🔴 Not Started
**Target:** Sprint 3
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| E1 | Create Outbox table | 🔴 Not Started | - | EF migration |
| E2 | Write Outbox events at commit points | 🔴 Not Started | - | Same transaction as business data |
| E3 | Implement SyncWorker | 🔴 Not Started | - | BackgroundService with retry |
| E4 | Local observability endpoints | 🔴 Not Started | - | GET /sync/status |

**Deliverable:** Disconnect internet → sales continue → reconnect → events sync

---

## Epic F: Cloud API

**Goal:** Central cloud API + PostgreSQL (Singapore)
**Status:** 🔴 Not Started
**Target:** Sprint 4
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| F1 | Create IndyPOS.CloudApi project | 🔴 Not Started | - | ASP.NET Core + Dockerfile |
| F1a | Add CloudApi to AppHost | 🔴 Not Started | - | Wire up in Aspire for local dev |
| F2 | Idempotent event ingestion | 🔴 Not Started | - | POST /sync/events |
| F3 | Process event types | 🔴 Not Started | - | InvoiceCompleted, InventoryMovementRecorded |
| F4 | Master data endpoints | 🔴 Not Started | - | GET /master/products, GET /master/config |
| F5 | Auth (API key) | 🔴 Not Started | - | StoreId + shared secret |

**Deliverable:** Cloud accepts events; stores them; supports master data pull

---

## Epic G: Desktop Integration

**Goal:** Desktop app becomes hub client
**Status:** 🔴 Not Started
**Target:** Sprint 5
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| G1 | Desktop becomes hub client | 🔴 Not Started | - | Call localhost hub endpoints |
| G2 | Prepare for tablet | 🔴 Not Started | - | LAN interface + device auth |
| G3 | Decommission direct SQLite writes | 🔴 Not Started | - | After hub is stable |

**Deliverable:** Desktop uses hub API; local Postgres is single source of truth

---

## Epic H: Testing & Rollout

**Goal:** Comprehensive testing and pilot deployment
**Status:** 🔴 Not Started
**Target:** Sprint 6
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| H1 | Automated tests | 🔴 Not Started | - | Unit + integration tests |
| H2 | Upgrade path tests | 🔴 Not Started | - | SQLite → Postgres migration |
| H3 | Pilot rollout | 🔴 Not Started | - | Store 1 first, monitor 1 week |
| H4 | Operational runbook | 🔴 Not Started | - | Backup, monitoring, troubleshooting |

**Deliverable:** Production-ready system; pilot store live

---

## .NET 10 Upgrade ✅ COMPLETE

**Goal:** Upgrade entire solution to .NET 10 LTS
**Status:** 🟢 Complete
**Completed:** 2026-03-06
**Commit:** `0afb961`

### Summary

Upgraded the entire solution from .NET 8 to .NET 10 LTS before starting Epic C.

### Changes

| Project | Before | After |
|---------|--------|-------|
| IndyPOS.Domain | net8.0 | net10.0 |
| IndyPOS.Application | net8.0 | net10.0 |
| IndyPOS.Infrastructure | net8.0-windows | net10.0-windows |
| IndyPOS.Windows.Forms | net8.0-windows | net10.0-windows |
| IndyPOS.Application.Tests | net8.0-windows | net10.0-windows |
| IndyPOS.Windows.Forms.Tests | net8.0-windows | net10.0-windows |
| IndyPOS.Mock | net8.0 | net10.0 |

### Package Updates
- Microsoft.Extensions.* → 10.0.x
- Microsoft.EntityFrameworkCore → 10.0.0
- Npgsql.EntityFrameworkCore.PostgreSQL → 10.0.0
- Serilog packages → latest .NET 10 compatible
- Removed legacy packages (Microsoft.CSharp, System.Memory, System.ValueTuple, etc.)

### Fixes
- Added `NoWarn` for WFO1000 (WinForms designer serialization warnings)
- Fixed missing test runner packages

**Tests:** 32/32 passing ✅

---

## Current Focus

**Now:** .NET 10 Upgrade Complete ✅
**Next:** Epic C (Create StoreHub Service + Aspire Foundation)

### Next Actions
1. Create IndyPOS.ServiceDefaults (shared health/OTel)
2. Create IndyPOS.AppHost (Aspire orchestrator)
3. Create IndyPOS.StoreHub ASP.NET Core project (net10.0)
4. Wire StoreHub + Postgres in AppHost
5. Add PostgreSQL persistence with EF Core
6. Expose minimal API endpoints
7. Move selling logic to hub

---

## Statistics

- **Total Epics:** 7
- **Completed Epics:** 4 (Epic 0, A, B, D)
- **Total Tasks:** 41 (+4 Aspire tasks)
- **Completed:** 17
- **In Progress:** 0
- **Not Started:** 24
- **Overall Progress:** ~41%

---

## Notes

- .NET 10 upgrade completed before Epic C
- Epic order adjusted: D (Schema) now comes before C (StoreHub)
- Rationale: Need clear schema when transitioning SQLite → PostgreSQL
- All tasks should result in small, focused PRs
- Tests required for all epics
- Documentation updated as we go
- **v1.4.0 Update:** Added .NET Aspire for dev orchestration (AppHost + ServiceDefaults)
- **v1.4.0 Update:** Added terminal concurrency and transaction sequence details to Epic C
- Aspire is for development only; production remains Docker + DigitalOcean

## Reference Documentation

| Doc | Location | Purpose |
|-----|----------|---------|
| v1.4.0 Docs | `.planning/indypos-overhaul/IndyPOS_Docs_v1_4_0/` | Latest architecture specs |
| Aspire Plan | `docs/architecture/aspire.md` | Aspire setup details |
| Solution Layout | `docs/solution-structure/recommended-layout.md` | Project organization |
| Terminal Concurrency | `docs/storehub/terminal-concurrency-strategy.md` | Multi-terminal safety |
| Transaction Sequence | `docs/storehub/sales-transaction-sequence.md` | Sale commit flow |

---

## Legend

- 🔴 Not Started
- 🟡 In Progress
- 🟢 Completed
- ⏸️ Blocked
- ⏭️ Skipped

---

**Last Session:** 2026-03-08
