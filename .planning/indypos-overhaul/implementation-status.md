# IndyPOS Overhaul - Implementation Status

**Last Updated:** 2026-03-10
**Last Session:** 2026-03-10
**Current Sprint:** Sprint 5
**Current Epic:** Epic S (Security) - IN PROGRESS 🟡 (S4 Complete)
**Docs Version:** v1.4.0 (with .NET Aspire support)

---

## Sprint Overview

| Sprint | Focus | Status |
|--------|-------|--------|
| Sprint 1 | **Epic 0** ✅ + **Epic A** ✅ + **Epic B** ✅ + **Epic D** ✅ | 🟢 Complete |
| Sprint 2 | **Epic C** (StoreHub Service) ✅ | 🟢 Complete |
| Sprint 3 | **Epic E** (Outbox + Sync) ✅ | 🟢 Complete |
| Sprint 4 | **Epic F** (Cloud API) ✅ | 🟢 Complete |
| Sprint 5 | Epic S (Security) + Epic G (Desktop Integration) | Not Started |
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

## Epic C: Create StoreHub Service + Aspire Foundation ✅ COMPLETE

**Goal:** New StoreHub Windows Service project with .NET Aspire dev orchestration
**Status:** 🟢 Complete
**Completed:** 2026-03-08
**Priority:** HIGH

### Tasks

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| C0a | Add IndyPOS.ServiceDefaults project | 🟢 Complete | Health checks, OpenTelemetry, service discovery |
| C0b | Add IndyPOS.AppHost project | 🟢 Complete | Aspire orchestrator with PostgreSQL, PgAdmin, DbGate |
| C1 | Add IndyPOS.StoreHub project | 🟢 Complete | ASP.NET Core Web API |
| C2 | Add local Postgres persistence | 🟢 Complete | EF Core via Aspire integration |
| C3 | Expose minimal endpoints | 🟢 Complete | /, /health/ready |
| C4 | Implement selling logic | 🟢 Complete | GET /products, POST /sales/complete |
| C5 | Add StoreHub to AppHost | 🟢 Complete | Wired with service discovery |

### Aspire Setup Details

**IndyPOS.ServiceDefaults** provides:
- Health check endpoints (`/health`, `/alive`)
- OpenTelemetry tracing/metrics
- HTTP resilience handlers
- Service discovery

**IndyPOS.AppHost** orchestrates:
- PostgreSQL container (auto-start)
- PgAdmin (on-demand via `WithExplicitStart`)
- DbGate (on-demand via `WithExplicitStart`)
- StoreHub API (auto-start, waits for Postgres)

**Developer workflow:**
```bash
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222 (requires Docker)
```

### Task C4 Implementation Details

**GET /products endpoint:**
- `ProductDto`, `ProductExtensions` for entity-to-DTO mapping
- `GetProductsQuery` with filtering (activeOnly, category, search)
- `GetProductsQueryHandler` using Nokpirab CQRS
- `IProductRepository` + `ProductRepository` with EF Core
- 6 unit tests

**POST /sales/complete endpoint:**
- `CompleteSaleRequest/Response` DTOs
- `CompleteSaleCommand` + `CompleteSaleCommandHandler`
- Full transaction flow:
  - Creates invoice with calculated total
  - Creates invoice lines with product name snapshot
  - Creates payments
  - Creates inventory movements (negative for sales)
  - Creates outbox event for cloud sync
- `ISaleRepository` + `SaleRepository` with atomic EF Core transaction
- 4 unit tests

### Commits (2026-03-08)
- `443043a` docs: add v1.4.0 specs and update diagrams for Epic C
- `0a406fd` feat: add Aspire foundation and StoreHub API project (Epic C)
- `23f1b97` fix: add launchSettings and fix resource naming conflict
- `cb78af7` fix: correct connection name to match AppHost database reference
- `d2217f6` docs: update CLAUDE.md with method chaining style and Aspire setup
- `dc4a73f` feat: add DbGate database UI via Community Toolkit
- `6beefb6` feat: make PgAdmin and DbGate start on-demand via WithExplicitStart
- `c0d79ab` feat(storehub): implement GET /products endpoint with CQRS
- `c01fd0b` feat(storehub): implement POST /sales/complete endpoint with CQRS
- `362ea1f` chore: ignore .claude/settings.local.json

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

