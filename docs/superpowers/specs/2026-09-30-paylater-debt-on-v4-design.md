# Design — PayLater Debts on v4 Sales

> **Status:** Draft for review · **Date:** 2026-09-30 · **Layer scope:** Application (sale handler,
> one exception) → Infrastructure (sale repository) → StoreHub API (error mapping). No schema change.

## 1. Context & problem

**A credit sale rung on v4 creates no debt.** `CompleteSaleCommandHandler` saves the invoice, its
lines, its payments, the stock movements and the `InvoiceCompleted` outbox event, but never a
`PayLater` row. Only the MigrationTool ever does `new PayLater`
(`src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:668`).

It is not missing data. The till already collects everything the debt needs:
- `AcceptPaymentForm.cs:232` refuses a PayLater payment without a Note ("กรุณาใส่ Note สำหรับการลงบัญชี");
- `StoreHubSaleService.cs:329-332` sends that Note as the payment's `Note`, which is the customer's name.

StoreHub receives it, stores it on the `Payment`, and stops there. So, for every v4 credit sale:
- **the debt is invisible:** `GET /pay-later` and the till's PayLater panel read `pay_later` rows only;
- **it cannot be repaid:** `POST /pay-later/{id}/record-payment` needs the row;
- **it is missing from `/reports/pay-later`**, which also reads `pay_later` (`GetPayLaterReportQueryHandler.cs:31`);
- **the cash drawer expects money that never came in.** The cash-drawer summary takes its PayLater
  totals from `GetLegacySalesSummaryQueryHandler`, and that handler finds credit invoices through
  `pay_later` rows (`:81-86`), not payments. So a v4 credit sale counts as cash sales, and at
  closing the count comes out **short by every credit sale of the day**.

The cash-drawer `DebtRepayment` from #97 does not help. It is a hand-typed cash entry, *deliberately*
not linked to the PayLater row (`DebtRepayment.cs:3-5`).

**This blocks Epic 3 Phase B for GeneralHardware**, the store where PayLater is used (rural credit
culture). From go-live, every credit sale would be lost receivables. The invoice-void spec
(`2026-09-26-invoice-void-design.md`) also assumes these rows exist: void cancels an untouched debt,
and refuses (409) one with a repayment.

## 2. Goals / non-goals

**Goals**
- A v4 PayLater sale creates its debt, **atomically with the sale**, shaped exactly like a migrated
  v3 debt. It shows in the PayLater panel, it can be repaid there, and it appears in the report.
- A sale that would create an unusable debt is refused with a Thai **400**, before anything is saved.
- `/sales/complete` answers a bad request with **400, not 500**. That includes the existing check
  "method not available for this store".

**Non-goals**
- **Backfill.** No store runs v4 in production yet (Phase B not done), so only test data has
  PayLater sales without a debt, and test databases are reset (rollout Rule 1). Decided 2026-09-30.
- **The summary's split-payment rule** (the whole invoice counted as PayLater, see §5). It is
  pre-existing and changed by no one here.
- **A cloud mirror of PayLater debts.** `InvoiceCompleted` already carries the payment's method and
  note, so the cloud can see the credit. A debt mirror (balance, repayments) needs its own spec.
- **`UserId` taken from the request body** on the same route. It belongs to the route tidy-up PR.
- **Storing the catalogue's spelling of a payment code** (a follow-up from invoice-history plan 1).
  This change only has to *recognise* PayLater in any case.

## 3. Decisions (agreed 2026-09-30)

| Question | Decision | Why |
|---|---|---|
| Where is the debt created? | **In `CompleteSaleCommandHandler`, in the same `SaveChangesAsync`** as the invoice | A sale can never exist without its debt. A second step (event, second save) reopens the exact bug on a crash |
| Derive debts from payments instead? | **No** | Migrated v3 debts carry `PaidAmount` progress that payments do not, and the void spec builds on the rows |
| How many debts per sale? | **One per PayLater payment**, 1:1 with its `Payment` | The shape the MigrationTool already writes (`PaymentId` = that payment) |
| Customer name | **The PayLater payment's `Note`, trimmed** | It is what the till collects and what v3 stored |
| Backfill | **None** | Only test data is affected (see non-goals) |
| Bad input | **`SaleValidationException` → 400 Thai `{ error }`**, checked before any save | Validate at the boundary. Today only the WinForms form enforces the Note |
| Existing "method not available" check | **Also becomes `SaleValidationException` → 400** (was an unmapped `InvalidOperationException` → 500) | Same validation block, same route, same fix. It changes existing behaviour on purpose |

## 4. Design

### 4.1 Creating the debt

In `CompleteSaleCommandHandler`, after the payments are built:

```
for each payment where Method equals PaymentMethodCodes.PayLater, ignoring case:
    new PayLater
    {
        Id              = new Guid,
        PaymentId       = payment.Id,
        InvoiceId       = invoice.Id,
        Description     = payment.Note.Trim(),
        PayLaterAmount  = payment.Amount,
        PaidAmount      = 0,
        IsCompleted     = false,
        CreatedUtc      = now,
        LastModifiedUtc = now
    }
```

- **Ignoring case** matters: the handler accepts `paylater` as valid (`CompleteSaleCommandHandler.cs:44`
  uses `OrdinalIgnoreCase`) and stores the caller's spelling. So `Method == "PayLater"` would miss
  a real credit sale.
