# Design — Persist Cash-Flow Inputs (v4)

> **Status:** Approved (reviewed item by item with Pond, 2026-09-25/26) · **Date:** 2026-09-20 ·
> **Layer scope:** Domain → Application → Infrastructure → StoreHub API → Presentation (ViewModel)
> → Windows.Forms, plus CloudApi (mirror tables + event handlers)

## 1. Context & problem

In v3 (and v4 today) the cash-flow screen works out the day's expected vs actual cash. Its inputs
come from two places:

- **Derived from sales already in the database** — general/hardware/cash/transfer/welfare/PayLater
  totals and today's new PayLater list. Nothing new to store.
- **Typed in by hand through the day** — four inputs:
  1. **Cash-payout** (รายจ่าย, money out of the drawer) — `CashFlowData.Payouts`
  2. **Cash-float** (เงินทอน, money into the drawer) — `CashFlowData.Changes`
  3. **Customer debt repayment** (ลูกค้าชำระหนี้, money in when a customer pays off PayLater) —
     `CashFlowData.PaidPayLaterPayments`
  4. **Cash count** (the 9 banknote/coin counts) — `CashFlowData.BankNote*Count` / `Coin*Count`

On save, `CashFlowCalculatorPanel` writes the whole `CashFlowData` — lists included — to a
per-day JSON file on the till (`C:\ProgramData\IndyPOSCashFlow\Data\CashFlow_yyyy-MMMM-dd.json`), plus a
totals-only CSV copied to Google Drive (the lists are `[Ignore]`d in the CSV). That file-based approach
is the v3 legacy. Two gaps:

1. **Not queryable or editable** — the hand-typed inputs live in one loose JSON file per day per till, not in the
   database, so they cannot be queried, reported on, corrected with an audit trail, or synced.
2. **Payouts are undifferentiated** — there is no way to tell a *hardware* payout from a *general*
   (เบ็ดเตล็ด) one, even though the store already splits **sales** into
   `HardwareProductsTotal` vs `GeneralProductsTotal`.

## 2. Goals / non-goals

**Goals**
- Persist all four hand-typed inputs (§1) to StoreHub PostgreSQL, so the whole cash-flow calculation
  can be rebuilt from the database. Payouts, floats and repayments are individually queryable,
  timestamped rows.
- **Retire both v3 files.** The panel stops writing and reading the per-day JSON file, and stops
  writing the CSV and copying it to Google Drive. StoreHub is the single source of truth.
  **The Drive CSV was the off-site record for audit and later review** — so its replacement is cloud
  sync (§7), which this feature therefore requires, not just "nice to have". Cloud sync is the core of
  the v4 project: managers, admins and owners read store data from anywhere through a dashboard, and
  an MCP server will let AI agents (Claude, ChatGPT) answer "show store status / reports". So the
  cash-flow data must land in the cloud **queryable**, with its ids and audit fields intact.
- Tag each **payout** as `Hardware` or `General` (default `General`), chosen by the cashier.
- Allow correcting mistakes: entries are editable and removable, with an audit trail.
- Attribute each entry to the cashier who created/last-edited it.

**Non-goals (this change)**
- No import of historical cash-flow data. v4 starts fresh from the day it lands; the v3 JSON files
  and the CSVs already on Google Drive stay where they are, untouched, as the archive.
- No persisted sales totals or expected-vs-actual figures — they are derived from invoices and the
  persisted inputs on demand.
- Repayments are **not linked** to a customer's PayLater debt (`PayLater.PaidAmount` is untouched) —
  they are a hand-typed list, like payouts. See §11.
- No system-wide audit retrofit — see §11.

## 3. Domain model

New entities in `IndyPOS.Domain/Entities/Core`, following the existing convention (`Guid Id`,
`CreatedUtc`/`LastModifiedUtc`, money as `decimal`; see `Payment`, `PayLater`).
`StoreId` is a `string` in every new table, matching the codebase convention (`Invoice`,
`StoreUser`, `OutboxEvent`, CloudApi, `IStoreIdentityService`).

