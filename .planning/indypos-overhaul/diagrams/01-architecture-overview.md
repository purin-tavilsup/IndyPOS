# Architecture Overview - Current vs Target

Version: 2.0.0
Date: 2026-03-31
Status: ✅ All Core Epics Complete - System Production-Ready

## Current Architecture (Legacy - SQLite Based)

```
┌─────────────────────────────────────────────────────────────┐
│                      STORE (LOCAL)                          │
│                                                             │
│  ┌──────────────────────────────────────────────────┐      │
│  │   Windows.Forms Desktop App                      │      │
│  │   (Single Terminal)                              │      │
│  │                                                  │      │
│  │   • Selling                                      │      │
│  │   • Inventory                                    │      │
│  │   • Reports                                      │      │
│  └───────────┬──────────────────────────┬───────────┘      │
│              │                          │                   │
│              ▼                          ▼                   │
│  ┌────────────────────┐    ┌──────────────────────┐       │
│  │   SQLite Database  │    │ [DEPRECATED]         │       │
│  │   (Store.db)       │    │ PostgreSQL Report    │       │
│  │                    │    │ Writer               │       │
│  │ • Invoice          │    │                      │       │
│  │ • InvoiceProduct   │    │ Pushes to:          │       │
│  │ • Payment          │    │ rungrat_report.*    │       │
│  │ • Product          │    └──────────┬───────────┘       │
│  │ • Inventory        │               │                   │
│  └────────────────────┘               │                   │
│                                       │                   │
└───────────────────────────────────────┼───────────────────┘
                                        │
                                        ▼
                        ┌───────────────────────────┐
                        │   CLOUD (Somewhere)       │
                        │                           │
                        │   PostgreSQL              │
                        │   (rungrat_report.*)      │
                        │                           │
                        │   ⚠️  Unreliable!         │
                        │   No retry, no queue      │
                        └───────────────────────────┘
```

### Problems with Current Architecture
- ❌ Single terminal only (SQLite file locking issues)
- ❌ No offline resilience (report writes fail if internet down)
- ❌ No retry mechanism
- ❌ Deprecated report schema
- ❌ Can't add tablet terminals

---