**Deliverable:** Hub runs locally via Aspire; can complete a sale into local Postgres ✅

---

## Epic E: Outbox + SyncWorker ✅ COMPLETE

**Goal:** Reliable sync from StoreHub to Cloud
**Status:** 🟢 Complete
**Completed:** 2026-03-08
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| E1 | Create Outbox table | 🟢 Complete | OutboxEvent entity + EF config |
| E2 | Write Outbox events at commit points | 🟢 Complete | Done in CompleteSaleCommandHandler |
| E3 | Implement SyncWorker | 🟢 Complete | BackgroundService with exponential backoff retry |
| E4 | Local observability endpoints | 🟢 Complete | GET /sync/status |

### Implementation Details

**SyncWorker BackgroundService:**
- Polls outbox table every N seconds (configurable)
- Batch processing with configurable size
- Exponential backoff retry (30s, 60s, 120s, 240s...)
- Max retries limit (default 5)
- Enable/disable via configuration
- Uses `ICloudSyncClient` interface (stub until Epic F)

**Files Created:**
- `Application/Abstractions/StoreHub/Repositories/IOutboxRepository.cs`
- `Application/Abstractions/StoreHub/Services/ICloudSyncClient.cs`
- `Infrastructure/Persistence/StoreHub/Repositories/OutboxRepository.cs`
- `Infrastructure/Services/StoreHub/SyncWorker.cs`
- `Infrastructure/Services/StoreHub/SyncWorkerOptions.cs`
- `Infrastructure/Services/StoreHub/StubCloudSyncClient.cs`

**Observability Endpoint:**
- `GET /sync/status` returns pending count, failed count, sync status

**Tests:** 4 new tests (46 total passing)

### Commits (2026-03-08)
- `df4f1b5` feat(storehub): add SyncWorker and /sync/status endpoint (Epic E)

**Deliverable:** Disconnect internet → sales continue → reconnect → events sync ✅

---

## Epic F: Cloud API ✅ COMPLETE

**Goal:** Central cloud API + PostgreSQL (Singapore)
**Status:** 🟢 Complete
**Completed:** 2026-03-09
**Target:** Sprint 4
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| F1 | Create IndyPOS.CloudApi project | 🟢 Complete | ASP.NET Core, net10.0 |
| F1a | Add CloudApi to AppHost | 🟢 Complete | Wired with cloud-db |
| F2 | Idempotent event ingestion | 🟢 Complete | POST /sync/events |
| F3 | Process event types | 🟢 Complete | InvoiceCompletedEvent with rich payload |
| F4 | Master data endpoints | 🟢 Complete | GET /master/products, GET /master/config |
| F5 | OAuth2 + OpenIddict auth | 🟢 Complete | Client Credentials flow, JWT tokens |

### Implementation Details (2026-03-09)

**F1-F2: CloudApi Foundation**
- Created `IndyPOS.CloudApi` project (ASP.NET Core, net10.0)
- Added Scalar API documentation UI
- Implemented `POST /sync/events` with idempotent ingestion
- Added `ISyncedEventRepository` abstraction
- Added `IngestEventsCommand/Handler` with CQRS pattern

**F3: Event Processing with Rich Payload**
- Defined `InvoiceCompletedEvent` contract with schema versioning
- Rich transaction snapshot: invoice header, lines, payments, inventory movements
- Created Cloud domain entities:
  - `CloudInvoice`, `CloudInvoiceLine`, `CloudPayment`, `CloudInventoryMovement`
  - `ProcessedEvent` for first-class idempotency/dedupe
- Added `CloudDbContext` with EF Core configurations
- Added `DbSyncedEventRepository` (database-backed)
- Added `EventProcessor` BackgroundService with idempotent processing

