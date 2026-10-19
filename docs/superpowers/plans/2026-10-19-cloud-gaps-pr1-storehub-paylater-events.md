# Cloud Gaps PR 1 — StoreHub records every ลงบัญชี repayment and sends debts to the outbox — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** StoreHub keeps a record of every repayment taken on the ลงบัญชี screen (amount, time, cashier) and
writes `PayLaterChanged` / `PayLaterRepaid` sync events for every debt created, repaid or migrated from v3,
so PR 2 can mirror them in the cloud.

**Architecture:** A new append-only `PayLaterRepayment` entity and an additive `PayLater.LegacyPaymentId`
column (one StoreHub migration). A `PayLaterOutbox` builder makes the two events from small payload records
that carry `StoreId` (the cloud's ingest check reads the payload's top-level `StoreId`; `PayLater` has none).
The sale, the repayment and the MigrationTool each add their events **in the same save** as the rows they
describe. Migrated debts get a name-based (deterministic) id. Nothing reaches the cloud's tables yet: the
cloud leaves these unknown event types pending until PR 2 handles them, so nothing is lost meanwhile.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql, xUnit + FluentAssertions + Moq, Testcontainers Postgres.

**Spec:** `docs/superpowers/specs/2026-10-19-cloud-paylater-and-drawer-balance-design.md` §3 (ลงบัญชี at the
store, sync events, v3 debts) and §4. **PR 1 of 3**: PR 2 = cloud mirror of debts and repayments; PR 3 =
`BusinessDate` on `InvoiceCompleted` + the cloud's `CashDrawerDays` view. Each later plan is written after the
previous PR merges.

## Global Constraints

- **One transaction** (spec §3, cash spec §7): a row never exists without its event. The sale's debt events
  go in `SaleRepository.CompleteSaleAsync`'s save; the repayment row, the debt update and both events in one
  save; the migrator's events in its own transaction (defect 12's all-or-nothing rule).
- **Repayments are append-only**: no edit, no delete, no soft delete.
- **The acting user comes from the token**, never the body (`user.GetRequiredUserId()` behind
  `RequireUserIdFilter`, as the `/cash` routes do).
- **Payload JSON is PascalCase with a top-level `StoreId`** (default `System.Text.Json`, as `CashDrawerOutbox`);
  a payload without it would make the cloud refuse the store's **whole batch** (403) and stall its outbox.