## Target Architecture (Offline-First with StoreHub) ✅ IMPLEMENTED

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         STORE (LOCAL)                                         │
│                                                                              │
│  ┌──────────────────┐           ┌──────────────────┐                        │
│  │  POS Client #1   │           │  POS Client #2   │                        │
│  │  (Desktop)       │           │  (Desktop/Tablet)│                        │
│  │                  │           │                  │                        │
│  │  Windows.Forms   │           │  (Future: MAUI)  │                        │
│  └────────┬─────────┘           └─────────┬────────┘                        │
│           │                               │                                  │
│           │ HTTP + JWT                    │ HTTP + JWT                       │
│           │                               │                                  │
│           ▼                               ▼                                  │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    StoreHub API (ASP.NET Core)                         │  │
│  │                                                                        │  │
│  │  Auth:                          API Endpoints:                         │  │
│  │  • POST /auth/login             • GET  /products                       │  │
│  │  • BCrypt password hashing      • POST /products                       │  │
│  │  • JWT token generation         • PUT  /products/{id}                  │  │
│  │                                 • DELETE /products/{id}                │  │
│  │  Sales:                         • POST /products/{id}/adjust-quantity  │  │
│  │  • POST /sales/complete         • POST /products/next-barcode          │  │
│  │                                                                        │  │
│  │  Reports:                       Sync:                                  │  │
│  │  • GET /reports/sales-summary   • GET /sync/status                     │  │
│  │  • GET /reports/invoices        • Background: SyncWorker               │  │
│  │  • GET /reports/pay-later                                              │  │
│  │  • GET /reports/product-sales                                          │  │
│  │                                                                        │  │
│  │  Authorization: RBAC (Capability-based)                                │  │
│  │  • Cashier: sales, product read                                        │  │
│  │  • Manager: + reports, inventory                                       │  │
│  │  • Admin: + user management                                            │  │
│  └───────────────────────────┬────────────────────────────────────────────┘  │
│                              │                                               │
│                              ▼                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │           PostgreSQL (Local - 127.0.0.1:5432)                          │  │
│  │           Database: indypos_storehub                                   │  │
│  │                                                                        │  │
│  │  Business Tables:          Sync Infrastructure:     Auth:              │  │
│  │  • invoice                 • outbox_event           • user             │  │
│  │  • invoice_line                                     • (BCrypt hash)    │  │
│  │  • payment                 Identity:                                   │  │
│  │  • pay_later               • id (UUID)              Settings:          │  │
│  │  • product                 • store_id               • store_setting    │  │
│  │  • inventory_movement      • created_utc                               │  │
│  │                            • last_modified_utc                         │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                              ▲                                               │
│                              │ Reads outbox                                  │
│                              │                                               │
│                      ┌───────┴────────┐                                      │
│                      │  SyncWorker    │                                      │
│                      │  (Background)  │                                      │
│                      │                │                                      │
│                      │ • Exponential  │                                      │
│                      │   backoff      │                                      │
│                      │ • Max 5 retries│                                      │
│                      └───────┬────────┘                                      │
│                              │                                               │
└──────────────────────────────┼───────────────────────────────────────────────┘
                               │
                               │ HTTPS + OAuth2 (Client Credentials)
                               │ POST /sync/events
                               │ Authorization: Bearer <JWT>
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                         CLOUD API (Singapore)                                 │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                    IndyPOS.CloudApi (ASP.NET Core)                     │  │
│  │                                                                        │  │
│  │  OAuth2 (OpenIddict):            Sync Endpoints:                       │  │
│  │  • POST /oauth/token             • POST /sync/events (idempotent)      │  │
│  │  • Client Credentials flow                                             │  │
│  │  • RSA-signed JWT (15min)        Master Data:                          │  │
│  │                                  • GET /master/products                │  │
│  │  Admin Endpoints:                • GET /master/config/{storeId}        │  │
│  │  • POST /admin/stores/register                                         │  │
│  │  • GET/POST/PUT/DEL /admin/users Background:                           │  │
│  │                                  • EventProcessor                      │  │
│  │  Authorization:                                                        │  │
│  │  • Scopes: sync.write, master.read                                     │  │
│  │  • RBAC: SystemAdmin for user management                               │  │
│  └───────────────────────────┬────────────────────────────────────────────┘  │
│                              │                                               │
│                              ▼                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │           PostgreSQL (Managed - DigitalOcean Singapore)                │  │
│  │           Database: indypos_cloud                                      │  │
│  │                                                                        │  │
│  │  Idempotency:              Aggregated Data:           Auth:            │  │
│  │  • synced_event            • cloud_invoice            • OpenIddict     │  │
│  │  • processed_event         • cloud_invoice_line         Applications   │  │
│  │    (event_id PK)           • cloud_payment            • cloud_user     │  │
│  │                            • cloud_inventory_movement                  │  │
│  │  Master Data:                                                          │  │
│  │  • cloud_product           Multi-tenant key:                           │  │
│  │  • cloud_store_config      • store_id + id                             │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Key Improvements

| Aspect | Current | Target | Status |
|--------|---------|--------|--------|
| **Terminals** | 1 per store | 2+ per store | ✅ Implemented |
| **Local DB** | SQLite (file-based) | PostgreSQL (concurrent) | ✅ Implemented |
| **Offline** | Report writes fail | ✅ Full offline operation | ✅ Implemented |
| **Sync** | Direct push (fragile) | Outbox + retry (reliable) | ✅ Implemented |
| **Identity** | Integer IDs only | GUID (UUID) + StoreId | ✅ Implemented |
| **Cloud Schema** | Deprecated reports | Clean transactional events | ✅ Implemented |
| **Auth** | None | OAuth2 + JWT + RBAC | ✅ Implemented |
| **Tablet Support** | ❌ Not possible | ✅ Architecture-ready | ⏭️ Deferred |

---