```
CashPayout                                   CashFloat  (เงินทอน)
----------                                   ---------
Id                 Guid    PK                Id                 Guid    PK
StoreId            string                    StoreId            string
Category           PayoutCategory (default   Amount             decimal
                     General, string)        Description        string?
Amount             decimal                   BusinessDate       DateOnly
Description        string?                   CreatedUtc         DateTime
BusinessDate       DateOnly                  LastModifiedUtc    DateTime
CreatedUtc         DateTime                  CreatedByUserId    Guid
LastModifiedUtc    DateTime                  LastModifiedByUserId Guid?
CreatedByUserId    Guid                      IsDeleted          bool
LastModifiedByUserId Guid?                   DeletedUtc         DateTime?
IsDeleted          bool
DeletedUtc         DateTime?
```

```csharp
public enum PayoutCategory { General, Hardware }   // persisted as string; General is the default
```

**Why two tables, not one `CashDrawerEntry` + discriminator:** a single table forces `Category`
nullable (only payouts have it) and mixes two concepts. Two tables keep `Category` non-null and match
how the store already thinks of them (two separate lists). The small duplication is worth the clarity.

**Debt repayment (ลูกค้าชำระหนี้) — a hand-typed list, like payouts.**

```
DebtRepayment
-------------
Id                   Guid      PK
StoreId              string
CustomerName         string    required (not blank)
Amount               decimal   > 0
BusinessDate         DateOnly
CreatedUtc           DateTime
LastModifiedUtc      DateTime
CreatedByUserId      Guid
LastModifiedByUserId Guid?
IsDeleted            bool
DeletedUtc           DateTime?
```

- `CustomerName` is **required** — today's screen accepts a blank name; v4 rejects it at the boundary.
- No `Hardware`/`General` tag: a repayment settles a debt, not a product type, and v3 only ever
  tracked a single repayment total.
- Not linked to the customer's `PayLater` row (see §2 non-goals, §11).

**Cash count — a new row for every count, never edited or removed.**

```
CashCount
---------
Id                   Guid      PK
StoreId              string
BusinessDate         DateOnly            index (StoreId, BusinessDate, CreatedUtc)
BankNote1000Count    int       >= 0
BankNote500Count     int       >= 0
BankNote100Count     int       >= 0
BankNote50Count      int       >= 0
BankNote20Count      int       >= 0
Coin10Count          int       >= 0
Coin5Count           int       >= 0
Coin2Count           int       >= 0
Coin1Count           int       >= 0
CreatedUtc           DateTime            = when the count was taken
LastModifiedUtc      DateTime            = CreatedUtc (repo convention; never changes)
CreatedByUserId      Guid                = who counted
```

- Thai denominations are a fixed set, so nine columns beat a child table per denomination.
- **Every count is kept.** Cashiers count about three times a day (~06:00, before lunch, ~20:00),
  often different people, and that is deliberate — it is how the owner traces *when* and *on whose
  count* the drawer went wrong. So each saved count is a **new row**; nothing overwrites it.
- **Only the latest count of the day is used in any calculation** (highest `CreatedUtc` for that
  `StoreId` + `BusinessDate`): counted total, expected cash and the difference. Earlier counts are
  **history and audit only** — never summed, averaged or compared by the cash formula.
- **Rows are immutable:** no edit, no delete, no soft-delete. A mistyped count is fixed by saving a
  new count, which then becomes the latest; the wrong one stays on record — that is the audit trail
  working, not a flaw. This is why there is no `LastModifiedByUserId`.
- The screen shows the latest count, plus a read-only list of the day's counts (time, cashier,
  total) so anyone can see earlier ones.
- Totals, expected cash and the difference are **not stored** — derived on demand, as today.
- **Reset never touches saved rows.** Reset clears the counting fields on screen only (as
  `ResetCountingButton_Click` does today); saving afterwards adds a new row. If they reset and walk
  away without saving, the latest saved count is unchanged and reloads next time.

