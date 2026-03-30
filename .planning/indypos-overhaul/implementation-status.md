# IndyPOS Overhaul - Implementation Status

**Last Updated:** 2026-03-29
**Last Session:** 2026-03-29
**Current Sprint:** Sprint 6
**Current Epic:** Epic H (Testing & Rollout) - ✅ COMPLETE
**Docs Version:** v1.4.0 (with .NET Aspire support)

---

## Sprint Overview

| Sprint | Focus | Status |
|--------|-------|--------|
| Sprint 1 | **Epic 0** ✅ + **Epic A** ✅ + **Epic B** ✅ + **Epic D** ✅ | 🟢 Complete |
| Sprint 2 | **Epic C** (StoreHub Service) ✅ | 🟢 Complete |
| Sprint 3 | **Epic E** (Outbox + Sync) ✅ | 🟢 Complete |
| Sprint 4 | **Epic F** (Cloud API) ✅ | 🟢 Complete |
| Sprint 5 | Epic S (Security) + Epic G (Desktop Integration) | 🟡 In Progress |
| Sprint 6 | Epic H (Testing & Rollout) | 🟢 Complete |

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
| S5 | RSA key signing + DPAPI secrets | 🟢 Complete | MEDIUM | RSA 2048+ for CloudApi, DPAPI for StoreHub |
| S6 | Key rotation support | 🔴 Not Started | LOW | 6-month rotation for JWT signing |
| S7 | Security audit logging | 🔴 Not Started | LOW | Login, permission changes, etc. |
| S8 | Rate limiting | 🔴 Not Started | LOW | API abuse protection |
| S9 | Secrets management | 🔴 Not Started | LOW | Covered by S5 (env vars + DPAPI) |

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

### S5: RSA Key Signing + DPAPI Secrets Details (2026-03-27)

**Two-part implementation** based on where secrets are stored:

**S5a: CloudApi RSA Signing Key (Linux/Cloud)**
- RSA 2048+ key loaded from `INDYPOS_RSA_SIGNING_KEY` environment variable
- Key format: PEM (PKCS#1 or PKCS#8), base64-encoded
- Falls back to development certificates when env var not set
- Automatic key ID generation from SHA256 hash of public key (for rotation)

**S5b: StoreHub ClientSecret with DPAPI (Windows)**
- `ISecretStorage` abstraction for platform-agnostic secret storage
- `DpapiSecretStorage` implementation using Windows DPAPI
- Secrets stored in `%ProgramData%\IndyPOS\Secrets\` as encrypted files
- `DataProtectionScope.LocalMachine` allows any user on the POS to access
- `SecureCloudTokenOptions` wraps `CloudTokenOptions` with secure secret retrieval
- Falls back to config value during migration/development

**Files Created:**
- `Application/Abstractions/Security/ISecretStorage.cs`
- `Infrastructure/Services/Security/DpapiSecretStorage.cs`
- `Infrastructure/Services/StoreHub/SecureCloudTokenOptions.cs`
- `scripts/generate-rsa-key.ps1` - Helper script for RSA key generation
- `Application.Tests/Infrastructure/Security/DpapiSecretStorageTests.cs` (10 tests)

**Files Modified:**
- `CloudApi/Infrastructure/Auth/OpenIddictExtensions.cs` - RSA key loading
- `Infrastructure/Services/StoreHub/CloudTokenService.cs` - Uses SecureCloudTokenOptions
- `Infrastructure/ConfigureServices.cs` - Registers DPAPI storage

**Tests Added:** 10 new tests (166 total)

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
**Status:** 🟢 G1 Complete (incl. E2E tests)
**Target:** Sprint 5
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| G1 | Desktop becomes hub client | 🟢 Complete | - | Full StoreHub client integration + E2E tests |
| G2 | Prepare for tablet | ⏭️ Deferred | - | Deferred to post-MAUI migration (2027+) |
| G3 | Decommission direct SQLite writes | 🟡 In Progress | - | Phase 1 complete (backend), Phases 2-7 remaining |

### G1: Desktop Hub Client Details (2026-03-27)

**Architecture:** WinForms → StoreHub API → PostgreSQL (Option 2: Full migration with local cache)

**New Abstractions:**
- `IStoreHubClient` - HTTP client for StoreHub API
- `IProductCacheService` - Local product cache with sync/clear

**New Implementations:**
- `StoreHubHttpClient` - HTTP client with JWT auth
- `ProductCacheService` - In-memory cache with barcode lookup
- `StoreHubSaleService` - ISaleService using StoreHub API
- `StoreHubUserLogInService` - IUserLogInService using StoreHub auth
- `StoreHubOptions` - Configuration for StoreHub mode

**Model Extensions:**
- `Product.StoreHubProductId` - UUID for StoreHub products
- `InventoryProductDto.StoreHubProductId` - Bridge for compatibility
- `IInvoiceInfo.StoreHubInvoiceId` - UUID for StoreHub invoices

**UserId Simplification (2026-03-27):**
- Changed `ILoggedInUser.UserId` from `int` to `Guid` (removed `StoreHubUserId`)
- Updated StoreHub entities to use `Guid UserId` (Invoice, CloudInvoice, InvoiceCompletedEvent)
- Legacy WinForms extracts int from deterministic Guid for SQLite compatibility
- All new users will be created in StoreHub with Guid IDs

**Configuration (appsettings.json):**
```json
{
  "StoreHub": {
    "Enabled": true,
    "BaseUrl": "http://localhost:5000",
    "AutoSyncProductsOnStartup": true,
    "TimeoutSeconds": 30
  }
}
```

**Flow:**
1. User launches WinForms app
2. Login form calls `IUserLogInService.LogInAsync()`
3. `StoreHubUserLogInService` calls StoreHub `/auth/login`
4. On success, JWT token cached in `IStoreHubClient`
5. Products auto-synced to `IProductCacheService`
6. Sales call `StoreHubSaleService.CompleteSaleAsync()` → StoreHub API

**Commits:**
- `10288f1` feat(desktop): add StoreHub client integration foundation (Epic G1)
- `73e6e35` feat(desktop): add StoreHub authentication flow (Epic G1e)
- `473da76` refactor: simplify UserId from int to Guid
- `b04e026` test(e2e): add development seeder and WireMock integration tests (Epic G1f)

**E2E Testing (G1f):**
- `DevelopmentDataSeeder` - Seeds test users and products in dev mode
- `StoreHubHttpClientTests` - 8 tests with mocked HTTP handler
- `StoreHubE2ETests` - 5 tests using WireMock for realistic API simulation
- Tests cover: login, get products, complete sale, health check, logout
- **Total: 179 tests passing**

**Deliverable:** Desktop uses hub API; local Postgres is single source of truth ✅

### G3: Decommission SQLite Writes - In Progress (2026-03-28)

**Goal:** Migrate all product write operations from SQLite to StoreHub API

**Design Decisions:**
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Inventory tracking | Movement-based | Audit trail, supports history/reports |
| ID strategy | Full Guid migration | Clean break, small UI footprint (~13 refs) |
| Concurrency | Last-write-wins | Simple, acceptable for 1-2 terminals |
| Soft delete | `IsActive = false` | Preserve history |

**Phase Progress:**

| Phase | Description | Status |
|-------|-------------|--------|
| 1.3 | InventoryMovement repository | ✅ Complete |
| 1.4 | StoreSetting entity + EF config | ✅ Complete |
| 1.1-1.2 | Extend ProductRepository (Update, Delete, ExistsByBarcode) | ✅ Complete |
| 1.5-1.6 | Capabilities + StoreHub endpoints | ✅ Complete |
| 2 | Extend IStoreHubClient + cache invalidation | ✅ Complete |
| 3 | Migrate to Guid IDs | ✅ Complete |
| 4-5 | Create IInventoryProductService + update WinForms | ✅ Complete |
| 6 | Database migration for StoreSetting | ✅ Complete |
| 7 | Unit tests for StoreHubInventoryProductService | ✅ Complete |

**New Files Created (Phase 6-7):**
- `20260329060235_AddStoreSettingTable.cs` - Initial EF Core migration for all StoreHub tables
- `StoreHubInventoryProductServiceTests.cs` - 10 unit tests for inventory service
- Updated `DevelopmentDataSeeder.cs` - Seeds BarcodeCounter setting

**New Files Created (Phase 4-5):**
- `IInventoryProductService.cs` - Service interface for inventory operations
- `StoreHubInventoryProductService.cs` - StoreHub-based implementation
- Updated `AddNewInventoryProductForm.cs` - Uses IInventoryProductService
- Updated `UpdateInventoryProductForm.cs` - Uses IInventoryProductService
- Updated `AddNewInventoryProductWithCustomBarcodeForm.cs` - Uses IInventoryProductService
- Updated `InventoryPanel.cs` - Uses IInventoryProductService
- Updated `ConfigureServices.cs` - Registered StoreHubInventoryProductService

**New Files Created (Phase 1):**
- `IInventoryMovementRepository.cs` + `InventoryMovementRepository.cs`
- `IStoreSettingRepository.cs` + `StoreSettingRepository.cs`
- `StoreSetting.cs` + `StoreSettingConfiguration.cs`
- `CreateProductCommand.cs` + `CreateProductCommandHandler.cs`
- `UpdateProductCommand.cs` + `UpdateProductCommandHandler.cs`
- `DeleteProductCommand.cs` + `DeleteProductCommandHandler.cs`
- `AdjustProductQuantityCommand.cs` + `AdjustProductQuantityCommandHandler.cs`
- `GenerateBarcodeQuery.cs` + `GenerateBarcodeQueryHandler.cs`
- `AdjustQuantityRequest.cs`

**New Capabilities:**
- `products.manage` - Create/Update/Delete products
- `inventory.adjust` - Adjust product quantities

**New StoreHub Endpoints:**
- `POST /products` - Create product
- `PUT /products/{id}` - Update product
- `DELETE /products/{id}` - Soft delete product
- `POST /products/{id}/adjust-quantity` - Adjust quantity via movement
- `POST /products/next-barcode` - Generate next barcode

**Other Changes:**
- Added `StoreCode` property to `IStoreIdentityService` for barcode generation
- Extended `IProductRepository` with `UpdateAsync`, `SoftDeleteAsync`, `ExistsByBarcodeAsync`
- Registered `IInventoryMovementRepository` and `IStoreSettingRepository` in DI

**Tests:** 202 passing ✅

---

## Epic H: Testing & Rollout ✅ COMPLETE

**Goal:** Comprehensive testing and pilot deployment
**Status:** 🟢 Complete
**Completed:** 2026-03-29
**Priority:** MEDIUM

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| H1 | Automated tests | 🟢 Complete | - | WebApplicationFactory + Testcontainers PostgreSQL |
| H2 | Upgrade path tests | 🟢 Complete | - | SQLite → Postgres migration tests |
| H3 | Pilot rollout | 🟢 Complete | - | Checklists, smoke tests, rollback plan |
| H4 | Operational runbook | 🟢 Complete | - | Troubleshooting, update procedure, monitoring |

### H1: Integration Tests Details (2026-03-29)

**Test Project:** `IndyPOS.StoreHub.IntegrationTests`
- WebApplicationFactory for realistic API testing
- Testcontainers PostgreSQL for real database tests
- Respawn for test isolation (database state reset)
- xUnit test collections for sequential execution

**Tests Added:**
| Test Class | Count | Description |
|------------|-------|-------------|
| AuthEndpointTests | 10 | Login, registration, token validation |
| ProductsEndpointTests | 12 | CRUD operations, filtering, pagination |
| SalesEndpointTests | 7 | Complete sale flow, validations |
| ReportsEndpointTests | 14 | All report endpoints + authorization |
| SyncEndpointTests | 6 | Sync status endpoint + authorization |

**Total: 49 integration tests**

### H2: Migration Tests Details (2026-03-29)

**Test Project:** `IndyPOS.Migration.Tests`
- `MigrationTestFixture` - Manages SQLite source + PostgreSQL target
- `SqliteTestDataSeeder` - Seeds test data matching legacy schema
- `MigrationService` - Migrates products and invoices with ID mapping

**Tests Added:**
| Test Class | Count | Description |
|------------|-------|-------------|
| ProductMigrationTests | 9 | Products, stock, group pricing, unicode |
| InvoiceMigrationTests | 6 | Invoices, payments, inventory deductions |

**Total: 15 migration tests**

### H3: Pilot Documentation Details (2026-03-29)

**Files Created in `docs/operations/`:**
| File | Description |
|------|-------------|
| `pilot-checklist.md` | Step-by-step deployment guide with verifications |
| `smoke-test.ps1` | Automated health verification script |
| `rollback-plan.md` | Emergency recovery procedure (RTO: 30min) |
| `post-deployment-monitoring.md` | Metrics, alerts, maintenance procedures |

### H4: Operational Runbook Details (2026-03-29)

**Files Created in `docs/operations/`:**
| File | Description |
|------|-------------|
| `troubleshooting-guide.md` | Common issues and resolutions |
| `update-procedure.md` | How to apply StoreHub updates |
| `health-check.ps1` | Scheduled monitoring script (Event Log) |
| `RUNBOOK.md` | Comprehensive operations reference |

**Deliverable:** Production-ready system; pilot documentation complete ✅

**Test Summary:**
- 202 unit tests (Application.Tests)
- 49 integration tests (StoreHub.IntegrationTests)
- 15 migration tests (Migration.Tests)
- **Total: 266 tests passing**

---

## Epic I: Cloud Infrastructure & Multi-Store Sync

**Goal:** Deploy cloud infrastructure and enable cross-store synchronization
**Status:** 🔴 Not Started
**Target:** Post-pilot (after H3)
**Priority:** LOW (until multi-store sync needed)

### Prerequisites
- ✅ Epic F (CloudApi) - already implemented
- ⏳ Epic H3 (Pilot rollout) - must complete first

### Tasks

| Task | Description | Status | PR | Notes |
|------|-------------|--------|-----|-------|
| I1 | Provision DigitalOcean Droplet | 🔴 Not Started | - | Ubuntu, Docker, nginx |
| I2 | Provision DO Managed PostgreSQL | 🔴 Not Started | - | Singapore region, backups enabled |
| I3 | Deploy CloudApi to Droplet | 🔴 Not Started | - | Docker Compose + SSL |
| I4 | Configure SyncWorker to use real CloudApi | 🔴 Not Started | - | Replace stub client |
| I5 | Multi-store sync testing | 🔴 Not Started | - | Verify events flow correctly |
| I6 | Central reporting dashboard | 🔴 Not Started | - | Aggregate reports across stores |

### Cloud Infrastructure Specs
| Resource | Spec | Notes |
|----------|------|-------|
| **Droplet** | Basic Premium AMD | 2 GB RAM, 1 vCPU, 50 GB SSD |
| **Managed PostgreSQL** | Smallest tier | 1 GB RAM, 1 vCPU, 10-30 GB disk |
| **Region** | Singapore | Closest to Thailand |

- **Not needed until:** Multi-store sync or central reporting required
- **Monthly cost:** ~$20-30 USD (Droplet ~$14 + Managed PG ~$15)

**Deliverable:** Cloud infrastructure live; stores syncing to central database

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

**Now:** Epic H (Testing & Rollout) - **COMPLETE** ✅
**Next:** Epic I (Cloud Infrastructure) or remaining Epic S tasks (S6-S9)

### Completed This Session (2026-03-29)

1. ✅ **Solution Folder Reorganization**
   - Organized 14 projects into logical solution folders
   - Fixed all NuGet vulnerabilities and version conflicts

2. ✅ **Package Security Updates**
   - Fixed Azure.Identity 1.3.0 vulnerability → 1.13.2
   - Fixed KubernetesClient vulnerability → 17.0.14
   - Aligned all Microsoft.Extensions.* packages to 10.0.5
   - Aligned Microsoft.EntityFrameworkCore to 10.0.5
   - **Build: 0 Warnings, 0 Errors**

3. ✅ **H2: Migration Tool with Thai Locale Support**
   - Created `IndyPOS.MigrationTool` console app (SQLite → PostgreSQL)
   - Created `IndyPOS.MigrationTool.Tests` with Testcontainers
   - Added Thai product names and user names for realistic test data
   - 23 migration tests passing

**Total: 225+ tests passing**

### Solution Folder Structure

```
📁 Core
├── IndyPOS.Domain
├── IndyPOS.Application
└── IndyPOS.Infrastructure
📁 DesktopApp
└── IndyPOS.Windows.Forms
📁 Services
├── IndyPOS.StoreHub
└── IndyPOS.CloudApi
📁 DevAppHost
├── IndyPOS.AppHost
└── IndyPOS.ServiceDefaults
📁 Tools
└── IndyPOS.MigrationTool
📁 Tests
├── 📁 Core → IndyPOS.Application.Tests
├── 📁 DesktopApp → IndyPOS.Windows.Forms.Tests
├── 📁 Services → IndyPOS.StoreHub.IntegrationTests
├── 📁 Tools → IndyPOS.Migration.Tests, IndyPOS.MigrationTool.Tests
└── IndyPOS.Mock
```

### Previous Session (2026-03-29)

1. ✅ **H1: Integration Tests**
   - Created `IndyPOS.StoreHub.IntegrationTests` project
   - WebApplicationFactory + Testcontainers PostgreSQL + Respawn
   - 49 integration tests (Auth, Products, Sales, Reports, Sync)

2. ✅ **H2: Migration Tests**
   - Created `IndyPOS.Migration.Tests` project
   - SQLite → PostgreSQL migration verification
   - 15 migration tests (Products, Invoices)

3. ✅ **H3: Pilot Preparation**
   - `pilot-checklist.md` - Step-by-step deployment guide
   - `smoke-test.ps1` - Automated health verification
   - `rollback-plan.md` - Emergency recovery (RTO: 30min)
   - `post-deployment-monitoring.md` - Metrics and alerts

4. ✅ **H4: Operational Runbook**
   - `troubleshooting-guide.md` - Common issues and resolutions
   - `update-procedure.md` - How to apply updates
   - `health-check.ps1` - Scheduled monitoring script
   - `RUNBOOK.md` - Comprehensive operations reference

### Completed Previous Session (2026-03-28)
1. ✅ **G3 Phase 6-7: Database Migration + Unit Tests**
   - Created initial EF Core migration for all StoreHub tables
   - Added BarcodeCounter seeding to `DevelopmentDataSeeder`
   - Added 10 unit tests for `StoreHubInventoryProductService`

2. ✅ **G3 Phase 4-5: IInventoryProductService + WinForms Migration**
   - Created `IInventoryProductService` interface for inventory operations
   - Implemented `StoreHubInventoryProductService` using StoreHub API + cache
   - Updated all 4 WinForms inventory forms to use IInventoryProductService

3. ✅ **G3 Phases 1-3** (previous commits):
   - Phase 1: StoreHub Product Write API (backend complete)
   - Phase 2: Extended IStoreHubClient + cache invalidation
   - Phase 3: Migrated to Guid IDs with LegacyIdHelper

### New Product Write Endpoints
| Endpoint | Description |
|----------|-------------|
| `POST /products` | Create product with initial stock |
| `PUT /products/{id}` | Update product |
| `DELETE /products/{id}` | Soft delete (IsActive=false) |
| `POST /products/{id}/adjust-quantity` | Adjust via movement |
| `POST /products/next-barcode` | Generate next barcode |

### Previous Session (2026-03-27 - Report API)
1. ✅ Implemented Report API for StoreHub dashboard:
   - Created clean DTOs: `SalesSummaryDto`, `PaymentBreakdownDto`, `TopProductDto`, `InvoiceSummaryDto`, `InvoiceDetailDto`, `PayLaterSummaryDto`, `ProductSalesDto`, `PagedResult<T>`
   - Created CQRS queries with DateOnly parameters (not legacy TimePeriod enum)
   - Implemented 5 query handlers in Infrastructure layer (EF Core)
   - Added `Capability.ReportsView` for StoreManager and SystemAdmin
   - Added `CanViewReports` authorization policy
   - Created 5 report endpoints in StoreHub
   - Added Bruno API collection for reports (5 `.bru` files)
   - Added 13 unit tests with EF Core InMemory
   - **Total: 192 tests passing**

### Report API Endpoints
| Endpoint | Description |
|----------|-------------|
| `GET /reports/sales-summary` | Sales aggregation with payment breakdown |
| `GET /reports/invoices` | Paginated invoice list |
| `GET /reports/invoices/{id}` | Invoice detail with lines & payments |
| `GET /reports/pay-later` | Accounts receivable by customer |
| `GET /reports/product-sales` | Product sales by date range |

### Previous Session (2026-03-27)
1. ✅ Implemented RSA key signing for CloudApi (S5a)
2. ✅ Implemented DPAPI secret storage for StoreHub (S5b)
3. ✅ Implemented StoreHub client integration (G1):
   - Created `IStoreHubClient` + `StoreHubHttpClient`
   - Created `IProductCacheService` + `ProductCacheService`
   - Created `StoreHubSaleService` (replaces legacy SaleService)
   - Created `StoreHubUserLogInService` (replaces legacy UserLogInService)
   - Added `StoreHubOptions` for config-based mode switching
   - Extended models with StoreHub IDs (Product, Invoice, User)
4. ✅ Simplified UserId from int to Guid:
   - `ILoggedInUser.UserId` changed from int to Guid
   - Removed `StoreHubUserId` property (merged into `UserId`)
   - Updated StoreHub entities (Invoice, CloudInvoice, InvoiceCompletedEvent)
   - Legacy code extracts int from deterministic Guid for SQLite
   - Decision: All new users created in StoreHub with Guid IDs
5. ✅ Added E2E testing (G1f):
   - `DevelopmentDataSeeder` for test users/products
   - `StoreHubHttpClientTests` (8 tests with mocked HTTP)
   - `StoreHubE2ETests` (5 tests with WireMock)
   - Total: 179 tests passing

### Previous Session (2026-03-10)
1. ✅ Implemented capability-based RBAC (S3)
2. ✅ Added Aspire service discovery for StoreHub → CloudApi
3. ✅ Implemented CloudApi user management (S4)

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
1. **Epic I:** Cloud Infrastructure deployment (post-pilot, when multi-store sync needed)
2. **Epic S:** S6-S9 (LOW priority - key rotation, audit logging, rate limiting, secrets management)

---

## Statistics

- **Total Epics:** 10 (added Epic I: Cloud Infrastructure)
- **Completed Epics:** 8 (Epic 0, A, B, C, D, E, F, H)
- **In Progress Epics:** 2 (Epic S - 5/9, Epic G - G1 ✅, G3 ✅)
- **Total Tasks:** 56 (41 + 9 security + 3 desktop + 4 testing - 1 deferred)
- **Completed:** 52
- **In Progress:** 0
- **Deferred:** 1 (G2 - tablet prep, post-MAUI)
- **Not Started:** 4 (Epic S: S6-S9)
- **Overall Progress:** ~93% (H complete, only S6-S9 remaining)
- **Total Tests:** 225+ (all passing)
- **Build Status:** 0 Warnings, 0 Errors ✅

## Package Versions (2026-03-29)

| Package | Version | Notes |
|---------|---------|-------|
| Microsoft.EntityFrameworkCore | 10.0.5 | Aligned across all projects |
| Microsoft.Extensions.* | 10.0.5 | Aligned across all projects |
| Azure.Identity | 1.13.2 | Fixed vulnerability |
| KubernetesClient | 17.0.14 | Fixed vulnerability GHSA-w7r3-mgwf-4mqq |
| Aspire.* | 9.3.0 | SDK and hosting packages |
| Testcontainers.PostgreSql | 4.3.0 | Integration tests |
| Bogus | 35.6.1 | Fake data generation |

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
| Solution Layout | See Solution Folder Structure section above | Project organization |
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

**Last Session:** 2026-03-29
