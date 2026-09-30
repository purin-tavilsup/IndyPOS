# Invoice History — Cloud Mirror Implementation Plan (plan 2 of 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cloud stores each bill's number and every reprint, so remote dashboards and AI agents see the same bill numbers and reprint trail as the till.

**Architecture:** Two additive CloudApi migrations: a nullable `InvoiceNumber` on the cloud `Invoices` table, and a new insert-only `InvoiceReprints` table. `EventProcessor` copies the number from `InvoiceCompleted`, and gets a new `InvoiceReprinted` handler with an ordering guard. That guard throws, so the event stays unprocessed and retries, if the reprint's invoice has not reached the cloud yet. `BulkMigrationCommandHandler` stores the number for migrated v3 history. The inbox gains a deterministic tiebreak, so events that arrived in one ingest batch are processed in the order the store sent them.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql, xUnit, FluentAssertions, Testcontainers (Postgres 16), System.Text.Json (default options on both ends).

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` (§9 Cloud, §10 Cloud tests). Plan 1, `docs/superpowers/plans/2026-09-27-invoice-history-storehub-backend.md`, defines the event and command contracts this plan consumes.

## Prerequisites

- **Plan 1 is merged.** This plan reads three things plan 1 adds to `IndyPOS.Application`, and does not create them itself:
  - `InvoiceCompletedEvent.InvoiceNumber` (`long?`, plan 1 Task 2);
  - `MigratedInvoice.InvoiceNumber` (`long?`, the last positional parameter, default `null`; plan 1 Task 3);
  - `InvoiceReprintedEvent` and the type name `"InvoiceReprinted"` (plan 1 Task 8).

  Check before starting: `grep -n "InvoiceNumber" src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceCompletedEvent.cs src/IndyPOS.Application/UseCases/Cloud/Sync/BulkMigration/BulkMigrationCommand.cs` prints two lines, and `src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceReprintedEvent.cs` exists.
- **PR #101 is merged** (the event-pipeline repair, 2026-09-30). Before it, no event was ever processed, so none of this plan could have worked. It also gives this plan `CloudPostgresFixture`, the `EventProcessorTests.Pipeline` harness and `HandledEventTypes`.
- **Docker is running.** Every test in this plan runs on a real Postgres container, with the Npgsql retry strategy on, as Aspire configures it.

## Global Constraints

- **Forward-only migrations (release gate, CLAUDE.md):** additive only; new columns nullable or with a default; no renames, drops or type narrowing. Both migrations here are additive: a nullable column and a new table.
- **Nullable across versions (spec §9):** "It is nullable so either side can be older: an older cloud ignores the field, and an older migrator sends `null`." The same holds for `InvoiceCompleted` events queued before plan 1: they carry no number, and it is stored as `NULL`, never as `0`.
- **Insert-only reprint mirror (spec §9):** "`InvoiceReprinted` populates a new **insert-only** … mirror. Idempotency comes from `ProcessedEvents`. `HasComment` on the table and columns keeps it readable for dashboards and AI."
- **Ordering guard (spec §9):** "a reprint event that arrives before its `InvoiceCompleted` **fails so it retries**, and is never marked processed."
- **Deploy order (spec §9):** "Deploy the cloud handler before any store runs this version." Since #101, the processor only fetches the types in `HandledEventTypes`, so an early `InvoiceReprinted` would wait rather than be dropped. But nothing reaches the mirror until this plan ships.
- **Test style:** `Subject_WhenScenario_DirectVerbOutcome`, one behaviour per test, negative cases first, and Arrange/Act/Assert separated by blank lines. FluentAssertions chains go on separate lines with the dots aligned.
- **Every test must be able to fail:** run each RED step and read the failure message before writing the fix.

## Review Focus

The five inputs the spec implies but its test list does not exercise, most likely first. Each has a test in the task that owns the code:

1. **A reprint and its invoice arriving in the same ingest batch.** `IngestEventsCommandHandler` stamps one `ReceivedAtUtc` on the whole batch, and the inbox orders only by that, so the reprint can be processed first. Expected: both are stored in one poll, invoice first. → Task 4 (`GetUnprocessedAsync_WithEventsReceivedAtTheSameInstant_ReturnsThemInArrivalOrder`).
2. **An `InvoiceCompleted` queued on a till before the upgrade.** Its payload has no `InvoiceNumber`. Expected: the invoice is stored, and its number is `NULL`, not `0`. → Task 1 (`…WithAnEventFromBeforeBillNumbers_StoresANullNumber`).
3. **An older MigrationTool pushing history.** It sends no `InvoiceNumber`. Expected: the import succeeds, and the number is `NULL`. → Task 2 (`HandleAsync_WithAnInvoiceFromAnOlderMigrator_StoresANullNumber`).
4. **The same reprint delivered twice.** A crash between the handler's save and marking the event leaves it unprocessed. Expected: one mirror row. → Task 3 (`…WithAReprintAlreadyInProcessedEvents_StoresOnlyTheFreshOne`).
5. **One bill reprinted twice.** Spec §6: "pressing reprint again writes a second one. The owner sees both attempts." Expected: two mirror rows. → Task 3 (`…WithTwoReprintsOfOneInvoice_StoresBoth`).

---

## File Structure

```
src/IndyPOS.CloudApi/
  Domain/CloudInvoice.cs                        MOD  + long? InvoiceNumber
  Domain/CloudInvoiceReprint.cs                 NEW  insert-only reprint mirror
  Infrastructure/CloudDbContext.cs              MOD  InvoiceNumber config + InvoiceReprints table
  Infrastructure/EventProcessor.cs              MOD  copy number; InvoiceReprinted handler + guard
  Infrastructure/BulkMigrationCommandHandler.cs MOD  copy number
  Infrastructure/DbSyncedEventRepository.cs     MOD  ThenBy(Id) tiebreak
  Infrastructure/Migrations/<ts>_AddInvoiceNumberToInvoices.cs   NEW (dotnet ef)
  Infrastructure/Migrations/<ts>_AddInvoiceReprints.cs           NEW (dotnet ef)

