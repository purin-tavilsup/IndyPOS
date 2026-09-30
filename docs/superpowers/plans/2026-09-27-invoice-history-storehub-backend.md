# Invoice History — StoreHub Backend Implementation Plan (1 of 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every bill a database-assigned, per-store running bill number (v3 numbers kept), and serve bills from one REST resource `/sales` (list, by id, by number, reprint with an audit row + sync event) with a "today only" rule for cashiers — plus the MigrationTool changes that carry v3 numbers across.

**Architecture:** One additive EF migration adds `invoice.invoice_number` from a Postgres sequence with an ordered backfill written as raw SQL; a second adds the append-only `invoice_reprint` table. The sale handler reserves its number from that same sequence so the `InvoiceCompleted` payload carries it in the one `SaveChangesAsync`. Application holds the pure rules (`TodayOnlyRule`, `SalesQueryRules`, `SaleFigures`) and the reprint command; Infrastructure holds the two EF query handlers; StoreHub maps a `/sales` route group gated by a new `sales.reprint` capability, reusing the `/cash` user-id filter and adding a `SalesExceptionFilter`.

**Tech Stack:** C# .NET 10, EF Core 10 (Npgsql), ASP.NET Core minimal APIs, Nokpirab CQRS (`ICommand`/`IQuery`/handlers), xUnit + FluentAssertions + Moq, EF InMemory (unit), Testcontainers PostgreSQL (integration + migration), `Microsoft.Extensions.Time.Testing` (`FakeTimeProvider`).

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` — read it alongside this plan; the plan argues from it.

**This is plan 1 of 3.** Plan 2 = CloudApi (`cloud_invoice.invoice_number`, `cloud_invoice_reprint`, the `InvoiceReprinted` handler and ordering guard, `BulkMigrationCommandHandler` storing the number) + `IStoreHubClient`/`StoreHubHttpClient` methods. Plan 3 = `IndyPOS.Presentation` ViewModels, `ReceiptDocument`/factory/printer, WinForms screens, removal of the stubbed `StoreHubReportService` members. This plan leaves the till untouched: WinForms keeps working exactly as today.

## Global Constraints

Copied verbatim from the spec (section in brackets); every task's requirements include these.

- [§4] "`invoice.invoice_number   bigint NULL  DEFAULT nextval('invoice_number_seq')` / `UNIQUE INDEX (store_id, invoice_number)` / `SEQUENCE invoice_number_seq  AS bigint`" — **see Deviation D2: the column ends NOT NULL after the backfill.**
- [§4] "**The database assigns every number.** The column default fires for any `INSERT`, including one from binaries restored by an installer rollback, which never mention the column. No C# code picks a number, so two tills selling at once can never clash." — **see Deviation D1.**
- [§4] "Existing rows are backfilled in the same migration, in this order, so the two kinds of number never collide on the unique index: 1. rows with a `legacy_invoice_id` get that number; 2. `setval` moves the sequence past the highest legacy number; 3. the remaining v4-native rows get sequence values in `created_utc` order."
- [§4] "**Domain:** `Invoice.InvoiceNumber` is a `long`, database-generated and never set by application code (`ValueGeneratedOnAdd`)." — **see Deviation D1.**
- [§4] "It writes `invoice_number = LegacyInvoiceId` for each imported v3 invoice" / "**The target must hold no v4-native invoices.**" / "After the invoice phase, **inside the same transaction**, it runs `setval('invoice_number_seq', max(invoice_number))`."
- [§4] "`POST /sales/complete` returns the new `InvoiceNumber`, so the receipt printed right after a sale shows the real number."
- [§4] "**Forward-only gate:** apply the release's schema, then write a complete sale using only the previous release's columns. The row must get an `invoice_number` from the default."
- [§5] "`sales.reprint` (**new**) | Cashier, StoreManager, SystemAdmin | Bills of **today** (store business date) + reprint"
- [§5] "**'Today'** is the server's Bangkok business date, from the same clock as the cash drawer (`ICashDrawerClock`)."
- [§5] "A caller with `sales.reprint` but not `reports.view` is held to today: a list for any other date → `403`; a bill from another day → `404`, so an old number's existence is not revealed."
- [§5] "every `/cash` read whose `businessDate` is not today requires `reports.view` (→ `403`), enforced once for the whole `/cash` group so a future route cannot forget it." *(widened from `/cash/summary` alone in plan review, 2026-09-29)*
- [§5] "the user id comes from the token, never the body; a token without a usable user id → `401`; no capability → `403`; Thai `{ error }` bodies."
- [§6] "`GET /sales?from=&to=&page=&pageSize=` | List bills, newest first | … `from`/`to` default to today's business date; `pageSize` default 50, max 200"
- [§6] "There is still no page total, because the list is not a report."
- [§6] "`GET /reports/invoices` and `GET /reports/invoices/{invoiceId:guid}` are **retired**. … their tests move to `/sales`."
- [§6] "`POST /sales/complete` → `POST /sales` is **not** done here."
- [§6] "table `invoice_reprint`, append-only and immutable, with no update or delete" / "One `SaveChangesAsync` writes the row and an **`InvoiceReprinted`** outbox event." / "a printer failure afterwards leaves the record in place, and pressing reprint again writes a second one."
- [§9] "`MigratedInvoice` (`BulkMigrationCommand.cs:45`) gains a **nullable** `InvoiceNumber` … It is nullable so either side can be older"
- [§10] "Negative-first, `Subject_WhenScenario_DirectVerbOutcome`, one behaviour per test."

Repo constraints (CLAUDE.md, lessons):

- **Branch:** `feat/invoice-history-backend` from `development`.
- **Forward-only migration gate:** additive only; a new `NOT NULL` column only with a default; no renames, drops, type narrowing.
- **Never a bare `BeginTransactionAsync`** on the StoreHub `DbContext` (Aspire registers a retrying execution strategy). This plan needs no explicit transaction in StoreHub: every write is one `SaveChangesAsync`. (The MigrationTool builds its own non-retrying context and keeps its existing transaction.)
- **Error body:** `{ "error": "<Thai message>" }`. The policy-level `403` and the `401` carry no body, exactly like `/cash`.
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome` (`When`/`With` scenario, direct verb outcome, never `Should`). One behaviour per test. Negative tests first; more than half failure/edge/boundary. Named constants for boundary values. Arrange / Act / Assert separated by blank lines. Do not rename existing `Should…` tests.
- **StoreHub integration tests:** `[Collection("Integration")]` + `IntegrationTestBase`. The shared test database is **not** reset between tests — assert only on rows your test created. Test users via `CreateTestUserAsync` (uses `NextLegacyUserId()`). Hand-built tokens only through `LocalTokenOptions` binding (see `BuildToken`, moved to the base class in Task 7).
- **Server "today" in integration tests** is `TimeZoneInfo.Local` (`TestStoreIdentityService.TimeZone`), so tests compute today as `DateOnly.FromDateTime(DateTime.Now)`.
- **Fluent chains:** dots vertically aligned. File-scoped namespaces, nullable enabled, 4-space indent (`ConfigureServices.cs` is tab-indented — match it).
- **Docker must be running** for `IndyPOS.StoreHub.IntegrationTests` and `IndyPOS.MigrationTool.Tests`.

## Review Focus

Inputs the spec implies but does not spell out, most likely to bite first. Each has a pinning test in the task that owns the code:

1. **A swapped date range** (`from=2026-09-27&to=2026-09-01`) — `ReportDateRange.ToUtcRange` throws `ArgumentException`, which would surface as a `500`. Expect `400` with a Thai error. → Task 6 (`EnsureValidRange_WithToBeforeFrom_Throws`) + Task 7 (`ListSales_WithToBeforeFrom_ReturnsBadRequest`).
2. **Paging values out of range** (`page=0`, `pageSize=0`, `pageSize=10000`, `page=2147483647`) — a negative `OFFSET` is a Npgsql `500`, an unbounded page reads the whole table, and `(page-1)*pageSize` overflows. Expect `400`. → Task 6 (`EnsureValidPage_WithPageZero_Throws`, `…_WithPageSizeAboveMaximum_Throws`, `…_WithPageBeyondAddressableRows_Throws`) + Task 7 (`ListSales_WithPageSizeAboveMaximum_ReturnsBadRequest`).
3. **A bill sold just after Bangkok midnight** (00:05 local = 17:05 UTC the day before) — a UTC-date comparison would call it "yesterday" and hide it from the cashier who just sold it. Expect it visible as today. → Task 5 (`BusinessDateOf_WithUtcEveningAfterBangkokMidnight_ReturnsTheNextDay`) + Task 6 (`HandleById_AsTodayOnlyCallerForABillJustAfterBangkokMidnight_ReturnsIt`).
4. **A bill number that is not a positive `long`** (`/sales/abc`, `/sales/0`, `/sales/-3`, `/sales/99999999999999999999`) — typed route constraints make a non-match a `404`, not the spec's `400`. Expect `400` with a Thai error. → Task 6 (`EnsureValidNumber_WithZero_Throws`) + Task 7 (`GetSaleByNumber_WithNonNumericValue_ReturnsBadRequest`, `GetSaleByNumber_WithOverflowingValue_ReturnsBadRequest`, `GetSaleByNumber_WithNegativeNumber_ReturnsBadRequest`).
5. **A migrated bill whose cashier, product category or payment method is missing from v4's tables** (deleted user, defect-13 placeholder product with no category, retired campaign code not in the catalogue) — a strict join would drop lines or `404` the bill. Expect `200` with `CashierName = null`, `CategoryKind = null`, and the method code as its display name. → Task 6 (`HandleById_WithAnUnknownCashier_ReturnsNullCashierName`, `HandleById_WithALineWithoutACategory_ReturnsNullKind`, `HandleById_WithAMethodMissingFromTheCatalogue_UsesTheCodeAsDisplayName`).

---

## File Structure

```
src/IndyPOS.Domain/
  Entities/Core/Invoice.cs                                   MOD  + long InvoiceNumber
  Entities/Core/InvoiceReprint.cs                            NEW  append-only audit row

src/IndyPOS.Application/
  Abstractions/StoreHub/Repositories/ISaleRepository.cs      MOD  + ReserveInvoiceNumberAsync
  Abstractions/StoreHub/Repositories/IInvoiceReprintRepository.cs NEW
  Common/Authorization/Capability.cs                         MOD  + SalesReprint
  Common/Authorization/RoleCapabilities.cs                   MOD  grant to all three roles
  Common/Authorization/TodayOnlyRule.cs                      NEW  "today's figures yes, other days no"
  Common/Exceptions/OtherDayForbiddenException.cs            NEW  -> 403
  Common/Exceptions/SaleNotFoundException.cs                 NEW  -> 404
  Common/Exceptions/SalesQueryValidationException.cs         NEW  -> 400
  UseCases/Cloud/Sync/Events/InvoiceCompletedEvent.cs        MOD  + long? InvoiceNumber
  UseCases/Cloud/Sync/Events/InvoiceReprintedEvent.cs        NEW
  UseCases/Cloud/Sync/BulkMigration/BulkMigrationCommand.cs  MOD  MigratedInvoice + long? InvoiceNumber
  UseCases/StoreHub/Sales/CompleteSaleResponse.cs            MOD  + long InvoiceNumber
  UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs MOD reserve + stamp + payload
  UseCases/StoreHub/Sales/History/SaleDtos.cs                NEW  (moved from ReportDtos + additions)
  UseCases/StoreHub/Sales/History/SaleFigures.cs             NEW  received / change / refund / PayLater
  UseCases/StoreHub/Sales/History/SalesQueryRules.cs         NEW  date / range / page / number rules
  UseCases/StoreHub/Sales/History/ListSalesQuery.cs, GetSaleByIdQuery.cs, GetSaleByNumberQuery.cs NEW
  UseCases/StoreHub/Sales/Reprints/CreateInvoiceReprintCommand.cs + Handler, InvoiceReprintDtos.cs, InvoiceReprintOutbox.cs NEW
  UseCases/StoreHub/Reports/ReportDtos.cs                    MOD  - 4 invoice DTOs (moved)
  UseCases/StoreHub/Reports/GetInvoices/, GetInvoiceDetail/  DEL  retired

src/IndyPOS.Infrastructure/
  Persistence/StoreHub/InvoiceNumberSequence.cs              NEW  the sequence name, once
  Persistence/StoreHub/StoreHubDbContext.cs                  MOD  HasSequence + DbSet<InvoiceReprint>
  Persistence/StoreHub/Configurations/InvoiceConfiguration.cs MOD invoice_number + unique index
  Persistence/StoreHub/Configurations/InvoiceReprintConfiguration.cs NEW
  Persistence/StoreHub/Repositories/SaleRepository.cs        MOD  ReserveInvoiceNumberAsync
  Persistence/StoreHub/Repositories/InvoiceReprintRepository.cs NEW
  Persistence/StoreHub/Migrations/<ts>_AddInvoiceNumber.*    NEW  generated, Up hand-written
  Persistence/StoreHub/Migrations/<ts>_AddInvoiceReprintTable.* NEW generated
  QueryHandlers/Sales/ListSalesQueryHandler.cs               NEW
  QueryHandlers/Sales/GetSaleQueryHandler.cs                 NEW  by id + by number
  QueryHandlers/Reports/GetInvoicesQueryHandler.cs, GetInvoiceDetailQueryHandler.cs DEL retired
  ConfigureServices.cs                                       MOD  register reprint repository

src/IndyPOS.StoreHub/
  Endpoints/Cash/ClaimsPrincipalExtensions.cs                MOD  + HasCapability
  Endpoints/Cash/CashEndpoints.cs                            MOD  group gains TodayOnlyBusinessDateFilter
  Endpoints/Cash/CashExceptionFilter.cs                      MOD  OtherDayForbiddenException -> 403
  Endpoints/Cash/TodayOnlyBusinessDateFilter.cs              NEW  past businessDate needs reports.view
  Endpoints/Sales/SalesEndpoints.cs                          NEW  group, policy, filters
  Endpoints/Sales/SaleQueryEndpoints.cs                      NEW  list / by id / by number / malformed
  Endpoints/Sales/SaleReprintEndpoints.cs                    NEW  POST /sales/{id}/reprints
  Endpoints/Sales/SalesExceptionFilter.cs                    NEW
  Endpoints/Sales/SalesServiceCollectionExtensions.cs        NEW  AddSalesHistory()
  Program.cs                                                 MOD  policy, registrations, retire routes

src/IndyPOS.MigrationTool/
  Services/SqliteMigrationService.cs                         MOD  number, native refusal, setval, bulk field
  Services/MigrationVerifier.cs                              MOD  two checks
  IndyPOS.MigrationTool.csproj                               MOD  InternalsVisibleTo tests

tests/ (see each task)
```

---

### Task 1: The bill number column — sequence, ordered backfill, unique per store

**Files:**
- Modify: `src/IndyPOS.Domain/Entities/Core/Invoice.cs:26` (add property after `LegacyInvoiceId`)
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/InvoiceNumberSequence.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/InvoiceConfiguration.cs:41-47`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs:32-35`
- Generate: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<timestamp>_AddInvoiceNumber.cs` (+ `.Designer.cs`, snapshot) — `Up`/`Down` hand-written
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/IntegrationTestBase.cs` (add `SeedInvoiceAsync`)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/InvoiceNumberPersistenceTests.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Migrations/AddInvoiceNumberMigrationTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `Invoice.InvoiceNumber` — `public long InvoiceNumber { get; set; }`; `0` = not yet assigned (EF omits the column, the default fills it).
  - `IndyPOS.Infrastructure.Persistence.StoreHub.InvoiceNumberSequence.Name` — `public const string Name = "invoice_number_seq";`
  - Column `invoice.invoice_number bigint NOT NULL DEFAULT nextval('invoice_number_seq')`, unique index `IX_invoice_store_id_invoice_number` on `(store_id, invoice_number)`.
  - `IntegrationTestBase.SeedInvoiceAsync(DateTime createdUtc, decimal totalAmount = 350m, Guid? userId = null) : Task<Invoice>` — writes an invoice with no lines/payments for `test-store`; returns it with the database-assigned `InvoiceNumber`.

- [ ] **Step 1: Add the `SeedInvoiceAsync` helper to the test base**

In `tests/IndyPOS.StoreHub.IntegrationTests/IntegrationTestBase.cs`, add after `GetProductStockAsync`:

```csharp
    /// <summary>
    /// Writes an invoice straight to the database — no lines, no payments — leaving
    /// <see cref="Invoice.InvoiceNumber"/> at 0 so the column default assigns it. For bills on
    /// another day, which the sale endpoint cannot create.
    /// </summary>
    protected async Task<Invoice> SeedInvoiceAsync(DateTime createdUtc, decimal totalAmount = 350m, Guid? userId = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = TestStoreIdentityService.TestStoreId,
            UserId = userId ?? Guid.NewGuid(),
            TotalAmount = totalAmount,
            CreatedUtc = createdUtc,
            LastModifiedUtc = createdUtc
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return invoice;
    }
```

- [ ] **Step 2: Write the failing persistence tests**

`tests/IndyPOS.StoreHub.IntegrationTests/InvoiceNumberPersistenceTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Real-PostgreSQL checks for invoice.invoice_number: the default fills every INSERT (including
/// one that never names the column), and the number is unique per store, not per database.
/// </summary>
[Collection("Integration")]
public class InvoiceNumberPersistenceTests : IntegrationTestBase
{
    private const string OtherStoreId = "other-store";

    /// <summary>Explicit numbers far above anything the sequence reaches in a test run.</summary>
    private static long _lastExplicitNumber = 900_000_000;

    public InvoiceNumberPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static long NextExplicitNumber() => Interlocked.Increment(ref _lastExplicitNumber);

    private static Invoice NewInvoice(string storeId, long invoiceNumber) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        UserId = Guid.NewGuid(),
        TotalAmount = 10m,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow,
        InvoiceNumber = invoiceNumber
    };

    private async Task SaveAsync(Invoice invoice)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Insert_WithADuplicateNumberInTheSameStore_ThrowsUniqueViolation()
    {
        var number = NextExplicitNumber();
        await SaveAsync(NewInvoice(TestStoreIdentityService.TestStoreId, number));

        var act = () => SaveAsync(NewInvoice(TestStoreIdentityService.TestStoreId, number));

        (await act.Should()
                  .ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should()
                           .Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Insert_WithTheSameNumberInAnotherStore_Succeeds()
    {
        var number = NextExplicitNumber();
        await SaveAsync(NewInvoice(TestStoreIdentityService.TestStoreId, number));

        var act = () => SaveAsync(NewInvoice(OtherStoreId, number));

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task Insert_WithOnlyPreReleaseColumns_GetsANumberFromTheDefault()
    {
        // The forward-only gate in code form: the previous release's binaries INSERT an invoice
        // without ever naming invoice_number, and the row must still get one.
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc)
            VALUES (gen_random_uuid(), 'test-store', gen_random_uuid(), 10, now(), now())
            RETURNING invoice_number
            """, connection);

        var number = (long)(await command.ExecuteScalarAsync())!;

        number.Should()
              .BePositive();
    }

    [Fact]
    public async Task Insert_ThroughEfWithoutANumber_ReadsBackTheDatabaseNumber()
    {
        var invoice = await SeedInvoiceAsync(DateTime.UtcNow);

        invoice.InvoiceNumber.Should()
                             .BePositive();
    }

    [Fact]
    public async Task Insert_TwiceWithoutANumber_AssignsAHigherNumberSecond()
    {
        var first = await SeedInvoiceAsync(DateTime.UtcNow);

        var second = await SeedInvoiceAsync(DateTime.UtcNow);

        second.InvoiceNumber.Should()
                            .BeGreaterThan(first.InvoiceNumber);
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~InvoiceNumberPersistenceTests"`
Expected: build FAILS with `CS0117: 'Invoice' does not contain a definition for 'InvoiceNumber'`.

- [ ] **Step 4: Add the domain property**

In `src/IndyPOS.Domain/Entities/Core/Invoice.cs`, after `public int? LegacyInvoiceId { get; set; }` (line 26):

```csharp

    /// <summary>
    /// The bill number printed on the receipt: per store, running, never reused. Assigned by the
    /// database from <c>invoice_number_seq</c>. A v3 bill keeps its v3 number and v4 carries on
    /// after the store's last one.
    /// </summary>
    /// <remarks>
    /// 0 means "not assigned yet": EF then leaves the column out of the INSERT and the column
    /// default fills it. The same default is what lets the previous release's binaries, which never
    /// mention the column, keep writing sales after an installer rollback.
    /// </remarks>
    public long InvoiceNumber { get; set; }
```

- [ ] **Step 5: Add the sequence name constant**

`src/IndyPOS.Infrastructure/Persistence/StoreHub/InvoiceNumberSequence.cs`:

```csharp
namespace IndyPOS.Infrastructure.Persistence.StoreHub;

/// <summary>
/// The one Postgres sequence every bill number comes from: the column default, the sale handler's
/// reservation, the migration backfill and the MigrationTool's setval all name it through here.
/// </summary>
public static class InvoiceNumberSequence
{
    public const string Name = "invoice_number_seq";
}
```