- **Event types:** `PayLaterChanged` (the debt's full state, a snapshot) and `PayLaterRepaid` (one repayment).
- **Migrated debt id** = name-based UUID (RFC 4122 v5, SHA-1) of `"{storeId}:{legacyPaymentId}"` in a fixed
  namespace; `LegacyPaymentId` = the v3 `PayLater.PaymentId`. A v4 debt keeps `Guid.NewGuid()` and a null
  `LegacyPaymentId`.
- **Forward-only migration** (CLAUDE.md release gate): one new table and one nullable column; run the gate.
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome`, never `Should`; one behaviour per test;
  negative first; named constants; AAA blank lines; dots vertically aligned. Docker for Tasks 1, 3, 4.

## Review Focus

1. **The payload's `StoreId`** is the store's, at the top level, or the cloud refuses the whole batch. →
   Task 2 `Changed_ForADebt_PutsTheStoreIdAtTheTop`.
2. **A refused repayment** (debt already completed, amount ≤ 0) leaves no repayment row and no event. →
   Task 3 `HandleAsync_ForACompletedDebt_WritesNoRepayment`, `…_WritesNoEvent`.
3. **A repayment without a user in the token** is refused (401) and saves nothing. → Task 3
   `RecordPayment_WithoutAUserId_ReturnsUnauthorized`.
4. **Migrating the same v3 store twice** (after a reset) gives the same debt ids. → Task 4
   `Migrate_TwiceIntoFreshDatabases_GivesEachDebtTheSameId`.
5. **A cash-only sale** writes no `PayLaterChanged`. → Task 2 `HandleAsync_WithACashOnlySale_WritesOnlyInvoiceCompleted`.

---

## File Structure

```
src/IndyPOS.Domain/Entities/Core/PayLaterRepayment.cs                 CREATE
src/IndyPOS.Domain/Entities/Core/PayLater.cs                          MODIFY  + int? LegacyPaymentId
src/IndyPOS.Domain/ValueObjects/NameBasedGuid.cs                      CREATE  RFC 4122 v5
src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/PayLaterRepaymentConfiguration.cs  CREATE
src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/PayLaterConfiguration.cs          MODIFY (legacy_payment_id)
src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs  MODIFY  DbSet<PayLaterRepayment>
src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<ts>_AddPayLaterRepayments.cs   CREATE (dotnet ef)
src/IndyPOS.Application/UseCases/StoreHub/PayLater/PayLaterOutbox.cs  CREATE  events + payload records
src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs     MODIFY
src/IndyPOS.Application/Abstractions/StoreHub/Repositories/ISaleRepository.cs + SaleRepository.cs  MODIFY (events list)
src/IndyPOS.Application/UseCases/StoreHub/PayLater/RecordPayLaterPaymentCommand.cs         MODIFY (+UserId, repayment)
src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IPayLaterRepository.cs + PayLaterRepository.cs  MODIFY
src/IndyPOS.StoreHub/Endpoints/PayLater/PayLaterEndpoints.cs          MODIFY  user from token
src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs          MODIFY  MigratePayLaterAsync
docs/operations/upgrade-procedure.md, CLAUDE.md, ONBOARDING.md        MODIFY  gate + counts
```

---

### Task 1: The repayment table and the v3 key

**Interfaces — Produces:**
```csharp
public class PayLaterRepayment   // Domain.Entities.Core
{ Guid Id; string StoreId; Guid PayLaterId; decimal Amount; DateOnly BusinessDate; DateTime CreatedUtc; Guid CreatedByUserId; }
// PayLater: public int? LegacyPaymentId { get; set; }   // the v3 PayLater.PaymentId; null for a v4 debt
public static class NameBasedGuid { public static Guid Create(Guid namespaceId, string name); }   // RFC 4122 v5
// StoreHubDbContext.PayLaterRepayments
```
Table `pay_later_repayment` (snake_case like the cash tables): FK `pay_later_id` → `pay_later` (Restrict),
index `(store_id, business_date)`, `amount` like the other money columns. `pay_later.legacy_payment_id`
nullable, non-unique index.

- [ ] **Step 1: Failing tests.** Domain: `Create_WithTheRfcExample_ReturnsItsUuid` — the RFC 4122 Appendix B
  v5 vector (DNS namespace `6ba7b810-9dad-11d1-80b4-00c04fd430c8`, name `"www.example.com"` →
  `2ed6657d-e927-568b-95e1-2665a8aea6a2`); `Create_WithTheSameName_ReturnsTheSameGuid`;
  `Create_WithDifferentNames_ReturnsDifferentGuids`. StoreHub integration (`PayLaterRepaymentPersistenceTests`):
  `Insert_ForAnUnknownDebt_ThrowsForeignKeyViolation`, `Insert_ForADebt_RoundTripsTheCashier`,
  `LegacyPaymentId_WhenSet_RoundTrips`.
- [ ] **Step 2: Run, expect compile FAIL.** `dotnet test tests/IndyPOS.Domain.Tests` and
  `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~PayLaterRepaymentPersistenceTests"`
- [ ] **Step 3: Implement**, then generate the migration from `src/IndyPOS.Infrastructure`:
  `dotnet ef migrations add AddPayLaterRepayments --startup-project ../IndyPOS.StoreHub --context StoreHubDbContext`.
  Read it: one `CreateTable`, one `AddColumn` (nullable), their indexes, nothing else.
- [ ] **Step 4: PASS** (both commands). **Step 5: Commit** — `feat(storehub): a table for ลงบัญชี repayments, and the v3 key on a debt`

### Task 2: The events, and a credit sale's debt event

**Interfaces — Produces** (`Application/UseCases/StoreHub/PayLater/PayLaterOutbox.cs`):
```csharp
public sealed record PayLaterChangedPayload(Guid Id, string StoreId, Guid PaymentId, Guid InvoiceId, string Description,
    decimal PayLaterAmount, decimal PaidAmount, bool IsCompleted, int? LegacyPaymentId, DateTime CreatedUtc, DateTime LastModifiedUtc);
public sealed record PayLaterRepaidPayload(Guid Id, string StoreId, Guid PayLaterId, decimal Amount, DateOnly BusinessDate,
    DateTime CreatedUtc, Guid CreatedByUserId);
public static class PayLaterOutbox
{
    public const string PayLaterChanged = "PayLaterChanged";
    public const string PayLaterRepaid = "PayLaterRepaid";
    public static OutboxEvent Changed(PayLater debt, string storeId, DateTime utcNow);
    public static OutboxEvent Repaid(PayLaterRepayment repayment, DateTime utcNow);
}
// ISaleRepository.CompleteSaleAsync: OutboxEvent outboxEvent → IReadOnlyList<OutboxEvent> outboxEvents (same save)
```
`CompleteSaleCommandHandler` passes `[invoiceCompleted, ..one PayLaterChanged per new debt]`.

- [ ] **Step 1: Failing tests:** `Changed_ForADebt_PutsTheStoreIdAtTheTop` (parse `PayloadJson`;
  `root.GetProperty("StoreId")` equals the store), `Changed_ForADebt_CarriesTheFullState` (PaidAmount,
  IsCompleted, LegacyPaymentId), `Repaid_ForARepayment_CarriesTheCashier`;
  `HandleAsync_WithACashOnlySale_WritesOnlyInvoiceCompleted`, `HandleAsync_WithACreditSale_WritesItsDebtEvent`
  (capture the events list in the existing `CompleteSaleCommandHandlerTests` mocks: two events, the second
  `PayLaterChanged` for the debt's `Id`). Update the existing mocks to the list parameter.
- [ ] **Step 2: FAIL. Step 3: Implement. Step 4: PASS** — `dotnet test tests/IndyPOS.Application.Tests`;
  `dotnet build` (the StoreHub sale tests use the real repository). **Step 5: Commit** —
  `feat(storehub): a credit sale queues its debt for the cloud`

### Task 3: Every repayment is a record, with its cashier

**Interfaces — Consumes:** Tasks 1–2; `ICashDrawerClock` (store business date). **Produces:**
```csharp
public record RecordPayLaterPaymentCommand(Guid PayLaterId, decimal PaymentAmount, Guid UserId) : ICommand<PayLaterDto>;
// IPayLaterRepository
Task RecordRepaymentAsync(PayLater debt, PayLaterRepayment repayment, IReadOnlyList<OutboxEvent> events, CancellationToken ct);
```
The handler validates as today (feature on, debt found, not completed, amount > 0), then builds the repayment
(`BusinessDate = clock.Now().BusinessDate`, `CreatedUtc = clock.Now().Utc`), updates `PaidAmount` /
`IsCompleted` / `LastModifiedUtc`, and saves the debt, the repayment and `[Repaid, Changed]` in one save.
Route: `.AddEndpointFilter<RequireUserIdFilter>()`, `ClaimsPrincipal user`, `user.GetRequiredUserId()`.

- [ ] **Step 1: Failing tests** (negative first): `HandleAsync_ForACompletedDebt_WritesNoRepayment`,
  `HandleAsync_ForACompletedDebt_WritesNoEvent`, `HandleAsync_WithAZeroAmount_WritesNoRepayment`,
  `HandleAsync_ForAnOpenDebt_RecordsTheCashier`, `HandleAsync_ForAnOpenDebt_FilesItOnTodaysBusinessDate`,
  `HandleAsync_ForAnOpenDebt_WritesARepaidAndAChangedEvent`, `HandleAsync_PayingTheRest_CompletesTheDebt` (existing
  behaviour kept); StoreHub HTTP: `RecordPayment_WithoutAUserId_ReturnsUnauthorized`,
  `RecordPayment_WhenSaved_StoresTheRepaymentUnderTheTokensUser` (a real Postgres row).
- [ ] **Step 2: FAIL. Step 3: Implement. Step 4: PASS** — Application + `--filter "FullyQualifiedName~PayLater"` on
  StoreHub. **Step 5: Commit** — `feat(storehub): each ลงบัญชี repayment is recorded with its cashier and queued for the cloud`

### Task 4: v3 debts — stable ids, the v3 key, and an event each

`MigratePayLaterAsync`: `Id = NameBasedGuid.Create(MigratedDebtNamespace, $"{_options.StoreId}:{payLater.PaymentId}")`,
`LegacyPaymentId = (int)payLater.PaymentId`, and `context.OutboxEvents.Add(PayLaterOutbox.Changed(debt, _options.StoreId, now))`
beside `context.PayLaters.Add` (both skipped in a dry run). `MigratedDebtNamespace` is a fixed `Guid` constant
with a comment saying never to change it (it would re-key every migrated debt in the cloud).

- [ ] **Step 1: Failing tests** (MigrationTool, Postgres fixture; use the suite's existing PayLater fixtures):
  `Migrate_WithAPayLater_QueuesOneEventForIt`, `Migrate_WithAPayLater_RecordsItsV3PaymentId`,
  `Migrate_TwiceIntoFreshDatabases_GivesEachDebtTheSameId`, `Migrate_InADryRun_QueuesNoEvent`,
  `Migrate_WithACompletedPayLater_QueuesItToo`.
- [ ] **Step 2: FAIL. Step 3: Implement. Step 4: PASS** — `dotnet test tests/IndyPOS.MigrationTool.Tests`
  (the real-store tests too, if the gitignored `.db` files are present). **Step 5: Commit** —
  `feat(migration): v3 debts get stable ids and are queued for the cloud`

### Task 5: Gate, docs, counts, PR

- [ ] Run the StoreHub forward-only gate (`docs/operations/upgrade-procedure.md` recipe) for
  `AddPayLaterRepayments`: apply, then write a complete sale **and** a ลงบัญชี repayment's debt update naming
  only the previous release's columns. Record it in the doc and add it to CLAUDE.md's verified list.
- [ ] Full solution with TRX, nothing else running; update CLAUDE.md / ONBOARDING.md counts.
- [ ] Commit `docs: …`; PR (note: the cloud holds these events pending until PR 2); Codex; merge when findings
  are handled and CI is green (PR head == local HEAD).
