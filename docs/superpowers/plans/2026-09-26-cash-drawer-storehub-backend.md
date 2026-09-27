# Cash Drawer — StoreHub Backend Implementation Plan (1 of 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist the four hand-typed cash-drawer inputs (payouts, cash floats, debt repayments, cash counts) in StoreHub PostgreSQL behind a `/cash` API, write an outbox event for every change, and serve one summary that does all the cash maths.

**Architecture:** Four new Domain entities (three share a `CashDrawerEntry` base) mapped to four new tables by one additive EF migration; soft-delete is an EF 10 **named global query filter**. Application holds the commands/queries, the Bangkok business-date clock, the validation rules and the pure cash formula. A new `/cash` route group in StoreHub, gated once by a new `cash.manage` capability, maps exceptions to `400/404/409` in one endpoint filter and takes the acting user from the token.

**Tech Stack:** C# .NET 10, EF Core 10 (Npgsql), ASP.NET Core minimal APIs, Nokpirab CQRS (`ICommand`/`IQuery`/handlers), xUnit + FluentAssertions + Moq, EF InMemory (unit), Testcontainers PostgreSQL (integration), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`).

**Spec:** `docs/superpowers/specs/2026-09-20-cash-payout-float-persistence-design.md` — read it alongside this plan; the plan argues from it.

**This is plan 1 of 3.** Plan 2 = CloudApi mirror tables + event handlers (spec §7 cloud side). Plan 3 = `IndyPOS.Presentation` + `CashDrawerViewModel` + WinForms rewire + retiring the JSON/CSV files + `Change`→`CashFloat` rename (spec §8, §9). This plan leaves the WinForms panel untouched: it still works exactly as today until plan 3.

## Global Constraints