- [ ] **Step 6: Map the column, the unique index and the sequence**

In `InvoiceConfiguration.cs`, replace lines 41-47 (the `LegacyInvoiceId` property and its index) with:

```csharp
        builder.Property(e => e.LegacyInvoiceId)
            .HasColumnName("legacy_invoice_id");

        // The database assigns every bill number from one sequence, so two tills selling at once
        // can never clash and restored older binaries still get a number (the forward-only gate).
        builder.Property(e => e.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasDefaultValueSql($"nextval('{InvoiceNumberSequence.Name}')")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // Defect 8, scoped per store for the same reason as Product's: GeneralHardware invoices run
        // 79..139,758 and MimyMart's 67,994..165,286, which overlap.
        builder.HasIndex(e => new { e.StoreId, e.LegacyInvoiceId })
            .IsUnique();

        // Per store, like the legacy id: legacy numbers overlap across stores.
        builder.HasIndex(e => new { e.StoreId, e.InvoiceNumber })
            .IsUnique();
```

In `StoreHubDbContext.cs`, replace `OnModelCreating` (lines 32-35) with:

```csharp
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(InvoiceNumberSequence.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreHubDbContext).Assembly);
    }
```

- [ ] **Step 7: Run the persistence tests to verify they pass**

The integration host builds its schema with `EnsureCreatedAsync` from the model, so the sequence, default and index exist without the migration.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~InvoiceNumberPersistenceTests"`
Expected: PASS (5 tests).

- [ ] **Step 8: Write the failing migration tests**

`tests/IndyPOS.StoreHub.IntegrationTests/Migrations/AddInvoiceNumberMigrationTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Migrations;

/// <summary>
/// Exercises AddInvoiceNumber's backfill against a real database that already holds both kinds of
/// invoice. IntegrationTestBase uses EnsureCreated, which never runs a migration, so this is the only
/// coverage of the backfill order: legacy ids, then setval, then native rows by created_utc.
/// </summary>
public class AddInvoiceNumberMigrationTests
    : IClassFixture<AddInvoiceNumberMigrationTests.BackfilledInvoicesFixture>
{
    private const long HighestLegacyNumber = 7005;

    private readonly BackfilledInvoicesFixture _fixture;

    public AddInvoiceNumberMigrationTests(BackfilledInvoicesFixture fixture) => _fixture = fixture;

    private long? NumberOf(Guid id) => _fixture.Rows[id].InvoiceNumber;

    [Fact]
    public void Migrate_WithBothKinds_LeavesNoInvoiceWithoutANumber()
    {
        _fixture.Rows.Values.Should()
                            .OnlyContain(row => row.InvoiceNumber != null);
    }

    [Fact]
    public void Migrate_WithBothKinds_LeavesNoDuplicateNumberInAStore()
    {
        _fixture.Rows.Values.GroupBy(row => (row.StoreId, row.InvoiceNumber))
                            .Should()
                            .OnlyContain(group => group.Count() == 1);
    }

    [Fact]
    public void Migrate_WithNativeInvoicesInsertedNewestFirst_NumbersTheOlderOneFirst()
    {
        // The newer row was INSERTed first, so physical order alone would number it first.
        NumberOf(BackfilledInvoicesFixture.OlderNative).Should()
                                                       .Be(HighestLegacyNumber + 1);
    }

    [Fact]
    public void Migrate_WithNativeInvoices_NumbersTheNewerOneAfterTheOlder()
    {
        NumberOf(BackfilledInvoicesFixture.NewerNative).Should()
                                                       .Be(HighestLegacyNumber + 2);
    }

    [Fact]
    public void Migrate_WithBothKinds_MovesTheSequencePastTheMaximum()
    {
        _fixture.NextValue.Should()
                          .Be(HighestLegacyNumber + 3);
    }

    [Fact]
    public void Migrate_WithALegacyInvoice_KeepsItsV3Number()
    {
        NumberOf(BackfilledInvoicesFixture.Legacy7001StoreA).Should()
                                                            .Be(7001);
    }

    [Fact]
    public void Migrate_WithTheSameLegacyIdInAnotherStore_KeepsItsV3NumberThereToo()
    {
        NumberOf(BackfilledInvoicesFixture.Legacy7001StoreB).Should()
                                                            .Be(7001);
    }

    [Fact]
    public void Migrate_OnAnEmptyInvoiceTable_StartsNumberingAtOne()
    {
        // setval must be skipped when there is no maximum: setval(seq, NULL) would fail the migration.
        _fixture.FirstNumberOnEmptyTable.Should()
                                        .Be(1);
    }

    /// <summary>
    /// Migrates one database to the revision before AddInvoiceNumber, plants legacy and native
    /// invoices, then migrates to latest. A second, empty database proves the empty-table path.
    /// </summary>
    public sealed class BackfilledInvoicesFixture : IAsyncLifetime
    {
        /// <summary>The last migration of the previous (cash-drawer) release.</summary>
        private const string MigrationBeforeInvoiceNumber = "20260926152602_AddCashDrawerTables";

        private const string EmptyDatabase = "storehub_empty";

        private const string NextValueSql =
            $"SELECT CASE WHEN is_called THEN last_value + 1 ELSE last_value END FROM {InvoiceNumberSequence.Name}";

        public static readonly Guid Legacy7001StoreA = Guid.Parse("10000000-0000-0000-0000-000000007001");
        public static readonly Guid Legacy7005StoreA = Guid.Parse("10000000-0000-0000-0000-000000007005");
        public static readonly Guid Legacy7001StoreB = Guid.Parse("20000000-0000-0000-0000-000000007001");
        public static readonly Guid OlderNative = Guid.Parse("30000000-0000-0000-0000-000000000001");
        public static readonly Guid NewerNative = Guid.Parse("30000000-0000-0000-0000-000000000002");

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("storehub_backfill_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        public Dictionary<Guid, (string StoreId, long? InvoiceNumber)> Rows { get; } = new();

        public long NextValue { get; private set; }

        public long FirstNumberOnEmptyTable { get; private set; }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            var connectionString = _postgres.GetConnectionString();

            await using (var context = NewContext(connectionString))
            {
                var migrator = context.GetService<IMigrator>();
                await migrator.MigrateAsync(MigrationBeforeInvoiceNumber);
                await context.Database.ExecuteSqlRawAsync(PlantedInvoicesSql);
                await migrator.MigrateAsync();
            }

            await LoadRowsAsync(connectionString);
            NextValue = await ScalarAsync<long>(connectionString, NextValueSql);
            FirstNumberOnEmptyTable = await MigrateEmptyDatabaseAndInsertAsync(connectionString);
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        /// <summary>
        /// Pre-release columns only (no invoice_number yet). The newer native row goes in FIRST, so
        /// a backfill that numbered by physical order instead of created_utc would fail the tests.
        /// </summary>
        private static string PlantedInvoicesSql => $"""
            INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc, legacy_invoice_id)
            VALUES
              ('{Legacy7001StoreA}', 'store-a', gen_random_uuid(), 120, '2024-03-15 07:30:00+00', '2024-03-15 07:30:00+00', 7001),
              ('{Legacy7005StoreA}', 'store-a', gen_random_uuid(),  80, '2024-03-16 07:30:00+00', '2024-03-16 07:30:00+00', 7005),
              ('{Legacy7001StoreB}', 'store-b', gen_random_uuid(),  60, '2024-03-15 08:00:00+00', '2024-03-15 08:00:00+00', 7001),
              ('{NewerNative}',      'store-a', gen_random_uuid(),  50, '2026-09-25 03:00:00+00', '2026-09-25 03:00:00+00', NULL),
              ('{OlderNative}',      'store-a', gen_random_uuid(),  40, '2026-09-20 03:00:00+00', '2026-09-20 03:00:00+00', NULL);
            """;

        private static StoreHubDbContext NewContext(string connectionString) =>
            new(new DbContextOptionsBuilder<StoreHubDbContext>()
                .UseNpgsql(connectionString)
                .Options);

        private async Task LoadRowsAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT id, store_id, invoice_number FROM invoice", connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Rows[reader.GetGuid(0)] = (reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt64(2));
            }
        }

        private static async Task<long> MigrateEmptyDatabaseAndInsertAsync(string connectionString)
        {
            await ScalarAsync<object>(connectionString, $"CREATE DATABASE {EmptyDatabase}");
            var emptyConnectionString = new NpgsqlConnectionStringBuilder(connectionString) { Database = EmptyDatabase }.ConnectionString;

            await using (var context = NewContext(emptyConnectionString))
            {
                await context.Database.MigrateAsync();
            }

            return await ScalarAsync<long>(emptyConnectionString, """
                INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc)
                VALUES (gen_random_uuid(), 'store-a', gen_random_uuid(), 10, now(), now())
                RETURNING invoice_number
                """);
        }

        private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            return (T)(await command.ExecuteScalarAsync())!;
        }
    }
}
```

Note: `ScalarAsync<object>` on `CREATE DATABASE` returns `null`; the null-forgiving cast to `object` is fine because the value is discarded. If your compiler complains, replace that one call with an `ExecuteNonQueryAsync` on a fresh `NpgsqlCommand`.

- [ ] **Step 9: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~AddInvoiceNumberMigrationTests"`
Expected: FAIL — `KeyNotFoundException`/`PostgresException: column "invoice_number" does not exist`, because no migration adds the column yet.

- [ ] **Step 10: Generate the migration**

Run:
```bash
dotnet ef migrations add AddInvoiceNumber --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --output-dir Persistence/StoreHub/Migrations
```
Expected: a new `<timestamp>_AddInvoiceNumber.cs`, its `.Designer.cs`, and a snapshot change adding `HasSequence("invoice_number_seq")`, the `InvoiceNumber` property (`bigint`, `HasDefaultValueSql("nextval('invoice_number_seq')")`) and the `IX_invoice_store_id_invoice_number` unique index. Leave the Designer and snapshot as generated.

- [ ] **Step 11: Replace the generated `Up`/`Down` with the ordered backfill**

EF generates `CreateSequence` + `AddColumn(nullable: false, defaultValueSql: …)` + `CreateIndex`. That is wrong here: adding a `NOT NULL DEFAULT nextval()` column fills existing rows in physical order **before** the legacy numbers are written, so native rows would take numbers that legacy rows then collide with. Replace the whole class body with:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Migrations
{
    /// <summary>
    /// Adds the bill number. Hand-written rather than generated because the ORDER is the point
    /// (spec §4): legacy rows keep their v3 number, setval moves the sequence past the highest one,
    /// and only then do v4-native rows draw sequence values, oldest first. The default and NOT NULL
    /// are attached last, once every existing row has a number.
    /// </summary>
    /// <remarks>
    /// Forward-only: the column is NOT NULL but has a default, so the previous release's INSERTs,
    /// which never name it, still succeed after an installer rollback.
    /// </remarks>
    public partial class AddInvoiceNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SEQUENCE invoice_number_seq AS bigint START WITH 1 INCREMENT BY 1 NO CYCLE;");

            migrationBuilder.Sql("ALTER TABLE invoice ADD COLUMN invoice_number bigint NULL;");

            // 1. v3 bills keep their v3 number.
            migrationBuilder.Sql("UPDATE invoice SET invoice_number = legacy_invoice_id WHERE legacy_invoice_id IS NOT NULL;");

            // 2. Move the sequence past the highest number. HAVING skips the call on an empty
            //    table, where MAX is NULL and setval would fail.
            migrationBuilder.Sql("SELECT setval('invoice_number_seq', MAX(invoice_number)) FROM invoice HAVING MAX(invoice_number) IS NOT NULL;");

            // 3. v4-native rows, oldest first. A loop, because the order nextval() is evaluated in
            //    an UPDATE ... FROM is not guaranteed. Native rows are few (no till runs v4 yet).
            migrationBuilder.Sql("""
                DO $$
                DECLARE native record;
                BEGIN
                    FOR native IN SELECT id FROM invoice WHERE invoice_number IS NULL ORDER BY created_utc, id LOOP
                        UPDATE invoice SET invoice_number = nextval('invoice_number_seq') WHERE id = native.id;
                    END LOOP;
                END $$;
                """);

            migrationBuilder.Sql("ALTER TABLE invoice ALTER COLUMN invoice_number SET DEFAULT nextval('invoice_number_seq');");
            migrationBuilder.Sql("ALTER TABLE invoice ALTER COLUMN invoice_number SET NOT NULL;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_invoice_store_id_invoice_number\" ON invoice (store_id, invoice_number);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoice_store_id_invoice_number",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "invoice_number",
                table: "invoice");

            migrationBuilder.DropSequence(
                name: "invoice_number_seq");
        }
    }
}
```

- [ ] **Step 12: Confirm the model and snapshot agree**

Run: `dotnet ef migrations has-pending-model-changes --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext`
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 13: Run the migration and persistence tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~AddInvoiceNumberMigrationTests|FullyQualifiedName~InvoiceNumberPersistenceTests|FullyQualifiedName~ReclassifyPaymentMethodKindsMigrationTests"`
Expected: PASS (8 + 5 + the existing reclassify tests, which also migrate to latest).

- [ ] **Step 14: Commit**

```bash
git add src/IndyPOS.Domain/Entities/Core/Invoice.cs src/IndyPOS.Infrastructure/Persistence/StoreHub tests/IndyPOS.StoreHub.IntegrationTests/IntegrationTestBase.cs tests/IndyPOS.StoreHub.IntegrationTests/InvoiceNumberPersistenceTests.cs tests/IndyPOS.StoreHub.IntegrationTests/Migrations/AddInvoiceNumberMigrationTests.cs
git commit -m "feat(storehub): add database-assigned invoice_number with ordered backfill"
```

---

### Task 2: A sale gets its bill number — response and `InvoiceCompleted` payload

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/ISaleRepository.cs` (add method)
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/SaleRepository.cs` (add method)
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/CompleteSaleResponse.cs`
- Modify: `src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceCompletedEvent.cs:22-24`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Complete/CompleteSaleCommandHandler.cs:66-78, 143-150, 199-202`
- Modify: `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubE2ETests.cs:207-210`, `tests/IndyPOS.Application.Tests/Integration/StoreHub/StoreHubHttpClientTests.cs:139-142` (add the new record argument)
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleInvoiceNumberTests.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs` (add 4 tests)

**Interfaces:**
- Consumes: Task 1 `Invoice.InvoiceNumber`, `InvoiceNumberSequence.Name`.
- Produces:
  - `ISaleRepository.ReserveInvoiceNumberAsync(CancellationToken cancellationToken = default) : Task<long>` — `nextval` from the invoice sequence.
  - `CompleteSaleResponse(Guid InvoiceId, decimal TotalAmount, DateTime CreatedUtc, long InvoiceNumber)`.
  - `InvoiceCompletedEvent.InvoiceNumber : long?` (null only in events queued before this release).

- [ ] **Step 1: Write the failing unit tests**

`tests/IndyPOS.Application.Tests/StoreHub/Sales/Commands/CompleteSaleInvoiceNumberTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Mock;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Sales.Commands;

public class CompleteSaleInvoiceNumberTests
{
    private const long ReservedNumber = 139_759;
    private const string StoreId = "STORE-001";
    private const string UnofferableMethod = "Bitcoin";

    private readonly Mock<ISaleRepository> _sales = new();
    private readonly Mock<IPaymentMethodCatalogService> _catalog = new();
    private Invoice? _savedInvoice;
    private OutboxEvent? _savedEvent;