## Authentication & Authorization Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    TWO AUTHENTICATION DOMAINS                                │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────┐    ┌─────────────────────────────────┐
│     STORE (User Auth)               │    │     CLOUD (Machine Auth)        │
│                                     │    │                                 │
│  POS User Login:                    │    │  Store Registration:            │
│  ┌─────────────┐                    │    │  ┌─────────────┐                │
│  │ Username    │                    │    │  │ ClientId:   │                │
│  │ Password    │                    │    │  │ store_1     │                │
│  └──────┬──────┘                    │    │  │             │                │
│         │                           │    │  │ Secret:     │                │
│         ▼                           │    │  │ (DPAPI)     │                │
│  ┌─────────────┐                    │    │  └──────┬──────┘                │
│  │ StoreHub    │                    │    │         │                       │
│  │ /auth/login │                    │    │         ▼                       │
│  │             │                    │    │  ┌─────────────┐                │
│  │ BCrypt      │                    │    │  │ CloudApi    │                │
│  │ verify      │                    │    │  │ /oauth/token│                │
│  └──────┬──────┘                    │    │  │             │                │
│         │                           │    │  │ RSA sign    │                │
│         ▼                           │    │  └──────┬──────┘                │
│  ┌─────────────┐                    │    │         │                       │
│  │ JWT Token   │                    │    │         ▼                       │
│  │ (8 hours)   │                    │    │  ┌─────────────┐                │
│  │             │                    │    │  │ JWT Token   │                │
│  │ Claims:     │                    │    │  │ (15 min)    │                │
│  │ • sub       │                    │    │  │             │                │
│  │ • role      │                    │    │  │ Scopes:     │                │
│  │ • store_id  │                    │    │  │ • sync.write│                │
│  └─────────────┘                    │    │  │ • master.read               │
│                                     │    │  └─────────────┘                │
└─────────────────────────────────────┘    └─────────────────────────────────┘
```

---

## Network Resilience

### Current: Fragile
```
POS → [Internet Down] → ❌ Sale Fails (if report write enabled)
```

### Target: Resilient ✅
```
POS → StoreHub → Local DB → ✅ Sale Complete (instant)
                    ↓
                Outbox (queued)
                    ↓
                [Internet Down - waits, retries]
                    ↓
                [Internet Up - exponential backoff]
                    ↓
                CloudApi → Idempotent ingestion → ✅ Synced
```

---

## Deployment Model

### Per Store ✅ READY
```
┌────────────────────────────────────┐
│  Desktop PC (Main Terminal)         │
│                                     │
│  • Windows 10/11                    │
│  • PostgreSQL Service               │
│  • StoreHub Service (ASP.NET Core)  │
│  • POS Desktop App (Windows.Forms)  │
│                                     │
│  Secrets:                           │
│  • %ProgramData%\IndyPOS\Secrets\   │
│  • DPAPI encrypted                  │
└────────────────────────────────────┘

┌────────────────────────────────────┐
│  Desktop #2 (Optional)              │  ─┐
│                                     │   │
│  • Windows 10/11                    │   ├─ Same LAN
│  • POS Desktop App only             │   │  HTTP to StoreHub
│  • Calls StoreHub via HTTP          │   │
└────────────────────────────────────┘  ─┘
```

### Cloud (All Stores) 🔴 PENDING (Epic I)
```
┌────────────────────────────────────┐
│  DigitalOcean VPS (Singapore)       │
│                                     │
│  • Ubuntu Server                    │
│  • Docker / Docker Compose          │
│  • IndyPOS.CloudApi                 │
│  • nginx (reverse proxy, SSL)       │
│                                     │
│  Secrets:                           │
│  • INDYPOS_RSA_SIGNING_KEY (env)    │
└────────────────────────────────────┘

┌────────────────────────────────────┐
│  Managed PostgreSQL (Singapore)     │
│                                     │
│  • 1 GB RAM, 1 vCPU                 │
│  • Daily backups enabled            │
│  • SSL connections                  │
└────────────────────────────────────┘

