# Implementation Roadmap - Epic Dependencies

Version: 2.0.0
Date: 2026-03-31
Status: ✅ 93% Complete (Epic H Done, S6-S9 and I remaining)

## Epic Dependency Graph

```
┌─────────────────────────────────────────────────────────────────┐
│  LEGEND                                                         │
│  ══════                                                         │
│  ╔════════╗                                                     │
│  ║ Epic ✅║  Epic complete                                      │
│  ╚════════╝                                                     │
│                                                                 │
│  ┌────────┐                                                     │
│  │ Epic   │  Epic in progress                                   │
│  └────────┘                                                     │
│                                                                 │
│  [Epic 🔴]   Epic not started                                   │
│                                                                 │
│  ────────►  Dependency (must complete first)                    │
└─────────────────────────────────────────────────────────────────┘


                        START
                          │
                          ▼
                  ╔═══════════════╗
                  ║   Epic 0  ✅  ║  Sprint 1
                  ║   Extract     ║  ✅ COMPLETE
                  ║   Business    ║
                  ║   Logic       ║
                  ╚═══════╤═══════╝
                          │
                          │ Enables clean separation
                          ▼
         ┌────────────────┴────────────────┐
         │                                 │
         ▼                                 ▼
╔═══════════════╗               ╔═══════════════╗
║   Epic B  ✅  ║               ║   Epic A  ✅  ║  Sprint 1
║   Remove      ║               ║   Prepare     ║  ✅ COMPLETE
║   Deprecated  ║               ║   Codebase    ║
║   PG Report   ║               ║   (StoreId)   ║
╚═══════╤═══════╝               ╚═══════╤═══════╝
         │                               │
         └───────────────┬───────────────┘
                         │ Both enable
                         ▼
                  ╔═══════════════╗
                  ║   Epic D  ✅  ║  Sprint 1
                  ║   Schema      ║  ✅ COMPLETE
                  ║   Design      ║
                  ║   (PublicId)  ║
                  ╚═══════╤═══════╝
                          │
                          │ Required for
                          ▼
                  ╔═══════════════╗
                  ║   Epic C  ✅  ║  Sprint 2
                  ║   StoreHub    ║  ✅ COMPLETE
                  ║   + Aspire    ║
                  ║   Service     ║
                  ╚═══════╤═══════╝
                          │
                          │ Enables
                          ▼
         ┌────────────────┴────────────────┐
         │                                 │
         ▼                                 ▼
╔═════════════════╗             ╔═════════════════╗
║   Epic E  ✅    ║             ║   Epic F  ✅    ║  Sprint 3-4
║   Outbox +      ║             ║   Cloud API     ║  ✅ COMPLETE
║   SyncWorker    ║             ║   + OAuth2      ║
║   (Background)  ║             ║   + OpenIddict  ║
╚════════╤════════╝             ╚════════╤════════╝
         │                               │
         │                               │
         └───────────┬───────────────────┘
                     │ Both required for
                     ▼
         ┌───────────┴───────────────────┐
         │                               │
         ▼                               ▼
┌─────────────────┐           ╔═════════════════╗
│   Epic S  🟡    │           ║   Epic G  ✅    ║  Sprint 5
│   Security      │           ║   Desktop       ║  ✅ G1, G3 COMPLETE
│   Hardening     │           ║   Integration   ║  ⏭️ G2 Deferred
│   (S1-S5 ✅)    │           ║   (Client)      ║
│   (S6-S9 🔴)    │           ╚════════╤════════╝
└─────────────────┘                    │
                                       │ Enables
                                       ▼
                              ╔═════════════════╗
                              ║   Epic H  ✅    ║  Sprint 6
                              ║   Testing &     ║  ✅ COMPLETE
                              ║   Rollout       ║
                              ║   (Production)  ║
                              ╚════════╤════════╝
                                       │
                                       ▼
                              [Epic I  🔴]        Post-Pilot
                              [Cloud Infra]      (when needed)
                              [DigitalOcean]
                                       │
                                       ▼
                                      END
```

---

## Current Status (2026-03-31)