**Event Processing Flow:**
```
POST /sync/events → SyncedEvents table → EventProcessor (background)
                                              ↓
                                         ProcessedEvents (idempotency check)
                                              ↓
                                         CloudInvoice + CloudInventoryMovement
```

**Commits:**
- `caba1dd` feat(cloudapi): add CloudApi project with idempotent sync endpoint (Epic F)
- `0ea7a33` feat(cloudapi): add event processing with rich payload and idempotency (F3)

**F4: Master Data Endpoints**
- Implemented GET /master/products with filtering (activeOnly, modifiedSince)
- Implemented GET /master/config/{storeId} for store configuration
- Created CloudProduct and CloudStoreConfig domain entities

**F5: OAuth2 + OpenIddict Authentication**
- Added OpenIddict packages (AspNetCore, EntityFrameworkCore, BCrypt)
- Created OpenIddictExtensions for server configuration:
  - Client Credentials flow
  - Token endpoint at /oauth/token
  - 15-minute access tokens, 24-hour refresh tokens
  - Scopes: sync.write, master.read
- Created TokenController for OAuth2 token endpoint
- Created RegisterStoreHandler for store registration:
  - Generates ClientId (store_{storeId})
  - Generates secure ClientSecret (32-byte random, Base64)
  - Stores BCrypt-hashed secret
- Created ITokenService + CloudTokenService for StoreHub:
  - In-memory token caching
  - Thread-safe acquisition
  - 30-second expiry buffer
- Created HttpCloudSyncClient with Bearer auth:
  - Attaches JWT token to requests
  - Handles 401 retry with token refresh
  - Graceful offline degradation
- Protected all sensitive endpoints with [Authorize]
- Deleted legacy ApiKeyAuthHandler

**Tests:** 8 new tests total (54 passing)

**Deliverable:** Cloud accepts authenticated events; stores them; supports master data pull ✅

---

## Epic S: Security Hardening 🔐

**Goal:** Complete security implementation per security design spec
**Status:** 🟡 In Progress (4/9 complete)
**Target:** Sprint 5
**Priority:** HIGH
**Reference:** `.planning/indypos-overhaul/security/indypos_security_design_spec.md`

### Already Implemented (Epic F)

| Item | Status | Notes |
|------|--------|-------|
| OAuth2 + OpenIddict | ✅ Done | Client Credentials flow |
| JWT Bearer tokens | ✅ Done | 15min access, 24hr refresh |
| Store registration | ✅ Done | ClientId/Secret generation |
| Token validation | ✅ Done | Signature, expiry, issuer |
| BCrypt password hashing | ✅ Done | For client secrets |
| [Authorize] on endpoints | ✅ Done | All sensitive APIs protected |

### Tasks

| Task | Description | Status | Priority | Notes |
|------|-------------|--------|----------|-------|
| S1 | POS offline authentication | 🟢 Complete | HIGH | BCrypt, JWT, migration from TripleDES |
| S2 | Local user cache | 🟢 Complete | HIGH | Sync users from cloud, cache locally |
| S3 | RBAC implementation | 🟢 Complete | HIGH | Capability-based RBAC |
| S4 | CloudApi user management | 🟢 Complete | MEDIUM | Admin CRUD endpoints (no full Identity) |
| S5 | RSA key signing | 🔴 Not Started | MEDIUM | Replace dev certs with RSA 2048+ |
| S6 | Key rotation support | 🔴 Not Started | LOW | 6-month rotation for JWT signing |
| S7 | Security audit logging | 🔴 Not Started | LOW | Login, permission changes, etc. |
| S8 | Rate limiting | 🔴 Not Started | LOW | API abuse protection |
| S9 | Secrets management | 🔴 Not Started | LOW | Azure Key Vault / env vars |

### S3: RBAC Implementation Details (2026-03-10)

**Capability-based RBAC** - Roles map to capabilities for future flexibility.

**Files Created:**
- `Application/Common/Authorization/Capability.cs` - Capability constants
- `Application/Common/Authorization/RoleCapabilities.cs` - Role-to-capability mapping
- `Application/Common/Authorization/CapabilityAuthorizationHandler.cs` - ASP.NET handler
- `Application.Tests/Common/Authorization/RoleCapabilitiesTests.cs` - 21 tests

