# IndyPOS

A Point-of-Sale system for small retail stores in Thailand (3 stores, 1–2 tills each). **Offline-first**: a till
never waits for the internet. Every sale and cash-drawer entry is queued at the store and synced to the cloud,
where dashboards and AI agents read it.

```
┌────────────────┐     ┌────────────────┐
│  Desktop POS   │     │  Avalonia POS  │
│  (WinForms)    │     │  (Future)      │
└───────┬────────┘     └───────┬────────┘
        │ HTTP                 │ HTTP
        └──────────┬───────────┘
                   ▼
        ┌─────────────────────┐
        │   StoreHub API      │ ← Local service
        │   (ASP.NET Core)    │
        └──────────┬──────────┘
                   ▼
        ┌─────────────────────┐
        │  Local PostgreSQL   │ ← Per-store
        └──────────┬──────────┘
                   │ HTTPS (when online)
                   ▼
        ┌─────────────────────┐
        │     Cloud API       │ ← Central
        └─────────────────────┘
```

## What it does

- **Sales and receipts.** Barcode scanning, open-price items, many payment methods per store (cash, transfer,
  welfare card, campaigns), change, refunds, printed receipts and reprints.
- **ลงบัญชี (PayLater).** Credit sales for known customers. Every repayment is recorded with the cashier who took it.
- **ลิ้นชักเก็บเงิน (cash drawer).** Payouts, change floats, debt repayments and cash counts. Expected cash and the
  difference (ขาด / เกิน / พอดี) for the day.
- **Sales history.** Today's bills, any day or range, bill detail, and reprints.
- **Per-store differences.** Each store type (GeneralHardware, MimyMart, MimyShop) gets its own features and
  buttons, and its own payment methods.
- **Cloud copy.** Sales, the cash drawer and ลงบัญชี reach the cloud through an outbox that never loses an event.
  `CashDrawerDays` gives each store's daily drawer balance there, with the same figures as the till.
- **Installer.** A fresh install, or an in-place upgrade that rolls back if anything fails.
- **v3 migration.** A tool that brings a store's SQLite history into PostgreSQL and checks the result.

## Quick Start

**New here? Start with [`ONBOARDING.md`](ONBOARDING.md)**: prerequisites, build and test, how to run it, and the
traps that waste the most time.

```bash
# Clone & build
git clone https://github.com/purin-tavilsup/IndyPOS.git
cd IndyPOS && dotnet build

# Run with Aspire (requires Docker Desktop); dashboard at https://localhost:17222
dotnet run --project src/IndyPOS.AppHost --launch-profile https

# Run as a dev store: its own database, products, payment methods and receipt header
dotnet run --project src/IndyPOS.AppHost -- --store MimyMart     # or GeneralHardware (default), MimyShop
dotnet run --project src/IndyPOS.AppHost -- --store all          # all three, one CloudApi

# Run every test (Docker must be running; see Testing)
dotnet test
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Desktop UI | Windows.Forms (.NET 10), ViewModels with CommunityToolkit.Mvvm (Avalonia port planned) |
| APIs | ASP.NET Core Minimal APIs (StoreHub per store, CloudApi central) |
| Database | PostgreSQL 18 with EF Core 10 (Npgsql) |
| CQRS | Nokpirab |
| Auth | StoreHub: JWT + BCrypt + role capabilities. CloudApi: OpenIddict client credentials per store |
| Secrets | DPAPI (`IndyPOS.Vault`): connection string and JWT key |
| Installer | Bootstrapper + Velopack (fresh install and in-place upgrade) |
| Dev orchestration | .NET Aspire |
| Tests | xUnit, FluentAssertions, Moq/AutoFixture, Testcontainers PostgreSQL |
| CI | GitHub Actions: build and every suite on each PR to `development` |
| v3 import | SQLite (read only, by the MigrationTool) |

## Architecture

Clean Architecture:

- **Domain**: entities and business rules, with no dependencies
- **Application**: use cases (CQRS commands and queries), interfaces, DTOs
- **Infrastructure**: EF Core, repositories, external services
- **Presentation**: the WinForms till, its ViewModels (`IndyPOS.Presentation`, no WinForms), and the
  StoreHub and CloudApi endpoints

More: [Architecture Overview](docs/architecture/overview.md).

## Solution Structure

`src/` is flat, one directory per project:

```
src/
  IndyPOS.Domain/            # Entities, value objects, domain rules
  IndyPOS.Application/       # Use cases (commands/queries), interfaces, DTOs
  IndyPOS.Infrastructure/    # EF Core, repositories, services
  IndyPOS.Windows.Forms/     # The till (desktop UI)
  IndyPOS.Presentation/      # Till ViewModels (CommunityToolkit.Mvvm), no WinForms
  IndyPOS.StoreHub/          # Local API service, one per store
  IndyPOS.CloudApi/          # Central cloud API
  IndyPOS.Vault/             # DPAPI secret protection
  IndyPOS.MigrationTool/     # v3 SQLite -> PostgreSQL migration
  IndyPOS.AppHost/           # Aspire orchestrator (dev only)
  IndyPOS.ServiceDefaults/   # Shared health checks, OpenTelemetry
  IndyPOS.StoreProfiles/     # Dev store profiles (--store), dev seeding

