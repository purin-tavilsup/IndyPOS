# IndyPOS - Project Context

## Session Start

**New to this repo, or a fresh clone?** Start with [`ONBOARDING.md`](ONBOARDING.md) — prerequisites,
build/test, how to run it, and the five traps that waste the most time.

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
  IndyPOS.MigrationTool.Tests/     IndyPOS.Mock/                        # Mock = shared fakes, not a test project

docs/                        # Architecture docs, operations
.planning/                   # Planning docs, diagrams, completed epics
fonts/  scripts/  publish/   # Bundled fonts, helper scripts, build output
.claude/                     # Session context - GITIGNORED, local only
```

⚠️ **`.claude/` is gitignored** (since the BFG history purge), so `STATUS.md` never commits — do not
try to include it in a PR.

⚠️ **The SQLite → PostgreSQL migration tests contain deliberate PINNING tests.**
`tests/IndyPOS.MigrationTool.Tests` exercises the shipped migrator against schema artefacts dumped
from real stores. Some of its tests assert **today's wrong behaviour on purpose** — they name their
defect (8), record the correct answer in the message, and were each verified able to
fail. **When you fix a defect, invert its pin; do not "repair" the assertion to match new behaviour
without reading its comment.** Defects 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
and 21 are fixed. Only **8** is still pinned.

⚠️ **A pin's stated "correct answer" can itself be wrong — defect 6's was.** It claimed 51% of real
invoice lines were discounted; that figure was really the count of rows where `OriginalUnitPrice` is
*zero*, and the true number of discounted lines across all three stores is **0**. So two of its five
columns were deliberately NOT restored, and both pins now assert that decision instead. **Re-measure
before trusting a pin's premise**, not just its assertion.

✅ **All three real stores now migrate and verify end to end.** GeneralHardware and MimyMart had
never once completed before defect 19; both take about a minute now. GeneralHardware's `verify` still
exits 1, correctly and permanently: 2 legacy payments reference an invoice that no longer exists
(defect 20), so ฿1,000 can never reach v4. Every *migration* check is green — the one ✗ is
`Payments (no invoice)`, and the remedy is in the legacy database, not a re-run.

⚠️ **A full migration takes about a minute per store and peaks near 2.8 GB.** Defect 19 removed the
per-invoice queries that made it take hours (GeneralHardware went 3.5 h → **1.2 min**), so a run that
sits there for many minutes is now a real problem rather than normal. The memory is inherent to the
single `SaveChangesAsync` that defect 12 made deliberate — everything is held until then — so watch
it on a low-RAM till.

⚠️ **Two tests flake under the CPU load of a full-solution run**, and one of them is now fixed.
`SyncWorkerTests.SyncWorker_ShouldHandleException_WithoutCrashing` was the long-unidentified
`Application.Tests` flake: it slept a fixed 100 ms and then asserted a background worker had polled,
which is not guaranteed when six suites and two Testcontainers Postgres instances are competing. It
now waits for the condition instead. **An earlier note here blamed the number of running containers —
that was wrong**; it recurred with only one container up, and the cause was always the fixed sleep.

`ProductsEndpointTests.CreateProduct_WithValidData_ReturnsCreatedProduct` has flaked once under the
same load and is **not** fixed. It passes in isolation. Re-run before investigating it as a
regression, and be suspicious of any other test that sleeps rather than waiting for a condition.

⚠️ **Defect 7 is open but has NO pin** — do not go looking for one. Its pin asserted that migrating a
service line produced a stock movement; defect 14 removed that replay, so the pin was deleted rather
than re-aimed. What survives (7b: live sales deduct stock for services) cannot be pinned until
`Core.Product` carries `IsTrackable`, so it is documented as a commented trip-wire in
`CompleteSaleCommandHandlerTests.HandleAsync_ShouldCreateInventoryMovementsForEachLine`. Full
reasoning in Epic 2's defect table in `PLAN.md`.

⚠️ **Never hand-write the legacy SQLite schema.** `LegacySchema/*.sql` are generated dumps from real
`Store.db` files. Regenerate by setting `INDYPOS_REGENERATE_LEGACY_SCHEMA=1` and running
`dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"` — the variable is
required, because `--filter` cannot un-skip a test. Hand-writing the schema is what produced a
fixture no store had, the root of defects 2 and 3.

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

# Docker must be RUNNING for two suites - they spin up a real Postgres container.
# With Docker down they fail fast (each suite in under a second), which reads like a
# code regression but is not. Expect 161 failures with Docker stopped, all here.
# (161 is DERIVED as 97 + 64, not measured - both suites were last run with Docker up.)
#   tests/IndyPOS.StoreHub.IntegrationTests   (97 of 104; 7 need no container)
#   tests/IndyPOS.MigrationTool.Tests         (64 of 127; 38 pure units, 24 need the
#                                              gitignored real store .db files, 1 manual tool)

# The installer is NOT in IndyPOS.sln, so the two commands above never touch it.
# Run it explicitly (231 tests: 223 pass, 8 skipped):
dotnet test tests/IndyPOS.Bootstrapper.Tests

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222
```

Solution suites total **610** with Docker running and the real store databases present (609 pass,
1 skipped) — measured 2026-08-17. Per suite: Domain 36 · Vault 17 · MigrationTool 127 (126 pass,
1 skip) · StoreHub.IntegrationTests 104 · Application 298 · Windows.Forms 28. Without the real
store databases the suite discovers **590** (DERIVED as 610 − 20, not measured) — a skipped
`[Theory]` is one entry, not one per row. See [`ONBOARDING.md`](ONBOARDING.md) for the per-suite
breakdown, the dev-vs-installed port split, and the `/health` vs `/health/ready` trap.

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