```
╔═════════════════════════════════════════════════════════════════════════════╗
║                           IMPLEMENTATION STATUS                              ║
╠═════════════════════════════════════════════════════════════════════════════╣
║                                                                             ║
║   Epic 0: Extract Business Logic          ✅ COMPLETE (2026-03-06)         ║
║   Epic A: Prepare Codebase                ✅ COMPLETE (2026-03-06)         ║
║   Epic B: Remove Deprecated PG Report     ✅ COMPLETE (2026-03-06)         ║
║   Epic D: Schema Design                   ✅ COMPLETE (2026-03-06)         ║
║   Epic C: StoreHub + Aspire               ✅ COMPLETE (2026-03-08)         ║
║   Epic E: Outbox + SyncWorker             ✅ COMPLETE (2026-03-08)         ║
║   Epic F: CloudApi + OAuth2               ✅ COMPLETE (2026-03-09)         ║
║   Epic S: Security Hardening              🟡 S1-S5 ✅ | S6-S9 🔴           ║
║   Epic G: Desktop Integration             ✅ G1,G3 COMPLETE | G2 Deferred  ║
║   Epic H: Testing & Rollout               ✅ COMPLETE (2026-03-29)         ║
║   Epic I: Cloud Infrastructure            🔴 NOT STARTED (post-pilot)      ║
║                                                                             ║
║   Overall Progress: 93%                                                     ║
║   Tests Passing: 266+                                                       ║
║   Build Status: 0 Warnings, 0 Errors                                       ║
║                                                                             ║
╚═════════════════════════════════════════════════════════════════════════════╝
```

---

## Sprint Summary

```
Sprint 1 (Complete)
═══════════════════════════════════════════════════════════
╔═════════════╗   ╔═════════════╗   ╔═════════════╗   ╔═════════════╗
║   Epic 0 ✅ ║ → ║  Epic B ✅  ║ → ║  Epic A ✅  ║ → ║  Epic D ✅  ║
║ (Extract)   ║   ║  (Remove)   ║   ║  (Prepare)  ║   ║  (Schema)   ║
╚═════════════╝   ╚═════════════╝   ╚═════════════╝   ╚═════════════╝

Deliverables:
  ✅ Business logic extracted to Domain/Application layers
  ✅ Deprecated PG report code removed
  ✅ StoreId infrastructure added
  ✅ PostgreSQL schema designed with PublicId + StoreId


Sprint 2 (Complete)
═══════════════════════════════════════════════════════════
╔═════════════════════════════════════════════════════════╗
║                     Epic C ✅                           ║
║                   (StoreHub + Aspire)                   ║
║                                                         ║
║   • IndyPOS.StoreHub (ASP.NET Core)                     ║
║   • IndyPOS.AppHost (Aspire orchestrator)               ║
║   • IndyPOS.ServiceDefaults (health checks, telemetry)  ║
║   • GET /products, POST /sales/complete                 ║
╚═════════════════════════════════════════════════════════╝

Deliverables:
  ✅ StoreHub service with CQRS pattern
  ✅ .NET Aspire development environment
  ✅ PostgreSQL + PgAdmin + DbGate via Aspire
  ✅ Product and sale endpoints working


Sprint 3-4 (Complete)
═══════════════════════════════════════════════════════════
╔═════════════════════════════╗   ╔═════════════════════════════╗
║         Epic E ✅           ║   ║         Epic F ✅           ║
║   (Outbox + SyncWorker)     ║   ║   (CloudApi + OAuth2)       ║
╚═════════════════════════════╝   ╚═════════════════════════════╝

Deliverables:
  ✅ Outbox pattern for reliable sync
  ✅ SyncWorker background service
  ✅ CloudApi with idempotent event ingestion
  ✅ OAuth2/OpenIddict authentication
  ✅ Master data endpoints


Sprint 5 (Complete)
═══════════════════════════════════════════════════════════
╔═════════════════════════════╗   ╔═════════════════════════════╗
║      Epic S (Partial) 🟡     ║   ║        Epic G ✅            ║
║   S1-S5 ✅ | S6-S9 🔴        ║   ║   (Desktop Integration)     ║
╚═════════════════════════════╝   ╚═════════════════════════════╝

Deliverables:
  ✅ POS offline authentication (BCrypt, JWT)
  ✅ Local user cache (sync from cloud)
  ✅ Capability-based RBAC
  ✅ CloudApi user management
  ✅ RSA key signing + DPAPI secrets
  ✅ Desktop StoreHub client (IStoreHubClient)
  ✅ Product cache service
  ✅ StoreHub-based sale and login services
  ✅ Guid ID migration
  ⏭️ G2 deferred (tablet support, post-MAUI)


Sprint 6 (Complete)
═══════════════════════════════════════════════════════════
╔═════════════════════════════════════════════════════════╗
║                     Epic H ✅                           ║
║                (Testing & Rollout)                      ║
╚═════════════════════════════════════════════════════════╝

Deliverables:
  ✅ Integration tests (49 tests, WebApplicationFactory + Testcontainers)
  ✅ Migration tests (15 tests, SQLite → PostgreSQL verification)
  ✅ MigrationTool CLI (23 tests, Thai locale support)
  ✅ Pilot documentation (checklist, smoke tests, rollback plan)
  ✅ Operational runbook (troubleshooting, updates, monitoring)
```