    public CompleteSaleInvoiceNumberTests()
    {
        _sales.Setup(s => s.ReserveInvoiceNumberAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(ReservedNumber);

        _sales.Setup(s => s.CompleteSaleAsync(
                  It.IsAny<Invoice>(),
                  It.IsAny<IReadOnlyList<InvoiceLine>>(),
                  It.IsAny<IReadOnlyList<Payment>>(),
                  It.IsAny<IReadOnlyList<InventoryMovement>>(),
                  It.IsAny<OutboxEvent>(),
                  It.IsAny<CancellationToken>()))
              .Callback((Invoice invoice, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                         IReadOnlyList<InventoryMovement> _, OutboxEvent outboxEvent, CancellationToken _) =>
              {
                  _savedInvoice = invoice;
                  _savedEvent = outboxEvent;
              })
              .ReturnsAsync((Invoice invoice, IReadOnlyList<InvoiceLine> _, IReadOnlyList<Payment> _,
                             IReadOnlyList<InventoryMovement> _, OutboxEvent _, CancellationToken _) => invoice);

        _catalog.Setup(c => c.GetOfferableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([new PaymentMethod { Code = "Cash", DisplayName = "Cash", IsEnabled = true, StoreId = StoreId }]);
    }

    private CompleteSaleCommandHandler Handler() => new(
        _sales.Object,
        new Mock<IProductRepository>().Object,
        MockStoreIdentityService.GeneralHardware(),
        _catalog.Object,
        NullLogger<CompleteSaleCommandHandler>.Instance);

    private static CompleteSaleCommand CashSale(string method = "Cash") =>
        new(StoreId, Guid.NewGuid(), Lines: [], Payments: [new SalePaymentRequest(method, 0m)]);

    [Fact]
    public async Task HandleAsync_WhenPaymentMethodNotOfferable_DoesNotReserveANumber()
    {
        try { await Handler().HandleAsync(CashSale(UnofferableMethod)); }
        catch (InvalidOperationException) { }

        _sales.Verify(s => s.ReserveInvoiceNumberAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithAReservedNumber_StampsItOnTheInvoice()
    {
        await Handler().HandleAsync(CashSale());

        _savedInvoice!.InvoiceNumber.Should()
                                    .Be(ReservedNumber);
    }

    [Fact]
    public async Task HandleAsync_WithAReservedNumber_CarriesItInTheInvoiceCompletedPayload()
    {
        await Handler().HandleAsync(CashSale());

        JsonSerializer.Deserialize<InvoiceCompletedEvent>(_savedEvent!.PayloadJson)!
                      .InvoiceNumber.Should()
                                    .Be(ReservedNumber);
    }

    [Fact]
    public async Task HandleAsync_WithAReservedNumber_ReturnsIt()
    {
        var response = await Handler().HandleAsync(CashSale());

        response.InvoiceNumber.Should()
                              .Be(ReservedNumber);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CompleteSaleInvoiceNumberTests"`
Expected: build FAILS — `ISaleRepository` has no `ReserveInvoiceNumberAsync`, `CompleteSaleResponse` has no `InvoiceNumber`.

- [ ] **Step 3: Add the reservation to the repository contract and implementation**

In `ISaleRepository.cs`, add inside the interface after `CompleteSaleAsync`:

```csharp

    /// <summary>
    /// Takes the next bill number from the same database sequence the invoice column defaults to.
    /// The database still picks the number (no clash between tills); reserving it before the save
    /// is what lets the InvoiceCompleted payload carry it in the same SaveChangesAsync.
    /// </summary>
    Task<long> ReserveInvoiceNumberAsync(CancellationToken cancellationToken = default);
```

In `SaleRepository.cs`, add `using Microsoft.EntityFrameworkCore;` at the top and this member after `CompleteSaleAsync`:

```csharp

    /// <summary>A constant, so EF's raw-SQL analyzer has nothing to warn about.</summary>
    private const string NextInvoiceNumberSql = $"SELECT nextval('{InvoiceNumberSequence.Name}') AS \"Value\"";

    public Task<long> ReserveInvoiceNumberAsync(CancellationToken cancellationToken = default) =>
        _dbContext.Database
                  .SqlQueryRaw<long>(NextInvoiceNumberSql)
                  .SingleAsync(cancellationToken);
```

- [ ] **Step 4: Add the number to the response and the event**

`CompleteSaleResponse.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Sales;

/// <summary>
/// Response after completing a sale. InvoiceNumber is the bill number the receipt prints.
/// </summary>
public record CompleteSaleResponse(
    Guid InvoiceId,
    decimal TotalAmount,
    DateTime CreatedUtc,
    long InvoiceNumber);
```

In `InvoiceCompletedEvent.cs`, after `public DateTime CreatedAtUtc { get; init; }` (line 24):

```csharp

    /// <summary>
    /// The bill number printed on the receipt. Null only for an event queued before this field
    /// existed, so a consumer can tell "unknown" from a real number.
    /// </summary>
    public long? InvoiceNumber { get; init; }
```

- [ ] **Step 5: Reserve, stamp and carry the number in the handler**

In `CompleteSaleCommandHandler.cs`, replace lines 66-78 (from `var now = DateTime.UtcNow;` to the end of the `invoice` initializer) with:

```csharp
        var now = DateTime.UtcNow;
        var invoiceId = Guid.NewGuid();

        // Reserved AFTER validation, so a rejected sale burns no number, and from the same sequence
        // the column defaults to, so the database still decides and two tills cannot clash.
        var invoiceNumber = await _saleRepository.ReserveInvoiceNumberAsync(cancellationToken);

        // Build invoice
        var invoice = new Invoice
        {
            Id = invoiceId,
            StoreId = command.StoreId,
            UserId = command.UserId,
            InvoiceNumber = invoiceNumber,
            TotalAmount = command.Lines.Sum(l => l.Quantity * l.UnitPrice),
            CreatedUtc = now,
            LastModifiedUtc = now
        };
```

In the `InvoiceCompletedEvent` initializer (after `TotalAmount = invoice.TotalAmount,`), add:

```csharp
            InvoiceNumber = invoice.InvoiceNumber,
```

Replace the return statement (lines 199-202) with:

```csharp
        return new CompleteSaleResponse(
            InvoiceId: invoice.Id,
            TotalAmount: invoice.TotalAmount,
            CreatedUtc: invoice.CreatedUtc,
            InvoiceNumber: invoice.InvoiceNumber);
```

- [ ] **Step 6: Update the two existing `CompleteSaleResponse` constructions**

`StoreHubE2ETests.cs:207-210` and `StoreHubHttpClientTests.cs:139-142` — add a final argument to each:

```csharp
            CreatedUtc: DateTime.UtcNow,
            InvoiceNumber: 1001);
```

(Replace the existing `CreatedUtc: DateTime.UtcNow);` line.)

- [ ] **Step 7: Run the unit tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CompleteSale"`
Expected: PASS — the 4 new tests and the existing `CompleteSaleCommandHandlerTests` (AutoMoq returns a value for the new method).

- [ ] **Step 8: Write the failing integration tests**

Add to `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs` (add `using System.Text.Json;`, `using IndyPOS.Application.UseCases.Cloud.Sync.Events;`, `using IndyPOS.Infrastructure.Persistence.StoreHub;`, `using Microsoft.EntityFrameworkCore;`, `using Microsoft.Extensions.DependencyInjection;`):

```csharp
    private async Task<CompleteSaleRequest> OneCashSaleRequestAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 10m, initialStock: 100);
        var user = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");
        return new CompleteSaleRequest(
            UserId: user.Id,
            Lines: [new SaleLineRequest(product.Id, Quantity: 1, UnitPrice: 10m)],
            Payments: [new SalePaymentRequest("Cash", Amount: 10m)]);
    }

    private async Task<CompleteSaleResponse> CompleteAsync(CompleteSaleRequest request)
    {
        var response = await Client.PostAsJsonAsync("/sales/complete", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CompleteSale_WithTwoConcurrentSales_AssignsDistinctNumbers()
    {
        await AuthenticateAsCashierAsync();
        var request = await OneCashSaleRequestAsync();

        var results = await Task.WhenAll(CompleteAsync(request), CompleteAsync(request));

        results.Select(r => r.InvoiceNumber).Should()
                                            .OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CompleteSale_WithValidData_ReturnsAPositiveInvoiceNumber()
    {
        await AuthenticateAsCashierAsync();

        var result = await CompleteAsync(await OneCashSaleRequestAsync());

        result.InvoiceNumber.Should()
                            .BePositive();
    }

    [Fact]
    public async Task CompleteSale_WithValidData_StoresTheReturnedNumberOnTheInvoice()
    {
        await AuthenticateAsCashierAsync();

        var result = await CompleteAsync(await OneCashSaleRequestAsync());

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        (await db.Invoices.SingleAsync(i => i.Id == result.InvoiceId)).InvoiceNumber.Should()
                                                                              .Be(result.InvoiceNumber);
    }

    [Fact]
    public async Task CompleteSale_WithValidData_CarriesTheNumberInTheOutboxEvent()
    {
        await AuthenticateAsCashierAsync();

        var result = await CompleteAsync(await OneCashSaleRequestAsync());

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var outbox = await db.OutboxEvents.SingleAsync(e =>
            e.Type == "InvoiceCompleted" && e.PayloadJson.Contains(result.InvoiceId.ToString()));
        JsonSerializer.Deserialize<InvoiceCompletedEvent>(outbox.PayloadJson)!
                      .InvoiceNumber.Should()
                                    .Be(result.InvoiceNumber);
    }
```

- [ ] **Step 9: Run the integration tests**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesEndpointTests"`
Expected: PASS (existing 7 + 4 new). If you ran Step 8 before Step 5 they fail with `InvoiceNumber` = 0 — that is the RED this step would have shown.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.Application src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/SaleRepository.cs tests/IndyPOS.Application.Tests tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesEndpointTests.cs
git commit -m "feat(sales): return the bill number and carry it in InvoiceCompleted"
```

---

### Task 3: MigrationTool — v3 numbers, native-invoice refusal, `setval`, bulk contract

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Services/SqliteMigrationService.cs:76-88` (add refusal after it), `:116-120` (setval), `:164-174` (doc comment), `:506-515` (number), `:762-796` (internal + bulk field)
- Modify: `src/IndyPOS.MigrationTool/IndyPOS.MigrationTool.csproj` (`InternalsVisibleTo`)
- Modify: `src/IndyPOS.Application/UseCases/Cloud/Sync/BulkMigration/BulkMigrationCommand.cs:45-51`
- Modify: `docs/operations/store-installation-guide.md` §5.3 (one paragraph)
- Test: `tests/IndyPOS.MigrationTool.Tests/InvoiceNumberMigrationTests.cs`

**Interfaces:**
- Consumes: Task 1 `Invoice.InvoiceNumber`, `InvoiceNumberSequence.Name`.
- Produces:
  - `MigratedInvoice(Guid Id, Guid UserId, decimal TotalAmount, DateTime CreatedAtUtc, IReadOnlyList<MigratedInvoiceLine> Lines, IReadOnlyList<MigratedPayment> Payments, long? InvoiceNumber = null)` — plan 2's CloudApi handler stores it.
  - `SqliteMigrationService.BuildBulkMigrationRequestAsync(CancellationToken ct) : Task<BulkMigrationRequest>` becomes `internal`.
  - Phase-failure name `"NativeInvoices"` when the target store already holds invoices without a legacy id.

**Finding (spec §4 "check whether today's rule guarantees it"): it does not.** `CountMigratedInvoicesAsync` (`SqliteMigrationService.cs:164-174`) counts only rows with `LegacyInvoiceId != null`, and its own doc comment says a store trading in v4 must NOT be refused. A target holding v4-native invoices therefore migrates today — and would now collide on `(store_id, invoice_number)`. The RED test below proves it.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.MigrationTool.Tests/InvoiceNumberMigrationTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.MigrationTool;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Spec §4: every imported v3 invoice keeps its v3 number as its bill number, the sequence carries
/// on after the highest one, and a target that already took v4 sales is refused.
/// </summary>
[Collection("Postgres")]
public class InvoiceNumberMigrationTests : IAsyncLifetime
{
    private const long LowerLegacyNumber = 6999;
    private const long HigherLegacyNumber = 7001;
    private const string OtherStoreId = "ANOTHER-STORE";

    private readonly PostgresFixture _postgres;

    public InvoiceNumberMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Two sales, the HIGHER legacy id seeded first, so "the last row" is not the maximum.</summary>
    private static async Task SeedTwoSalesAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        foreach (var (invoiceId, paymentId) in new[] { ((int)HigherLegacyNumber, 9001), ((int)LowerLegacyNumber, 9002) })
        {
            await builder.AddInvoiceAsync(invoiceId, userId: 1, total: 120m, dateCreated: "2024-03-15 14:30:00");
            await builder.AddInvoiceLineAsync(
                invoiceProductId: invoiceId, invoiceId: invoiceId, productId: 10, barcode: "8850001000010",
                description: "Cement 50kg", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
            await builder.AddPaymentAsync(
                paymentId: paymentId, invoiceId: invoiceId, paymentTypeId: 1, amount: 120m,
                dateCreated: "2024-03-15 14:30:00");
        }
    }

    /// <summary>An invoice v4 created itself: no legacy id, number from the column default.</summary>
    private async Task<Invoice> SeedNativeInvoiceAsync(string storeId)
    {
        await using var db = _postgres.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private MigrationOptions Options(LegacyStoreDatabase store) => new()
    {
        SqlitePath = store.Path,
        PostgresConnectionString = _postgres.ConnectionString,
        StoreId = MigrationScenario.StoreId,
        DryRun = false
    };

    [Fact]
    public async Task MigrateAll_WhenTheTargetStoreHasANativeInvoice_RefusesTheRun()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await SeedNativeInvoiceAsync(MigrationScenario.StoreId);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PhaseFailures.Should()
                            .ContainSingle(f => f.Phase == "NativeInvoices");
    }

    [Fact]
    public async Task MigrateAll_WhenTheTargetStoreHasANativeInvoice_WritesNoLegacyInvoice()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await SeedNativeInvoiceAsync(MigrationScenario.StoreId);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync(i => i.LegacyInvoiceId != null)).Should()
                                                                      .Be(0);
    }

    [Fact]
    public async Task MigrateAll_WhenOnlyAnotherStoreHasANativeInvoice_Succeeds()
    {
        // Bill numbers are unique per store, so another store's v4 sales cannot collide.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await SeedNativeInvoiceAsync(OtherStoreId);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should()
                        .BeTrue();
    }

    [Fact]
    public async Task MigrateAll_WithALegacyInvoice_WritesItsV3NumberAsTheBillNumber()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.SingleAsync(i => i.LegacyInvoiceId == HigherLegacyNumber)).InvoiceNumber.Should()
                                                                                              .Be(HigherLegacyNumber);
    }

    [Fact]
    public async Task MigrateAll_AfterImport_GivesTheNextSaleTheNumberAfterTheHighestV3Number()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await MigrationScenario.RunAsync(store, _postgres);

        var nextSale = await SeedNativeInvoiceAsync(MigrationScenario.StoreId);

        nextSale.InvoiceNumber.Should()
                              .Be(HigherLegacyNumber + 1);
    }

    [Fact]
    public async Task BuildBulkMigrationRequest_AfterImport_CarriesTheV3BillNumber()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        var service = new SqliteMigrationService(Options(store), NullLogger<SqliteMigrationService>.Instance);
        await service.MigrateAllAsync();

        var request = await service.BuildBulkMigrationRequestAsync(CancellationToken.None);

        request.Invoices.Select(i => i.InvoiceNumber).Should()
                                                     .BeEquivalentTo(new long?[] { HigherLegacyNumber, LowerLegacyNumber });
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "FullyQualifiedName~InvoiceNumberMigrationTests"`
Expected: build FAILS on `BuildBulkMigrationRequestAsync` (private) and `MigratedInvoice.InvoiceNumber`. After Steps 3-4 only, the refusal tests FAIL (the run succeeds), the v3-number test FAILS (number came from the sequence), and the next-sale test FAILS (sequence not advanced).

- [ ] **Step 3: Carry the number on the bulk contract**

In `BulkMigrationCommand.cs`, replace the `MigratedInvoice` record (lines 42-51) with:

```csharp
/// <summary>
/// Invoice data for migration sync.
/// </summary>
/// <param name="InvoiceNumber">
/// The bill number (a v3 invoice's v3 number). Nullable so either side can be older: an older cloud
/// ignores the field, and an older migrator sends null.
/// </param>
public record MigratedInvoice(
    Guid Id,
    Guid UserId,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<MigratedInvoiceLine> Lines,
    IReadOnlyList<MigratedPayment> Payments,
    long? InvoiceNumber = null);
```

- [ ] **Step 4: Open the builder to the tests**

In `IndyPOS.MigrationTool.csproj`, add before `</Project>`:

```xml
  <ItemGroup>
    <InternalsVisibleTo Include="IndyPOS.MigrationTool.Tests" />
  </ItemGroup>
```

In `SqliteMigrationService.cs`, change line 762 from `private async Task<BulkMigrationRequest> BuildBulkMigrationRequestAsync(CancellationToken ct)` to:

```csharp
    internal async Task<BulkMigrationRequest> BuildBulkMigrationRequestAsync(CancellationToken ct)
```

and in its `new MigratedInvoice(` projection (lines 786-792) add the number as the last argument:

```csharp
            .Select(i => new MigratedInvoice(
                i.Id,
                i.UserId,
                i.TotalAmount,
                i.CreatedUtc,
                i.Lines.Select(l => new MigratedInvoiceLine(l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList(),
                i.Payments.Select(p => new MigratedPayment(p.Id, p.Method, p.Amount, p.Note)).ToList(),
                i.InvoiceNumber))
```

- [ ] **Step 5: Write the v3 number on every imported invoice**

In `MigrateInvoicesAsync`, in the `new Invoice` initializer (lines 506-515), after `LegacyInvoiceId = (int)invoice.InvoiceId,` add:

```csharp
                    // Spec §4: the v3 number IS the bill number, so an old paper receipt still finds
                    // its bill. Set explicitly, so EF sends it instead of letting the default draw one.
                    InvoiceNumber = invoice.InvoiceId,
```

- [ ] **Step 6: Refuse a target store that already took v4 sales**

In `MigrateAllAsync`, directly after the `AlreadyMigrated` block (after line 88), add:

```csharp

        // Spec §4. An invoice v4 created itself drew its bill number from the sequence, starting at
        // 1 -- the same range the imported v3 numbers occupy -- so importing on top of it would
        // collide on (store_id, invoice_number) as an opaque 23505. Refused up front, per store
        // like the guard above: another store's sales hold numbers in another store's range.
        if (!_options.DryRun && await CountNativeInvoicesAsync(context, ct) is var native and > 0)
        {
            _result.AddPhaseFailure("NativeInvoices",
                $"Store '{_options.StoreId}' already has {native} invoice(s) created by v4 itself. " +
                "Their bill numbers would collide with the imported v3 numbers, so nothing was " +
                "written. Migrate into a database that has not yet taken a v4 sale for this store.");

            _logger.LogError(
                "Migration REFUSED. Store {StoreId} already has {Count} v4-native invoice(s).",
                _options.StoreId, native);

            return _result;
        }
```

Replace the `CountMigratedInvoicesAsync` doc comment and method (lines 164-174) with:

```csharp
    /// <summary>
    /// How many invoices this store has already migrated into the target.
    /// </summary>
    /// <remarks>
    /// Counted by <see cref="Invoice.LegacyInvoiceId"/> being set. v4-native invoices are refused by
    /// the separate <see cref="CountNativeInvoicesAsync"/> check, with its own message, because the
    /// problem is a different one: bill-number collision, not duplicated history.
    /// </remarks>
    private async Task<int> CountMigratedInvoicesAsync(StoreHubDbContext context, CancellationToken ct) =>
        await context.Invoices
            .CountAsync(i => i.StoreId == _options.StoreId && i.LegacyInvoiceId != null, ct);

    /// <summary>How many invoices this store's till created in v4 (no legacy id).</summary>
    private async Task<int> CountNativeInvoicesAsync(StoreHubDbContext context, CancellationToken ct) =>
        await context.Invoices
            .CountAsync(i => i.StoreId == _options.StoreId && i.LegacyInvoiceId == null, ct);
```

- [ ] **Step 7: Move the sequence past the highest imported number before commit**

Add a constant near the top of the class (after `InitialStockReason`):

```csharp

    /// <summary>
    /// Moves the bill-number sequence to the highest number present. HAVING skips the call when there
    /// are no invoices, where MAX is NULL and setval would fail.
    /// </summary>
    private const string AdvanceInvoiceNumberSequenceSql =
        $"SELECT setval('{InvoiceNumberSequence.Name}', MAX(invoice_number)) FROM invoice HAVING MAX(invoice_number) IS NOT NULL";
```

Replace lines 116-120 with:

```csharp
        if (!_options.DryRun && _result.PhaseFailures.Count == 0)
        {
            await context.SaveChangesAsync(ct);

            // Spec §4: "after the invoice phase, inside the same transaction". It runs after the
            // final save, not at the end of MigrateInvoicesAsync, because the last batch of invoices
            // only reaches the database here. Note setval itself is not rolled back if the commit
            // then fails -- harmless, it only leaves a gap in the numbering.
            await context.Database.ExecuteSqlRawAsync(AdvanceInvoiceNumberSequenceSql, ct);
            await transaction!.CommitAsync(ct);
        }
```

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "FullyQualifiedName~InvoiceNumberMigrationTests|FullyQualifiedName~SqliteMigrationServiceTests|FullyQualifiedName~LegacyIdMigrationTests|FullyQualifiedName~MigrationPhaseIsolationTests"`
Expected: PASS — 6 new, and the existing re-run / two-store / phase-isolation tests unchanged.

- [ ] **Step 9: Tell the operator about the new refusal**

In `docs/operations/store-installation-guide.md` §5.3, after the paragraph ending "…measured on a real store: 15 invoices and ฿1,056 became 30 and ฿2,112." add:

```markdown
>
> It is also **refused** if this store's database already holds a sale made in v4
> (`Migration REFUSED … v4-native invoice(s)`): those bills took numbers from 1 upward, the same
> range the imported v3 bill numbers use. Migrate before the till makes its first v4 sale.
```

- [ ] **Step 10: Confirm CloudApi still builds and passes**

Run: `dotnet build src/IndyPOS.CloudApi && dotnet test tests/IndyPOS.CloudApi.Tests`
Expected: build succeeds; tests PASS (the new parameter is optional and last, so no caller changes).

- [ ] **Step 11: Commit**

```bash
git add src/IndyPOS.MigrationTool src/IndyPOS.Application/UseCases/Cloud/Sync/BulkMigration/BulkMigrationCommand.cs tests/IndyPOS.MigrationTool.Tests/InvoiceNumberMigrationTests.cs docs/operations/store-installation-guide.md
git commit -m "feat(migration): keep v3 bill numbers, refuse v4-native targets, advance the sequence"
```

---

### Task 4: MigrationTool `verify` — two bill-number checks

**Files:**
- Modify: `src/IndyPOS.MigrationTool/Services/MigrationVerifier.cs:12-22` (constants), `:118` (calls), `:144-145` (exclusions), new private methods
- Modify: `docs/operations/store-installation-guide.md` §5.4 (two table rows)
- Test: `tests/IndyPOS.MigrationTool.Tests/MigrationVerifierTests.cs` (add 5 tests)

**Interfaces:**
- Consumes: Task 3 migration writing `InvoiceNumber = LegacyInvoiceId` and advancing the sequence.
- Produces: `VerificationCheck` rows named `"Invoice numbers"` (`SqliteCount` = migrated invoices, `PostgresCount` = those carrying their v3 number) and `"Invoice number sequence"` (`SqliteCount` = max bill number, `PostgresCount` = next value).

- [ ] **Step 1: Write the failing tests**

Add to `MigrationVerifierTests.cs` (add `using FluentAssertions;` if it is not global):

```csharp
    private const string InvoiceNumberCheck = "Invoice numbers";
    private const string SequenceCheck = "Invoice number sequence";
    private const int TamperedLegacyInvoiceId = 3;

    private async Task<MigrationOptions> MigrateTenInvoicesAsync()
    {
        await SeedManyAsync(userCount: 1, productCount: 2, invoiceCount: 10);
        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();
        return options;
    }

    private async Task<VerificationResult> VerifyAsync(MigrationOptions options) =>
        await new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance).VerifyAsync();

    [Fact]
    public async Task VerifyAsync_WhenAMigratedBillNumberDiffersFromItsLegacyId_FailsTheInvoiceNumberCheck()
    {
        // Non-vacuity: proves the check can fail.
        var options = await MigrateTenInvoicesAsync();
        await using (var db = _postgres.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                $"UPDATE invoice SET invoice_number = invoice_number + 100000 WHERE legacy_invoice_id = {TamperedLegacyInvoiceId}");
        }

        var result = await VerifyAsync(options);

        result.Checks.Single(c => c.EntityName == InvoiceNumberCheck).IsValid.Should()
                                                                            .BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_WhenAMigratedBillNumberDiffersFromItsLegacyId_NamesTheInvoice()
    {
        var options = await MigrateTenInvoicesAsync();
        await using (var db = _postgres.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                $"UPDATE invoice SET invoice_number = invoice_number + 100000 WHERE legacy_invoice_id = {TamperedLegacyInvoiceId}");
        }

        var result = await VerifyAsync(options);

        result.Errors.Should()
                     .Contain(e => e.Contains($"legacy invoice {TamperedLegacyInvoiceId}: bill number 100003"));
    }

    [Fact]
    public async Task VerifyAsync_WhenTheSequenceIsBehindTheHighestBillNumber_FailsTheSequenceCheck()
    {
        var options = await MigrateTenInvoicesAsync();
        await using (var db = _postgres.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("SELECT setval('invoice_number_seq', 1)");
        }

        var result = await VerifyAsync(options);

        result.Checks.Single(c => c.EntityName == SequenceCheck).IsValid.Should()
                                                                        .BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_AfterSuccessfulMigration_PassesTheInvoiceNumberCheck()
    {
        var options = await MigrateTenInvoicesAsync();

        var result = await VerifyAsync(options);

        result.Checks.Single(c => c.EntityName == InvoiceNumberCheck).IsValid.Should()
                                                                            .BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_AfterSuccessfulMigration_PassesTheSequenceCheck()
    {
        var options = await MigrateTenInvoicesAsync();

        var result = await VerifyAsync(options);

        result.Checks.Single(c => c.EntityName == SequenceCheck).IsValid.Should()
                                                                        .BeTrue();
    }
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "FullyQualifiedName~MigrationVerifierTests"`
Expected: the 5 new tests FAIL with `InvalidOperationException: Sequence contains no matching element` (no such check rows yet).

- [ ] **Step 3: Add the two checks**

In `MigrationVerifier.cs`, add after `NoInvoicePaymentCheckName` (line 22):

```csharp

    /// <summary>Migrated invoices must carry their v3 number as their bill number.</summary>
    private const string InvoiceNumberCheckName = "Invoice numbers";

    /// <summary>The next bill number must be above every existing one, or the next sale fails.</summary>
    private const string InvoiceSequenceCheckName = "Invoice number sequence";

    /// <summary>Reads the sequence's next value WITHOUT consuming it, as nextval() would.</summary>
    private const string NextInvoiceNumberSql =
        $"SELECT CASE WHEN is_called THEN last_value + 1 ELSE last_value END AS \"Value\" FROM {InvoiceNumberSequence.Name}";
```

After `await VerifyCategoriesAsync(sqliteConnection, context, result, ct);` (line 118) add:

```csharp

        // Spec §4: an old paper receipt must find its bill, and the next v4 sale must not collide.
        await VerifyInvoiceNumbersAsync(context, result, ct);
        await VerifyInvoiceSequenceAsync(context, result, ct);
```

Extend the exclusion list (lines 144-145) so these rows are not restated generically:

```csharp
            if (check.EntityName is not ("Total Revenue" or StockCheckName or CategoryCheckName
                                        or BarcodeKeyCheckName or NoInvoicePaymentCheckName
                                        or InvoiceNumberCheckName or InvoiceSequenceCheckName))
```

Add these methods before `VerifyPayLaterAsync`:

```csharp
    /// <summary>
    /// Every migrated invoice of this store carries its v3 number as its bill number. Counts are of
    /// migrated invoices, so the row also reads as coverage.
    /// </summary>
    private async Task VerifyInvoiceNumbersAsync(StoreHubDbContext context, VerificationResult result, CancellationToken ct)
    {
        var migrated = context.Invoices.Where(i => i.StoreId == _options.StoreId && i.LegacyInvoiceId != null);
        var mismatched = migrated.Where(i => i.InvoiceNumber != (long)i.LegacyInvoiceId!.Value);

        var total = await migrated.CountAsync(ct);
        var mismatchCount = await mismatched.CountAsync(ct);

        result.Checks.Add(new VerificationCheck(InvoiceNumberCheckName, total, total - mismatchCount, mismatchCount == 0));

        if (mismatchCount == 0)
        {
            return;
        }

        result.Errors.Add(
            $"{mismatchCount} migrated invoice(s) do not carry their v3 number as their bill number, " +
            "so their old paper receipts will not find them.");

        var examples = await mismatched.OrderBy(i => i.LegacyInvoiceId)
                                       .Take(MaxMismatchesReported)
                                       .Select(i => new { i.LegacyInvoiceId, i.InvoiceNumber })
                                       .ToListAsync(ct);

        foreach (var row in examples)
        {
            result.Errors.Add($"  legacy invoice {row.LegacyInvoiceId}: bill number {row.InvoiceNumber}");
        }

        if (mismatchCount > MaxMismatchesReported)
        {
            result.Errors.Add($"  ... and {mismatchCount - MaxMismatchesReported} more");
        }
    }

    /// <summary>
    /// The sequence's next value is above the highest bill number in the database. Database-wide,
    /// because the sequence is: a behind sequence makes the next sale fail on the unique index.
    /// </summary>
    /// <remarks>
    /// Reported in the count columns as (highest number, next value) -- not counts, but the two
    /// figures the operator needs to see.
    /// </remarks>
    private static async Task VerifyInvoiceSequenceAsync(StoreHubDbContext context, VerificationResult result, CancellationToken ct)
    {
        var highest = await context.Invoices.MaxAsync(i => (long?)i.InvoiceNumber, ct);
        var next = await context.Database.SqlQueryRaw<long>(NextInvoiceNumberSql).SingleAsync(ct);
        var isValid = highest is null || next > highest;

        result.Checks.Add(new VerificationCheck(
            InvoiceSequenceCheckName, ClampToInt(highest ?? 0), ClampToInt(next), isValid));

        if (!isValid)
        {
            result.Errors.Add(
                $"The next bill number would be {next}, but bill number {highest} already exists, so " +
                $"the next sale would fail. Fix with: SELECT setval('{InvoiceNumberSequence.Name}', {highest});");
        }
    }

    private static int ClampToInt(long value) => (int)Math.Min(value, int.MaxValue);
```

- [ ] **Step 4: Run the verifier tests to verify they pass**

Run: `dotnet test tests/IndyPOS.MigrationTool.Tests --filter "FullyQualifiedName~MigrationVerifierTests"`
Expected: PASS — 5 new, and the existing ones including `VerifyAsync_AfterSuccessfulMigration_ReturnsValid` (the new rows are valid after a real migration).

- [ ] **Step 5: Document the two rows**

In `docs/operations/store-installation-guide.md` §5.4, change "Four rows check **values**" to "Six rows check **values**" and add to that table:

```markdown
| `Invoice numbers` | A migrated bill does not carry its v3 number, so its old paper receipt will not find it. A real problem |
| `Invoice number sequence` | The next bill number is not above the highest one, so the next sale would fail. The error prints the one-line `setval` fix |
```

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.MigrationTool/Services/MigrationVerifier.cs tests/IndyPOS.MigrationTool.Tests/MigrationVerifierTests.cs docs/operations/store-installation-guide.md
git commit -m "feat(migration): verify bill numbers and the invoice sequence"
```

---

### Task 5: `sales.reprint`, the today-only rule, and closing `/cash`'s past-day gap

**Files:**
- Modify: `src/IndyPOS.Application/Common/Authorization/Capability.cs` (add constant)
- Modify: `src/IndyPOS.Application/Common/Authorization/RoleCapabilities.cs` (grant to 3 roles)
- Create: `src/IndyPOS.Application/Common/Authorization/TodayOnlyRule.cs`
- Create: `src/IndyPOS.Application/Common/Exceptions/OtherDayForbiddenException.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Cash/ClaimsPrincipalExtensions.cs` (add `HasCapability`)
- Create: `src/IndyPOS.StoreHub/Endpoints/Cash/TodayOnlyBusinessDateFilter.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Cash/CashEndpoints.cs:16-19` (add the filter to the group)
- Modify: `src/IndyPOS.StoreHub/Endpoints/Cash/CashExceptionFilter.cs` (map to 403)
- Modify: `tests/IndyPOS.Application.Tests/Common/Authorization/RoleCapabilitiesTests.cs:216-268` (counts 3→4, 7→8, 13→14)
- Test: `tests/IndyPOS.Application.Tests/Common/Authorization/SalesReprintCapabilityTests.cs`
- Test: `tests/IndyPOS.Application.Tests/Common/Authorization/TodayOnlyRuleTests.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashPastDayAccessTests.cs`

**Interfaces:**
- Consumes: `ICashDrawerClock` (`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/ICashDrawerClock.cs`).
- Produces:
  - `Capability.SalesReprint = "sales.reprint"`.
  - `TodayOnlyRule.Allows(DateOnly day, DateOnly today, bool canViewAnyDay) : bool`
  - `TodayOnlyRule.EnsureAllowed(DateOnly day, DateOnly today, bool canViewAnyDay)` — throws `OtherDayForbiddenException`.
  - `TodayOnlyRule.BusinessDateOf(DateTime utc, TimeZoneInfo storeTimeZone) : DateOnly`
  - `OtherDayForbiddenException` (Thai message) → `403` in both `CashExceptionFilter` and (Task 7) `SalesExceptionFilter`.
  - `ClaimsPrincipalExtensions.HasCapability(this ClaimsPrincipal user, string capability) : bool` (internal, namespace `IndyPOS.StoreHub.Endpoints.Cash`).
  - `TodayOnlyBusinessDateFilter` — an `IEndpointFilter` on the whole `/cash` group.

- [ ] **Step 1: Write the failing regression test for the #97 gap (RED first)**

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashPastDayAccessTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Spec §5: a cashier sees today's cash figures but never another day's. Since #97 every /cash read
/// took ?businessDate= with no check, so any cashier could read any past day's sales totals, counted
/// cash, payouts, floats and debt repayments. Each read is listed so a route cannot slip the rule.
/// </summary>
[Collection("Integration")]
public class CashPastDayAccessTests : IntegrationTestBase
{
    public CashPastDayAccessTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    public static TheoryData<string> CashReads =>
    [
        "/cash/summary",
        "/cash/counts",
        "/cash/payouts",
        "/cash/floats",
        "/cash/debt-repayments",
    ];

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    private static string For(string route, DateOnly day) => $"{route}?businessDate={day:yyyy-MM-dd}";

    private sealed record ErrorBody(string Error);

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsCashierForAPastDate_ReturnsForbidden(string route)
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For(route, Today.AddDays(-1)));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsCashierForAFutureDate_ReturnsForbidden(string route)
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For(route, Today.AddDays(1)));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CashRead_AsCashierForAPastDate_ReturnsAThaiError()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For("/cash/counts", Today.AddDays(-1)));

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Contain("วันนี้");
    }

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsCashierForToday_ReturnsOk(string route)
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync(For(route, Today));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(CashReads))]
    public async Task CashRead_AsManagerForAPastDate_ReturnsOk(string route)
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync(For(route, Today.AddDays(-1)));

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Run to verify the gap is real**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CashPastDayAccessTests"`
Expected: every `…ReturnsForbidden` row (5 past + 5 future) and `…ReturnsAThaiError` FAIL (actual `200`); the `ReturnsOk` rows (5 today + 5 manager) PASS.

- [ ] **Step 3: Write the failing unit tests for the rule and the capability**

`tests/IndyPOS.Application.Tests/Common/Authorization/TodayOnlyRuleTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Authorization;

public class TodayOnlyRuleTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    private static readonly DateOnly Today = new(2026, 9, 27);

    /// <summary>00:05 Bangkok on 27 Sep = 17:05 UTC on 26 Sep.</summary>
    private static readonly DateTime JustAfterBangkokMidnightUtc = new(2026, 9, 26, 17, 5, 0, DateTimeKind.Utc);

    /// <summary>23:55 Bangkok on 26 Sep = 16:55 UTC on 26 Sep.</summary>
    private static readonly DateTime JustBeforeBangkokMidnightUtc = new(2026, 9, 26, 16, 55, 0, DateTimeKind.Utc);

    [Fact]
    public void Allows_WithTodayOnlyAndYesterday_ReturnsFalse()
    {
        TodayOnlyRule.Allows(Today.AddDays(-1), Today, canViewAnyDay: false).Should()
                                                                             .BeFalse();
    }

    [Fact]
    public void Allows_WithTodayOnlyAndTomorrow_ReturnsFalse()
    {
        TodayOnlyRule.Allows(Today.AddDays(1), Today, canViewAnyDay: false).Should()
                                                                            .BeFalse();
    }

    [Fact]
    public void EnsureAllowed_WithTodayOnlyAndYesterday_Throws()
    {
        var act = () => TodayOnlyRule.EnsureAllowed(Today.AddDays(-1), Today, canViewAnyDay: false);

        act.Should()
           .Throw<OtherDayForbiddenException>();
    }

    [Fact]
    public void BusinessDateOf_WithUtcEveningAfterBangkokMidnight_ReturnsTheNextDay()
    {
        TodayOnlyRule.BusinessDateOf(JustAfterBangkokMidnightUtc, Bangkok).Should()
                                                                          .Be(Today);
    }

    [Fact]
    public void BusinessDateOf_WithUtcEveningBeforeBangkokMidnight_ReturnsThatDay()
    {
        TodayOnlyRule.BusinessDateOf(JustBeforeBangkokMidnightUtc, Bangkok).Should()
                                                                           .Be(Today.AddDays(-1));
    }

    [Fact]
    public void BusinessDateOf_WithUnspecifiedKind_TreatsItAsUtc()
    {
        // InMemory and some Npgsql paths hand back Unspecified; ConvertTimeFromUtc would throw on Local.
        var unspecified = DateTime.SpecifyKind(JustAfterBangkokMidnightUtc, DateTimeKind.Unspecified);

        TodayOnlyRule.BusinessDateOf(unspecified, Bangkok).Should()
                                                          .Be(Today);
    }

    [Fact]
    public void Allows_WithTodayOnlyAndToday_ReturnsTrue()
    {
        TodayOnlyRule.Allows(Today, Today, canViewAnyDay: false).Should()
                                                                .BeTrue();
    }

    [Fact]
    public void Allows_WithAnyDayAndYesterday_ReturnsTrue()
    {
        TodayOnlyRule.Allows(Today.AddDays(-1), Today, canViewAnyDay: true).Should()
                                                                            .BeTrue();
    }
}
```

`tests/IndyPOS.Application.Tests/Common/Authorization/SalesReprintCapabilityTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Authorization;

public class SalesReprintCapabilityTests
{
    private const int UnknownRoleId = 99;

    [Fact]
    public void HasCapability_WithUnknownRole_ReturnsFalse()
    {
        RoleCapabilities.HasCapability(UnknownRoleId, Capability.SalesReprint).Should()
                                                                              .BeFalse();
    }

    [Fact]
    public void HasCapability_AsCashier_DoesNotGrantReportsView()
    {
        // The today-only limit rests on this: sales.reprint without reports.view.
        RoleCapabilities.HasCapability((int)UserRole.Cashier, Capability.ReportsView).Should()
                                                                                     .BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.StoreManager)]
    [InlineData(UserRole.SystemAdmin)]
    public void HasCapability_WithStoreRole_GrantsSalesReprint(UserRole role)
    {
        RoleCapabilities.HasCapability((int)role, Capability.SalesReprint).Should()
                                                                          .BeTrue();
    }
}
```

- [ ] **Step 4: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~TodayOnlyRuleTests|FullyQualifiedName~SalesReprintCapabilityTests"`
Expected: build FAILS — `TodayOnlyRule`, `OtherDayForbiddenException`, `Capability.SalesReprint` do not exist.

- [ ] **Step 5: Add the capability and grant it**

`Capability.cs`, under `// Sales operations`:

```csharp
    public const string SalesComplete = "sales.complete";

    // Find and reprint bills. Alone it is held to today's bills; reports.view lifts that.
    public const string SalesReprint = "sales.reprint";
```

`RoleCapabilities.cs` — add `Capability.SalesReprint,` after `Capability.SalesComplete,` in each of the three role lists (Cashier, StoreManager, SystemAdmin).

In `RoleCapabilitiesTests.cs`, in each `GetCapabilities_*` test add `Assert.Contains(Capability.SalesReprint, capabilities);` and change the counts: Cashier `3` → `4` (line 227), StoreManager `7` → `8` (line 245), SystemAdmin `13` → `14` (line 268).

- [ ] **Step 6: Add the exception and the rule**

`src/IndyPOS.Application/Common/Exceptions/OtherDayForbiddenException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// A caller without reports.view asked for a day other than today. Mapped to 403 — the day's
/// existence is not a secret, only its figures are. (A single other-day BILL is 404 instead, so an
/// old bill number's existence is not revealed.)
/// </summary>
public class OtherDayForbiddenException()
    : Exception("ดูได้เฉพาะบิลและยอดของวันนี้เท่านั้น");
```

`src/IndyPOS.Application/Common/Authorization/TodayOnlyRule.cs`:

```csharp
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.Common.Authorization;

/// <summary>
/// "Today's figures yes, other days no" (spec §5). A caller without reports.view is held to the
/// store's current business date. Shared by /sales and /cash so the two cannot drift.
/// </summary>
public static class TodayOnlyRule
{
    public static bool Allows(DateOnly day, DateOnly today, bool canViewAnyDay) =>
        canViewAnyDay || day == today;

    public static void EnsureAllowed(DateOnly day, DateOnly today, bool canViewAnyDay)
    {
        if (!Allows(day, today, canViewAnyDay))
            throw new OtherDayForbiddenException();
    }

    /// <summary>
    /// The store-local calendar date an instant falls on — the same boundary (plain midnight in the
    /// store's timezone) the cash-drawer clock uses. Unspecified kind is read as UTC.
    /// </summary>
    public static DateOnly BusinessDateOf(DateTime utc, TimeZoneInfo storeTimeZone)
    {
        var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(asUtc, storeTimeZone));
    }
}
```

- [ ] **Step 7: Run the unit tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~TodayOnlyRuleTests|FullyQualifiedName~SalesReprintCapabilityTests|FullyQualifiedName~RoleCapabilitiesTests|FullyQualifiedName~CashCapabilityTests"`
Expected: PASS.

- [ ] **Step 8: Gate every `/cash` read's past dates, once for the group**

In `ClaimsPrincipalExtensions.cs`, add `using IndyPOS.Application.Common.Authorization;` and this member:

```csharp

    /// <summary>Whether the token's role grants a capability — the same lookup the policies use.</summary>
    public static bool HasCapability(this ClaimsPrincipal user, string capability) =>
        int.TryParse(user.FindFirst("role_id")?.Value, out var roleId)
        && RoleCapabilities.HasCapability(roleId, capability);
```

`src/IndyPOS.StoreHub/Endpoints/Cash/TodayOnlyBusinessDateFilter.cs`:

```csharp
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>
/// Spec §5: today's cash figures are the cashier's to count; another day's are a report. Applied to
/// the whole /cash group rather than per route, so a new read that takes a businessDate cannot
/// forget the rule — five routes shipped in #97 without it.
/// </summary>
/// <remarks>
/// The only <see cref="DateOnly"/> any /cash route binds is its <c>businessDate</c> query value; the
/// writes take request records. A null (omitted) date means today and passes. A malformed date never
/// gets here: binding answers it with 400 before any filter runs.
/// </remarks>
internal sealed class TodayOnlyBusinessDateFilter(ICashDrawerClock clock) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var day in context.Arguments.OfType<DateOnly>())
        {
            TodayOnlyRule.EnsureAllowed(
                day,
                clock.Now().BusinessDate,
                context.HttpContext.User.HasCapability(Capability.ReportsView));
        }

        return next(context);
    }
}
```

In `CashEndpoints.cs`, add the filter to the group **after** `CashExceptionFilter`. Filters added earlier wrap the later ones, so this order lets the exception filter turn the throw into the 403:

```csharp
        var cash = app.MapGroup("/cash")
                      .RequireAuthorization(Policy)
                      .AddEndpointFilter<RequireUserIdFilter>()
                      .AddEndpointFilter<CashExceptionFilter>()
                      .AddEndpointFilter<TodayOnlyBusinessDateFilter>();
```

The `/summary` mapping itself is unchanged.

In `CashExceptionFilter.cs`, add a catch after the `CashDayClosedException` one:

```csharp
        catch (OtherDayForbiddenException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
```

- [ ] **Step 9: Run the integration tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~Cash"`
Expected: PASS — 21 new rows (4 theories × 5 routes + 1 fact). No existing `/cash` test sends a past `businessDate`; the one that sends `2026-13-01` (`CashPayoutEndpointsTests.cs:172`) still gets binding's `400`.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.Application/Common src/IndyPOS.StoreHub/Endpoints/Cash tests/IndyPOS.Application.Tests/Common/Authorization tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashPastDayAccessTests.cs
git commit -m "feat(auth): add sales.reprint and hold cashiers to today's cash figures"
```

---

### Task 6: Sale queries — list and detail with the today-only rule

**Files:**
- Create: `src/IndyPOS.Application/Common/Exceptions/SaleNotFoundException.cs`, `SalesQueryValidationException.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SaleDtos.cs`, `SaleFigures.cs`, `SalesQueryRules.cs`, `ListSalesQuery.cs`, `GetSaleByIdQuery.cs`, `GetSaleByNumberQuery.cs`
- Create: `src/IndyPOS.Infrastructure/QueryHandlers/Sales/ListSalesQueryHandler.cs`, `GetSaleQueryHandler.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Reports/ReportDtos.cs:35-75` (delete the four invoice DTOs — moved)
- Delete: `src/IndyPOS.Application/UseCases/StoreHub/Reports/GetInvoices/GetInvoicesQuery.cs`, `src/IndyPOS.Application/UseCases/StoreHub/Reports/GetInvoiceDetail/GetInvoiceDetailQuery.cs`, `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetInvoicesQueryHandler.cs`, `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetInvoiceDetailQueryHandler.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs:21-22` (usings), `:116-117` (registrations), `:593-624` (the two retired routes)
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ReportsEndpointTests.cs:96-167` (delete the 5 invoice tests; Task 7 re-creates them against `/sales`)
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/SalesHistoryTestContext.cs`, `SalesQueryRulesTests.cs`, `SaleFiguresTests.cs`, `ListSalesQueryHandlerTests.cs`, `GetSaleQueryHandlerTests.cs`

**Receipt field check (spec §6 "checks these field by field against `ReceiptPrinterService`").** What `ReceiptPrinterService.cs` reads, and where the detail now carries it:

| Printer reads | Line | Detail field |
|---|---|---|
| `invoiceInfo.Id` (bill number) | 159 | `InvoiceNumber` |
| `DateTime.Now` (date, time) | 164, 169 | `CreatedUtc` (plan 3 converts to Bangkok) |
| logged-in user's name | 174 | `CashierName` (original cashier) |
| `product.Description`, `Note`, `Quantity`, `UnitPrice` | 194-200, 221-232 | `Lines[].ProductName`, `Note`, `Quantity`, `UnitPrice`, `LineTotal` |
| `InvoiceTotal` | 214 | `TotalAmount` |
| `IsRefundInvoice` | 223, 276 | `IsRefund` |
| `_paymentTypeDictionary[payment.PaymentTypeId]` (a **display name**) | 274 | **`Payments[].MethodDisplayName`** — not in the spec's list; added |
| `payment.Note`, `payment.Amount` | 250, 279 | `Payments[].Note`, `Amount` |
| `PaymentTotal` | 260 | `AmountReceived` |
| `Changes` | 265 | `ChangeGiven` |
| `HasPayLaterPayment` (`IInvoiceInfo`) | — | `HasPayLater`, `PayLaterAmount` |
| — (split in `SaleHistoryByInvoiceIdForm`) | — | `Lines[].CategoryKind`, `Lines[].Barcode` |

**Interfaces:**
- Consumes: Task 5 `TodayOnlyRule`, `OtherDayForbiddenException`; `ICashDrawerClock`; `ReportDateRange.ToUtcRange` (`src/IndyPOS.Infrastructure/QueryHandlers/Reports/ReportDateRange.cs`).
- Produces (namespace `IndyPOS.Application.UseCases.StoreHub.Sales.History`):

```csharp
public record InvoiceSummaryDto(Guid Id, long InvoiceNumber, DateTime CreatedUtc, decimal TotalAmount, string PrimaryPaymentMethod, int LineCount);
public record SalesPage(IReadOnlyList<InvoiceSummaryDto> Items, int Page, int PageSize, bool HasMore);
public record InvoiceDetailDto(Guid Id, long InvoiceNumber, string StoreId, Guid UserId, string? CashierName,
    decimal TotalAmount, decimal AmountReceived, decimal ChangeGiven, bool IsRefund, bool HasPayLater, decimal PayLaterAmount,
    DateTime CreatedUtc, IReadOnlyList<InvoiceLineDto> Lines, IReadOnlyList<PaymentDto> Payments);
public record InvoiceLineDto(Guid Id, Guid ProductId, string ProductName, string? Barcode, string? Note,
    ProductCategoryKind? CategoryKind, int Quantity, decimal UnitPrice, decimal LineTotal);
public record PaymentDto(Guid Id, string Method, string MethodDisplayName, decimal Amount, string? Note);

public record ListSalesQuery(DateOnly? From, DateOnly? To, int Page, int PageSize, bool CanViewAnyDay) : IQuery<SalesPage>;
public record GetSaleByIdQuery(Guid InvoiceId, bool CanViewAnyDay) : IQuery<InvoiceDetailDto?>;
public record GetSaleByNumberQuery(long InvoiceNumber, bool CanViewAnyDay) : IQuery<InvoiceDetailDto?>;

public static class SalesQueryRules { FirstPage = 1; DefaultPageSize = 50; MaxPageSize = 200; DateFormat = "yyyy-MM-dd";
    DateOnly? ParseDate(string? value); void EnsureValidRange(DateOnly from, DateOnly to);
    void EnsureValidPage(int page, int pageSize); void EnsureValidNumber(long number); string InvalidNumberMessage(string value); }
public readonly record struct SalePayment(string Method, decimal Amount);
public readonly record struct SaleFigures(decimal AmountReceived, decimal ChangeGiven, bool IsRefund, bool HasPayLater, decimal PayLaterAmount)
    { static SaleFigures From(decimal totalAmount, IEnumerable<SalePayment> payments); }
```

  - `SaleNotFoundException()` ("ไม่พบบิล") and `SaleNotFoundException(long invoiceNumber)` ("ไม่พบบิลเลขที่ {n}") → 404; `SalesQueryValidationException(string message)` → 400.
  - Handlers (namespace `IndyPOS.Infrastructure.QueryHandlers.Sales`): `ListSalesQueryHandler(StoreHubDbContext db, IStoreIdentityService storeIdentity, ICashDrawerClock clock)`; `GetSaleQueryHandler(same)` implementing both detail queries. A bill the caller may not see is returned as `null` (the endpoint turns it into 404).

- [ ] **Step 1: Write the failing rule and figures tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/SalesQueryRulesTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class SalesQueryRulesTests
{
    private const int PageSizeAboveMaximum = SalesQueryRules.MaxPageSize + 1;
    private const int PageBeyondAddressableRows = int.MaxValue;
    private static readonly DateOnly Day = new(2026, 9, 27);

    [Theory]
    [InlineData("27/09/2026")]
    [InlineData("2026-02-30")]
    [InlineData("today")]
    public void ParseDate_WithAMalformedValue_Throws(string value)
    {
        var act = () => SalesQueryRules.ParseDate(value);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidRange_WithToBeforeFrom_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidRange(Day, Day.AddDays(-1));

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageZero_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidPage(0, SalesQueryRules.DefaultPageSize);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageSizeZero_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidPage(SalesQueryRules.FirstPage, 0);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageSizeAboveMaximum_Throws()
    {
        var act = () => SalesQueryRules.EnsureValidPage(SalesQueryRules.FirstPage, PageSizeAboveMaximum);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void EnsureValidPage_WithPageBeyondAddressableRows_Throws()
    {
        // (page - 1) * pageSize would overflow int and reach Skip as a negative number.
        var act = () => SalesQueryRules.EnsureValidPage(PageBeyondAddressableRows, SalesQueryRules.MaxPageSize);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-3L)]
    public void EnsureValidNumber_WithANonPositiveNumber_Throws(long number)
    {
        var act = () => SalesQueryRules.EnsureValidNumber(number);

        act.Should()
           .Throw<SalesQueryValidationException>();
    }

    [Fact]
    public void ParseDate_WithNull_ReturnsNull()
    {
        SalesQueryRules.ParseDate(null).Should()
                                       .BeNull();
    }

    [Fact]
    public void ParseDate_WithAnIsoDate_ReturnsIt()
    {
        SalesQueryRules.ParseDate("2026-09-27").Should()
                                               .Be(Day);
    }

    [Fact]
    public void EnsureValidRange_WithTheSameDay_DoesNotThrow()
    {
        var act = () => SalesQueryRules.EnsureValidRange(Day, Day);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureValidPage_WithPageSizeAtMaximum_DoesNotThrow()
    {
        var act = () => SalesQueryRules.EnsureValidPage(SalesQueryRules.FirstPage, SalesQueryRules.MaxPageSize);

        act.Should()
           .NotThrow();
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/SaleFiguresTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class SaleFiguresTests
{
    private const decimal RefundTotal = -120m;

    private static SalePayment Cash(decimal amount) => new(PaymentMethodCodes.Cash, amount);

    [Fact]
    public void From_WithARefundTotal_ReturnsNoChange()
    {
        SaleFigures.From(RefundTotal, [Cash(RefundTotal)]).ChangeGiven.Should()
                                                                      .Be(0m);
    }

    [Fact]
    public void From_WithARefundTotal_FlagsARefund()
    {
        SaleFigures.From(RefundTotal, [Cash(RefundTotal)]).IsRefund.Should()
                                                                   .BeTrue();
    }

    [Fact]
    public void From_WithUnderpayment_ReturnsNoChange()
    {
        SaleFigures.From(350m, [Cash(300m)]).ChangeGiven.Should()
                                                        .Be(0m);
    }

    [Fact]
    public void From_WithALowerCasePayLaterCode_FlagsPayLater()
    {
        // The sale handler accepts method codes case-insensitively, so a till may store "paylater".
        SaleFigures.From(350m, [new SalePayment("paylater", 350m)]).HasPayLater.Should()
                                                                              .BeTrue();
    }

    [Fact]
    public void From_WithoutPayLater_ReturnsZeroPayLaterAmount()
    {
        SaleFigures.From(350m, [Cash(500m)]).PayLaterAmount.Should()
                                                           .Be(0m);
    }

    [Fact]
    public void From_WithOverpayment_ReturnsTheChange()
    {
        SaleFigures.From(350m, [Cash(500m)]).ChangeGiven.Should()
                                                        .Be(150m);
    }

    [Fact]
    public void From_WithTwoPayments_SumsTheAmountReceived()
    {
        SaleFigures.From(350m, [Cash(200m), new SalePayment(PaymentMethodCodes.MoneyTransfer, 150m)]).AmountReceived.Should()
                                                                                                                  .Be(350m);
    }

    [Fact]
    public void From_WithAPayLaterPayment_ReturnsItsAmount()
    {
        SaleFigures.From(350m, [Cash(100m), new SalePayment(PaymentMethodCodes.PayLater, 250m)]).PayLaterAmount.Should()
                                                                                                           .Be(250m);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~SalesQueryRulesTests|FullyQualifiedName~SaleFiguresTests"`
Expected: build FAILS — the `Sales.History` namespace does not exist.

- [ ] **Step 3: Add the exceptions, DTOs, rules, figures and queries**

`src/IndyPOS.Application/Common/Exceptions/SalesQueryValidationException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>A malformed date, range, page or bill number on a /sales request. Mapped to 400.</summary>
public class SalesQueryValidationException(string message) : Exception(message);
```

`src/IndyPOS.Application/Common/Exceptions/SaleNotFoundException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// An unknown bill, or another day's bill asked for by a today-only caller. Mapped to 404 either way,
/// so an old bill number's existence is not revealed.
/// </summary>
public class SaleNotFoundException : Exception
{
    public SaleNotFoundException() : base("ไม่พบบิล") { }

    public SaleNotFoundException(long invoiceNumber) : base($"ไม่พบบิลเลขที่ {invoiceNumber}") { }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SaleDtos.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>A bill in the list. Per-bill amounts help a cashier find "the ฿350 one at 2 pm".</summary>
public record InvoiceSummaryDto(
    Guid Id,
    long InvoiceNumber,
    DateTime CreatedUtc,
    decimal TotalAmount,
    string PrimaryPaymentMethod,
    int LineCount);

/// <summary>
/// One page of bills. Deliberately no money total and no total count: the list is for finding a
/// bill, not a report. HasMore drives "load more".
/// </summary>
public record SalesPage(
    IReadOnlyList<InvoiceSummaryDto> Items,
    int Page,
    int PageSize,
    bool HasMore);

/// <summary>One bill with everything its receipt prints (spec §6, checked against ReceiptPrinterService).</summary>
public record InvoiceDetailDto(
    Guid Id,
    long InvoiceNumber,
    string StoreId,
    Guid UserId,
    string? CashierName,
    decimal TotalAmount,
    decimal AmountReceived,
    decimal ChangeGiven,
    bool IsRefund,
    bool HasPayLater,
    decimal PayLaterAmount,
    DateTime CreatedUtc,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments);

/// <param name="CategoryKind">Null when the product has no category or its code is not in the catalogue.</param>
public record InvoiceLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? Barcode,
    string? Note,
    ProductCategoryKind? CategoryKind,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <param name="MethodDisplayName">The catalogue's name, or the code itself for a method no longer in it.</param>
public record PaymentDto(
    Guid Id,
    string Method,
    string MethodDisplayName,
    decimal Amount,
    string? Note);
```

`src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SalesQueryRules.cs`:

```csharp
using System.Globalization;
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Input rules for /sales. Each failure is a Thai 400, never a 500 from deeper down.</summary>
public static class SalesQueryRules
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// InvariantCulture is load-bearing: the server may run on a th-TH machine, whose Buddhist
    /// calendar would read 2026 as a Buddhist-era year.
    /// </summary>
    public static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new SalesQueryValidationException($"วันที่ไม่ถูกต้อง: {value} (ใช้รูปแบบ {DateFormat})");
    }

    public static void EnsureValidRange(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new SalesQueryValidationException("วันที่เริ่มต้นต้องไม่อยู่หลังวันที่สิ้นสุด");
    }

    public static void EnsureValidPage(int page, int pageSize)
    {
        if (page < FirstPage)
            throw new SalesQueryValidationException("หน้าต้องเริ่มที่ 1");

        if (pageSize is < 1 or > MaxPageSize)
            throw new SalesQueryValidationException($"จำนวนต่อหน้าต้องอยู่ระหว่าง 1 ถึง {MaxPageSize}");

        if ((long)(page - 1) * pageSize > int.MaxValue)
            throw new SalesQueryValidationException("หน้าที่ขอเกินจำนวนบิลที่มีได้");
    }

    public static void EnsureValidNumber(long number)
    {
        if (number < 1)
            throw new SalesQueryValidationException(InvalidNumberMessage(number.ToString(CultureInfo.InvariantCulture)));
    }

    public static string InvalidNumberMessage(string value) => $"เลขที่บิลไม่ถูกต้อง: {value}";
}
```

`src/IndyPOS.Application/UseCases/StoreHub/Sales/History/SaleFigures.cs`:

```csharp
using IndyPOS.Application.Common.Constants;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

public readonly record struct SalePayment(string Method, decimal Amount);

/// <summary>
/// The money a receipt prints that the invoice row does not store. Derived with the same rules the
/// till used when it printed the original (StoreHubSaleService.CalculateChanges /
/// IsRefundInvoice), so a reprint shows the same figures.
/// </summary>
public readonly record struct SaleFigures(
    decimal AmountReceived,
    decimal ChangeGiven,
    bool IsRefund,
    bool HasPayLater,
    decimal PayLaterAmount)
{
    public static SaleFigures From(decimal totalAmount, IEnumerable<SalePayment> payments)
    {
        var all = payments.ToList();
        var received = all.Sum(p => p.Amount);
        var isRefund = totalAmount < 0;
        var payLater = all.Where(p => string.Equals(p.Method, PaymentMethodCodes.PayLater, StringComparison.OrdinalIgnoreCase))
                          .ToList();

        return new SaleFigures(
            AmountReceived: received,
            ChangeGiven: isRefund ? 0m : Math.Max(received - totalAmount, 0m),
            IsRefund: isRefund,
            HasPayLater: payLater.Count > 0,
            PayLaterAmount: payLater.Sum(p => p.Amount));
    }
}
```

`ListSalesQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <param name="From">Null = today's business date.</param>
/// <param name="To">Null = today's business date.</param>
/// <param name="CanViewAnyDay">The caller holds reports.view; otherwise both dates must be today.</param>
public record ListSalesQuery(DateOnly? From, DateOnly? To, int Page, int PageSize, bool CanViewAnyDay) : IQuery<SalesPage>;
```

`GetSaleByIdQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Null when unknown, or on another day and the caller lacks reports.view.</summary>
public record GetSaleByIdQuery(Guid InvoiceId, bool CanViewAnyDay) : IQuery<InvoiceDetailDto?>;
```

`GetSaleByNumberQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Same body and rules as <see cref="GetSaleByIdQuery"/>, keyed by the receipt's bill number.</summary>
public record GetSaleByNumberQuery(long InvoiceNumber, bool CanViewAnyDay) : IQuery<InvoiceDetailDto?>;
```

- [ ] **Step 4: Run the rule and figures tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~SalesQueryRulesTests|FullyQualifiedName~SaleFiguresTests"`
Expected: PASS (16 test cases).

- [ ] **Step 5: Write the handler test context**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/SalesHistoryTestContext.cs`:

```csharp
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Sales;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

/// <summary>What one seeded bill looks like. Defaults: ฿350 paid with ฿500 cash, one line.</summary>
internal sealed record InvoiceSeed(long Number, DateTime CreatedUtc)
{
    public decimal Total { get; init; } = 350m;
    public string? StoreId { get; init; }
    public Guid UserId { get; init; } = SalesHistoryTestContext.CashierId;
    public string? ProductCategory { get; init; }
    public IReadOnlyList<SalePayment> Payments { get; init; } = [new(PaymentMethodCodes.Cash, 500m)];
}

/// <summary>
/// One InMemory StoreHub database, a fake clock at 10:00 Bangkok on <see cref="Today"/>, and a
/// store in the Bangkok timezone.
/// </summary>
internal sealed class SalesHistoryTestContext : IAsyncDisposable
{
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    public static readonly DateOnly Today = new(2026, 9, 27);
    public static readonly DateOnly Yesterday = Today.AddDays(-1);

    /// <summary>10:00 Bangkok on <see cref="Today"/>.</summary>
    public static readonly DateTimeOffset TenAmBangkok = new(2026, 9, 27, 3, 0, 0, TimeSpan.Zero);

    /// <summary>09:00 Bangkok on <see cref="Today"/>.</summary>
    public static readonly DateTime NineAmTodayUtc = new(2026, 9, 27, 2, 0, 0, DateTimeKind.Utc);

    /// <summary>09:30 Bangkok on <see cref="Today"/>.</summary>
    public static readonly DateTime HalfPastNineTodayUtc = new(2026, 9, 27, 2, 30, 0, DateTimeKind.Utc);

    /// <summary>09:00 Bangkok on <see cref="Yesterday"/>.</summary>
    public static readonly DateTime NineAmYesterdayUtc = new(2026, 9, 26, 2, 0, 0, DateTimeKind.Utc);

    /// <summary>00:05 Bangkok on <see cref="Today"/> — still 26 Sep in UTC.</summary>
    public static readonly DateTime JustAfterMidnightTodayUtc = new(2026, 9, 26, 17, 5, 0, DateTimeKind.Utc);

    public static readonly Guid CashierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string OtherStoreId = "other-store";

    public SalesHistoryTestContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new StoreHubDbContext(options);
        StoreIdentity = MockStoreIdentityService.GeneralHardware();
        StoreIdentity.TimeZone = Bangkok;
        Clock = new CashDrawerClock(new FakeTimeProvider(TenAmBangkok), StoreIdentity);
    }

    public StoreHubDbContext Db { get; }
    public MockStoreIdentityService StoreIdentity { get; }
    public ICashDrawerClock Clock { get; }

    public ListSalesQueryHandler ListHandler() => new(Db, StoreIdentity, Clock);

    public GetSaleQueryHandler DetailHandler() => new(Db, StoreIdentity, Clock);

    public async Task<Invoice> SeedInvoiceAsync(InvoiceSeed seed)
    {
        var storeId = seed.StoreId ?? StoreIdentity.StoreId;
        var product = new Product
        {
            Id = Guid.NewGuid(), StoreId = storeId, Barcode = $"885{seed.Number:D10}", Name = "Cement 50kg",
            Category = seed.ProductCategory, UnitPrice = seed.Total, CreatedUtc = seed.CreatedUtc, LastModifiedUtc = seed.CreatedUtc
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), StoreId = storeId, UserId = seed.UserId, InvoiceNumber = seed.Number,
            TotalAmount = seed.Total, CreatedUtc = seed.CreatedUtc, LastModifiedUtc = seed.CreatedUtc
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, ProductId = product.Id, ProductName = product.Name,
            Quantity = 1, UnitPrice = seed.Total, Note = "ถุงใหญ่", CreatedUtc = seed.CreatedUtc, Product = product
        });
        foreach (var payment in seed.Payments)
        {
            invoice.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(), InvoiceId = invoice.Id, Method = payment.Method, Amount = payment.Amount, CreatedUtc = seed.CreatedUtc
            });
        }

        Db.Invoices.Add(invoice);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return invoice;
    }