tests/IndyPOS.CloudApi.IntegrationTests/
  EventProcessorTests.cs                        MOD  number + reprint tests; Pipeline helpers
  BulkMigrationCommandHandlerTests.cs           MOD  number tests
  DbSyncedEventRepositoryTests.cs               MOD  ordering tiebreak test

.planning/indypos-overhaul/epic-3-store-rollout-plan.md   MOD  deploy the cloud first (Task 3)
CLAUDE.md, ONBOARDING.md                                    MOD  suite counts (Task 4)
```

`<ts>` is the timestamp `dotnet ef` generates.

---

### Task 1: Cloud invoices keep their bill number

**Files:**
- Modify: `src/IndyPOS.CloudApi/Domain/CloudInvoice.cs:7-19`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` (the `CloudInvoice` block)
- Modify: `src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs` (`ProcessInvoiceCompletedAsync`)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddInvoiceNumberToInvoices.cs` (generated)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs`

**Interfaces:**
- Consumes: `InvoiceCompletedEvent.InvoiceNumber : long?` (plan 1 Task 2).
- Produces: `CloudInvoice.InvoiceNumber : long?`; the `Pipeline.AddInvoiceCompletedAsync(Guid? invoiceId = null, long? invoiceNumber = null)` test helper, used again by Task 3.

- [ ] **Step 1: Let the test harness send a bill number**

In `EventProcessorTests.cs`, change the `Pipeline.AddInvoiceCompletedAsync` signature and initializer so that tests can set the number:

```csharp
        public async Task<InvoiceCompletedEvent> AddInvoiceCompletedAsync(Guid? invoiceId = null, long? invoiceNumber = null)
        {
            var evt = new InvoiceCompletedEvent
            {
                EventId = Guid.NewGuid(),
                InvoiceId = invoiceId ?? Guid.NewGuid(),
                InvoiceNumber = invoiceNumber,
```

Leave the rest of the initializer (`StoreId` through `Payments`) as it is. Every existing call passes at most `invoiceId`, so all of them still compile.

- [ ] **Step 2: Write the failing tests**

Add this constant under `EventTypeWithoutAHandler`:

```csharp
    // A v4 number continuing a v3 store's history (spec §4).
    private const long KnownInvoiceNumber = 7008;
```

Add these tests after `ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_MarksTheEventProcessed`:

```csharp
    // Spec §9: a till queues InvoiceCompleted before it upgrades, so its payload has no number.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventFromBeforeBillNumbers_StoresANullNumber()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync(invoiceNumber: null);

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        invoice.Should()
               .NotBeNull(pipeline.LoggedErrors);
        invoice!.InvoiceNumber.Should()
                              .BeNull();
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithABillNumber_StoresIt()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync(invoiceNumber: KnownInvoiceNumber);

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        invoice.Should()
               .NotBeNull(pipeline.LoggedErrors);
        invoice!.InvoiceNumber.Should()
                              .Be(KnownInvoiceNumber);
    }
```

