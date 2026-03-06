# IndyPOS Overhaul - Implementation Status

**Last Updated:** 2026-03-06
**Current Sprint:** Sprint 1
**Current Epic:** Epic 0, A, B, D Complete - Ready for Epic C

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

## Epic C: Create StoreHub Service

**Goal:** New StoreHub Windows Service project
**Status:** 🔴 Not Started
**Target:** Sprint 2
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| C1 | Add IndyPOS.StoreHub project | 🔴 Not Started | - | ASP.NET Core + Windows Service template |
| C2 | Add local Postgres persistence | 🔴 Not Started | - | EF Core + connection string |
| C3 | Expose minimal endpoints | 🔴 Not Started | - | POST /sales/complete, GET /products, etc. |
| C4 | Move selling logic to hub | 🔴 Not Started | - | Reuse Application services initially |

**Deliverable:** Hub runs locally; can complete a sale into local Postgres

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
| F1 | Create cloud API project | 🔴 Not Started | - | IndyPOS.Cloud + Dockerfile |
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

## Current Focus

**Now:** Sprint 1 Complete ✅ (Epic 0, A, B, D)
**Next:** Epic C (Create StoreHub Service)

### Next Actions
1. Create IndyPOS.StoreHub ASP.NET Core project
2. Add PostgreSQL persistence with EF Core
3. Expose minimal API endpoints
4. Move selling logic to hub

---

## Statistics

- **Total Epics:** 7
- **Completed Epics:** 4 (Epic 0, A, B, D)
- **Total Tasks:** 37
- **Completed:** 17
- **In Progress:** 0
- **Not Started:** 20
- **Overall Progress:** ~46%

---

## Notes

- Epic order adjusted: D (Schema) now comes before C (StoreHub)
- Rationale: Need clear schema when transitioning SQLite → PostgreSQL
- All tasks should result in small, focused PRs
- Tests required for all epics
- Documentation updated as we go

---

## Legend

- 🔴 Not Started
- 🟡 In Progress
- 🟢 Completed
- ⏸️ Blocked
- ⏭️ Skipped

---

**Last Session:** 2026-03-06