    public async Task SeedCashierAsync(string firstName, string lastName)
    {
        Db.StoreUsers.Add(new StoreUser
        {
            Id = CashierId, StoreId = StoreIdentity.StoreId, Username = "somchai", FirstName = firstName, LastName = lastName,
            RoleId = 1, CreatedAtUtc = NineAmTodayUtc, LastModifiedAtUtc = NineAmTodayUtc
        });
        await Db.SaveChangesAsync();
    }

    public async Task SeedCategoryAsync(string code, ProductCategoryKind kind)
    {
        Db.ProductCategories.Add(new ProductCategory
        {
            StoreId = StoreIdentity.StoreId, Code = code, DisplayName = code, Kind = kind, IsEnabled = true,
            DisplayOrder = 1, CreatedUtc = NineAmTodayUtc, LastModifiedUtc = NineAmTodayUtc
        });
        await Db.SaveChangesAsync();
    }

    public async Task SeedPaymentMethodAsync(string code, string displayName)
    {
        Db.PaymentMethods.Add(new PaymentMethod
        {
            StoreId = StoreIdentity.StoreId, Code = code, DisplayName = displayName, IsEnabled = true,
            DisplayOrder = 1, CreatedUtc = NineAmTodayUtc, LastModifiedUtc = NineAmTodayUtc
        });
        await Db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
```

- [ ] **Step 6: Write the failing handler tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/ListSalesQueryHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;
using static IndyPOS.Application.Tests.UseCases.StoreHub.Sales.SalesHistoryTestContext;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class ListSalesQueryHandlerTests
{
    private const int PageSizeOfOne = 1;

    private static ListSalesQuery TodayOnly(DateOnly? from = null, DateOnly? to = null, int pageSize = SalesQueryRules.DefaultPageSize) =>
        new(from, to, SalesQueryRules.FirstPage, pageSize, CanViewAnyDay: false);

    [Fact]
    public async Task Handle_WithToBeforeFrom_ThrowsValidation()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(from: Today, to: Yesterday) with { CanViewAnyDay = true });

        await act.Should()
                 .ThrowAsync<SalesQueryValidationException>();
    }

    [Fact]
    public async Task Handle_WithPageSizeAboveMaximum_ThrowsValidation()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(pageSize: SalesQueryRules.MaxPageSize + 1));

        await act.Should()
                 .ThrowAsync<SalesQueryValidationException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterday_ThrowsForbidden()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(from: Yesterday, to: Yesterday));

        await act.Should()
                 .ThrowAsync<OtherDayForbiddenException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerWithARangeReachingBackToYesterday_ThrowsForbidden()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.ListHandler().HandleAsync(TodayOnly(from: Yesterday, to: Today));

        await act.Should()
                 .ThrowAsync<OtherDayForbiddenException>();
    }

    [Fact]
    public async Task Handle_WithoutDates_ExcludesYesterdaysBill()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmYesterdayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .BeEmpty();
    }

    [Fact]
    public async Task Handle_WithABillJustAfterBangkokMidnight_IncludesItInToday()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, JustAfterMidnightTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .ContainSingle(i => i.InvoiceNumber == 1001);
    }

    [Fact]
    public async Task Handle_WithAnotherStoresBillToday_ExcludesIt()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc) { StoreId = OtherStoreId });

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Should()
                  .BeEmpty();
    }

    [Fact]
    public async Task Handle_WithExactlyOnePageOfBills_ReportsNoMore()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly(pageSize: PageSizeOfOne));

        page.HasMore.Should()
                    .BeFalse();
    }

    [Fact]
    public async Task Handle_WithMoreBillsThanThePage_ReportsHasMore()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));
        await c.SeedInvoiceAsync(new InvoiceSeed(1002, HalfPastNineTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly(pageSize: PageSizeOfOne));

        page.HasMore.Should()
                    .BeTrue();
    }

    [Fact]
    public async Task Handle_WithTwoBills_ReturnsTheNewestFirst()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmTodayUtc));
        await c.SeedInvoiceAsync(new InvoiceSeed(1002, HalfPastNineTodayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly());

        page.Items.Select(i => i.InvoiceNumber).Should()
                                               .Equal(1002, 1001);
    }

    [Fact]
    public async Task Handle_AsAnyDayCallerForYesterday_ReturnsYesterdaysBill()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(1001, NineAmYesterdayUtc));

        var page = await c.ListHandler().HandleAsync(TodayOnly(from: Yesterday, to: Yesterday) with { CanViewAnyDay = true });

        page.Items.Should()
                  .ContainSingle(i => i.InvoiceNumber == 1001);
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/GetSaleQueryHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Enums;
using Xunit;
using static IndyPOS.Application.Tests.UseCases.StoreHub.Sales.SalesHistoryTestContext;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class GetSaleQueryHandlerTests
{
    private const long KnownNumber = 7001;
    private const string RetiredCampaignCode = "WeWin";

    [Fact]
    public async Task HandleById_WithAnUnknownId_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(Guid.NewGuid(), CanViewAnyDay: true));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleById_AsTodayOnlyCallerForYesterdaysBill_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleByNumber_AsTodayOnlyCallerForYesterdaysBill_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(KnownNumber, CanViewAnyDay: false));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleByNumber_WithAnotherStoresNumber_ReturnsNull()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc) { StoreId = OtherStoreId });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(KnownNumber, CanViewAnyDay: true));

        sale.Should()
            .BeNull();
    }

    [Fact]
    public async Task HandleByNumber_WithZero_ThrowsValidation()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(0, CanViewAnyDay: true));

        await act.Should()
                 .ThrowAsync<SalesQueryValidationException>();
    }

    [Fact]
    public async Task HandleById_WithAnUnknownCashier_ReturnsNullCashierName()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.CashierName.Should()
                         .BeNull();
    }

    [Fact]
    public async Task HandleById_WithALineWithoutACategory_ReturnsNullKind()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc) { ProductCategory = null });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Lines.Single().CategoryKind.Should()
                                         .BeNull();
    }

    [Fact]
    public async Task HandleById_WithAMethodMissingFromTheCatalogue_UsesTheCodeAsDisplayName()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc)
        {
            Payments = [new SalePayment(RetiredCampaignCode, 350m)]
        });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Payments.Single().MethodDisplayName.Should()
                                                 .Be(RetiredCampaignCode);
    }

    [Fact]
    public async Task HandleById_AsTodayOnlyCallerForABillJustAfterBangkokMidnight_ReturnsIt()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, JustAfterMidnightTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale.Should()
            .NotBeNull();
    }

    [Fact]
    public async Task HandleById_AsAnyDayCallerForYesterdaysBill_ReturnsIt()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: true));

        sale!.InvoiceNumber.Should()
                           .Be(KnownNumber);
    }

    [Fact]
    public async Task HandleByNumber_WithAKnownNumber_ReturnsTheSameBillAsById()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var byNumber = await c.DetailHandler().HandleAsync(new GetSaleByNumberQuery(KnownNumber, CanViewAnyDay: false));
        var byId = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        byNumber.Should()
                .BeEquivalentTo(byId);
    }

    [Fact]
    public async Task HandleById_WithAKnownCashier_ReturnsTheirFullName()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedCashierAsync("สมชาย", "ใจดี");
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.CashierName.Should()
                         .Be("สมชาย ใจดี");
    }

    [Fact]
    public async Task HandleById_WithAHardwareLine_ReturnsTheHardwareKind()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedCategoryAsync(ProductCategoryCodes.GeneralMaterials, ProductCategoryKind.Hardware);
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc) { ProductCategory = ProductCategoryCodes.GeneralMaterials });

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Lines.Single().CategoryKind.Should()
                                         .Be(ProductCategoryKind.Hardware);
    }

    [Fact]
    public async Task HandleById_WithACatalogueMethod_ReturnsItsDisplayName()
    {
        await using var c = new SalesHistoryTestContext();
        await c.SeedPaymentMethodAsync(PaymentMethodCodes.Cash, "เงินสด");
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Payments.Single().MethodDisplayName.Should()
                                                 .Be("เงินสด");
    }

    [Fact]
    public async Task HandleById_WithOverpayment_ReturnsTheChangeGiven()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.ChangeGiven.Should()
                         .Be(150m);
    }

    [Fact]
    public async Task HandleById_WithALineNote_ReturnsIt()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var sale = await c.DetailHandler().HandleAsync(new GetSaleByIdQuery(invoice.Id, CanViewAnyDay: false));

        sale!.Lines.Single().Note.Should()
                                 .Be("ถุงใหญ่");
    }
}
```

- [ ] **Step 7: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ListSalesQueryHandlerTests|FullyQualifiedName~GetSaleQueryHandlerTests"`
Expected: build FAILS — `IndyPOS.Infrastructure.QueryHandlers.Sales` does not exist.

- [ ] **Step 8: Implement the list handler**

`src/IndyPOS.Infrastructure/QueryHandlers/Sales/ListSalesQueryHandler.cs`:

```csharp
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// This store's bills, newest first, one page at a time. Reads one row more than the page to answer
/// "is there more?" without a COUNT.
/// </summary>
public class ListSalesQueryHandler(
    StoreHubDbContext db,
    IStoreIdentityService storeIdentity,
    ICashDrawerClock clock) : IQueryHandler<ListSalesQuery, SalesPage>
{
    public async Task<SalesPage> HandleAsync(ListSalesQuery query, CancellationToken cancellationToken = default)
    {
        var today = clock.Now().BusinessDate;
        var from = query.From ?? today;
        var to = query.To ?? today;

        // Malformed input is a 400 before the today-only rule is a 403.
        SalesQueryRules.EnsureValidRange(from, to);
        SalesQueryRules.EnsureValidPage(query.Page, query.PageSize);
        TodayOnlyRule.EnsureAllowed(from, today, query.CanViewAnyDay);
        TodayOnlyRule.EnsureAllowed(to, today, query.CanViewAnyDay);

        var range = ReportDateRange.ToUtcRange(from, to, storeIdentity.TimeZone);
        var rows = await FetchPageAndOneMoreAsync(query, range, cancellationToken);

        return new SalesPage(rows.Take(query.PageSize).ToList(), query.Page, query.PageSize, HasMore: rows.Count > query.PageSize);
    }

    private Task<List<InvoiceSummaryDto>> FetchPageAndOneMoreAsync(
        ListSalesQuery query, UtcDateTimeRange range, CancellationToken cancellationToken) =>
        db.Invoices
          .AsNoTracking()
          .Where(i => i.StoreId == storeIdentity.StoreId
                      && i.CreatedUtc >= range.StartUtc
                      && i.CreatedUtc < range.EndExclusiveUtc)
          .OrderByDescending(i => i.CreatedUtc)
          .ThenByDescending(i => i.InvoiceNumber)   // stable paging for bills in the same instant
          .Skip((query.Page - 1) * query.PageSize)
          .Take(query.PageSize + 1)
          .Select(i => new InvoiceSummaryDto(
              i.Id,
              i.InvoiceNumber,
              i.CreatedUtc,
              i.TotalAmount,
              i.Payments.OrderByDescending(p => p.Amount).Select(p => p.Method).FirstOrDefault() ?? "Unknown",
              i.Lines.Count))
          .ToListAsync(cancellationToken);
}
```

- [ ] **Step 9: Implement the detail handler**

`src/IndyPOS.Infrastructure/QueryHandlers/Sales/GetSaleQueryHandler.cs`:

```csharp
using System.Linq.Expressions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.Sales;

/// <summary>
/// One bill of this store, by id or by bill number — same body either way. A bill the caller may
/// not see (another day, no reports.view) comes back as null, exactly like an unknown one.
/// </summary>
/// <remarks>
/// Cashier, category kinds and payment-method names are looked up separately and may be missing:
/// migrated history can reference a deleted user, a placeholder product with no category, or a
/// retired campaign code. None of those may hide the bill.
/// </remarks>
public class GetSaleQueryHandler(
    StoreHubDbContext db,
    IStoreIdentityService storeIdentity,
    ICashDrawerClock clock)
    : IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?>,
      IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?>
{
    public Task<InvoiceDetailDto?> HandleAsync(GetSaleByIdQuery query, CancellationToken cancellationToken = default) =>
        LoadVisibleAsync(i => i.Id == query.InvoiceId, query.CanViewAnyDay, cancellationToken);

    public Task<InvoiceDetailDto?> HandleAsync(GetSaleByNumberQuery query, CancellationToken cancellationToken = default)
    {
        SalesQueryRules.EnsureValidNumber(query.InvoiceNumber);
        return LoadVisibleAsync(i => i.InvoiceNumber == query.InvoiceNumber, query.CanViewAnyDay, cancellationToken);
    }

    private async Task<InvoiceDetailDto?> LoadVisibleAsync(
        Expression<Func<Invoice, bool>> match, bool canViewAnyDay, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
                              .AsNoTracking()
                              .Where(i => i.StoreId == storeIdentity.StoreId)
                              .Where(match)
                              .Include(i => i.Lines)
                              .ThenInclude(l => l.Product)
                              .Include(i => i.Payments)
                              .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null || !IsVisible(invoice, canViewAnyDay))
            return null;

        return await ToDetailAsync(invoice, cancellationToken);
    }

    private bool IsVisible(Invoice invoice, bool canViewAnyDay) =>
        TodayOnlyRule.Allows(
            TodayOnlyRule.BusinessDateOf(invoice.CreatedUtc, storeIdentity.TimeZone),
            clock.Now().BusinessDate,
            canViewAnyDay);

    private async Task<InvoiceDetailDto> ToDetailAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var cashierName = await FindCashierNameAsync(invoice.UserId, cancellationToken);
        var kinds = await FindCategoryKindsAsync(invoice, cancellationToken);
        var methodNames = await FindMethodNamesAsync(invoice, cancellationToken);
        var figures = SaleFigures.From(invoice.TotalAmount, invoice.Payments.Select(p => new SalePayment(p.Method, p.Amount)));

        return new InvoiceDetailDto(
            Id: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            StoreId: invoice.StoreId,
            UserId: invoice.UserId,
            CashierName: cashierName,
            TotalAmount: invoice.TotalAmount,
            AmountReceived: figures.AmountReceived,
            ChangeGiven: figures.ChangeGiven,
            IsRefund: figures.IsRefund,
            HasPayLater: figures.HasPayLater,
            PayLaterAmount: figures.PayLaterAmount,
            CreatedUtc: invoice.CreatedUtc,
            Lines: invoice.Lines.OrderBy(l => l.Priority ?? int.MaxValue)
                                .ThenBy(l => l.Id)
                                .Select(l => ToLineDto(l, kinds))
                                .ToList(),
            Payments: invoice.Payments.OrderBy(p => p.Id)
                                      .Select(p => ToPaymentDto(p, methodNames))
                                      .ToList());
    }

    private async Task<string?> FindCashierNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.StoreUsers
                           .AsNoTracking()
                           .Where(u => u.Id == userId)
                           .Select(u => new { u.FirstName, u.LastName })
                           .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : $"{user.FirstName} {user.LastName}".Trim();
    }

    private Task<Dictionary<string, ProductCategoryKind>> FindCategoryKindsAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var codes = invoice.Lines.Select(l => l.Product?.Category).OfType<string>().Distinct().ToList();

        return db.ProductCategories
                 .AsNoTracking()
                 .Where(c => c.StoreId == invoice.StoreId && codes.Contains(c.Code))
                 .ToDictionaryAsync(c => c.Code, c => c.Kind, cancellationToken);
    }

    private Task<Dictionary<string, string>> FindMethodNamesAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var codes = invoice.Payments.Select(p => p.Method).Distinct().ToList();

        return db.PaymentMethods
                 .AsNoTracking()
                 .Where(m => m.StoreId == invoice.StoreId && codes.Contains(m.Code))
                 .ToDictionaryAsync(m => m.Code, m => m.DisplayName, cancellationToken);
    }

    private static InvoiceLineDto ToLineDto(InvoiceLine line, IReadOnlyDictionary<string, ProductCategoryKind> kinds) =>
        new(Id: line.Id,
            ProductId: line.ProductId,
            ProductName: line.ProductName,
            Barcode: line.Product?.Barcode,
            Note: line.Note,
            CategoryKind: line.Product?.Category is { } code && kinds.TryGetValue(code, out var kind) ? kind : null,
            Quantity: line.Quantity,
            UnitPrice: line.UnitPrice,
            LineTotal: line.LineTotal);

    private static PaymentDto ToPaymentDto(Payment payment, IReadOnlyDictionary<string, string> methodNames) =>
        new(Id: payment.Id,
            Method: payment.Method,
            MethodDisplayName: methodNames.GetValueOrDefault(payment.Method, payment.Method),
            Amount: payment.Amount,
            Note: payment.Note);
}
```

- [ ] **Step 10: Run the handler tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~ListSalesQueryHandlerTests|FullyQualifiedName~GetSaleQueryHandlerTests"`
Expected: PASS (11 + 16).

- [ ] **Step 11: Retire the old invoice queries, handlers, DTOs, registrations and routes**

1. Delete `src/IndyPOS.Application/UseCases/StoreHub/Reports/GetInvoices/GetInvoicesQuery.cs`, `src/IndyPOS.Application/UseCases/StoreHub/Reports/GetInvoiceDetail/GetInvoiceDetailQuery.cs`, `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetInvoicesQueryHandler.cs`, `src/IndyPOS.Infrastructure/QueryHandlers/Reports/GetInvoiceDetailQueryHandler.cs` (`git rm`).
2. In `ReportDtos.cs`, delete the `InvoiceSummaryDto`, `InvoiceDetailDto`, `InvoiceLineDto` and `PaymentDto` records with their summary comments (lines 35-75). `PagedResult<T>` stays (product-sales and pay-later use it).
3. In `Program.cs`, delete lines 21-22 (`using …Reports.GetInvoiceDetail;`, `using …Reports.GetInvoices;`), lines 116-117 (the two `AddTransient` registrations), and lines 593-624 (`// Invoice list (paginated)` through the end of the `/reports/invoices/{invoiceId:guid}` mapping).
4. In `ReportsEndpointTests.cs`, delete the five tests at lines 96-167 (`GetInvoices_WithoutAuth_ReturnsUnauthorized`, `GetInvoices_AsManager_ReturnsPagedResult`, `GetInvoices_WithPagination_RespectsPageSize`, `GetInvoiceDetail_WithoutAuth_ReturnsUnauthorized`, `GetInvoiceDetail_NonExistent_ReturnsNotFound`). Task 7 re-creates each against `/sales`.

Run: `dotnet build && dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~ReportsEndpointTests"`
Expected: build succeeds (no remaining references); remaining report tests PASS.

- [ ] **Step 12: Commit**

```bash
git add -A src/IndyPOS.Application src/IndyPOS.Infrastructure/QueryHandlers src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ReportsEndpointTests.cs
git commit -m "feat(sales): add sale list/detail queries with the today-only rule; retire invoice report queries"
```

---

### Task 7: The `/sales` API — list, by id, by number, malformed numbers

**Files:**
- Create: `src/IndyPOS.StoreHub/Endpoints/Sales/SalesEndpoints.cs`, `SaleQueryEndpoints.cs`, `SalesExceptionFilter.cs`, `SalesServiceCollectionExtensions.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` (using, `AddSalesHistory()`, policy, `MapSalesEndpoints()`)
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/IntegrationTestBase.cs` (move `BuildToken` + two token helpers here)
- Modify: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashAuthorizationTests.cs` (use the base helpers)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesHistoryEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 5 `Capability.SalesReprint`, `HasCapability`, `OtherDayForbiddenException`; Task 6 queries, DTOs, `SalesQueryRules`, `SaleNotFoundException`, `SalesQueryValidationException`, both handlers.
- Produces:
  - `SalesEndpoints.Policy = "CanReprintSales"`; `SalesEndpoints.MapSalesEndpoints(this IEndpointRouteBuilder app)`.
  - `SalesServiceCollectionExtensions.AddSalesHistory(this IServiceCollection services)`.
  - Routes: `GET /sales`, `GET /sales/{id:guid}`, `GET /sales/{number:long}`, `GET /sales/{value}` (malformed → 400).
  - `IntegrationTestBase.BuildToken(IEnumerable<Claim>)`, `TokenWithoutUserId(UserRole role)`, `TokenWithRole(int roleId)` (protected).

- [ ] **Step 1: Move the token helpers to the base class (second use → extract)**

In `IntegrationTestBase.cs` add usings `System.IdentityModel.Tokens.Jwt`, `System.Security.Claims`, `System.Text`, `IndyPOS.Application.Common.Models`, `Microsoft.Extensions.Configuration`, `Microsoft.IdentityModel.Tokens`, and move `BuildToken` from `CashAuthorizationTests.cs` (the method and its doc comment, verbatim) into the base as `protected string BuildToken(IEnumerable<Claim> claims)`. Add:

```csharp
    /// <summary>A token that authenticates with <paramref name="role"/> but carries no user-id claim.</summary>
    protected string TokenWithoutUserId(UserRole role) =>
        BuildToken([new Claim("role_id", ((int)role).ToString()), new Claim("store_id", "test-store")]);

    /// <summary>A fully formed token (user id present) for an arbitrary role id.</summary>
    protected string TokenWithRole(int roleId) =>
        BuildToken(
        [
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role_id", roleId.ToString()),
            new Claim("store_id", "test-store")
        ]);
```

In `CashAuthorizationTests.cs`, delete its private `BuildToken`, `TokenWithoutUserId()` and `TokenWithRole(int)` and change the two `TokenWithoutUserId()` calls to `TokenWithoutUserId(UserRole.Cashier)`. Remove usings that become unused.

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CashAuthorizationTests"`
Expected: PASS (unchanged behaviour).

- [ ] **Step 2: Write the failing endpoint tests**

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SalesHistoryEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// GET /sales, /sales/{id}, /sales/{number}. Includes the tests moved from the retired
/// /reports/invoices routes (spec §6).
/// </summary>
[Collection("Integration")]
public class SalesHistoryEndpointsTests : IntegrationTestBase
{
    /// <summary>Matches <c>SalesReprintCapabilityTests.UnknownRoleId</c> — no role maps it to sales.reprint.</summary>
    private const int RoleWithoutSalesReprint = 99;
    private const int PageSizeAboveMaximum = SalesQueryRules.MaxPageSize + 1;
    private const string OverflowingNumber = "99999999999999999999";

    public SalesHistoryEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Two days back, so a run near midnight cannot land it on "today".</summary>
    private static DateTime TwoDaysAgoUtc => DateTime.UtcNow.AddDays(-2);

    private static string Day(DateTime utc) => DateOnly.FromDateTime(utc.ToLocalTime()).ToString("yyyy-MM-dd");

    private sealed record ErrorBody(string Error);

    private void UseToken(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<CompleteSaleResponse> SellOneAsync()
    {
        var product = await CreateTestProductAsync(unitPrice: 350m, initialStock: 10);
        var seller = await CreateTestUserAsync($"seller_{Guid.NewGuid():N}", "Password123!");
        var response = await Client.PostAsJsonAsync("/sales/complete", new CompleteSaleRequest(
            seller.Id, [new SaleLineRequest(product.Id, 1, 350m)], [new SalePaymentRequest("Cash", 500m)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompleteSaleResponse>(JsonOptions))!;
    }

    // ---- negative: authentication and capability ----

    [Fact]
    public async Task ListSales_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/sales");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSales_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        UseToken(TokenWithoutUserId(UserRole.Cashier));

        var response = await Client.GetAsync("/sales");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSales_WithRoleLackingSalesReprint_ReturnsForbidden()
    {
        UseToken(TokenWithRole(RoleWithoutSalesReprint));

        var response = await Client.GetAsync("/sales");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSaleById_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync($"/sales/{Guid.NewGuid()}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    // ---- negative: today-only ----

    [Fact]
    public async Task ListSales_AsCashierForAnotherDay_ReturnsForbidden()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales?from={Day(TwoDaysAgoUtc)}&to={Day(TwoDaysAgoUtc)}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListSales_AsCashierForAnotherDay_ReturnsAThaiError()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales?from={Day(TwoDaysAgoUtc)}&to={Day(TwoDaysAgoUtc)}");

        (await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions))!.Error.Should()
                                                                          .Contain("วันนี้");
    }

    [Fact]
    public async Task GetSaleByNumber_AsCashierForAnotherDaysBill_ReturnsNotFound()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales/{old.InvoiceNumber}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaleById_AsCashierForAnotherDaysBill_ReturnsNotFound()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales/{old.Id}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    // ---- negative: malformed input ----

    [Fact]
    public async Task ListSales_WithAMalformedFrom_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/sales?from=27/09/2026");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSales_WithToBeforeFrom_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales?from={Today:yyyy-MM-dd}&to={Today.AddDays(-5):yyyy-MM-dd}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListSales_WithPageSizeAboveMaximum_ReturnsBadRequest()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales?pageSize={PageSizeAboveMaximum}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaleByNumber_WithNonNumericValue_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/sales/abc");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    // The /{value} catch-all must stay inside the authorised group: an anonymous caller gets 401,
    // never the 400 that would confirm the route exists.
    [Fact]
    public async Task GetSaleByNumber_WithNonNumericValueWithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/sales/abc");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSaleByNumber_WithOverflowingValue_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync($"/sales/{OverflowingNumber}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaleByNumber_WithNegativeNumber_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/sales/-3");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaleByNumber_WithUnknownNumber_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{long.MaxValue}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaleById_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{Guid.NewGuid()}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReportsInvoices_AfterRetirement_ReturnsNotFound()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/reports/invoices?fromDate={Today:yyyy-MM-dd}&toDate={Today:yyyy-MM-dd}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    // ---- positive ----

    [Fact]
    public async Task ListSales_Always_OmitsAPageTotal()
    {
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync("/sales");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.EnumerateObject().Select(p => p.Name).Should()
                                                              .BeEquivalentTo(["items", "page", "pageSize", "hasMore"]);
    }

    [Fact]
    public async Task ListSales_WithoutPageSize_UsesTheDefault()
    {
        await AuthenticateAsManagerAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>("/sales", JsonOptions);

        page!.PageSize.Should()
                      .Be(SalesQueryRules.DefaultPageSize);
    }

    [Fact]
    public async Task ListSales_WithAPageSize_EchoesIt()
    {
        await AuthenticateAsManagerAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>("/sales?page=1&pageSize=10", JsonOptions);

        page!.PageSize.Should()
                      .Be(10);
    }

    [Fact]
    public async Task ListSales_AsCashierWithoutDates_IncludesTheSaleJustMade()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOneAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>($"/sales?pageSize={SalesQueryRules.MaxPageSize}", JsonOptions);

        page!.Items.Should()
                   .Contain(i => i.Id == sale.InvoiceId);
    }

    [Fact]
    public async Task ListSales_AsManagerForAnotherDay_ReturnsThatDaysBill()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsManagerAsync();

        var page = await Client.GetFromJsonAsync<SalesPage>(
            $"/sales?from={Day(old.CreatedUtc)}&to={Day(old.CreatedUtc)}&pageSize={SalesQueryRules.MaxPageSize}", JsonOptions);

        page!.Items.Should()
                   .Contain(i => i.Id == old.Id);
    }

    [Fact]
    public async Task GetSaleByNumber_WithAKnownNumber_ReturnsTheSameBillAsTheGuidRoute()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOneAsync();

        var byNumber = await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceNumber}", JsonOptions);
        var byId = await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceId}", JsonOptions);