**Cash flow is per store, not per till.** Each store has one central cash drawer, and stays that way
even if more terminals are added. So every entry and the cash count are keyed by `StoreId` +
`BusinessDate` only — no terminal id. All terminals in a store share one cash flow for the day.

**Field notes**
- `BusinessDate` (`DateOnly`) is the store-local cash-flow day an entry belongs to (store TZ is
  Bangkok); `CreatedUtc` is the UTC instant. Grouping/queries use `BusinessDate`; reuse the existing
  Bangkok→UTC handling from the migration/sales code.
- **`BusinessDate` is set by the server, never the client:** the Bangkok calendar date at the moment
  the row is created. The request body has no date field, so a wrong till clock cannot file an entry
  on the wrong day. **Edits never change it** — correcting yesterday's payout today leaves it on
  yesterday. The day boundary is **plain midnight**: all three stores close around 20:00 and nobody
  counts after midnight (counts happen ~06:00, before lunch, and ~20:00), so no day-cutoff setting
  is needed (YAGNI).
- `CreatedByUserId` / `LastModifiedByUserId` reference the StoreHub user (v4 auth).
- Soft-delete via `IsDeleted` + `DeletedUtc`: the row stays for audit but leaves calculations and
  normal queries.

## 4. Persistence & schema

- EF Core configurations under `IndyPOS.Infrastructure/Persistence/StoreHub/Configurations` —
  `CashPayoutConfiguration`, `CashFloatConfiguration`, `DebtRepaymentConfiguration`,
  `CashCountConfiguration`; `PayoutCategory` mapped with `.HasConversion<string>()`; `Amount` mapped
  like existing money columns.
- Repositories `ICashPayoutRepository` / `ICashFloatRepository` / `IDebtRepaymentRepository` /
  `ICashCountRepository` (`Application/Abstractions/StoreHub/Repositories`) + implementations
  (`Infrastructure/Persistence/StoreHub/Repositories`).
- **Soft-delete is a global query filter**, not a `where` in each query: the three soft-deletable
  tables (payout, float, debt repayment) get an EF 10 **named** filter,
  `HasQueryFilter("SoftDelete", x => !x.IsDeleted)`, so every query hides deleted rows by default.
  **Why:** a forgotten `where` would put a deleted payout back into the drawer's money — the filter
  makes that mistake impossible. The only opt-out is the delete handler's lookup
  (`IgnoreQueryFilters(["SoftDelete"])`), which must see a deleted row to answer the idempotent
  `204` (§5). Naming the filter lets later filters (e.g. per store) be added or bypassed
  independently. This is the **first global query filter in the repo** (products still filter
  `IsActive` explicitly) — deliberate, not an inconsistency to "fix". `CashCount` has no filter; its
  rows are never deleted.
- **One additive EF migration** creating the four tables.

**Forward-only release-gate compliance:** this migration only *adds* four new tables — no renames, drops,
type narrowing, or new `NOT NULL` column on an existing table. It is runnable against the previous
release's binaries (they simply ignore the new tables), so the installer's rollback claim holds
(`docs/operations/upgrade-procedure.md`).

## 5. Application layer

- **Commands** (each `[Verb][Noun]Command` + handler):
  `AddCashPayout`, `EditCashPayout`, `DeleteCashPayout` (sets `IsDeleted`/`DeletedUtc`) — mirror trio
  for `CashFloat` and for `DebtRepayment`. `Add` defaults `Category = General` when unspecified
  (payouts only). `Edit`/`Delete` set `LastModifiedByUserId` + `LastModifiedUtc`.
- **Cash count:** `AddCashCount` only — no edit or delete command exists (append-only, §3).
- **Queries:** `GetCashPayoutsQuery` / `GetCashFloatsQuery` / `GetDebtRepaymentsQuery` by `StoreId` +
  `BusinessDate` (optionally a range), excluding soft-deleted; `GetCashCountsQuery` returns the day's
  counts newest first (the history list).
