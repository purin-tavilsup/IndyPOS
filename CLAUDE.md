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
  IndyPOS.StoreProfiles/     # Dev store profiles (AppHost --store, dev seeding, per-store tests)

installer/
  IndyPOS.Bootstrapper/      # Installer: fresh install + in-place upgrade

tests/
  IndyPOS.Domain.Tests/            IndyPOS.Application.Tests/
  IndyPOS.Vault.Tests/             IndyPOS.Bootstrapper.Tests/
  IndyPOS.Windows.Forms.Tests/     IndyPOS.StoreHub.IntegrationTests/   # needs Docker
  IndyPOS.MigrationTool.Tests/     IndyPOS.Mock/                        # Mock = shared fakes, not a test project
  IndyPOS.ServiceDefaults.Tests/                                         # health probes on a slim host; needs Docker

docs/                        # Architecture docs, operations
.planning/                   # Planning docs, diagrams, completed epics
fonts/  scripts/  publish/   # Bundled fonts, helper scripts, build output
.claude/                     # Session context - GITIGNORED, local only
```

⚠️ **`.claude/` is gitignored** (since the BFG history purge), so `STATUS.md` never commits — do not
try to include it in a PR.

✅ **Epic 2's pinned defects are all fixed.** The harness below is now pure regression cover.

⚠️ **The SQLite → PostgreSQL migration tests contained deliberate PINNING tests.**
`tests/IndyPOS.MigrationTool.Tests` exercises the shipped migrator against schema artefacts dumped
from real stores. Those tests asserted **today's wrong behaviour on purpose**: each named its defect,
recorded the correct answer in the message, and was verified able to fail. **None are left pinned** —
defects 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 and 21 are all fixed —
**Epic 2's defect list is now empty.**

Several of those tests now pin a **decision** instead — which columns are deliberately *not* migrated,
for instance — so the rule that mattered still holds: **read the comment before changing one.**

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

⚠️ **A full migration takes about a minute per store and peaks under 750 MB.** Defect 19 removed the
per-invoice queries that made it take hours (GeneralHardware went 3.5 h → **1.2 min**), so a run that
sits there for many minutes is now a real problem rather than normal. Memory was 2.8 GB against a
documented **4 GB minimum** till; invoices are now flushed in batches inside one transaction, which
brought GeneralHardware to **724 MB** and MimyMart to 672 MB.

⚠️ **Defect 12's all-or-nothing guarantee now rests on that transaction, not on saving once.** Rows
reach PostgreSQL before the run is known to be good, and are rolled back if any phase fails. If you
touch `FlushAsync` or the transaction in `MigrateAllAsync`, the test that protects this is
`MigrationPhaseIsolationTests.MigrateAllAsync_WhenAPhaseFailsAfterABatchWasFlushed_StillPersistsNothing`
— the older one-invoice test cannot catch a broken transaction, because it never flushes.

⚠️ **Two tests flake under the CPU load of a full-solution run**, and one of them is now fixed.
`SyncWorkerTests.SyncWorker_ShouldHandleException_WithoutCrashing` was the long-unidentified
`Application.Tests` flake: it slept a fixed 100 ms and then asserted a background worker had polled,
which is not guaranteed when six suites and two Testcontainers Postgres instances are competing. It
now waits for the condition instead. **An earlier note here blamed the number of running containers —
that was wrong**; it recurred with only one container up, and the cause was always the fixed sleep.

`ProductsEndpointTests.CreateProduct_WithValidData_ReturnsCreatedProduct` has flaked once under the
same load. Its failure message was never captured, but its likely cause was found on 2026-09-27, when a TRX capture caught
`ProductsEndpointTests.AdjustQuantity_WithAZeroDelta_ShouldReturnBadRequest` failing in *setup*:
`23505 duplicate key ... "IX_store_user_legacy_user_id"`. `CreateTestUserAsync` picked
`LegacyUserId` at random from ~9,000 values, the column is unique, and the shared test database is
**not reset between tests** — so users accumulated and ids collided (the birthday problem), in any
test that signs in. It now comes from `IntegrationTestBase.NextLegacyUserId()`, a counter, pinned by
`TestUserIdTests`. The `CreateProduct` flake signs in the same way, but its message was never captured,
so that link is likely rather than proven. **Run the full solution with
`--logger trx --results-directory <dir>`** so any further flake is caught with its message.

✅ **Defect 7b is fixed and its trip-wire is discharged.** `Core.Product` now carries `IsTrackable`
and a sale of a non-trackable product moves no stock — the invoice line is still written, because the
money is real. 29 real products are affected. It is a per-PRODUCT flag, not derived from
`ProductCategoryKind.Service`: every category holding a non-trackable product also holds trackable
ones (เบ็ดเตล็ด has 12 against 3,391), so the category cannot stand in for it.

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
| API conventions | `docs/architecture/api-conventions.md` | Before adding or changing a StoreHub route |

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
See `docs/operations/upgrade-procedure.md`, which now carries a **recipe for verifying the gate** —
apply the release's schema, then write a complete sale using only the columns that existed before it.
Verified for the 2026-08-17 release's three migrations, the cash-drawer release's
`AddCashDrawerTables`, the invoice-history release's `AddInvoiceNumber` and `AddInvoiceReprintTable`,
invoice-history plan 2's cloud `AddInvoiceNumberToInvoices`, `AddInboxRetrySchedule` and
`AddInvoiceReprints`, and the route tidy-up release's StoreHub `AddInventoryMovementUser` and cloud
`AddSyncedEventSourceStore` (the cloud variant of the recipe is in the same doc). Before the
2026-08-17 release the gate had only ever been reasoned about.

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
# Run all tests. Exits 0 on success -- IndyPOS.Mock is marked IsTestProject=false, so the
# runner no longer tries to execute the shared-fakes assembly and fail on it.
dotnet test

# Build
dotnet build

# Docker must be RUNNING for four suites - they spin up a real Postgres container.
# With Docker down they fail fast (each suite in under a second), which reads like a
# code regression but is not. Expect 484 failures with Docker stopped, all here.
# (484 = 336 + 82 + 58 + 8. The StoreHub 128 was measured 2026-09-27 with Docker down; the
# final-review test additions (+13, all HTTP integration tests) and the concurrent-delete
# race tests (+7, all on real Postgres) all need Docker, bringing it to 148, and the PayLater-debt fix's +10 (all HTTP
# integration tests) to 158, and the sale-user fix's +2 to 160 -- DERIVED, not re-measured with Docker down. CloudApi.IntegrationTests grew 2 -> 22 on 2026-09-30 with the
# event-pipeline repair and the sync store check, all on real Postgres -- also DERIVED. The invoice-history plan then added 71
# StoreHub tests (HTTP and persistence tests on real Postgres) and 11 MigrationTool tests (all on the Postgres fixture),
# bringing them to 231 and 82 -- DERIVED too; the final-review fix added one more (the reprint 403 test) for 232, and the Codex route fix one more for 233. Invoice-history plan 2 then took CloudApi.IntegrationTests from 22 to 44 (22 more, all on real Postgres) -- DERIVED the same way. The route tidy-up PR A (2026-10-02) then added 26 StoreHub tests and 5 CloudApi tests, all needing a container, for 259 and 49 -- DERIVED, not re-measured with Docker down. Route tidy-up B (2026-10-06) added 22 StoreHub tests, all needing a container, for 281 -- DERIVED the same way. The health-check convention (2026-10-07) added 3 StoreHub, 2 CloudApi and 8 ServiceDefaults tests, all on a container, for 284, 51 and 8 -- DERIVED the same way. The dev store profiles (2026-10-07) added 20 StoreHub and 3 CloudApi tests, all on a container, for 304 and 54, and its review fixes 1 more each, for 305 and 55, and the EnsureCreated-database check 4 and 3 more, for 309 and 58, and per-store UI slice 2 21 more StoreHub (all on a container), for 330, and the 2026-10-10 /auth/me and sale-shape fixes 6 more (all HTTP), for 336 -- DERIVED the same way. The per-suite totals were measured; only the Docker-down split was not.)
#   tests/IndyPOS.StoreHub.IntegrationTests   (336 of 344; 8 need no container, unchanged since 2026-09-27, derived
#                                              -- not individually named)
#   tests/IndyPOS.MigrationTool.Tests         (82 of 145; 38 pure units, 24 need the
#                                              gitignored real store .db files, 1 manual tool)
#   tests/IndyPOS.CloudApi.IntegrationTests   (58 of 58; all need a container)
#   tests/IndyPOS.ServiceDefaults.Tests       (8 of 8; every test's fixture starts a Postgres)

# CI (.github/workflows/ci.yml) runs the build and every suite on each PR to `development`. It sets
# INDYPOS_TEST_POSTGRES to a full Npgsql connection string so the Postgres suites use that server
# (one throwaway indypos_test_<guid> database per fixture) instead of Docker. Reproduce it locally:
#   export INDYPOS_TEST_POSTGRES='Host=localhost;Port=55520;Username=postgres;Password=pass'
# (see ONBOARDING.md, "Continuous integration and running without Docker")

# The installer is NOT in IndyPOS.sln, so the two commands above never touch it.
# Run it explicitly (235 tests: 227 pass, 8 skipped):
dotnet test tests/IndyPOS.Bootstrapper.Tests

# Run with Aspire (requires Docker)
dotnet run --project src/IndyPOS.AppHost --launch-profile https
# Dashboard: https://localhost:17222

# Run as a dev store (own database, products, methods and receipt header; see ONBOARDING "Running as a store")
dotnet run --project src/IndyPOS.AppHost -- --store MimyMart     # or GeneralHardware (default), MimyShop
dotnet run --project src/IndyPOS.AppHost -- --store all          # all three on :5012/:5013/:5014, one CloudApi
```

