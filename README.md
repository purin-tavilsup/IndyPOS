# IndyPOS

A Point-of-Sale system for small retail stores in Thailand. Supports offline-first operations with cloud sync.

```
┌────────────────┐     ┌────────────────┐
│  Desktop POS   │     │  Tablet POS    │
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

## Quick Start

```bash
# Clone & build
git clone https://github.com/your-repo/IndyPOS.git
cd IndyPOS && dotnet build

# Run with Aspire (requires Docker Desktop)
dotnet run --project src/IndyPOS.AppHost --launch-profile https

# Run tests (no Docker needed)
dotnet test tests/IndyPOS.Application.Tests/
```

**Full setup guide:** [docs/development/getting-started.md](docs/development/getting-started.md)

## Tech Stack

| Component | Technology |
|-----------|------------|
| Desktop UI | Windows.Forms (.NET 10) |
| API | ASP.NET Core Minimal APIs |
| Database | PostgreSQL (EF Core) |
| Legacy DB | SQLite (Dapper) - migrating |
| Dev Orchestration | .NET Aspire |
| CQRS | Nokpirab |
| Auth | JWT + BCrypt |

## Architecture

Clean Architecture with 4 layers:

- **Domain** - Entities, business rules (no dependencies)
- **Application** - Use cases, CQRS commands/queries
- **Infrastructure** - Repositories, external services
- **Presentation** - UI (WinForms), APIs (StoreHub, CloudApi)

## Solution Structure

```
📁 Core
├── IndyPOS.Domain/              # Core entities, business rules
├── IndyPOS.Application/         # Use cases (CQRS commands/queries)
└── IndyPOS.Infrastructure/      # EF Core, repositories, services

📁 DesktopApp
└── IndyPOS.Windows.Forms/       # Desktop UI (WinForms)

📁 Services
├── IndyPOS.StoreHub/            # Local API service (per-store)
└── IndyPOS.CloudApi/            # Central cloud API

📁 DevAppHost
├── IndyPOS.AppHost/             # Aspire orchestrator (dev only)
└── IndyPOS.ServiceDefaults/     # Shared health checks, telemetry

📁 Tools
└── IndyPOS.MigrationTool/       # SQLite → PostgreSQL migration CLI

📁 Tests
├── 📁 Core
│   └── IndyPOS.Application.Tests/
├── 📁 DesktopApp
│   └── IndyPOS.Windows.Forms.Tests/
├── 📁 Services
│   └── IndyPOS.StoreHub.IntegrationTests/
├── 📁 Tools
│   └── IndyPOS.MigrationTool.Tests/
└── IndyPOS.Mock/

docs/                            # Documentation
.planning/                       # Planning docs, ADRs
```

## Testing

```bash
# All tests (unit tests - no Docker required)
dotnet test tests/IndyPOS.Application.Tests/
dotnet test tests/IndyPOS.Windows.Forms.Tests/

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/IndyPOS.StoreHub.IntegrationTests/
dotnet test tests/IndyPOS.MigrationTool.Tests/

# Run all tests
dotnet test
```

See [`ONBOARDING.md`](ONBOARDING.md) for the per-suite counts, which suites need Docker, and the
installer suite that sits outside `IndyPOS.sln`. Kept in one place so the numbers cannot drift apart.

## Documentation

- [Developer Guide](docs/development/getting-started.md) - Full setup & testing
- [Architecture Overview](docs/architecture/overview.md)
- [ASCII Diagrams](docs/diagrams/architecture-overview.md)
- [Data Flow](docs/diagrams/data-flow.md)
- [Operations Runbook](docs/operations/RUNBOOK.md) - Production support
- [Migration Tool](src/IndyPOS.MigrationTool/README.md) - SQLite → PostgreSQL

## Store Configuration

Required file: `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`

```json
{
  "StoreFullName": "Test Store",
  "StoreName": "Test Store",
  "StoreAddressLine1": "123 Test Street",
  "StoreAddressLine2": "Test City 12345",
  "StorePhoneNumber": "000-000-0000",
  "PrinterName": "XP-58",
  "Code": 1
}
```
