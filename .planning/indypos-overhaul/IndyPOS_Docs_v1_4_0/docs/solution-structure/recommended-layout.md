# Recommended Repo / Solution Layout

Version: 1.4.0
Updated: 2026-02-28

This document proposes a repo structure that fits the current IndyPOS codebase and the planned future architecture.

## Goals

- Preserve existing Windows Forms app while we migrate
- Introduce StoreHub and Cloud services cleanly
- Add Aspire without disrupting production deployment
- Keep domain/application code reusable across local and cloud services
- Make EF Core migrations and operations easy to find

---

## Recommended top-level structure

```text
IndyPOS/
  docs/
    architecture/
    cloud/
    database/
    decisions/
    examples/
    migration/
    operations/
    solution-structure/
    storehub/

  src/
    IndyPOS.Domain/
    IndyPOS.Application/
    IndyPOS.Infrastructure/

    IndyPOS.Windows.Forms/
    IndyPOS.StoreHub/
    IndyPOS.CloudApi/
    IndyPOS.SyncWorker/

    IndyPOS.AppHost/
    IndyPOS.ServiceDefaults/

  tests/
    IndyPOS.Domain.Tests/
    IndyPOS.Application.Tests/
    IndyPOS.Infrastructure.Tests/
    IndyPOS.StoreHub.IntegrationTests/
    IndyPOS.CloudApi.IntegrationTests/
```

---

## Project responsibilities

### `IndyPOS.Domain`
Contains:
- entities
- value objects
- enums
- domain rules
- domain events (if added later)

Examples:
- `Invoice`
- `InvoiceLine`
- `Payment`
- `InventoryMovement`
- `Product`
- `Store`

### `IndyPOS.Application`
Contains:
- use cases
- commands / queries
- interfaces
- DTOs
- validation

Examples:
- `CompleteSaleCommand`
- `SyncInvoiceCommand`
- `GetProductsQuery`
- `IStoreIdentityService`
- `IClock`

### `IndyPOS.Infrastructure`
Contains:
- EF Core DbContexts
- repository implementations
- external service adapters
- cloud sync client
- PostgreSQL and SQLite upgrade utilities
- file / backup helpers

Examples:
- `StoreHubDbContext`
- `CloudDbContext`
- `OutboxRepository`
- `CloudSyncClient`

### `IndyPOS.Windows.Forms`
Contains:
- existing POS UI
- local presentation logic
- API client to StoreHub
- transitional adapters while direct DB writes are removed

Important:
- long term, this project should stop writing directly to the database.
- it should call StoreHub API instead.

### `IndyPOS.StoreHub`
Contains:
- ASP.NET Core local API
- sale endpoints
- product endpoints
- sync status endpoints
- local background jobs if needed

This becomes the **local source of truth service** in each store.

### `IndyPOS.CloudApi`
Contains:
- cloud sync ingestion API
- master data endpoints
- HQ/reporting-facing endpoints
- auth for store-to-cloud communication

### `IndyPOS.SyncWorker`
Contains:
- background worker for cloud sync
- outbox polling
- retry/backoff logic
- optional pull-sync for master data

This can run:
- inside StoreHub initially, or
- as a separate service later

### `IndyPOS.AppHost`
Aspire development orchestrator.

Runs:
- CloudApi
- SyncWorker
- PostgreSQL
- future MCP/reporting services

### `IndyPOS.ServiceDefaults`
Shared Aspire/OpenTelemetry defaults:
- health checks
- metrics
- tracing
- service conventions

---

## Suggested folder layout inside each project

### `IndyPOS.StoreHub`

```text
IndyPOS.StoreHub/
  Controllers/
    SalesController.cs
    ProductsController.cs
    SyncController.cs
    HealthController.cs
  Extensions/
  Configuration/
  Program.cs
  appsettings.json
```

### `IndyPOS.CloudApi`

```text
IndyPOS.CloudApi/
  Controllers/
    SyncEventsController.cs
    MasterDataController.cs
    HealthController.cs
  Services/
  Extensions/
  Program.cs
  appsettings.json
```

