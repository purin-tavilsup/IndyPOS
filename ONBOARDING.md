# IndyPOS — Getting Started

Everything needed to go from a fresh clone to a running, tested build. Facts only; every number
below was measured against this repository, except where marked derived.

For architecture and coding standards read [`CLAUDE.md`](CLAUDE.md). For the roadmap and open work
read [`.planning/indypos-overhaul/PLAN.md`](.planning/indypos-overhaul/PLAN.md).

---

## What this is

A Point-of-Sale system for three small retail stores in Thailand, 1–2 terminals each. The UI is Thai.

Three deployable pieces:

| Piece | Project | Role |
|---|---|---|
| **Till app** | `src/IndyPOS.Windows.Forms` | Windows Forms desktop UI, one per terminal |
| **StoreHub** | `src/IndyPOS.StoreHub` | ASP.NET Core API + PostgreSQL, one per store; the till talks to it |
| **CloudApi** | `src/IndyPOS.CloudApi` | Central API for cross-store reporting |

.NET 10, Clean Architecture, CQRS. PostgreSQL only — SQLite was removed from the application, and
now appears solely as the *source* format for migrating legacy store data.

Stores currently run the previous generation (v3.7.0, SQLite-backed). v4 installs **side by side**
with it rather than upgrading it.

---

## Prerequisites

| Requirement | Detail |
|---|---|
| **.NET SDK 10** | `global.json` pins `10.0.107` with `rollForward: latestMinor`, so any later 10.0.x works |
| **Docker Desktop** | Required by 117 of the tests (see below) and by Aspire |
| **Windows** | Several projects target `net10.0-windows`; the till is Windows Forms |
| **FC Subject font** | In [`fonts/`](fonts/) — install `Regular` and `Bold`. Every panel names this family explicitly, so without it Windows substitutes a fallback and Thai captions clip |

---

## Build and test

```bash
dotnet build
dotnet test
```

### ⚠️ Trap 1 — two suites need Docker and fail loudly without it

They spin up a real PostgreSQL container via Testcontainers. With Docker stopped they fail **fast**
(each suite in under a second), which reads exactly like a code regression but is not.

Expect **139 failures** with Docker stopped, all from these two suites. That figure is **derived**
(97 + 42 by construction), not measured — the suites were last run with Docker up:

| Suite | Total | Fails without Docker |
|---|---|---|
| `IndyPOS.MigrationTool.Tests` | 92 | **42** (of the other 50, see Trap 3) |
| `IndyPOS.StoreHub.IntegrationTests` | 104 | **97** (7 need no container) |

**Start Docker and re-run before investigating any of these.**

### ⚠️ Trap 2 — the installer is not in the solution

`IndyPOS.sln` contains 18 projects and **excludes `installer/IndyPOS.Bootstrapper` and
`tests/IndyPOS.Bootstrapper.Tests`**. Root `dotnet build` and `dotnet test` never touch them, so a
green run says nothing about the installer. Run it explicitly:

```bash
dotnet test tests/IndyPOS.Bootstrapper.Tests
```

### ⚠️ Trap 3 — the migration suite discovers *fewer* tests on a fresh clone, and that is correct

`RealStoreSchemaTests` compares the committed legacy-schema artefacts against **real store
databases** at `.planning/indypos-overhaul/sqlite_database/<Shape>/Store.db`. Those files are
gitignored and ~64 MB each, so a fresh clone does not have them.

Without them the tests report **Skipped, never Passed** — deliberately. The version before them
returned early instead, so 8 tests reported *Passed* on every machine but one, including CI. Those
tests also used one-directional `Should().Contain(...)` column checks, which a table with extra
columns satisfies — so they could not have detected a dropped column even when they did run.

So `IndyPOS.MigrationTool.Tests`'s 92 break down as: **42** need Docker, **24** need those real
databases, **25** are pure units needing neither, and **1** is the manual schema extractor (Trap 3b).

**The total itself changes.** A skipped `[Theory]` is one skipped entry, not one per data row, so the
21-case artefact comparison collapses to a single entry. On a fresh clone the suite therefore
discovers **72**, not 92 (**derived** as 92 − 20, not measured — the same 20-row collapse described
above). Nothing turns red.

