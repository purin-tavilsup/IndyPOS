# Dev Store Profiles — Design

**Date:** 2026-10-07 · **Status:** approved in conversation (Pond), awaiting written-spec review
**Scope:** a new `IndyPOS.StoreProfiles` project, the AppHost, StoreHub's and CloudApi's Development
startup, and the StoreHub integration tests. Development and tests only: no production behaviour changes.
**Builds on:** the health-check convention branch (`feat/health-check-convention`): its
`WithHttpHealthCheck` on StoreHub and CloudApi, and StoreHub no longer waiting for CloudApi. This work
lands after it.

## 1. Why

IndyPOS serves three real stores with different features:

| Store | `StoreType` | What differs |
|---|---|---|
| GeneralHardware | `GeneralHardware` (1) | ลงบัญชี (PayLater), hardware categories, all payment methods |
| MimyMart | `Minimart` (2) | no PayLater; cash + transfer |
| MimyShop | `MimyShop` (4) | its own methods and service products (จัดส่ง, เอกสาร) |

Today a developer cannot easily see or test the other two:
- Locally nothing sets `Store:Type`, so **every dev run is GeneralHardware.**
- Running as another store means hand-setting environment variables (`Store__Type`, `Store__Id`, a
  connection string with a hyphen in its key) and starting StoreHub and the till by hand.
- Every store would also see the same 5 generic dev products, so MimyShop does not look like MimyShop.
- **Aspire runs one StoreHub on one persistent database** (`storehub-db`).
  - Development creates its schema with `EnsureCreated`, which never updates an existing database.
  - On 2026-10-07 that database lacked columns added weeks earlier (`must_change_password`,
    `created_by_user_id`), and every Aspire run of StoreHub crashed while seeding.
- **No test runs the same flow per store type.** A change can break MimyMart or MimyShop without any test
  noticing.
- **Dev stores cannot sync.** A store needs a cloud client id and secret, which only CloudApi's admin-only
  `/admin/stores/register` hands out. Nothing in dev does that.

## 2. What Pond asked for (2026-10-07)

- **Run the till as any store** with one command. What matters is the UI, the flows and the tests.
- **Several stores at once:** by default one chosen store, with a switch to start all three side by side
  against one CloudApi (for cloud-sync work).
- **Tests per store type:** API-level flow tests on StoreHub. UI automation waits for the Avalonia port.
- **No real data needed.** Seeded, store-appropriate data is enough; the MigrationTool is not involved.
- **Approach A:** one store-profile catalogue in code, shared by the AppHost, the dev seeder and the tests.

## 3. The store-profile catalogue

A new project, **`src/IndyPOS.StoreProfiles`** (`net10.0`, referencing only `IndyPOS.Domain`).

`StoreProfile` record:
- `Key`: `GeneralHardware`, `MimyMart` or `MimyShop`. This is what `--store` takes, matched without
  regard to case.
- `Type`: the `StoreType`.
- `StoreId`: `DEV-GENERALHARDWARE`, `DEV-MIMYMART`, `DEV-MIMYSHOP`. Stable across runs, so sync rows and
  cloud data line up.
- `Name` and receipt lines: what the till prints.
- `DevPort`: StoreHub's HTTP port, 5012 / 5013 / 5014. **GeneralHardware keeps 5012**, today's dev port.
- `Products`: 5–8 seed products that fit the store. Each has a barcode, a Thai name, a category code
  valid for that store type, a price and an initial stock.
  - GeneralHardware: hardware items.
  - MimyMart: groceries.
  - MimyShop: its two service products with the real barcodes **`2002500000014`** (จัดส่ง) and
    **`2002500000021`** (เอกสาร), which v4 must reuse verbatim, plus a few goods.

`StoreProfiles` (static):
- `All`: the three profiles;
- `Default`: GeneralHardware;
- `Find(string key)` → `StoreProfile?`;
- `ForType(StoreType)` → `StoreProfile?`.

What it does **not** hold:
- **Categories and payment methods.** They are already seeded per store type
  (`ProductCategorySeeder.SeedsFor`), or seeded whole and filtered by type at runtime
  (`PaymentMethodPolicy`). They follow `Type` on their own.
- **Users.** The dev users (`cashier`, `manager`, `admin`) are the same for every store.
- **Test expectations** (§6).

Adding a store type later means adding one entry.

## 4. Aspire

### 4.1 Choosing stores

```
dotnet run --project src/IndyPOS.AppHost                       # GeneralHardware, as today
dotnet run --project src/IndyPOS.AppHost -- --store MimyMart   # one chosen store
dotnet run --project src/IndyPOS.AppHost -- --store all        # all three side by side
```

- The value comes from the AppHost's configuration (`store`). An environment variable or launch-profile
  argument works too.
- **An unknown key stops the AppHost** before anything starts, with a message listing the valid keys.

### 4.2 Per selected profile

- **Database:** `storehub-<key>`, lower case (e.g. `storehub-mimymart`), on the shared Aspire Postgres.
  Stores never share data. The old `storehub-db` is simply left unused.
- **StoreHub:** resource `storehub-<key>`, with:
  - its `http` endpoint on the profile's `DevPort`;
  - `Store__Type`, `Store__Id` and `Store__Name` from the profile;
  - its database reference;
  - its CloudApi reference (service discovery) and credentials (§4.3);
  - `WithHttpHealthCheck("/health/ready")`;
  - `WaitFor(postgres)` only. **It never waits for CloudApi** (offline-first).
