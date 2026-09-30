# Design — Invoice History on v4

> **Status:** Draft for review · **Date:** 2026-09-27 · **Layer scope:** Domain → Application →
> Infrastructure → StoreHub API → new `IndyPOS.Presentation` (ViewModels) → Windows.Forms, plus
> MigrationTool and CloudApi (mirror columns/table + event handler)

## 1. Context & problem

On the v4 till, nobody can see sales history. Every invoice method of `StoreHubReportService`
(`src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubReportService.cs:44-102`) is a stub:

- **The lists return empty.** That covers the invoice list by period and by date range, invoice
  products and PayLater payments.
- **The per-invoice methods are stubs too.** Invoice detail, payments and reprint data still take
  the legacy `int` id and return empty lists or a zeroed `StubInvoiceInfo`.

StoreHub itself already serves Guid-based invoice list and detail queries. Only the till's client
and its screens were never moved to v4.

A second defect sits on the same path. **Every v4 receipt prints `Invoice No.: 0000000000`.**
`StoreHubSaleService.CreateInvoiceInfo` sets the legacy `Id = 0`
(`StoreHubSaleService.cs:348`), and `ReceiptPrinterService` prints only that `Id`
(`ReceiptPrinterService.cs:159`). A paper receipt therefore cannot be matched to a bill.

Both block **Epic 3 Phase B**:
- managers need sales history at go-live;
- the invoice-void feature (`docs/superpowers/specs/2026-09-26-invoice-void-design.md`) plugs its
  button into the invoice detail this spec builds.

## 2. Goals / non-goals

**Goals**
- **Bill numbers.** Every bill has a human bill number, printed on its receipt. v3 bills keep
  their v3 number, and v4 numbering carries on after it.
- **Managers** can list bills by period or date range, find one by its number from any day, open
  its detail and reprint it.
- **Cashiers** can find and reprint **today's** bills only. They see today's figures (they already
  need them to count the drawer) but never another day's; see §5.
- **Reprints are marked and recorded.** Every reprint prints **สำเนา / COPY** and leaves an audit
  record that syncs to the cloud.
- **Logic lives in ViewModels** in a new `IndyPOS.Presentation` project, unit-tested and reusable by
  the Avalonia port. The WinForms screens stay thin.

**Non-goals (this change)**
- **A polished WinForms UX.** The real redesign happens in the Avalonia port. WinForms here is lean
  and functional.
- **The other stubbed reports.** These are listed as Phase B gaps in §9, not built here.
- **The bill number as a scannable barcode** on the receipt (follow-up).
- **Renaming existing working routes** such as `POST /sales/complete`. That belongs to the separate
  API route tidy-up PR (§6).

## 3. Decisions (agreed 2026-09-27)

| Question | Decision | Why |
|---|---|---|
| What may a cashier see? *(after PR review)* | **Today's figures yes, other days no.** Today's bill amounts are listed, and every `/cash` read for a past `businessDate` now needs `reports.view` | Cashiers count the drawer, so today's expected cash is theirs to see. Past days were exposed by `?businessDate=` on `/cash/summary` and its four sibling reads (merged in #97); this closes that gap |
| Scope | Invoice list + detail + reprint, and the PayLater invoice popup; the other stubbed reports listed as gaps | What the void and go-live need, without a full reports migration |
| Bill number | **Per-store running `bigint`**, assigned by a Postgres sequence **default**, continuing after the store's last v3 number | Staff know running numbers from v3, old paper receipts still match, and no overflow (10 digits ≈ 27,000 years at 1,000 bills/day) |
| Finding a bill | Period filters **plus a bill-number search** | A manager holding a receipt gets to the bill in one step |
| Reprint marking | **สำเนา / COPY** banner + "reprinted at/by" line | A copy cannot pass as a second original |
| Cashier access | New **`sales.reprint`**: today's bills only, no totals | Covers "can I have my receipt again" without exposing sales figures |
| Reprint audit | **`invoice_reprint` table + `InvoiceReprinted` sync event** | An honest, cloud-visible trail; sync and audit are the core |
| Till architecture | **MVVM now**: `IndyPOS.Presentation` with CommunityToolkit.Mvvm | Logic becomes testable, the void plugs in, and the Avalonia port reuses it |
| API shape | **One REST resource root `/sales`** for bills; `/reports/*` for aggregates only | One root per resource; authorization, not a second URL, decides what each caller sees |

