# Design — ลงบัญชี status and the daily drawer balance in the cloud

> **Status:** Draft for Pond's review · **Date:** 2026-10-19 · **Layers:** Domain → Application →
> Infrastructure → StoreHub API → MigrationTool → CloudApi (mirror tables, event handlers, a daily view)

## 1. Why

Managers, owners and the AI agents on the MCP server read store data from the cloud. Pond's two
everyday questions, per store, per day, cannot be answered there today:

1. **Does the cash count match what should be in the drawer (ลงตัวมั้ย)?** And, over weeks, the trend:
   which store is often short, how much cash usually sits in the drawer.
2. **Did anyone take ลงบัญชี today, and how much?** And **who still owes what**, including debts
   carried over from v3 that customers are still paying off.

What the cloud has today (surveyed 2026-10-19):

- Every sale (`InvoiceCompleted`), with each payment's method, amount and **note** (for ลงบัญชี, the
  customer's name). So "who took ลงบัญชี today and how much" is already answerable.
- The cash-drawer rows (D2: payouts, floats, debt repayments, counts).
- **Nothing computes the drawer balance**, and the cloud lacks two inputs: the store's time zone and
  the change rule.
- **The debts themselves never leave the store.** A repayment on the ลงบัญชี screen
  (`POST /pay-later/{id}/payments`) only adds to `PayLater.PaidAmount`: no row per repayment, no
  record of who took it, no sync event.
- v3 debts are migrated into StoreHub but not uploaded to the cloud. v3 kept only each debt's totals
  (`PayLaterAmount`, `PaidAmount`, `IsCompleted`), no per-repayment history.

The 2026-09-30 PayLater spec deferred a debt mirror to "its own spec". This is it.

## 2. Goals, non-goals, anti-goals

**Goals**
- **A. Daily drawer balance in the cloud**, per store per day: expected cash, latest count, difference
  (ขาด / เกิน / พอดี), kept for every day so trends can be read.
- **B. ลงบัญชี status in the cloud:** every debt's original amount, paid so far, still owed, completed or
  not, the customer's name and the sale it came from.
- **C. Every repayment on the ลงบัญชี screen is its own record**, at the store and in the cloud: amount,
  time, **which cashier** took it. For audit (Pond: "good for audit").
- **D. v3 debts come too**, open and fully paid, so balances are right from day one and v3 debts still
  being paid off keep being tracked (Pond: "we still need to track unfulfilled debt from v3").

**Non-goals (this design)**
- Stock adjustments, product edits, payment-method changes: **the next design** (stock + products).
- A ปิดการขาย / close-the-day button. Not needed (§3).
- Linking the cash drawer's ลูกค้าชำระหนี้ entry to a debt. Pond: the two are recorded on purpose for two
  jobs (the debt; the drawer's cash); they stay separate.
- Per-repayment history for v3 debts: it does not exist. v3 debts arrive with their totals only.
- Any editing in the cloud. It is a read-only copy.

**Anti-goals (a failure even if it "works")**
- The cloud's expected cash ever disagreeing with the till's for the same day.
- A repayment taken at the store that never reaches the cloud, or is counted twice there.
- The till waiting on the cloud for anything (offline-first: the till must work with the internet down).

## 3. Decisions

**The day** (Pond): stores close around 20:00 and sales never cross midnight, so a store's day is its
calendar day in its own time zone, **ending at midnight**, as the till already defines it. **No close
button**: a past day is already final, because StoreHub refuses edits to an earlier day's cash entries
and only the latest count of a day counts. Today's figures move as data arrives; earlier days do not.

**The drawer balance is computed in the cloud from what is already synced**, never typed or sent again
(as the cash spec §2 says: no persisted expected-vs-actual figures). For each store and day:

- **Cash kept** = for each of the day's invoices, its Cash-method payments minus the change it gave
  (change = payments − total, never below 0, and 0 for a refund) — the till's `SaleFigures` rule.
- **Expected cash** = cash kept + debt repaid (drawer entries) + floats − payouts (non-deleted).
- **Counted** = the latest count of the day; **difference** = counted − expected; none until counted.
- The rules are **the same as StoreHub's** (`CashDrawerCalculator`, D1). A test feeds one set of rows
  to both and asserts equal figures, so they cannot drift.
- **Which day a sale belongs to:** StoreHub already decides it (its store time zone, midnight boundary),
  so **`InvoiceCompleted` gains the sale's `BusinessDate`** (additive; the cash rows already carry
  theirs). The cloud then needs no time zone and can never put a sale on a different day from the till.
  Real stores are not on v4 yet, so every real sale will carry it; a dev invoice without one falls back
  to its UTC date. *(My call, after the builder check: one source for the day, the store.)*