installer/
  IndyPOS.Bootstrapper/      # Installer: fresh install + in-place upgrade (not in IndyPOS.sln)

tests/                       # one suite per project, plus:
  IndyPOS.Mock/              # shared fakes (not a test project)
  IndyPOS.Testing.Postgres/  # shared Postgres fixture (Testcontainers, or a CI server)

docs/                        # Architecture, development, operations
.planning/                   # Planning docs, completed epics
```

## Testing

```bash
# Everything in IndyPOS.sln. Docker must be RUNNING: four suites start a real PostgreSQL.
dotnet test

# Suites that need no Docker
dotnet test tests/IndyPOS.Domain.Tests/
dotnet test tests/IndyPOS.Application.Tests/
dotnet test tests/IndyPOS.Presentation.Tests/
dotnet test tests/IndyPOS.Windows.Forms.Tests/

# Suites on PostgreSQL (Docker, or INDYPOS_TEST_POSTGRES as CI does)
dotnet test tests/IndyPOS.StoreHub.IntegrationTests/
dotnet test tests/IndyPOS.CloudApi.IntegrationTests/
dotnet test tests/IndyPOS.MigrationTool.Tests/
dotnet test tests/IndyPOS.ServiceDefaults.Tests/

# The installer sits outside IndyPOS.sln
dotnet test tests/IndyPOS.Bootstrapper.Tests/
```

See [`ONBOARDING.md`](ONBOARDING.md) for the per-suite counts and the failures to expect with Docker stopped.
They are kept in one place so the numbers cannot drift apart.

## Documentation

- [Onboarding](ONBOARDING.md): start here
- [Developer Guide](docs/development/getting-started.md): full setup and testing
- [Architecture Overview](docs/architecture/overview.md)
- [API conventions](docs/architecture/api-conventions.md): read before adding or changing a StoreHub route
- [ASCII Diagrams](docs/diagrams/architecture-overview.md)
- [Data Flow](docs/diagrams/data-flow.md)
- [Operations Runbook](docs/operations/RUNBOOK.md): production support
- [Upgrade procedure](docs/operations/upgrade-procedure.md): in-place upgrade, rollback, and the forward-only migration gate
- [Migration Tool](src/IndyPOS.MigrationTool/README.md): SQLite → PostgreSQL

## Store Configuration

Only needed when running the till **without** Aspire; the AppHost generates one per store. Create
`C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`:

```json
{
  "StoreFullName": "Test Store",
  "StoreName": "Test Store",
  "StoreAddressLine1": "123 Test Street",
  "StoreAddressLine2": "Test City 12345",
  "StorePhoneNumber": "000-000-0000",
  "PrinterName": "XP-58",
  "BarcodeScannerDeviceName": "",
  "SerialPortName": "COM1",
  "Code": 1
}
```