---

## Remaining Work

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         REMAINING TASKS                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Epic S (Security - LOW Priority)                                           │
│  ─────────────────────────────────                                          │
│  • S6: Key rotation support (6-month rotation for JWT signing)              │
│  • S7: Security audit logging (login, permission changes)                   │
│  • S8: Rate limiting (API abuse protection)                                 │
│  • S9: Secrets management (covered by S5)                                   │
│                                                                             │
│  Epic I (Cloud Infrastructure - when multi-store sync needed)               │
│  ───────────────────────────────────────────────────────────                │
│  • I1: Provision DigitalOcean Droplet (Ubuntu, Docker, nginx)               │
│  • I2: Provision Managed PostgreSQL (Singapore, backups)                    │
│  • I3: Deploy CloudApi to Droplet (Docker Compose + SSL)                    │
│  • I4: Configure SyncWorker with real CloudApi                              │
│  • I5: Multi-store sync testing                                             │
│  • I6: Central reporting dashboard                                          │
│                                                                             │
│  Backlog                                                                    │
│  ───────                                                                    │
│  • Migration --sync-to-cloud flag (create outbox events for history)        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Epic Details

### Completed Epics

```
╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic 0: Extract Business Logic                                   ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • Product.GetTotal(), InvoiceProductDto.GetTotal()                         ║
║  • PayLaterPayment.RecordPayment(), RemainingAmount, WouldBeCompletedWith() ║
║  • CashFlowData.CalculateExpectedCash(), CalculateActualCash()              ║
║  • 24 unit tests added                                                      ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic A: Prepare Codebase                                         ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • IStoreIdentityService + StoreIdentityService                             ║
║  • Architecture docs and ASCII diagrams                                     ║
║  • docker-compose.yml for dev environment                                   ║
║  • 8 unit tests added                                                       ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic B: Remove Deprecated PG Report                              ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • Removed Persistence/Repositories/PostgreSql/ folder                      ║
║  • Removed UseCases/SalesReports/ and UseCases/PaymentsReports/             ║
║  • Removed Npgsql package                                                   ║
║  • Removed CloudDatabaseEnabled config                                      ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic D: Schema Design                                            ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • Core entities with UUID Id (Invoice, Payment, Product, etc.)             ║
║  • OutboxEvent and InventoryMovement entities                               ║
║  • StoreHubDbContext with EF Core configurations                            ║
║  • ADR-002 for entity organization strategy                                 ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic C: StoreHub + Aspire                                        ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • IndyPOS.StoreHub (ASP.NET Core API)                                      ║
║  • IndyPOS.AppHost (Aspire orchestrator)                                    ║
║  • IndyPOS.ServiceDefaults (health checks, OpenTelemetry)                   ║
║  • GET /products, POST /sales/complete endpoints                            ║
║  • PostgreSQL + PgAdmin + DbGate via Aspire                                 ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic E: Outbox + SyncWorker                                      ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • OutboxEvent entity + IOutboxRepository                                   ║
║  • SyncWorker BackgroundService with exponential backoff                    ║
║  • ICloudSyncClient interface (HttpCloudSyncClient impl)                    ║
║  • GET /sync/status observability endpoint                                  ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic F: CloudApi + OAuth2                                        ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  • IndyPOS.CloudApi (ASP.NET Core)                                          ║
║  • POST /sync/events with idempotent ingestion                              ║
║  • InvoiceCompletedEvent with rich payload                                  ║
║  • Cloud domain entities (CloudInvoice, etc.)                               ║
║  • EventProcessor BackgroundService                                         ║
║  • GET /master/products, GET /master/config endpoints                       ║
║  • OpenIddict OAuth2 (Client Credentials, JWT)                              ║
║  • Store registration (ClientId/Secret generation)                          ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic S: Security Hardening (Partial)                             🟡        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  ✅ S1: POS offline authentication (BCrypt, JWT)                            ║
║  ✅ S2: Local user cache (sync from cloud)                                  ║
║  ✅ S3: RBAC with capability-based authorization                            ║
║  ✅ S4: CloudApi user management (CRUD endpoints)                           ║
║  ✅ S5: RSA key signing + DPAPI secrets                                     ║
║  🔴 S6: Key rotation support (LOW priority)                                 ║
║  🔴 S7: Security audit logging (LOW priority)                               ║
║  🔴 S8: Rate limiting (LOW priority)                                        ║
║  🔴 S9: Secrets management (covered by S5)                                  ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic G: Desktop Integration                                      ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  ✅ G1: Desktop becomes StoreHub client                                     ║
║     • IStoreHubClient + StoreHubHttpClient                                  ║
║     • IProductCacheService + ProductCacheService                            ║
║     • StoreHubSaleService, StoreHubUserLogInService                         ║
║     • UserId simplified from int to Guid                                    ║
║     • E2E tests with WireMock                                               ║
║  ⏭️ G2: Tablet support (deferred to post-MAUI, 2027+)                       ║
║  ✅ G3: Decommission SQLite writes                                          ║
║     • IInventoryProductService + StoreHubInventoryProductService            ║
║     • Product write endpoints (CRUD + adjust-quantity)                      ║
║     • EF Core migrations                                                    ║
╚═════════════════════════════════════════════════════════════════════════════╝

╔═════════════════════════════════════════════════════════════════════════════╗
║  Epic H: Testing & Rollout                                        ✅        ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  ✅ H1: Integration tests (49 tests)                                        ║
║     • WebApplicationFactory + Testcontainers PostgreSQL                     ║
║     • Respawn for test isolation                                            ║
║     • Auth, Products, Sales, Reports, Sync endpoints                        ║
║  ✅ H2: Migration tests (15 + 23 = 38 tests)                                ║
║     • IndyPOS.Migration.Tests (verification)                                ║
║     • IndyPOS.MigrationTool.Tests (CLI tool)                                ║
║     • Thai locale support (Bogus)                                           ║
║  ✅ H3: Pilot preparation                                                   ║
║     • pilot-checklist.md, smoke-test.ps1, rollback-plan.md                  ║
║     • post-deployment-monitoring.md                                         ║
║  ✅ H4: Operational runbook                                                 ║
║     • troubleshooting-guide.md, update-procedure.md                         ║
║     • health-check.ps1, RUNBOOK.md                                          ║
╚═════════════════════════════════════════════════════════════════════════════╝
```