## 4. Bill number — data & migration

**Schema** (one additive migration, forward-only-safe):

```
invoice.invoice_number   bigint NOT NULL  DEFAULT nextval('invoice_number_seq')   -- NOT NULL after the backfill (D2)
UNIQUE INDEX (store_id, invoice_number)
SEQUENCE invoice_number_seq  AS bigint
```

- **The database assigns every number.** The column default fires for any `INSERT`, including one
  from binaries restored by an installer rollback, which never mention the column. Only the
  database sequence picks a number, so two tills selling at once can never clash. The sale handler
  may reserve its number with `nextval` before the save, so the `InvoiceCompleted` event can carry
  it; that number still comes from the same sequence.
- **Existing rows are backfilled in the same migration, in this order,** so the two kinds of number
  never collide on the unique index:
  1. rows with a `legacy_invoice_id` get that number;
  2. `setval` moves the sequence past the highest legacy number;
  3. the remaining v4-native rows get sequence values in `created_utc` order.

  After the backfill the same migration makes the column `NOT NULL`. It keeps its sequence
  default, so it is never empty and still passes the forward-only gate: an `INSERT` that omits the
  column gets a number from the default.
- **Domain:** `Invoice.InvoiceNumber` is a `long` that always comes from the database sequence
  (`ValueGeneratedOnAdd`). A v4 sale only sets it to a value it reserved from that sequence;
  the MigrationTool is the one exception, and keeps the legacy invoice id (§4, MigrationTool).

**MigrationTool:**
- It writes `invoice_number = LegacyInvoiceId` for each imported v3 invoice (next to
  `SqliteMigrationService.cs:510`).
- **The target must hold no v4-native invoices.** Their sequence numbers would collide with the
  imported v3 numbers. The migrator refuses, with a clear message, when the target already has
  invoices without a `legacy_invoice_id`. The plan checks whether today's "Fresh target" rule already
  guarantees this, and pins it with a test either way.
- After the invoice phase, **inside the same transaction**, it runs
  `setval('invoice_number_seq', max(invoice_number))`. The next v4 sale then carries on from the
  store's last v3 bill.
- `verify` gains two checks:
  - every migrated invoice has `invoice_number = legacy_invoice_id`;
  - the sequence's next value is greater than the maximum `invoice_number`.

**Sale response:** `POST /sales/complete` returns the new `InvoiceNumber`, so the receipt printed
right after a sale shows the real number.

**Forward-only gate:** apply the release's schema, then write a complete sale using only the
previous release's columns. The row must get an `invoice_number` from the default. This is recorded
in `docs/operations/upgrade-procedure.md` as for earlier releases.

## 5. Permissions

| Capability | Roles | Grants |
|---|---|---|
| `reports.view` (existing) | StoreManager, SystemAdmin | Bills of **any** date |
| `sales.reprint` (**new**) | Cashier, StoreManager, SystemAdmin | Bills of **today** (store business date) + reprint |

- **"Today"** is the server's Bangkok business date, from the same clock as the cash drawer
  (`ICashDrawerClock`).
- **A caller with `sales.reprint` but not `reports.view`** is held to today:
  - a list for any other date → `403`;
  - a bill from another day → `404`, so an old number's existence is not revealed.
- **Reprint limits differ by role:** managers can reprint any day, cashiers only today.
- **The cashier rule is "today's figures yes, other days no".** Cashiers count the drawer, so they
  already see today's totals through `GET /cash/summary`. That same route also takes
  `?businessDate=`, so today (`CashEndpoints.cs:21-25`, merged in #97) a cashier can read **any past
  day's** sales totals. The four sibling reads (`/cash/counts`, `/cash/payouts`,
  `/cash/floats`, `/cash/debt-repayments`) take the same parameter, and past counted cash is the most
  sensitive figure of all. This spec closes all five: every `/cash` read whose `businessDate` is not
  today requires `reports.view` (→ `403`), enforced once for the whole `/cash` group so a future
  route cannot forget it. *(Widened from `/cash/summary` alone in plan review, 2026-09-29.)* This is a small fix to
  shipped code, made before any till runs v4.