### ⚠️ Trap 3b — regenerating the schema artefacts needs an environment variable

`LegacySchema/*.sql` are generated dumps. Never hand-edit them — hand-writing the legacy schema is
what produced a fixture schema no real store had. To regenerate:

```powershell
$env:INDYPOS_REGENERATE_LEGACY_SCHEMA = "1"
dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
```

**The variable is required.** `--filter` selects a test; it cannot un-skip one. Without it the run
reports `Skipped: 1` and writes nothing — which looks like success while leaving the artefacts stale.

### Expected counts

Solution suites (`dotnet test` at the root), Docker running **and** the real store databases present
— **575 total** (574 pass, 1 skipped) — measured 2026-08-10. Without those databases the total is
**555** (**derived** as 575 − 20, not measured), still all green:

| Suite | Tests |
|---|---|
| `IndyPOS.Application.Tests` | 298 |
| `IndyPOS.StoreHub.IntegrationTests` | 104 (Docker) |
| `IndyPOS.MigrationTool.Tests` | 92 (Docker; 1 skipped. **72** without the real store data — Trap 3, derived) |
| `IndyPOS.Domain.Tests` | 36 |
| `IndyPOS.Windows.Forms.Tests` | 28 |
| `IndyPOS.Vault.Tests` | 17 |

Outside the solution: `IndyPOS.Bootstrapper.Tests` — **231** (223 pass, 8 skipped).

`tests/IndyPOS.Mock` is shared fakes, not a test project.

> **The SQLite → PostgreSQL migration paths now have real coverage.** `tests/IndyPOS.Migration.Tests`
> was deleted, not repaired — it exercised a parallel migration implementation the product never
> referenced, against a SQLite schema no real store has, so its 15 green tests were misleading.
> `tests/IndyPOS.MigrationTool.Tests` replaces it: the **shipped** migrator, run against schema
> artefacts dumped from real stores. Defects 2, 3, 4, 10, 11 and 12 are fixed; defects 5, 6, 7, 8 and
> 13 are **pinned** — tests that assert today's wrong behaviour, name the correct answer, and were
> each verified able to fail. When a defect is fixed, invert exactly one pinning test. See Epic 2 in
> `PLAN.md`.

---

## Running it

### With Aspire (recommended for development)

```bash
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

Dashboard: `https://localhost:17222`. Aspire starts PostgreSQL in Docker and wires StoreHub to it,
so no local PostgreSQL install is needed.

### StoreHub directly

Dev ports come from `src/IndyPOS.StoreHub/Properties/launchSettings.json`:

- `http` profile → `http://localhost:5012`
- `https` profile → `https://localhost:7150` + `http://localhost:5012`

An **installed** StoreHub listens on **`:5000`**.

### ⚠️ Trap 4 — the till points at the installed port, not the dev one

`src/IndyPOS.Windows.Forms/appsettings.json` sets `BaseUrl` to `http://localhost:5000`, while a
dev-run StoreHub listens on `:5012`. Running both straight from source, the till cannot reach the
API until you change one of them.

### Health endpoints

Registered in `src/IndyPOS.ServiceDefaults/Extensions.cs`:

| Path | Runs | Availability |
|---|---|---|
| `/health/live` | checks tagged `live` — process is up | always |
| `/health/ready` | checks tagged `ready` — dependencies healthy | always |
| `/health` | all checks, verbose body | **Development only** |

### ⚠️ Trap 5 — `/health` returns 404 on an installed service

That is by design: the verbose endpoint sits inside an `IsDevelopment()` branch because it leaks
implementation detail. **Use `/health/ready`** against a real install. A 404 on `/health` is the
wrong path, not a sick service.

### Store configuration (required by the till)

The Windows Forms app reads `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`. Create it before
running in Debug — see the *Store Configuration* section of `CLAUDE.md` for the exact shape.

---

## Where knowledge lives

| Source | Contents |
|---|---|
| `CLAUDE.md` | Architecture, layer rules, naming, entity and migration conventions |
| `.planning/indypos-overhaul/PLAN.md` | Roadmap, epics, open defects |
| `docs/architecture/`, `docs/development/` | Design and dev-environment docs |
| `docs/operations/` | Runbook, upgrade procedure, pilot checklist |
| `docs/superpowers/specs/`, `docs/superpowers/plans/` | Per-feature design specs and implementation plans |
| `.planning/indypos-overhaul/diagrams/` | Architecture and schema diagrams |

