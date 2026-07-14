# IndyPOS - Project Context

## Session Start

**Always read first:** `.claude/STATUS.md` (quick checkpoint, ~50 lines)

**Need more detail?** `.planning/indypos-overhaul/PLAN.md`

---

## Overview

IndyPOS is a Point-of-Sale system for small retail stores (3 stores, 1-2 terminals each).

**Tech Stack:**
- Backend: C# .NET 10 (Clean Architecture)
- UI: Windows.Forms (future: MAUI)
- Database: PostgreSQL (StoreHub) - SQLite removed
- Dev Environment: .NET Aspire
- Patterns: CQRS, Domain-Driven Design

## Project Structure

```
src/
  Core/
    IndyPOS.Domain/          # Entities, Value Objects, Domain Logic
    IndyPOS.Application/     # Use Cases (Commands/Queries), Interfaces, DTOs
    IndyPOS.Infrastructure/  # Repositories, External Services
  DesktopApp/
    IndyPOS.Windows.Forms/   # Desktop UI (legacy)
  Services/
    IndyPOS.StoreHub/        # Local API service (ASP.NET Core)
    IndyPOS.CloudApi/        # Central cloud API
  DevAppHost/
    IndyPOS.AppHost/         # Aspire orchestrator
    IndyPOS.ServiceDefaults/ # Shared health checks, OpenTelemetry
  Tools/
    IndyPOS.MigrationTool/   # SQLite -> PostgreSQL migration

tests/                       # Unit, integration, migration tests
docs/                        # Architecture docs, operations
.planning/                   # Planning docs, diagrams, completed epics
.claude/                     # Session context (STATUS.md, session-log.md)
```

## Key Documentation

| Purpose | Location | When to Read |
|---------|----------|--------------|
| Quick status | `.claude/STATUS.md` | **Always first** |
| Full plan | `.planning/indypos-overhaul/PLAN.md` | When you need epic details |
| Session history | `.claude/session-log.md` | When resuming work |
| Completed epics | `.planning/indypos-overhaul/completed/` | For historical context |
| Security spec | `.planning/indypos-overhaul/security/` | For auth/security work |
| Diagrams | `.planning/indypos-overhaul/diagrams/` | For architecture visuals |
| Operations | `docs/operations/` | For deployment/runbook |

## Coding Standards

### Clean Architecture Layers
- **Domain**: No external dependencies, pure business logic
- **Application**: Orchestrates use cases, depends on Domain only
- **Infrastructure**: Implements interfaces, depends on Application + Domain
- **UI**: Thin presentation layer

### Naming Patterns
- **Use Cases**: `[Verb][Noun]Command/Query` (e.g., `CompleteSaleCommand`)
- **Handlers**: `[Command/Query]Handler` (e.g., `CompleteSaleCommandHandler`)
- **Repositories**: `I[Entity]Repository` (e.g., `IInvoiceRepository`)
- **Services**: `I[Domain]Service` (e.g., `IStoreIdentityService`)

### Entity Conventions
- All new entities use `Guid Id` (UUID primary keys)
- All entities: `DateTime CreatedUtc`, `DateTime LastModifiedUtc`

### Method Chaining Style
```csharp
// Good - dots vertically aligned
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
       .WithReference(storeHubDb)
       .WaitFor(postgres);

// Good - also acceptable with standard indent
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
    .WithReference(storeHubDb)
    .WaitFor(postgres);
```

## Development Workflow

- **Branch**: Feature branches from `development`
- **Main Branch**: `development`
- **Commits**: Follow conventional commits
- **PRs**: Small, focused changes with tests

## Quick Commands

```bash
# Run all tests
dotnet test

# Build
dotnet build

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222
```

## Store Configuration (Required for Debug)

Create `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`:

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

## Important Reminders

- Apply SOLID principles and Clean Code standards
- Write small, testable functions
- Use async/await for I/O operations
- Validate input at system boundaries
- **Modernization mindset**: Look for opportunities to improve code while working