- **Where it lives:** a Postgres **view**, `CashDrawerDays`, with a comment on every column, so
  dashboards and AI agents query one plain table. *(My call; a builder may choose a refreshed table if
  the view is too slow, provided the figures stay derived and identical.)*

**ลงบัญชี at the store** (needed for goal C):
- New StoreHub table `PayLaterRepayment`: `Id`, `StoreId`, `PayLaterId`, `Amount`, `BusinessDate`,
  `CreatedUtc`, `CreatedByUserId`. **Append-only**, never edited or deleted, like cash counts. A wrong
  repayment is out of scope for now (it needs an admin correction, as sale corrections do).
- `POST /pay-later/{id}/payments` takes the acting user from the token (never the body), writes the
  repayment row, updates the debt's `PaidAmount` / `IsCompleted`, and writes the outbox events, **all in
  one transaction**.

**Sync events** (StoreHub outbox, drained as today):
- `PayLaterChanged`: the **full debt row** (a snapshot), written when a debt is created (in the sale's
  own transaction, beside `InvoiceCompleted`) and whenever it changes. Cloud: upsert by `Id`, **newest
  `LastModifiedUtc` wins** (as D2's cash rows).
- `PayLaterRepaid`: the repayment row. Cloud: **insert-only**, once per `Id`.
- A repayment that reaches the cloud before its debt waits and retries (as `InvoiceReprinted` does).

**Cloud mirror:** `PayLaters` (the debts: customer, amounts, paid, owed, completed, the sale, the store,
whether it came from v3) and `PayLaterRepayments`. Every table and column carries a comment an agent can
read; user ids join `Users.Id` (#133/#134); a v3 debt has no `CreatedByUserId`.

**v3 debts** (goal D), every migrated debt, open and completed, with its totals:
- **Through the outbox, not the bulk upload.** The migration writes a `PayLaterChanged` event for each
  migrated debt in StoreHub's outbox, in the migration's own transaction, so the normal sync delivers
  them with its retries. The bulk upload is one-shot and best-effort (a failure is printed, never
  retried), which would quietly lose untouched open debts. *(My call, after the builder check: "never
  lose an event".)*
- **A stable id.** A migrated debt's `Id` is derived from the store and the v3 debt's `PaymentId`
  (a name-based UUID), not `Guid.NewGuid()`, so a store reset and migrated again produces the same ids
  and the cloud updates the debts rather than holding each twice. *(My call, after the builder check.)*
  Only the debt's id is stable: its invoice and payment get new ids on every run, and both runs' snapshots
  carry the same `LastModifiedUtc` (v3's `DateUpdated`). So the cloud's upsert must let an **equal**
  `LastModifiedUtc` win too (`>=`, not `>`), or a re-migrated debt would keep pointing at the first run's
  invoice. *(Review finding on PR 1, for PR 2 to build.)*
- **Marked as v3.** The debt row records that it came from v3 (an additive nullable column carrying the
  v3 `PaymentId`), so the snapshot tells the cloud and the cloud's `FromV3` needs no guess.
- Real stores are not on v4 yet, so this covers every store's history. A later repayment of a v3 debt on
  v4 updates it and adds its own repayment record, exactly like a v4 debt.

## 4. Constraints

- **Offline-first:** every event goes through the outbox; the till never waits for the cloud.
- **Never lose an event:** the cloud already leaves unknown event types pending, so the order of
  deploying the cloud and the stores does not matter.
- **Forward-only migrations:** both new StoreHub objects and every cloud table are additive; each release
  passes the forward-only gate for StoreHub and the cloud (`docs/operations/upgrade-procedure.md`).
- **One store per StoreHub database;** every cloud row carries its `StoreId`.

## 5. Left to the builder

Field names beyond those above; the view's SQL; the name-based UUID scheme; the retry guard's wording;
test layout. Tests should cover,
at least: the cloud-vs-till parity of expected cash (including change, a mixed bill and a refund); a
day boundary in Bangkok (a 23:59 sale stays on its day); a stale debt snapshot never rolling back a newer
one; a repayment arriving before its debt; the same repayment delivered twice; a v3 debt repaid on v4;
migrating the same store twice yields the same debt ids; a migration whose cloud upload is skipped still
delivers every debt.

## 6. Next

The stock and products design (stock adjustments, product edits), agreed with Pond as the next one.