**`.claude/` is gitignored and will not exist in your clone.** That is deliberate: this repository is
public, and those files hold machine-specific paths and test credentials. `CLAUDE.md` and `PLAN.md`
are the committed sources of truth.

Real store databases under `.planning/indypos-overhaul/sqlite_database/` are also gitignored — they
contain live sales data. Tests that need them **skip silently** when absent, including in CI.

---

## Domain facts that will bite you

Non-obvious rules, each verified against the three real store databases. Treating any of them as a
bug and "fixing" it causes data loss.

### Legacy category ids collide across stores

The same numeric id means different things in different stores:

| id | GeneralHardware | MimyMart | MimyShop |
|---|---|---|---|
| 10 | เบ็ดเตล็ด (misc) | เบ็ดเตล็ด | **ของขวัญ (gifts)** |
| 20 | การเกษตร (agriculture) | การเกษตร | **ขนมและเครื่องดื่ม (snacks & drinks)** |

So no global id → category mapping can be correct. v4 replaces the old two-value enum with a
**store-scoped `product_category` table** keyed `(StoreId, Code)`, holding a stable English `Code`, a
Thai `DisplayName` and a `Kind` of `GeneralGoods` / `Hardware` / `Service`. Write the exact casing
from `ProductCategoryCodes`; comparisons are ordinal everywhere on purpose.

### `InvoiceProduct.IsTrackable` is dead data

Measured across all three stores: **602,114 invoice lines, every one `IsTrackable = 1`, not a single
`0`** — including 73,798 lines sold from products that *are* non-trackable. The legacy write path
omits the column, so SQLite applies its `DEFAULT 1`.

Read the flag from **`InventoryProduct`**, never the invoice line. Only 29 products across the three
stores are non-trackable, and that column *is* maintained.

### Legacy payment type ids

`PaymentType` is identical in all three stores. Ids 4 (`ม.33`) and 6 (`ผ่อนชำระ`) were never used.

| id | Thai | Means |
|---|---|---|
| 1 | เงินสด | Cash |
| 2 | ลงบัญชี | PayLater (store credit) |
| 3 | บัตรสวัสดิการแห่งรัฐ | WelfareCard (government campaign) |
| 5 | โอนเข้าบัญชี | MoneyTransfer — the **cashless** bucket (debit tap, Apple/Google Pay) |
| 7 | คนละครึ่ง | FiftyFifty (government campaign) |
| 8 | เราชนะ | WeWin (government campaign) |

Payment methods are a **store-scoped `payment_method` catalogue table**, not an enum — government
campaigns start and end without a redeploy. PayLater is a `GeneralHardware`-only invariant.

### Schema divergence

All three stores share exactly **10** tables: `InventoryProduct`, `Invoice`, `InvoiceProduct`,
`Payment`, `PaymentType`, `ProductBarcodeCounter`, `ProductCategory`, `User`, `UserCredential`,
`UserRole`.

GeneralHardware adds three and the other two add none, so the divergence *is* the PayLater feature:
`PayLater`, `Customers`, `Installments` (13 tables total). `Customers` and `Installments` are empty
everywhere and out of scope.

Note the source table is **`Payment`** — not `InvoicePayment`, which appears in some older test
fixtures and exists in no real store.

### Migrations are forward-only — this is a release gate

An upgrade rolls back binaries and config, **not schema**. Every migration in a release must run
against the *previous* release's binaries:

- Additive only; new columns nullable or defaulted
- No renames, no drops, no type narrowing
- No new `NOT NULL` column without a default

Breaking this makes the installer's rollback claim false. See `docs/operations/upgrade-procedure.md`.

### Entity conventions

New entities use `Guid Id`, plus `DateTime CreatedUtc` and `DateTime LastModifiedUtc`.

---

## Contributing

- Branch from **`development`** (the default branch; `main` is not used)
- Conventional commits; small, focused, logically grouped
- `development` is protected — changes land by **pull request**
- Run `dotnet test` **with Docker running**, and `dotnet test tests/IndyPOS.Bootstrapper.Tests`
  separately, before opening one
