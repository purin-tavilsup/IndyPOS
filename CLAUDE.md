# IndyPOS - Project Context

## Session Start

**New to this repo, or a fresh clone?** Start with [`ONBOARDING.md`](ONBOARDING.md) — prerequisites,
build/test, how to run it, and the four traps that waste the most time.

**Resuming work on an existing checkout?** Read `.claude/STATUS.md` (quick checkpoint, ~50 lines).
⚠️ **That file is gitignored and will not exist in a fresh clone** — this repo is public, so session
context stays local. Do not try to commit it.

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

**`src/` is flat** — one directory per project, no `Core/`/`Services/`/`Tools/` grouping layer.

```
src/
  IndyPOS.Domain/            # Entities, Value Objects, Domain Logic
  IndyPOS.Application/       # Use Cases (Commands/Queries), Interfaces, DTOs
  IndyPOS.Infrastructure/    # Repositories, External Services
  IndyPOS.Windows.Forms/     # Desktop UI (legacy)
  IndyPOS.StoreHub/          # Local API service (ASP.NET Core)
  IndyPOS.CloudApi/          # Central cloud API
  IndyPOS.Vault/             # DPAPI secret protection (connection string, JWT key)
  IndyPOS.MigrationTool/     # SQLite -> PostgreSQL migration
  IndyPOS.AppHost/           # Aspire orchestrator
  IndyPOS.ServiceDefaults/   # Shared health checks, OpenTelemetry

installer/
  IndyPOS.Bootstrapper/      # Installer: fresh install + in-place upgrade

tests/
  IndyPOS.Domain.Tests/            IndyPOS.Application.Tests/
  IndyPOS.Vault.Tests/             IndyPOS.Bootstrapper.Tests/
  IndyPOS.Windows.Forms.Tests/     IndyPOS.StoreHub.IntegrationTests/   # needs Docker
  IndyPOS.MigrationTool.Tests/     IndyPOS.Migration.Tests/             # needs Docker
  IndyPOS.Mock/                                                        # shared fakes, not a test project

docs/                        # Architecture docs, operations
.planning/                   # Planning docs, diagrams, completed epics
fonts/  scripts/  publish/   # Bundled fonts, helper scripts, build output
.claude/                     # Session context - GITIGNORED, local only
```

⚠️ **`.claude/` is gitignored** (since the BFG history purge), so `STATUS.md` never commits — do not
try to include it in a PR.

⚠️ **`tests/IndyPOS.Migration.Tests` is scheduled for deletion**, not repair — it tests a parallel
implementation against a schema no store has. See Epic 2 in `PLAN.md`.

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

### Database Migrations - Forward-Only (release gate)

An upgrade rolls back binaries and config, **not schema**. Every migration in a release
must therefore be runnable against the *previous* release's binaries:

- Additive only. New columns nullable or with a default.
- No renames, no drops, no type narrowing.
- No new `NOT NULL` column without a default - the restored binaries' INSERT would fail
  and the till could not complete a sale.

A migration that breaks this makes the installer's rollback claim false.
See `docs/operations/upgrade-procedure.md`.

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

# Docker must be RUNNING for three suites - they spin up a real Postgres container.
# With Docker down they fail fast (each suite in under a second), which reads like a
# code regression but is not. Measured with Docker stopped: 117 failures, all here.
#   tests/IndyPOS.StoreHub.IntegrationTests   (87 of 94; 7 need no container)
#   tests/IndyPOS.Migration.Tests             (15 - all of them)
#   tests/IndyPOS.MigrationTool.Tests         (15 of 37; 21 are pure units, 1 skipped)

# The installer is NOT in IndyPOS.sln, so the two commands above never touch it.
# Run it explicitly (231 tests: 223 pass, 8 skipped):
dotnet test tests/IndyPOS.Bootstrapper.Tests

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222
```

Solution suites total **512** with Docker running. See [`ONBOARDING.md`](ONBOARDING.md) for the
per-suite breakdown, the dev-vs-installed port split, and the `/health` vs `/health/ready` trap.

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