- **Branch:** `feat/cash-drawer-backend` from `development` (after PR #96 merges; if it has not, branch from `docs/cash-flow-spec`).
- **Forward-only migration gate** (CLAUDE.md): the migration **only adds four new tables** — no renames, drops, type narrowing, or new `NOT NULL` column on an existing table.
- **`StoreId` is `string`** in every new table (repo-wide convention). Set from `IStoreIdentityService.StoreId`, never from the request.
- **`BusinessDate` is set by the server, never the client**: the store-timezone (Bangkok) calendar date at the moment the row is created. **Edits never change it.** Day boundary = plain midnight.
- **Acting user comes from the token** (`ClaimTypes.NameIdentifier` or `sub`), never from the request body. Request bodies carry **no** user id and **no** `BusinessDate`.
- **Only today is editable.** Editing/deleting a row whose `BusinessDate` ≠ today → **`409`**. Adds are always today by construction.
- **Deleted entry is gone to the API:** editing a soft-deleted or unknown id → **`404`**. **Delete is idempotent:** re-deleting → **`204`**, audit fields and outbox untouched. Unknown id on delete → `404`.
- **Last save wins** — no version column, no concurrency check.
- **Cash counts are append-only:** `POST` + `GET` only; no edit, no delete, no soft-delete, no `LastModifiedByUserId`. **Only the latest count of the day** (highest `CreatedUtc`, ties broken by `Id`) feeds any calculation.
- **`PayoutCategory` is `{ General, Hardware }`, default `General`, persisted AND serialized as its name** (`"Hardware"`, not `1`) — the cloud copy is read by AI agents.
- **Every add / edit / delete / new count writes one `OutboxEvent` carrying the full current row, in the same `SaveChangesAsync`** as the row. Event types: `CashPayoutChanged`, `CashFloatChanged`, `DebtRepaymentChanged`, `CashCountChanged`.
- **Soft-delete = EF 10 named filter `"SoftDelete"`** on `CashPayout`, `CashFloat`, `DebtRepayment` — the **first global query filter in the repo, deliberately**. Only the delete lookup opts out.
- **Error body convention:** `{ "error": "<Thai message>" }` — the shape every existing StoreHub route uses. (The spec says "ProblemDetails"; the repo has none, and the WinForms client parses `error`. Repo convention wins.)
- **No FluentValidation validators:** the spec says "as elsewhere", but the repo has no `AbstractValidator` anywhere; rules are explicit checks (`CashEntryRules`). YAGNI.
- **Test naming:** `Subject_WhenScenario_DirectVerbOutcome` — `When`/`With` scenario, direct-verb outcome (`Returns`, `Throws`, `Keeps`, `Excludes`, `DoesNot…`), **never `Should`**. One behaviour per test. **Negative tests first; more than half** failure/edge/boundary. Named constants for boundary values. Arrange / Act / Assert separated by blank lines. (Existing tests still use `Should…` names — do not rename them.)
- **Fluent chains:** dots vertically aligned (repo style), e.g. `result.Should()` / `.Be(expected);`.
- **Files:** file-scoped namespaces, 4-space indent, nullable enabled.
- **Docker must be running** for `tests/IndyPOS.StoreHub.IntegrationTests` (Testcontainers).

## Review Focus

Inputs the spec implies but does not spell out, most likely to bite first. Each has a test in the task that owns the code:

1. **Money with more than 2 decimals** (`10.005`) — the column is `numeric(18,2)` and would round silently. Expect `400`. → Task 3 (`EnsureValidAmount_WithThreeDecimalPlaces_Throws`).
2. **An absurdly large amount** (`100000000000000000`) — overflows `numeric(18,2)` as a `500`. Expect `400` above `฿9,999,999.99`. → Task 3 (`EnsureValidAmount_AboveMaximum_Throws`).
3. **Whitespace-only description / customer name** (`"   "`) — stored as noise. Description becomes `null`; a blank customer name is rejected. → Task 3 (`NormalizeDescription_WithWhitespaceOnly_ReturnsNull`, `NormalizeCustomerName_WithWhitespaceOnly_Throws`).
4. **A category sent as a number outside the enum** (`"category": 7`) — `JsonStringEnumConverter` accepts integers. Expect `400`, not a row with category `7`. → Task 3 (`EnsureDefined_WithUndefinedValue_Throws`) + Task 9 (`AddPayout_WithUndefinedNumericCategory_ReturnsBadRequest`).
5. **A token with a valid role but no user-id claim** — the capability check passes, and a naive `Guid.Parse` would `500`. Expect `401`. → Task 9 (`AddPayout_WithTokenMissingUserId_ReturnsUnauthorized`).

---

## File Structure

```
src/IndyPOS.Domain/
  Enums/PayoutCategory.cs                                   NEW  enum + JSON-as-name
  Entities/Core/CashDrawerEntry.cs                          NEW  shared base: audit, soft-delete
  Entities/Core/CashPayout.cs                               NEW
  Entities/Core/CashFloat.cs                                NEW
  Entities/Core/DebtRepayment.cs                            NEW
  Entities/Core/CashCount.cs                                NEW  append-only, CountedTotal

src/IndyPOS.Application/
  Abstractions/StoreHub/Repositories/ICashEntryRepository.cs NEW  generic, for the 3 entry types
  Abstractions/StoreHub/Repositories/ICashCountRepository.cs NEW
  Common/Authorization/Capability.cs                         MOD  + CashManage
  Common/Authorization/RoleCapabilities.cs                   MOD  grant to Cashier/Manager/Admin
  Common/Exceptions/CashEntryNotFoundException.cs            NEW  -> 404
  Common/Exceptions/CashDayClosedException.cs                NEW  -> 409
  Common/Exceptions/CashEntryValidationException.cs          NEW  -> 400
  UseCases/StoreHub/CashDrawer/
    Common/ICashDrawerClock.cs + CashDrawerClock.cs          NEW  Bangkok business date
    Common/CashEntryRules.cs                                 NEW  amount/description/name/category/count rules
    Common/CashDayGuard.cs                                   NEW  only-today rule
    Common/CashDrawerOutbox.cs                               NEW  full-row outbox events
    Payouts/  (Add/Edit commands+handlers, Get query+handler, CashPayoutDto, requests)
    Floats/   (same shape)
    DebtRepayments/ (same shape)
    Delete/DeleteCashEntryCommand.cs + handler               NEW  generic, idempotent
    Counts/   (AddCashCount command+handler, GetCashCounts query+handler, CashCountDto, request)
    Summary/  GetCashDrawerSummaryQuery.cs, CashDrawerSummaryDto.cs, CashDrawerCalculator.cs

src/IndyPOS.Infrastructure/
  Persistence/StoreHub/StoreHubDbContext.cs                  MOD  + 4 DbSets
  Persistence/StoreHub/CashDrawerQueryFilters.cs             NEW  "SoftDelete" name
  Persistence/StoreHub/Configurations/CashDrawerEntryMapping.cs NEW shared column mapping + filter
  Persistence/StoreHub/Configurations/Cash{Payout,Float}Configuration.cs, DebtRepaymentConfiguration.cs, CashCountConfiguration.cs NEW
  Persistence/StoreHub/Repositories/CashEntryRepository.cs   NEW  generic
  Persistence/StoreHub/Repositories/CashCountRepository.cs   NEW
  Persistence/StoreHub/Migrations/<ts>_AddCashDrawerTables.* NEW  generated
  QueryHandlers/CashDrawer/GetCashDrawerSummaryQueryHandler.cs NEW (report handlers live here)
  ConfigureServices.cs                                       MOD  register repos

src/IndyPOS.StoreHub/
  Endpoints/Cash/CashEndpoints.cs                            NEW  MapCashEndpoints(): group, policy, filters, summary
  Endpoints/Cash/Cash{Payout,Float,Count}Endpoints.cs, DebtRepaymentEndpoints.cs NEW
  Endpoints/Cash/CashExceptionFilter.cs                      NEW  exceptions -> 400/404/409
  Endpoints/Cash/RequireUserIdFilter.cs                      NEW  no user id -> 401
  Endpoints/Cash/ClaimsPrincipalExtensions.cs                NEW  FindUserId / GetRequiredUserId
  Endpoints/Cash/CashDrawerServiceCollectionExtensions.cs    NEW  AddCashDrawer(): clock + handlers
  Program.cs                                                 MOD  policy + AddCashDrawer + MapCashEndpoints

tests/
  IndyPOS.Domain.Tests/Entities/CashDrawerEntryTests.cs, CashCountTests.cs          NEW
  IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/...                         NEW
  IndyPOS.StoreHub.IntegrationTests/CashDrawerPersistenceTests.cs                    NEW
  IndyPOS.StoreHub.IntegrationTests/Endpoints/Cash*EndpointsTests.cs                 NEW
```

---

### Task 1: Domain entities and `PayoutCategory`

**Files:**
- Create: `src/IndyPOS.Domain/Enums/PayoutCategory.cs`
- Create: `src/IndyPOS.Domain/Entities/Core/CashDrawerEntry.cs`, `CashPayout.cs`, `CashFloat.cs`, `DebtRepayment.cs`, `CashCount.cs`
- Test: `tests/IndyPOS.Domain.Tests/Entities/CashDrawerEntryTests.cs`, `tests/IndyPOS.Domain.Tests/Entities/CashCountTests.cs`

**Interfaces:**
- Produces: `PayoutCategory { General, Hardware }`; `abstract class CashDrawerEntry` with `Id, StoreId, Amount, BusinessDate, CreatedUtc, LastModifiedUtc, CreatedByUserId, LastModifiedByUserId, IsDeleted, DeletedUtc`, `void Touch(Guid userId, DateTime utcNow)`, `void MarkDeleted(Guid userId, DateTime utcNow)`; `CashPayout : CashDrawerEntry` (+ `PayoutCategory Category`, `string? Description`); `CashFloat : CashDrawerEntry` (+ `string? Description`); `DebtRepayment : CashDrawerEntry` (+ `string CustomerName`); `CashCount` (9 `int` counts, `Id, StoreId, BusinessDate, CreatedUtc, LastModifiedUtc, CreatedByUserId`, computed `decimal CountedTotal`).

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Domain.Tests/Entities/CashDrawerEntryTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Domain.Tests.Entities;

public class CashDrawerEntryTests
{
    private static readonly Guid CreatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EditorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly BusinessDate = new(2026, 9, 26);
    private static readonly DateTime CreatedUtc = new(2026, 9, 26, 1, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaterUtc = new(2026, 9, 26, 5, 0, 0, DateTimeKind.Utc);

    private static CashFloat NewEntry() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "test-store",
        Amount = 500m,
        BusinessDate = BusinessDate,
        CreatedUtc = CreatedUtc,
        LastModifiedUtc = CreatedUtc,
        CreatedByUserId = CreatorId
    };

    [Fact]
    public void MarkDeleted_WhenAlreadyDeleted_Throws()
    {
        var entry = NewEntry();
        entry.MarkDeleted(EditorId, LaterUtc);

        var act = () => entry.MarkDeleted(CreatorId, LaterUtc.AddHours(1));

        act.Should()
           .Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkDeleted_WhenAlreadyDeleted_KeepsOriginalAuditFields()
    {
        var entry = NewEntry();
        entry.MarkDeleted(EditorId, LaterUtc);

        try { entry.MarkDeleted(CreatorId, LaterUtc.AddHours(1)); } catch (InvalidOperationException) { }

        entry.DeletedUtc.Should()
                        .Be(LaterUtc);
        entry.LastModifiedByUserId.Should()
                                  .Be(EditorId);
    }

    [Fact]
    public void Touch_WithLaterTime_DoesNotChangeBusinessDate()
    {
        var entry = NewEntry();

        entry.Touch(EditorId, LaterUtc.AddDays(1));

        entry.BusinessDate.Should()
                          .Be(BusinessDate);
    }

    [Fact]
    public void MarkDeleted_WhenActive_SetsDeletedFlagAndTime()
    {
        var entry = NewEntry();

        entry.MarkDeleted(EditorId, LaterUtc);

        entry.IsDeleted.Should()
                       .BeTrue();
        entry.DeletedUtc.Should()
                        .Be(LaterUtc);
    }

    [Fact]
    public void MarkDeleted_WhenActive_RecordsWhoDeleted()
    {
        var entry = NewEntry();

        entry.MarkDeleted(EditorId, LaterUtc);

        entry.LastModifiedByUserId.Should()
                                  .Be(EditorId);
        entry.LastModifiedUtc.Should()
                             .Be(LaterUtc);
    }

    [Fact]
    public void Touch_WithEditor_RecordsEditorAndTime()
    {
        var entry = NewEntry();

        entry.Touch(EditorId, LaterUtc);

        entry.LastModifiedByUserId.Should()
                                  .Be(EditorId);
        entry.LastModifiedUtc.Should()
                             .Be(LaterUtc);
    }
}
```

`tests/IndyPOS.Domain.Tests/Entities/CashCountTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Domain.Tests.Entities;

public class CashCountTests
{
    [Fact]
    public void CountedTotal_WithNoNotesOrCoins_ReturnsZero()
    {
        var count = new CashCount();

        count.CountedTotal.Should()
                          .Be(0m);
    }

    [Theory]
    [InlineData(nameof(CashCount.BankNote1000Count), 1000)]
    [InlineData(nameof(CashCount.BankNote500Count), 500)]
    [InlineData(nameof(CashCount.BankNote100Count), 100)]
    [InlineData(nameof(CashCount.BankNote50Count), 50)]
    [InlineData(nameof(CashCount.BankNote20Count), 20)]
    [InlineData(nameof(CashCount.Coin10Count), 10)]
    [InlineData(nameof(CashCount.Coin5Count), 5)]
    [InlineData(nameof(CashCount.Coin2Count), 2)]
    [InlineData(nameof(CashCount.Coin1Count), 1)]
    public void CountedTotal_WithOneOfADenomination_ReturnsItsFaceValue(string property, int faceValue)
    {
        var count = new CashCount();
        typeof(CashCount).GetProperty(property)!.SetValue(count, 1);

        count.CountedTotal.Should()
                          .Be(faceValue);
    }

    [Fact]
    public void CountedTotal_WithMixedDenominations_ReturnsTheSum()
    {
        var count = new CashCount { BankNote1000Count = 2, BankNote100Count = 3, Coin5Count = 4, Coin1Count = 7 };

        count.CountedTotal.Should()
                          .Be(2327m);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Domain.Tests --filter "FullyQualifiedName~CashDrawerEntryTests|FullyQualifiedName~CashCountTests"`
Expected: build FAILS — `CashFloat`, `CashCount` do not exist.

- [ ] **Step 3: Write the implementation**

`src/IndyPOS.Domain/Enums/PayoutCategory.cs`:

```csharp
using System.Text.Json.Serialization;

namespace IndyPOS.Domain.Enums;

/// <summary>
/// Which side of the store a cash payout belongs to, mirroring how sales are already split.
/// Serialized by name ("Hardware", not 1) so the API and the cloud copy read without a code table.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PayoutCategory>))]
public enum PayoutCategory
{
    General,
    Hardware
}
```

`src/IndyPOS.Domain/Entities/Core/CashDrawerEntry.cs`:

```csharp
namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Shared shape of a hand-typed, soft-deletable cash-drawer entry (payout, float, debt repayment).
/// Not an EF entity itself: each subclass maps to its own table.
/// </summary>
public abstract class CashDrawerEntry
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public decimal Amount { get; set; }

    /// <summary>Store-local cash day the entry belongs to. Set once at creation; edits never move it.</summary>
    public DateOnly BusinessDate { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedUtc { get; set; }

    /// <summary>Records who last changed the entry and when.</summary>
    public void Touch(Guid userId, DateTime utcNow)
    {
        LastModifiedByUserId = userId;
        LastModifiedUtc = utcNow;
    }

    /// <summary>
    /// Soft-deletes the entry. A second call throws rather than overwriting the first deletion's
    /// audit fields — callers treat a repeat delete as a no-op before reaching here.
    /// </summary>
    public void MarkDeleted(Guid userId, DateTime utcNow)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException($"Cash entry {Id} is already deleted.");
        }

        IsDeleted = true;
        DeletedUtc = utcNow;
        Touch(userId, utcNow);
    }
}
```

`src/IndyPOS.Domain/Entities/Core/CashPayout.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>Cash taken out of the drawer (รายจ่าย), tagged Hardware or General.</summary>
public class CashPayout : CashDrawerEntry
{
    public PayoutCategory Category { get; set; } = PayoutCategory.General;
    public string? Description { get; set; }
}
```

`src/IndyPOS.Domain/Entities/Core/CashFloat.cs`:

```csharp
namespace IndyPOS.Domain.Entities.Core;

/// <summary>Cash put into the drawer as change (เงินทอน).</summary>
public class CashFloat : CashDrawerEntry
{
    public string? Description { get; set; }
}
```

`src/IndyPOS.Domain/Entities/Core/DebtRepayment.cs`:

```csharp
namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Cash a customer paid towards a debt (ลูกค้าชำระหนี้). A hand-typed list: deliberately NOT linked
/// to the customer's PayLater row.
/// </summary>
public class DebtRepayment : CashDrawerEntry
{
    public string CustomerName { get; set; } = default!;
}
```

`src/IndyPOS.Domain/Entities/Core/CashCount.cs`:

```csharp
namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// One count of the drawer's notes and coins. Append-only: every count is kept for audit, and
/// only the latest of the day feeds the cash difference.
/// </summary>
public class CashCount
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public DateOnly BusinessDate { get; set; }
    public int BankNote1000Count { get; set; }
    public int BankNote500Count { get; set; }
    public int BankNote100Count { get; set; }
    public int BankNote50Count { get; set; }
    public int BankNote20Count { get; set; }
    public int Coin10Count { get; set; }
    public int Coin5Count { get; set; }
    public int Coin2Count { get; set; }
    public int Coin1Count { get; set; }

    /// <summary>When the count was taken.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Equals CreatedUtc: kept for the repo-wide entity convention; a count never changes.</summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>Who counted.</summary>
    public Guid CreatedByUserId { get; set; }

    public decimal CountedTotal =>
        BankNote1000Count * 1000m
        + BankNote500Count * 500m
        + BankNote100Count * 100m
        + BankNote50Count * 50m
        + BankNote20Count * 20m
        + Coin10Count * 10m
        + Coin5Count * 5m
        + Coin2Count * 2m
        + Coin1Count;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Domain.Tests --filter "FullyQualifiedName~CashDrawerEntryTests|FullyQualifiedName~CashCountTests"`
Expected: PASS (17 test cases: 6 entry + 11 count).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Domain tests/IndyPOS.Domain.Tests
git commit -m "feat(domain): add cash drawer entities and PayoutCategory"
```

---

### Task 2: Persistence — tables, soft-delete filter, repositories, migration

**Files:**
- Create: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/ICashEntryRepository.cs`, `ICashCountRepository.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/CashDrawerQueryFilters.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/CashDrawerEntryMapping.cs`, `CashPayoutConfiguration.cs`, `CashFloatConfiguration.cs`, `DebtRepaymentConfiguration.cs`, `CashCountConfiguration.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/CashEntryRepository.cs`, `CashCountRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs` (add 4 `DbSet`s after `ProductCategories`)
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs:66-74` (register repos in `AddStoreHubServices`)
- Generate: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<timestamp>_AddCashDrawerTables.cs` (+ Designer, snapshot)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/CashDrawerPersistenceTests.cs`

**Interfaces:**
- Consumes: Task 1 entities.
- Produces:

```csharp
public interface ICashEntryRepository<TEntry> where TEntry : CashDrawerEntry
{
    Task<TEntry?> FindAsync(Guid id, CancellationToken cancellationToken = default);                 // hides deleted
    Task<TEntry?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntry>> ListAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default); // oldest first, hides deleted
    Task AddAsync(TEntry entry, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);   // tracked edits + event, one SaveChanges
}

public interface ICashCountRepository
{
    Task AddAsync(CashCount count, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashCount>> ListNewestFirstAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<CashCount?> GetLatestAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);
}
```

Tables: `cash_payout`, `cash_float`, `debt_repayment`, `cash_count` (snake_case singular, like `pay_later`).

- [ ] **Step 1: Write the failing integration tests**

`tests/IndyPOS.StoreHub.IntegrationTests/CashDrawerPersistenceTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Real-PostgreSQL checks for the cash-drawer tables: the soft-delete filter, the category stored
/// by name, and the deterministic "latest count" order. InMemory cannot prove uuid ordering.
/// </summary>
[Collection("Integration")]
public class CashDrawerPersistenceTests : IntegrationTestBase
{
    private const string StoreId = "test-store";
    private static readonly DateOnly Day = new(2026, 9, 26);
    private static readonly DateTime SameInstantUtc = new(2026, 9, 26, 3, 0, 0, DateTimeKind.Utc);

    public CashDrawerPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private static CashPayout NewPayout(bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = StoreId,
        Category = PayoutCategory.Hardware,
        Amount = 120m,
        BusinessDate = Day,
        CreatedUtc = SameInstantUtc,
        LastModifiedUtc = SameInstantUtc,
        CreatedByUserId = Guid.NewGuid(),
        IsDeleted = isDeleted,
        DeletedUtc = isDeleted ? SameInstantUtc : null
    };

    private static CashCount NewCount(Guid id) => new()
    {
        Id = id,
        StoreId = StoreId,
        BusinessDate = Day,
        BankNote100Count = 1,
        CreatedUtc = SameInstantUtc,
        LastModifiedUtc = SameInstantUtc,
        CreatedByUserId = Guid.NewGuid()
    };

    private static OutboxEvent NewEvent() => new()
    {
        Id = Guid.NewGuid(), StoreId = StoreId, Type = "Test", PayloadJson = "{}", CreatedUtc = SameInstantUtc
    };

    private async Task<Guid> SeedPayoutAsync(bool isDeleted)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var payout = NewPayout(isDeleted);
        db.CashPayouts.Add(payout);
        await db.SaveChangesAsync();
        return payout.Id;
    }

    private CashEntryRepository<CashPayout> PayoutRepository(out IServiceScope scope)
    {
        scope = Factory.Services.CreateScope();
        return new CashEntryRepository<CashPayout>(scope.ServiceProvider.GetRequiredService<StoreHubDbContext>());
    }

    [Fact]
    public async Task FindAsync_WithSoftDeletedPayout_ReturnsNull()
    {
        var id = await SeedPayoutAsync(isDeleted: true);
        var repository = PayoutRepository(out var scope);
        using var _ = scope;

        var found = await repository.FindAsync(id);

        found.Should()
             .BeNull();
    }

    [Fact]
    public async Task ListAsync_WithSoftDeletedPayout_ExcludesIt()
    {
        var id = await SeedPayoutAsync(isDeleted: true);
        var repository = PayoutRepository(out var scope);
        using var _ = scope;

        var list = await repository.ListAsync(StoreId, Day);

        list.Should()
            .NotContain(p => p.Id == id);
    }

    [Fact]
    public async Task FindIncludingDeletedAsync_WithSoftDeletedPayout_ReturnsIt()
    {
        var id = await SeedPayoutAsync(isDeleted: true);
        var repository = PayoutRepository(out var scope);
        using var _ = scope;

        var found = await repository.FindIncludingDeletedAsync(id);

        found!.IsDeleted.Should()
                        .BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WithCategory_StoresItsName()
    {
        var id = await SeedPayoutAsync(isDeleted: false);
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var stored = await db.Database
                             .SqlQuery<string>($"SELECT category AS \"Value\" FROM cash_payout WHERE id = {id}")
                             .SingleAsync();

        stored.Should()
              .Be("Hardware");
    }

    [Fact]
    public async Task GetLatestAsync_WithTwoCountsAtTheSameInstant_ReturnsTheSameRowEveryTime()
    {
        await ResetDatabaseAsync();
        var repository = new CashCountRepository(GetDbContext());
        await repository.AddAsync(NewCount(Guid.NewGuid()), NewEvent());
        await repository.AddAsync(NewCount(Guid.NewGuid()), NewEvent());

        var first = await new CashCountRepository(GetDbContext()).GetLatestAsync(StoreId, Day);
        var second = await new CashCountRepository(GetDbContext()).GetLatestAsync(StoreId, Day);

        second!.Id.Should()
                  .Be(first!.Id);
    }

    [Fact]
    public async Task ListNewestFirstAsync_WithCountsAtDifferentTimes_ReturnsNewestFirst()
    {
        await ResetDatabaseAsync();
        var repository = new CashCountRepository(GetDbContext());
        var morning = NewCount(Guid.NewGuid());
        var evening = NewCount(Guid.NewGuid());
        evening.CreatedUtc = SameInstantUtc.AddHours(10);
        await repository.AddAsync(morning, NewEvent());
        await repository.AddAsync(evening, NewEvent());

        var list = await new CashCountRepository(GetDbContext()).ListNewestFirstAsync(StoreId, Day);

        list.Select(c => c.Id).Should()
                              .ContainInOrder(evening.Id, morning.Id);
    }

    [Fact]
    public async Task AddAsync_WithOutboxEvent_PersistsBothInOneSave()
    {
        var repository = PayoutRepository(out var scope);
        using var _ = scope;
        var payout = NewPayout();
        var outboxEvent = NewEvent();

        await repository.AddAsync(payout, outboxEvent);

        var db = GetDbContext();
        (await db.CashPayouts.AnyAsync(p => p.Id == payout.Id)).Should()
                                                               .BeTrue();
        (await db.OutboxEvents.AnyAsync(e => e.Id == outboxEvent.Id)).Should()
                                                                     .BeTrue();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CashDrawerPersistenceTests"`
Expected: build FAILS — `CashPayouts`, `CashEntryRepository`, `CashCountRepository` do not exist.

- [ ] **Step 3: Add the repository interfaces**

`src/IndyPOS.Application/Abstractions/StoreHub/Repositories/ICashEntryRepository.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>
/// Persistence for the soft-deletable cash-drawer entries. Every write saves the row and its
/// outbox event in one SaveChanges, so a row never exists without its sync event.
/// </summary>
public interface ICashEntryRepository<TEntry> where TEntry : CashDrawerEntry
{
    /// <summary>Returns the entry, or null if unknown OR soft-deleted.</summary>
    Task<TEntry?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the entry even when soft-deleted. Only the delete path uses this.</summary>
    Task<TEntry?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The day's non-deleted entries, oldest first.</summary>
    Task<IReadOnlyList<TEntry>> ListAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntry entry, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>Saves changes made to an entry loaded through this repository, plus its event.</summary>
    Task SaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
```

`src/IndyPOS.Application/Abstractions/StoreHub/Repositories/ICashCountRepository.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.Abstractions.StoreHub.Repositories;

/// <summary>Append-only persistence for cash counts: add and read, never update or delete.</summary>
public interface ICashCountRepository
{
    Task AddAsync(CashCount count, OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>The day's counts, newest first; same-instant ties broken by Id so the order is stable.</summary>
    Task<IReadOnlyList<CashCount>> ListNewestFirstAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);

    /// <summary>The count used for calculations: the first row of <see cref="ListNewestFirstAsync"/>.</summary>
    Task<CashCount?> GetLatestAsync(string storeId, DateOnly businessDate, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Add the EF mapping**

`src/IndyPOS.Infrastructure/Persistence/StoreHub/CashDrawerQueryFilters.cs`:

```csharp
namespace IndyPOS.Infrastructure.Persistence.StoreHub;

/// <summary>
/// Names of the global query filters on the cash-drawer tables. Named (EF 10) so a later filter
/// can be added or bypassed without disturbing this one.
/// </summary>
public static class CashDrawerQueryFilters
{
    /// <summary>
    /// Hides soft-deleted rows from every query. This is the repo's first global filter, on purpose:
    /// a forgotten Where would put a deleted payout back into the drawer's money.
    /// </summary>
    public const string SoftDelete = "SoftDelete";
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/CashDrawerEntryMapping.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

/// <summary>Column mapping, soft-delete filter and day index shared by the three entry tables.</summary>
internal static class CashDrawerEntryMapping
{
    public static void MapCashDrawerEntry<TEntry>(this EntityTypeBuilder<TEntry> builder, string tableName)
        where TEntry : CashDrawerEntry
    {
        builder.ToTable(tableName);
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(e => e.BusinessDate).HasColumnName("business_date").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(e => e.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.DeletedUtc).HasColumnName("deleted_utc");

        builder.HasQueryFilter(CashDrawerQueryFilters.SoftDelete, e => !e.IsDeleted);
        builder.HasIndex(e => new { e.StoreId, e.BusinessDate });
    }
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/CashPayoutConfiguration.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class CashPayoutConfiguration : IEntityTypeConfiguration<CashPayout>
{
    public void Configure(EntityTypeBuilder<CashPayout> builder)
    {
        builder.MapCashDrawerEntry("cash_payout");

        builder.Property(e => e.Category)
               .HasColumnName("category")
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(Domain.Enums.PayoutCategory.General)
               .IsRequired();

        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
    }
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/CashFloatConfiguration.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class CashFloatConfiguration : IEntityTypeConfiguration<CashFloat>
{
    public void Configure(EntityTypeBuilder<CashFloat> builder)
    {
        builder.MapCashDrawerEntry("cash_float");
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
    }
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/DebtRepaymentConfiguration.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

public class DebtRepaymentConfiguration : IEntityTypeConfiguration<DebtRepayment>
{
    public void Configure(EntityTypeBuilder<DebtRepayment> builder)
    {
        builder.MapCashDrawerEntry("debt_repayment");
        builder.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
    }
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/CashCountConfiguration.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Configurations;

/// <summary>Append-only: no soft-delete column and no query filter — counts are never removed.</summary>
public class CashCountConfiguration : IEntityTypeConfiguration<CashCount>
{
    public void Configure(EntityTypeBuilder<CashCount> builder)
    {
        builder.ToTable("cash_count");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.StoreId).HasColumnName("store_id").HasMaxLength(50).IsRequired();
        builder.Property(e => e.BusinessDate).HasColumnName("business_date").IsRequired();
        builder.Property(e => e.BankNote1000Count).HasColumnName("bank_note_1000_count").IsRequired();
        builder.Property(e => e.BankNote500Count).HasColumnName("bank_note_500_count").IsRequired();
        builder.Property(e => e.BankNote100Count).HasColumnName("bank_note_100_count").IsRequired();
        builder.Property(e => e.BankNote50Count).HasColumnName("bank_note_50_count").IsRequired();
        builder.Property(e => e.BankNote20Count).HasColumnName("bank_note_20_count").IsRequired();
        builder.Property(e => e.Coin10Count).HasColumnName("coin_10_count").IsRequired();
        builder.Property(e => e.Coin5Count).HasColumnName("coin_5_count").IsRequired();
        builder.Property(e => e.Coin2Count).HasColumnName("coin_2_count").IsRequired();
        builder.Property(e => e.Coin1Count).HasColumnName("coin_1_count").IsRequired();
        builder.Property(e => e.CreatedUtc).HasColumnName("created_utc").IsRequired();
        builder.Property(e => e.LastModifiedUtc).HasColumnName("last_modified_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Ignore(e => e.CountedTotal);
        builder.HasIndex(e => new { e.StoreId, e.BusinessDate, e.CreatedUtc });
    }
}
```

In `StoreHubDbContext.cs`, after `public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();` add:

```csharp
    public DbSet<CashPayout> CashPayouts => Set<CashPayout>();
    public DbSet<CashFloat> CashFloats => Set<CashFloat>();
    public DbSet<DebtRepayment> DebtRepayments => Set<DebtRepayment>();
    public DbSet<CashCount> CashCounts => Set<CashCount>();
```

- [ ] **Step 5: Add the repositories**

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/CashEntryRepository.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class CashEntryRepository<TEntry>(StoreHubDbContext db) : ICashEntryRepository<TEntry>
    where TEntry : CashDrawerEntry
{
    public Task<TEntry?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<TEntry>()
          .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<TEntry?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Set<TEntry>()
          .IgnoreQueryFilters([CashDrawerQueryFilters.SoftDelete])
          .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TEntry>> ListAsync(
        string storeId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        await db.Set<TEntry>()
                .AsNoTracking()
                .Where(e => e.StoreId == storeId && e.BusinessDate == businessDate)
                .OrderBy(e => e.CreatedUtc)
                .ThenBy(e => e.Id)
                .ToListAsync(cancellationToken);

    public async Task AddAsync(TEntry entry, OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.Set<TEntry>().Add(entry);
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/CashCountRepository.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;

public class CashCountRepository(StoreHubDbContext db) : ICashCountRepository
{
    public async Task AddAsync(CashCount count, OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        db.CashCounts.Add(count);
        db.OutboxEvents.Add(outboxEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CashCount>> ListNewestFirstAsync(
        string storeId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        await NewestFirst(storeId, businessDate).ToListAsync(cancellationToken);

    public Task<CashCount?> GetLatestAsync(
        string storeId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default) =>
        NewestFirst(storeId, businessDate).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<CashCount> NewestFirst(string storeId, DateOnly businessDate) =>
        db.CashCounts
          .AsNoTracking()
          .Where(c => c.StoreId == storeId && c.BusinessDate == businessDate)
          .OrderByDescending(c => c.CreatedUtc)
          .ThenByDescending(c => c.Id);
}
```

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, inside `AddStoreHubServices`, extend the repository chain (after `.AddScoped<IProductCategoryRepository, ProductCategoryRepository>()`):

```csharp
		        .AddScoped(typeof(ICashEntryRepository<>), typeof(CashEntryRepository<>))
		        .AddScoped<ICashCountRepository, CashCountRepository>()
```

- [ ] **Step 6: Run the tests to verify they pass**

Run (Docker running): `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~CashDrawerPersistenceTests"`
Expected: PASS (7). The test host uses `EnsureCreated`, so the new tables exist without a migration.

- [ ] **Step 7: Generate the migration**

Run:

```bash
cd src/IndyPOS.Infrastructure
dotnet ef migrations add AddCashDrawerTables --startup-project ../IndyPOS.StoreHub --context StoreHubDbContext --output-dir Persistence/StoreHub/Migrations
```

If `dotnet-ef` (installed 9.0.8) refuses an EF 10 model, first run `dotnet tool update --global dotnet-ef --version 10.*`.

Open the generated `<timestamp>_AddCashDrawerTables.cs` and confirm `Up` contains **only** four `CreateTable` and four `CreateIndex` calls — no `AlterColumn`, `DropColumn`, `RenameColumn` or `AddColumn` on an existing table. If anything else appears, the model drifted from the snapshot: stop and investigate, do not ship it.

Then: `dotnet ef migrations has-pending-model-changes --startup-project ../IndyPOS.StoreHub --context StoreHubDbContext`
Expected: "No changes have been made to the model since the last migration."

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Application/Abstractions src/IndyPOS.Infrastructure tests/IndyPOS.StoreHub.IntegrationTests/CashDrawerPersistenceTests.cs
git commit -m "feat(storehub): add cash drawer tables, soft-delete filter and repositories"
```

---

### Task 3: Shared cash rules — clock, validation, only-today guard, outbox, exceptions

**Files:**
- Create: `src/IndyPOS.Application/Common/Exceptions/CashEntryNotFoundException.cs`, `CashDayClosedException.cs`, `CashEntryValidationException.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/ICashDrawerClock.cs`, `CashDrawerClock.cs`, `CashEntryRules.cs`, `CashDayGuard.cs`, `CashDrawerOutbox.cs`
- Modify: `tests/IndyPOS.Application.Tests/IndyPOS.Application.Tests.csproj` (add `Microsoft.Extensions.TimeProvider.Testing`)
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/CashDrawerClockTests.cs`, `CashEntryRulesTests.cs`, `CashDayGuardTests.cs`, `CashDrawerOutboxTests.cs`, plus the shared `CashDrawerTestContext.cs`

**Interfaces:**
- Consumes: Task 1 entities; `IStoreIdentityService.StoreId`, `.TimeZone`.
- Produces:

```csharp
public readonly record struct CashDrawerInstant(DateTime Utc, DateOnly BusinessDate);
public interface ICashDrawerClock { CashDrawerInstant Now(); }
public sealed class CashDrawerClock(TimeProvider timeProvider, IStoreIdentityService storeIdentity) : ICashDrawerClock;

public static class CashEntryRules
{
    public const decimal MaxAmount = 9_999_999.99m;
    public const int MaxDescriptionLength = 500;
    public const int MaxCustomerNameLength = 200;
    public static void EnsureValidAmount(decimal amount);
    public static string? NormalizeDescription(string? description);
    public static string NormalizeCustomerName(string? customerName);
    public static PayoutCategory EnsureDefined(PayoutCategory category);
    public static void EnsureValidCounts(params int[] counts);
}
public static class CashDayGuard { public static void EnsureEditable(DateOnly businessDate, DateOnly today); }
public static class CashDrawerOutbox
{
    public const string CashPayoutChanged = "CashPayoutChanged";
    public const string CashFloatChanged = "CashFloatChanged";
    public const string DebtRepaymentChanged = "DebtRepaymentChanged";
    public const string CashCountChanged = "CashCountChanged";
    public static OutboxEvent Changed(CashDrawerEntry entry, DateTime utcNow);
    public static OutboxEvent CountAdded(CashCount count, DateTime utcNow);
}
public class CashEntryNotFoundException(Guid id) : Exception;       // Thai message
public class CashDayClosedException(DateOnly businessDate) : Exception;
public class CashEntryValidationException(string message) : Exception;
```

Test helper produced (used by Tasks 4–8): `CashDrawerTestContext` (below).

- [ ] **Step 1: Add the test package**

Run: `dotnet add tests/IndyPOS.Application.Tests package Microsoft.Extensions.TimeProvider.Testing`

- [ ] **Step 2: Write the shared test context**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/CashDrawerTestContext.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

/// <summary>
/// One InMemory StoreHub database, a fake clock set to 10:00 Bangkok on <see cref="Today"/>, and a
/// store in the Bangkok timezone. Real repositories, so the soft-delete filter is exercised.
/// </summary>
internal sealed class CashDrawerTestContext : IAsyncDisposable
{
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    public static readonly DateOnly Today = new(2026, 9, 26);
    public static readonly DateOnly Yesterday = Today.AddDays(-1);

    /// <summary>10:00 Bangkok (UTC+7) on <see cref="Today"/>.</summary>
    public static readonly DateTimeOffset TenAmBangkok = new(2026, 9, 26, 3, 0, 0, TimeSpan.Zero);

    public static readonly Guid CashierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OtherCashierId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public CashDrawerTestContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        Db = new StoreHubDbContext(options);
        Time = new FakeTimeProvider(TenAmBangkok);
        StoreIdentity = MockStoreIdentityService.GeneralHardware();
        StoreIdentity.TimeZone = Bangkok;
        Clock = new CashDrawerClock(Time, StoreIdentity);
    }

    public StoreHubDbContext Db { get; }
    public FakeTimeProvider Time { get; }
    public MockStoreIdentityService StoreIdentity { get; }
    public ICashDrawerClock Clock { get; }

    public CashEntryRepository<TEntry> EntryRepository<TEntry>() where TEntry : CashDrawerEntry => new(Db);

    public CashCountRepository CountRepository() => new(Db);

    /// <summary>Seeds a payout directly, bypassing the handlers (e.g. a row from an earlier day).</summary>
    public async Task<CashPayout> SeedPayoutAsync(DateOnly businessDate, bool isDeleted = false)
    {
        var payout = new CashPayout
        {
            Id = Guid.NewGuid(),
            StoreId = StoreIdentity.StoreId,
            Amount = 100m,
            BusinessDate = businessDate,
            CreatedUtc = TenAmBangkok.UtcDateTime,
            LastModifiedUtc = TenAmBangkok.UtcDateTime,
            CreatedByUserId = CashierId,
            IsDeleted = isDeleted,
            DeletedUtc = isDeleted ? TenAmBangkok.UtcDateTime : null,
            LastModifiedByUserId = isDeleted ? CashierId : null
        };
        Db.CashPayouts.Add(payout);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return payout;
    }

    public List<OutboxEvent> OutboxEvents() => Db.OutboxEvents.AsNoTracking().ToList();

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
```

- [ ] **Step 3: Write the failing tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/CashDrawerClockTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Mock;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDrawerClockTests
{
    /// <summary>23:59:59 Bangkok on 2026-09-26 is 16:59:59 UTC the same day.</summary>
    private static readonly DateTimeOffset OneSecondBeforeBangkokMidnight = new(2026, 9, 26, 16, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset BangkokMidnight = new(2026, 9, 26, 17, 0, 0, TimeSpan.Zero);

    private static CashDrawerClock ClockAt(DateTimeOffset utcNow)
    {
        var store = MockStoreIdentityService.GeneralHardware();
        store.TimeZone = CashDrawerTestContext.Bangkok;
        return new CashDrawerClock(new FakeTimeProvider(utcNow), store);
    }

    [Fact]
    public void Now_WhenOneSecondBeforeBangkokMidnight_ReturnsTheSameBusinessDate()
    {
        var clock = ClockAt(OneSecondBeforeBangkokMidnight);

        var now = clock.Now();

        now.BusinessDate.Should()
                        .Be(new DateOnly(2026, 9, 26));
    }

    [Fact]
    public void Now_WhenAtBangkokMidnight_ReturnsTheNextBusinessDate()
    {
        var clock = ClockAt(BangkokMidnight);

        var now = clock.Now();

        now.BusinessDate.Should()
                        .Be(new DateOnly(2026, 9, 27));
    }

    [Fact]
    public void Now_WhenUtcDateIsStillYesterday_ReturnsTheBangkokDate()
    {
        var clock = ClockAt(new DateTimeOffset(2026, 9, 25, 23, 30, 0, TimeSpan.Zero)); // 06:30 Bangkok on 26th

        var now = clock.Now();

        now.BusinessDate.Should()
                        .Be(new DateOnly(2026, 9, 26));
    }

    [Fact]
    public void Now_WithAnyTime_ReturnsUtcKind()
    {
        var clock = ClockAt(BangkokMidnight);

        var now = clock.Now();

        now.Utc.Kind.Should()
                    .Be(DateTimeKind.Utc);
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/CashEntryRulesTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashEntryRulesTests
{
    private const decimal ThreeDecimalPlaces = 10.005m;
    private const decimal JustAboveMaximum = CashEntryRules.MaxAmount + 0.01m;
    private const PayoutCategory UndefinedCategory = (PayoutCategory)7;

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void EnsureValidAmount_WithZeroOrNegative_Throws(decimal amount)
    {
        var act = () => CashEntryRules.EnsureValidAmount(amount);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureValidAmount_WithThreeDecimalPlaces_Throws()
    {
        var act = () => CashEntryRules.EnsureValidAmount(ThreeDecimalPlaces);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureValidAmount_AboveMaximum_Throws()
    {
        var act = () => CashEntryRules.EnsureValidAmount(JustAboveMaximum);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeDescription_AboveMaximumLength_Throws()
    {
        var tooLong = new string('ก', CashEntryRules.MaxDescriptionLength + 1);

        var act = () => CashEntryRules.NormalizeDescription(tooLong);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCustomerName_WithMissingOrBlank_Throws(string? customerName)
    {
        var act = () => CashEntryRules.NormalizeCustomerName(customerName);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeCustomerName_WithWhitespaceOnly_Throws()
    {
        var act = () => CashEntryRules.NormalizeCustomerName("\t  \n");

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeCustomerName_AboveMaximumLength_Throws()
    {
        var tooLong = new string('ก', CashEntryRules.MaxCustomerNameLength + 1);

        var act = () => CashEntryRules.NormalizeCustomerName(tooLong);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureDefined_WithUndefinedValue_Throws()
    {
        var act = () => CashEntryRules.EnsureDefined(UndefinedCategory);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void EnsureValidCounts_WithOneNegativeCount_Throws()
    {
        var act = () => CashEntryRules.EnsureValidCounts(1, 0, -1);

        act.Should()
           .Throw<CashEntryValidationException>();
    }

    [Fact]
    public void NormalizeDescription_WithWhitespaceOnly_ReturnsNull()
    {
        var result = CashEntryRules.NormalizeDescription("   ");

        result.Should()
              .BeNull();
    }

    [Fact]
    public void NormalizeDescription_WithSurroundingSpaces_ReturnsTrimmed()
    {
        var result = CashEntryRules.NormalizeDescription("  ค่าน้ำแข็ง  ");

        result.Should()
              .Be("ค่าน้ำแข็ง");
    }

    [Fact]
    public void NormalizeCustomerName_WithSurroundingSpaces_ReturnsTrimmed()
    {
        var result = CashEntryRules.NormalizeCustomerName("  ลุงสมชาย ");

        result.Should()
              .Be("ลุงสมชาย");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(9999999.99)]
    public void EnsureValidAmount_AtTheBoundaries_DoesNotThrow(decimal amount)
    {
        var act = () => CashEntryRules.EnsureValidAmount(amount);

        act.Should()
           .NotThrow();
    }

    [Fact]
    public void EnsureValidCounts_WithAllZero_DoesNotThrow()
    {
        var act = () => CashEntryRules.EnsureValidCounts(0, 0, 0, 0, 0, 0, 0, 0, 0);

        act.Should()
           .NotThrow();
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/CashDayGuardTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDayGuardTests
{
    private static readonly DateOnly Today = new(2026, 9, 26);

    [Fact]
    public void EnsureEditable_WithYesterday_Throws()
    {
        var act = () => CashDayGuard.EnsureEditable(Today.AddDays(-1), Today);

        act.Should()
           .Throw<CashDayClosedException>();
    }

    [Fact]
    public void EnsureEditable_WithFutureDate_Throws()
    {
        var act = () => CashDayGuard.EnsureEditable(Today.AddDays(1), Today);

        act.Should()
           .Throw<CashDayClosedException>();
    }

    [Fact]
    public void EnsureEditable_WithToday_DoesNotThrow()
    {
        var act = () => CashDayGuard.EnsureEditable(Today, Today);

        act.Should()
           .NotThrow();
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/CashDrawerOutboxTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDrawerOutboxTests
{
    private static readonly DateTime NowUtc = new(2026, 9, 26, 3, 0, 0, DateTimeKind.Utc);

    private static CashPayout NewPayout() => new()
    {
        Id = Guid.NewGuid(),
        StoreId = "test-store",
        Category = PayoutCategory.Hardware,
        Amount = 250m,
        BusinessDate = new DateOnly(2026, 9, 26),
        CreatedUtc = NowUtc,
        LastModifiedUtc = NowUtc,
        CreatedByUserId = Guid.NewGuid()
    };

    [Fact]
    public void Changed_WithPayout_SerializesCategoryByName()
    {
        var outboxEvent = CashDrawerOutbox.Changed(NewPayout(), NowUtc);

        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        payload.RootElement.GetProperty("Category").GetString().Should()
                                                               .Be("Hardware");
    }

    [Fact]
    public void Changed_WithDeletedPayout_CarriesTheDeletedFlag()
    {
        var payout = NewPayout();
        payout.MarkDeleted(Guid.NewGuid(), NowUtc);

        var outboxEvent = CashDrawerOutbox.Changed(payout, NowUtc);

        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        payload.RootElement.GetProperty("IsDeleted").GetBoolean().Should()
                                                                 .BeTrue();
    }

    [Theory]
    [InlineData(typeof(CashPayout), CashDrawerOutbox.CashPayoutChanged)]
    [InlineData(typeof(CashFloat), CashDrawerOutbox.CashFloatChanged)]
    [InlineData(typeof(DebtRepayment), CashDrawerOutbox.DebtRepaymentChanged)]
    public void Changed_WithEntryType_ReturnsItsEventType(Type entryType, string expectedType)
    {
        var entry = (CashDrawerEntry)Activator.CreateInstance(entryType)!;
        entry.StoreId = "test-store";

        var outboxEvent = CashDrawerOutbox.Changed(entry, NowUtc);

        outboxEvent.Type.Should()
                        .Be(expectedType);
    }

    [Fact]
    public void CountAdded_WithCount_ReturnsPendingCashCountChangedEvent()
    {
        var count = new CashCount { Id = Guid.NewGuid(), StoreId = "test-store" };

        var outboxEvent = CashDrawerOutbox.CountAdded(count, NowUtc);

        outboxEvent.Type.Should()
                        .Be(CashDrawerOutbox.CashCountChanged);
        outboxEvent.Status.Should()
                          .Be("Pending");
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~UseCases.StoreHub.CashDrawer"`
Expected: build FAILS — `CashDrawerClock`, `CashEntryRules`, `CashDayGuard`, `CashDrawerOutbox`, the exceptions do not exist.

- [ ] **Step 5: Write the implementation**

`src/IndyPOS.Application/Common/Exceptions/CashEntryNotFoundException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>The entry is unknown or soft-deleted. Mapped to 404.</summary>
public class CashEntryNotFoundException(Guid id)
    : Exception($"ไม่พบรายการ ({id})");
```

`src/IndyPOS.Application/Common/Exceptions/CashDayClosedException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>
/// The entry belongs to a day other than today. Past days are closed records: a mismatch is raised
/// and resolved the same day. Mapped to 409.
/// </summary>
public class CashDayClosedException(DateOnly businessDate)
    : Exception($"แก้ไขได้เฉพาะรายการของวันนี้เท่านั้น (รายการนี้เป็นของวันที่ {businessDate:yyyy-MM-dd})");
```

`src/IndyPOS.Application/Common/Exceptions/CashEntryValidationException.cs`:

```csharp
namespace IndyPOS.Application.Common.Exceptions;

/// <summary>Input broke a cash-drawer rule. The message is shown to the cashier. Mapped to 400.</summary>
public class CashEntryValidationException(string message) : Exception(message);
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/ICashDrawerClock.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>A UTC instant and the store-local cash day it falls on, read together so they cannot straddle midnight.</summary>
public readonly record struct CashDrawerInstant(DateTime Utc, DateOnly BusinessDate);

public interface ICashDrawerClock
{
    CashDrawerInstant Now();
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/CashDrawerClock.cs`:

```csharp
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>
/// Server-side source of the business date: the store's timezone (Bangkok), plain midnight
/// boundary. The client never supplies the date, so a wrong till clock cannot misfile an entry.
/// </summary>
public sealed class CashDrawerClock(TimeProvider timeProvider, IStoreIdentityService storeIdentity) : ICashDrawerClock
{
    public CashDrawerInstant Now()
    {
        var utc = timeProvider.GetUtcNow().UtcDateTime;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, storeIdentity.TimeZone);
        return new CashDrawerInstant(utc, DateOnly.FromDateTime(local));
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/CashEntryRules.cs`:

```csharp
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>Boundary rules for hand-typed cash input. Messages are Thai: the cashier reads them.</summary>
public static class CashEntryRules
{
    /// <summary>Well inside numeric(18,2), far above any real drawer movement.</summary>
    public const decimal MaxAmount = 9_999_999.99m;
    public const int MaxDescriptionLength = 500;
    public const int MaxCustomerNameLength = 200;

    public static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0)
            throw new CashEntryValidationException("จำนวนเงินต้องมากกว่า 0");

        if (amount > MaxAmount)
            throw new CashEntryValidationException($"จำนวนเงินต้องไม่เกิน {MaxAmount:N2}");

        // The column keeps 2 decimals; rejecting beats silently rounding the cashier's number.
        if (decimal.Round(amount, 2) != amount)
            throw new CashEntryValidationException("จำนวนเงินมีทศนิยมได้ไม่เกิน 2 ตำแหน่ง");
    }

    public static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > MaxDescriptionLength)
            throw new CashEntryValidationException($"รายละเอียดยาวเกิน {MaxDescriptionLength} ตัวอักษร");

        return trimmed;
    }

    public static string NormalizeCustomerName(string? customerName)
    {
        var trimmed = customerName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new CashEntryValidationException("กรุณาระบุชื่อลูกค้า");

        if (trimmed.Length > MaxCustomerNameLength)
            throw new CashEntryValidationException($"ชื่อลูกค้ายาวเกิน {MaxCustomerNameLength} ตัวอักษร");

        return trimmed;
    }

    public static PayoutCategory EnsureDefined(PayoutCategory category)
    {
        // The JSON converter accepts integers, so an out-of-range number can arrive here.
        if (!Enum.IsDefined(category))
            throw new CashEntryValidationException("หมวดหมู่รายจ่ายไม่ถูกต้อง");

        return category;
    }

    public static void EnsureValidCounts(params int[] counts)
    {
        if (counts.Any(c => c < 0))
            throw new CashEntryValidationException("จำนวนนับต้องไม่ติดลบ");
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/CashDayGuard.cs`:

```csharp
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>Only today's entries can change; any other day is view-only.</summary>
public static class CashDayGuard
{
    public static void EnsureEditable(DateOnly businessDate, DateOnly today)
    {
        if (businessDate != today)
            throw new CashDayClosedException(businessDate);
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Common/CashDrawerOutbox.cs`:

```csharp
using System.Text.Json;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>
/// Builds the sync event for a cash-drawer change. The payload is the FULL current row — a state
/// snapshot the cloud upserts by Id, never a delta.
/// </summary>
public static class CashDrawerOutbox
{
    public const string CashPayoutChanged = "CashPayoutChanged";
    public const string CashFloatChanged = "CashFloatChanged";
    public const string DebtRepaymentChanged = "DebtRepaymentChanged";
    public const string CashCountChanged = "CashCountChanged";

    public static OutboxEvent Changed(CashDrawerEntry entry, DateTime utcNow) =>
        Create(EventTypeFor(entry), entry.StoreId, entry, utcNow);

    public static OutboxEvent CountAdded(CashCount count, DateTime utcNow) =>
        Create(CashCountChanged, count.StoreId, count, utcNow);

    private static string EventTypeFor(CashDrawerEntry entry) => entry switch
    {
        CashPayout => CashPayoutChanged,
        CashFloat => CashFloatChanged,
        DebtRepayment => DebtRepaymentChanged,
        _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.GetType().Name, "No sync event for this entry type.")
    };

    private static OutboxEvent Create(string type, string storeId, object row, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        Type = type,
        PayloadJson = JsonSerializer.Serialize(row, row.GetType()),
        CreatedUtc = utcNow,
        Status = "Pending"
    };
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~UseCases.StoreHub.CashDrawer"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application tests/IndyPOS.Application.Tests
git commit -m "feat(cash): add business-date clock, input rules, only-today guard and outbox events"
```

---

### Task 4: Cash payouts — add, edit, list

**Files:**
- Create in `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/`: `CashPayoutDto.cs`, `CashPayoutRequests.cs`, `AddCashPayoutCommand.cs`, `AddCashPayoutCommandHandler.cs`, `EditCashPayoutCommand.cs`, `EditCashPayoutCommandHandler.cs`, `GetCashPayoutsQuery.cs`, `GetCashPayoutsQueryHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Payouts/AddCashPayoutCommandHandlerTests.cs`, `EditCashPayoutCommandHandlerTests.cs`, `GetCashPayoutsQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `ICashEntryRepository<CashPayout>`, `ICashDrawerClock`, `IStoreIdentityService`, `CashEntryRules`, `CashDayGuard`, `CashDrawerOutbox`, `CashDrawerTestContext`.
- Produces:

```csharp
public record CashPayoutDto(Guid Id, PayoutCategory Category, decimal Amount, string? Description, DateOnly BusinessDate,
    DateTime CreatedUtc, DateTime LastModifiedUtc, Guid CreatedByUserId, Guid? LastModifiedByUserId);
public record AddCashPayoutRequest(decimal Amount, PayoutCategory? Category, string? Description);
public record EditCashPayoutRequest(decimal Amount, PayoutCategory Category, string? Description);
public record AddCashPayoutCommand(Guid UserId, decimal Amount, PayoutCategory? Category, string? Description) : ICommand<CashPayoutDto>;
public record EditCashPayoutCommand(Guid Id, Guid UserId, decimal Amount, PayoutCategory Category, string? Description) : ICommand<CashPayoutDto>;
public record GetCashPayoutsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashPayoutDto>>;   // null = today
public static CashPayoutDto ToDto(this CashPayout payout);   // in CashPayoutDto.cs
```

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Payouts/AddCashPayoutCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Payouts;

public class AddCashPayoutCommandHandlerTests
{
    private static AddCashPayoutCommandHandler HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock, context.StoreIdentity);

    private static AddCashPayoutCommand Command(decimal amount = 150m, PayoutCategory? category = null) =>
        new(CashDrawerTestContext.CashierId, amount, category, "ค่าน้ำแข็ง");

    [Fact]
    public async Task HandleAsync_WithZeroAmount_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(amount: 0m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithZeroAmount_WritesNothing()
    {
        await using var context = new CashDrawerTestContext();

        try { await HandlerFor(context).HandleAsync(Command(amount: 0m)); } catch (CashEntryValidationException) { }

        context.Db.CashPayouts.Should()
                              .BeEmpty();
        context.OutboxEvents().Should()
                              .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithUndefinedCategory_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(category: (PayoutCategory)7));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithoutCategory_DefaultsToGeneral()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command(category: null));

        result.Category.Should()
                       .Be(PayoutCategory.General);
    }

    [Fact]
    public async Task HandleAsync_WithHardwareCategory_KeepsHardware()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command(category: PayoutCategory.Hardware));

        result.Category.Should()
                       .Be(PayoutCategory.Hardware);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_StampsTodayFromTheServerClock()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command());

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_RecordsTheCashierFromTheCommand()
    {
        await using var context = new CashDrawerTestContext();

        var result = await HandlerFor(context).HandleAsync(Command());

        result.CreatedByUserId.Should()
                              .Be(CashDrawerTestContext.CashierId);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_WritesOnePayoutChangedEvent()
    {
        await using var context = new CashDrawerTestContext();

        await HandlerFor(context).HandleAsync(Command());

        context.OutboxEvents().Should()
                              .ContainSingle(e => e.Type == CashDrawerOutbox.CashPayoutChanged);
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Payouts/EditCashPayoutCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Payouts;

public class EditCashPayoutCommandHandlerTests
{
    private const decimal EditedAmount = 275m;

    private static EditCashPayoutCommandHandler HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock);

    private static EditCashPayoutCommand Command(Guid id, decimal amount = EditedAmount) =>
        new(id, CashDrawerTestContext.OtherCashierId, amount, PayoutCategory.Hardware, "แก้ไข");

    [Fact]
    public async Task HandleAsync_WithUnknownId_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(Guid.NewGuid()));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedId_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WithYesterdaysPayout_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task HandleAsync_WhenCreatedBeforeMidnightAndEditedAfter_Throws()
    {
        await using var context = new CashDrawerTestContext();
        context.Time.SetUtcNow(new DateTimeOffset(2026, 9, 26, 16, 59, 0, TimeSpan.Zero)); // 23:59 Bangkok
        var added = await new AddCashPayoutCommandHandler(context.EntryRepository<CashPayout>(), context.Clock, context.StoreIdentity)
            .HandleAsync(new AddCashPayoutCommand(CashDrawerTestContext.CashierId, 100m, null, null));
        context.Time.Advance(TimeSpan.FromMinutes(2)); // 00:01 Bangkok, next day

        var act = () => HandlerFor(context).HandleAsync(Command(added.Id));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task HandleAsync_WithYesterdaysPayout_LeavesTheRowUnchanged()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        try { await HandlerFor(context).HandleAsync(Command(payout.Id)); } catch (CashDayClosedException) { }

        var stored = await context.Db.CashPayouts.AsNoTracking().SingleAsync(p => p.Id == payout.Id);
        stored.Amount.Should()
                     .Be(payout.Amount);
    }

    [Fact]
    public async Task HandleAsync_WithNegativeAmount_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id, amount: -1m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_UpdatesTheAmount()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(Command(payout.Id));

        result.Amount.Should()
                     .Be(EditedAmount);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_RecordsTheEditor()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(Command(payout.Id));

        result.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.OtherCashierId);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_KeepsItsBusinessDate()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(Command(payout.Id));

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_WritesOnePayoutChangedEvent()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        context.OutboxEvents().Should()
                              .ContainSingle(e => e.Type == CashDrawerOutbox.CashPayoutChanged);
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Payouts/GetCashPayoutsQueryHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Payouts;

public class GetCashPayoutsQueryHandlerTests
{
    private static GetCashPayoutsQueryHandler HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock, context.StoreIdentity);

    [Fact]
    public async Task HandleAsync_WithSoftDeletedPayout_ExcludesIt()
    {
        await using var context = new CashDrawerTestContext();
        var deleted = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        var result = await HandlerFor(context).HandleAsync(new GetCashPayoutsQuery(null));

        result.Should()
              .NotContain(p => p.Id == deleted.Id);
    }

    [Fact]
    public async Task HandleAsync_WithoutDate_ReturnsOnlyToday()
    {
        await using var context = new CashDrawerTestContext();
        var yesterdays = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);
        var todays = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        var result = await HandlerFor(context).HandleAsync(new GetCashPayoutsQuery(null));

        result.Select(p => p.Id).Should()
                                .Equal(todays.Id);
    }

    [Fact]
    public async Task HandleAsync_WithPastDate_ReturnsThatDay()
    {
        await using var context = new CashDrawerTestContext();
        var yesterdays = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        var result = await HandlerFor(context).HandleAsync(new GetCashPayoutsQuery(CashDrawerTestContext.Yesterday));

        result.Select(p => p.Id).Should()
                                .Equal(yesterdays.Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Payouts"`
Expected: build FAILS — payout commands/handlers do not exist.

- [ ] **Step 3: Write the implementation**

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/CashPayoutDto.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

public record CashPayoutDto(
    Guid Id,
    PayoutCategory Category,
    decimal Amount,
    string? Description,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId);

public static class CashPayoutMapping
{
    public static CashPayoutDto ToDto(this CashPayout payout) => new(
        payout.Id,
        payout.Category,
        payout.Amount,
        payout.Description,
        payout.BusinessDate,
        payout.CreatedUtc,
        payout.LastModifiedUtc,
        payout.CreatedByUserId,
        payout.LastModifiedByUserId);
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/CashPayoutRequests.cs`:

```csharp
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <summary>Request bodies. No user id and no date: the server sets both.</summary>
public record AddCashPayoutRequest(decimal Amount, PayoutCategory? Category, string? Description);

public record EditCashPayoutRequest(decimal Amount, PayoutCategory Category, string? Description);
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/AddCashPayoutCommand.cs`:

```csharp
using IndyPOS.Domain.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
/// <param name="Category">Null means General.</param>
public record AddCashPayoutCommand(Guid UserId, decimal Amount, PayoutCategory? Category, string? Description)
    : ICommand<CashPayoutDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/AddCashPayoutCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

public class AddCashPayoutCommandHandler(
    ICashEntryRepository<CashPayout> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddCashPayoutCommand, CashPayoutDto>
{
    public async Task<CashPayoutDto> HandleAsync(AddCashPayoutCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var category = CashEntryRules.EnsureDefined(command.Category ?? PayoutCategory.General);
        var description = CashEntryRules.NormalizeDescription(command.Description);
        var now = clock.Now();

        var payout = new CashPayout
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            Category = category,
            Amount = command.Amount,
            Description = description,
            BusinessDate = now.BusinessDate,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(payout, CashDrawerOutbox.Changed(payout, now.Utc), cancellationToken);
        return payout.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/EditCashPayoutCommand.cs`:

```csharp
using IndyPOS.Domain.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record EditCashPayoutCommand(Guid Id, Guid UserId, decimal Amount, PayoutCategory Category, string? Description)
    : ICommand<CashPayoutDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/EditCashPayoutCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <summary>Last save wins: no version check, by decision (one shared drawer).</summary>
public class EditCashPayoutCommandHandler(
    ICashEntryRepository<CashPayout> repository,
    ICashDrawerClock clock) : ICommandHandler<EditCashPayoutCommand, CashPayoutDto>
{
    public async Task<CashPayoutDto> HandleAsync(EditCashPayoutCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var category = CashEntryRules.EnsureDefined(command.Category);
        var description = CashEntryRules.NormalizeDescription(command.Description);

        var payout = await repository.FindAsync(command.Id, cancellationToken)
                     ?? throw new CashEntryNotFoundException(command.Id);
        var now = clock.Now();
        CashDayGuard.EnsureEditable(payout.BusinessDate, now.BusinessDate);

        payout.Amount = command.Amount;
        payout.Category = category;
        payout.Description = description;
        payout.Touch(command.UserId, now.Utc);

        await repository.SaveChangesAsync(CashDrawerOutbox.Changed(payout, now.Utc), cancellationToken);
        return payout.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/GetCashPayoutsQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <param name="BusinessDate">Null means today (store timezone). Past days are viewable, not editable.</param>
public record GetCashPayoutsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashPayoutDto>>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts/GetCashPayoutsQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

public class GetCashPayoutsQueryHandler(
    ICashEntryRepository<CashPayout> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashPayoutsQuery, IReadOnlyList<CashPayoutDto>>
{
    public async Task<IReadOnlyList<CashPayoutDto>> HandleAsync(GetCashPayoutsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var payouts = await repository.ListAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return payouts.Select(p => p.ToDto()).ToList();
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Payouts"`
Expected: PASS (21).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Payouts tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Payouts
git commit -m "feat(cash): add cash payout add, edit and list use cases"
```

---

### Task 5: Idempotent soft-delete for all three entry types

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Delete/DeleteCashEntryCommand.cs`, `DeleteCashEntryCommandHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Delete/DeleteCashEntryCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ICashEntryRepository<TEntry>.FindIncludingDeletedAsync`, `.SaveChangesAsync`; `CashDayGuard`; `CashDrawerOutbox.Changed`; `CashDrawerEntry.MarkDeleted`.
- Produces:

```csharp
public record DeleteCashEntryCommand<TEntry>(Guid Id, Guid UserId) : ICommand where TEntry : CashDrawerEntry;
public class DeleteCashEntryCommandHandler<TEntry>(ICashEntryRepository<TEntry> repository, ICashDrawerClock clock)
    : ICommandHandler<DeleteCashEntryCommand<TEntry>> where TEntry : CashDrawerEntry;
```

Rule order (deliberate): unknown → `404`; **already deleted → no-op (checked before the day rule**, so re-deleting yesterday's already-deleted row is still a harmless `204`); past day → `409`; else soft-delete + event.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Delete/DeleteCashEntryCommandHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Delete;

public class DeleteCashEntryCommandHandlerTests
{
    private static DeleteCashEntryCommandHandler<CashPayout> HandlerFor(CashDrawerTestContext context) =>
        new(context.EntryRepository<CashPayout>(), context.Clock);

    private static DeleteCashEntryCommand<CashPayout> Command(Guid id) =>
        new(id, CashDrawerTestContext.OtherCashierId);

    private static Task<CashPayout> StoredAsync(CashDrawerTestContext context, Guid id) =>
        context.Db.CashPayouts.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == id);

    [Fact]
    public async Task HandleAsync_WithUnknownId_Throws()
    {
        await using var context = new CashDrawerTestContext();

        var act = () => HandlerFor(context).HandleAsync(Command(Guid.NewGuid()));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_WithYesterdaysPayout_Throws()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task HandleAsync_WithYesterdaysPayout_LeavesItActive()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday);

        try { await HandlerFor(context).HandleAsync(Command(payout.Id)); } catch (CashDayClosedException) { }

        (await StoredAsync(context, payout.Id)).IsDeleted.Should()
                                                         .BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_DoesNotThrow()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_KeepsTheOriginalDeleter()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        var stored = await StoredAsync(context, payout.Id);
        stored.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.CashierId);
        stored.DeletedUtc.Should()
                         .Be(payout.DeletedUtc);
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyDeleted_WritesNoEvent()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today, isDeleted: true);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        context.OutboxEvents().Should()
                              .BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenYesterdaysPayoutAlreadyDeleted_DoesNotThrow()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Yesterday, isDeleted: true);

        var act = () => HandlerFor(context).HandleAsync(Command(payout.Id));

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_SoftDeletesIt()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        var stored = await StoredAsync(context, payout.Id);
        stored.IsDeleted.Should()
                        .BeTrue();
        stored.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.OtherCashierId);
    }

    [Fact]
    public async Task HandleAsync_WithTodaysPayout_WritesOneEventCarryingTheDeletedFlag()
    {
        await using var context = new CashDrawerTestContext();
        var payout = await context.SeedPayoutAsync(CashDrawerTestContext.Today);

        await HandlerFor(context).HandleAsync(Command(payout.Id));

        context.OutboxEvents().Should()
                              .ContainSingle(e => e.Type == CashDrawerOutbox.CashPayoutChanged
                                                  && e.PayloadJson.Contains("\"IsDeleted\":true"));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Delete"`
Expected: build FAILS — `DeleteCashEntryCommandHandler<>` does not exist.

- [ ] **Step 3: Write the implementation**

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Delete/DeleteCashEntryCommand.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record DeleteCashEntryCommand<TEntry>(Guid Id, Guid UserId) : ICommand
    where TEntry : CashDrawerEntry;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Delete/DeleteCashEntryCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;

/// <summary>
/// Soft-deletes a payout, float or debt repayment. Idempotent: a repeat delete (double-click, retry
/// after a network blip) changes nothing and writes no event, so the audit record is never rewritten.
/// </summary>
public class DeleteCashEntryCommandHandler<TEntry>(
    ICashEntryRepository<TEntry> repository,
    ICashDrawerClock clock) : ICommandHandler<DeleteCashEntryCommand<TEntry>>
    where TEntry : CashDrawerEntry
{
    public async Task HandleAsync(DeleteCashEntryCommand<TEntry> command, CancellationToken cancellationToken = default)
    {
        var entry = await repository.FindIncludingDeletedAsync(command.Id, cancellationToken)
                    ?? throw new CashEntryNotFoundException(command.Id);

        // Before the day rule on purpose: re-deleting any already-deleted row is a harmless no-op.
        if (entry.IsDeleted)
            return;

        var now = clock.Now();
        CashDayGuard.EnsureEditable(entry.BusinessDate, now.BusinessDate);

        entry.MarkDeleted(command.UserId, now.Utc);
        await repository.SaveChangesAsync(CashDrawerOutbox.Changed(entry, now.Utc), cancellationToken);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Delete"`
Expected: PASS (9).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Delete tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Delete
git commit -m "feat(cash): add idempotent soft-delete for cash drawer entries"
```

---

### Task 6: Cash floats and debt repayments — add, edit, list

Same rules as payouts (Task 4) — code repeated in full on purpose so this task reads standalone. Floats have `Description`, no category. Debt repayments have a **required** `CustomerName`, no description, no category.

**Files:**
- Create in `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/`: `CashFloatDto.cs`, `CashFloatRequests.cs`, `AddCashFloatCommand.cs`, `AddCashFloatCommandHandler.cs`, `EditCashFloatCommand.cs`, `EditCashFloatCommandHandler.cs`, `GetCashFloatsQuery.cs`, `GetCashFloatsQueryHandler.cs`
- Create in `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/`: `DebtRepaymentDto.cs`, `DebtRepaymentRequests.cs`, `AddDebtRepaymentCommand.cs`, `AddDebtRepaymentCommandHandler.cs`, `EditDebtRepaymentCommand.cs`, `EditDebtRepaymentCommandHandler.cs`, `GetDebtRepaymentsQuery.cs`, `GetDebtRepaymentsQueryHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Floats/CashFloatHandlersTests.cs`, `.../DebtRepayments/DebtRepaymentHandlersTests.cs`

**Interfaces:**
- Consumes: as Task 4, with `ICashEntryRepository<CashFloat>` / `<DebtRepayment>`.
- Produces:

```csharp
public record CashFloatDto(Guid Id, decimal Amount, string? Description, DateOnly BusinessDate,
    DateTime CreatedUtc, DateTime LastModifiedUtc, Guid CreatedByUserId, Guid? LastModifiedByUserId);
public record AddCashFloatRequest(decimal Amount, string? Description);
public record EditCashFloatRequest(decimal Amount, string? Description);
public record AddCashFloatCommand(Guid UserId, decimal Amount, string? Description) : ICommand<CashFloatDto>;
public record EditCashFloatCommand(Guid Id, Guid UserId, decimal Amount, string? Description) : ICommand<CashFloatDto>;
public record GetCashFloatsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashFloatDto>>;

public record DebtRepaymentDto(Guid Id, string CustomerName, decimal Amount, DateOnly BusinessDate,
    DateTime CreatedUtc, DateTime LastModifiedUtc, Guid CreatedByUserId, Guid? LastModifiedByUserId);
public record AddDebtRepaymentRequest(string? CustomerName, decimal Amount);
public record EditDebtRepaymentRequest(string? CustomerName, decimal Amount);
public record AddDebtRepaymentCommand(Guid UserId, string? CustomerName, decimal Amount) : ICommand<DebtRepaymentDto>;
public record EditDebtRepaymentCommand(Guid Id, Guid UserId, string? CustomerName, decimal Amount) : ICommand<DebtRepaymentDto>;
public record GetDebtRepaymentsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<DebtRepaymentDto>>;
```

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Floats/CashFloatHandlersTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Floats;

public class CashFloatHandlersTests
{
    private static AddCashFloatCommandHandler AddHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<CashFloat>(), c.Clock, c.StoreIdentity);

    private static EditCashFloatCommandHandler EditHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<CashFloat>(), c.Clock);

    private static GetCashFloatsQueryHandler GetHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<CashFloat>(), c.Clock, c.StoreIdentity);

    private static async Task<CashFloat> SeedFloatAsync(CashDrawerTestContext c, DateOnly businessDate)
    {
        var cashFloat = new CashFloat
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, Amount = 500m, BusinessDate = businessDate,
            CreatedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            LastModifiedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            CreatedByUserId = CashDrawerTestContext.CashierId
        };
        c.Db.CashFloats.Add(cashFloat);
        await c.Db.SaveChangesAsync();
        c.Db.ChangeTracker.Clear();
        return cashFloat;
    }

    [Fact]
    public async Task Add_WithNegativeAmount_Throws()
    {
        await using var c = new CashDrawerTestContext();

        var act = () => AddHandler(c).HandleAsync(new AddCashFloatCommand(CashDrawerTestContext.CashierId, -5m, null));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Edit_WithUnknownId_Throws()
    {
        await using var c = new CashDrawerTestContext();

        var act = () => EditHandler(c).HandleAsync(new EditCashFloatCommand(Guid.NewGuid(), CashDrawerTestContext.CashierId, 10m, null));

        await act.Should()
                 .ThrowAsync<CashEntryNotFoundException>();
    }

    [Fact]
    public async Task Edit_WithYesterdaysFloat_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var cashFloat = await SeedFloatAsync(c, CashDrawerTestContext.Yesterday);

        var act = () => EditHandler(c).HandleAsync(new EditCashFloatCommand(cashFloat.Id, CashDrawerTestContext.CashierId, 10m, null));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task Add_WithValidCommand_StampsTodayAndWritesAFloatChangedEvent()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashFloatCommand(CashDrawerTestContext.CashierId, 1000m, " ทอนเช้า "));

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
        c.OutboxEvents().Should()
                        .ContainSingle(e => e.Type == CashDrawerOutbox.CashFloatChanged);
    }

    [Fact]
    public async Task Edit_WithTodaysFloat_UpdatesAmountAndEditor()
    {
        await using var c = new CashDrawerTestContext();
        var cashFloat = await SeedFloatAsync(c, CashDrawerTestContext.Today);

        var result = await EditHandler(c).HandleAsync(new EditCashFloatCommand(cashFloat.Id, CashDrawerTestContext.OtherCashierId, 700m, null));

        result.Amount.Should()
                     .Be(700m);
        result.LastModifiedByUserId.Should()
                                   .Be(CashDrawerTestContext.OtherCashierId);
    }

    [Fact]
    public async Task Get_WithoutDate_ReturnsOnlyToday()
    {
        await using var c = new CashDrawerTestContext();
        await SeedFloatAsync(c, CashDrawerTestContext.Yesterday);
        var todays = await SeedFloatAsync(c, CashDrawerTestContext.Today);

        var result = await GetHandler(c).HandleAsync(new GetCashFloatsQuery(null));

        result.Select(f => f.Id).Should()
                                .Equal(todays.Id);
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/DebtRepayments/DebtRepaymentHandlersTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Domain.Entities.Core;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.DebtRepayments;

public class DebtRepaymentHandlersTests
{
    private const string CustomerName = "ลุงสมชาย";

    private static AddDebtRepaymentCommandHandler AddHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<DebtRepayment>(), c.Clock, c.StoreIdentity);

    private static EditDebtRepaymentCommandHandler EditHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<DebtRepayment>(), c.Clock);

    private static GetDebtRepaymentsQueryHandler GetHandler(CashDrawerTestContext c) =>
        new(c.EntryRepository<DebtRepayment>(), c.Clock, c.StoreIdentity);

    private static async Task<DebtRepayment> SeedRepaymentAsync(CashDrawerTestContext c, DateOnly businessDate)
    {
        var repayment = new DebtRepayment
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, CustomerName = CustomerName, Amount = 300m,
            BusinessDate = businessDate,
            CreatedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            LastModifiedUtc = CashDrawerTestContext.TenAmBangkok.UtcDateTime,
            CreatedByUserId = CashDrawerTestContext.CashierId
        };
        c.Db.DebtRepayments.Add(repayment);
        await c.Db.SaveChangesAsync();
        c.Db.ChangeTracker.Clear();
        return repayment;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_WithBlankCustomerName_Throws(string? customerName)
    {
        await using var c = new CashDrawerTestContext();

        var act = () => AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, customerName, 300m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Add_WithZeroAmount_Throws()
    {
        await using var c = new CashDrawerTestContext();

        var act = () => AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, CustomerName, 0m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Edit_WithBlankCustomerName_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var repayment = await SeedRepaymentAsync(c, CashDrawerTestContext.Today);

        var act = () => EditHandler(c).HandleAsync(new EditDebtRepaymentCommand(repayment.Id, CashDrawerTestContext.CashierId, " ", 300m));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Edit_WithYesterdaysRepayment_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var repayment = await SeedRepaymentAsync(c, CashDrawerTestContext.Yesterday);

        var act = () => EditHandler(c).HandleAsync(new EditDebtRepaymentCommand(repayment.Id, CashDrawerTestContext.CashierId, CustomerName, 300m));

        await act.Should()
                 .ThrowAsync<CashDayClosedException>();
    }

    [Fact]
    public async Task Add_WithValidCommand_TrimsNameAndWritesARepaymentChangedEvent()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddDebtRepaymentCommand(CashDrawerTestContext.CashierId, "  ลุงสมชาย ", 300m));

        result.CustomerName.Should()
                           .Be(CustomerName);
        c.OutboxEvents().Should()
                        .ContainSingle(e => e.Type == CashDrawerOutbox.DebtRepaymentChanged);
    }

    [Fact]
    public async Task Get_WithoutDate_ReturnsOnlyToday()
    {
        await using var c = new CashDrawerTestContext();
        await SeedRepaymentAsync(c, CashDrawerTestContext.Yesterday);
        var todays = await SeedRepaymentAsync(c, CashDrawerTestContext.Today);

        var result = await GetHandler(c).HandleAsync(new GetDebtRepaymentsQuery(null));

        result.Select(r => r.Id).Should()
                                .Equal(todays.Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Floats|FullyQualifiedName~CashDrawer.DebtRepayments"`
Expected: build FAILS.

- [ ] **Step 3: Write the float implementation**

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/CashFloatDto.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

public record CashFloatDto(
    Guid Id,
    decimal Amount,
    string? Description,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId);

public static class CashFloatMapping
{
    public static CashFloatDto ToDto(this CashFloat cashFloat) => new(
        cashFloat.Id,
        cashFloat.Amount,
        cashFloat.Description,
        cashFloat.BusinessDate,
        cashFloat.CreatedUtc,
        cashFloat.LastModifiedUtc,
        cashFloat.CreatedByUserId,
        cashFloat.LastModifiedByUserId);
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/CashFloatRequests.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <summary>Request bodies. No user id and no date: the server sets both.</summary>
public record AddCashFloatRequest(decimal Amount, string? Description);

public record EditCashFloatRequest(decimal Amount, string? Description);
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/AddCashFloatCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record AddCashFloatCommand(Guid UserId, decimal Amount, string? Description) : ICommand<CashFloatDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/AddCashFloatCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

public class AddCashFloatCommandHandler(
    ICashEntryRepository<CashFloat> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddCashFloatCommand, CashFloatDto>
{
    public async Task<CashFloatDto> HandleAsync(AddCashFloatCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var description = CashEntryRules.NormalizeDescription(command.Description);
        var now = clock.Now();

        var cashFloat = new CashFloat
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            Amount = command.Amount,
            Description = description,
            BusinessDate = now.BusinessDate,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(cashFloat, CashDrawerOutbox.Changed(cashFloat, now.Utc), cancellationToken);
        return cashFloat.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/EditCashFloatCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record EditCashFloatCommand(Guid Id, Guid UserId, decimal Amount, string? Description) : ICommand<CashFloatDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/EditCashFloatCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <summary>Last save wins: no version check, by decision (one shared drawer).</summary>
public class EditCashFloatCommandHandler(
    ICashEntryRepository<CashFloat> repository,
    ICashDrawerClock clock) : ICommandHandler<EditCashFloatCommand, CashFloatDto>
{
    public async Task<CashFloatDto> HandleAsync(EditCashFloatCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var description = CashEntryRules.NormalizeDescription(command.Description);

        var cashFloat = await repository.FindAsync(command.Id, cancellationToken)
                        ?? throw new CashEntryNotFoundException(command.Id);
        var now = clock.Now();
        CashDayGuard.EnsureEditable(cashFloat.BusinessDate, now.BusinessDate);

        cashFloat.Amount = command.Amount;
        cashFloat.Description = description;
        cashFloat.Touch(command.UserId, now.Utc);

        await repository.SaveChangesAsync(CashDrawerOutbox.Changed(cashFloat, now.Utc), cancellationToken);
        return cashFloat.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/GetCashFloatsQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <param name="BusinessDate">Null means today (store timezone). Past days are viewable, not editable.</param>
public record GetCashFloatsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashFloatDto>>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Floats/GetCashFloatsQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

public class GetCashFloatsQueryHandler(
    ICashEntryRepository<CashFloat> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashFloatsQuery, IReadOnlyList<CashFloatDto>>
{
    public async Task<IReadOnlyList<CashFloatDto>> HandleAsync(GetCashFloatsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var floats = await repository.ListAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return floats.Select(f => f.ToDto()).ToList();
    }
}
```

- [ ] **Step 4: Write the debt-repayment implementation**

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/DebtRepaymentDto.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

public record DebtRepaymentDto(
    Guid Id,
    string CustomerName,
    decimal Amount,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId);

public static class DebtRepaymentMapping
{
    public static DebtRepaymentDto ToDto(this DebtRepayment repayment) => new(
        repayment.Id,
        repayment.CustomerName,
        repayment.Amount,
        repayment.BusinessDate,
        repayment.CreatedUtc,
        repayment.LastModifiedUtc,
        repayment.CreatedByUserId,
        repayment.LastModifiedByUserId);
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/DebtRepaymentRequests.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <summary>Request bodies. No user id and no date: the server sets both.</summary>
public record AddDebtRepaymentRequest(string? CustomerName, decimal Amount);

public record EditDebtRepaymentRequest(string? CustomerName, decimal Amount);
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/AddDebtRepaymentCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record AddDebtRepaymentCommand(Guid UserId, string? CustomerName, decimal Amount) : ICommand<DebtRepaymentDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/AddDebtRepaymentCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

public class AddDebtRepaymentCommandHandler(
    ICashEntryRepository<DebtRepayment> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddDebtRepaymentCommand, DebtRepaymentDto>
{
    public async Task<DebtRepaymentDto> HandleAsync(AddDebtRepaymentCommand command, CancellationToken cancellationToken = default)
    {
        var customerName = CashEntryRules.NormalizeCustomerName(command.CustomerName);
        CashEntryRules.EnsureValidAmount(command.Amount);
        var now = clock.Now();

        var repayment = new DebtRepayment
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            CustomerName = customerName,
            Amount = command.Amount,
            BusinessDate = now.BusinessDate,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(repayment, CashDrawerOutbox.Changed(repayment, now.Utc), cancellationToken);
        return repayment.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/EditDebtRepaymentCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record EditDebtRepaymentCommand(Guid Id, Guid UserId, string? CustomerName, decimal Amount) : ICommand<DebtRepaymentDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/EditDebtRepaymentCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <summary>Last save wins: no version check, by decision (one shared drawer).</summary>
public class EditDebtRepaymentCommandHandler(
    ICashEntryRepository<DebtRepayment> repository,
    ICashDrawerClock clock) : ICommandHandler<EditDebtRepaymentCommand, DebtRepaymentDto>
{
    public async Task<DebtRepaymentDto> HandleAsync(EditDebtRepaymentCommand command, CancellationToken cancellationToken = default)
    {
        var customerName = CashEntryRules.NormalizeCustomerName(command.CustomerName);
        CashEntryRules.EnsureValidAmount(command.Amount);

        var repayment = await repository.FindAsync(command.Id, cancellationToken)
                        ?? throw new CashEntryNotFoundException(command.Id);
        var now = clock.Now();
        CashDayGuard.EnsureEditable(repayment.BusinessDate, now.BusinessDate);

        repayment.CustomerName = customerName;
        repayment.Amount = command.Amount;
        repayment.Touch(command.UserId, now.Utc);

        await repository.SaveChangesAsync(CashDrawerOutbox.Changed(repayment, now.Utc), cancellationToken);
        return repayment.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/GetDebtRepaymentsQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <param name="BusinessDate">Null means today (store timezone). Past days are viewable, not editable.</param>
public record GetDebtRepaymentsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<DebtRepaymentDto>>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/DebtRepayments/GetDebtRepaymentsQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

public class GetDebtRepaymentsQueryHandler(
    ICashEntryRepository<DebtRepayment> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetDebtRepaymentsQuery, IReadOnlyList<DebtRepaymentDto>>
{
    public async Task<IReadOnlyList<DebtRepaymentDto>> HandleAsync(GetDebtRepaymentsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var repayments = await repository.ListAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return repayments.Select(r => r.ToDto()).ToList();
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Floats|FullyQualifiedName~CashDrawer.DebtRepayments"`
Expected: PASS (15).

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/CashDrawer tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer
git commit -m "feat(cash): add cash float and debt repayment use cases"
```

---

### Task 7: Cash counts — append-only add and day history

**Files:**
- Create in `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/`: `CashCountDto.cs`, `AddCashCountRequest.cs`, `AddCashCountCommand.cs`, `AddCashCountCommandHandler.cs`, `GetCashCountsQuery.cs`, `GetCashCountsQueryHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Counts/CashCountHandlersTests.cs`

**Interfaces:**
- Consumes: `ICashCountRepository`, `ICashDrawerClock`, `IStoreIdentityService`, `CashEntryRules.EnsureValidCounts`, `CashDrawerOutbox.CountAdded`.
- Produces:

```csharp
public record CashCountDto(Guid Id, DateOnly BusinessDate, int BankNote1000Count, int BankNote500Count, int BankNote100Count,
    int BankNote50Count, int BankNote20Count, int Coin10Count, int Coin5Count, int Coin2Count, int Coin1Count,
    decimal CountedTotal, DateTime CreatedUtc, Guid CreatedByUserId);
public static CashCountDto ToDto(this CashCount count);
public record AddCashCountRequest(int BankNote1000Count, int BankNote500Count, int BankNote100Count, int BankNote50Count,
    int BankNote20Count, int Coin10Count, int Coin5Count, int Coin2Count, int Coin1Count);
public record AddCashCountCommand(Guid UserId, AddCashCountRequest Counts) : ICommand<CashCountDto>;
public record GetCashCountsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashCountDto>>;   // newest first
```

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Counts/CashCountHandlersTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Counts;

public class CashCountHandlersTests
{
    private static readonly AddCashCountRequest MorningCount = new(1, 0, 5, 0, 0, 10, 0, 0, 3);   // 1×1000 + 5×100 + 10×10 + 3×1 = 1603
    private static readonly AddCashCountRequest EveningCount = new(2, 0, 0, 0, 0, 0, 0, 0, 0);    // 2000

    private static AddCashCountCommandHandler AddHandler(CashDrawerTestContext c) =>
        new(c.CountRepository(), c.Clock, c.StoreIdentity);

    private static GetCashCountsQueryHandler GetHandler(CashDrawerTestContext c) =>
        new(c.CountRepository(), c.Clock, c.StoreIdentity);

    [Fact]
    public async Task Add_WithANegativeDenomination_Throws()
    {
        await using var c = new CashDrawerTestContext();
        var negative = MorningCount with { Coin5Count = -1 };

        var act = () => AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, negative));

        await act.Should()
                 .ThrowAsync<CashEntryValidationException>();
    }

    [Fact]
    public async Task Add_WithANegativeDenomination_WritesNothing()
    {
        await using var c = new CashDrawerTestContext();
        var negative = MorningCount with { BankNote1000Count = -3 };

        try { await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, negative)); }
        catch (CashEntryValidationException) { }

        c.Db.CashCounts.Should()
                       .BeEmpty();
    }

    [Fact]
    public async Task Add_WhenSecondCountSameDay_KeepsBothRows()
    {
        await using var c = new CashDrawerTestContext();
        await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));
        c.Time.Advance(TimeSpan.FromHours(10));

        await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.OtherCashierId, EveningCount));

        c.Db.CashCounts.Should()
                       .HaveCount(2);
    }

    [Fact]
    public async Task Add_WithValidCounts_ReturnsTheCountedTotal()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));

        result.CountedTotal.Should()
                           .Be(1603m);
    }

    [Fact]
    public async Task Add_WithValidCounts_RecordsWhoCountedAndWritesACountChangedEvent()
    {
        await using var c = new CashDrawerTestContext();

        var result = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));

        result.CreatedByUserId.Should()
                              .Be(CashDrawerTestContext.CashierId);
        c.OutboxEvents().Should()
                        .ContainSingle(e => e.Type == CashDrawerOutbox.CashCountChanged);
    }

    [Fact]
    public async Task Get_WithTwoCountsToday_ReturnsNewestFirst()
    {
        await using var c = new CashDrawerTestContext();
        var morning = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, MorningCount));
        c.Time.Advance(TimeSpan.FromHours(10));
        var evening = await AddHandler(c).HandleAsync(new AddCashCountCommand(CashDrawerTestContext.OtherCashierId, EveningCount));

        var result = await GetHandler(c).HandleAsync(new GetCashCountsQuery(null));

        result.Select(r => r.Id).Should()
                                .Equal(evening.Id, morning.Id);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Counts"`
Expected: build FAILS.

- [ ] **Step 3: Write the implementation**

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/CashCountDto.cs`:

```csharp
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

public record CashCountDto(
    Guid Id,
    DateOnly BusinessDate,
    int BankNote1000Count,
    int BankNote500Count,
    int BankNote100Count,
    int BankNote50Count,
    int BankNote20Count,
    int Coin10Count,
    int Coin5Count,
    int Coin2Count,
    int Coin1Count,
    decimal CountedTotal,
    DateTime CreatedUtc,
    Guid CreatedByUserId);

public static class CashCountMapping
{
    public static CashCountDto ToDto(this CashCount count) => new(
        count.Id,
        count.BusinessDate,
        count.BankNote1000Count,
        count.BankNote500Count,
        count.BankNote100Count,
        count.BankNote50Count,
        count.BankNote20Count,
        count.Coin10Count,
        count.Coin5Count,
        count.Coin2Count,
        count.Coin1Count,
        count.CountedTotal,
        count.CreatedUtc,
        count.CreatedByUserId);
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/AddCashCountRequest.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <summary>The nine denomination counts. No user id and no date: the server sets both.</summary>
public record AddCashCountRequest(
    int BankNote1000Count,
    int BankNote500Count,
    int BankNote100Count,
    int BankNote50Count,
    int BankNote20Count,
    int Coin10Count,
    int Coin5Count,
    int Coin2Count,
    int Coin1Count);
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/AddCashCountCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <param name="UserId">Who counted, taken from the login token by the endpoint.</param>
public record AddCashCountCommand(Guid UserId, AddCashCountRequest Counts) : ICommand<CashCountDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/AddCashCountCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <summary>Every count is a new row; nothing is overwritten. A mistyped count is fixed by counting again.</summary>
public class AddCashCountCommandHandler(
    ICashCountRepository repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddCashCountCommand, CashCountDto>
{
    public async Task<CashCountDto> HandleAsync(AddCashCountCommand command, CancellationToken cancellationToken = default)
    {
        var c = command.Counts;
        CashEntryRules.EnsureValidCounts(
            c.BankNote1000Count, c.BankNote500Count, c.BankNote100Count, c.BankNote50Count, c.BankNote20Count,
            c.Coin10Count, c.Coin5Count, c.Coin2Count, c.Coin1Count);
        var now = clock.Now();

        var count = new CashCount
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            BusinessDate = now.BusinessDate,
            BankNote1000Count = c.BankNote1000Count,
            BankNote500Count = c.BankNote500Count,
            BankNote100Count = c.BankNote100Count,
            BankNote50Count = c.BankNote50Count,
            BankNote20Count = c.BankNote20Count,
            Coin10Count = c.Coin10Count,
            Coin5Count = c.Coin5Count,
            Coin2Count = c.Coin2Count,
            Coin1Count = c.Coin1Count,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(count, CashDrawerOutbox.CountAdded(count, now.Utc), cancellationToken);
        return count.ToDto();
    }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/GetCashCountsQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <summary>The day's counts, newest first — the history list. The first row is the one used for calculations.</summary>
/// <param name="BusinessDate">Null means today (store timezone).</param>
public record GetCashCountsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashCountDto>>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts/GetCashCountsQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

public class GetCashCountsQueryHandler(
    ICashCountRepository repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>>
{
    public async Task<IReadOnlyList<CashCountDto>> HandleAsync(GetCashCountsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var counts = await repository.ListNewestFirstAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return counts.Select(c => c.ToDto()).ToList();
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Counts"`
Expected: PASS (6).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Counts tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Counts
git commit -m "feat(cash): add append-only cash count use cases"
```

---

### Task 8: The cash-drawer summary — all the maths in one query

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/GetCashDrawerSummaryQuery.cs`, `CashDrawerSummaryDto.cs`, `CashDrawerCalculator.cs`
- Create: `src/IndyPOS.Infrastructure/QueryHandlers/CashDrawer/GetCashDrawerSummaryQueryHandler.cs` (report handlers live in Infrastructure, next to `GetLegacySalesSummaryQueryHandler`)
- Test: `tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary/CashDrawerCalculatorTests.cs`, `GetCashDrawerSummaryQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>` (`InvoiceTotalWithoutPayLaterPayments`, `GeneralProductsTotal`, `HardwareProductsTotal`, `PayLaterPaymentsTotalForGeneralProducts`, `PayLaterPaymentsTotalForHardwareProducts`); `IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>` (`MoneyTransferTotal`, `WelfareCardTotal`); `ICashEntryRepository<CashPayout|CashFloat|DebtRepayment>.ListAsync`; `ICashCountRepository.GetLatestAsync`; `ICashDrawerClock`; `IStoreIdentityService`.
- Produces:

```csharp
public record GetCashDrawerSummaryQuery(DateOnly? BusinessDate) : IQuery<CashDrawerSummaryDto>;
public record CashDrawerSummaryDto { /* see below */ }
public readonly record struct CashDrawerTotals(decimal CashSalesTotal, decimal DebtRepaymentsTotal, decimal CashFloatsTotal,
    decimal MoneyTransferTotal, decimal WelfareCardTotal, decimal PayoutsTotal);
public static class CashDrawerCalculator
{
    public static decimal ExpectedCash(CashDrawerTotals totals);
    public static decimal? Difference(decimal? countedCash, decimal expectedCash);
}
```

Formula, **unchanged from today's `CashFlowData.CalculateExpectedCash`**: `cash sales (excl. PayLater) + debt repayments + cash floats − money transfer − welfare card − payouts`. Difference = latest count's total − expected (positive = over, negative = short); `null` when nothing has been counted that day.

- [ ] **Step 1: Write the failing tests**

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary/CashDrawerCalculatorTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Summary;

public class CashDrawerCalculatorTests
{
    private static readonly CashDrawerTotals Day = new(
        CashSalesTotal: 10_000m,
        DebtRepaymentsTotal: 300m,
        CashFloatsTotal: 1_000m,
        MoneyTransferTotal: 2_500m,
        WelfareCardTotal: 400m,
        PayoutsTotal: 650m);

    [Fact]
    public void Difference_WithNoCount_ReturnsNull()
    {
        var result = CashDrawerCalculator.Difference(countedCash: null, expectedCash: 100m);

        result.Should()
              .BeNull();
    }

    [Fact]
    public void Difference_WhenCountIsShort_ReturnsNegative()
    {
        var result = CashDrawerCalculator.Difference(countedCash: 90m, expectedCash: 100m);

        result.Should()
              .Be(-10m);
    }

    [Fact]
    public void ExpectedCash_WithPayoutsAboveTakings_ReturnsNegative()
    {
        var result = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(0m, 0m, 0m, 0m, 0m, 50m));

        result.Should()
              .Be(-50m);
    }

    [Fact]
    public void ExpectedCash_WithAllZero_ReturnsZero()
    {
        var result = CashDrawerCalculator.ExpectedCash(default);

        result.Should()
              .Be(0m);
    }

    [Fact]
    public void ExpectedCash_WithATypicalDay_ReturnsTodaysFormula()
    {
        var result = CashDrawerCalculator.ExpectedCash(Day);

        result.Should()
              .Be(10_000m + 300m + 1_000m - 2_500m - 400m - 650m);
    }

    [Fact]
    public void Difference_WhenCountIsOver_ReturnsPositive()
    {
        var result = CashDrawerCalculator.Difference(countedCash: 120m, expectedCash: 100m);

        result.Should()
              .Be(20m);
    }
}
```

`tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary/GetCashDrawerSummaryQueryHandlerTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.QueryHandlers.CashDrawer;
using Moq;
using Nokpirab;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer.Summary;

public class GetCashDrawerSummaryQueryHandlerTests
{
    private const decimal CashSales = 5_000m;

    private static GetCashDrawerSummaryQueryHandler HandlerFor(CashDrawerTestContext c)
    {
        var sales = new Mock<IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>>();
        sales.Setup(h => h.HandleAsync(It.IsAny<GetLegacySalesSummaryQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new SalesSummary { InvoiceTotalWithoutPayLaterPayments = CashSales });
        var payments = new Mock<IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>>();
        payments.Setup(h => h.HandleAsync(It.IsAny<GetLegacyPaymentsSummaryQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentsSummary());

        return new GetCashDrawerSummaryQueryHandler(
            sales.Object, payments.Object, c.Db, c.CountRepository(), c.Clock, c.StoreIdentity);
    }

    private static Task AddCountAsync(CashDrawerTestContext c, AddCashCountRequest counts) =>
        new AddCashCountCommandHandler(c.CountRepository(), c.Clock, c.StoreIdentity)
            .HandleAsync(new AddCashCountCommand(CashDrawerTestContext.CashierId, counts));

    private static async Task AddPayoutAsync(CashDrawerTestContext c, decimal amount, PayoutCategory category, bool isDeleted = false)
    {
        c.Db.CashPayouts.Add(new CashPayout
        {
            Id = Guid.NewGuid(), StoreId = c.StoreIdentity.StoreId, Amount = amount, Category = category,
            BusinessDate = CashDrawerTestContext.Today, CreatedByUserId = CashDrawerTestContext.CashierId,
            IsDeleted = isDeleted
        });
        await c.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task HandleAsync_WithNoCountToday_ReturnsNullDifference()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CashDifference.Should()
                             .BeNull();
        result.LatestCount.Should()
                          .BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithSoftDeletedPayout_ExcludesItFromTotals()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);
        await AddPayoutAsync(c, 999m, PayoutCategory.General, isDeleted: true);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.PayoutsTotal.Should()
                           .Be(100m);
    }

    [Fact]
    public async Task HandleAsync_WithTwoCounts_UsesOnlyTheLatest()
    {
        await using var c = new CashDrawerTestContext();
        await AddCountAsync(c, new AddCashCountRequest(9, 0, 0, 0, 0, 0, 0, 0, 0));   // 9,000 at 10:00
        c.Time.Advance(TimeSpan.FromHours(10));
        await AddCountAsync(c, new AddCashCountRequest(5, 0, 0, 0, 0, 0, 0, 0, 0));   // 5,000 at 20:00

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.CountedCash.Should()
                          .Be(5_000m);
        result.CashDifference.Should()
                             .Be(5_000m - CashSales);
    }

    [Fact]
    public async Task HandleAsync_WithPastDate_ReturnsNotEditable()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(CashDrawerTestContext.Yesterday));

        result.IsEditable.Should()
                         .BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithPayoutsInBothCategories_SplitsTheirTotals()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);
        await AddPayoutAsync(c, 250m, PayoutCategory.Hardware);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.GeneralPayoutsTotal.Should()
                                  .Be(100m);
        result.HardwarePayoutsTotal.Should()
                                   .Be(250m);
    }

    [Fact]
    public async Task HandleAsync_WithPayouts_SubtractsThemFromExpectedCash()
    {
        await using var c = new CashDrawerTestContext();
        await AddPayoutAsync(c, 100m, PayoutCategory.General);

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.ExpectedCash.Should()
                           .Be(CashSales - 100m);
    }

    [Fact]
    public async Task HandleAsync_WithoutDate_ReturnsTodayAsEditable()
    {
        await using var c = new CashDrawerTestContext();

        var result = await HandlerFor(c).HandleAsync(new GetCashDrawerSummaryQuery(null));

        result.BusinessDate.Should()
                           .Be(CashDrawerTestContext.Today);
        result.IsEditable.Should()
                         .BeTrue();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Summary"`
Expected: build FAILS.

- [ ] **Step 3: Write the Application side**

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/GetCashDrawerSummaryQuery.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

/// <param name="BusinessDate">Null means today (store timezone).</param>
public record GetCashDrawerSummaryQuery(DateOnly? BusinessDate) : IQuery<CashDrawerSummaryDto>;
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/CashDrawerSummaryDto.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

/// <summary>
/// Everything the cash-drawer screen shows, already computed. The UI renders it and does no maths,
/// so the Avalonia port inherits the rules for free.
/// </summary>
public record CashDrawerSummaryDto
{
    public required DateOnly BusinessDate { get; init; }

    /// <summary>True only for today; the ViewModel disables edit controls otherwise. The server still enforces it.</summary>
    public required bool IsEditable { get; init; }

    public required decimal GeneralProductsTotal { get; init; }
    public required decimal HardwareProductsTotal { get; init; }
    public required decimal CashSalesTotal { get; init; }
    public required decimal MoneyTransferTotal { get; init; }
    public required decimal WelfareCardTotal { get; init; }
    public required decimal PayLaterGeneralProductsTotal { get; init; }
    public required decimal PayLaterHardwareProductsTotal { get; init; }

    public required decimal PayoutsTotal { get; init; }
    public required decimal GeneralPayoutsTotal { get; init; }
    public required decimal HardwarePayoutsTotal { get; init; }
    public required decimal CashFloatsTotal { get; init; }
    public required decimal DebtRepaymentsTotal { get; init; }

    public required decimal ExpectedCash { get; init; }

    /// <summary>The latest count of the day, or null if nobody has counted yet.</summary>
    public CashCountDto? LatestCount { get; init; }
    public decimal? CountedCash { get; init; }

    /// <summary>Counted − expected. Positive = over, negative = short, null = not counted yet.</summary>
    public decimal? CashDifference { get; init; }
}
```

`src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary/CashDrawerCalculator.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;

public readonly record struct CashDrawerTotals(
    decimal CashSalesTotal,
    decimal DebtRepaymentsTotal,
    decimal CashFloatsTotal,
    decimal MoneyTransferTotal,
    decimal WelfareCardTotal,
    decimal PayoutsTotal);

/// <summary>The cash formula, unchanged from v3's CashFlowData.CalculateExpectedCash.</summary>
public static class CashDrawerCalculator
{
    public static decimal ExpectedCash(CashDrawerTotals totals) =>
        totals.CashSalesTotal
        + totals.DebtRepaymentsTotal
        + totals.CashFloatsTotal
        - totals.MoneyTransferTotal
        - totals.WelfareCardTotal
        - totals.PayoutsTotal;

    public static decimal? Difference(decimal? countedCash, decimal expectedCash) =>
        countedCash - expectedCash;
}
```

- [ ] **Step 4: Write the Infrastructure handler**

`src/IndyPOS.Infrastructure/QueryHandlers/CashDrawer/GetCashDrawerSummaryQueryHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.Infrastructure.QueryHandlers.CashDrawer;

/// <summary>
/// Gathers the day's sales, payments, hand-typed inputs and latest count, then applies the cash
/// formula. Soft-deleted entries drop out through the global "SoftDelete" filter.
/// </summary>
public class GetCashDrawerSummaryQueryHandler(
    IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> salesSummary,
    IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary> paymentsSummary,
    StoreHubDbContext db,
    ICashCountRepository counts,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashDrawerSummaryQuery, CashDrawerSummaryDto>
{
    public async Task<CashDrawerSummaryDto> HandleAsync(GetCashDrawerSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var today = clock.Now().BusinessDate;
        var day = query.BusinessDate ?? today;
        var storeId = storeIdentity.StoreId;

        var sales = await salesSummary.HandleAsync(new GetLegacySalesSummaryQuery(day, day), cancellationToken);
        var payments = await paymentsSummary.HandleAsync(new GetLegacyPaymentsSummaryQuery(day, day), cancellationToken);
        var payouts = await db.CashPayouts.Where(p => p.StoreId == storeId && p.BusinessDate == day)
                              .Select(p => new { p.Category, p.Amount }).ToListAsync(cancellationToken);
        var floatsTotal = await db.CashFloats.Where(f => f.StoreId == storeId && f.BusinessDate == day)
                                  .SumAsync(f => f.Amount, cancellationToken);
        var repaymentsTotal = await db.DebtRepayments.Where(r => r.StoreId == storeId && r.BusinessDate == day)
                                      .SumAsync(r => r.Amount, cancellationToken);
        var latest = await counts.GetLatestAsync(storeId, day, cancellationToken);

        var payoutsTotal = payouts.Sum(p => p.Amount);
        var expected = CashDrawerCalculator.ExpectedCash(new CashDrawerTotals(
            sales.InvoiceTotalWithoutPayLaterPayments, repaymentsTotal, floatsTotal,
            payments.MoneyTransferTotal, payments.WelfareCardTotal, payoutsTotal));
        var counted = latest?.CountedTotal;

        return new CashDrawerSummaryDto
        {
            BusinessDate = day,
            IsEditable = day == today,
            GeneralProductsTotal = sales.GeneralProductsTotal,
            HardwareProductsTotal = sales.HardwareProductsTotal,
            CashSalesTotal = sales.InvoiceTotalWithoutPayLaterPayments,
            MoneyTransferTotal = payments.MoneyTransferTotal,
            WelfareCardTotal = payments.WelfareCardTotal,
            PayLaterGeneralProductsTotal = sales.PayLaterPaymentsTotalForGeneralProducts,
            PayLaterHardwareProductsTotal = sales.PayLaterPaymentsTotalForHardwareProducts,
            PayoutsTotal = payoutsTotal,
            GeneralPayoutsTotal = payouts.Where(p => p.Category == PayoutCategory.General).Sum(p => p.Amount),
            HardwarePayoutsTotal = payouts.Where(p => p.Category == PayoutCategory.Hardware).Sum(p => p.Amount),
            CashFloatsTotal = floatsTotal,
            DebtRepaymentsTotal = repaymentsTotal,
            ExpectedCash = expected,
            LatestCount = latest?.ToDto(),
            CountedCash = counted,
            CashDifference = CashDrawerCalculator.Difference(counted, expected)
        };
    }
}
```

(This method is longer than 20 lines because it is one flat projection; splitting the DTO build into a private `BuildSummary(...)` with 7+ parameters would read worse. Leave it flat — a reviewer may push back, and that is a fair discussion, not a defect.)

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashDrawer.Summary"`
Expected: PASS (13).

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/CashDrawer/Summary src/IndyPOS.Infrastructure/QueryHandlers/CashDrawer tests/IndyPOS.Application.Tests/UseCases/StoreHub/CashDrawer/Summary
git commit -m "feat(cash): add cash drawer summary query with the cash formula"
```

---

### Task 9: The `/cash` API — capability, endpoints, error mapping, user from token

**Files:**
- Modify: `src/IndyPOS.Application/Common/Authorization/Capability.cs` (add after `ReportsView`)
- Modify: `src/IndyPOS.Application/Common/Authorization/RoleCapabilities.cs` (grant to all three roles)
- Create in `src/IndyPOS.StoreHub/Endpoints/Cash/`: `CashEndpoints.cs`, `CashPayoutEndpoints.cs`, `CashFloatEndpoints.cs`, `DebtRepaymentEndpoints.cs`, `CashCountEndpoints.cs`, `CashExceptionFilter.cs`, `RequireUserIdFilter.cs`, `ClaimsPrincipalExtensions.cs`, `CashDrawerServiceCollectionExtensions.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` — policy (in the `AddAuthorizationBuilder()` chain, line ~159-180), `builder.Services.AddCashDrawer();` (after the PayLater handler registrations, line ~127), `app.MapCashEndpoints();` (before `app.Run();`)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashPayoutEndpointsTests.cs`, `CashCountEndpointsTests.cs`, `CashSummaryEndpointsTests.cs`, `CashAuthorizationTests.cs`; `tests/IndyPOS.Application.Tests/Common/Authorization/CashCapabilityTests.cs`

**Interfaces:**
- Consumes: every command/query/handler from Tasks 4–8.
- Produces: `Capability.CashManage = "cash.manage"`; policy `"CanManageCash"`; `IEndpointRouteBuilder MapCashEndpoints(this IEndpointRouteBuilder app)`; `IServiceCollection AddCashDrawer(this IServiceCollection services)`; `Guid? FindUserId(this ClaimsPrincipal)`, `Guid GetRequiredUserId(this ClaimsPrincipal)`.

Routes:

```
GET    /cash/summary?businessDate=
GET    /cash/payouts?businessDate=        POST /cash/payouts        PUT /cash/payouts/{id}        DELETE /cash/payouts/{id}
GET    /cash/floats?businessDate=         POST /cash/floats         PUT /cash/floats/{id}         DELETE /cash/floats/{id}
GET    /cash/debt-repayments?businessDate= POST /cash/debt-repayments PUT /cash/debt-repayments/{id} DELETE /cash/debt-repayments/{id}
GET    /cash/counts?businessDate=         POST /cash/counts         (no PUT, no DELETE)
```

- [ ] **Step 1: Write the failing capability test**

`tests/IndyPOS.Application.Tests/Common/Authorization/CashCapabilityTests.cs`:

```csharp
using FluentAssertions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Enums;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Authorization;

public class CashCapabilityTests
{
    private const int UnknownRoleId = 99;

    [Fact]
    public void HasCapability_WithUnknownRole_ReturnsFalse()
    {
        var result = RoleCapabilities.HasCapability(UnknownRoleId, Capability.CashManage);

        result.Should()
              .BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.StoreManager)]
    [InlineData(UserRole.SystemAdmin)]
    public void HasCapability_WithStoreRole_GrantsCashManage(UserRole role)
    {
        var result = RoleCapabilities.HasCapability((int)role, Capability.CashManage);

        result.Should()
              .BeTrue();
    }
}
```

- [ ] **Step 2: Write the failing integration tests**

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashAuthorizationTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashAuthorizationTests : IntegrationTestBase
{
    public CashAuthorizationTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// A correctly signed cashier token that carries a role but NO user-id claim — the capability
    /// check passes, so only the user-id filter stands between it and a 500.
    /// </summary>
    private string TokenWithoutUserId()
    {
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("LocalToken");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["SecretKey"]!));
        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: [new Claim("role_id", ((int)UserRole.Cashier).ToString()), new Claim("store_id", "test-store")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetSummary_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/cash/summary");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddPayout_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutUserId());

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_AsCashier_ReturnsOk()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/cash/summary");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }
}
```

If the `LocalToken` section key names differ, read them from `LocalTokenOptions` (`src/IndyPOS.StoreHub` / Infrastructure — the property names are `Issuer`, `Audience`, `SecretKey`, as used in `Program.cs:137-150`) and adjust; if `Issuer`/`Audience` come only from option defaults (not configuration), resolve `IOptions<LocalTokenOptions>` or read the defaults from the `LocalTokenOptions` class instead.

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashPayoutEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashPayoutEndpointsTests : IntegrationTestBase
{
    public CashPayoutEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<CashPayoutDto> AddPayoutAsync(decimal amount = 150m)
    {
        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount, category = "Hardware", description = "ตะปู" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CashPayoutDto>(JsonOptions))!;
    }

    private async Task<Guid> SeedYesterdaysPayoutAsync()
    {
        await using var db = GetDbContext();
        var payout = new CashPayout
        {
            Id = Guid.NewGuid(), StoreId = "test-store", Amount = 80m,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2),
            CreatedUtc = DateTime.UtcNow.AddDays(-2), LastModifiedUtc = DateTime.UtcNow.AddDays(-2),
            CreatedByUserId = Guid.NewGuid()
        };
        db.CashPayouts.Add(payout);
        await db.SaveChangesAsync();
        return payout.Id;
    }

    [Fact]
    public async Task AddPayout_WithZeroAmount_ReturnsBadRequestWithThaiError()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 0m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should()
                                                    .Contain("จำนวนเงิน");
    }

    [Fact]
    public async Task AddPayout_WithUndefinedNumericCategory_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m, category = 7 });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPayout_WithUnknownCategoryName_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m, category = "Food" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditPayout_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PutAsJsonAsync($"/cash/payouts/{Guid.NewGuid()}", new { amount = 10m, category = "General" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditPayout_AfterDelete_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();
        var payout = await AddPayoutAsync();
        await Client.DeleteAsync($"/cash/payouts/{payout.Id}");

        var response = await Client.PutAsJsonAsync($"/cash/payouts/{payout.Id}", new { amount = 10m, category = "General" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditPayout_FromAPastDay_ReturnsConflict()
    {
        await AuthenticateAsCashierAsync();
        var id = await SeedYesterdaysPayoutAsync();

        var response = await Client.PutAsJsonAsync($"/cash/payouts/{id}", new { amount = 10m, category = "General" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePayout_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.DeleteAsync($"/cash/payouts/{Guid.NewGuid()}");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePayout_Twice_ReturnsNoContentBothTimes()
    {
        await AuthenticateAsCashierAsync();
        var payout = await AddPayoutAsync();

        var first = await Client.DeleteAsync($"/cash/payouts/{payout.Id}");
        var second = await Client.DeleteAsync($"/cash/payouts/{payout.Id}");

        first.StatusCode.Should()
                        .Be(HttpStatusCode.NoContent);
        second.StatusCode.Should()
                         .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ListPayouts_WithMalformedDate_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/cash/payouts?businessDate=2026-13-01");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPayout_AsCashier_RecordsTheTokenUserNotABodyField()
    {
        await AuthenticateAsCashierAsync();
        var me = await Client.GetFromJsonAsync<Dictionary<string, string>>("/auth/me", JsonOptions);
        var forged = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m, createdByUserId = forged, userId = forged });
        var payout = await response.Content.ReadFromJsonAsync<CashPayoutDto>(JsonOptions);

        payout!.CreatedByUserId.Should()
                               .Be(Guid.Parse(me!["userId"]));
    }

    [Fact]
    public async Task AddPayout_AsCashier_ReturnsCreatedWithCategoryByName()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 150m, category = "Hardware" });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Created);
        (await response.Content.ReadAsStringAsync()).Should()
                                                    .Contain("\"Hardware\"");
    }

    [Fact]
    public async Task AddPayout_AsCashier_WritesTheOutboxEventWithTheRow()
    {
        await AuthenticateAsCashierAsync();

        var payout = await AddPayoutAsync();

        await using var db = GetDbContext();
        (await db.OutboxEvents.AnyAsync(e => e.Type == "CashPayoutChanged" && e.PayloadJson.Contains(payout.Id.ToString())))
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task DeletePayout_ThenList_ExcludesIt()
    {
        await AuthenticateAsCashierAsync();
        var payout = await AddPayoutAsync();

        await Client.DeleteAsync($"/cash/payouts/{payout.Id}");
        var list = await Client.GetFromJsonAsync<List<CashPayoutDto>>("/cash/payouts", JsonOptions);

        list.Should()
            .NotContain(p => p.Id == payout.Id);
    }
}
```

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashCountEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashCountEndpointsTests : IntegrationTestBase
{
    private static readonly AddCashCountRequest OneThousand = new(1, 0, 0, 0, 0, 0, 0, 0, 0);

    public CashCountEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task AddCount_WithNegativeDenomination_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/counts", OneThousand with { Coin1Count = -1 });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditCount_WithAnyId_IsNotAllowed()
    {
        await AuthenticateAsCashierAsync();
        var created = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand)).Content
                                                                                       .ReadFromJsonAsync<CashCountDto>(JsonOptions);

        var response = await Client.PutAsJsonAsync($"/cash/counts/{created!.Id}", OneThousand);

        ((int)response.StatusCode).Should()
                                  .BeOneOf(404, 405);
    }

    [Fact]
    public async Task DeleteCount_WithAnyId_IsNotAllowed()
    {
        await AuthenticateAsCashierAsync();
        var created = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand)).Content
                                                                                       .ReadFromJsonAsync<CashCountDto>(JsonOptions);

        var response = await Client.DeleteAsync($"/cash/counts/{created!.Id}");

        ((int)response.StatusCode).Should()
                                  .BeOneOf(404, 405);
    }

    [Fact]
    public async Task AddCount_Twice_ListsBothNewestFirst()
    {
        await ResetDatabaseAsync();
        await AuthenticateAsCashierAsync();
        var first = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand)).Content.ReadFromJsonAsync<CashCountDto>(JsonOptions);
        var second = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand with { BankNote1000Count = 2 })).Content
                                                                                                                     .ReadFromJsonAsync<CashCountDto>(JsonOptions);

        var list = await Client.GetFromJsonAsync<List<CashCountDto>>("/cash/counts", JsonOptions);

        list!.Select(c => c.Id).Should()
                               .Equal(second!.Id, first!.Id);
    }
}
```

`tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/CashSummaryEndpointsTests.cs`:

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashSummaryEndpointsTests : IntegrationTestBase
{
    public CashSummaryEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetSummary_WithFloatPayoutRepaymentAndTwoCounts_UsesLatestCountOnly()
    {
        await ResetDatabaseAsync();
        await AuthenticateAsCashierAsync();
        await Client.PostAsJsonAsync("/cash/floats", new { amount = 1000m });
        await Client.PostAsJsonAsync("/cash/payouts", new { amount = 200m });
        await Client.PostAsJsonAsync("/cash/debt-repayments", new { customerName = "ลุงสมชาย", amount = 300m });
        await Client.PostAsJsonAsync("/cash/counts", new AddCashCountRequest(9, 0, 0, 0, 0, 0, 0, 0, 0));
        await Client.PostAsJsonAsync("/cash/counts", new AddCashCountRequest(1, 0, 1, 0, 0, 0, 0, 0, 0));

        var summary = await Client.GetFromJsonAsync<CashDrawerSummaryDto>("/cash/summary", JsonOptions);

        summary!.ExpectedCash.Should()
                             .Be(1000m + 300m - 200m);
        summary.CountedCash.Should()
                           .Be(1100m);
        summary.CashDifference.Should()
                              .Be(1100m - 1100m);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashCapabilityTests"` → build FAILS (`Capability.CashManage` missing).
Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~Endpoints.Cash"` → build FAILS or `404`s (no `/cash` routes).

- [ ] **Step 4: Add the capability**

In `Capability.cs`, after `public const string ReportsView = "reports.view";`:

```csharp

    // Cash drawer (ลิ้นชักเก็บเงิน): add/edit/delete/view payouts, floats, repayments and counts
    public const string CashManage = "cash.manage";
```

In `RoleCapabilities.cs`, add `Capability.CashManage,` as the last item of each of the three role lists (`Cashier`, `StoreManager`, `SystemAdmin`).

- [ ] **Step 5: Add the StoreHub endpoint files**

`src/IndyPOS.StoreHub/Endpoints/Cash/ClaimsPrincipalExtensions.cs`:

```csharp
using System.Security.Claims;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>The login token's user id (NameIdentifier, or the raw "sub"), or null if absent or not a Guid.</summary>
    public static Guid? FindUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>Only call behind <see cref="RequireUserIdFilter"/>, which guarantees the id exists.</summary>
    public static Guid GetRequiredUserId(this ClaimsPrincipal user) =>
        user.FindUserId() ?? throw new InvalidOperationException("RequireUserIdFilter must run before this endpoint.");
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/RequireUserIdFilter.cs`:

```csharp
namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>
/// Every cash write is stamped with the acting user, so a token without a usable user id is
/// rejected up front (401) instead of failing later as a 500.
/// </summary>
internal sealed class RequireUserIdFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.User.FindUserId() is null
            ? ValueTask.FromResult<object?>(Results.Unauthorized())
            : next(context);
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/CashExceptionFilter.cs`:

```csharp
using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>Maps cash-drawer rule failures to HTTP, in the { error } shape every StoreHub route uses.</summary>
internal sealed class CashExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (CashEntryValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (CashEntryNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (CashDayClosedException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/CashEndpoints.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>
/// The ลิ้นชักเก็บเงิน API. Cash routes live here rather than inline in Program.cs; moving the
/// existing routes out is a separate clean-up.
/// </summary>
public static class CashEndpoints
{
    public const string Policy = "CanManageCash";

    public static IEndpointRouteBuilder MapCashEndpoints(this IEndpointRouteBuilder app)
    {
        var cash = app.MapGroup("/cash")
                      .RequireAuthorization(Policy)
                      .AddEndpointFilter<RequireUserIdFilter>()
                      .AddEndpointFilter<CashExceptionFilter>();

        cash.MapGet("/summary", async (
            IQueryHandler<GetCashDrawerSummaryQuery, CashDrawerSummaryDto> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashDrawerSummaryQuery(businessDate), cancellationToken)));

        cash.MapCashPayouts();
        cash.MapCashFloats();
        cash.MapDebtRepayments();
        cash.MapCashCounts();
        return app;
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/CashPayoutEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class CashPayoutEndpoints
{
    public static void MapCashPayouts(this RouteGroupBuilder cash)
    {
        var payouts = cash.MapGroup("/payouts");

        payouts.MapGet("", async (
            IQueryHandler<GetCashPayoutsQuery, IReadOnlyList<CashPayoutDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashPayoutsQuery(businessDate), cancellationToken)));

        payouts.MapPost("", async (
            ICommandHandler<AddCashPayoutCommand, CashPayoutDto> handler,
            ClaimsPrincipal user,
            AddCashPayoutRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new AddCashPayoutCommand(user.GetRequiredUserId(), request.Amount, request.Category, request.Description);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/cash/payouts/{result.Id}", result);
        });

        payouts.MapPut("/{id:guid}", async (
            ICommandHandler<EditCashPayoutCommand, CashPayoutDto> handler,
            ClaimsPrincipal user,
            Guid id,
            EditCashPayoutRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new EditCashPayoutCommand(id, user.GetRequiredUserId(), request.Amount, request.Category, request.Description);
            return Results.Ok(await handler.HandleAsync(command, cancellationToken));
        });

        payouts.MapDelete("/{id:guid}", async (
            ICommandHandler<DeleteCashEntryCommand<CashPayout>> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCashEntryCommand<CashPayout>(id, user.GetRequiredUserId()), cancellationToken);
            return Results.NoContent();
        });
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/CashFloatEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class CashFloatEndpoints
{
    public static void MapCashFloats(this RouteGroupBuilder cash)
    {
        var floats = cash.MapGroup("/floats");

        floats.MapGet("", async (
            IQueryHandler<GetCashFloatsQuery, IReadOnlyList<CashFloatDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashFloatsQuery(businessDate), cancellationToken)));

        floats.MapPost("", async (
            ICommandHandler<AddCashFloatCommand, CashFloatDto> handler,
            ClaimsPrincipal user,
            AddCashFloatRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new AddCashFloatCommand(user.GetRequiredUserId(), request.Amount, request.Description);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/cash/floats/{result.Id}", result);
        });

        floats.MapPut("/{id:guid}", async (
            ICommandHandler<EditCashFloatCommand, CashFloatDto> handler,
            ClaimsPrincipal user,
            Guid id,
            EditCashFloatRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new EditCashFloatCommand(id, user.GetRequiredUserId(), request.Amount, request.Description);
            return Results.Ok(await handler.HandleAsync(command, cancellationToken));
        });

        floats.MapDelete("/{id:guid}", async (
            ICommandHandler<DeleteCashEntryCommand<CashFloat>> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCashEntryCommand<CashFloat>(id, user.GetRequiredUserId()), cancellationToken);
            return Results.NoContent();
        });
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/DebtRepaymentEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

internal static class DebtRepaymentEndpoints
{
    public static void MapDebtRepayments(this RouteGroupBuilder cash)
    {
        var repayments = cash.MapGroup("/debt-repayments");

        repayments.MapGet("", async (
            IQueryHandler<GetDebtRepaymentsQuery, IReadOnlyList<DebtRepaymentDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetDebtRepaymentsQuery(businessDate), cancellationToken)));

        repayments.MapPost("", async (
            ICommandHandler<AddDebtRepaymentCommand, DebtRepaymentDto> handler,
            ClaimsPrincipal user,
            AddDebtRepaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new AddDebtRepaymentCommand(user.GetRequiredUserId(), request.CustomerName, request.Amount);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Created($"/cash/debt-repayments/{result.Id}", result);
        });

        repayments.MapPut("/{id:guid}", async (
            ICommandHandler<EditDebtRepaymentCommand, DebtRepaymentDto> handler,
            ClaimsPrincipal user,
            Guid id,
            EditDebtRepaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new EditDebtRepaymentCommand(id, user.GetRequiredUserId(), request.CustomerName, request.Amount);
            return Results.Ok(await handler.HandleAsync(command, cancellationToken));
        });

        repayments.MapDelete("/{id:guid}", async (
            ICommandHandler<DeleteCashEntryCommand<DebtRepayment>> handler,
            ClaimsPrincipal user,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteCashEntryCommand<DebtRepayment>(id, user.GetRequiredUserId()), cancellationToken);
            return Results.NoContent();
        });
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/CashCountEndpoints.cs`:

```csharp
using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>Append-only by design: POST and GET only. There is deliberately no PUT or DELETE.</summary>
internal static class CashCountEndpoints
{
    public static void MapCashCounts(this RouteGroupBuilder cash)
    {
        var counts = cash.MapGroup("/counts");

        counts.MapGet("", async (
            IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>> handler,
            DateOnly? businessDate,
            CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetCashCountsQuery(businessDate), cancellationToken)));

        counts.MapPost("", async (
            ICommandHandler<AddCashCountCommand, CashCountDto> handler,
            ClaimsPrincipal user,
            AddCashCountRequest request,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new AddCashCountCommand(user.GetRequiredUserId(), request), cancellationToken);
            return Results.Created($"/cash/counts/{result.Id}", result);
        });
    }
}
```

`src/IndyPOS.StoreHub/Endpoints/Cash/CashDrawerServiceCollectionExtensions.cs`:

```csharp
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.QueryHandlers.CashDrawer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>Registers the cash-drawer clock and handlers (repositories come from AddStoreHubServices).</summary>
public static class CashDrawerServiceCollectionExtensions
{
    public static IServiceCollection AddCashDrawer(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICashDrawerClock, CashDrawerClock>();

        services.AddTransient<ICommandHandler<AddCashPayoutCommand, CashPayoutDto>, AddCashPayoutCommandHandler>();
        services.AddTransient<ICommandHandler<EditCashPayoutCommand, CashPayoutDto>, EditCashPayoutCommandHandler>();
        services.AddTransient<IQueryHandler<GetCashPayoutsQuery, IReadOnlyList<CashPayoutDto>>, GetCashPayoutsQueryHandler>();

        services.AddTransient<ICommandHandler<AddCashFloatCommand, CashFloatDto>, AddCashFloatCommandHandler>();
        services.AddTransient<ICommandHandler<EditCashFloatCommand, CashFloatDto>, EditCashFloatCommandHandler>();
        services.AddTransient<IQueryHandler<GetCashFloatsQuery, IReadOnlyList<CashFloatDto>>, GetCashFloatsQueryHandler>();

        services.AddTransient<ICommandHandler<AddDebtRepaymentCommand, DebtRepaymentDto>, AddDebtRepaymentCommandHandler>();
        services.AddTransient<ICommandHandler<EditDebtRepaymentCommand, DebtRepaymentDto>, EditDebtRepaymentCommandHandler>();
        services.AddTransient<IQueryHandler<GetDebtRepaymentsQuery, IReadOnlyList<DebtRepaymentDto>>, GetDebtRepaymentsQueryHandler>();

        services.AddTransient<ICommandHandler<DeleteCashEntryCommand<CashPayout>>, DeleteCashEntryCommandHandler<CashPayout>>();
        services.AddTransient<ICommandHandler<DeleteCashEntryCommand<CashFloat>>, DeleteCashEntryCommandHandler<CashFloat>>();
        services.AddTransient<ICommandHandler<DeleteCashEntryCommand<DebtRepayment>>, DeleteCashEntryCommandHandler<DebtRepayment>>();

        services.AddTransient<ICommandHandler<AddCashCountCommand, CashCountDto>, AddCashCountCommandHandler>();
        services.AddTransient<IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>>, GetCashCountsQueryHandler>();

        services.AddTransient<IQueryHandler<GetCashDrawerSummaryQuery, CashDrawerSummaryDto>, GetCashDrawerSummaryQueryHandler>();
        return services;
    }
}
```

- [ ] **Step 6: Wire `Program.cs`**

Add `using IndyPOS.StoreHub.Endpoints.Cash;` to the usings.

After the PayLater handler registrations (`...RecordPayLaterPaymentCommandHandler>();`):

```csharp

// Cash drawer (ลิ้นชักเก็บเงิน): clock + handlers
builder.Services.AddCashDrawer();
```

Append to the `AddAuthorizationBuilder()` chain, after the `"CanManagePaymentMethods"` policy (move the closing `;`):

```csharp
    .AddPolicy(CashEndpoints.Policy, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.CashManage)));
```

Immediately before `app.Run();`:

```csharp
// Cash drawer routes (/cash/...)
app.MapCashEndpoints();

```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~CashCapabilityTests"` → PASS (4).
Run (Docker running): `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter "FullyQualifiedName~Endpoints.Cash"` → PASS (21).

If `AddPayout_WithUnknownCategoryName_ReturnsBadRequest` returns `500` instead of `400`, the host is surfacing the JSON binding failure as an exception in this environment; confirm the factory's `UseEnvironment("Testing")` is not Development and that minimal APIs return `400` for `BadHttpRequestException` (they do by default outside Development). Do not add a global exception handler for this — it would change every route.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Application/Common/Authorization src/IndyPOS.StoreHub tests/IndyPOS.Application.Tests/Common tests/IndyPOS.StoreHub.IntegrationTests/Endpoints
git commit -m "feat(storehub): add /cash endpoints behind the cash.manage capability"
```

---

### Task 10: Release-gate check and docs

**Files:**
- Modify: `CLAUDE.md` (per-suite test counts line), `ONBOARDING.md` if it lists per-suite counts
- Modify: `docs/operations/upgrade-procedure.md` (append a result line under "Verifying the forward-only gate")

- [ ] **Step 1: Run the forward-only gate recipe**

Follow `docs/operations/upgrade-procedure.md` §"Verifying the forward-only gate before a release" against a throwaway Postgres with this branch's schema applied. For this migration specifically, confirm:
- `information_schema.columns` shows **no** new column on `product`, `invoice`, `invoice_line`, `payment`, `inventory_movement`, `pay_later`, `outbox_event`;
- a complete sale can still be written with the pre-release `INSERT`s (the four new tables are simply unused by old binaries).

- [ ] **Step 2: Record the result**

Append to the gate section of `docs/operations/upgrade-procedure.md` (and a Change Log row):

```markdown
Result for the cash-drawer release (1 migration, `AddCashDrawerTables`): four new tables only
(`cash_payout`, `cash_float`, `debt_repayment`, `cash_count`); no existing table changed; every
pre-release `INSERT` succeeded and a complete sale was written using only pre-release columns.
```

- [ ] **Step 3: Run the whole solution and update the counts**

Run (Docker running, real store DBs present if you have them): `dotnet test`
Expected: all green except known-skipped. Update the per-suite counts in `CLAUDE.md` ("Solution suites total **N** …" and the per-suite list) from the measured output — **measure, do not derive**.

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md ONBOARDING.md docs/operations/upgrade-procedure.md
git commit -m "docs: record cash-drawer forward-only gate result and test counts"
```

---

## Self-Review

**Spec coverage** (spec § → task):
- §3 entities, `PayoutCategory`, `CashCount` append-only, `BusinessDate` server-side, per-store keying → Tasks 1, 2, 3.
- §4 four configurations, repositories, named `"SoftDelete"` filter, one additive migration, forward-only → Tasks 2, 10.
- §5 commands/queries for all four inputs, summary query with the unchanged formula over latest count, edge rules (only-today 409, deleted 404, idempotent delete, last-save-wins, validation) → Tasks 3–8. "Failed save never shows in the list / typed input kept" is a **ViewModel** rule → **plan 3**.
- §6 `/cash` group, `cash.manage` for Cashier/Manager/Admin, policy once on the group, user from token, no date/user in bodies, `GET` defaults to today → Task 9.
- §7 StoreHub side: full-row outbox event in the same save, four event types → Tasks 3–7. Cloud side (mirror tables, `HasComment`, upsert with stale skip, user-id join check) → **plan 2**. `EventProcessor` unknown-type drop + suspected transaction bug → **separate changes**, as the spec says.
- §8 UI / §9 rename → **plan 3**.
- §10 tests: naming, negative-first, per-layer, count rules, tie-break, forward-only → every task + Task 10.
- §12 timing → not code; honoured by the plan split (this plan can land any time before Phase B).

**Deviations from the spec, called out:** `{ error }` body instead of ProblemDetails; explicit rules instead of FluentValidation validators; cash routes split across a `Endpoints/Cash/` folder instead of one `CashEndpoints.cs` file (one file per resource keeps each under ~60 lines); the summary handler lives in Infrastructure beside the other report handlers; one generic `ICashEntryRepository<TEntry>` instead of spec §4's three separate `ICashPayoutRepository` / `ICashFloatRepository` / `IDebtRepaymentRepository` interfaces, because the three entry types share every operation (add, find, list, soft-delete) and the generic form is what makes `DeleteCashEntryCommand<TEntry>` possible as a single handler instead of three near-identical ones. Each is repo convention winning over a spec assumption the code proved wrong.

**Placeholder scan:** none left. Task 7's expected count total was re-checked by hand: 1×1000 + 5×100 + 10×10 + 3×1 = 1603.

**Type consistency:** `CashDrawerInstant(Utc, BusinessDate)` used everywhere via `clock.Now()`; repository names `FindAsync / FindIncludingDeletedAsync / ListAsync / AddAsync / SaveChangesAsync` and `ListNewestFirstAsync / GetLatestAsync` match across Tasks 2–9; DTO `ToDto()` extensions defined in each DTO file; event constants on `CashDrawerOutbox`.

**Found while planning, belongs to plan 2:** `HttpCloudSyncClient.ParseStoreId` converts the string `StoreId` to `int` and falls back to `0` (`src/IndyPOS.Infrastructure/.../HttpCloudSyncClient.cs:131-136`). With non-numeric store ids every synced event would claim store `0`. Verify with a RED test before plan 2 relies on the envelope.