- **Shared rules, as for `/cash`:**
  - the user id comes from the token, never the body;
  - a token without a usable user id → `401`;
  - no capability → `403`;
  - Thai `{ error }` bodies.

## 6. API (REST, one resource root)

Bills are the resource **`/sales`**. `/reports/*` keeps aggregates only (sales summary, product
sales).

| Route | Purpose | Notes |
|---|---|---|
| `GET /sales?from=&to=&page=&pageSize=` | List bills, newest first | Filters and paging are query params; `from`/`to` default to today's business date; `pageSize` default 50, max 200 |
| `GET /sales/{id:guid}` | One bill by internal id | |
| `GET /sales/{number:long}` | One bill by receipt number | Same body as the Guid route; typed route constraints tell the two apart |
| `POST /sales/{id:guid}/reprints` | Create a reprint record | `201` + the bill detail and the reprint record (who, when) |

**Response bodies:**
- **List items** carry: `Id`, `InvoiceNumber`, `CreatedUtc`, `TotalAmount`, `PrimaryPaymentMethod`,
  `LineCount`. Per-bill amounts stay for today-only callers: they help a cashier find the right bill
  ("the ฿350 one at 2 pm"), and today's figures are cashier-visible by design (§5). There is still no
  page total, because the list is not a report.
- **Detail** = today's `InvoiceDetailDto` plus what a receipt needs:
  - `InvoiceNumber` and the cashier's display name;
  - amount received, change given, and an `IsRefund` flag;
  - per line: barcode, note, and the product's category kind (for the Hardware/General split);
  - the PayLater marker and amount.

  The plan checks these field by field against `ReceiptPrinterService`.

**Changes to existing routes:**
- `GET /reports/invoices` and `GET /reports/invoices/{invoiceId:guid}` are **retired**. Nothing on
  the till calls them, because of the stubs; their tests move to `/sales`.
- The void spec's route becomes **`POST /sales/{id:guid}/void`**, edited in the same PR as this
  spec.
- `POST /sales/complete` → `POST /sales` is **not** done here. It belongs to the separate route
  tidy-up PR, which must land before Phase B while no till runs v4.

**Reprint record:** table `invoice_reprint`, append-only and immutable, with no update or delete:

```
Id               Guid      PK
InvoiceId        Guid      FK → invoice.id
StoreId          string
CreatedUtc       DateTime  = when reprinted
LastModifiedUtc  DateTime  = CreatedUtc (repo convention; never changes)
CreatedByUserId  Guid      = who reprinted
```

One `SaveChangesAsync` writes the row and an **`InvoiceReprinted`** outbox event. The record
means a reprint was **requested**: a printer failure afterwards leaves the record in place, and
pressing reprint again writes a second one. The owner sees both attempts.

## 7. Till — ViewModels and screens

**Project `src/IndyPOS.Presentation`:**
- net10.0 with `CommunityToolkit.Mvvm`;
- references Application only, with **no WinForms reference**;
- tests in a new `tests/IndyPOS.Presentation.Tests`, using a fake `IStoreHubClient`.

This is the first ViewModel project in the repo. Cash-drawer plan 3 and the void feature reuse it.

**Client:** new `IStoreHubClient` methods, implemented in `StoreHubHttpClient` with the existing
`SendAuthenticatedAsync` pattern and tested in `StoreHubHttpClientTests`:
- `ListSalesAsync(from, to, page)`
- `GetSaleAsync(Guid)`
- `GetSaleByNumberAsync(long)`
- `CreateReprintAsync(Guid)`

**`InvoiceHistoryViewModel`** (the list):
- **Mode from capabilities:**
  - **History** (`reports.view`): Today / This Month / This Year / a date range.
  - **Today only** (`sales.reprint` alone): no period picker, no totals.
- **Bill-number search:**
  - non-numeric text → Thai error;
  - not found → "ไม่พบบิลเลขที่ …";
  - found → opens its detail directly, on any day for managers.
- **Paging:** "load more", 50 at a time.
- **Latest request wins.** Changing period or search cancels the in-flight load, so an older
  response can never overwrite a newer list.
- **No optimistic UI.** `IsBusy` and `ErrorMessage` state; nothing changes until the server
  answers.

**`InvoiceDetailViewModel`** (one bill, keyed by **Guid**):
- **Contents:** number, date/time shown in **Bangkok time** (the server sends UTC), cashier, lines,
  payments, total, received, change, and refund and PayLater badges.