Estimated cost: ~$20-30/month
```

---

## Development Environment (.NET Aspire) ✅

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    .NET Aspire Development                                │
│                                                                          │
│  Command: dotnet run --project src/IndyPOS.AppHost --launch-profile https│
│  Dashboard: https://localhost:17222                                      │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │                     IndyPOS.AppHost                                 ││
│  │                   (Aspire Orchestrator)                             ││
│  └───────────────────────────┬─────────────────────────────────────────┘│
│                              │                                           │
│       ┌──────────────────────┼──────────────────────┐                   │
│       │                      │                      │                   │
│       ▼                      ▼                      ▼                   │
│  ┌──────────┐         ┌──────────┐          ┌──────────┐               │
│  │ StoreHub │         │ CloudApi │          │PostgreSQL│               │
│  │   API    │         │   API    │          │Container │               │
│  │ :5000    │         │ :5001    │          │ :5432    │               │
│  └──────────┘         └──────────┘          └──────────┘               │
│       │                      │                      │                   │
│       │                      │                      │                   │
│       └──────────────────────┴──────────────────────┘                   │
│                              │                                           │
│                              ▼                                           │
│                    ┌──────────────────┐                                 │
│                    │ Service Discovery│                                 │
│                    │ + Health Checks  │                                 │
│                    │ + OpenTelemetry  │                                 │
│                    └──────────────────┘                                 │
│                                                                          │
│  On-Demand Tools (WithExplicitStart):                                   │
│  • PgAdmin - Database administration                                    │
│  • DbGate - Modern database UI                                          │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## API Endpoints Summary

### StoreHub API (Local)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/auth/login` | POST | None | User login, returns JWT |
| `/products` | GET | JWT | List products (filtered) |
| `/products` | POST | JWT+Manager | Create product |
| `/products/{id}` | PUT | JWT+Manager | Update product |
| `/products/{id}` | DELETE | JWT+Manager | Soft delete product |
| `/products/{id}/adjust-quantity` | POST | JWT+Manager | Adjust inventory |
| `/products/next-barcode` | POST | JWT+Manager | Generate barcode |
| `/sales/complete` | POST | JWT | Complete sale transaction |
| `/reports/sales-summary` | GET | JWT+Manager | Sales aggregation |
| `/reports/invoices` | GET | JWT+Manager | Invoice list |
| `/reports/invoices/{id}` | GET | JWT+Manager | Invoice detail |
| `/reports/pay-later` | GET | JWT+Manager | Accounts receivable |
| `/reports/product-sales` | GET | JWT+Manager | Product sales report |
| `/sync/status` | GET | JWT+Manager | Sync queue status |
| `/health`, `/alive` | GET | None | Health checks |

### CloudApi (Cloud)

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/oauth/token` | POST | ClientId+Secret | OAuth2 token |
| `/sync/events` | POST | OAuth2+sync.write | Idempotent event sync |
| `/master/products` | GET | OAuth2+master.read | Master product list |
| `/master/config/{storeId}` | GET | OAuth2+master.read | Store configuration |
| `/admin/stores/register` | POST | OAuth2+Admin | Register new store |
| `/admin/users` | GET/POST | OAuth2+Admin | User management |
| `/admin/users/{id}` | GET/PUT/DEL | OAuth2+Admin | User CRUD |

---

## Implementation Status

| Component | Status | Tests |
|-----------|--------|-------|
| StoreHub API | ✅ Complete | 202 unit tests |
| CloudApi | ✅ Complete | 49 integration tests |
| SyncWorker | ✅ Complete | Included in unit tests |
| Desktop Client | ✅ Complete | E2E with WireMock |
| OAuth2/OpenIddict | ✅ Complete | Auth endpoint tests |
| RBAC | ✅ Complete | 21 capability tests |
| Migration Tool | ✅ Complete | 38 migration tests |
| **Total** | **93%** | **266+ tests** |

---

**Next:** See `02-data-flow.md` for detailed flow diagrams
**Security:** See `12-security-auth-flow.md` for authentication details