- **Edge rules**
  - **Only today is editable.** Add / edit / delete (and adding a count) work only on today's
    Bangkok `BusinessDate`; any earlier day is **view-only**. **Why:** the count-vs-expected
    comparison exists so a mismatch is raised and resolved **the same day** — a past day is a
    closed record. Adds are safe by construction (the server stamps today, §3). Edit / delete check
    the stored row's `BusinessDate` and reject a past-day row with **`409 Conflict`** (ProblemDetails,
    Thai message), even if the screen was opened before midnight. The ViewModel disables edit
    controls when the viewed day is not today, but the server is the guard. Correcting a closed day
    is out of scope — it belongs with admin corrections (§11).
  - **A deleted entry is gone to the API.** Editing a soft-deleted entry returns **`404`**, the same
    as an unknown id; the row stays in the table for audit only.
  - **Delete is idempotent.** Deleting an already-deleted entry returns **`204`** and changes
    nothing — the original `DeletedUtc` and `LastModifiedByUserId` (who deleted it) are kept, so a
    double-click or a retry after a network blip never rewrites the audit record, and no second
    outbox event is written. An **unknown** id is still `404`.
  - **Last save wins.** Two edits of the same entry are not checked against each other — no version
    column, no conflict message. With one shared drawer this does not happen in practice, and the
    audit fields record who saved last. Each save still emits its own outbox event, so the cloud keeps
    the earlier value in its event history.
  - **A failed save never shows in the list (no optimistic UI).** The ViewModel adds nothing to a
    list until the server confirms the save; it then reloads from the server. On failure (network,
    `409`, validation) the list is unchanged, **the typed input stays in the form** so nothing is
    re-typed, and a Thai error message is shown.
- Validation at the boundary (FluentValidation, as elsewhere): `Amount > 0`; `Category` a defined enum;
  `Description` length bound.
- **One summary query does all the maths:** `GetCashDrawerSummaryQuery(StoreId, BusinessDate)` gathers
  the day's sales and payment totals, the four inputs and the **latest** cash count, and returns
  expected cash, counted cash and the difference. The cash formula is unchanged
  (`… + CashFloatsTotal − PayoutsTotal`), now over persisted, non-deleted rows. Today the panel
  assembles `CashFlowData` itself (`CreateCashFlowData`); that assembly moves here, so the UI only
  renders what the query returns.

## 6. StoreHub API

All cash routes live in **their own file, `CashEndpoints.cs`**, as a `MapCashEndpoints()` extension
called from `Program.cs`, grouped under `/cash`. (Today every StoreHub route is inline in
`Program.cs` — there is no `ProductsEndpoints` to copy. Moving the existing routes out is a separate
clean-up, not part of this change.) The `cash.manage` policy is applied **once, on the group**.

```
GET    /cash/summary?businessDate=YYYY-MM-DD      §5 numbers: expected, counted, difference
POST   /cash/payouts                              add
GET    /cash/payouts?businessDate=YYYY-MM-DD      list (non-deleted)
PUT    /cash/payouts/{id}                         edit
DELETE /cash/payouts/{id}                         soft-delete
       /cash/floats           same four routes as payouts
       /cash/debt-repayments  same four routes as payouts
POST   /cash/counts                               add a count
GET    /cash/counts?businessDate=YYYY-MM-DD       the day's counts, newest first
```

- **No `PUT` or `DELETE` on `/cash/counts`** — counts are append-only (§3).
- **`businessDate` is optional on `GET`:** omitted means today in Bangkok. Past dates are allowed
  for viewing. Whether past days may be *edited* is decided in §5's edge rules.
- Request bodies never carry `BusinessDate` or a user id — the server sets both (§3, below).

**Authorization:** one new capability, `Capability.CashManage = "cash.manage"`, granted to
**Cashier, StoreManager and SystemAdmin** in `RoleCapabilities`, with a matching policy in StoreHub
`Program.cs`. It covers add/edit/delete/view for all four inputs and opens the ลิ้นชักเก็บเงิน section
(§8). One capability is enough — no view/edit split. Past-day edit rules are separate (see §5).