Solution suites total **1358** with Docker running and the real store databases present (1357 pass,
1 skipped) — measured 2026-10-10 (after the dev template products; 1341 earlier on 2026-10-10 after the till Development fix, 1343 on 2026-10-07 after per-store UI slice 2, 1291 earlier on 2026-10-07 after slice 1, 1269 earlier on 2026-10-07 after the dev store profiles and their EnsureCreated-database check, 1209 earlier on 2026-10-07 after the health-check
convention, 1186 after the till startup fix, 1183 on 2026-10-06 after route tidy-up B, 1152 on 2026-10-02 after route tidy-up A, 1111 earlier on 2026-10-02 after invoice-history plan 2, 1089 on 2026-10-01, 928 on 2026-09-30, 853 on 2026-09-27, after the
cash-drawer release). Per suite: Domain 61 · Vault 17 · CloudApi 6 · CloudApi.IntegrationTests 58
(Docker) · MigrationTool 145 (144 pass, 1 skip) · StoreHub.IntegrationTests 344 · Application 640 ·
Windows.Forms 79 · ServiceDefaults 8 (Docker). The growth since the 1209 measurement is the dev store profiles their review fixes and the per-store payment methods (Application +24, StoreHub +21,
CloudApi +4, Windows.Forms +4): 1209 + 53 = 1262, and the EnsureCreated-database check (StoreHub +4, CloudApi +3)
for 1269. Per-store UI slice 1 then added Domain +5, Application +5 and Windows.Forms +12, none on a container:
1269 + 22 = 1291. Slice 2 then added Application +19, Windows.Forms +12 and StoreHub +21: 1291 + 52 = 1343. The till Development fix then deleted the dead `AddApplicationServices` and its 3 Application tests
and added 1 Windows.Forms test: 1343 − 2 = 1341. The /auth/me fix then added StoreHub +1, the sale-shape fix Application +7 and StoreHub +5,
and the dev template products Application +4: 1341 + 17 = 1358. Without the
real store databases the suite discovers **1338** (1358 − 20, DERIVED; CI measured 1166 = 1186 − 20 on PR #114, 2026-10-07) — a skipped
`[Theory]` is one entry, not one per row.
See [`ONBOARDING.md`](ONBOARDING.md) for the per-suite breakdown, the dev-vs-installed port split,
and the `/health` vs `/health/ready` trap.

## Store Configuration (Required for Debug)

Needed only when running the till without Aspire; the AppHost generates one per store. Create
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

## Important Reminders

- Apply SOLID principles and Clean Code standards
- Write small, testable functions
- Use async/await for I/O operations
- Validate input at system boundaries
- **Modernization mindset**: Look for opportunities to improve code while working