        byNumber.Should()
                .BeEquivalentTo(byId);
    }

    [Fact]
    public async Task GetSaleById_AfterASale_ReturnsTheSellersName()
    {
        await AuthenticateAsCashierAsync();
        var sale = await SellOneAsync();

        var detail = await Client.GetFromJsonAsync<InvoiceDetailDto>($"/sales/{sale.InvoiceId}", JsonOptions);

        detail!.CashierName.Should()
                           .Be("Test User");
    }

    [Fact]
    public async Task GetSaleById_AsManagerForAnotherDaysBill_ReturnsOk()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsManagerAsync();

        var response = await Client.GetAsync($"/sales/{old.Id}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesHistoryEndpointsTests"`
Expected: FAIL — every `/sales` GET returns `404`/`405` (no such routes); `ReportsInvoices_AfterRetirement_ReturnsNotFound` and the `WithoutAuth` tests may already pass.

- [ ] **Step 4: Add the exception filter, the endpoints and the registration**

`src/IndyPOS.StoreHub/Endpoints/Sales/SalesExceptionFilter.cs`:

```csharp
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>Maps /sales rule failures to HTTP, in the { error } shape every StoreHub route uses.</summary>
internal sealed class SalesExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (SalesQueryValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (SaleNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (OtherDayForbiddenException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Sales/SaleQueryEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.StoreHub.Endpoints.Cash;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

internal static class SaleQueryEndpoints
{
    public static void MapSaleQueries(this RouteGroupBuilder sales)
    {
        sales.MapGet("", async (
            IQueryHandler<ListSalesQuery, SalesPage> handler,
            ClaimsPrincipal user,
            string? from,
            string? to,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            // Dates bind as strings so a malformed one is our Thai 400, not the framework's bare one.
            var query = new ListSalesQuery(
                From: SalesQueryRules.ParseDate(from),
                To: SalesQueryRules.ParseDate(to),
                Page: page ?? SalesQueryRules.FirstPage,
                PageSize: pageSize ?? SalesQueryRules.DefaultPageSize,
                CanViewAnyDay: CanViewAnyDay(user));

            return Results.Ok(await handler.HandleAsync(query, cancellationToken));
        });

        sales.MapGet("/{id:guid}", async (
            IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetSaleByIdQuery(id, CanViewAnyDay(user)), cancellationToken)
                       ?? throw new SaleNotFoundException()));

        sales.MapGet("/{number:long}", async (
            IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?> handler,
            ClaimsPrincipal user,
            long number,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetSaleByNumberQuery(number, CanViewAnyDay(user)), cancellationToken)
                       ?? throw new SaleNotFoundException(number)));

        // Typed constraints alone answer "/sales/abc" or an overflowing number with 404 (no route
        // matched). An unconstrained parameter has LOWER routing precedence than the two above, so
        // it only catches what they reject, and turns it into the 400 the spec asks for.
        sales.MapGet("/{value}", (string value) =>
            Results.BadRequest(new { error = SalesQueryRules.InvalidNumberMessage(value) }));
    }

    private static bool CanViewAnyDay(ClaimsPrincipal user) => user.HasCapability(Capability.ReportsView);
}
```

`src/IndyPOS.StoreHub/Endpoints/Sales/SalesEndpoints.cs`:

```csharp
using IndyPOS.StoreHub.Endpoints.Cash;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>
/// Bills as one REST resource (spec §6). sales.reprint opens it; reports.view lifts the today-only
/// limit. POST /sales/complete is still mapped in Program.cs — renaming it is the route tidy-up PR.
/// </summary>
public static class SalesEndpoints
{
    public const string Policy = "CanReprintSales";

    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var sales = app.MapGroup("/sales")
                       .RequireAuthorization(Policy)
                       .AddEndpointFilter<RequireUserIdFilter>()
                       .AddEndpointFilter<SalesExceptionFilter>();

        sales.MapSaleQueries();
        return app;
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Sales/SalesServiceCollectionExtensions.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Infrastructure.QueryHandlers.Sales;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>Registers the /sales handlers. "Today" comes from the cash drawer's clock (spec §5).</summary>
public static class SalesServiceCollectionExtensions
{
    public static IServiceCollection AddSalesHistory(this IServiceCollection services)
    {
        // TryAdd: AddCashDrawer registers the same clock; either order works.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICashDrawerClock, CashDrawerClock>();

        services.AddTransient<IQueryHandler<ListSalesQuery, SalesPage>, ListSalesQueryHandler>();
        services.AddTransient<IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?>, GetSaleQueryHandler>();
        services.AddTransient<IQueryHandler<GetSaleByNumberQuery, InvoiceDetailDto?>, GetSaleQueryHandler>();
        return services;
    }
}
```

- [ ] **Step 5: Wire it into `Program.cs`**

1. Add `using IndyPOS.StoreHub.Endpoints.Sales;` after `using IndyPOS.StoreHub.Endpoints.Cash;`.
2. After `builder.Services.AddCashDrawer();` add:

```csharp

// Sales history (/sales): list, detail, reprint
builder.Services.AddSalesHistory();
```

3. In the `AddAuthorizationBuilder()` chain, replace the final `.AddPolicy(CashEndpoints.Policy, …);` with:

```csharp
    .AddPolicy(CashEndpoints.Policy, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.CashManage)))
    .AddPolicy(SalesEndpoints.Policy, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.SalesReprint)));