### `IndyPOS.Infrastructure`

```text
IndyPOS.Infrastructure/
  Persistence/
    DbContexts/
    EntityConfigurations/
    Migrations/
    Repositories/
    Upgrades/
  Services/
    Sync/
    Identity/
    Time/
  Options/
```

### `IndyPOS.Application`

```text
IndyPOS.Application/
  Sales/
    Commands/
    Queries/
    Validators/
  Products/
  Inventory/
  Sync/
  Common/
```

---

## Recommended migration path from current codebase

### Phase 1 — keep existing app, add new services
- keep `IndyPOS.Windows.Forms`
- add `IndyPOS.StoreHub`
- add `IndyPOS.CloudApi`
- add `IndyPOS.AppHost`
- add `IndyPOS.ServiceDefaults`

### Phase 2 — move persistence into EF Core
- reduce direct repository-specific SQL
- replace deprecated report PostgreSQL path
- centralize DB access in `IndyPOS.Infrastructure`

### Phase 3 — make Windows Forms a StoreHub client
- remove direct DB writes from UI
- route sales, inventory, and product flows through local API

### Phase 4 — split SyncWorker if needed
- start as hosted service inside StoreHub
- move to separate project if operationally useful

---

## Example `.sln` target layout

```text
IndyPOS.sln
  src/IndyPOS.Domain/IndyPOS.Domain.csproj
  src/IndyPOS.Application/IndyPOS.Application.csproj
  src/IndyPOS.Infrastructure/IndyPOS.Infrastructure.csproj
  src/IndyPOS.Windows.Forms/IndyPOS.Windows.Forms.csproj
  src/IndyPOS.StoreHub/IndyPOS.StoreHub.csproj
  src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj
  src/IndyPOS.SyncWorker/IndyPOS.SyncWorker.csproj
  src/IndyPOS.AppHost/IndyPOS.AppHost.csproj
  src/IndyPOS.ServiceDefaults/IndyPOS.ServiceDefaults.csproj
```

---

## Example project references

```text
IndyPOS.Domain
  -> no project references

IndyPOS.Application
  -> IndyPOS.Domain

IndyPOS.Infrastructure
  -> IndyPOS.Application
  -> IndyPOS.Domain

IndyPOS.StoreHub
  -> IndyPOS.Application
  -> IndyPOS.Infrastructure
  -> IndyPOS.ServiceDefaults

IndyPOS.CloudApi
  -> IndyPOS.Application
  -> IndyPOS.Infrastructure
  -> IndyPOS.ServiceDefaults

IndyPOS.SyncWorker
  -> IndyPOS.Application
  -> IndyPOS.Infrastructure
  -> IndyPOS.ServiceDefaults

IndyPOS.Windows.Forms
  -> IndyPOS.Application
  -> IndyPOS.Infrastructure
  -> temporary direct references during migration only

IndyPOS.AppHost
  -> project references to CloudApi / SyncWorker
```

---

## Recommended naming conventions

- Use `PublicId` for external identity
- Use `StoreId` for store-scoped sync identity
- Use `CreatedUtc` / `LastModifiedUtc`
- Use `DbContext` per deployment boundary:
  - `StoreHubDbContext`
  - `CloudDbContext`

---

## Examples of where planned features fit

### Aspire
- `src/IndyPOS.AppHost`
- `src/IndyPOS.ServiceDefaults`

### EF Core migrations
- `src/IndyPOS.Infrastructure/Persistence/Migrations/`
or
- separate migrations folder under `StoreHub` and `CloudApi` if contexts diverge heavily

### SQLite → PostgreSQL upgrade helpers
- `src/IndyPOS.Infrastructure/Persistence/Upgrades/`

### Ops scripts
- versioned docs only:
  - `docs/operations/ops-kit/`

---

## Recommendation

For now, keep the repo simple:
1. add the new service projects
2. keep shared logic in existing Domain/Application/Infrastructure
3. make the Windows Forms app thinner over time
4. keep documentation versioned and bundled with ops kit
