# Defect 14 — migrated stock double-counts the sales history

**Date:** 2026-08-09
**Branch:** `fix/migration-stock-double-count`
**Epic:** 2 — SQLite → PostgreSQL migration hardening
**Status:** design approved, not implemented

---

## Problem

`Product` has no stock column. `InventoryMovement.cs:6` defines current stock as
`SUM(QuantityDelta) WHERE StoreId AND ProductId`. The migrator writes that sum twice over:

- `MigrateProductsAsync` (`SqliteMigrationService.cs:242`) writes one `Migration:InitialStock`
  movement of `+QuantityInStock` — **today's** stock, already net of every sale the store ever made.
- `MigrateInvoicesAsync` (`SqliteMigrationService.cs:334`) then replays **every historical invoice
  line** as a `Migration:Sale` movement of `-Quantity`.

So every unit sold is subtracted a second time.

| Store | Net stock after migration | Products going negative |
|---|---|---|
| GeneralHardware | **−400,541 units** | 7,028 of 10,590 (66%) |
| MimyMart | **−249,652 units** | 2,350 of 6,335 (37%) |

Every till would show deeply negative stock the moment a store cut over. Nothing catches it:
`MigrationVerifier` has no stock or movement check at all, and every existing count-based assertion
reconciles perfectly against the wrong answer.

### Bug 14b, same method

Line 240 guards the movement with `if (product.QuantityInStock > 0)`. Products already sitting at
negative stock in the legacy data therefore get **no movement at all** and arrive as 0 — a silent
data change, not a decision.

## Measurements

Taken from the three real `Store.db` files on 2026-08-09.

| Store | Products | stock < 0 | = 0 | > 0 | Σ stock | Units sold | Never sold |
|---|---|---|---|---|---|---|---|
| GeneralHardware | 10,590 | 952 | 2,427 | 7,211 | 18,279 | 454,724 | 1,427 |
| MimyMart | 6,335 | 583 | 1,741 | 4,011 | 19,729 | 286,441 | 3,783 |
| MimyShop | 100 | 0 | 0 | 100 | 719 | 17 | — |

Value distribution:

| Store | Negative range | ≤ −100 | −99..−20 | −19..−1 | Positive max | ≥ 1000 | 100..999 | 1..99 |
|---|---|---|---|---|---|---|---|---|
| GeneralHardware | to −3,882 | 46 | 122 | 784 | 792 | 0 | 43 | 7,168 |
| MimyMart | to −2,095 | 25 | 81 | 477 | 1,175 | 1 | 24 | 3,986 |

**`QuantityInStock` has never been strictly maintained** (Pond, 2026-08-09) — restocks and
adjustments were often not keyed in. The distribution agrees: positives are plausible (99% under
100), while the negative tail reaching −3,882 is unrecorded restock, not real shelf state. 82% of
negatives are small drift in −1..−19.

## Decision — stop replaying sales

The migration writes exactly **one** movement per product and no longer replays history.

```
MigrateProductsAsync   ->  1 x Migration:InitialStock,  QuantityDelta = QuantityInStock
MigrateInvoicesAsync   ->  no inventory movement
```

Net stock after migration equals the number the old till displayed, product for product.

Sales history is not lost. It lives in `Invoice` / `InvoiceLine`, which is the source of truth for
sales reporting; `InventoryMovement` only ever claimed to answer "what is on the shelf now".
The migrated ledger becomes an honest one-line statement — *at cutover we observed N units, as
recorded by the old till* — and every movement after it is real.

### Rejected: back-compute an opening balance

Keep the replay and set `InitialStock = QuantityInStock + Σ units sold`. The net also lands right and
the ledger reconciles against invoice lines.

Rejected because it fabricates an opening entry of ~473,000 units dated at product creation, which no
store ever held, and because legacy records **no** restocks, losses or adjustments — so that single
entry silently absorbs every year of unrecorded stock movement. Starting from a number that was never
maintained and stacking 454k replayed units on top produces a ledger that *looks* authoritative while
being fiction. It also needs a per-product pre-pass and special handling for defect 13's placeholder
products, which have sales but no product to open a balance for.

### Rejected: hybrid recent window

Replay only the last N days against a balance computed at the window start. Less fabrication, but the
same objection applies to the window's opening entry, and it adds a cutoff parameter, a second code
path and a date-boundary edge case for a ledger nobody reads for pre-cutover periods.

## Changes

Three commits, in this order.

### 1. Delete the `Migration:Sale` replay

Remove the `context.InventoryMovements.Add(...)` block at `SqliteMigrationService.cs:333-343`. The
`InvoiceLine` write immediately above it is untouched — the line record stays, only the stock
movement goes.

### 2. Clamp negative legacy stock to zero, and report it

```
if (qty > 0)  emit the movement
else if (qty < 0)  record the clamp, log a warning, write no movement
else  nothing (unchanged)
```

Decided by Pond over carrying negatives forward faithfully: the negative values are a data-quality
artefact of unrecorded restocks, not shelf state, and importing −3,882 would be importing garbage.

A log line alone is not enough. The clamp is a deliberate data change affecting 1,535 real products,
and the operator needs the list to drive a recount — so it goes on `MigrationResult`:

```csharp
public sealed record ClampedStock(string Barcode, string ProductName, int LegacyQuantity);
public IReadOnlyList<ClampedStock> ClampedStocks { get; }
```

**Uncapped**, unlike `AddError`'s 100-per-phase limit. That cap exists because a phase failure can
produce one error per invoice line (up to 325,780 on real data); clamps are bounded by the product
count, so 1,535 records is nothing. It is also not an *error* — the run is a success, `Failed` is not
incremented, and `Outcome` is unaffected.

`Program.cs` `DisplayResults` prints one line after the results table — the count, and the first ten
barcodes with their legacy quantities — following the shape it already uses for `result.Errors`
(`Program.cs:337-344`).

This also makes the behaviour testable: the test suite constructs the service with
`NullLogger<SqliteMigrationService>.Instance`, so nothing can assert on a log line today.

### 3. Date the movement at migration time, not `createdUtc`

Today the movement is stamped with the **product's** `DateCreated`, sometimes years back. Under this
design the quantity means "observed at cutover", so dating it 2021 would have any stock-over-time
report claim the store held today's inventory four years ago.

Capture `DateTime.UtcNow` **once** into a private field at the top of `MigrateAllAsync`, so every
`Migration:InitialStock` movement in a run shares one timestamp. No clock abstraction — a single
captured field is enough to make the run deterministic and the assertion writable.

The `Product` row's own `CreatedUtc` / `LastModifiedUtc` keep mapping from the legacy dates. Only the
movement moves.

## Impact on defect 7's pin — deliberate, not a repair

`ProductMigrationTests.cs:52` (`MigrateProducts_CurrentlyDropsIsTrackable_Defect7`) asserts that a
non-trackable service line still produces **exactly 1** `Migration:Sale` movement of `-1`. Change 1
makes that count 0, so the pin breaks.

**This does not fix defect 7.** Defect 7 is that v4's `Core.Product` has no `IsTrackable`, so the
flag is lost. Two consequences follow from it, and only one is resolved here:

| Consequence | After this change |
|---|---|
| Migration replays a service sale as a stock deduction | Gone — no sale movements are written at all |
| Live v4 sales deduct stock for services (`CompleteSaleCommandHandler`) | **Still broken.** The flag is still dropped |

So the pin is **re-aimed, not repaired**: rewrite it to assert the structural fact that survives —
`Core.Product` exposes no `IsTrackable` property — following the reflection pattern defect 8's pin
already uses (`ProductMigrationTests.cs:116`). Its comment must record that the movement half was
resolved by defect 14 on 2026-08-09 and that the flag half is what remains pinned.

The 29 non-trackable products across the three stores (21 / 7 / 1) are unaffected by the migration
either way once the replay is gone.

## Testing

RED first, per the repo convention. Every pin must be watched failing before the fix lands.

1. **New pin for defect 14** in `ProductMigrationTests.cs`: one product with `QuantityInStock = 20`
   and an invoice line selling 5 of it. `QuantityInStock` is already net of that sale, so **20 is the
   correct net and 15 is what the code produces today**. The pin asserts 15 and records 20 as the
   right answer in its message. Verify it fails once change 1 lands, then invert it to 20.
2. **New test for the clamp**: a product with `QuantityInStock = -5` produces **no** movement, so net
   stock is 0, and `result.ClampedStocks` holds one entry naming that barcode and `-5`. A second
   product with positive stock in the same run must not appear in that list.
3. **New test for the movement date**: the `Migration:InitialStock` movement's `CreatedUtc` is not
   the product's `DateCreated` (2024-03-15) and falls inside the run window.
4. **Re-aim defect 7's pin** as described above, in its own commit, with the comment rewritten.
5. `MigrateProducts_WithStock_ShouldRecordAnInitialStockMovement` (`:150`) stays as-is and must stay
   green — it asserts `QuantityDelta == 20` with no date assertion.
6. **Real run.** Build a fixture from the committed `GeneralHardware.sql` artefact, run the tool
   against `docker run postgres:16-alpine`, and confirm `SUM(QuantityDelta)` per product equals the
   legacy `QuantityInStock` for every product with positive stock, and 0 for every negative one. The
   console and log strings have no automated cover, so this run is the only check on them.

## Out of scope

- **Defect 7's live half** — the missing `IsTrackable` on `Core.Product`. Needs a schema change and
  stays pinned.
- **Defects 5, 6, 8** — unrelated, still pinned.
- **A stock check in `MigrationVerifier`.** It has none today, which is part of why this went
  unnoticed. Worth adding, but it is its own change and this fix is verified by tests plus a real
  run.
- **Recounting the 1,535 clamped products.** An operational task for rollout, driven off the warning
  log.

## Risks

- **The clamp is a deliberate data change.** `ClampedStocks` and the console line make it visible,
  but visible is not the same as acted on — the rollout runbook should capture the list per store
  before the till goes live.
- **Pre-cutover stock movement reports will be empty.** Accepted: they would otherwise be fiction,
  and the sales data behind them is intact in `Invoice` / `InvoiceLine`.
- **Migrated stock is only as good as `QuantityInStock`**, which was never strictly maintained. This
  change makes the migration faithful to the old till, not correct in absolute terms — no migration
  can deliver the latter. The stores recount as they go.
