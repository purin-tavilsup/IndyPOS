# IndyPOS Offline‑First + Cloud Sync Plan (3 stores, 1–2 terminals/store)

Owner: POS Development Team  
Scope date: 2026‑02‑28

## 1) Goals & constraints

### Goals
- **Offline-first selling**: each store can sell even if the internet is down.
- **Two terminals per store** (desktop + tablet later): both can sell offline against the **same local store data**.
- **Cloud sync** for reporting, HQ dashboards, cross-store visibility, and later MCP/AI queries.
- Replace the **deprecated PostgreSQL report writer** with a new, more general sync pipeline.
- Introduce **globally unique identifiers** without breaking existing installs/data.

### Non-goals (for now)
- Tablet app implementation details (UI/UX) — we’ll enable the architecture so the tablet can plug in later.
- Complex bi-directional conflict editing of transactional data (we’ll avoid conflicts by design).

---

## 2) Recommended architecture

### 2.1 Store “Hub” (local-first)
Each store has a **Store Hub** running on the desktop PC (or later a small dedicated mini-PC).

**Store Hub contains:**
- Local database (recommended: **PostgreSQL locally** for 2 terminals)
- Local API service (ASP.NET Core) on the store LAN
- Background Sync Worker (push up, pull down)

**POS clients (desktop now, tablet later):**
- Use Store Hub API over LAN.
- If a client is temporarily disconnected from the hub (rare on LAN), it can show “hub offline” — but store can still sell on the hub machine.

> Why local Postgres now?
> - 2 separate terminals writing concurrently to the same data is much easier with Postgres than SQLite file locking/coordination.

### 2.2 Cloud (central)
- Central API (ASP.NET Core) + PostgreSQL hosted in **Singapore region** (DigitalOcean VPS or managed later).
- Cloud stores:
  - master data (products/prices/config) as “source of truth”
  - transactional data from stores (sales/payments/movements) as append-only events

### 2.3 Sync style
- **Outbox pattern** on the Store Hub DB for reliable delivery.
- Cloud ingestion is **idempotent** (safe to retry the same event multiple times).

---

## 3) What exists today (from IndyPOS codebase)

### Local DB today
- IndyPOS uses **SQLite** on Windows: `C:\ProgramData\IndyPOS\db\Store.db`
  - `IndyPOS.Infrastructure/Persistence/Repositories/SQLite/DbConnectionProvider.cs`

### Deprecated cloud report writer today
- `IndyPOS.Infrastructure/Persistence/Repositories/PostgreSql/*`
  - `ReportDbConnectionProvider` uses `Npgsql` and connection string `RungratPosDb`
  - `ReportRepository` inserts into `rungrat_report.sales_report` and `rungrat_report.payments_report`
- Handlers like `CreateSalesReportCommandHandler` generate report data and try to insert to PG; on failure they back up JSON files.

This report writer is deprecated and should be removed and replaced with the new sync pipeline.

---

## 4) Unique ID strategy (backward compatible)

### 4.1 Guid/Ulid decision
Pick one:
- **GUID**: built-in, simple, fine for this scale
- **ULID**: sortable and nicer for logs; requires a small library

Recommendation: **GUID first** (least friction). ULID can be added later if needed.

### 4.2 Backward compatibility approach
**Do NOT replace integer PKs immediately.** Instead:
1. Add a new column `PublicId` (GUID) to key tables.
2. Populate `PublicId` for existing rows (migration).
3. New APIs and sync events use `PublicId`.
4. Keep existing code paths that still reference int IDs working.
5. Gradually refactor domain/application code to prefer `PublicId`.

### 4.3 Suggested columns
For tables like Invoice/InvoiceProduct/InvoicePayment/Inventory/Users/etc.:
- `PublicId` UNIQUE NOT NULL (GUID)
- `StoreId` (string) NOT NULL
- `CreatedUtc`, `LastModifiedUtc` (UTC timestamps)

> When sending to cloud, always include `StoreId` + entity `PublicId`.

---

## 5) Data model adjustments (store side)

### 5.1 Introduce movement-based inventory
Avoid syncing “set quantity = X”. Instead sync **inventory movements**:
- Sale line -> movement `-qty`
- Restock -> movement `+qty`
- Adjustment -> movement `+/-qty`

Cloud computes stock by summing movements per store.

**Store DB additions:**
- `InventoryMovement`
  - `PublicId` (GUID, PK)
  - `StoreId`
  - `ProductPublicId`
  - `QuantityDelta`
  - `Reason` (Sale/Restock/Adjustment/Transfer)
  - `ReferencePublicId` (InvoicePublicId etc.)
  - `CreatedUtc`