```

4. After `app.MapCashEndpoints();` add:

```csharp

// Sales history routes (/sales/...). POST /sales/complete above is unchanged.
app.MapSalesEndpoints();
```

- [ ] **Step 6: Run the endpoint tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SalesHistoryEndpointsTests|FullyQualifiedName~SalesEndpointTests|FullyQualifiedName~CashAuthorizationTests"`
Expected: PASS (25 new; `POST /sales/complete` tests unchanged).

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.StoreHub tests/IndyPOS.StoreHub.IntegrationTests
git commit -m "feat(sales): serve bills from GET /sales, /sales/{id} and /sales/{number}"
```

---

### Task 8: Reprints — `invoice_reprint` row and `InvoiceReprinted` event in one save

**Files:**
- Create: `src/IndyPOS.Domain/Entities/Core/InvoiceReprint.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/InvoiceReprintConfiguration.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs` (add `DbSet`)
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IInvoiceReprintRepository.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/InvoiceReprintRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs:66-76` (register)
- Create: `src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceReprintedEvent.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Sales/Reprints/CreateInvoiceReprintCommand.cs`, `CreateInvoiceReprintCommandHandler.cs`, `InvoiceReprintDtos.cs`, `InvoiceReprintOutbox.cs`
- Create: `src/IndyPOS.StoreHub/Endpoints/Sales/SaleReprintEndpoints.cs`
- Modify: `src/IndyPOS.StoreHub/Endpoints/Sales/SalesEndpoints.cs`, `SalesServiceCollectionExtensions.cs`
- Generate: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<timestamp>_AddInvoiceReprintTable.*`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/CreateInvoiceReprintCommandHandlerTests.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SaleReprintEndpointsTests.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/InvoiceReprintPersistenceTests.cs`