- **The Hardware / General subtotal split** moves here from `SaleHistoryByInvoiceIdForm`'s
  code-behind.
- **`ReprintCommand`:**
  - enabled with `sales.reprint`, within that capability's date rule;
  - calls `CreateReprintAsync`;
  - builds a copy receipt (§8) and prints it;
  - on failure, shows a Thai error and prints nothing.
- **The void feature later adds `VoidCommand` here.** This is the "invoice detail" the void spec
  refers to.

**Screens:** WinForms, lean. They show ViewModel state and forward clicks only, with no logic in
code-behind.
- **`InvoiceDetailView` (UserControl):**
  - a header with bill number, date/time, cashier and badges;
  - lines and payments grids, then totals;
  - a **พิมพ์ซ้ำ** button, and space kept for the void button.
- **`SalesHistoryReportPanel`, rebuilt:**
  - a top bar with the period buttons, date range and bill-number box (Enter searches);
  - the bill list on the left, with "load more";
  - `InvoiceDetailView` on the right.
- **Cashier entry:** a **บิลวันนี้** button on the sale screen, shown with `sales.reprint`, opens the
  same screen in today-only mode. Cashiers cannot open Reports.
- **`SaleHistoryByInvoiceIdForm`** (opened from the PayLater panel) becomes a dialog that hosts
  `InvoiceDetailView`, keyed by the debt's Guid `InvoiceId`.
- **Binding:**
  - WinForms `DataBindings` on the ViewModels (`ObservableObject` raises `INotifyPropertyChanged`);
  - commands through .NET 10 WinForms' `Button.Command` binding if it proves usable, otherwise a
    small Click → `Execute` helper with `Enabled` following `CanExecute`.

**Removed:** the stubbed `IReportService` / `StoreHubReportService` members these two screens used:
- `GetInvoicesByPeriodAsync`, `GetInvoicesByDateRangeAsync`;
- `GetInvoiceProductsByInvoiceIdAsync`, `GetPaymentsByInvoiceIdAsync`;
- `GetInvoiceInfoAsync` and `StubInvoiceInfo`.

The other stubs stay (§9).

## 8. Receipt printing

**`ReceiptDocument`** (a record in Application) becomes the printer's only input, replacing
`IInvoiceInfo`:
- **Bill details:** `InvoiceNumber`, date/time (Bangkok), **original cashier name**, lines,
  payments, total, received, change, `IsRefund`;
- **Reprint fields:** `IsCopy`, `ReprintedAtUtc` and `ReprintedBy`;
- **Later:** the void feature adds `IsVoided` for its VOID banner.

**`ReceiptDocumentFactory`** (Application, unit-tested) builds it in two places:
1. **After a sale:** `StoreHubSaleService` builds it from the sale response, which now carries the
   bill number. This fixes the `0000000000` bug.
2. **On reprint:** `InvoiceDetailViewModel` builds it from the `POST /sales/{id}/reprints`
   response, with `IsCopy = true`.

**What the printer prints:**
- `Invoice No.: {InvoiceNumber:0000000000}`.
- The document's cashier, **not** the logged-in user.
- On a copy:
  - a large **สำเนา / COPY** banner at the top;
  - the line **"พิมพ์ซ้ำ dd/MM/yyyy HH:mm โดย <name>"**.
- Refund labels work as today.
- The `[Conditional("RELEASE")]` print gate is unchanged. Drawing code stays thin and only reads
  the document.

## 9. Cloud sync, errors, gaps

**Cloud:**
- **`InvoiceCompleted`** carries `InvoiceNumber`.
  - `cloud_invoice` gains a nullable `invoice_number` column, backfilled where known.
- **Bulk migration carries the number too.** Migrated v3 history reaches the cloud once, through
  `SqliteMigrationService.BuildBulkMigrationRequestAsync` → `BulkMigrationCommandHandler`, not through
  `InvoiceCompleted`. So `MigratedInvoice` (`BulkMigrationCommand.cs:45`) gains a **nullable**
  `InvoiceNumber`: the migrator fills it and the handler stores it. It is nullable so either side
  can be older: an older cloud ignores the field, and an older migrator sends `null`.