### 5.2 Outbox table
- `Outbox`
  - `PublicId` (GUID, PK)
  - `StoreId`
  - `Type` (string)
  - `PayloadJson` (text)
  - `CreatedUtc`
  - `Attempts` (int)
  - `LastAttemptUtc` (nullable)
  - `NextRetryUtc` (nullable)
  - `Status` (Pending/Sent/Failed)

---

## 6) Cloud model (minimum viable)

### 6.1 Idempotent event ingestion
- `SyncedEvent`
  - `EventPublicId` (GUID, PK)
  - `StoreId`
  - `Type`
  - `ReceivedUtc`

If `EventPublicId` already exists -> return 200 OK (already processed).

### 6.2 Core transactional tables (cloud)
- `Invoice` (PublicId PK, StoreId, totals, createdUtc, etc.)
- `InvoiceLine`
- `Payment`
- `InventoryMovement` (same concept as store)

### 6.3 Master data tables (cloud)
- `Product` (PublicId PK, …)
- `PriceList` (optional)
- `StoreConfig` (per store, last updated)

---

## 7) Detailed task breakdown (actionable)

Below is the recommended implementation order. Each task should result in a PR.

### Epic A — Prepare the codebase
A1. **Create architecture docs folder**
- Add `/docs/architecture/` with:
  - `offline-first-overview.md`
  - `sync-events-contract.md`
  - `migration-guidelines.md`

A2. **Add “StoreId” concept**
- Add config in `appsettings.json` (Windows.Forms + future hub service):
  - `Store:Id`
  - `Store:Name` (optional)
- Add `IStoreIdentityService` in Application layer and implementation in Infrastructure.

A3. **Pick local hub DB**
- Decision: **Local Postgres** for store hub.
- Add docker-compose for store hub dev environment:
  - `postgres:16` container
  - volume for data persistence

Deliverable: documented decision + dev compose file.

---

### Epic B — Remove deprecated PG report feature
B1. **Remove PG report infrastructure registrations**
- In `IndyPOS.Infrastructure/ConfigureServices.cs`:
  - remove `IReportDbConnectionProvider` and `IReportRepository` registrations

B2. **Delete or archive deprecated PG repository code**
- Remove folder `IndyPOS.Infrastructure/Persistence/Repositories/PostgreSql/` (or move to `Deprecated/`)

B3. **Remove Npgsql package reference**
- Remove `Npgsql` PackageReference from `IndyPOS.Infrastructure.csproj` if no longer used.

B4. **Remove report write use cases**
- Remove:
  - `CreateSalesReportCommand*`
  - `CreatePaymentsReportCommand*`
  - any wiring that triggers them (currently commented handler)
- Decide whether to keep *local report calculation* (without cloud write). If local-only reports are useful, keep `ReportService` but delete cloud write handlers.

B5. **Delete config keys**
- Remove `RungratPosDb` connection string usage, and `CloudDatabaseEnabled` gating if it only existed for the old report path.

Deliverable: build passes; no PG report references remain.

---

### Epic C — Create the Store Hub service (new project)
C1. **Add new project**
- `src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj` (ASP.NET Core)
- Add it to `IndyPOS.sln`

C2. **Add local Postgres persistence for hub**
- EF Core or Dapper (choose one; recommendation: EF Core for migrations)
- Connection string stored locally (env/appsettings)
- Migrations folder under StoreHub project

C3. **Expose minimal endpoints for POS**
- `POST /sales/complete`
- `GET /products`
- `POST /inventory/adjust`
- `GET /health`

For now, desktop app can call these endpoints locally (localhost). Later tablet uses LAN IP.

C4. **Move “selling” to hub**
- Option 1 (fastest): reuse existing Application services inside hub and call them from controllers.
- Option 2 (cleaner): make desktop app a client that calls hub only.

Recommendation: **Option 1 first**, then refactor desktop to become a thin client later.

Deliverable: hub runs locally and can complete a sale into its local Postgres DB.

---

### Epic D — Unique IDs + schema migrations (store hub DB)
D1. **Add `PublicId` to core entities**
- Invoice, InvoiceLine, Payment, Product, InventoryMovement, User, etc.

D2. **Populate `PublicId` for existing data**
- For SQLite legacy DB: write a one-time migration/upgrade step:
  - if `PublicId` is null -> generate GUID and save
- For hub Postgres: new schema uses `PublicId` as primary identifier from day 1.

D3. **Backward compatible mapping**
- Maintain existing int keys in legacy SQLite (for old installs).
- When syncing to cloud, always use PublicId; int IDs stay internal.