**Acting user comes from the login token, never from the request.** Handlers read the user id from
the token's `sub` / `NameIdentifier` claim (as `/auth/me` already does) and stamp `CreatedByUserId` /
`LastModifiedByUserId` themselves. Request bodies carry no user id, so nobody can write an entry in
someone else's name.

## 7. Cloud sync (outbox)

Required, not optional (§2): the cloud copy is the off-site audit record and what dashboards and the
future MCP server query.

**StoreHub side**
- Every add / edit / delete / new count writes an `OutboxEvent` carrying the **full current row**
  (all fields, incl. `Category`, audit fields, `IsDeleted`/`DeletedUtc`) — a state snapshot, not a
  delta. Drained by the existing `SyncWorker`.
- The event is written **in the same transaction** as the row (as `SaleRepository` does for
  `InvoiceCompleted`), so a row can never exist without its event.
- One event type per table: `CashPayoutChanged`, `CashFloatChanged`, `DebtRepaymentChanged`,
  `CashCountChanged`.

**Cloud side**
- Four mirror tables (`CloudCashPayout`, `CloudCashFloat`, `CloudDebtRepayment`, `CloudCashCount`),
  same shape as StoreHub's — queryable by dashboards and MCP.
- `EventProcessor` gains one handler per event type that **upserts by `Id`**: insert if new, update
  if present. **Stale events are ignored:** if the incoming `LastModifiedUtc` is not newer than the
  stored row's, skip it — retries and out-of-order delivery can never roll a newer edit back.
- Idempotency via `ProcessedEvents`, as today.
- Unlike `InvoiceCompleted` (insert-only, a sale never changes), these rows are mutable, hence upsert.
  (`CashCount` rows are insert-only like sales, but share the same upsert path — one handler shape.)

**Readable by AI agents, not just dashboards.** Everything pushed to the cloud will be read by our
own MCP server and the AI agents that use it, so the mirror tables must explain themselves:
- **Plain names, no codes:** descriptive table/column names; enums stored as strings (`Hardware`,
  not `1`); money as `decimal` baht; instants as `…Utc`, the cash day as `BusinessDate`.
- **Documented in the schema itself:** every mirror table and column gets a Postgres comment
  (EF `HasComment`), e.g. *"Latest row per StoreId+BusinessDate is the count used for the cash
  difference; earlier rows are audit history."* An agent reading the schema learns the rules
  without this spec.
- **Joinable ids:** `StoreId` joins the cloud store config; `CreatedByUserId` /
  `LastModifiedByUserId` should join cloud `Users` for the cashier's name — **verify at plan time**
  that StoreHub user ids equal the cloud master ids (users are distributed from the cloud, so they
  should).

**Handled separately (not in this change):**
- `EventProcessor`'s default case logs and **marks unknown event types processed**, i.e. drops them.
  It should leave them pending instead, so a store that upgrades before the cloud loses nothing. This
  changes shared cloud behaviour, so it gets its own change. Until then, **deploy the cloud before the
  stores** for this feature.
- Suspected bug: `ProcessInvoiceCompletedAsync` opens a bare `BeginTransactionAsync`, the same shape
  as the I0-E defect (`AddNpgsqlDbContext` enables retries, and Npgsql rejects a bare transaction
  then). The new handlers must use `CreateExecutionStrategy().ExecuteAsync`, and the existing one
  needs its own RED test + fix.

## 8. Windows.Forms

**The UI stays thin.** A later project ports the UI from WinForms to **Avalonia** (cross-platform),
so no business logic lives in the panel: no totals, no formula, no "which count is latest", no
validation beyond input shape. The panel calls StoreHub, renders the §5 summary, and sends user
input. Anything the Avalonia UI would otherwise have to re-implement belongs in Application.