**Endpoint Protection:**

| Endpoint | Policy | Cashier | Manager | Admin |
|----------|--------|:-------:|:-------:|:-----:|
| `GET /products` | CanReadProducts | ✅ | ✅ | ✅ |
| `POST /sales/complete` | CanCompleteSales | ✅ | ✅ | ✅ |
| `GET /sync/status` | CanViewSyncStatus | ❌ | ✅ | ✅ |
| `POST /admin/stores/register` | SystemAdminOnly | ❌ | ❌ | ✅ |

**Design Decisions:**
- Keep current role names (Cashier, StoreManager, SystemAdmin)
- Full admin auth for `/admin/stores/register` (not API key)
- Capability pattern enables future fine-grained permissions

**Commits:**
- `3b18010` feat(security): add RBAC with capability-based authorization (Epic S3)
- `c9b98ee` feat(aspire): add service discovery for StoreHub → CloudApi communication

**Additional Improvements:**
- StoreHub → CloudApi now uses Aspire service discovery instead of hardcoded URLs
- `CloudTokenOptions.BaseUrl` marked `[Obsolete]` (replaced by Aspire)
- HttpClient BaseAddress configured via DI using `services:cloud-api:https:0`

**Plan:** `.planning/indypos-overhaul/s3-rbac-implementation-plan.md`

### S4: CloudApi User Management Details (2026-03-10)

**Decision:** Admin CRUD endpoints instead of full ASP.NET Identity (passwords stay local at StoreHub).

**New Capabilities Added:**
- `users.read` - Read user data
- `users.create` - Create users
- `users.update` - Update users
- `users.deactivate` - Soft delete users

