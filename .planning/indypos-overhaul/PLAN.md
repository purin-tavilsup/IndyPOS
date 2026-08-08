# IndyPOS Overhaul - Implementation Plan

**Last Updated:** 2026-07-29
**Where we actually are:** v4 is feature-complete and VM-validated, but **no store is running it yet**.
All three stores still run v3.7.0 on SQLite. The critical path is getting them onto v4 with their
history intact — not cloud infrastructure.

> **This replaces the 2026-04-27 revision**, which claimed "~98% complete (Local Ready, Cloud
> Infrastructure Pending)". That framing was wrong on the most important axis: local deployment had
> never been exercised on a real store, and three months of installer, security and store-type work
> has landed since.

---

## The critical path

| # | Epic | Status | Why this order |
|---|------|--------|----------------|
| 1 | **Product categories + MimyShop** | ✅ **SHIPPED 2026-07-31 (PR #55)** | The migration needs a category model to map into. Blocked 2 |
| 2 | **SQLite → PostgreSQL migration hardening** | 🚧 **IN PROGRESS** — test harness landed; defects 2, 3, 4, 10, 11, 12 fixed; 5/6/7/8/13 pinned; 9 open | 3 divergent legacy schemas. The risky half of the rollout |
| 3 | **Store rollout** (fresh v4 install + migrate, per store) | ⏳ Blocked by 2 | Where the business value lands: stores off a 3.7.0-era system |
| 4 | **Epic I: Cloud infrastructure** | 🔴 Not started | Additive. No longer on the critical path — see "Legacy history" below |
| 5 | **Epic MCP: agent-facing API** | 🔴 Not started | Depends on 4 |
| 6 | **Store-type panel consolidation** (3 forked apps → 1) | 🔵 **NEW candidate** — analysed, not spec'd | Retires the MimyMart/MimyShop forks. Scheduled after 3; see `findings-2026-07-31.md` §B |

**Legacy history reaches the cloud for free.** After migration each store's own v4 PostgreSQL holds
its full legacy history, so ordinary sync carries it upward. There is no separate legacy→cloud
pipeline to build. The consequence is that "the cloud needs legacy history" is not a new component —
it is a **correctness requirement on epic 2**.

> **📎 Read alongside this plan:
> [`findings-2026-07-31.md`](findings-2026-07-31.md)** — cross-repo analysis produced after Epic 1
> merged. It carries material this plan only summarises:
> **§A** entity identity (defects 8-9 below, and the UUIDv7 window) ·
> **§B** the three-app consolidation, measured, incl. **defect B3** — the sales report's
> hardcoded campaign labels defeat Epic M, so a new government campaign collects money that
> never appears in the report ·
> **§C** MimyShop's service requirement, its two blockers, and the pinned service barcodes.
>
> A pattern runs through all three: **v4 repeatedly makes something data-driven and leaves one
> hardcoded remnant behind.** Epic M catalogued payment methods but left four hardcoded report
> labels; Epic 1 catalogued categories but the `Service` kind is locked out by a flag doing
> double duty; `SalePanel` gates hardware by feature flag but hardcodes its template barcodes.
> Worth naming explicitly when epic 6 is spec'd.

---

## Shipped since the last revision (2026-05 → 2026-07)

All merged to `development`. Specs in `docs/superpowers/specs/`, plans in `docs/superpowers/plans/`,
per-task ledger in `.superpowers/sdd/progress.md`.

| Epic | What | Merged |
|------|------|--------|
| **Vault (DPAPI)** | `IndyPOS.Vault` + `SecretProtector`; connection string and JWT key DPAPI-protected at rest, machine scope, `DPAPI:` marker; plaintext `storehub.key` dropped; `appsettings.json` ACL-locked | 2026-06 |
| **Admin provisioning** | Random single-use bootstrap credential, server-side force-rotate on first login (`must_change` JWT claim + middleware gate), `POST /auth/change-password`, `reset-admin` CLI recovery | PR #50 |
| **Silent installer** | `IndyPOS-Setup.exe --silent --store-id <ID>`; ACL-locked log, `INDYPOS_MARKER` lines, exit codes 0/1/2/3/4 | PR #51 |
| **Epic M: data-driven payment methods** | `payment_method` catalogue replaces the hardcoded `PaymentType` enum; store-type gating; `PaymentMethodKind` taxonomy (`Standard`/`GovernmentCampaign`/`Special`); admin management screen; fixed the silent-GeneralHardware default | 2026-07-19 |
| **Product-type restriction** | `GET /store/features`; server rejects Hardware products on general-only stores; WinForms hides Hardware affordances | 2026-07-19 |
| **Cosmetic-minors batch** | Kind taxonomy re-spec, Thai Kind labels, grid refresh, payment-button icons, `PUT /products/{id}` → 404, one-shot features-error dialog | PR #52 |
| **Installer upgrade support** | In-place upgrade with verified rollback. See below | PR #54 |
| **Epic 1: product categories + MimyShop** | Store-scoped `product_category` catalogue replaces the two-value enum; `StoreType.MimyShop`, `CoffeeShop` retired with value 3 reserved; `GET /product-categories`; create/update gate on category `Kind`. Verified on 3 fresh VM installs (16/17/10 rows) + live UI | PR #55 |
| **Global UI error handling** | No unhandled WinForms exception can show the default English crash dialog or go unlogged. Thai message + `ERR-XXXX` reference code, same code in the log. Three framework channels wired. `IndyPOS.Windows.Forms.Tests` 0 → 19 tests. VM-verified end to end | PR #56 |
| **EAN-13 barcode fix** | The generator emitted 9 digits (`{StoreCode}{Sequence:D8}`), which EAN-13 cannot encode, so every generated barcode crashed the add-product dialog. Now `200 + {StoreCode:D2} + {Sequence:D7}` + check digit, with two real shelf labels as test vectors | PR #55 |

### Installer upgrade support (PR #54, open)

14 planned tasks plus 7 bugs found by the VM gate and whole-branch review. Design's load-bearing
idea is a **mutation line**: steps 0–3 touch nothing, 4–7 roll back and *prove* recovery with a
health probe, step 8 (POS app) does not roll back. `DatabaseSetup` never runs on an upgrade, which
is what makes the config-clobbering bug disappear rather than be worked around.

VM-validated: happy upgrade 4.0.0 → 4.1.0 (22/22, `POS_UPDATED=true`), forced mid-upgrade rollback
(store came back healthy), fresh-install regression (18/18). Version is now **4.1.0**.

Notable finds worth remembering: locking a directory with non-inheritable ACEs leaves its children
with an **empty DACL** (broke every backup silently); and the VM harness could report a green run
that never happened, by grading a crashed run against an `install-*.log` baked into the snapshot.

---

## Epic 1: Product categories + MimyShop ✅ SHIPPED (PR #55, 2026-07-31)

**Problem.** `ProductCategory { GeneralGoods = 10, Hardware = 50 }` is hardcoded, and `Product.Category`
stores the enum *name*. The real stores have 16 / 11 / 17 fine-grained Thai categories, and **the ids
collide with different meanings across store types**:

| id | GeneralHardware | MimyMart | MimyShop |
|----|-----------------|----------|----------|
| 10 | เบ็ดเตล็ด (misc) | เบ็ดเตล็ด | **ของขวัญ (gifts)** |
| 11 | เครื่องดื่ม (drinks) | เครื่องดื่ม | **ของเล่น (toys)** |
| 18 | ของเล่น (toys) | ของเล่น | **ของใช้ในบ้าน (household)** |
| 50–54 | วัสดุ* (hardware) | — | — |

So any global id→category mapping corrupts one store to fix another.

**Approach (approved):** mirror Epic M. A store-scoped `product_category` table —
`StoreId, Code, DisplayName, Kind, DisplayOrder, IsEnabled` — with readable English `Code`s
(`Gifts`, `PlumbingMaterials`, `Services`), Thai `DisplayName`, and
`Kind ∈ { GeneralGoods, Hardware, Service }`. `Kind` replaces the
`string.Equals(category, nameof(ProductCategory.Hardware))` comparisons in the create/update handlers
and the legacy sales-summary handler. The enum and `HardcodedStoreConstants.ProductCategories` go away.

Shared codes where meanings genuinely match (`Toys`, `Household`, `Miscellaneous`) make cross-store
reporting a plain `GROUP BY code` — which legacy-id codes would have made silently wrong.

**Store types:** add `MimyShop = 4` with Minimart-equivalent flags (no PayLater, single product type).
**Remove `CoffeeShop`** — Pond's call: its products and services differ enough to deserve a dedicated
app. Check no persisted config or DB row carries `Type=CoffeeShop` before deleting; do not reuse `3`.

**Out of scope (YAGNI):** an admin category screen (campaigns churn, categories don't), and any
non-stock/service *behaviour* — `Kind=Service` is data only for now.

**Anticipated later divergence for MimyShop** (do not build now): it sells services, and its
reporting/receipt needs differ. Payment methods will *converge* — MimyMart is expected to offer
MimyShop's set eventually.

---

## Epic 2: SQLite → PostgreSQL migration hardening 📋

**This is the risky half of the rollout.** The tool was written against an assumed schema and appears
never to have been run end-to-end against a real store database.

**Evidence base:** real store DBs now at `.planning/indypos-overhaul/sqlite_database/{GeneralHardware,MimyMart,MimyShop}/Store.db`
(gitignored). Volumes: 139,680 / 97,293 / 15 invoices.

### Legacy schema divergence — confirmed, not assumed

All three share exactly **10** tables (`InventoryProduct`, `Invoice`, `InvoiceProduct`, `Payment`,
`PaymentType`, `ProductBarcodeCounter`, `ProductCategory`, `User`, `UserCredential`, `UserRole`) —
**corrected 2026-08-01 from "11", measured by set intersection.** GeneralHardware adds three
(`PayLater`, `Customers`, `Installments`) for 13 total; MimyMart and MimyShop add none. So the
divergence *is* the PayLater feature. `PaymentType` is identical in all three (8 rows), so the
payment mapping is store-agnostic.

`Customers` and `Installments` are **never used** (0 rows where present) — out of scope permanently.
Legacy payment id 6 `ผ่อนชำระ` is dead with them.

### Defects

| # | Defect | Impact | Status |
|---|--------|--------|--------|
| 1 | Payment mapping **shifted**: PayLater→`"Card"`, WelfareCard→`"Transfer"`, MoneyTransfer→`"WelfareCard"`, campaigns→`"Other"` | ~15% of ฿21.2M attributed to the wrong method; ฿836k of PayLater credit invisible | ✅ Fixed `af4ea14` |
| 2 | `MigratePayLaterAsync` selects `PayLaterId, UserId, CustomerName, PaymentAmount` — **none exist** (real: `PaymentId, Description, PayLaterAmount, PaidAmount`) | Throws; aborts the whole migration. 5,181 rows / ฿836k | ✅ Fixed `926a245` |
| 3 | Same method queries `FROM PayLater` **unconditionally** | Throws "no such table" on MimyMart and MimyShop | ✅ Fixed `926a245` |
| 4 | `ParseDate` does `SpecifyKind(..., Utc)` on `datetime('now','localtime')` values | **Every timestamp 7h off**; corrupts all daily/monthly totals | ✅ Fixed `28fef5f` |
| 5 | `Category = product.Category?.ToString()` writes the raw numeric id | All products uncategorised; breaks the Hardware gate and pickers | ❌ (needs Epic 1) — **pinned** `3ae92f1` |
| 6 | `InvoiceProduct` selects 7 of 17 columns, dropping `OriginalUnitPrice`, `GroupPrice`, `IsGroupProduct`, `Note`, `Priority` | **165,690 of 325,780 line items (51%)** were discounted; the record is lost | ❌ — **pinned** `3c73f11` |
| 7 | v4 `Core.Product` has no `IsTrackable`; legacy does (21/7/1 non-trackable products) | Services would be stock-tracked and inventory-deducted | ❌ **priority raised** — **pinned** `3ae92f1` |
| 8 | **Legacy ids are not preserved** on products, invoices, invoice lines or payments (only `StoreUser.LegacyUserId` is) | The migration cannot be re-run idempotently, and a v4 row cannot be reconciled against its SQLite source | ❌ **new** — **pinned** `3ae92f1` |
| 9 | `LegacyIdHelper` is dead code that maps the same legacy id to the same Guid **in every store** | Latent cross-store collision once Epic I syncs three shops into one cloud. Delete it | ❌ **new** — not pinned (dead code; deletion, not behaviour) |
| 10 | `MigratePayLaterAsync` **creates a second `Payment`** for money `MigrateInvoicesAsync` has already migrated. `PayLater.PaymentId` is an FK to `Payment.PaymentId` — 5,181/5,181 match, all `PaymentTypeId=2` | **฿836,013 double-counted.** Unreachable today because defect 2 crashes first, so fixing 2 alone turns a loud crash into silent revenue inflation | ✅ Fixed `cf61005` |
| 11 | `ParseDate` calls bare `DateTime.TryParse` with **no `CultureInfo`**, and the tool runs on the store's Thai-locale till | Under `th-TH` the Buddhist calendar reads `2024` as a BE year → **1481 AD, 543 years off**. Every invoice falls outside every date-range report, so a migrated store shows **zero** sales history | ✅ Fixed `28fef5f` |
| 12 | **A phase-level throw still discards the whole run.** The `sqlite_master` probe and the `PayLater` SELECT sit *outside* any `try`, `MigrateAllAsync` does not wrap its four phase calls, and `SaveChangesAsync` runs after the last phase | Defects 2 and 3 were fixed at the symptom (right columns, a table probe) but **the amplifier remains**: the next column-level drift — a store whose `PayLater` lacks `PaidAmount`, say — again throws before any save, so an entire migration is discarded instead of one phase failing | ✅ Fixed `b28cb08` |
| 13 | **An invoice line whose product no longer exists is silently dropped.** `MigrateInvoicesAsync` `continue`s when `ProductIdMap` has no entry for the line's `InventoryProductId` | **1,980 real GeneralHardware lines** reference a deleted product, so those invoices migrate with fewer lines than they had — an invoice's lines can sum to less than its own `TotalAmount`, with only a log warning. `ProductName` is already a historical snapshot on `InvoiceLine`, so the line could be preserved without the product | ❌ **new 2026-08-04** — **pinned** `3c73f11` (`MigrateInvoiceLines_WithADeletedProduct_CurrentlySkipsTheLine`) |

> **"Pinned" means there is an executable test asserting today's WRONG behaviour**, naming the defect
> and recording the correct answer in its message. Each was verified able to fail. When you fix one,
> invert exactly one pinning test — see `tests/IndyPOS.MigrationTool.Tests`.
>
> Defects 4 and 11 shared the same six-line `ParseDate`, but their pins were independent: fixing the
> culture flipped only 11's pin, and fixing the offset flipped only 4's. Verified both ways, and
> confirmed again at the moment of the fix — with both pins inverted and the old code still in place,
> defect 4's test reported `14:30` vs `07:30` while defect 11's reported `1481-03-15 14:30` vs
> `2024-03-15 07:30`. Both are now ✅ (`28fef5f`); the two tests survive as ordinary assertions.
>
> **Why `InvariantCulture` is safe on real data.** Measured before choosing it: all **493,845**
> timestamps across `Invoice`, `Payment`, `InventoryProduct`, `User` and `PayLater` in all three
> stores use exactly one shape, `YYYY-MM-DD HH:MM:SS`, with Gregorian years 2021-2026 and zero
> Buddhist-era values. The legacy write path is SQLite's `datetime()`, never .NET `ToString()`, so no
> real row can fail to parse and fall back to `DateTime.UtcNow`.

> ### ✅ RESOLVED 2026-08-04 — kept for the reasoning. Was: the migration could not complete against ANY real store (measured 2026-08-03)
>
> Fixed by `926a245` (defects 2 and 3) with `cf61005` (defect 10) in the same branch, because fixing 2
> alone would have converted a loud crash into silent revenue inflation. A test now runs the shipped
> migrator to completion against both real store shapes and asserts rows are persisted.
> The analysis below stands as the record of why count-based verification never caught any of it.
>
> **The structural cause was fixed separately, as defect 12 (`b28cb08`).** Those two fixes corrected
> the *symptom* — the wrong column names and the missing table probe. What made a single wrong column
> name mean "no store can migrate at all" was the paragraph below: a phase-level throw escaping past
> `SaveChangesAsync`. Each phase is now isolated, so one run reports every schema problem, while the
> save stays gated on there being no phase failure — atomicity preserved deliberately rather than by
> an escaping exception.
>
> The `PayLater` SELECT run against each real `Store.db`:
>
> ```
> GeneralHardware   -> no such column: PayLaterId
> MimyMart          -> no such table: PayLater
> MimyShop          -> no such table: PayLater
> ```
>
> That `QueryAsync` sits **outside** the per-row `try` (line 338 vs the `try` at 346),
> `MigrateAllAsync` does not wrap its four phase calls, and **`SaveChangesAsync` is line 55 — after
> `MigratePayLaterAsync` at line 51.** So the throw escapes before anything is saved.
>
> This is not "PayLater is skipped": the tool does the entire migration in memory and then **discards
> all of it**. It has only ever completed against the fabricated test schema. Defects 4, 5, 6, 7, 8
> and 11 are all observed by inspecting migrated rows, so **none of them can be observed at all**
> until defects 2 and 3 are fixed.
>
> Also measured: `PaidAmount` is a real maintained column that the code **derives away** from
> `IsCompleted` (line 387), destroying the 2 genuinely part-paid rows of 5,181 (e.g. ฿700 owed, ฿299
> paid); and `Payment` has **5,182** type-2 rows against 5,181 PayLater rows (฿836,083 vs ฿836,013),
> so the relationship is **not 1:1 in the Payment→PayLater direction** — one ฿70 orphan exists and no
> fix may assume otherwise. The **PayLater→Payment** direction *is* total: all 5,181 PayLater rows
> resolve to an existing type-2 `Payment`, zero orphans, which is what defect 10's fix relies on and
> what `RealStore_PayLaterShouldExtendPayment_OneToOne` pins. Both statements are true; mind the
> direction.

**Defect 7 reframed (2026-07-31).** It is not merely "legacy has a field we omitted" — the legacy
sale path **already filters on `IsTrackable` before touching stock, in production**
(`MimyShop SaleService.cs:413-418`). v4 dropped a working guard, and
`CompleteSaleCommandHandler` now deducts stock for every line unconditionally. This is the
difference between v4 supporting services at all and v4 driving service stock permanently
negative, so it is no longer only a migration concern.

> ### ⚠️ Defect 7 addendum (2026-08-01): read `IsTrackable` from the PRODUCT, never the invoice line
>
> `InvoiceProduct.IsTrackable` exists and looks authoritative. It is **dead data** — measured across
> all three real store DBs:
>
> | Store | `InvoiceProduct` lines | `IsTrackable = 1` | `IsTrackable = 0` |
> |---|---|---|---|
> | GeneralHardware | 325,780 | **325,780** | **0** |
> | MimyMart | 276,317 | **276,317** | **0** |
> | MimyShop | 17 | **17** | **0** |
>
> **602,114 lines, not a single `0`** — including **73,798** GeneralHardware and **11,890** MimyMart
> lines sold from products that *are* non-trackable.
>
> **Cause — verified in one codebase, inferred for the other two.** In **MimyShop**'s source,
> `InvoiceProductRepository`'s `INSERT` omits the `IsTrackable` column, so SQLite applies the
> schema's `DEFAULT 1` on every row. GeneralHardware's rows were written by **IndyPOS v3.7.0** and
> MimyMart's by a third codebase, and **neither is in this repo** (`grep 'INSERT INTO
> InvoiceProduct'` over `src/` returns nothing) — so for those two the same defect is *inferred from
> an identical symptom*, not read off the code. **The conclusion does not depend on the cause:** it
> rests on the measurement above, which covers all three stores directly.
>
> Legacy stock handling is nevertheless correct, which is why this was never noticed: the guard
> filters the **in-memory** `IInvoiceProduct` (flag copied from the product) and never reads the
> persisted column.
>
> **Consequence for this defect:** a migration that restores a per-line trackable flag by reading
> `InvoiceProduct.IsTrackable` will mark **every service line as stock-tracked** — the exact bug
> defect 7 exists to fix, faithfully reproduced from data that looks legitimate. Join
> `InventoryProduct` on `InventoryProductId` and take the flag from there: only **29** products
> across the three stores are non-trackable (**21** GeneralHardware + **7** MimyMart + **1**
> MimyShop, and zero `NULL`s anywhere), and that column *is* maintained.
>
> ⚠️ This also means **`InvoiceProduct.IsTrackable` cannot be used to verify the migration** — it
> reconciles perfectly against a wrong answer, the same trap as defect 1's payment scramble.
> Found while verifying MimyShop's interim service buttons, where the two seeded service products
> are `IsTrackable = 0` yet both invoice lines persisted as `1`.

See `findings-2026-07-31.md` §A for defects 8-9 and the UUIDv7 recommendation (worth adopting
while v4 has never run a store — that window closes at the first migration).

v4's `PayLater` entity is already a field-for-field match for the legacy table
(`PaymentId, Description, PayLaterAmount, PaidAmount, IsCompleted` + calculated `RemainingAmount`),
so defect 2 is a rename, not a redesign.

### Why none of it was caught

- `MigrationVerifier` compared only row **counts** and `SUM(Invoice.Total)` — both reconcile
  perfectly under a scrambled mapping. Now compares count *and* amount **per method** (`af4ea14`).
- **No test anywhere creates a `Payment` table** — see the audit below.
- The real-DB tests skip silently when the `.db` files are absent — including in CI.

### Test-harness audit — ✅ DONE 2026-08-01. **Project deleted 2026-08-03.**

`tests/IndyPOS.Migration.Tests` is gone. The audit that justified it is preserved below, because it
is also the specification for what the replacement must do differently.

The suspicion was that its 15 passing tests validate nothing. Confirmed, and it is worse than that.

**1. The tests cannot reach the shipped migrator.** `tests/IndyPOS.Migration.Tests/MigrationService.cs`
(382 lines) is a **second, parallel migration implementation**. The shipped one is
`src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs`.

The decisive evidence is structural, not textual: **`IndyPOS.Migration.Tests.csproj` has no
`ProjectReference` to `IndyPOS.MigrationTool`** — only to `IndyPOS.Domain` and
`IndyPOS.Infrastructure`. No test in that project *can* touch the shipped migrator, and a grep for
`SqliteMigrationService` inside it returns nothing. So every one of the 9 defects could be fixed or
reintroduced in the real tool without moving a single test here.

The test copy also still carries the **pre-`af4ea14` scrambled payment map** (`2=>"Card"`,
`3=>"Transfer"`, `4=>"PayLater"`, `5=>"WelfareCard"`, `_=>"Other"`), its own
`SpecifyKind(..., Utc)` date bug, and its own `Category?.ToString()` — i.e. it is a frozen copy of
the bugs Epic 2 exists to fix.

⚠️ **Precisely what is and is not real:** because the project *does* reference
`IndyPOS.Infrastructure`, `MigrationTestFixture` calls `EnsureCreatedAsync()` on the **real**
`StoreHubDbContext`. So the **target** schema is genuine; the **source** schema and the **migration
logic** are not.

**2. The schema it builds exists in no store.** Verified against
`sqlite_database/GeneralHardware/Store.db`, whose 13 tables are `Customers`, `Installments`,
`InventoryProduct`, `Invoice`, `InvoiceProduct`, `PayLater`, `Payment`, `PaymentType`,
`ProductBarcodeCounter`, `ProductCategory`, `User`, `UserCredential`, `UserRole`:

| Real store | `MigrationTestFixture` builds |
|---|---|
| `Payment` | `InvoicePayment` — wrong name *and* shape |
| `PayLater` | `AccountsReceivable` + `AccountsReceivablePayment` — exist in no store |
| `PaymentType`, `ProductCategory`, `UserRole` | absent |
| — | `StoreConstant` — exists in no store |

**3. Defect 6 independently confirmed.** Real `InvoiceProduct` has **17** columns; the fixture's has
**9**, missing exactly `Manufacturer`, `Brand`, `Category`, `IsTrackable`, `Note`, `GroupPrice`,
`IsGroupProduct`, `OriginalUnitPrice`.

**Consequence for Epic 2:** do not "fix" this project — delete it. That removes 15 misleading green
tests and loses no coverage.

**Why "no coverage" is safe to claim.** The only real thing the project exercises is
`StoreHubDbContext.EnsureCreatedAsync()` against a live Postgres container — and
`IndyPOS.StoreHub.IntegrationTests` already does exactly that, on the same context, across its 94
tests. The target-schema smoke test is therefore duplicated, not lost.

Real coverage has to be built against `SqliteMigrationService` using a fixture whose schema is
**generated from a real `Store.db`** rather than hand-written — hand-writing it is what produced a
schema no store has. Until that exists, **treat the invoice and product migration paths as
untested.**

> ### ⚠️ Correction 2026-08-03: this audit undersold the problem
>
> "Untested" was too generous. `tests/IndyPOS.MigrationTool.Tests` **already has 11 tests that do
> exercise the shipped `SqliteMigrationService`** end-to-end against a real Postgres container, and
> they pass. Its `SqliteTestDataSeeder.CreateSchemaAsync()` hand-writes `PayLater` with exactly the
> columns *the migrator SELECTs* (`PayLaterId, UserId, CustomerName, PaymentAmount`) and not one of
> the columns the real table has, and declares `InvoiceProduct` with exactly the 7 columns the
> migrator reads against the real 17.
>
> **The fixture was derived from the SELECT statements, not from a store, so the suite confirms the
> migrator agrees with itself.** Defects 2, 3 and 6 are invisible *by construction*; and because the
> data is random (Bogus), no assertion can name an expected value — every one is a count or a
> `BeGreaterThan`, which conceals 4, 5, 7 and 8. Same failure as the deleted project, in a project
> that *has* the right `ProjectReference`. Fixing the reference was never the point.
>
> Also: `RealDatabaseSchemaTests` hardcodes `C:\personal\IndyPOS\...\Store.db` and `return`s when
> absent, so its 8 tests report **Passed**, not Skipped, on every other machine including CI — and
> its `Should().Contain(...)` assertions are one-directional, so a 17-column table satisfies a test
> listing 7. That is how defect 6 survived a test called `RealDatabase_ProductTable_HasExpectedColumns`.
>
> Design for the replacement: `docs/superpowers/specs/2026-08-03-migration-test-coverage-design.md`

---

## Epic 3: Store rollout ⏳

Per store: fresh v4 install (`--silent --store-id <ID> --store-type <T>`), then migrate, then verify.
v4 installs **alongside** v3.7.0 — detection classifies a v3-only machine as `Fresh`
(pinned by `Detect_OnAMachineRunningOnlyV3_ShouldReturnFresh`).

Owed before the first store: PR #52's visual smoke (payment-button caption clipping on
`บัตรสวัสดิการแห่งรัฐ`), and one v3.7.0-coexistence check against a real v3 footprint.

---

## Epic I: Cloud Infrastructure 🔴

**Goal (revised):** central reporting, an agent-facing API, and an off-site copy of all three stores'
history — including migrated legacy data.

**Schema recommendation:** one managed PostgreSQL, **one schema shaped like StoreHub's, every row
carrying `StoreId`**. Reasoning:

- The StoreHub schema is *already* multi-store-shaped (`Product.StoreId`, `Invoice.StoreId`, store-scoped
  `payment_method`), so sync is a row copy rather than a translation — and translation layers are where
  the payment-mapping class of bug breeds.
- ~237k invoices / ~600k lines total. Volume forces nothing exotic.
- Store-type differences need no DDL divergence: PayLater is simply *absent rows*; category sets are
  *rows* in a store-scoped table.
- Cross-store reporting and MCP both want one table to `GROUP BY`, not a union of per-type tables.

"Different tables" is right in one sense — the cloud will likely want **additional** denormalised
rollups / materialised views alongside the synced tables. That is additive and later.

Rejected: per-store-type tables or per-store PostgreSQL schemas. No entity-shape difference, one
owner (no tenancy isolation need), and every cross-store report would become a cross-schema query.

| Task | Description | Priority |
|------|-------------|----------|
| I0 | Dockerfile for CloudApi + compose stack | HIGH |
| I1 | Provision DigitalOcean Droplet (Singapore) | HIGH |
| I2 | Provision DO Managed PostgreSQL | HIGH |
| I3 | Deploy CloudApi | HIGH |
| I4 | Configure SyncWorker against the real CloudApi | HIGH |
| I5 | Multi-store sync testing (3 store types) | MEDIUM |
| I6 | Backfill migrated legacy history to cloud | MEDIUM |
| I7 | Central reporting | LOW |
| I8 | Cloud deployment guide | MEDIUM |

**Specs:** sizing, firewall ports, scaling roadmap and rejected alternatives in
`docs/architecture/IndyPOS_Production_Infrastructure_Guide.md` — read before I1–I2.
Droplet ~$12/mo + managed PG ~$15/mo ≈ **$27/mo**.

---

## Epic MCP: agent-facing API 🔴

**New requirement (2026-07-29).** An MCP server so an AI agent can query store data — sales,
inventory, cross-store comparisons. Depends on Epic I.

Not yet designed. Open questions when we get there: read-only or write-capable; per-store or
cross-store scoping; auth model; whether it fronts CloudApi or the database directly.

---

## Epic S: Security Hardening (5/9)

Landed via the Vault and admin-provisioning epics: DPAPI at-rest secrets, ACL-locked config and
credential files, single-use bootstrap admin with forced rotation, capability-based authorization
(`CapabilityRequirement` + named policies, not role names).

Remaining items in `.planning/indypos-overhaul/security/`.

---

## Backlog

| Item | Description | Priority |
|------|-------------|----------|
| ~~`Migration.Tests` — audit, then delete~~ | ✅ **Done 2026-08-03.** Audited 2026-08-01, project deleted. Replacement coverage against `SqliteMigrationService` is now Epic 2's first task | — |
| Headless installer crash | An unknown argument silently launches the wizard; headless that is a bare CLR crash with no log | MEDIUM |
| Migration `--sync-to-cloud` | Outbox events for migrated invoices (folds into I6) | LOW |
| **Auto-Update System (Epic U)** | Remote updates for StoreHub + WinForms. Superseded in part by the installer upgrade path — revisit scope | Future |
| MAUI Migration | Replace WinForms | Future |
| Legacy Report Cleanup | Remove int-based report methods | With MAUI |

### Auto-Update (Epic U) — status note

PR #54 delivers in-place upgrade via `IndyPOS-Setup.exe --silent`, and the POS app already
self-updates through Velopack. What Epic U would add is the **trigger**: stores checking a server
rather than someone running the installer. Option A (CloudApi as update server) still stands and now
depends only on Epic I. The U1–U8 task list in the previous revision remains broadly valid but
should be re-scoped against what the installer now does.

---

## Technical Debt

### StoreHubReportService legacy methods
Stubs returning empty collections; address during MAUI migration:
`GetInvoicesByPeriodAsync`, `GetInvoicesByDateRangeAsync`, `GetPayLaterPaymentsByPeriodAsync`,
`GetPayLaterPaymentsAsync`, `GetInvoiceProductsByDateAsync`, `GetInvoiceProductsByDateRangeAsync`,
`GetInvoiceProductsByInvoiceIdAsync(int)`, `GetPaymentsByInvoiceIdAsync(int)`, `GetInvoiceInfoAsync(int)`.

---

## Test state (2026-08-04, all rows re-measured)

| Suite | Count |
|-------|-------|
| IndyPOS.Bootstrapper.Tests | 223 pass / 8 skip (admin-required) |
| IndyPOS.Application.Tests | 294 |
| IndyPOS.StoreHub.IntegrationTests | 94 (real PostgreSQL; 87 need Docker) |
| IndyPOS.MigrationTool.Tests | 85 pass / 1 skip (86) — 36 need Docker, 24 need the gitignored real store `.db` files, 25 pure units, 1 manual extractor. **66 discovered** without those `.db` files, still all green |
| ~~IndyPOS.Migration.Tests~~ | **deleted 2026-08-03** — validated a schema no store has |
| IndyPOS.Domain.Tests | 36 |
| IndyPOS.Windows.Forms.Tests | 19 |
| IndyPOS.Vault.Tests | 17 |
| Solution total | **546** (545 pass / 1 skip) with Docker running and real store `.db` files present; **526** without them |
| Release build | 0 errors |

---

## Reference Documentation

| Doc | Location |
|-----|----------|
| Current status | `.claude/STATUS.md` |
| Specs | `docs/superpowers/specs/` |
| Plans | `docs/superpowers/plans/` |
| SDD ledger | `.superpowers/sdd/progress.md` |
| Completed epics (through H) | `.planning/indypos-overhaul/completed/` |
| Cloud target infrastructure | `docs/architecture/IndyPOS_Production_Infrastructure_Guide.md` |
| Upgrade procedure | `docs/operations/upgrade-procedure.md` |
| Security spec | `.planning/indypos-overhaul/security/` |
| Diagrams | `.planning/indypos-overhaul/diagrams/` |

---

## Solution Structure

```
src/
  IndyPOS.Domain            # Entities, value objects, domain logic
  IndyPOS.Application       # Use cases, interfaces, DTOs
  IndyPOS.Infrastructure    # Repositories, EF, external services
  IndyPOS.Vault             # DPAPI secret protection
  IndyPOS.Windows.Forms     # Desktop UI (legacy)
  IndyPOS.StoreHub          # Local API service
  IndyPOS.CloudApi          # Central cloud API
  IndyPOS.AppHost           # Aspire orchestrator
  IndyPOS.ServiceDefaults   # Health checks, OpenTelemetry
  IndyPOS.MigrationTool     # SQLite -> PostgreSQL migration

installer/
  IndyPOS.Bootstrapper      # Fresh install + in-place upgrade

tests/                      # See test-state table above
```

---

## Quick Commands

```bash
dotnet test                 # all suites (Docker needed for integration/migration)
dotnet build IndyPOS.sln -c Release
dotnet run --project src/IndyPOS.AppHost --launch-profile https   # Aspire, needs Docker
```