**MVVM from this change on.** The target pattern for the Avalonia port is MVVM, and this panel is
being rewritten anyway, so it gets its ViewModel now and becomes the template for the port:
- **New project `src/IndyPOS.Presentation`** (`net10.0`, **not** `-windows`, so Avalonia can reference
  it as-is). References Application only; **no WinForms or Avalonia types.** Uses
  **CommunityToolkit.Mvvm** (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- **`CashDrawerViewModel`** owns the *screen* logic: the day's lists, the latest count and the count
  history, the nine denomination inputs, the selected payout category (default General), busy /
  error state, and whether Save / Delete are enabled. Its commands (add / edit / delete an entry,
  save a count, reset, refresh) call a client interface over StoreHub and reload the §5 summary.
  Business maths stays in Application (§5) — the ViewModel shows results, it does not compute them.
- **WinForms binds to it:** controls bind through `BindingSource` / `DataBindings`, buttons through
  `Button.Command` (supported since .NET 7). The panel's code-behind shrinks to wiring and layout.
- **Tests:** new `tests/IndyPOS.Presentation.Tests` exercises the ViewModel against a fake client —
  no UI, no StoreHub. Same naming and negative-first rules as §10.
  Includes: a failing client leaves the list unchanged, keeps the typed input, and sets the error
  message; edit controls are disabled when the viewed day is not today.


- **Move the panel out of Reports into its own top-level section, labelled ลิ้นชักเก็บเงิน (Cash
  Drawer).** Cash flow is a daily job the cashier does all day and at close — not a report. Add a
  `SubPanel.CashDrawer` value and a main-menu button in `MainForm`; remove `CashFlowCalculatorPanel`
  from `ReportsPanel` (and its `ReportSubPanel` entry). Reports keeps `ReportsView`; the new section is
  gated by its own capability (§6).

- `CashFlowCalculatorPanel`: the add-payout UI gains a **Hardware/General selector defaulting to
  General**; every add / edit / delete / count is saved to StoreHub **immediately**, one call per
  action — never batched until close. The day's view comes from one `GET /cash/summary` plus the
  lists (all through `CashDrawerViewModel`).
- Deleting an entry in the UI performs a soft-delete.
- **Remove the file code:** `LoadDataFromFile`, `SaveDataToJsonFileAsync`, `SaveDataToCsvFileAsync`,
  `CopyCsvFileToSharedDirectory`, and the three hard-coded paths (incl. `G:\My Drive\Rungrat\...`).
  The panel no longer needs `IJsonService` / `ICsvService`; drop the `[Name]`/`[Ignore]` CsvHelper
  attributes from `CashFlowData` if nothing else uses them.

## 9. Rename: `Change` → `CashFloat`

The in-memory concept is currently named `Change`/`Changes` (เงินทอน) but means *cash-float*. Rename
the C# identifiers for clarity: `Change` model → `CashFloat`; `CashFlowData.Changes` → `CashFloats`;
`ChangesTotal` → `CashFloatsTotal`; related panel members. Safe rename — the only things that
depended on the old names were the JSON file (property names) and the CSV (Thai headers), and both
are retired (§2). The new tables are named `CashFloat` from the start.

## 10. Testing strategy

**Naming:** `Subject_WhenScenario_DirectVerbOutcome` — scenario introduced by `When`/`With`, outcome
starts with a direct verb (`Returns`, `Throws`, `Keeps`, `Excludes`, `DoesNot…`), never `Should`.
One behaviour per test. Example: `AddCashCount_WhenSecondCountSameDay_KeepsBothRows`.

**Negative tests first — aim for more than half** failure / edge / boundary cases, with named
constants for the boundary values:
- `Amount` zero or negative → rejected (payout, float, debt repayment).
- A negative denomination count → rejected.
- Unknown `PayoutCategory` → rejected.
- Debt repayment with a missing or blank `CustomerName` → rejected.
- Caller without `cash.manage` → `403`.
- Edit of a soft-deleted or unknown id → `404`; delete of an unknown id → `404`.
- A normal query never returns a soft-deleted row (global filter), while the delete lookup still
  finds it.
- Delete of an already-deleted row → `204`, with `DeletedUtc` / `LastModifiedByUserId` unchanged and
  no new outbox event.
- Edit or delete of a **past-day** row → `409`, and the row is unchanged (incl. a row created just
  before midnight and edited just after).

**Cash count (append-only rules):**
- A second count on the same day keeps both rows.
- The calculation uses **only the latest** count of the day; earlier counts do not change the
  counted total, expected cash or difference.
- There is no edit or delete route for counts (a `PUT`/`DELETE` returns `405`/`404`).
- Two counts with the same `CreatedUtc` resolve to the same "latest" every time — ties break by `Id`,
  so the answer is deterministic.

**Per layer:**
- **Domain/Application:** handler tests — default category is `General`; soft-delete excludes rows
  from queries and totals; edit bumps `LastModifiedUtc`/`LastModifiedByUserId`; `BusinessDate` is
  server-derived and unchanged by edits; the cash-drawer summary (§5) computes expected / counted /
  difference correctly. Because the formula lives here, not in the UI, it is tested without WinForms.
- **Infrastructure/Integration:** StoreHub integration tests (Testcontainers Postgres) for the four
  endpoint groups incl. soft-delete round-trip, `businessDate` filtering, and the outbox event
  written in the same transaction as the row.
- **Migration:** verify the additive migration applies and that a pre-migration binary can still write
  a sale (forward-only gate recipe in `upgrade-procedure.md`).

## 11. Future considerations (out of scope)

- **System-wide traceability review:** the `CreatedByUserId` / `LastModifiedByUserId` pattern adopted
  here is valuable beyond cash entries. A later pass should look for gaps — entities that mutate
  without recording *who* — and apply the same pattern where it earns its place. Not done here to keep
  this change focused.
- **Admin sale corrections (own spec):** today a sale cannot be changed after it completes, e.g. when a
  cashier picks the wrong payment type. Never overwrite money records; keep the original and record
  who changed what, from → to, when. Start with **payment-method correction only**, limited to swaps
  with no debt side effect (Cash ↔ Transfer ↔ Welfare card); to/from PayLater creates or removes a
  debt and comes later. Wrong items/quantities → **void and re-ring** (reverses stock) instead of
  editing lines. The cloud needs a correction event, since `InvoiceCompleted` is insert-only. Cash
  flow follows automatically, as expected cash is derived from sales.
- **Link repayments to PayLater debt:** record a repayment against the customer's `PayLater` row so
  `PaidAmount` / `IsCompleted` update. Only if really needed — a much bigger feature than a hand-typed
  list.

- **Project MVVM skill (`.claude/skills/indypos-mvvm/`):** once `CashDrawerViewModel` has landed,
  capture the conventions it set — ViewModel folder layout, `[ObservableProperty]` /
  `[RelayCommand]` usage, "no UI types in `IndyPOS.Presentation`", ViewModel test naming — as a
  project skill (via `skill-creator`), so every screen in the Avalonia port is built the same way.
  Written from real code, not guessed up front. It sits **on top of** a general Avalonia skill set
  (e.g. `linuxdevel/Avalonia-skills`), which covers the framework itself.

## 12. Timing vs the store rollout

**Decision: it does not block Phase A; it must land before Phase B.**
- **Phase A (dev-machine rehearsal) starts now, in parallel with building this.** It runs on static
  database copies, so the new tables change nothing there — and it is the run most likely to find
  migration problems, so starting it early is worth more than waiting.
- **Phase B (real stores) waits for this feature**, so that:
  1. each store's v4 database has the cash tables **from day one** — no second migration on a live
     till;
  2. no store ever runs v4 on the old JSON + Google Drive CSV path — cashiers learn the new
     ลิ้นชักเก็บเงิน screen once;
  3. cash traceability to the cloud is live from the first real day, which is the point of the sync.
- **Ordering rule:** the cloud (with the four new event handlers) is deployed **before** the first
  store goes live, because `EventProcessor` currently drops unknown event types (§7). The Phase A-2
  rehearsal (with Cloud API) should cover the cash sync end to end.

## 13. Open questions

None open. Resolved: `BusinessDate` source — server-side Bangkok date at creation, midnight boundary
(§3 field notes). One thing to **verify at plan time**, not decide: StoreHub user ids equal cloud
`Users` ids (§7).
