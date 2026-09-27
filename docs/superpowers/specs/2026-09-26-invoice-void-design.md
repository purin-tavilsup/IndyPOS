# Design — Invoice Void (v4)

> **Status:** Reviewed — ready for planning · **Date:** 2026-09-26 (revised 2026-09-27) · **Layer scope:** Domain → Application →
> Infrastructure → StoreHub API → Presentation (ViewModel) → Windows.Forms, plus CloudApi
> (mirror table + event handler + view)

## 1. Context & problem

A completed sale in v4 can never be taken back. Two needs make that a blocker:

1. **Epic 3 Phase B** (`.planning/indypos-overhaul/epic-3-store-rollout-plan.md`, step 9) rings a
   real test sale from each terminal at go-live. Without a void, those sales stay in the store's
   money totals forever. The rollout plan makes this feature a **Phase B prerequisite**.
2. **Real mistakes** after go-live — a wrong item, a wrong amount, a customer who changes their mind
   before leaving. This is the first step of "admin sale corrections" in the cash-flow spec §11
   (`docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md`).

v4 already has **refund invoices** (a sale whose total is negative, `StoreHubSaleService.
IsRefundInvoice()`), which *reverse* money and stock by ringing a new negative sale. That is not a
void: both invoices stay in every list and count. This design adds a **true void**.

## 2. Goals / non-goals

**Goals**
- A manager or admin can void **today's** invoice, with a recorded reason.
- A voided sale **drops out of every sales figure** — sales summary, payments summary, product
  sales, PayLater lists and the cash-drawer summary — so the day's cash expectation returns to what
  it was.
- **Nothing is overwritten or deleted.** The invoice, its lines and payments stay exactly as rung;
  the void is a new record of who, when and why. Stock is reversed by new movements.
- The void reaches the cloud as a queryable, AI-readable record.

**Non-goals (this change)**
- Voiding a single line (void the bill and ring it again).
- Voiding a past day (a closed day stays closed — same rule as the cash drawer).
- Undoing a void (ring the sale again).
- A manager-override dialog on the cashier's screen (§8).
- Payment-method correction without a void (still §11 of the cash spec).

## 3. Decisions (agreed 2026-09-26)

| Question | Decision | Why |
|---|---|---|
| Reverse or void? | **True void**, not a refund-style negative invoice | The owner and AI agents see one clearly voided bill, not a sale plus its mirror |
| Who? | **StoreManager and SystemAdmin only** — new `sales.void` | "Ring, pocket the cash, void" is the classic drawer theft; a void needs someone above the cashier |
| How far back? | **Today only** (store timezone) | Voiding yesterday would silently change a counted, closed day's expected cash |
| PayLater sale? | Void **cancels an untouched debt**; **refuses (409)** if any repayment is recorded | Paid money on a cancelled debt needs a human decision — later corrections work |
| Reason? | **Fixed list + optional note**; note required for `Other` | Groupable by dashboards and AI; test sales clearly marked |
| Storage? | **Separate `invoice_void` table + named "Voided" global filter** | The money record never changes; the cloud invoice stays insert-only; no report can forget the filter |
| Manager at the till? | **Manager signs in on that till** (existing login) | No new auth surface; voids are rare |
| PayLater races? *(2026-09-27)* | **One concurrency token (`PayLater.LastModifiedUtc`) for every PayLater write**; losers get `409` (§5a) | Closes void-vs-payment both ways and the pre-existing lost-payment race, with one rule |

## 4. Domain & persistence

