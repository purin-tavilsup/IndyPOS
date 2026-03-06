# Architecture Overview - Current vs Target

Version: 1.0.0
Date: 2026-03-03

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

## Target Architecture (Offline-First with StoreHub)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         STORE (LOCAL)                                    │
│                                                                          │
│  ┌──────────────────┐           ┌──────────────────┐                   │
│  │  POS Client #1   │           │  POS Client #2   │                   │
│  │  (Desktop)       │           │  (Tablet/Desktop)│                   │
│  └────────┬─────────┘           └─────────┬────────┘                   │
│           │                               │                             │
│           │ HTTP/LAN                      │ HTTP/LAN                    │
│           │                               │                             │
│           ▼                               ▼                             │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │            StoreHub API (Windows Service)                   │       │
│  │                                                             │       │
│  │  Controllers:                  Background Services:         │       │
│  │  • POST /sales/complete        • SyncWorker                │       │
│  │  • GET  /products              • MasterDataSync (pull)     │       │
│  │  • POST /inventory/adjust      • HealthCheck               │       │
│  │  • GET  /sync/status                                       │       │
│  └───────────────────┬─────────────────────────────────────────┘       │
│                      │                                                  │
│                      ▼                                                  │
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │         PostgreSQL (Local - 127.0.0.1:5432)                  │      │
│  │         Database: indypos_storehub                           │      │
│  │                                                              │      │
│  │  Business Tables:        Sync Infrastructure:               │      │
│  │  • invoice               • outbox_event                     │      │
│  │  • invoice_line          • synced_master_data               │      │
│  │  • payment                                                  │      │
│  │  • product               Columns:                           │      │
│  │  • inventory_movement    • public_id (GUID)                 │      │
│  │                          • store_id                         │      │
│  │                          • created_utc                      │      │
│  │                          • last_modified_utc                │      │
│  └──────────────────────────────────────────────────────────────┘      │
│                      ▲                                                  │
│                      │ Reads outbox                                     │
│                      │                                                  │
│              ┌───────┴────────┐                                         │
│              │  SyncWorker    │                                         │
│              │  (Background)  │                                         │
│              └───────┬────────┘                                         │
│                      │                                                  │
└──────────────────────┼──────────────────────────────────────────────────┘
                       │
                       │ HTTPS (retry-safe)
                       │ POST /sync/events
                       │
                       ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                    CLOUD API (Singapore)                                 │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │              IndyPOS.Cloud API                              │        │
│  │                                                             │        │
│  │  Endpoints:                                                 │        │
│  │  • POST /sync/events          (idempotent ingestion)       │        │
│  │  • GET  /master/products      (pull down)                  │        │
│  │  • GET  /master/config        (per store)                  │        │
│  │  • GET  /reports/dashboard    (HQ view)                    │        │
│  └───────────────────┬─────────────────────────────────────────┘        │
│                      │                                                  │
│                      ▼                                                  │
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │      PostgreSQL (Central - Managed/DigitalOcean)             │      │
│  │      Database: indypos_cloud                                 │      │
│  │                                                              │      │
│  │  Deduplication:           Transactional Data:               │      │
│  │  • synced_event           • invoice (all stores)            │      │
│  │    (event_public_id PK)   • invoice_line                    │      │
│  │                           • payment                         │      │
│  │  Master Data:             • inventory_movement              │      │
│  │  • product (source)                                         │      │
│  │  • price_list             Keyed by:                         │      │
│  │  • store_config           • store_id + public_id            │      │
│  └──────────────────────────────────────────────────────────────┘      │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Key Improvements

| Aspect | Current | Target |
|--------|---------|--------|
| **Terminals** | 1 per store | 2+ per store |
| **Local DB** | SQLite (file-based) | PostgreSQL (concurrent) |
| **Offline** | Report writes fail | ✅ Full offline operation |
| **Sync** | Direct push (fragile) | Outbox + retry (reliable) |
| **Identity** | Integer IDs only | GUID (PublicId) + StoreId |
| **Cloud Schema** | Deprecated reports | Clean transactional events |
| **Tablet Support** | ❌ Not possible | ✅ Architecture-ready |

---

## Network Resilience

### Current: Fragile
```
POS → [Internet Down] → ❌ Sale Fails (if report write enabled)
```

### Target: Resilient
```
POS → StoreHub → Local DB → ✅ Sale Complete
                  ↓
              Outbox (queued)
                  ↓
              [Internet Down - waits]
                  ↓
              [Internet Up - retry]
                  ↓
              Cloud ✅
```

---

## Deployment Model

### Per Store
```
┌────────────────────────────────┐
│  Desktop PC (Main Terminal)    │
│                                │
│  • Windows 10/11               │
│  • PostgreSQL Service          │
│  • StoreHub Service            │
│  • POS Desktop App             │
└────────────────────────────────┘

┌────────────────────────────────┐
│  Tablet/Desktop #2             │  ─┐
│                                │   │
│  • Windows 10/11 or Android    │   ├─ Same LAN
│  • POS App (calls StoreHub)    │   │
└────────────────────────────────┘  ─┘
```

### Cloud (All Stores)
```
┌────────────────────────────────┐
│  DigitalOcean VPS (Singapore)  │
│                                │
│  • Ubuntu Server               │
│  • Docker / Docker Compose     │
│  • IndyPOS.Cloud API           │
│  • PostgreSQL (Managed)        │
└────────────────────────────────┘
```

---

**Next:** See `02-data-flow.md` for detailed flow diagrams
