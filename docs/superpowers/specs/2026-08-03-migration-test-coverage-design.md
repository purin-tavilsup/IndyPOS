# Design: real test coverage for the SQLite → PostgreSQL migration

**Date:** 2026-08-03
**Epic:** 2 — SQLite → PostgreSQL migration hardening
**Status:** Approved, ready for an implementation plan
**Depends on:** PR #60 (deletes `tests/IndyPOS.Migration.Tests`)

---

## 1. Why this exists

The migration tool is the only thing between three live v3.7.0 stores and v4. It was written against
an assumed schema and has never been run end-to-end against a real store database.

PR #60 deleted `tests/IndyPOS.Migration.Tests` because its 15 tests validated a parallel
implementation the product never references. That was the obvious half of the problem. This spec
addresses the half the audit missed.

### The audit undersold the problem

`tests/IndyPOS.MigrationTool.Tests` already contains **11 tests that do exercise the shipped
`SqliteMigrationService`** end-to-end, through a real PostgreSQL container. They pass. The reason
they pass is worse than having no tests at all.

Its `SqliteTestDataSeeder.CreateSchemaAsync()` hand-writes a legacy schema. Compare `PayLater`:

| | Columns |
|---|---|
| **Real store** | `PaymentId`, `Description`, `InvoiceId`, `IsCompleted`, `DateCreated`, `DateUpdated`, `PayLaterAmount`, `PaidAmount` |
| **What the migrator SELECTs** | `PayLaterId`, `InvoiceId`, `UserId`, `CustomerName`, `PaymentAmount`, `IsCompleted`, `DateCreated`, `DateUpdated` |
| **What the seeder CREATEs** | `PayLaterId`, `InvoiceId`, `UserId`, `CustomerName`, `PaymentAmount`, `IsCompleted`, `DateCreated`, `DateUpdated` |

The fixture matches the **migrator**, column for column, and neither matches the **store**. The same
holds for `InvoiceProduct`: the seeder declares exactly the 7 columns the migrator reads, where the
real table has 17.

**The fixture was derived from the SELECT statements rather than from a store, so the suite confirms
that the migrator agrees with itself.** Defects 2, 3 and 6 are invisible *by construction*. And
because the seeder generates random values with Bogus, no assertion can state an expected value —
every one is a count or a `BeGreaterThan`, which is what conceals defects 4, 5, 7 and 8.

This is the same failure as the deleted project, in a project that has the right project reference.
Fixing the reference was never the point; deriving the fixture from reality is.

---

## 2. Findings established while designing this

All measured against the three real store databases at
`.planning/indypos-overhaul/sqlite_database/{GeneralHardware,MimyMart,MimyShop}/Store.db`
(gitignored). Volumes: 139,680 / 97,293 / 15 invoices.

### 2.1 The migration cannot complete against any real store

The exact `PayLater` SELECT from `SqliteMigrationService` line 338, run against each real database:

```
GeneralHardware   -> OperationalError: no such column: PayLaterId
MimyMart          -> OperationalError: no such table: PayLater
MimyShop          -> OperationalError: no such table: PayLater
```

That `QueryAsync` sits **outside** the per-row `try` (line 338 versus the `try` at line 346), and
`MigrateAllAsync` does not wrap its four phase calls (lines 48–51). `SaveChangesAsync` is line 55 —
**after** `MigratePayLaterAsync` at line 51.

So the throw escapes before anything is saved. This is not "PayLater is skipped": the tool performs
the entire migration in memory, then discards all of it. **Against every real store, the migration
tool persists nothing.** It only completes against the fabricated test schema.

Consequence for this design: defects 4, 5, 6, 7, 8 and 11 are all observed by inspecting rows in
PostgreSQL after a run, and against an honest schema no run reaches the save. Defects 2 and 3 must
therefore be fixed *as part of building the harness*, or the harness can observe nothing.

### 2.2 Defect 10 (new): PayLater payments are double-counted

`PayLater.PaymentId` is a foreign key to `Payment.PaymentId`. Measured on GeneralHardware:

| Probe | Result |
|---|---|
| PayLater rows | 5,181 |
| `PayLater.PaymentId` matching a `Payment` row | **5,181 of 5,181** |
| Orphans (no matching `Payment`) | **0** |
| `PaymentTypeId` of every linked payment | **all `2` = ลงบัญชี (PayLater)** |
| `InvoiceId` disagreements | 0 |
| `Amount` vs `PayLaterAmount` disagreements | 0 |

**The domain reason (confirmed by Pond, 2026-08-03): PayLater is a special kind of *payment*, not a
separate transaction.** `PayLater` is a 1:1 extension table on `Payment` — table-per-subtype — which
the primary keys state outright:

```
Payment  :  PRIMARY KEY("PaymentId" AUTOINCREMENT)     -- generates the id
PayLater :  PRIMARY KEY("PaymentId")                   -- no AUTOINCREMENT, receives it
```

So `PayLater` needs no id of its own, no `UserId` (the payment's invoice has one) and no
`PaymentAmount` (that is `Payment.Amount`). All it adds is credit-tracking state: who owes it
(`Description`), how much is settled (`PaidAmount`), whether it is closed (`IsCompleted`). The four
columns the migrator invents are exactly the four that a table-per-subtype row does not need.

**A PayLater row is therefore an annotation on a money event, not a money event.**

`MigrateInvoicesAsync` selects an invoice's payments with **no type filter**, and
`LegacyPaymentTypeMap` maps `[2] = PaymentMethodCodes.PayLater`. So all 5,181 type-2 rows are already
migrated as `Method = PayLater`. `MigratePayLaterAsync` then **creates a second `Payment`** for the
same money (line 369).

#### Root cause (Pond, 2026-08-03): the migration re-enacts the sale instead of copying it

At the till, a PayLater sale writes **the `Payment` first, then the `PayLater`** as the tracking record
for that debt, until the customer has repaid it all and it completes. `MigratePayLaterAsync` reproduces
exactly that sequence — create a payment, then create its tracking record.

That is correct for a till making a new sale. It is wrong for a migration, because **both rows already
exist in SQLite**. A migration copies history; it does not replay the transaction that produced it.
Re-enacting a two-step write against data that already contains both steps yields one real payment and
one invented one.

This is worth recording because it explains why the code reads as entirely sensible: it faithfully
models the domain flow. The defect is not a misunderstanding of PayLater — it is applying a *write*
model where a *copy* model was needed.

**฿836,013 counted twice.** This is currently unreachable because defect 2 crashes first — fixing
defect 2 alone would convert a loud crash into silent revenue inflation, which is precisely the
failure mode this epic exists to eliminate. It is therefore fixed here, not deferred.

The relationship is **not** 1:1 in the other direction: `Payment` has **5,182** type-2 rows against
5,181 PayLater rows, and ฿836,083 against ฿836,013. The single orphan is identified: **`PaymentId 90`,
฿70, with an empty `Note` and no `PayLater` row** — the one type-2 payment in the store that records
no customer at all, i.e. an abandoned credit sale. The fix must not assume a PayLater exists for every
type-2 payment, nor a payment for every PayLater.

### 2.3 `PaidAmount` is the only record of repayment progress, and the code derives it away

**The domain (Pond, 2026-08-03): a PayLater sale records the customer's name and the customer must
come back and pay, in instalments, until the amount is complete.** Partial repayment is therefore the
feature's *core workflow*, not an edge case.

Line 387 computes `PaidAmount = payLater.IsCompleted == 1 ? amount : 0m` — reconstructing from a
boolean a column that is actually maintained. Measured state:

| State | Rows |
|---|---|
| `PaidAmount = 0` — nothing repaid yet | 142 |
| `0 < PaidAmount < PayLaterAmount` — mid-repayment | **1** |
| `PaidAmount = PayLaterAmount` — settled | 5,037 |
| `PaidAmount > PayLaterAmount` | **1** |

Outstanding debt across incomplete rows: **฿20,010**.

`DateUpdated` corroborates that this is a repayment ledger: 142 rows are `NULL` (never repaid) and
**5,039 have moved off `DateCreated`** — exactly the 5,037 settled plus the two in-flight rows. So
`DateUpdated` is the last-repayment timestamp.

⚠️ **The snapshot counts understate the loss.** Only two rows are mid-flight *at the instant the
database was copied*, but every one of the 5,181 passed through partial states, and `Installments` is
empty everywhere — so `PaidAmount` is the **only** surviving record of repayment progress. There is
no payment history to reconstruct it from. Deriving it discards the state of every debt still being
repaid at cutover.

Fixed here as part of defect 2: same query, same method.

#### 2.3.1 A live data-entry error the migration must carry faithfully

`PaymentId 139978`: **฿82 owed, ฿8,200 recorded as paid**, `IsCompleted = 0`. Exactly 100× — a
dropped decimal, not a real overpayment. `IsCompleted` stayed `0` because the app's completion check
compares for equality, which `8200 = 82` never satisfies.

The migration must **not** normalise, clamp or "correct" this. Inventing data is worse than carrying
a visible error, and a store can only fix what it can see. It does mean any later `MigrationVerifier`
work should *report* rows where `PaidAmount > PayLaterAmount` rather than reconcile them away — noted
here because it is the kind of row a sum-based check hides.

### 2.3.2 The customer name is stored twice, identically

Both `PayLater.Description` and the linked `Payment.Note` hold the customer name, and they agree on
**all 5,181 rows** — zero disagreements, zero rows where only one is set, zero rows where neither is.
`Description` is the intended home (Pond); `Payment.Note` mirrors it.

`Note` is effectively PayLater-specific by convention: **5,181 of 5,182** type-2 payments carry one,
against 649 of 121,669 cash payments and 48 of 12,458 transfers.

Consequence: once defect 10 is fixed, both values migrate through their own columns with **no special
handling** — legacy `PayLater.Description` → v4 `PayLater.Description`, legacy `Payment.Note` → v4
`Payment.Note`. The current code's `Note = payLater.CustomerName` on an invented payment (line 375) is
copying data the real payment already carries. Longest real `Description` is 41 characters, so the
500-character truncate never fires.

### 2.4 Defect 11 (new): `ParseDate` is culture-dependent, and the tool runs on a Thai till

`ParseDate` (line 511) calls bare `DateTime.TryParse` with no `CultureInfo`. Verified:

```
InvariantCulture : "2024-03-15 14:30:00" -> 2024-03-15   (GregorianCalendar)
th-TH            : "2024-03-15 14:30:00" -> 1481-03-15   (ThaiBuddhistCalendar)
```

Under `th-TH` the Buddhist calendar reads `2024` as a Buddhist-era year, giving **1481 AD — 543
years off**. The migration tool runs on the store's own Thai-locale machine.

Defect 4 puts every timestamp 7 hours out. This puts every invoice in the 15th century, so a
migrated store would show **zero sales in any date-range report**. Both live in the same six-line
method. This defect is pinned here and fixed in the same later spec as defect 4.

### 2.5 Real schema shapes

GeneralHardware has 13 tables; MimyMart and MimyShop have 10. The divergence is the PayLater
feature (`PayLater`, `Customers`, `Installments`).

**`Customers` and `Installments` are obsolete and were never used, in every store** — confirmed by
Pond (2026-08-03), and 0 rows measured in GeneralHardware, the only store that has them. They stay
permanently out of scope, and legacy payment type 6 (`ผ่อนชำระ`, instalments) is dead with them and
must remain unmapped.

They are nonetheless kept in the `GeneralHardware.sql` artefact, because that file is a **faithful
dump** and hand-removing tables from generated output is the very habit this design exists to break.
Instead a test pins that the migration *ignores* them, so nobody later "completes" the migration by
importing a feature no store uses.

MimyShop's 10 shared tables are **byte-identical DDL** to GeneralHardware's, so the two artefacts
differ only by those three tables.

Other differences from the hand-written seeder, all of which matter:

- Real `PayLater` has **no `UserId` column at all**, so the migrator's PayLater user mapping
  (line 357) is mapping a column that does not exist.
- Real `UserCredential`'s primary key is `UserId`, one credential per user; the seeder invented a
  `UserCredentialId` autoincrement and made `Username` `NOT NULL UNIQUE`.
- Money columns are `NUMERIC` in the real schema and were `REAL` in the seeder, while the migrator
  maps them to `double`.
- The real databases have **no indexes at all**.
- `PaymentType` (8 rows), `ProductCategory` (16/17), `UserRole` (3) exist in every store and are
  absent from the seeder entirely.

### 2.6 `RealDatabaseSchemaTests` is vacuous everywhere but one machine

It hardcodes `C:\personal\IndyPOS\.planning\...\Store.db` and `return;`s when the file is absent, so
its 8 tests report **Passed** on every other machine, CI included. Its assertions are also
one-directional `Should().Contain(...)`, so a table with 17 columns satisfies a test that lists 7 —
which is how defect 6 survived a test named `RealDatabase_ProductTable_HasExpectedColumns`. It has
no PayLater column test at all, which is defect 2's blind spot exactly.

---

## 3. Scope

**In scope**

1. An honest fixture: legacy SQLite schema **generated** from a real `Store.db`, committed.
2. Deterministic, value-level coverage of every migration phase against that schema.
3. Pinning tests that document defects 4, 5, 6, 7, 8 and 11 as executable, named facts.
4. Fixes for defects **2, 3 and 10** — the minimum that lets the harness observe anything, plus the
   double-count that fixing defect 2 would otherwise introduce.
5. Rewriting the 11 fabricated-schema tests and de-vacuuming the 8 real-database tests.

**Out of scope** — each gets its own spec:

- Defects 4, 5, 6, 7, 8, 11 (pinned here, fixed later)
- Defect 9 — deleting the dead `LegacyIdHelper`
- `MigrationVerifier` changes
- The `--sync-to-cloud` path
- `tests/IndyPOS.Mock` causing a root `dotnet test` to always exit 1 (pre-existing, unrelated)
- Migrating `Customers` / `Installments` (0 rows everywhere, permanently out of scope)

---

## 4. Architecture

No new test project. `tests/IndyPOS.MigrationTool.Tests` already references `IndyPOS.MigrationTool`
and already has `PostgresFixture` with Testcontainers. The deleted project's cardinal sin was the
missing reference; this project has it.

### 4.1 Components

| Component | Responsibility | Depends on |
|---|---|---|
| `Tools/LegacySchemaExtractor.ExtractLegacySchema` | Reads `sqlite_master` from a real `Store.db`, writes DDL to a `.sql` file. Re-runnable. | A real `Store.db` (developer machine only) |
| `LegacySchema/GeneralHardware.sql` | Generated DDL, 13 tables, **has** `PayLater` | — (committed artefact) |
| `LegacySchema/MimyShop.sql` | Generated DDL, 10 tables, **no** `PayLater` | — (committed artefact) |
| `LegacyStoreDatabase` | Creates a temp SQLite file and applies one committed DDL. Replaces `SqliteTestDataSeeder.CreateSchemaAsync`. | A `.sql` artefact |
| `LegacyStoreDataBuilder` | Inserts deterministic rows using real column names | `LegacyStoreDatabase` |
| Phase test classes | One migration phase each | Both of the above, `PostgresFixture` |

The two `.sql` files each carry a header recording source store, extraction date, table count, and
that they are generated and must not be hand-edited. Committing them means the tests run in CI with
no real `.db` present, and a schema change surfaces as a diff on a tracked file.

The extraction tool is committed so the provenance of the schema is reproducible rather than
folklore. That is the whole point: **hand-writing the schema is what produced one no store has.**

> **Amended 2026-08-04 — the extractor shipped as a skipped xUnit test, not a `.ps1`.**
> It is `[Fact(Skip = "Manual tool…")]` on `Tools/LegacySchemaExtractor`, run explicitly with
> `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"`.
>
> Why: as a test it reuses the suite's own `System.Data.SQLite` dependency and its `LegacyStoreShape`
> enum, so the artefact path and the shape list cannot drift from what the tests consume. A `.ps1`
> would need its own SQLite access and its own copy of that list — a second source of truth for
> exactly the thing this spec exists to keep singular. It also stays inside `dotnet test`, so no
> contributor needs PowerShell to regenerate an artefact.
>
> Cost, recorded honestly: a skipped test is easy to overlook, and the skip reason is the only place
> that says it is a tool rather than dead coverage.

### 4.2 The fidelity rule that matters most

`LegacyStoreDataBuilder` **must omit `InvoiceProduct.IsTrackable` on insert**, so SQLite applies the
schema's `DEFAULT 1`.

This reproduces the dead-data condition from the defect 7 addendum: 602,114 real invoice lines,
every one `IsTrackable = 1`, not a single `0`, because the legacy write path omits the column. A
builder that helpfully set the column would recreate exactly the blindness this work removes, and
would let a future "fix" read the flag from the line — marking every service line stock-tracked,
which is the bug defect 7 exists to prevent.

### 4.3 Test class layout

One class per phase, so each file stays small and focused:

```
tests/IndyPOS.MigrationTool.Tests/
  LegacySchema/
    GeneralHardware.sql            generated, do not hand-edit
    MimyShop.sql                   generated, do not hand-edit
  Fixtures/
    PostgresFixture.cs             unchanged
    LegacyStoreDatabase.cs         new
    LegacyStoreDataBuilder.cs      new
  MigrationCompletesTests.cs       whole-run, both store shapes
  UserMigrationTests.cs
  ProductMigrationTests.cs
  InvoiceLineMigrationTests.cs
  PaymentMigrationTests.cs
  PayLaterMigrationTests.cs
  RealStoreSchemaTests.cs          rewritten; today the file is RealDatabaseMigrationTests.cs
                                   and the class inside it is RealDatabaseSchemaTests
  LegacyPaymentTypeMapTests.cs     unchanged
  MigrationVerifierTests.cs        unchanged
```

---

## 5. How a caught defect is expressed

Fixes for defects 4, 5, 6, 7, 8 and 11 land in later specs, so a suite asserting correct behaviour
would be red for the whole of Epic 2 — during which a root `dotnet test` could detect no regression
anywhere else in the solution.

Instead each defect gets a **pinning test**: it asserts what the migrator does *today*, is named for
its defect, and carries the correct expectation as a comment.

```csharp
[Fact]
// Defect 5: writes the raw legacy category id instead of a catalogue code.
// Correct: "GeneralMaterials" (ProductCategoryCodes), resolved from legacy id 50.
public async Task MigrateProducts_Category_CurrentlyWritesRawLegacyId_Defect5()
{
    ...
    product.Category.Should().Be("50");
}
```

Green therefore keeps meaning "nothing changed unintentionally", each later fix inverts exactly one
test, and the test names make the defects loud enough that nobody mistakes green for correct.

This is deliberately *not* the trap PR #60 removed. Those tests were green while claiming to
validate a migration. These are green while stating, in their own names, that the migration is
wrong.

---

## 6. Test inventory

### 6.1 Pinning tests — assert today's behaviour

| Test | Seeded | Asserts today | Correct answer |
|---|---|---|---|
| `ParseDate_UnderThaiCulture_CurrentlyYields1481_Defect11` | `DateCreated = '2024-03-15 14:30:00'`, `CurrentCulture = th-TH` | `CreatedUtc.Year == 1481` | `2024` |
| `MigrateInvoices_DateCreated_CurrentlyRelabelsThaiLocalAsUtc_Defect4` | same, invariant culture | `CreatedUtc == 2024-03-15T14:30:00Z` | `2024-03-15T07:30:00Z` |
| `MigrateProducts_Category_CurrentlyWritesRawLegacyId_Defect5` | product `Category = 50` | `Category == "50"` | `"GeneralMaterials"` |
| `MigrateInvoiceLines_CurrentlyCannotDistinguishADiscountedLine_Defect6` | two lines: `OriginalUnitPrice 100 / UnitPrice 80` and `100 / 100` | both migrate to rows identical but for price; the ฿20 discount is unrecoverable | discount preserved |
| `MigrateInvoiceLines_CurrentlyDiscardsNoteAndGroupPricing_Defect6` | line with `Note`, `IsGroupProduct = 1`, `GroupPrice`, `Priority` | the v4 `InvoiceLine` carries none of them | all preserved |
| `MigrateProducts_CurrentlyDropsIsTrackable_Defect7` | non-trackable product, sold on an invoice | a negative `Migration:Sale` `InventoryMovement` exists for it | no stock movement |
| `LegacyInvoiceProductIsTrackable_IsDeadData_Defect7Addendum` | line inserted **without** `IsTrackable`; its product is `IsTrackable = 0` | legacy line reads `1` while its product reads `0` | fixture-fidelity guard |
| `MigrateProducts_CurrentlyPreservesNoLegacyId_Defect8` | product `InventoryProductId = 4242` | the migrated `Product` exposes no property carrying `4242`, so the row cannot be reconciled against its SQLite source. Asserted as: `StoreUser` has `LegacyUserId` but `Product`, `Invoice`, `InvoiceLine` and `Payment` have no equivalent | legacy id preserved on all five |

The defect 7 addendum test is a guard on the *fixture*, not the migrator. It fails if
`LegacyStoreDataBuilder` ever starts setting `IsTrackable` on the line, which would silently restore
the blindness described in §4.2.

### 6.2 New green tests — none of these can exist today

| Test | Proves |
|---|---|
| `MigrateAllAsync_AgainstGeneralHardwareShape_CompletesAndPersists` | The headline. The tool reaches `SaveChangesAsync` against a real store shape for the first time. |
| `MigrateAllAsync_AgainstAStoreWithNoPayLaterTable_Completes` | Defect 3 fixed, using the MimyShop shape |
| `MigratePayLater_ReadsRealColumns_MapsDescriptionAndAmounts` | Defect 2 fixed |
| `MigratePayLater_LinksTheExistingPayment_WithoutCreatingASecond` | Defect 10 fixed — one `Method = PayLater` payment, not two |
| `MigratePayLater_WithAPartiallyPaidRow_PreservesPaidAmount` | ฿700 owed / ฿299 paid survives — the mid-repayment customer (§2.3) |
| `MigratePayLater_WithPaidAmountExceedingTheDebt_CarriesItUnchanged` | ฿82 owed / ฿8,200 paid is **not** clamped or corrected (§2.3.1) |
| `MigratePayLater_WithNeverRepaidRow_PreservesZeroPaidAndNullDateUpdated` | The 142-row case: `PaidAmount = 0`, `DateUpdated` null |
| `MigratePayLater_CustomerName_SurvivesInBothDescriptionAndNote` | §2.3.2 — both columns carry through with no special handling |
| `MigratePayLater_WhenThePaymentIsMissing_RecordsAnErrorAndSkips` | The `PaymentId 90` orphan — refuses rather than inventing a payment |
| `MigratePayments_EachLegacyType_MapsToItsCatalogueCodeAndAmount` | Defect 1 regression guard, per method rather than in aggregate |

⚠️ One more test belongs here but its outcome is genuinely unknown until it is written:
`MigrateProducts_WithANumericPrice_PreservesTheValue`, covering `NUMERIC` → `double` → `decimal` on a
value like `19.99` (§2.5). If it passes it is a green regression guard. **If it fails it is a new
defect** and the plan must record it rather than adjust the expectation to match. Stated explicitly
so that an implementer does not quietly weaken the assertion to make it pass.

### 6.3 Ported from the existing 11

Kept because they assert real behaviour, rewritten onto the honest schema: idempotent re-run skips
users and products, a user without credentials is skipped, dry run writes nothing. The remaining
count-only tests are superseded by §6.1 and §6.2 and are dropped.

### 6.4 Rewritten: `RealStoreSchemaTests`

- An absent `.db` reports **Skipped**, never Passed.
- The path resolves from the repository root, not `C:\personal\`.
- Assertions become **exact column-set equality**, not `Contain`, so a column added to a real store
  fails the test instead of passing it.
- Gains the `PayLater` column-set test it never had.

---

## 7. The three fixes

### 7.1 Defect 2 — `MigratePayLaterAsync` reads columns that do not exist

Replace the SELECT with the real columns:

```sql
SELECT PaymentId, Description, InvoiceId, IsCompleted, DateCreated, DateUpdated,
       PayLaterAmount, PaidAmount
FROM PayLater
```

- Delete the `payLater.UserId` mapping (lines 357–360) — no such column exists.
- `Description` → `PayLater.Description` (it holds the customer name; the 500 truncate stays).
- `PayLaterAmount` → `PayLaterAmount`; `PaidAmount` → `PaidAmount`, **read, not derived**.

v4's `PayLater` entity is already a field-for-field match, so this is a rename, not a redesign.

Because `PayLater` extends `Payment` rather than duplicating it (§2.2), the authoritative money is
`Payment.Amount` — which agrees with `PayLaterAmount` on all 5,181 real rows — while
`PayLaterAmount` and `PaidAmount` are credit state. The migration must not treat the extension row as
a source of revenue.

### 7.2 Defect 3 — the query runs unconditionally

Guard on table existence before querying:

```sql
SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'PayLater'
```

Absent means log and return: zero migrated, **no error**. A Minimart or MimyShop store has no
PayLater feature, so its absence is normal, not a failure.

### 7.3 Defect 10 — the double-counted payment

- Add `MigrationResult.PaymentIdMap` (`Dictionary<int, Guid>`, legacy `PaymentId` → new `Guid`).
- Populate it in `MigrateInvoicesAsync` as each `Payment` is created.
- `MigratePayLaterAsync` **looks up** `payLater.PaymentId` in that map instead of creating a payment.
- On a miss, record an error and skip **the extension row only** — never the payment, and never
  invent one. The ฿70 orphan is the reverse case: a type-2 `Payment` with no `PayLater` row, i.e. a
  credit sale whose tracking record was never written. The money happened, so that payment still
  migrates; it simply has no credit record. Same philosophy as the defect 1 fix, which refuses an
  unmappable payment type rather than guessing `"Other"`.

The phase ordering already supports this and needs no change: the legacy till writes the `Payment`
before the `PayLater`, and `MigrateAllAsync` correspondingly runs `MigrateInvoicesAsync` (line 50,
which migrates payments) before `MigratePayLaterAsync` (line 51). So `PaymentIdMap` is always populated
before it is read.

⚠️ The payment loop currently sits inside `if (!_options.DryRun)` (line 256). The map must be
populated outside that guard, or dry-run leaves it empty and every PayLater row then fails its
lookup. Noted because it is easy to miss and would make the dry-run test misleading.

---

## 8. Ordering

TDD, and not incidentally:

1. Extraction script and the two committed `.sql` artefacts.
2. `LegacyStoreDatabase` + `LegacyStoreDataBuilder`.
3. The pinning and green tests. **The suite is now red** on defects 2, 3 and 10 — including most of
   the ported tests, because the honest schema makes `MigrateAllAsync` throw.
4. Fix defects 2, 3 and 10. The suite goes green.
5. Rewrite `RealStoreSchemaTests`.

Step 3 must land before step 4. The fixes change shipped behaviour that has no prior real coverage,
and a failing test first is the only thing that demonstrates the fix does what it claims. Asserting
it afterwards is how the old suite became worthless.

---

## 9. Risks

| Risk | Mitigation |
|---|---|
| Changing shipped migration behaviour with no prior real coverage | The harness lands first and the fixes are driven by failing tests (§8). Nothing else depends on `MigratePayLaterAsync`. |
| Date tests are machine-dependent | Every date test pins `CurrentCulture` explicitly. Without it, defect 11's test only passes on a Thai-locale box and defect 4's only on a non-Thai one. |
| The committed `.sql` drifts from the real stores | `RealStoreSchemaTests` asserts exact column-set equality when a real `.db` is present, so drift fails on a developer machine. It cannot be caught in CI, which is accepted: the `.db` files are gitignored and 63.8 MB. |
| `double` for money loses precision | §6.2's round-trip test pins current behaviour. Converting the migrator to `decimal` is a separate concern and is not in this spec. |
| Two `.sql` artefacts imply the harness covers all three stores | It covers the two *shapes* (with and without `PayLater`). MimyMart shares MimyShop's shape. Stated here so nobody reads two files as two of three stores. |

---

## 10. Verification

- Solution build 0 errors.
- `dotnet test tests/IndyPOS.MigrationTool.Tests` green, with the new test count recorded.
- Full solution suite green, with the per-suite counts recorded in `ONBOARDING.md` updated.
- Every pinning test demonstrated **non-vacuous**: temporarily invert its expectation and confirm it
  fails. A pinning test that cannot fail documents nothing, which is the trap this spec exists to
  close.
- `MigrateAllAsync_AgainstGeneralHardwareShape_CompletesAndPersists` confirmed to fail before the
  §7 fixes and pass after.