**Interfaces:**
- Consumes: Task 5 `TodayOnlyRule` (via Task 6 handler), `HasCapability`, `GetRequiredUserId`; Task 6 `GetSaleByIdQuery`, `InvoiceDetailDto`, `SaleNotFoundException`; `ICashDrawerClock`.
- Produces:
  - `InvoiceReprint { Guid Id; Guid InvoiceId; string StoreId; DateTime CreatedUtc; DateTime LastModifiedUtc; Guid CreatedByUserId; }` → table `invoice_reprint`.
  - `IInvoiceReprintRepository.AddAsync(InvoiceReprint reprint, OutboxEvent outboxEvent, CancellationToken cancellationToken = default) : Task` (the only method — append-only).
  - `InvoiceReprintOutbox.InvoiceReprinted = "InvoiceReprinted"`; `InvoiceReprintOutbox.Reprinted(InvoiceReprint reprint) : OutboxEvent`.
  - `InvoiceReprintedEvent { int SchemaVersion = 1; Guid EventId; Guid ReprintId; Guid InvoiceId; string StoreId; DateTime CreatedUtc; Guid CreatedByUserId; }` — plan 2's cloud handler reads this.
  - `CreateInvoiceReprintCommand(Guid InvoiceId, Guid UserId, bool CanViewAnyDay) : ICommand<InvoiceReprintResultDto>`.
  - `InvoiceReprintDto(Guid Id, Guid InvoiceId, DateTime CreatedUtc, Guid CreatedByUserId)`; `InvoiceReprintResultDto(InvoiceDetailDto Sale, InvoiceReprintDto Reprint)`.
  - Route `POST /sales/{id:guid}/reprints` → `201` + `InvoiceReprintResultDto`.

- [ ] **Step 1: Write the failing unit tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/Sales/CreateInvoiceReprintCommandHandlerTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static IndyPOS.Application.Tests.UseCases.StoreHub.Sales.SalesHistoryTestContext;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.Sales;

public class CreateInvoiceReprintCommandHandlerTests
{
    private const long KnownNumber = 7001;
    private static readonly Guid ReprinterId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static CreateInvoiceReprintCommandHandler Handler(SalesHistoryTestContext c) =>
        new(c.DetailHandler(), new InvoiceReprintRepository(c.Db), c.Clock);

    private static CreateInvoiceReprintCommand TodayOnly(Guid invoiceId) => new(invoiceId, ReprinterId, CanViewAnyDay: false);

    [Fact]
    public async Task Handle_WithAnUnknownInvoice_ThrowsNotFound()
    {
        await using var c = new SalesHistoryTestContext();

        var act = () => Handler(c).HandleAsync(TodayOnly(Guid.NewGuid()));

        await act.Should()
                 .ThrowAsync<SaleNotFoundException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterdaysBill_ThrowsNotFound()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        var act = () => Handler(c).HandleAsync(TodayOnly(invoice.Id));

        await act.Should()
                 .ThrowAsync<SaleNotFoundException>();
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterdaysBill_WritesNoRow()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        try { await Handler(c).HandleAsync(TodayOnly(invoice.Id)); }
        catch (SaleNotFoundException) { }

        (await c.Db.InvoiceReprints.CountAsync()).Should()
                                                 .Be(0);
    }

    [Fact]
    public async Task Handle_AsTodayOnlyCallerForYesterdaysBill_WritesNoEvent()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        try { await Handler(c).HandleAsync(TodayOnly(invoice.Id)); }
        catch (SaleNotFoundException) { }