Both tests assert that the invoice exists before they look at its number. That is on purpose. Without it, a pipeline that stored nothing would pass the null test. And a `?.InvoiceNumber.Should()` chain skips the assertion entirely when the invoice is null, so it could never fail.

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~BillNumber"`
Expected: build FAILS, because `CloudInvoice` does not contain a definition for `InvoiceNumber`. That is the RED for both tests.

- [ ] **Step 4: Add the property**

In `CloudInvoice.cs`, after `public DateTime SyncedAtUtc { get; set; }`:

```csharp

    /// <summary>
    /// The bill number printed on the receipt, unique within a store. Migrated v3 history keeps
    /// its v3 number. Null for a sale synced before bill numbers existed.
    /// </summary>
    public long? InvoiceNumber { get; set; }
```

- [ ] **Step 5: Configure the column**

In `CloudDbContext.cs`, inside `modelBuilder.Entity<CloudInvoice>(entity => { … })`, after `entity.Property(e => e.TotalAmount).HasPrecision(18, 2);`:

```csharp

            // Not unique, on purpose. A store reset to Fresh and migrated again (rollout Rule 1)
            // re-imports the same numbers under new invoice ids. A unique index would turn that into
            // an event that fails forever and holds an inbox batch slot. Duplicates stay queryable.
            entity.Property(e => e.InvoiceNumber)
                  .HasComment("Bill number printed on the receipt, per store; v3 history keeps its v3 number. NULL for a sale synced before bill numbers existed.");
            entity.HasIndex(e => new { e.StoreId, e.InvoiceNumber });
```

- [ ] **Step 6: Copy the number in the handler**

In `EventProcessor.ProcessInvoiceCompletedAsync`, in the `new CloudInvoice { … }` initializer, after `CreatedAtUtc = eventData.CreatedAtUtc,`:

```csharp
            InvoiceNumber = eventData.InvoiceNumber,
```

- [ ] **Step 7: Generate the migration**

Run: `dotnet ef migrations add AddInvoiceNumberToInvoices --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations`

Expected: two new files, plus a snapshot change. The tool is 9.0.8 against runtime 10, and warns about that; the warning is known and harmless. Open the generated `Up` and check that it does exactly two things:
- an `AddColumn<long>` named `InvoiceNumber` on `Invoices`, with `nullable: true` and the comment;
- a `CreateIndex` named `IX_Invoices_StoreId_InvoiceNumber`, with no `unique: true`.

Anything else in `Up` means the model drifted: stop and investigate before continuing.

- [ ] **Step 8: Check the model and the snapshot agree**

Run: `dotnet ef migrations has-pending-model-changes --project src/IndyPOS.CloudApi`
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 9: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`
Expected: PASS, 22 tests (the 20 from #101 plus 2 new). The fixture runs `MigrateAsync`, so the new migration is exercised by every test.

- [ ] **Step 10: Commit**

```bash
git add src/IndyPOS.CloudApi/Domain/CloudInvoice.cs src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs src/IndyPOS.CloudApi/Infrastructure/Migrations tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs
git commit -m "feat(cloud): keep each synced invoice's bill number"
```

---

### Task 2: Bulk-migrated history keeps its v3 number

**Files:**
- Modify: `src/IndyPOS.CloudApi/Infrastructure/BulkMigrationCommandHandler.cs` (the `new CloudInvoice` in the invoice loop)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/BulkMigrationCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `MigratedInvoice.InvoiceNumber : long?` (plan 1 Task 3); `CloudInvoice.InvoiceNumber` (Task 1).
- Produces: nothing new.

- [ ] **Step 1: Write the failing tests**

Add this constant under `StoreId`:

```csharp
    // A v3 invoice id, which the migrator sends as the bill number (spec §4).
    private const long LegacyInvoiceNumber = 6999;