- The rows go to `ISaleRepository.CompleteSaleAsync`, which gains an `IReadOnlyList<PayLater> payLaters`
  parameter and adds them in the **same** `SaveChangesAsync` as everything else. A sale with no
  PayLater payment passes an empty list.
- `IsCompleted` and `PaidAmount` then change only through the existing `record-payment` path, as for
  a migrated debt.
- **No schema change,** so there is no migration and the forward-only gate is unaffected.
- **No new sync event.** `InvoiceCompleted` is unchanged.

### 4.2 Validation (before anything is saved)

`SaleValidationException` (new, `Application/Common/Exceptions`) carries a Thai message. The payment
checks run first in the handler, where the method check already is, and before any product lookup
or save. All of them throw it:

| Case | Message |
|---|---|
| A method not offerable for this store *(existing check, was `InvalidOperationException`)* | `ช่องทางชำระเงิน '{method}' ใช้กับร้านนี้ไม่ได้` |
| PayLater with no Note, or only whitespace | `กรุณาใส่ชื่อลูกค้าสำหรับการลงบัญชี` |
| PayLater Note over **500** characters, after trimming (`PayLater.Description` is `HasMaxLength(500)`; `Payment.Note` has no limit, so the save would fail with a database error) | `ชื่อลูกค้ายาวเกิน 500 ตัวอักษร` |
| PayLater amount ≤ 0. This also covers refunds: the till hides PayLater on a refund | `ยอดลงบัญชีต้องมากกว่า 0` |

### 4.3 Error mapping

`POST /sales/complete` (`StoreHub/Program.cs:539-553`) catches `SaleValidationException` and returns
`400` with `{ error = ex.Message }`, the shape the `/cash` routes use. Nothing else changes on the
route. A validation failure throws before `CompleteSaleAsync`, so a refused sale writes nothing:
no invoice, no payment, no debt, no stock movement, no outbox event.

## 5. Interactions

- **Invoice void spec:** its PayLater rules (cancel an untouched debt, 409 on a repayment, one
  concurrency token on `PayLater.LastModifiedUtc`) now apply to v4 sales as written. This change
  does not add that token; the void's own plan does.
- **Invoice-history plan 1** also changes `CompleteSaleCommandHandler` (it reserves the bill
  number) and `ISaleRepository`. The edits touch different lines. Whichever lands second rebases
  onto the other; neither changes the other's behaviour.
- **Cash-drawer summary:** its PayLater totals **start including v4 credit sales**, with no code
  change. `GetLegacySalesSummaryQueryHandler` finds credit invoices through `pay_later` rows
  (`:81-86`), so the new rows are what make it right. Its existing rule is unchanged: it counts an
  invoice's *whole* line total as PayLater whenever the invoice has any PayLater, even on a split
  payment. That rule is pre-existing, so it is flagged, not changed here.
- **Till:** unchanged. It already sends the Note, and its own form blocks a PayLater without one, so
  the till reaches the new 400 only if that check is bypassed. When it does, it shows a generic
  "StoreHub request failed: 400 BadRequest" (`StoreHubHttpClient.EnsureSuccessfulResponseAsync`), not
  the Thai `error`. Showing the server's message is till work: cash plan 3 already requires it.

## 6. Testing

Negative-first, `Subject_WhenScenario_DirectVerbOutcome`, one behaviour per test.

**Handler (`CompleteSaleCommandHandlerTests`, Application):**
- A PayLater payment with no Note → `SaleValidationException`.
- …with a whitespace-only Note → `SaleValidationException`.
- …with a Note of 501 characters → `SaleValidationException`. A Note of exactly 500 → accepted
  (boundary).
- …with amount 0 → `SaleValidationException`; …with a negative amount → `SaleValidationException`.
- A method not offerable → `SaleValidationException` (was `InvalidOperationException`; the existing
  test is updated).
- A refused sale passes nothing to `CompleteSaleAsync`.
- A PayLater payment → exactly one `PayLater`, with `PaymentId`, `InvoiceId`, the amount, the
  trimmed Note, `PaidAmount = 0` and `IsCompleted = false`.
- A lower-case `paylater` payment → one `PayLater`.
- A split payment (cash + PayLater) → one `PayLater`, for the PayLater amount only.
- A cash-only sale → no `PayLater`.

**StoreHub integration (real Postgres):**
- **The regression test:** a PayLater sale through `POST /sales/complete` → one `pay_later` row. It
  must fail on `development` before the fix.
- The new debt is listed by `GET /pay-later`, and `POST /pay-later/{id}/record-payment` repays it.
  That is the end-to-end proof that a v4 credit sale behaves like a migrated one.
- A PayLater sale with no Note → `400` with a Thai `error`, and no invoice in the database.
- A method not offerable for the store → `400`, not `500`.
- A v4 PayLater sale today is counted in `GET /cash/summary`'s PayLater total. It must fail on
  `development` before the fix, which proves the drawer shortage is closed.

## 7. Timing & ordering

- **A Phase B prerequisite for GeneralHardware.** It lands before go-live.
- It is independent of invoice-history plans 1 and 2 and of the void. If the void is implemented
  first, its PayLater tests need this change in place.

## 8. Open questions

None. Deferred by decision: backfill, a cloud debt mirror, `UserId` from the body, and the canonical
payment-code spelling (§2).
