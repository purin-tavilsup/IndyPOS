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
| **Docker Desktop** | Required by **476** of the tests (82 + 328 + 58 + 8, derived — see Trap 1) and by Aspire |
| **Windows** | Several projects target `net10.0-windows`; the till is Windows Forms |
| **FC Subject font** | In [`fonts/`](fonts/) — install `Regular` and `Bold`. Every panel names this family explicitly, so without it Windows substitutes a fallback and Thai captions clip |

---

## Build and test

```bash
dotnet build
dotnet test
```

### ⚠️ Trap 1 — four suites need Docker and fail loudly without it

They spin up a real PostgreSQL container via Testcontainers. With Docker stopped they fail **fast**
(each suite in under a second), which reads exactly like a code regression but is not.

Expect **476 failures** with Docker stopped, all from these four suites. The StoreHub row's **128**
was **measured** with Docker down on 2026-09-27; the final-review test additions then added 13 more
HTTP integration tests (`CashFloatEndpointsTests`, `DebtRepaymentEndpointsTests`, plus new cases in
`CashAuthorizationTests` and `CashPayoutEndpointsTests`) and the concurrent-delete fix added 7
real-Postgres race tests (`CashEntryConcurrencyTests`), all of which need Docker the same way, so its
**148** is **derived** (128 + 20), not re-measured; the PayLater-debt fix then added 10 more HTTP integration tests (`PayLaterSaleEndpointTests`), bringing it to **158**, and the sale-user fix 2 more, bringing it to **160**, all derived. (`TestUserIdTests` was added after that and needs
no container, so it does not change the count.) The invoice-history plan (2026-10-01) then added 71 more StoreHub tests (HTTP and persistence tests on real Postgres), bringing it to **231**, derived on the same assumption that every new one needs a container; the final-review fix added one more HTTP test (the reprint route's 403), making **232**, and the Codex route fix (one HTTP test, 2026-10-01) **233**, both derived the same way. The other two rows are **derived** too: MigrationTool's 11 new tests all use the Postgres fixture (**71 + 11 = 82**), and CloudApi's 20 new tests (2026-09-30, the event-pipeline repair and the sync store check) all run on a container, as do the 22 more from invoice-history plan 2 (2026-10-02, 22 to **44**). The route tidy-up PR A (2026-10-02) then added 26 StoreHub tests and 5 CloudApi tests, all on a container, bringing them to **259** and **49**, derived the same way. Route tidy-up B (2026-10-06) added 22 more StoreHub tests, all on a container, bringing it to **281**, derived the same way. The health-check convention (2026-10-07) added 3 StoreHub and 2 CloudApi tests and the new 8-test `IndyPOS.ServiceDefaults.Tests`, all on a container: **284**, **51** and **8**, derived the same way. The dev store profiles (2026-10-07) added 20 StoreHub and 3 CloudApi tests, all on a container: **304** and **54**, and its review fixes one more each (**305** and **55**), then the EnsureCreated-database check 4 and 3 more (**309** and **58**), and per-store UI slice 2 19 more StoreHub (**328**), derived the same way. The per-suite totals were measured with Docker up; only the Docker-down split was not:

| Suite | Total | Fails without Docker |
|---|---|---|
| `IndyPOS.MigrationTool.Tests` | 145 | **82** (of the other 63, see Trap 3) |
| `IndyPOS.StoreHub.IntegrationTests` | 336 | **328** (8 need no container, derived) |
| `IndyPOS.CloudApi.IntegrationTests` | 58 | **58** (all need a container) |
| `IndyPOS.ServiceDefaults.Tests` | 8 | **8** (each fixture starts a Postgres) |

**Start Docker and re-run before investigating any of these.**

### Continuous integration and running without Docker

GitHub Actions (`.github/workflows/ci.yml`) builds the solution and runs every suite, installer
included, on each PR to `development` and on each push to it. The job runs on Windows, which cannot
run Linux containers, so it uses the PostgreSQL service preinstalled on the runner instead of Docker.

The seam is `tests/IndyPOS.Testing.Postgres`. If the environment variable `INDYPOS_TEST_POSTGRES`
holds a full Npgsql connection string, each fixture creates its own empty `indypos_test_<guid>`
database on that server and drops it afterwards. If it is unset, each fixture starts a container as
before. To reproduce CI locally (any PostgreSQL 16+ will do):

```bash
docker run -d --name ci-pg -e POSTGRES_PASSWORD=pass -p 55520:5432 postgres:17-alpine
export INDYPOS_TEST_POSTGRES='Host=localhost;Port=55520;Username=postgres;Password=pass'
dotnet test            # the Docker-dependent suites now use ci-pg
dotnet test tests/IndyPOS.Bootstrapper.Tests
docker rm -f ci-pg
```

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

So `IndyPOS.MigrationTool.Tests`'s 145 break down as: **82** need Docker, **24** need those real
databases, **38** are pure units needing neither, and **1** is the manual schema extractor (Trap 3b).

**The total itself changes.** A skipped `[Theory]` is one skipped entry, not one per data row, so the
21-case artefact comparison collapses to a single entry. On a fresh clone the suite therefore
discovers **125**, not 145 (**derived** as 145 − 20, not measured — the same 20-row collapse described
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
— **1333 total** (1332 pass, 1 skipped) — measured 2026-10-07 (after per-store UI slice 2;
supersedes 1291 earlier on 2026-10-07 after slice 1, 1269 earlier on 2026-10-07 after the dev store profiles and their EnsureCreated-database check, 1209 earlier on 2026-10-07 after the health-check convention, 1186 earlier on 2026-10-07 after the till startup fix, 1183 on 2026-10-06 after route tidy-up B, 1152 on 2026-10-02 after route tidy-up A, 1111 earlier on 2026-10-02, 1089 on 2026-10-01, 928 on 2026-09-30, 853 on 2026-09-27 and 656 on 2026-09-17). Without those databases the total is **1313**
(derived as 1333 − 20; CI measured 1166 = 1186 − 20 on PR #114, 2026-10-07, which has no store databases), still all
green:

| Suite | Tests |
|---|---|
| `IndyPOS.Application.Tests` | 628 |
| `IndyPOS.StoreHub.IntegrationTests` | 336 (Docker) |
| `IndyPOS.MigrationTool.Tests` | 145 (Docker; 1 skipped. **125** without the real store data — Trap 3, derived) |
| `IndyPOS.Domain.Tests` | 61 |
| `IndyPOS.Windows.Forms.Tests` | 74 |
| `IndyPOS.Vault.Tests` | 17 |
| `IndyPOS.CloudApi.Tests` | 6 |
| `IndyPOS.CloudApi.IntegrationTests` | 58 (Docker) |
| `IndyPOS.ServiceDefaults.Tests` | 8 (Docker) |

Outside the solution: `IndyPOS.Bootstrapper.Tests` — **235** (227 pass, 8 skipped).

`tests/IndyPOS.Mock` is shared fakes, not a test project — it is marked `IsTestProject=false` so the
runner skips it. Without that, `dotnet test` tried to execute the assembly, failed to load it, and
the whole solution exited **1 on a fully green run**.

> **The SQLite → PostgreSQL migration paths now have real coverage.** `tests/IndyPOS.Migration.Tests`
> was deleted, not repaired — it exercised a parallel migration implementation the product never
> referenced, against a SQLite schema no real store has, so its 15 green tests were misleading.
> `tests/IndyPOS.MigrationTool.Tests` replaces it: the **shipped** migrator, run against schema
> artefacts dumped from real stores. Defects 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18,
> 19, 20 and 21 are fixed — **the whole defect list is closed** — and all three real stores migrate
> and verify end to end. **Nothing is pinned any more.** Several of those tests now pin a DECISION instead — which columns are deliberately
> not migrated, for instance — so read the comment before changing one. And re-measure a premise before
> trusting it: defect 6's turned out to be wrong. See Epic 2 in `PLAN.md`.

---

## Running it

### With Aspire (recommended for development)

```bash
dotnet run --project src/IndyPOS.AppHost --launch-profile https
```

Dashboard: `https://localhost:17222`. Aspire starts PostgreSQL in Docker and wires StoreHub to it,
so no local PostgreSQL install is needed.

### Running as a store

| Command | Starts |
|---|---|
| `dotnet run --project src/IndyPOS.AppHost` | GeneralHardware (ลงบัญชี, hardware products), StoreHub on :5012 |
| `dotnet run --project src/IndyPOS.AppHost -- --store MimyMart` | MimyMart (no ลงบัญชี, groceries) |
| `dotnet run --project src/IndyPOS.AppHost -- --store MimyShop` | MimyShop (service products จัดส่ง / เอกสาร) |
| `dotnet run --project src/IndyPOS.AppHost -- --store all` | all three, on :5012 / :5013 / :5014, syncing to one CloudApi |

- **Each store has its own database** (`storehub-generalhardware`, …), seeded from `IndyPOS.StoreProfiles`, and
  applies migrations on start. The old `storehub-db` and `cloud-db` databases are no longer used; drop them
  if you like. They were built by `EnsureCreated` and cannot be migrated, so a direct StoreHub or CloudApi
  run pointed at one stops at start with a message saying so, instead of a "relation already exists" error.
- **The till follows the store type** (`GET /store/features`, applied by `TillLayout`):
  - the sale panel's ฮาร์ดแวร์ button shows only for GeneralHardware;
  - the จัดส่ง / เอกสาร service buttons show only for MimyShop;
  - the รายการลงบัญชี menu shows only for GeneralHardware, and the buttons below it move up;
  - in Reports, the overview's money rows follow the store's payment methods (plus จัดส่ง / เอกสาร at
    MimyShop), the general/hardware and ลงบัญชี tiles and the ยอดการลงบัญชีค้างชำระ tab are GeneralHardware-only,
    and products sold lists every sold line with its bill number (`GET /sales/lines`).
- **Dev stores sync** with a fixed dev secret that CloudApi registers in Development only.
- **The till's receipt header** comes from a generated file under `src/IndyPOS.AppHost/obj/dev-stores/`.
  Your `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` is not used.

### StoreHub directly

Dev ports come from `src/IndyPOS.StoreHub/Properties/launchSettings.json`:

- `http` profile → `http://localhost:5012`
- `https` profile → `https://localhost:7150` + `http://localhost:5012`

An **installed** StoreHub listens on **`:5000`**.

### ⚠️ Trap 4 — the till points at the installed port, not the dev one

`src/IndyPOS.Windows.Forms/appsettings.json` sets `BaseUrl` to `http://localhost:5000`, while a
dev-run StoreHub listens on `:5012`. Running both straight from source, the till cannot reach the
API until you change one of them. **Under Aspire this does not apply:** the AppHost points each
till at its own store's StoreHub.

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
wrong path, not a sick service. Since 2026-10 every caller in the code, scripts and runbooks uses
`/health/ready`; the first-run wizard's "Test connection" used to probe `/health` and always reported a
404.

### Store configuration (required by the till)

The Windows Forms app reads `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`. **Under Aspire you
do not need it:** the AppHost generates one per store. Create it only to run the till without Aspire —
see the *Store Configuration* section of `CLAUDE.md` for the exact shape.

---

## Where knowledge lives

| Source | Contents |
|---|---|
| `CLAUDE.md` | Architecture, layer rules, naming, entity and migration conventions |
| `.planning/indypos-overhaul/PLAN.md` | Roadmap, epics, open defects |
| `docs/architecture/`, `docs/development/` | Design and dev-environment docs |
| `docs/architecture/api-conventions.md` | StoreHub route rules, and how to rename a route after go-live |
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