        (await c.Db.OutboxEvents.CountAsync()).Should()
                                              .Be(0);
    }

    [Fact]
    public async Task Handle_Twice_WritesTwoRows()
    {
        // A printer failure after the first leaves its record; pressing reprint again adds one.
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));
        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        (await c.Db.InvoiceReprints.CountAsync()).Should()
                                                 .Be(2);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_WritesOneInvoiceReprintedEvent()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        (await c.Db.OutboxEvents.SingleAsync()).Type.Should()
                                               .Be(InvoiceReprintOutbox.InvoiceReprinted);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_PutsTheReprintIdInThePayload()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var result = await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        var outbox = await c.Db.OutboxEvents.SingleAsync();
        JsonSerializer.Deserialize<InvoiceReprintedEvent>(outbox.PayloadJson)!.ReprintId.Should()
                                                                            .Be(result.Reprint.Id);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_StampsTheCallerAsReprinter()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        (await c.Db.InvoiceReprints.SingleAsync()).CreatedByUserId.Should()
                                                  .Be(ReprinterId);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_SetsLastModifiedToCreated()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        var row = await c.Db.InvoiceReprints.SingleAsync();
        row.LastModifiedUtc.Should()
                           .Be(row.CreatedUtc);
    }

    [Fact]
    public async Task Handle_WithTodaysBill_ReturnsTheBillDetail()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmTodayUtc));

        var result = await Handler(c).HandleAsync(TodayOnly(invoice.Id));

        result.Sale.InvoiceNumber.Should()
                                 .Be(KnownNumber);
    }

    [Fact]
    public async Task Handle_AsAnyDayCallerForYesterdaysBill_WritesOneRow()
    {
        await using var c = new SalesHistoryTestContext();
        var invoice = await c.SeedInvoiceAsync(new InvoiceSeed(KnownNumber, NineAmYesterdayUtc));

        await Handler(c).HandleAsync(TodayOnly(invoice.Id) with { CanViewAnyDay = true });

        (await c.Db.InvoiceReprints.CountAsync()).Should()
                                                 .Be(1);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CreateInvoiceReprintCommandHandlerTests"`
Expected: build FAILS — `InvoiceReprint`, `InvoiceReprints`, the `Reprints` namespace and `InvoiceReprintRepository` do not exist.

- [ ] **Step 3: Add the entity, mapping and repository**

`src/IndyPOS.Domain/Entities/Core/InvoiceReprint.cs`:

```csharp
namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// One reprint REQUESTED for a bill. Append-only and immutable: a printer failure afterwards leaves
/// the row in place, and pressing reprint again adds a second — the owner sees both attempts.
/// </summary>
public class InvoiceReprint
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string StoreId { get; set; } = default!;

    /// <summary>When the reprint was requested.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Equals CreatedUtc: kept for the repo-wide entity convention; a reprint never changes.</summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>Who reprinted — from the token, never the request body.</summary>
    public Guid CreatedByUserId { get; set; }
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/InvoiceReprintConfiguration.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class InvoiceReprintConfiguration : IEntityTypeConfiguration<InvoiceReprint>
{
    public void Configure(EntityTypeBuilder<InvoiceReprint> builder)
    {
        builder.ToTable("invoice_reprint");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        // Restrict: an audit row must never vanish with its bill.
        builder.HasOne<Invoice>()
               .WithMany()
               .HasForeignKey(e => e.InvoiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.InvoiceId);
        builder.HasIndex(e => new { e.StoreId, e.CreatedUtc });
    }
}
```

In `StoreHubDbContext.cs`, after `public DbSet<CashCount> CashCounts => Set<CashCount>();` add:

```csharp
    public DbSet<InvoiceReprint> InvoiceReprints => Set<InvoiceReprint>();
```

`src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IInvoiceReprintRepository.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>Append-only: a reprint record is added, never updated or deleted — so there is no method to.</summary>
public interface IInvoiceReprintRepository
{
    /// <summary>Writes the row and its outbox event in ONE SaveChangesAsync.</summary>
    Task AddAsync(InvoiceReprint reprint, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/InvoiceReprintRepository.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class InvoiceReprintRepository(StoreHubDbContext db) : IInvoiceReprintRepository
{
    public async Task AddAsync(InvoiceReprint reprint, OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.InvoiceReprints.Add(reprint);
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

In `ConfigureServices.cs` (tab-indented), after `.AddScoped<ICashCountRepository, CashCountRepository>()` add:

```csharp
		        .AddScoped<IInvoiceReprintRepository, InvoiceReprintRepository>()
```

- [ ] **Step 4: Add the event, DTOs, outbox builder, command and handler**

`src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceReprintedEvent.cs`:

```csharp
namespace IndyPOS.Application.UseCases.Cloud.Sync.Events;

/// <summary>A reprint was requested. The cloud mirrors it insert-only (plan 2).</summary>
public record InvoiceReprintedEvent
{
    public int SchemaVersion { get; init; } = 1;
    public Guid EventId { get; init; }
    public Guid ReprintId { get; init; }
    public Guid InvoiceId { get; init; }
    public string StoreId { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public Guid CreatedByUserId { get; init; }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/Sales/Reprints/InvoiceReprintDtos.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <summary>Who reprinted and when. The till prints "พิมพ์ซ้ำ … โดย &lt;name&gt;" with the logged-in user's name.</summary>
public record InvoiceReprintDto(Guid Id, Guid InvoiceId, DateTime CreatedUtc, Guid CreatedByUserId);

/// <summary>The 201 body: the bill to print as a copy, and the record just written.</summary>
public record InvoiceReprintResultDto(InvoiceDetailDto Sale, InvoiceReprintDto Reprint);

public static class InvoiceReprintMapping
{
    public static InvoiceReprintDto ToDto(this InvoiceReprint reprint) =>
        new(reprint.Id, reprint.InvoiceId, reprint.CreatedUtc, reprint.CreatedByUserId);
}
```

`src/IndyPOS.Application/UseCases/StoreHub/Sales/Reprints/InvoiceReprintOutbox.cs`:

```csharp
using System.Text.Json;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <summary>Builds the InvoiceReprinted sync event for a reprint row.</summary>
public static class InvoiceReprintOutbox
{
    public const string InvoiceReprinted = "InvoiceReprinted";

    public static OutboxEvent Reprinted(InvoiceReprint reprint)
    {
        var eventId = Guid.NewGuid();
        var payload = new InvoiceReprintedEvent
        {
            EventId = eventId,
            ReprintId = reprint.Id,
            InvoiceId = reprint.InvoiceId,
            StoreId = reprint.StoreId,
            CreatedUtc = reprint.CreatedUtc,
            CreatedByUserId = reprint.CreatedByUserId
        };

        return new OutboxEvent
        {
            Id = eventId,
            StoreId = reprint.StoreId,
            Type = InvoiceReprinted,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedUtc = reprint.CreatedUtc,
            Status = "Pending"
        };
    }
}
```

`CreateInvoiceReprintCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <param name="UserId">From the token, never the body.</param>
/// <param name="CanViewAnyDay">reports.view: managers reprint any day, cashiers only today.</param>
public record CreateInvoiceReprintCommand(Guid InvoiceId, Guid UserId, bool CanViewAnyDay) : ICommand<InvoiceReprintResultDto>;
```

`CreateInvoiceReprintCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.Sales.History;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <summary>
/// Records a reprint request. The visibility rule runs BEFORE the write, through the same detail
/// query the GET uses: a bill the caller cannot see is a 404 and leaves no row and no event.
/// </summary>
public class CreateInvoiceReprintCommandHandler(
    IQueryHandler<GetSaleByIdQuery, InvoiceDetailDto?> sales,
    IInvoiceReprintRepository repository,
    ICashDrawerClock clock) : ICommandHandler<CreateInvoiceReprintCommand, InvoiceReprintResultDto>
{
    public async Task<InvoiceReprintResultDto> HandleAsync(CreateInvoiceReprintCommand command, CancellationToken cancellationToken = default)
    {
        var sale = await sales.HandleAsync(new GetSaleByIdQuery(command.InvoiceId, command.CanViewAnyDay), cancellationToken)
                   ?? throw new SaleNotFoundException();

        var now = clock.Now().Utc;
        var reprint = new InvoiceReprint
        {
            Id = Guid.NewGuid(),
            InvoiceId = sale.Id,
            StoreId = sale.StoreId,
            CreatedUtc = now,
            LastModifiedUtc = now,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(reprint, InvoiceReprintOutbox.Reprinted(reprint), cancellationToken);
        return new InvoiceReprintResultDto(sale, reprint.ToDto());
    }
}
```

- [ ] **Step 5: Run the unit tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CreateInvoiceReprintCommandHandlerTests"`
Expected: PASS (11).

- [ ] **Step 6: Generate and check the migration**

Run:
```bash
dotnet ef migrations add AddInvoiceReprintTable --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --output-dir Persistence/StoreHub/Migrations
dotnet ef migrations has-pending-model-changes --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext
```
Expected: the generated `Up` contains only `CreateTable("invoice_reprint")` with the FK to `invoice` and two `CreateIndex` calls — **no** `AlterColumn`/`AddColumn`/`DropX` on any existing table (forward-only). Second command: `No changes have been made to the model since the last migration.`

- [ ] **Step 7: Write the failing endpoint and persistence tests**

`tests/IndyPOS.StoreHub.IntegrationTests/InvoiceReprintPersistenceTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class InvoiceReprintPersistenceTests : IntegrationTestBase
{
    public InvoiceReprintPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Insert_ForAnUnknownInvoice_ThrowsForeignKeyViolation()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        db.InvoiceReprints.Add(new InvoiceReprint
        {
            Id = Guid.NewGuid(), InvoiceId = Guid.NewGuid(), StoreId = TestStoreIdentityService.TestStoreId,
            CreatedUtc = DateTime.UtcNow, LastModifiedUtc = DateTime.UtcNow, CreatedByUserId = Guid.NewGuid()
        });

        var act = () => db.SaveChangesAsync();

        await act.Should()
                 .ThrowAsync<DbUpdateException>();
    }
}
```

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/SaleReprintEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class SaleReprintEndpointsTests : IntegrationTestBase
{
    public SaleReprintEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static DateTime TwoDaysAgoUtc => DateTime.UtcNow.AddDays(-2);

    private Task<HttpResponseMessage> ReprintAsync(Guid invoiceId) =>
        Client.PostAsync($"/sales/{invoiceId}/reprints", content: null);

    private async Task<int> ReprintRowsForAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        return await db.InvoiceReprints.CountAsync(r => r.InvoiceId == invoiceId);
    }

    private async Task<int> ReprintEventsForAsync(Guid invoiceId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        return await db.OutboxEvents.CountAsync(e =>
            e.Type == InvoiceReprintOutbox.InvoiceReprinted && e.PayloadJson.Contains(invoiceId.ToString()));
    }

    [Fact]
    public async Task Reprint_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await ReprintAsync(Guid.NewGuid());

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reprint_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutUserId(UserRole.Cashier));

        var response = await ReprintAsync(today.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reprint_WithAnUnknownInvoice_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await ReprintAsync(Guid.NewGuid());

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reprint_AsCashierForAnotherDaysBill_ReturnsNotFound()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        var response = await ReprintAsync(old.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reprint_AsCashierForAnotherDaysBill_WritesNoRow()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(old.Id);

        (await ReprintRowsForAsync(old.Id)).Should()
                                           .Be(0);
    }

    [Fact]
    public async Task Reprint_AsCashierForAnotherDaysBill_WritesNoEvent()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(old.Id);

        (await ReprintEventsForAsync(old.Id)).Should()
                                             .Be(0);
    }

    [Fact]
    public async Task Reprint_AsCashierForTodaysBill_ReturnsCreated()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();

        var response = await ReprintAsync(today.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Reprint_AsCashierForTodaysBill_WritesExactlyOneRow()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(today.Id);

        (await ReprintRowsForAsync(today.Id)).Should()
                                             .Be(1);
    }

    [Fact]
    public async Task Reprint_AsCashierForTodaysBill_WritesExactlyOneEvent()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();

        await ReprintAsync(today.Id);

        (await ReprintEventsForAsync(today.Id)).Should()
                                               .Be(1);
    }

    [Fact]
    public async Task Reprint_AsCashier_RecordsTheTokenUserAsReprinter()
    {
        var today = await SeedInvoiceAsync(DateTime.UtcNow);
        await AuthenticateAsCashierAsync();
        var me = (await Client.GetFromJsonAsync<MeBody>("/auth/me", JsonOptions))!;

        var result = await (await ReprintAsync(today.Id)).Content.ReadFromJsonAsync<InvoiceReprintResultDto>(JsonOptions);

        result!.Reprint.CreatedByUserId.Should()
                                       .Be(Guid.Parse(me.UserId));
    }

    [Fact]
    public async Task Reprint_AsManagerForAnotherDaysBill_ReturnsCreated()
    {
        var old = await SeedInvoiceAsync(TwoDaysAgoUtc);
        await AuthenticateAsManagerAsync();

        var response = await ReprintAsync(old.Id);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
    }

    private sealed record MeBody(string UserId);
}
```

- [ ] **Step 8: Run to verify the endpoint tests fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SaleReprintEndpointsTests|FullyQualifiedName~InvoiceReprintPersistenceTests"`
Expected: the persistence test PASSES (EnsureCreated builds the new table); reprint tests FAIL — `POST /sales/{id}/reprints` returns `404`/`405` (no route).

- [ ] **Step 9: Map the route and register the handler**

`src/IndyPOS.StoreHub/Endpoints/Sales/SaleReprintEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;
using IndyPOS.StoreHub.Endpoints.Cash;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>A reprint is a created record (201), not an action verb — spec §6.</summary>
internal static class SaleReprintEndpoints
{
    public static void MapSaleReprints(this RouteGroupBuilder sales) =>
        sales.MapPost("/{id:guid}/reprints", async (
            ICommandHandler<CreateInvoiceReprintCommand, InvoiceReprintResultDto> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateInvoiceReprintCommand(id, user.GetRequiredUserId(), user.HasCapability(Capability.ReportsView));
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/sales/{id}/reprints/{result.Reprint.Id}", result);
        });
}
```

In `SalesEndpoints.cs`, after `sales.MapSaleQueries();` add `sales.MapSaleReprints();`.

In `SalesServiceCollectionExtensions.cs`, add `using IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;` and before `return services;`:

```csharp
        services.AddTransient<ICommandHandler<CreateInvoiceReprintCommand, InvoiceReprintResultDto>, CreateInvoiceReprintCommandHandler>();
```

- [ ] **Step 10: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~SaleReprintEndpointsTests|FullyQualifiedName~InvoiceReprintPersistenceTests|FullyQualifiedName~SalesHistoryEndpointsTests"`
Expected: PASS (11 + 1 + Task 7's 25).

- [ ] **Step 11: Commit**

```bash
git add src tests
git commit -m "feat(sales): record reprints with an InvoiceReprinted event in one save"
```

---

### Task 9: Release-gate check and docs

**Files:**
- Modify: `docs/operations/upgrade-procedure.md` (result paragraph under "Verifying the forward-only gate before a release" + Change Log row)
- Modify: `CLAUDE.md` (the "Solution suites total …" paragraph and the Docker-down comment block), `ONBOARDING.md` (the two suite tables)

- [ ] **Step 1: Run the forward-only gate recipe**

Follow `docs/operations/upgrade-procedure.md` §"Verifying the forward-only gate before a release" against a throwaway `postgres:16-alpine` (`gate` container, port 55510), exactly as the cash-drawer result describes:
1. `dotnet ef database update 20260926152602_AddCashDrawerTables --project src/IndyPOS.Infrastructure --startup-project src/IndyPOS.StoreHub --context StoreHubDbContext --connection "Host=localhost;Port=55510;Database=storehub;Username=postgres;Password=pass"`; snapshot `information_schema.columns`.
2. Write a few invoices with pre-release columns, some with `legacy_invoice_id` and some without (so the backfill runs on real rows).
3. `dotnet ef database update` (latest); snapshot again and diff. Expected difference, and nothing else: `invoice.invoice_number bigint NOT NULL DEFAULT nextval('invoice_number_seq'::regclass)` and the six columns of the new `invoice_reprint` table.
4. Write a complete sale — `product` → `invoice` → `invoice_line` → `payment` → `inventory_movement` — in one transaction, naming only the previous release's columns. Then `SELECT invoice_number FROM invoice WHERE id = '<that id>'` — it must be non-null and above every backfilled number.
5. `SELECT store_id, invoice_number, count(*) FROM invoice GROUP BY 1, 2 HAVING count(*) > 1` returns no rows.

If step 4 fails, **stop: the release must not ship** (CLAUDE.md gate).

- [ ] **Step 2: Record the result**

Append to the gate section of `docs/operations/upgrade-procedure.md`, filling in the date you ran it:

```markdown
Result for the invoice-history release (2 migrations, `AddInvoiceNumber` and `AddInvoiceReprintTable`):
`invoice` gained one column, `invoice_number bigint NOT NULL DEFAULT nextval('invoice_number_seq')`,
backfilled in the migration (legacy ids first, then `setval`, then native rows by `created_utc`); one
new table, `invoice_reprint`. No other column changed. A complete sale written with only pre-release
columns succeeded and its invoice row took a number from the default, above every backfilled one, with
no duplicate `(store_id, invoice_number)`. The column is `NOT NULL`, which the gate allows because it
has a default. Verified <date> with `postgres:16-alpine` in a throwaway `gate` container on port 55510.
```

Add a Change Log row: `| <date> | Ran the gate against the invoice-history release's 2 migrations (AddInvoiceNumber, AddInvoiceReprintTable) |`.

- [ ] **Step 3: Run the whole solution and update the counts**

Run (Docker running; real store DBs present if you have them): `dotnet test`
Expected: all green except the known skip. Update from the **measured** output — never derived — the "Solution suites total **N** …" paragraph and per-suite list in `CLAUDE.md`, and both suite tables in `ONBOARDING.md`. Suites that changed in this plan: Application, StoreHub.IntegrationTests, MigrationTool. If you cannot re-measure the Docker-down split, mark the StoreHub "fails without Docker" figure **derived** and say so, as the existing text does.

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md ONBOARDING.md docs/operations/upgrade-procedure.md
git commit -m "docs: record invoice-history forward-only gate result and test counts"
```

---

## Self-Review

**Spec coverage** (spec § → task):
- §1 `0000000000` receipts → the number reaches the sale response in Task 2; the printer change itself is **plan 3**.
- §4 schema, sequence, ordered backfill, unique `(store_id, invoice_number)`, `InvoiceNumber` long → Task 1. Sale response → Task 2. MigrationTool number = legacy id, native refusal (with the "does today's rule guarantee it?" answer: **no**), `setval` in the transaction → Task 3. `verify` checks → Task 4. Forward-only gate → Task 1 (`Insert_WithOnlyPreReleaseColumns_GetsANumberFromTheDefault`) + Task 9.
- §5 `sales.reprint` for three roles, today from `ICashDrawerClock`, list → 403, bill → 404, reprint limits by role, every `/cash` read's past date → 403 with regression tests, 401/403/Thai bodies → Tasks 5, 6, 7, 8.
- §6 `GET /sales` (defaults, 50/200, newest first, no page total), `GET /sales/{id:guid}`, `GET /sales/{number:long}`, `POST /sales/{id:guid}/reprints` (201 + detail + record), detail fields checked against `ReceiptPrinterService` (table in Task 6), retirement of the two `/reports/invoices` routes with tests moved → Tasks 6, 7, 8. `invoice_reprint` append-only + `InvoiceReprinted` in one save → Task 8. `POST /sales/complete` rename → **not done** (route tidy-up PR), as the spec says.
- §7, §8 → **plan 3**. §9 StoreHub side: `InvoiceCompleted` carries the number → Task 2; `MigratedInvoice.InvoiceNumber` → Task 3; `InvoiceReprinted` event → Task 8. Cloud side (mirror column/table, handler, ordering guard, `ProcessedEvents`, `HasComment`, bulk handler storing the number) → **plan 2**.
- §10 server tests → Tasks 5-8; bill-number tests (concurrency, gate, backfill order, MigrationTool) → Tasks 1-4. Client/ViewModel/receipt/cloud tests → plans 2-3.
- §11 → not code; this plan changes MigrationTool, so it must land before the Epic 3 Phase A rehearsal.

**Deviations from the spec, called out:**
- **D1 — the sale handler reserves its number** (approved by Pond 2026-09-29; spec §4 reworded to match) (`SELECT nextval('invoice_number_seq')`) and sets `Invoice.InvoiceNumber` before the save. The spec says the property is "never set by application code" and "no C# code picks a number". The database still picks it (same sequence, so no clash between tills), but C# does set the property. The spec's own requirements force this: `InvoiceCompleted` must carry the number (§9), and the row and event are one `SaveChangesAsync`, but the handler serialises the payload *before* the save. The alternatives were two saves (breaks the outbox guarantee) or an explicit transaction (retry-strategy risk, and EF's change tracker cannot re-run it). `ValueGeneratedOnAdd` is kept, so any insert that leaves the number at 0 (the MigrationTool before Task 3, the tests, restored older binaries) still gets it from the default.
- **D2 — the column ends `NOT NULL`**, not `NULL` (approved by Pond 2026-09-29; spec §4 reworded to match). EF cannot map a non-nullable `long` to an optional column (`IsRequired(false)` throws for value types), and the spec also wants the domain type to be `long`. `NOT NULL` with a default passes the forward-only gate (CLAUDE.md: "No new `NOT NULL` column without a default"). The spec's backfill guarantees the column is never empty anyway.
- **D3 — detail adds `PaymentDto.MethodDisplayName`.** The printer prints a payment *name* (`ReceiptPrinterService.cs:274`), and v4 stores only the code. It falls back to the code for a method no longer in the catalogue.
- **D4 — the native-invoice refusal is per store**, not per database (approved by Pond 2026-09-29, both the refusal itself and its per-store scope; the Epic 3 runbook gains a "migrate before the first v4 sale" line). Bill numbers are unique per store, so another store's v4 sales cannot collide. This matches the existing `AlreadyMigrated` guard and the two-stores-one-database test.
- **D5 — `setval` runs after the final `SaveChangesAsync`, just before `CommitAsync`**, not literally at the end of `MigrateInvoicesAsync`. The last batch of invoices only reaches the database at that save (`SqliteMigrationService.cs:118`). It is still inside the transaction.
- **D6 — "no page total"** is read as no money total *and* no `TotalCount`/`TotalPages`: `SalesPage` has `HasMore` instead, fetched with page + 1 rows. The test pins the exact JSON property set.
- **D7 — a lower-precedence `GET /sales/{value}` route** turns a malformed number into `400` (approved by Pond 2026-09-29; Task 7 gains `GetSaleByNumber_WithNonNumericValueWithoutAuth_ReturnsUnauthorized` to pin it inside the authorised group). With typed constraints alone it would be `404`.
- **D8 — `InvoiceCompletedEvent.InvoiceNumber` is `long?`**, so events queued before the upgrade read as null, not as the magic value 0.
- **D9 — the reprint record returns `CreatedByUserId`, not a name.** The reprinter is always the caller, so plan 3 prints the logged-in user's name.
- **D10 — the shared helpers stay in `Endpoints/Cash/`.** `RequireUserIdFilter` and `ClaimsPrincipalExtensions` (+ `HasCapability`) are reused by `/sales` through a `using`. Moving them is left to the route tidy-up PR, which moves the rest of `Program.cs`.
- **D11 — the new handlers scope to `storeIdentity.StoreId`.** The retired ones did not, and bill numbers are only unique per store.

**Placeholder scan:** none left. The two `<timestamp>` file names are generated by `dotnet ef`. `<date>` in Task 9 is filled in when the gate is run. Figures checked by hand: backfill 7005 → natives 7006, 7007 → next 7008. Migrated numbers 6999/7001 → next sale 7002. Tampered legacy invoice 3 + 100000 = 100003. Figures ฿500 − ฿350 = ฿150 change.

**Type consistency:** `ReserveInvoiceNumberAsync` (Tasks 2). `InvoiceNumberSequence.Name` (Tasks 1-4). `TodayOnlyRule.Allows/EnsureAllowed/BusinessDateOf` (Tasks 5, 6, 7). `SalesQueryRules.{FirstPage, DefaultPageSize, MaxPageSize, ParseDate, EnsureValidRange, EnsureValidPage, EnsureValidNumber, InvalidNumberMessage}` (Tasks 6, 7). `ListSalesQuery`/`GetSaleByIdQuery`/`GetSaleByNumberQuery` with `CanViewAnyDay` (Tasks 6, 7, 8). `InvoiceDetailDto`/`InvoiceSummaryDto`/`SalesPage` in `Sales.History` (Tasks 6, 7, 8). `InvoiceReprintOutbox.InvoiceReprinted` (Task 8 tests + code). `SalesEndpoints.Policy = "CanReprintSales"` (Task 7 Program.cs). `SeedInvoiceAsync` (Task 1 base, used in Tasks 7, 8). `TokenWithoutUserId(UserRole)`/`TokenWithRole(int)` (Task 7 base, used in Tasks 7, 8).

**Review Focus check:** all five lines have tests in their owning tasks (listed in the section). Also considered and covered in tasks rather than listed there: concurrent sales (Task 2), two reprints (Task 8), an empty invoice table at migration time (Task 1), a lower-case PayLater code (Task 6), a same-instant paging tie (Task 6 `ThenByDescending(InvoiceNumber)`).

**Found while planning, outside this plan's scope (flag, don't fix here):**
- ~~`GET /cash/counts|payouts|floats|debt-repayments?businessDate=` still return past days to a cashier.~~ **Pulled into Task 5** in plan review (Pond, 2026-09-29): one group filter closes all five reads.
- `POST /sales/complete` takes `UserId` from the **body** (`Program.cs:545-549`), against the "user id from the token" rule. This belongs to the route tidy-up PR.
- v4 sales never write a `pay_later` row (only the MigrationTool does), and `SaleLineRequest` has no `Note`. So for v4 bills the PayLater marker comes from payments, and a line note is null.