D4. **Add `StoreId` to all outbound objects**
- Ensure created rows have StoreId set in hub DB.

Deliverable: tests prove old DB upgrades without data loss.

---

### Epic E — Outbox + reliable sync (store hub)
E1. **Create Outbox table**
- EF migration or DDL

E2. **Write Outbox events at commit point**
- On successful sale completion:
  - Create an `InvoiceCompleted` event containing invoice + lines + payments + movements (preferred: one event per invoice)
- On inventory adjustment:
  - Create `InventoryAdjusted` event (or general `InventoryMovementRecorded`)

E3. **Implement SyncWorker (BackgroundService)**
- Poll outbox every N seconds
- Batch send (e.g., 20 events)
- Retry with exponential backoff
- Persist attempt counts + next retry time
- Log failures clearly

E4. **Local observability**
- Add a small admin/status endpoint:
  - `GET /sync/status` -> pending count, last success time, last error
- Desktop UI can show “Synced / Pending (X)”

Deliverable: disconnect internet -> sales continue -> reconnect -> events sync.

---

### Epic F — Cloud API + Postgres (Singapore)
F1. **Create cloud API project**
- `src/IndyPOS.Cloud/IndyPOS.Cloud.csproj` (ASP.NET Core)
- Dockerfile + docker-compose for local dev

F2. **Idempotent event ingestion**
- `POST /sync/events`
- Check `SyncedEvent` table for EventPublicId
- If new -> process and insert SyncedEvent in same transaction

F3. **Process event types**
- `InvoiceCompleted` -> upsert invoice, lines, payments, movements
- `InventoryMovementRecorded` -> insert movement

F4. **Master data endpoints (pull down)**
- `GET /master/products?since=...`
- `GET /master/config?storeId=...`
- Store hub can poll or use “since token”

F5. **Auth**
- Simple store API key initially:
  - StoreId + shared secret
  - HMAC header or bearer token
- Rotate keys later.

Deliverable: cloud accepts events, stores them, and supports master data pull.

---

### Epic G — Legacy desktop (current Windows.Forms) integration plan
Because tablet is out of scope, we’ll stage this safely.

G1. **Stage 1: desktop becomes the hub host**
- Desktop app runs hub in-process OR launches hub as a Windows Service.
- Desktop UI calls localhost endpoints.

G2. **Stage 2: prepare for tablet**
- Allow hub to listen on LAN interface
- Add simple device auth for LAN clients (pairing code or token)

G3. **Decommission direct SQLite writes**
- After hub is stable, move remaining direct DB writes out of desktop UI layer.

Deliverable: desktop uses hub API for selling; local Postgres is the single source of truth.

---

### Epic H — Testing & rollout
H1. **Automated tests**
- Unit tests: outbox creation, event serialization
- Integration tests: hub DB + cloud API (docker-compose) round-trip
- Idempotency tests: same event posted twice -> no duplicates

H2. **Upgrade path tests**
- Take a real/backup SQLite Store.db from production
- Run upgrade tool -> verify PublicId populated and no data loss

H3. **Pilot rollout**
- Store 1: deploy hub + cloud sync, monitor 1 week
- Store 2–3: rollout after stability confirmed

H4. **Operational runbook**
- Backup plan for hub DB (local):
  - nightly pg_dump to external drive or cloud storage
- Monitoring:
  - disk space, service uptime, sync backlog size

---

## 8) Open decisions (pick early)
1. **Local DB**: Postgres on Store Hub (recommended) vs SQLite + file sharing (not recommended for 2 terminals).
2. **Identifier type**: GUID vs ULID (recommend GUID now).
3. **Event schema**: single “InvoiceCompleted” event vs multiple smaller events (recommend single event per invoice).
4. **Where hub runs**: in-process within desktop app vs separate Windows Service (recommend separate service long-term; in-process is acceptable for MVP).

---

## 9) Immediate next steps (suggested sprint 1)
1. Epic B (remove deprecated PG report feature)
2. Epic A (StoreId + docs + decision)
3. Epic C skeleton (StoreHub project + health endpoint + local Postgres dev compose)
4. Epic D initial (PublicId + StoreId in hub schema)

---

## Appendix — Notes on backward compatibility

### Legacy SQLite DB upgrade
- Add `PublicId` columns to existing SQLite tables.
- Write a small “DB upgrade” step that runs on app start:
  - detect missing column -> add it
  - detect null PublicId -> generate and fill
- Keep using old int IDs internally until the refactor is finished.

### Cloud compatibility
- Cloud never needs legacy int IDs.
- Cloud is keyed by `StoreId + PublicId` only.