- **`InvoiceReprinted`** populates a new **insert-only `cloud_invoice_reprint`** mirror.
  - Idempotency comes from `ProcessedEvents`.
  - `HasComment` on the table and columns keeps it readable for dashboards and AI.
- **Ordering guard**, as in the void spec: a reprint event that arrives before its
  `InvoiceCompleted` **fails so it retries**, and is never marked processed.
- **Deploy the cloud handler before any store runs this version.** `EventProcessor` still drops
  unknown event types (a separate known bug).

**Errors** (Thai `{ error }` bodies):

| Case | Response |
|---|---|
| Malformed bill number or date | `400` |
| Unknown bill, or another day's bill for a today-only caller | `404` |
| Another date's list for a today-only caller, or no capability | `403` |
| Token without user id | `401` |

The till shows each of these, and a printer failure, as a Thai message.

**Phase B gaps: listed, not built here.** Each needs its own small spec before go-live:
1. **`InvoiceProductsReportPanel`**: products sold over a date range. The server only has the
   per-product aggregate `/reports/product-sales`.
2. **`PayLaterPaymentsReportPanel`**: the PayLater list and report. The server's `/reports/pay-later`
   groups by customer.
3. **`CashFlowCalculatorPanel`'s PayLater-today stub.** Likely replaced by cash-drawer plan 3's
   screen.
4. **Legacy int-id DTOs** (`InvoiceDto`, `InvoiceProductDto`, `InvoicePaymentDto`,
   `PayLaterPaymentDto`, `IInvoiceInfo`/`InvoiceInfo`). Delete them once 1–3 have moved.

## 10. Testing

Negative-first, `Subject_WhenScenario_DirectVerbOutcome`, one behaviour per test.

**Server (StoreHub integration, real Postgres):**
- **Today-only caller:**
  - asking for another date's list → `403`;
  - opening another day's bill by number → `404`;
  - reprinting another day's bill → `404`.
- No capability → `403`; token without user id → `401`; malformed number → `400`.
- The number route and the Guid route return the same bill.
- A reprint writes exactly one `invoice_reprint` row and one `InvoiceReprinted` event in the same
  save; a rejected request writes neither.
- The list never includes a page total.
- Each of the five `/cash` reads for a past `businessDate` → `403` for a caller without
  `reports.view`; a past date with `reports.view` → `200`; today → `200` (regression tests for the
  #97 gap).

**Bill number:**
- Two concurrent sales get distinct numbers.
- A sale written with only pre-release columns still gets a number (forward-only gate).
- The migration backfills existing rows: legacy ids first, then `setval`, then the native rows in
  `created_utc` order. A database holding both kinds ends with no duplicates and the sequence past
  the maximum.
- MigrationTool:
  - imported invoices keep their v3 number;
  - the next sale continues after the maximum;
  - `verify` fails when either rule is broken;
  - a target that already has v4-native invoices is refused.

**Client:** each new `StoreHubHttpClient` method's URL, auth and error mapping.

**ViewModels:**
- The mode follows the capabilities.
- Search rejects non-numeric input, reports not-found, and opens a found bill.
- A slower earlier load cannot overwrite a later one.
- "Load more" appends.
- Reprint is disabled without `sales.reprint`.
- A failed reprint prints nothing and shows the error.
- The Hardware / General split sums correctly.
- Times display in Bangkok time.

**`ReceiptDocumentFactory`:** bill number, original cashier, `IsCopy` and reprint line, refund,
change.

**Cloud:**
- `InvoiceReprinted` before `InvoiceCompleted` stays pending, and succeeds after.
- A duplicate event is processed once.
- A bulk-migrated invoice lands in `cloud_invoice` with its v3 `invoice_number`.

## 11. Timing & ordering

- **Order: this spec → cash-drawer plans 2–3 → invoice void.** It is a Phase B prerequisite.
- **It changes MigrationTool** (v3 numbers + sequence), so it should land **before the Epic 3
  Phase A rehearsal**. Otherwise the rehearsal must run again to prove the bill numbers.
- **The API route tidy-up PR** (conventions doc + renames) is separate and also lands before
  Phase B.

## 12. Open questions

None. Deferred by decision:
- the bill number as a barcode on the receipt (§2);
- the other stubbed reports (§9 gaps);
- existing route renames (route tidy-up PR).