```

Add these tests after `HandleAsync_UnderTheRetryStrategy_StoresTheInvoiceWithItsLineAndPayment`:

```csharp
    // Spec §9: "an older migrator sends null".
    [Fact]
    public async Task HandleAsync_WithAnInvoiceFromAnOlderMigrator_StoresANullNumber()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var invoice = NewInvoice() with { InvoiceNumber = null };
        await using (var db = CloudPostgresFixture.CreateContext(connectionString))
            await NewHandler(db).HandleAsync(CommandWith(invoice));

        await using var check = CloudPostgresFixture.CreateContext(connectionString);
        var stored = await check.Invoices.SingleOrDefaultAsync(i => i.Id == invoice.Id);

        stored.Should()
              .NotBeNull();
        stored!.InvoiceNumber.Should()
                             .BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithAMigratedInvoice_StoresItsV3Number()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        var invoice = NewInvoice() with { InvoiceNumber = LegacyInvoiceNumber };
        await using (var db = CloudPostgresFixture.CreateContext(connectionString))
            await NewHandler(db).HandleAsync(CommandWith(invoice));

        await using var check = CloudPostgresFixture.CreateContext(connectionString);

        (await check.Invoices.SingleAsync(i => i.Id == invoice.Id)).InvoiceNumber.Should()
                                                                   .Be(LegacyInvoiceNumber);
    }
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~BulkMigrationCommandHandlerTests"`
Expected: `HandleAsync_WithAMigratedInvoice_StoresItsV3Number` FAILS with "Expected … to be 6999, but found <null>". `…FromAnOlderMigrator_StoresANullNumber` PASSES already. That is correct: today the handler drops every number, so a null is what it stores. It pins the older-migrator case for after the fix.

- [ ] **Step 3: Copy the number**

In `BulkMigrationCommandHandler.ImportAsync`, in the `new CloudInvoice { … }` initializer, after `CreatedAtUtc = invoice.CreatedAtUtc,`:

```csharp
                InvoiceNumber = invoice.InvoiceNumber,
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~BulkMigrationCommandHandlerTests"`
Expected: PASS, 7 tests (5 from #101 plus 2 new).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.CloudApi/Infrastructure/BulkMigrationCommandHandler.cs tests/IndyPOS.CloudApi.IntegrationTests/BulkMigrationCommandHandlerTests.cs
git commit -m "feat(cloud): keep the v3 bill number on bulk-migrated invoices"
```

---

### Task 3: Mirror every reprint, and only after its invoice

**Files:**
- Create: `src/IndyPOS.CloudApi/Domain/CloudInvoiceReprint.cs`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` (a `DbSet` and a new entity block)
- Modify: `src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs` (`HandledEventTypes`, the switch, a new handler)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddInvoiceReprints.cs` (generated)
- Modify: `.planning/indypos-overhaul/epic-3-store-rollout-plan.md` (deploy order)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs`

**Interfaces:**
- Consumes: `InvoiceReprintedEvent { int SchemaVersion; Guid EventId; Guid ReprintId; Guid InvoiceId; string StoreId; DateTime CreatedUtc; Guid CreatedByUserId }` (plan 1 Task 8); the `Pipeline` helpers `AddInvoiceCompletedAsync`, `AddProcessedEventAsync`, `CountUnprocessedAsync`, `WithDbAsync`, `LoggedErrors` (from #101 and Task 1).
- Produces: `CloudInvoiceReprint`; `CloudDbContext.InvoiceReprints`; the event type `"InvoiceReprinted"` in `EventProcessor.HandledEventTypes`.

- [ ] **Step 1: Add the reprint helpers to the harness**

In `EventProcessorTests.Pipeline`, after `AddInvoiceCompletedAsync`:

```csharp
        public async Task<InvoiceReprintedEvent> AddInvoiceReprintedAsync(Guid invoiceId)
        {
            var evt = new InvoiceReprintedEvent
            {
                EventId = Guid.NewGuid(),
                ReprintId = Guid.NewGuid(),
                InvoiceId = invoiceId,
                StoreId = "1",
                CreatedUtc = DateTime.UtcNow,
                CreatedByUserId = Guid.NewGuid()
            };

            await AddEventAsync("InvoiceReprinted", evt.EventId, JsonSerializer.Serialize(evt));

            return evt;
        }

        public Task<List<CloudInvoiceReprint>> ReprintsOfAsync(Guid invoiceId) =>
            WithDbAsync(db => db.InvoiceReprints
                                .AsNoTracking()
                                .Where(r => r.InvoiceId == invoiceId)
                                .ToListAsync());
```

- [ ] **Step 2: Write the failing tests**

Add these after the Task 1 tests. Negative cases first:

```csharp
    // Spec §9: a reprint that arrives before its InvoiceCompleted fails so it retries.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintBeforeItsInvoice_LeavesItUnprocessed()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        await pipeline.AddInvoiceReprintedAsync(Guid.NewGuid());

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintBeforeItsInvoice_StoresNoReprint()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var invoiceId = Guid.NewGuid();
        await pipeline.AddInvoiceReprintedAsync(invoiceId);

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(invoiceId)).Should()
                                                   .BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintWhoseInvoiceArrivesLater_StoresItOnTheNextPoll()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var invoiceId = Guid.NewGuid();
        await pipeline.AddInvoiceReprintedAsync(invoiceId);
        await pipeline.PollAsync();
        await pipeline.AddInvoiceCompletedAsync(invoiceId);

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(invoiceId)).Should()
                                                   .ContainSingle(pipeline.LoggedErrors);
    }

    // A fresh reprint beside it, so a handler that stores nothing at all cannot pass.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintAlreadyInProcessedEvents_StoresOnlyTheFreshOne()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var sale = await pipeline.AddInvoiceCompletedAsync();
        var alreadyProcessed = await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);
        await pipeline.AddProcessedEventAsync(alreadyProcessed.EventId);
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(sale.InvoiceId)).Should()
                                                        .ContainSingle(pipeline.LoggedErrors);
    }

    // Spec §6: pressing reprint again writes a second record, and the owner sees both.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithTwoReprintsOfOneInvoice_StoresBoth()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var sale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(sale.InvoiceId)).Should()
                                                        .HaveCount(2, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprint_StoresWhoReprintedItAndWhen()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var sale = await pipeline.AddInvoiceCompletedAsync();
        var reprint = await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);

        await pipeline.PollAsync();

        var stored = (await pipeline.ReprintsOfAsync(sale.InvoiceId)).SingleOrDefault();
        (stored?.Id, stored?.CreatedByUserId, stored?.CreatedAtUtc).Should()
                                                                   .Be((reprint.ReprintId, reprint.CreatedByUserId, reprint.CreatedUtc), pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintOfAStoredInvoice_MarksTheEventProcessed()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var sale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }
```

`…StoresWhoReprintedItAndWhen` compares `CreatedUtc` exactly. That works because Postgres `timestamp with time zone` keeps microseconds, and `DateTime.UtcNow` has 100-ns ticks. If it fails **only** on the time, truncate the event's `CreatedUtc` to microseconds in the helper (`new DateTime(t.Ticks - t.Ticks % 10, DateTimeKind.Utc)`). Do not loosen the assertion.

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~Reprint"`
Expected: build FAILS, because `CloudInvoiceReprint` and `CloudDbContext.InvoiceReprints` do not exist.

- [ ] **Step 4: Add the entity**

`src/IndyPOS.CloudApi/Domain/CloudInvoiceReprint.cs`:

```csharp
namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// One reprint of a bill, mirrored from the store's invoice_reprint row by InvoiceReprinted.
/// Insert-only: a reprint is an audit fact, so nothing updates or deletes it. It means a reprint
/// was requested; a printer failure afterwards does not remove it.
/// </summary>
public class CloudInvoiceReprint
{
    /// <summary>The store's reprint id, so a row can be traced back to the till.</summary>
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
```

- [ ] **Step 5: Map the table**

In `CloudDbContext.cs`, after `public DbSet<CloudInventoryMovement> InventoryMovements => Set<CloudInventoryMovement>();`:

```csharp
    public DbSet<CloudInvoiceReprint> InvoiceReprints => Set<CloudInvoiceReprint>();
```

and after the `CloudInventoryMovement` entity block:

```csharp

        // CloudInvoiceReprint - insert-only audit mirror, read by dashboards and AI agents
        modelBuilder.Entity<CloudInvoiceReprint>(entity =>
        {
            entity.ToTable(t => t.HasComment("Every bill reprint a store requested (insert-only audit). One row per press of reprint; a failed print still counts."));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => new { e.StoreId, e.CreatedAtUtc });
            entity.Property(e => e.Id).HasComment("The store's reprint id (invoice_reprint.id on the till).");
            entity.Property(e => e.InvoiceId).HasComment("The reprinted bill; Invoices.Id.");
            entity.Property(e => e.StoreId).HasMaxLength(50).HasComment("The store that reprinted the bill.");
            entity.Property(e => e.CreatedByUserId).HasComment("The user who pressed reprint.");
            entity.Property(e => e.CreatedAtUtc).HasComment("When reprint was pressed, UTC.");
            entity.Property(e => e.SyncedAtUtc).HasComment("When this row reached the cloud, UTC.");

            // A backstop to the handler's ordering guard: no reprint row without its bill.
            entity.HasOne<CloudInvoice>()
                  .WithMany()
                  .HasForeignKey(e => e.InvoiceId);
        });
```

- [ ] **Step 6: Handle the event**

In `EventProcessor.cs`:

1. Replace the `HandledEventTypes` line with:

```csharp
    internal static readonly IReadOnlyCollection<string> HandledEventTypes = ["InvoiceCompleted", "InvoiceReprinted"];
```

2. In the `switch (syncedEvent.EventType)`, before the `default` comment, add:

```csharp
            case "InvoiceReprinted":
                await ProcessInvoiceReprintedAsync(syncedEvent, dbContext, cancellationToken);
                break;
```

3. After `ProcessInvoiceCompletedAsync`, add:

```csharp
    /// <remarks>
    /// Ordering guard (spec §9): a reprint can reach the inbox before its invoice. Throwing leaves
    /// the event unprocessed, so the next poll tries it again once InvoiceCompleted has landed;
    /// marking it would lose the reprint. The ProcessedEvent row is keyed by the inbox's EventId,
    /// the id the idempotency check reads, and is saved with the reprint in one SaveChangesAsync.
    /// </remarks>
    private static async Task ProcessInvoiceReprintedAsync(
        SyncedEventEntity syncedEvent,
        CloudDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var eventData = JsonSerializer.Deserialize<InvoiceReprintedEvent>(syncedEvent.Payload)
            ?? throw new InvalidOperationException($"InvoiceReprinted {syncedEvent.EventId} has an empty payload.");

        if (!await dbContext.Invoices.AnyAsync(i => i.Id == eventData.InvoiceId, cancellationToken))
            throw new InvalidOperationException(
                $"Reprint {eventData.ReprintId} arrived before invoice {eventData.InvoiceId}; it is retried on the next poll.");

        var now = DateTime.UtcNow;

        dbContext.InvoiceReprints.Add(new CloudInvoiceReprint
        {
            Id = eventData.ReprintId,
            InvoiceId = eventData.InvoiceId,
            StoreId = eventData.StoreId,
            CreatedByUserId = eventData.CreatedByUserId,
            CreatedAtUtc = eventData.CreatedUtc,
            SyncedAtUtc = now
        });

        dbContext.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = syncedEvent.EventId,
            EventType = "InvoiceReprinted",
            StoreId = eventData.StoreId,
            ProcessedAtUtc = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
```

The `?? throw` differs on purpose from `InvoiceCompleted`'s log-and-mark on a null payload. Since #101, the rule is that a handled event is never dropped silently. (The `InvoiceCompleted` case is a flagged follow-up, not part of this plan.)

- [ ] **Step 7: Generate the migration**

Run: `dotnet ef migrations add AddInvoiceReprints --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations`

Expected: `Up` creates only the `InvoiceReprints` table, with its comment, the column comments, both indexes, and `FK_InvoiceReprints_Invoices_InvoiceId`. Nothing else.

- [ ] **Step 8: Check the model and the snapshot agree**

Run: `dotnet ef migrations has-pending-model-changes --project src/IndyPOS.CloudApi`
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 9: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`
Expected: PASS, 31 tests (22 + 2 from Task 2 + 7 here).

- [ ] **Step 10: Write the deploy order into the rollout plan**

In `.planning/indypos-overhaul/epic-3-store-rollout-plan.md`, directly after the **Rule 1b** paragraph, add:

```markdown
**Rule 1c — the cloud upgrades before the stores.** Deploy the CloudApi that holds invoice-history
plan 2 (bill numbers + the `InvoiceReprints` mirror) **before** any StoreHub running invoice-history
plan 1. A store's `InvoiceReprinted` events are not lost if it upgrades first: the processor only
fetches the types it can handle, so they wait in the inbox. But they reach no dashboard until the
cloud has the handler.
```

- [ ] **Step 11: Commit**

```bash
git add src/IndyPOS.CloudApi/Domain/CloudInvoiceReprint.cs src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs src/IndyPOS.CloudApi/Infrastructure/Migrations tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs .planning/indypos-overhaul/epic-3-store-rollout-plan.md
git commit -m "feat(cloud): mirror bill reprints, only after their invoice"
```

---

### Task 4: Process one ingest batch in the order the store sent it

**Files:**
- Modify: `src/IndyPOS.CloudApi/Infrastructure/DbSyncedEventRepository.cs` (`GetUnprocessedAsync`)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/DbSyncedEventRepositoryTests.cs`
- Modify: `CLAUDE.md`, `ONBOARDING.md` (suite counts)

**Interfaces:**
- Consumes: `ISyncedEventRepository.GetUnprocessedAsync(IReadOnlyCollection<string> eventTypes, int limit, CancellationToken)` (#101).
- Produces: nothing new. The order becomes `ReceivedAtUtc`, then `Id`.

**Why:** `IngestEventsCommandHandler` stamps one `receivedAt` on every event in a request, and saves them one by one in request order. So within a batch, `ReceivedAtUtc` ties, and only the identity `Id` records the order the store sent them in. With the tie unbroken, Postgres may return a reprint before its own invoice. The guard then fails it, and the reprint lands 5 s later on the next poll. That is harmless, but it's noise in the error log on every reprint synced together with its sale.

- [ ] **Step 1: Write the failing test**

Add to `DbSyncedEventRepositoryTests.cs`:

```csharp
    // Ingest stamps one ReceivedAtUtc on a whole batch; only the identity Id keeps the store's order.
    [Fact]
    public async Task GetUnprocessedAsync_WithEventsReceivedAtTheSameInstant_ReturnsThemInArrivalOrder()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var receivedAtUtc = DateTime.UtcNow;
        var sentSecond = await AddEventAsync(db, id: 2, receivedAtUtc);
        var sentFirst = await AddEventAsync(db, id: 1, receivedAtUtc);

        var batch = await new DbSyncedEventRepository(db).GetUnprocessedAsync(["InvoiceCompleted"]);

        batch.Select(e => e.EventId).Should()
                                    .Equal(sentFirst.EventId, sentSecond.EventId);
    }
```

Add this overload next to the existing `AddEventAsync(CloudDbContext db)`. It writes the higher `Id` first, so the table's physical order is the opposite of the arrival order:

```csharp
    private static async Task<SyncedEventEntity> AddEventAsync(CloudDbContext db, long id, DateTime receivedAtUtc)
    {
        var entity = new SyncedEventEntity
        {
            Id = id,
            EventId = Guid.NewGuid(),
            StoreId = 1,
            EventType = "InvoiceCompleted",
            Payload = "{}",
            CreatedAtUtc = receivedAtUtc,
            ReceivedAtUtc = receivedAtUtc
        };
        db.SyncedEvents.Add(entity);
        await db.SaveChangesAsync();

        return entity;
    }
```

Explicit `Id` values are accepted because Npgsql maps a `long` key to `GENERATED BY DEFAULT AS IDENTITY`.

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests --filter "FullyQualifiedName~ReturnsThemInArrivalOrder"`
Expected: FAIL with "Expected … to be equal to {<sentFirst>, <sentSecond>}, but … differs at index 0". Without a tiebreak, Postgres returns the rows in physical order. That order is not guaranteed, which is exactly the bug. If this passes before the fix, **stop**: the RED has not been shown, so re-check that the rows were inserted with `Id = 2` first.

- [ ] **Step 3: Add the tiebreak**

In `DbSyncedEventRepository.GetUnprocessedAsync`, replace `.OrderBy(e => e.ReceivedAtUtc)` with:

```csharp
            .OrderBy(e => e.ReceivedAtUtc)
            .ThenBy(e => e.Id)
```

Then add this line to the `GetUnprocessedAsync` doc comment in `ISyncedEventRepository`:

```csharp
    /// Ties on ReceivedAtUtc (one ingest batch) are broken by the inbox Id, which is the order the
    /// store sent them in, so an invoice is handled before a reprint of it that came in the same batch.
```

- [ ] **Step 4: Run the whole suite and measure**

Run: `dotnet test --logger trx --results-directory <dir>` (Docker running, with the real store databases present).
Expected: every suite green. `CloudApi.IntegrationTests` has **32**, and the solution has **884 total, 883 pass, 1 skipped**: 871 from #101, plus 13 from this plan. If the numbers differ, use the **measured** ones in the next step, and say in the commit which figure moved.

- [ ] **Step 5: Update the documented counts**

In `CLAUDE.md` and `ONBOARDING.md`, update the counts #101 set to the numbers measured in Step 4:
- `CloudApi.IntegrationTests`: 20 → 32;
- the solution total: 871 → 884, and pass 870 → 883;
- without the store databases: 851 → 864 (derived as total − 20);
- failures with Docker stopped: 239 → 251 (148 + 71 + 32, derived).

Search each file for the old numbers (`grep -n "871\|870\|851\|239\| 20 " CLAUDE.md ONBOARDING.md`), and leave no old figure behind.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.CloudApi/Infrastructure/DbSyncedEventRepository.cs src/IndyPOS.Application/Abstractions/Cloud/Repositories/ISyncedEventRepository.cs tests/IndyPOS.CloudApi.IntegrationTests/DbSyncedEventRepositoryTests.cs CLAUDE.md ONBOARDING.md
git commit -m "fix(cloud): process one ingest batch in the order the store sent it"
```

---

## Self-Review

**Spec coverage** (spec § → task):
- §9 "`InvoiceCompleted` carries `InvoiceNumber`" + "`cloud_invoice` gains a nullable `invoice_number`" → Task 1. The StoreHub side of the event is plan 1 Task 2.
- §9 "Bulk migration carries the number too … the handler stores it" → Task 2. The migrator side is plan 1 Task 3.
- §9 "`InvoiceReprinted` populates a new insert-only … mirror", "Idempotency comes from `ProcessedEvents`", "`HasComment` on the table and columns" → Task 3.
- §9 "Ordering guard … fails so it retries, and is never marked processed" → Task 3 (guard + 3 tests). Task 4 removes the needless retry within one batch.
- §9 "Deploy the cloud handler before any store runs this version" → Task 3 Step 10 (Rule 1c). #101 already made an early event wait rather than be dropped.
- §10 Cloud: "`InvoiceReprinted` before `InvoiceCompleted` stays pending, and succeeds after" → Task 3 (`…BeforeItsInvoice_LeavesItUnprocessed`, `…ArrivesLater_StoresItOnTheNextPoll`). "A duplicate event is processed once" → Task 3 (`…AlreadyInProcessedEvents_StoresOnlyTheFreshOne`). "A bulk-migrated invoice lands in `cloud_invoice` with its v3 `invoice_number`" → Task 2.

**Deviations from the spec, called out:**
- **P1: table and column names follow the cloud's existing convention.** The spec says `cloud_invoice`, `invoice_number` and `cloud_invoice_reprint`. The real tables are PascalCase names taken from the `DbSet`s (`Invoices`, `ProcessedEvents`; `InitialCloudSchema`), so this plan uses `Invoices.InvoiceNumber` and `InvoiceReprints`. Renaming the cloud to snake_case would break the forward-only gate.
- **P2: "backfilled where known" backfills nothing.** The cloud has no source for an existing row's number, and the table is empty anyway: until #101 no event and no bulk push ever landed. Backlog `InvoiceCompleted` events from before plan 1 carry no number, and store `NULL` (Task 1 pins this).
- **P3: the `(StoreId, InvoiceNumber)` index is not unique.** The till enforces uniqueness. In the cloud, a store reset and migrated again under rollout Rule 1 re-imports the same numbers under new ids, and a unique index would make that a permanently failing event. See the comment in Task 1 Step 5.
- **P4: an FK from `InvoiceReprints.InvoiceId` to `Invoices.Id`** backs up the guard at database level. It is not in the spec.
- **P5: the reprint's `ProcessedEvent` is keyed by the inbox `EventId`,** not the payload's. They are equal by construction (plan 1 sets `OutboxEvent.Id = eventId`), but the idempotency check reads the inbox's.
- **P6: the inbox tiebreak (Task 4)** is not in the spec. Review Focus 1.

**Placeholder scan:** none left. `<ts>` is generated by `dotnet ef`, and `<dir>` is any results folder. The expected counts are derived (22 → 24 → 31 → 32 cloud; 871 + 13 = 884), and Task 4 Step 4 replaces them with measured numbers.

**Type consistency:** `CloudInvoice.InvoiceNumber : long?` (Tasks 1, 2). `CloudInvoiceReprint { Id, InvoiceId, StoreId, CreatedByUserId, CreatedAtUtc, SyncedAtUtc }` and `CloudDbContext.InvoiceReprints` (Task 3, used in the `ReprintsOfAsync` helper). `Pipeline.AddInvoiceCompletedAsync(Guid? invoiceId = null, long? invoiceNumber = null)` (Task 1, used in Task 3). `Pipeline.AddInvoiceReprintedAsync(Guid invoiceId) : InvoiceReprintedEvent` and `ReprintsOfAsync(Guid)` (Task 3). `AddEventAsync(CloudDbContext, long id, DateTime receivedAtUtc)` (Task 4). `InvoiceReprintedEvent.CreatedUtc` → `CloudInvoiceReprint.CreatedAtUtc`: the cloud side follows the cloud's `…AtUtc` naming.

**Review Focus check:** all five lines have tests in their owning tasks, as named above.

**Found while planning, outside this plan's scope (flag, don't fix here):**
- **A reprint whose invoice never reaches the cloud** (because its `InvoiceCompleted` fails for good) is retried forever and holds a batch slot. This is the open poison-event problem from #101: it needs an attempt count or dead-letter rule, and its own small spec before Phase B.
- `InvoiceCompleted` with a payload that deserializes to `null` is still logged and marked processed. The reprint handler throws instead (Task 3 Step 6).