```
InvoiceVoid                       (table invoice_void)
-----------
InvoiceId        Guid      PK + FK → invoice.id   (one void per invoice, enforced by the DB)
StoreId          string
Reason           VoidReason  persisted and serialized as its name
Note             string?   ≤ 500; REQUIRED when Reason = Other
CreatedUtc       DateTime  = when voided
LastModifiedUtc  DateTime  = CreatedUtc (repo convention; never changes)
CreatedByUserId  Guid      = who voided
```

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<VoidReason>))]
public enum VoidReason { TestSale, WrongEntry, CustomerCancelled, Other }
// Thai labels: ทดสอบระบบ · คีย์ผิด · ลูกค้ายกเลิก · อื่นๆ
```

- **Immutable:** no update, no delete, no soft-delete.
- **No existing table changes.** The migration adds one table — forward-only-safe.
- **Named EF 10 filter `"Voided"`**, the second global filter in the repo (after `"SoftDelete"`):
  - `Invoice` — hidden when it has a void row;
  - `InvoiceLine`, `Payment`, `PayLater` — hidden when their invoice is voided.
  The PayLater filter **is** the debt cancellation: the `pay_later` row keeps its amounts; the void
  only stamps its `LastModifiedUtc` (see §5, step 4).
- **Stock is reversed, not filtered.** Stock = Σ movements, so the void writes one new
  `InventoryMovement` per original `Reason = "Sale"` movement of that invoice, with the **opposite**
  `QuantityDelta` (`-original`), `Reason = "Void"`, `ReferenceId = invoiceId`. Opposite, not
  `+quantity`: a refund invoice's sale movement is already positive (stock came back), so voiding it
  must take that stock out again. A non-trackable product never had a movement, so it has nothing to
  reverse. `InventoryMovement` gets **no** filter.
- **Opt-outs** (`IgnoreQueryFilters(["Voided"])`): the void handler's lookup, the invoice list and
  invoice detail queries (so a voided bill still shows, badged), and the MigrationTool / verify
  path if it reads v4 invoices.

**Rollback caveat (documented, accepted):** binaries restored by an installer rollback do not know
the filter and would count voided sales again until re-upgraded. The schema stays compatible; only
the numbers are affected, and only after a rollback.

## 5. The void operation

`VoidInvoiceCommand(Guid InvoiceId, Guid UserId, VoidReason Reason, string? Note)` — `UserId` from
the token, never the body. Rules, in order:

1. **Validate input** — `Reason` defined; `Note` ≤ 500, trimmed, **required when `Other`** → `400`.
2. **Load the invoice ignoring "Voided"** — unknown → `404`; **already voided → `409`**
   ("บิลนี้ถูกยกเลิกแล้ว"). Deliberately *not* idempotent like cash delete: a second void usually
   means two managers on one bill, and they should find out.
3. **Today only** — the invoice's store-timezone date (from `CreatedUtc`, via `ICashDrawerClock`)
   must equal today → else `409`.
4. **PayLater guard** — a PayLater row with `PaidAmount > 0` → `409`
   ("มีการชำระหนี้บิลนี้แล้ว ยกเลิกไม่ได้"). The check alone is not enough: a repayment
   (`RecordPayLaterPaymentCommand`) saved between this check and step 5 would be cancelled with the
   debt. So `PayLater.LastModifiedUtc` is an EF **concurrency token**, and the void stamps it in the
   same save. If a payment slipped in, that update matches no row, the whole save rolls back, and the
   manager gets the same `409`. No schema change. See **§5a** for what the token means for payments.

### 5a. One rule for every PayLater write

The token is on `LastModifiedUtc`, not `PaidAmount`, because **both** writers stamp it, so any two
writes to the same debt conflict whichever lands first. That closes three races with one rule:

| Race | Today | With the token |
|---|---|---|
| Payment lands, then the void saves | Debt with money on it is cancelled | Void → `409` (step 4) |
| Void lands, then a payment saves | Money recorded on a cancelled debt, or a `500` from `PayLaterRepository.UpdateAsync` re-reading a now-filtered row | Payment → `409` |
| Two payments on one debt at once | Both read the old `PaidAmount`; one payment is **lost** (pre-existing bug) | Second payment → `409` |

**The token only works if its original value is the one the calculation used.** Today the payment
path reads the debt twice: `PayLaterRepository.GetByIdAsync` loads it untracked and the handler
computes `PaidAmount + payment` from that, then `UpdateAsync` re-reads the row and saves. EF would
compare against the *second* read, so a payment committed between the two reads would pass the check
and still be lost. So every PayLater write — payment and void — **loads the debt once, tracked,
changes that entity and saves it**; `UpdateAsync`'s second read is removed.

The payment path therefore changes too:
- A conflict, or a debt that vanished under the `"Voided"` filter between load and save, returns
  **`409`** "ข้อมูลหนี้เปลี่ยนแปลง กรุณาลองใหม่", never a `500`. Nothing is written.
- A debt that is already voided when the payment starts is simply not found (the filter hides it) →
  the existing not-found response.
- The till shows the message and keeps the typed amount; the cashier re-opens the debt and retries.
  This is a behaviour change on an existing endpoint: a payment that used to overwrite silently now
  asks for a retry. That is deliberate — for money, a retry beats a lost ฿.
5. **One `SaveChangesAsync`** writes the `invoice_void` row, the reversal movements and one
   `InvoiceVoided` outbox event — all or nothing.
6. **Returns** the void record: who, when, reason, note, and the voided total.

**Concurrency:** two simultaneous voids both pass step 2; the `invoice_void` primary key rejects
the second insert. Map the Npgsql unique violation to the same `409` (as I0-E learned), run inside
`CreateExecutionStrategy().ExecuteAsync` if an explicit transaction is ever needed.

## 6. API, permission, reports

```
POST /invoices/{id}/void      body { reason, note? }   → 201 + void record
```

- New `Capability.SalesVoid = "sales.void"` for **StoreManager, SystemAdmin**; policy
  `CanVoidSales`. Cashier → `403`.
- Same filters as `/cash`: a token without a usable user id → `401`; `{ error }` Thai bodies.
- Routes in their own file, `src/IndyPOS.StoreHub/Endpoints/Sales/InvoiceVoidEndpoints.cs`.
- No "undo void" route.
- **Existing PayLater payment route changes** (§5a): a write conflict now returns `409` with a Thai
  body instead of overwriting or failing with `500`.

**Effect on what people see**
- Sales summary, payments summary, product sales, PayLater lists, cash-drawer summary: voided sale
  excluded automatically by the filter.
- Invoice list and detail: still shown, with a **ยกเลิกแล้ว** badge plus reason, who and when (DTOs
  gain a nullable `Void` block).
- Receipt reprint of a voided invoice prints a large **ยกเลิก / VOID** banner.
- Stock history: the sale's `−qty` and the void's `+qty`, netting to zero.

## 7. Cloud sync

- **`InvoiceVoided`** outbox event, full payload: the void row plus snapshots of the reversal
  movements (the cloud already holds the sale's movements from `InvoiceCompleted`).
- **`CloudInvoiceVoid`** mirror table, insert-only; cloud invoice rows stay untouched and
  insert-only. Idempotent via `ProcessedEvents`.
- **Ordering guard:** a retry can deliver `InvoiceVoided` before its `InvoiceCompleted`. If the
  invoice is not in the cloud yet, the handler **fails the event so it retries** — never marks it
  processed.
- **AI / dashboard readable:** `HasComment` on the table and columns stating the rule, and a cloud
  SQL view **`valid_invoices`** excluding voided sales, which the MCP server and dashboards query.
- **Deploy the cloud handler before any store can void** — `EventProcessor` still drops unknown
  event types (separate fix, see cash spec §7).

## 8. UI (MVVM)

- **Where:** Reports → invoice detail → **ยกเลิกบิล** button, visible only with `sales.void`,
  enabled only for today's, not-yet-voided invoices.
- **Dialog:** reason picker (4 Thai reasons), note box (required for อื่นๆ), summary
  "บิล #… ยอด ฿…", then a second confirmation — a void cannot be undone.
- **`InvoiceVoidViewModel`** in `IndyPOS.Presentation` owns enablement, the note rule and error
  display; same **no-optimistic-UI** rule as the cash drawer (nothing changes on screen until the
  server confirms; the typed note survives a failure). WinForms binds to it.
- **Manager at the till:** the manager signs in on that till, voids, signs out. A manager-override
  dialog is deferred until voids prove common (it needs a one-time elevated-action endpoint and its
  own security review).
- **Ordering:** needs the `IndyPOS.Presentation` project from cash-drawer plan 3; if this feature
  is built first, its plan creates that project.
- **Prerequisite — invoice history on v4 (separate spec).** On the v4 till the invoice list and
  detail are not reachable today: every invoice method in `StoreHubReportService` is a stub (the
  list returns empty; detail still takes legacy `int` ids and returns `StubInvoiceInfo`), although
  StoreHub already serves `GET /reports/invoices` and `GET /reports/invoices/{id}`. A separate
  spec moves that client and `SalesHistoryReportPanel` onto Guid ids and an
  `InvoiceHistoryViewModel`. This feature's ยกเลิกบิล button plugs into that detail view; it does not
  build its own lookup.

## 9. Testing

- **Negative first:** cashier → `403`; token without user id → `401`; unknown id → `404`; already
  voided → `409`; past day → `409`; PayLater with a repayment → `409`; `Other` without a note →
  `400`; undefined reason → `400`.
- Stock nets to zero for trackable lines; non-trackable lines produce no reversal.
- Voiding a **refund** invoice also nets stock to zero (its reversal movements are negative).
- Every report and the cash-drawer summary exclude the voided sale; invoice detail still returns it
  with the void block.
- Two concurrent voids → exactly one `invoice_void` row and one `409` (real PostgreSQL).
- PayLater races on real PostgreSQL, both sides loaded before either saves (§5a), one behaviour per
  test:
  - payment then void → the void gets `409`, and the payment stays;
  - void then payment → the payment gets `409`, and nothing is written;
  - two payments on one debt → one gets `409`, and `PaidAmount` equals the one that won (no lost ฿);
  - payment A commits **after** payment B loads the debt but **before** B saves → B gets `409`
    (pins the single-read rule);
  - payment on a debt voided before it started → not found.
- One `InvoiceVoided` event in the same save; none on any rejected path.
- Cloud: `InvoiceVoided` before `InvoiceCompleted` stays pending and succeeds after.
- Migration: one additive table; forward-only gate recipe (`docs/operations/upgrade-procedure.md`).
- Naming `Subject_WhenScenario_DirectVerbOutcome`, one behaviour per test.

## 10. Timing

A **Phase B prerequisite**: it must ship, with its cloud handler deployed, before the first store
cuts over. It does not block Phase A. It **depends on the invoice-history-on-v4 spec** (§8), which
is itself needed for Phase B, since managers must see sales history at go-live. Suggested order:
invoice history on v4, cash-drawer plans 1–3, then this (it reuses the Presentation project and the
clock), unless Phase B is scheduled sooner.

## 11. Open questions

None. Deferred by decision: manager-override dialog (§8), past-day voids and line voids (§2),
payment-method correction (cash spec §11).