**API Endpoints:**

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/admin/users` | GET | List users (paginated, filterable) |
| `/admin/users/{id}` | GET | Get single user |
| `/admin/users` | POST | Create user |
| `/admin/users/{id}` | PUT | Update user |
| `/admin/users/{id}` | DELETE | Deactivate user (soft delete) |

**Files Created:**
- `Application/Abstractions/Cloud/Repositories/ICloudUserRepository.cs`
- `Application/UseCases/Cloud/Users/CreateUser/CreateCloudUserCommand.cs`
- `Application/UseCases/Cloud/Users/CreateUser/CreateCloudUserCommandHandler.cs`
- `Application/UseCases/Cloud/Users/UpdateUser/UpdateCloudUserCommand.cs`
- `Application/UseCases/Cloud/Users/UpdateUser/UpdateCloudUserCommandHandler.cs`
- `Application/UseCases/Cloud/Users/DeactivateUser/DeactivateCloudUserCommand.cs`
- `Application/UseCases/Cloud/Users/DeactivateUser/DeactivateCloudUserCommandHandler.cs`
- `Application/UseCases/Cloud/Users/GetUsers/GetCloudUsersQuery.cs`
- `Application/UseCases/Cloud/Users/GetUsers/GetCloudUsersQueryHandler.cs`
- `CloudApi/Infrastructure/Repositories/CloudUserRepository.cs`

**Tests Added:** 29 new tests (156 total)

**Key Design Decisions:**
- Keep `CloudUser` entity (no migration to IdentityUser)
- Version increment on every change (critical for StoreHub sync)
- Soft delete only (IsActive = false)
- No password management in cloud (stays local)
- SystemAdmin authorization via CanManageUsers policy

### Threat Mitigations

| Threat | Mitigation | Status |
|--------|------------|--------|
| Stolen POS device | Encrypted local DB, device identity | 🔴 Not Started |
| Credential theft | Salted hashing, rate limiting, lockouts | 🟡 Partial (hashing done) |
| API abuse | JWT validation, rate limiting | 🟡 Partial (JWT done) |
| Insider misuse | RBAC, audit logging | 🟡 Partial (RBAC done) |

**Deliverable:** Production-ready security with offline auth, RBAC, and audit trail

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

**Now:** Epic S (Security) in progress - S1, S2, S3, S4 complete 🟡
**Next:** S5 (RSA key signing) or Epic G (Desktop Integration)

### Completed This Session (2026-03-10)
1. ✅ Implemented capability-based RBAC (S3):
   - Created Capability constants and RoleCapabilities mapping
   - Added CapabilityAuthorizationHandler for ASP.NET Core
   - Protected StoreHub endpoints with policies (products, sales, sync)
   - Protected CloudApi admin endpoint with SystemAdminOnly policy
   - Added 21 unit tests for RoleCapabilities
2. ✅ Added Aspire service discovery for StoreHub → CloudApi:
   - AppHost now wires CloudApi reference to StoreHub
   - HttpClient uses service discovery URL instead of hardcoded BaseUrl
   - Marked CloudTokenOptions.BaseUrl as [Obsolete]
3. ✅ Implemented CloudApi user management (S4):
   - Added user management capabilities (users.read/create/update/deactivate)
   - Created ICloudUserRepository interface and CloudUserRepository
   - Created CreateCloudUserCommand/Handler with validation
   - Created UpdateCloudUserCommand/Handler with partial updates
   - Created DeactivateCloudUserCommand/Handler (soft delete)
   - Created GetCloudUsersQuery/Handler with pagination
   - Added admin endpoints: GET/POST/PUT/DELETE /admin/users
   - Added 29 unit tests for user management
4. ✅ All tests passing (156 total)

### Previous Session (2026-03-09)
1. ✅ Created IndyPOS.CloudApi project (F1)
2. ✅ Wired CloudApi into AppHost with cloud-db (F1a)
3. ✅ Added Scalar API documentation UI to StoreHub and CloudApi
4. ✅ Implemented POST /sync/events with idempotent ingestion (F2)
5. ✅ Defined InvoiceCompletedEvent contract with schema versioning
6. ✅ Enhanced CompleteSaleCommandHandler to emit rich transaction snapshot
7. ✅ Created Cloud domain entities (CloudInvoice, CloudInvoiceLine, etc.)
8. ✅ Added ProcessedEvent table for first-class idempotency
9. ✅ Added CloudDbContext with EF Core configurations
10. ✅ Added EventProcessor BackgroundService with idempotent processing
11. ✅ Implemented GET /master/products and GET /master/config (F4)
12. ✅ Implemented OAuth2 + OpenIddict authentication (F5):
    - Added OpenIddict server configuration with Client Credentials flow
    - Created TokenController for /oauth/token endpoint
    - Added store registration endpoint (POST /admin/stores/register)
    - Created CloudTokenService for StoreHub token caching
    - Created HttpCloudSyncClient with Bearer auth
    - Protected all endpoints with [Authorize]
13. ✅ Added 8 unit tests (54 total passing)

### Next Actions
1. Epic S: S5 (RSA key signing) or S6-S9 (LOW priority)
2. Epic G: Desktop Integration

---

## Statistics

- **Total Epics:** 9 (added Epic S: Security)
- **Completed Epics:** 7 (Epic 0, A, B, C, D, E, F)
- **In Progress Epics:** 1 (Epic S - 4/9 tasks done)
- **Total Tasks:** 53 (41 + 9 security + 3 desktop)
- **Completed:** 45
- **In Progress:** 0
- **Not Started:** 8 (Epic S: 5, Epic G: 3)
- **Overall Progress:** ~85% (Security + Desktop integration remaining)
- **Total Tests:** 156 (all passing)

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
- **Coding Style:** Method chaining uses vertical dot alignment (see CLAUDE.md)
- **DB Tools:** PgAdmin and DbGate configured as on-demand (WithExplicitStart)
- **Epic E complete:** SyncWorker + /sync/status endpoint implemented

## Reference Documentation

| Doc | Location | Purpose |
|-----|----------|---------|
| v1.4.0 Docs | `.planning/indypos-overhaul/IndyPOS_Docs_v1_4_0/` | Latest architecture specs |
| Security Spec | `.planning/indypos-overhaul/security/indypos_security_design_spec.md` | Security design guide |
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

**Last Session:** 2026-03-10