### Not Started Epics

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Epic I: Cloud Infrastructure                                    🔴         │
├─────────────────────────────────────────────────────────────────────────────┤
│  Dependencies: Epic H (pilot complete)                                      │
│  Priority: LOW (not needed until multi-store sync required)                 │
│  Estimated Cost: ~$20-30/month (Droplet + Managed PostgreSQL)               │
│                                                                             │
│  Tasks:                                                                     │
│  • I1: Provision DigitalOcean Droplet (Ubuntu, Docker, nginx)               │
│  • I2: Provision Managed PostgreSQL (Singapore, backups)                    │
│  • I3: Deploy CloudApi (Docker Compose + SSL)                               │
│  • I4: Configure SyncWorker to use real CloudApi                            │
│  • I5: Multi-store sync testing                                             │
│  • I6: Central reporting dashboard                                          │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Success Criteria

```
Epic 0: ✅ 24 unit tests passing, business logic in Domain/Application
Epic B: ✅ Build passes, no PG report code remains
Epic A: ✅ StoreId in config, docker-compose works
Epic D: ✅ Schema reviewed, EF Core configurations created
Epic C: ✅ StoreHub completes sale, stores in Postgres
Epic E: ✅ Events sync to cloud with retry
Epic F: ✅ Cloud receives and dedups events, OAuth2 working
Epic S: ✅ S1-S5 complete (offline auth, RBAC, RSA/DPAPI)
Epic G: ✅ Desktop uses StoreHub exclusively, SQLite writes decommissioned
Epic H: ✅ 266+ tests passing, pilot docs complete
Epic I: ⏳ Store 1 live in cloud, syncing (when needed)
```

---

**See `../implementation-status.md` for detailed task tracking**