- **Till:** resource `till-<key>`, with:
  - explicit start, as today;
  - `StoreHub__BaseUrl` = that StoreHub's URL;
  - `Store__ConfigPath` = a `StoreConfiguration.json` the AppHost generates for the profile under the
    AppHost's own output folder, from the profile's name and receipt lines, printer `XP-58`;
  - `WaitFor` its StoreHub.

  **The developer's real `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json` is never read or
  written.**
- **When one store is chosen,** it keeps today's resource names (`storehub-api`, `winforms-app`), so the
  dashboard and habits stay the same. The `<key>` names are used with `--store all`.

### 4.3 One CloudApi, and dev cloud credentials

- **One `cloud-api` resource** serves every selected store.
- **CloudApi gets a Development-only seeder** that registers each profile's store as a cloud client:
  - idempotently, through the same code `/admin/stores/register` uses;
  - client id = the profile's `StoreId`;
  - secret = a fixed, documented dev value.
- **The AppHost passes the matching `CloudApi__ClientId` / `CloudApi__ClientSecret`** to each StoreHub.
- **Production is unchanged:** stores are registered only through the admin route. The seeder runs only
  when the environment is Development, and a test pins that it does nothing outside Development.

## 5. Development startup

- **StoreHub applies EF migrations in Development** (`MigrateStoreHubDatabaseAsync`, the same as the
  installer's `migrate` step), instead of `EnsureCreated`.
  - Every profile's database is new (§4.2), so the first migrate starts clean.
  - From then on, new columns arrive automatically.
- **CloudApi does the same:** migrations in Development, on a fresh database name (`cloud`), since its
  `cloud-db` was also built by `EnsureCreated`.
- **The dev seeder** (`DevelopmentDataSeeder`):
  - seeds the products of `StoreProfiles.ForType(Store:Type)`;
  - if no profile matches, keeps today's 5 generic products, so a plain `dotnet run` of StoreHub still
    works;
  - stays idempotent;
  - keeps users and settings as today.
- **`StoreHub` references `IndyPOS.StoreProfiles`** only for this Development seeding. Nothing on the
  production path reads a profile.

## 6. Tests (StoreHub integration, real Postgres, runs in CI)

- **A per-profile host.** The test factory gains a way to boot StoreHub as a profile:
  - its `Type` and `StoreId`;
  - its own `TestPostgres` database;
  - its products seeded.

  Each profile's host is built once and reused by all the profile's tests (a cached fixture per profile),
  so the suite starts three hosts, not one per test.
- **The same flows run once per store.** The theory data is the three profile keys. Negative cases
  first:
  1. `PayLaterSale_WithAStoreWithoutPayLater_ReturnsBadRequest`, with the Thai 400 (MimyMart and MimyShop
     rows).
  2. `PayLaterSale_OnGeneralHardware_CreatesTheDebt`.
  3. `OfferedPaymentMethods_ForEachStore_MatchTheStoresSet`. **The expected set is written in the test,
     per store, never read from the code under test**, so a policy change that drops a method fails.
  4. `ProductCategories_ForEachStore_MatchTheStoreType` (expected codes written in the test).
  5. `StoreFeatures_ForEachStore_ReportTheTypesFeatures`.
  6. `CashSale_ForEachStore_Completes`, using one of that profile's products.
  7. `DevSeedProducts_ForEachStore_AreAllSellable`: valid category for the type, positive price, stock
     present.
- **The catalogue itself** has unit tests:
  - keys are unique and match without regard to case;
  - an unknown key gives `null`;
  - each `StoreType` has exactly one profile;
  - `DevPort`s are unique;
  - MimyShop's service barcodes are exactly `2002500000014` and `2002500000021`.
- **CloudApi:** `DevStoreSeeder_OutsideDevelopment_RegistersNothing` and
  `DevStoreSeeder_RunTwice_RegistersEachStoreOnce`, on the CloudApi host from the health-check work.
- **AppHost:** no automated test.
  - Verified by hand: `--store MimyShop` shows the MimyShop products and methods in the till; `--store all`
    starts three healthy StoreHubs on 5012/5013/5014 that each sync a sale to the one CloudApi; an unknown
    key stops with the list.
  - Recorded in the PR.
- **Not in this work:** UI automation; real store data.
- Counts are measured, never derived. `CLAUDE.md` and `ONBOARDING.md` are updated.

## 7. Docs

- **ONBOARDING:**
  - a short "Running as a store" section: the three commands, what each store shows, and that dev stores
    sync with seeded credentials;
  - the Aspire section names the per-store databases and notes that the old `storehub-db` can be dropped.
- **CLAUDE.md:**
  - Quick Commands gains the `--store` examples;
  - the "Store Configuration (Required for Debug)" section says the AppHost generates it per store, and the
    hand-made file is only needed when running the till without Aspire.

## 8. Left out, on purpose

- **Real store data**, and the MigrationTool, in this flow.
- **UI automation per store.**
- **A store picker inside the till.** The store is a property of the StoreHub the till talks to, as in
  production.
- **Changing production startup or the installer.** Production still uses the installer's `migrate` and
  the admin registration route.

## 9. Success

- One command runs StoreHub plus the till as any of the three stores, with that store's products, payment
  methods and receipt header.
- One flag runs all three side by side, each on its own database, all syncing to one CloudApi.
- The same flows are tested once per store type in CI. A change that breaks one store's methods, PayLater
  rule or categories fails a test.
- A schema change no longer crashes a developer's Aspire run.
- Nothing changes in production.
