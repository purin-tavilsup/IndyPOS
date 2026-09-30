# Invoice History — Cloud Mirror Implementation Plan (plan 2 of 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The cloud stores each bill's number and every reprint, so remote dashboards and AI agents see the same bill numbers and reprint trail as the till.

**Architecture:** Two additive CloudApi migrations: a nullable `InvoiceNumber` on the cloud `Invoices` table, and a new insert-only `InvoiceReprints` table. `EventProcessor` copies the number from `InvoiceCompleted`, and gets a new `InvoiceReprinted` handler with an ordering guard. That guard throws, so the event stays unprocessed and retries, if the reprint's invoice has not reached the cloud yet. `BulkMigrationCommandHandler` stores the number for migrated v3 history. A failed inbox event now backs off instead of holding a batch slot, so an event waiting for its dependency cannot starve the ones it waits for. The inbox also gains a deterministic tiebreak, so events that arrived in one ingest batch are processed in the order the store sent them.

**Tech Stack:** C# .NET 10, EF Core 10 + Npgsql, xUnit, FluentAssertions, Testcontainers (Postgres 16), System.Text.Json (default options on both ends).

**Spec:** `docs/superpowers/specs/2026-09-27-invoice-history-v4-design.md` (§9 Cloud, §10 Cloud tests). Plan 1, `docs/superpowers/plans/2026-09-27-invoice-history-storehub-backend.md`, defines the event and command contracts this plan consumes.

## Prerequisites

- **Plan 1 is merged.** This plan reads three things plan 1 adds to `IndyPOS.Application`, and does not create them itself:
  - `InvoiceCompletedEvent.InvoiceNumber` (`long?`, plan 1 Task 2);
  - `MigratedInvoice.InvoiceNumber` (`long?`, the last positional parameter, default `null`; plan 1 Task 3);
  - `InvoiceReprintedEvent` and the type name `"InvoiceReprinted"` (plan 1 Task 8).

  Check before starting: `grep -n "InvoiceNumber" src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceCompletedEvent.cs src/IndyPOS.Application/UseCases/Cloud/Sync/BulkMigration/BulkMigrationCommand.cs` prints two lines, and `src/IndyPOS.Application/UseCases/Cloud/Sync/Events/InvoiceReprintedEvent.cs` exists.
- **PR #101 and the sync store-check fix are merged** (2026-09-30). #101 is the event-pipeline repair. Before it, no event was ever processed, so none of this plan could have worked. It also gives this plan `CloudPostgresFixture`, the `EventProcessorTests.Pipeline` harness and `HandledEventTypes`.
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

1. **A reprint and its invoice arriving in the same ingest batch.** `IngestEventsCommandHandler` stamps one `ReceivedAtUtc` on the whole batch, and the inbox orders only by that, so the reprint can be processed first. Expected: both are stored in one poll, invoice first. → Task 5 (`GetUnprocessedAsync_WithEventsReceivedAtTheSameInstant_ReturnsThemInArrivalOrder`).
2. **An `InvoiceCompleted` queued on a till before the upgrade.** Its payload has no `InvoiceNumber`. Expected: the invoice is stored, and its number is `NULL`, not `0`. → Task 1 (`…WithAnEventFromBeforeBillNumbers_StoresANullNumber`).
3. **An older MigrationTool pushing history.** It sends no `InvoiceNumber`. Expected: the import succeeds, and the number is `NULL`. → Task 2 (`HandleAsync_WithAnInvoiceFromAnOlderMigrator_StoresANullNumber`).
4. **The same reprint delivered twice.** A crash between the handler's save and marking the event leaves it unprocessed. Expected: one mirror row. → Task 4 (`…WithAReprintAlreadyInProcessedEvents_StoresOnlyTheFreshOne`).
5. **One bill reprinted twice.** Spec §6: "pressing reprint again writes a second one. The owner sees both attempts." Expected: two mirror rows. → Task 4 (`…WithTwoReprintsOfOneInvoice_StoresBoth`).

---

## File Structure

```
src/IndyPOS.Application/
  Abstractions/Cloud/Repositories/ISyncedEventRepository.cs   MOD  Attempts, NextAttemptAtUtc, MarkFailedAsync

src/IndyPOS.CloudApi/
  Domain/CloudInvoice.cs                        MOD  + long? InvoiceNumber
  Domain/CloudInvoiceReprint.cs                 NEW  insert-only reprint mirror
  Infrastructure/CloudDbContext.cs              MOD  InvoiceNumber config + InvoiceReprints table
  Infrastructure/EventProcessor.cs              MOD  copy number; InvoiceReprinted handler + guard
  Infrastructure/BulkMigrationCommandHandler.cs MOD  copy number
  Infrastructure/DbSyncedEventRepository.cs     MOD  skip events not yet due; MarkFailedAsync; ThenBy(Id)
  Infrastructure/Migrations/<ts>_AddInboxRetrySchedule.cs        NEW (dotnet ef)
  Infrastructure/Migrations/<ts>_AddInvoiceNumberToInvoices.cs   NEW (dotnet ef)
  Infrastructure/Migrations/<ts>_AddInvoiceReprints.cs           NEW (dotnet ef)

tests/IndyPOS.CloudApi.IntegrationTests/
  EventProcessorTests.cs                        MOD  number + reprint tests; Pipeline helpers
  BulkMigrationCommandHandlerTests.cs           MOD  number tests
  DbSyncedEventRepositoryTests.cs               MOD  ordering tiebreak test

.planning/indypos-overhaul/epic-3-store-rollout-plan.md   MOD  deploy the cloud first (Task 4)
CLAUDE.md, ONBOARDING.md                                    MOD  suite counts (Task 5)
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
- Produces: `CloudInvoice.InvoiceNumber : long?`; the `Pipeline.AddInvoiceCompletedAsync(Guid? invoiceId = null, long? invoiceNumber = null)` test helper, used again by Task 4.

- [ ] **Step 1: Let the test harness send a bill number**

In `EventProcessorTests.cs`, change the `Pipeline.AddInvoiceCompletedAsync` signature and initializer so that tests can set the number:

```csharp
        public async Task<InvoiceCompletedEvent> AddInvoiceCompletedAsync(
            Guid? invoiceId = null, long? invoiceNumber = null, string storeId = "1")
        {
            var evt = new InvoiceCompletedEvent
            {
                EventId = Guid.NewGuid(),
                InvoiceId = invoiceId ?? Guid.NewGuid(),
                InvoiceNumber = invoiceNumber,
                StoreId = storeId,
```

Delete the old `StoreId = "1",` line from the initializer, and leave the rest (`UserId` through `Payments`) as it is. Every existing call passes at most `invoiceId`, so all of them still compile. The `storeId` parameter is used in Task 4.

- [ ] **Step 2: Write the failing tests**

Add this constant under `EventTypeWithoutAHandler`:

```csharp
    // A v4 number continuing a v3 store's history (spec §4).
    private const long KnownInvoiceNumber = 7008;

    // The harness's events belong to store "1" unless a test says otherwise.
    private const string OtherStoreId = "2";
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
Expected: PASS, 24 tests (the 22 already there plus 2 new). The fixture runs `MigrateAsync`, so the new migration is exercised by every test.

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
Expected: PASS, 9 tests (the 7 already there plus 2 new).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.CloudApi/Infrastructure/BulkMigrationCommandHandler.cs tests/IndyPOS.CloudApi.IntegrationTests/BulkMigrationCommandHandlerTests.cs
git commit -m "feat(cloud): keep the v3 bill number on bulk-migrated invoices"
```

---

### Task 3: A failed event waits its turn instead of holding a batch slot

**Why (Codex P1 on PR #102; Pond chose to fix it in this plan, 2026-09-30):** the inbox fetches the 50 oldest unprocessed events, and a failed event stays the oldest. Task 4's ordering guard fails *by design* when a reprint arrives before its invoice, and that really happens. The store's `SyncWorker` keeps sending after a failure (`SyncWorker.cs:104-127`), so a backed-off `InvoiceCompleted` can arrive after its reprint. Once 50 events are waiting like that, they fill every batch, and the invoices they wait for never get fetched: **all cloud sync stops**. The same holds for any event that fails for good (#101's flagged poison case). The store outbox already solves this with `Attempts` + `NextRetryUtc` and exponential backoff (`SyncWorker.cs:130-151`), so the inbox copies that.

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/Cloud/Repositories/ISyncedEventRepository.cs` (entity fields + `MarkFailedAsync`)
- Modify: `src/IndyPOS.CloudApi/Infrastructure/DbSyncedEventRepository.cs`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs` (the per-event `catch`, plus `RetryDelay`)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddInboxRetrySchedule.cs` (generated)
- Modify: `tests/IndyPOS.Application.Tests/UseCases/Cloud/Sync/IngestEventsCommandHandlerTests.cs` (the fake repository)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/DbSyncedEventRepositoryTests.cs`, `tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs`

**Interfaces:**
- Consumes: `ISyncedEventRepository.GetUnprocessedAsync(IReadOnlyCollection<string> eventTypes, int limit, CancellationToken)` (#101).
- Produces:
  - `SyncedEventEntity.Attempts : int` (starts at 0) and `SyncedEventEntity.NextAttemptAtUtc : DateTime?` (null means due now).
  - `ISyncedEventRepository.MarkFailedAsync(Guid eventId, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default)`.
  - `EventProcessor.RetryDelay(int earlierFailures) : TimeSpan` (internal static), plus the constants `FirstRetryDelay` (5 s), `MaxRetryDelay` (1 h) and `ManualReviewAfterAttempts` (10).
  - The test helper `Pipeline.ElapseRetryDelaysAsync()`, used by Task 4.

- [ ] **Step 1: Write the failing repository tests**

Add to `DbSyncedEventRepositoryTests.cs`, before the `AddEventAsync` helpers:

```csharp
    [Fact]
    public async Task GetUnprocessedAsync_WithAnEventNotYetDueForRetry_SkipsIt()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var waiting = await AddEventAsync(db);
        await new DbSyncedEventRepository(db).MarkFailedAsync(waiting.EventId, DateTime.UtcNow.AddHours(1));

        var batch = await new DbSyncedEventRepository(db).GetUnprocessedAsync(["InvoiceCompleted"]);

        batch.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task GetUnprocessedAsync_WithAnEventWhoseRetryIsDue_ReturnsIt()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var due = await AddEventAsync(db);
        await new DbSyncedEventRepository(db).MarkFailedAsync(due.EventId, DateTime.UtcNow.AddSeconds(-1));

        var batch = await new DbSyncedEventRepository(db).GetUnprocessedAsync(["InvoiceCompleted"]);

        batch.Select(e => e.EventId).Should()
                                    .Equal(due.EventId);
    }

    [Fact]
    public async Task MarkFailedAsync_WithAStoredEvent_CountsTheAttempt()
    {
        await using var db = CloudPostgresFixture.CreateContext(await postgres.CreateDatabaseAsync());
        var failed = await AddEventAsync(db);

        await new DbSyncedEventRepository(db).MarkFailedAsync(failed.EventId, DateTime.UtcNow);
        await new DbSyncedEventRepository(db).MarkFailedAsync(failed.EventId, DateTime.UtcNow);

        (await db.SyncedEvents.AsNoTracking().SingleAsync(e => e.EventId == failed.EventId)).Attempts.Should()
                                                                                                    .Be(2);
    }
```

- [ ] **Step 2: Write the failing processor tests**

In `EventProcessorTests.cs`, add after `ProcessPendingEventsAsync_WhenAnEventFails_LeavesOnlyItUnprocessedToRetry`:

```csharp
    // Codex P1 (PR #102): failed events stayed the oldest, so a full batch of them was fetched on
    // every poll and the events that would unblock them never were.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithABatchOfFailingEvents_StillStoresAnInvoiceBehindThem()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        for (var i = 0; i < BatchSize; i++)
            await pipeline.AddInvoiceCompletedAsync(await pipeline.AddCloudInvoiceAsync());
        var sale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.PollAsync();

        await pipeline.PollAsync();

        (await pipeline.FindInvoiceAsync(sale.InvoiceId)).Should()
                                                         .NotBeNull();
    }

    [Fact]
    public void RetryDelay_WithNoEarlierFailures_IsTheFirstRetryDelay()
    {
        EventProcessor.RetryDelay(earlierFailures: 0).Should()
                                                     .Be(EventProcessor.FirstRetryDelay);
    }

    [Fact]
    public void RetryDelay_AfterManyFailures_IsCappedAtTheMaximum()
    {
        EventProcessor.RetryDelay(earlierFailures: 64).Should()
                                                      .Be(EventProcessor.MaxRetryDelay);
    }

    [Fact]
    public void RetryDelay_AfterOneFailure_Doubles()
    {
        EventProcessor.RetryDelay(earlierFailures: 1).Should()
                                                     .Be(EventProcessor.FirstRetryDelay * 2);
    }
```

`earlierFailures: 64` is chosen because `2^64` overflows a `double`-to-`TimeSpan` conversion. The cap must hold there, and not throw. Then add the helper to `Pipeline`, after `CountUnprocessedAsync`:

```csharp
        /// <summary>Stands in for the retry delays passing, so a test need not wait for real time.</summary>
        public Task ElapseRetryDelaysAsync() =>
            WithDbAsync(db => db.SyncedEvents.ExecuteUpdateAsync(s => s.SetProperty(e => e.NextAttemptAtUtc, (DateTime?)null)));
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests`
Expected: build FAILS, because `MarkFailedAsync`, `Attempts`, `NextAttemptAtUtc`, `RetryDelay`, `FirstRetryDelay` and `MaxRetryDelay` do not exist. After Step 6 builds, run `…WithABatchOfFailingEvents…` once **with the Step 5 `catch` change left out**, and confirm it FAILS with "Expected … not to be <null>". That proves the test catches the starvation. Then put the change back.

- [ ] **Step 4: Add the schedule to the inbox**

In `ISyncedEventRepository.cs`, on `SyncedEventEntity`, after `public DateTime? ProcessedAtUtc { get; set; }`:

```csharp

    /// <summary>How many times processing this event has failed.</summary>
    public int Attempts { get; set; }

    /// <summary>
    /// When a failed event may be tried again; null means now. A waiting event is not fetched, so
    /// it cannot hold a batch slot that the events it waits for need.
    /// </summary>
    public DateTime? NextAttemptAtUtc { get; set; }
```

And on `ISyncedEventRepository`, after `MarkProcessedAsync`:

```csharp

    /// <summary>
    /// Record a failed attempt, and hold the event back until <paramref name="nextAttemptAtUtc"/>.
    /// The event is never dropped.
    /// </summary>
    Task MarkFailedAsync(Guid eventId, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default);
```

In `DbSyncedEventRepository.cs`, change the `Where` in `GetUnprocessedAsync` to:

```csharp
        var now = DateTime.UtcNow;

        return await _dbContext.SyncedEvents
            .Where(e => e.ProcessedAtUtc == null
                        && eventTypes.Contains(e.EventType)
                        && (e.NextAttemptAtUtc == null || e.NextAttemptAtUtc <= now))
```

Keep the `OrderBy`, `Take` and `ToListAsync` as they are, and put the `var now` line before the `return`. Then add, after `MarkProcessedAsync`:

```csharp

    public async Task MarkFailedAsync(Guid eventId, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        await _dbContext.SyncedEvents
                        .Where(e => e.EventId == eventId)
                        .ExecuteUpdateAsync(s => s.SetProperty(e => e.Attempts, e => e.Attempts + 1)
                                                  .SetProperty(e => e.NextAttemptAtUtc, nextAttemptAtUtc), cancellationToken);
    }
```

In `IngestEventsCommandHandlerTests.cs`, add to the fake repository, after its `MarkProcessedAsync`:

```csharp

        public Task MarkFailedAsync(Guid eventId, DateTime nextAttemptAtUtc, CancellationToken cancellationToken = default)
        {
            if (Events.TryGetValue(eventId, out var entity))
            {
                entity.Attempts++;
                entity.NextAttemptAtUtc = nextAttemptAtUtc;
            }
            return Task.CompletedTask;
        }
```

- [ ] **Step 5: Schedule the retry when an event fails**

In `EventProcessor.cs`, after `HandledEventTypes`:

```csharp

    /// <summary>
    /// Backoff for a failed event, as the store outbox does (SyncWorker): fast at first, because a
    /// reprint usually waits only seconds for its invoice, then doubling to an hourly retry.
    /// Nothing is ever dropped. From ManualReviewAfterAttempts on, each failure is an Error.
    /// </summary>
    internal static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(1);
    internal const int ManualReviewAfterAttempts = 10;

    internal static TimeSpan RetryDelay(int earlierFailures)
    {
        var seconds = FirstRetryDelay.TotalSeconds * Math.Pow(2, earlierFailures);

        return seconds >= MaxRetryDelay.TotalSeconds ? MaxRetryDelay : TimeSpan.FromSeconds(seconds);
    }
```

Then replace the per-event `catch` in `ProcessPendingEventsAsync`:

```csharp
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {EventId} of type {EventType}",
                    syncedEvent.EventId, syncedEvent.EventType);
            }
```

with:

```csharp
            catch (Exception ex)
            {
                var attempts = syncedEvent.Attempts + 1;
                var nextAttemptAtUtc = DateTime.UtcNow + RetryDelay(syncedEvent.Attempts);

                // Waiting until then, the event is not fetched, so it cannot starve the batch.
                await eventRepository.MarkFailedAsync(syncedEvent.EventId, nextAttemptAtUtc, cancellationToken);

                _logger.Log(
                    attempts >= ManualReviewAfterAttempts ? LogLevel.Error : LogLevel.Warning,
                    ex,
                    "Event {EventId} of type {EventType} failed (attempt {Attempts}); retrying at {NextAttemptAtUtc}",
                    syncedEvent.EventId, syncedEvent.EventType, attempts, nextAttemptAtUtc);
            }
```

The test harness's `RecordingLogger` records `Error` and above. So the early failures now log as `Warning` and no longer show up in `LoggedErrors`. The assertion messages lose that detail, but no test's *result* changes. Change `RecordingLogger.IsEnabled` to `logLevel >= LogLevel.Warning`, so the messages keep the reason.

- [ ] **Step 6: Generate the migration**

Run: `dotnet ef migrations add AddInboxRetrySchedule --project src/IndyPOS.CloudApi --output-dir Infrastructure/Migrations`

Expected: `Up` holds exactly two changes on `SyncedEvents`:
- an `AddColumn<int>` named `Attempts`, with `nullable: false` and `defaultValue: 0`;
- an `AddColumn<DateTime>` named `NextAttemptAtUtc`, with `nullable: true`.

The default keeps the forward-only gate: an older binary's `INSERT` leaves `Attempts` out and gets 0.

- [ ] **Step 7: Check the model and the snapshot agree**

Run: `dotnet ef migrations has-pending-model-changes --project src/IndyPOS.CloudApi`
Expected: `No changes have been made to the model since the last migration.`

- [ ] **Step 8: Run the tests to verify they pass**

Run: `dotnet test tests/IndyPOS.CloudApi.IntegrationTests` and `dotnet test tests/IndyPOS.Application.Tests --filter "FullyQualifiedName~IngestEvents"`
Expected: PASS. That is 33 cloud tests (26 + 7), and 9 ingest tests.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/Cloud/Repositories/ISyncedEventRepository.cs src/IndyPOS.CloudApi/Infrastructure/DbSyncedEventRepository.cs src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs src/IndyPOS.CloudApi/Infrastructure/Migrations tests/IndyPOS.Application.Tests/UseCases/Cloud/Sync/IngestEventsCommandHandlerTests.cs tests/IndyPOS.CloudApi.IntegrationTests
git commit -m "fix(cloud): back off a failed inbox event instead of letting it hold a batch slot"
```

---

### Task 4: Mirror every reprint, and only after its invoice

**Files:**
- Create: `src/IndyPOS.CloudApi/Domain/CloudInvoiceReprint.cs`
- Modify: `src/IndyPOS.CloudApi/Infrastructure/CloudDbContext.cs` (a `DbSet` and a new entity block)
- Modify: `src/IndyPOS.CloudApi/Infrastructure/EventProcessor.cs` (`HandledEventTypes`, the switch, a new handler)
- Create: `src/IndyPOS.CloudApi/Infrastructure/Migrations/<ts>_AddInvoiceReprints.cs` (generated)
- Modify: `.planning/indypos-overhaul/epic-3-store-rollout-plan.md` (deploy order)
- Test: `tests/IndyPOS.CloudApi.IntegrationTests/EventProcessorTests.cs`

**Interfaces:**
- Consumes: `InvoiceReprintedEvent { int SchemaVersion; Guid EventId; Guid ReprintId; Guid InvoiceId; string StoreId; DateTime CreatedUtc; Guid CreatedByUserId }` (plan 1 Task 8); the `Pipeline` helpers `AddInvoiceCompletedAsync`, `AddProcessedEventAsync`, `CountUnprocessedAsync`, `WithDbAsync`, `LoggedErrors` (from #101 and Task 1), and `ElapseRetryDelaysAsync` (Task 3).
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

    // The failed reprint is held back (Task 3), so the poll that stores the late invoice skips it;
    // once its retry is due, the next poll stores it.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintWhoseInvoiceArrivesLater_StoresItOnceItsRetryIsDue()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var invoiceId = Guid.NewGuid();
        await pipeline.AddInvoiceReprintedAsync(invoiceId);
        await pipeline.PollAsync();
        await pipeline.AddInvoiceCompletedAsync(invoiceId);
        await pipeline.PollAsync();
        await pipeline.ElapseRetryDelaysAsync();

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(invoiceId)).Should()
                                                   .ContainSingle(pipeline.LoggedErrors);
    }

    // A fresh reprint beside it, so a handler that stores nothing at all cannot pass.
    // Ingest does not yet check an event's StoreId against the token, so the guard matches the store
    // too: a reprint naming another store's invoice must not corrupt either store's audit trail.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintOfAnotherStoresInvoice_StoresNoReprint()
    {
        await using var pipeline = await Pipeline.CreateAsync(postgres);
        var otherStoresSale = await pipeline.AddInvoiceCompletedAsync(storeId: OtherStoreId);
        await pipeline.AddInvoiceReprintedAsync(otherStoresSale.InvoiceId);

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(otherStoresSale.InvoiceId)).Should()
                                                                   .BeEmpty();
    }

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

        // The store must match too. Ingest does not yet check an event's StoreId against the token,
        // so a payload naming another store's invoice would otherwise land in the wrong audit trail.
        var invoiceArrived = await dbContext.Invoices.AnyAsync(
            i => i.Id == eventData.InvoiceId && i.StoreId == eventData.StoreId, cancellationToken);

        if (!invoiceArrived)
            throw new InvalidOperationException(
                $"Reprint {eventData.ReprintId} has no invoice {eventData.InvoiceId} in store {eventData.StoreId} yet; it is retried.");

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
Expected: PASS, 41 tests (33 after Task 3, plus 8 here).

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

### Task 5: Process one ingest batch in the order the store sent it

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
Expected: every suite green. `CloudApi.IntegrationTests` has **42**, and the solution has **899 total, 898 pass, 1 skipped**: 878 before this plan, plus 21 from it. If the numbers differ, use the **measured** ones in the next step, and say in the commit which figure moved.

- [ ] **Step 5: Update the documented counts**

In `CLAUDE.md` and `ONBOARDING.md`, update the counts #101 set to the numbers measured in Step 4:
- `CloudApi.IntegrationTests`: 22 → 42;
- the solution total: 878 → 899, and pass 877 → 898;
- without the store databases: 858 → 879 (derived as total − 20);
- failures with Docker stopped: 241 → 261 (148 + 71 + 42, derived).

Search each file for the old numbers (`grep -n "878\|877\|858\|241\| 22 " CLAUDE.md ONBOARDING.md`), and leave no old figure behind.

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
- §9 "`InvoiceReprinted` populates a new insert-only … mirror", "Idempotency comes from `ProcessedEvents`", "`HasComment` on the table and columns" → Task 4.
- §9 "Ordering guard … fails so it retries, and is never marked processed" → Task 4 (guard + 3 tests). Task 5 removes the needless retry within one batch.
- §9 "Deploy the cloud handler before any store runs this version" → Task 4 Step 10 (Rule 1c). #101 already made an early event wait rather than be dropped.
- §10 Cloud: "`InvoiceReprinted` before `InvoiceCompleted` stays pending, and succeeds after" → Task 4 (`…BeforeItsInvoice_LeavesItUnprocessed`, `…ArrivesLater_StoresItOnTheNextPoll`). "A duplicate event is processed once" → Task 4 (`…AlreadyInProcessedEvents_StoresOnlyTheFreshOne`). "A bulk-migrated invoice lands in `cloud_invoice` with its v3 `invoice_number`" → Task 2.

**Deviations from the spec, called out:**
- **P1: table and column names follow the cloud's existing convention.** The spec says `cloud_invoice`, `invoice_number` and `cloud_invoice_reprint`. The real tables are PascalCase names taken from the `DbSet`s (`Invoices`, `ProcessedEvents`; `InitialCloudSchema`), so this plan uses `Invoices.InvoiceNumber` and `InvoiceReprints`. Renaming the cloud to snake_case would break the forward-only gate.
- **P2: "backfilled where known" backfills nothing.** The cloud has no source for an existing row's number, and the table is empty anyway: until #101 no event and no bulk push ever landed. Backlog `InvoiceCompleted` events from before plan 1 carry no number, and store `NULL` (Task 1 pins this).
- **P3: the `(StoreId, InvoiceNumber)` index is not unique.** The till enforces uniqueness. In the cloud, a store reset and migrated again under rollout Rule 1 re-imports the same numbers under new ids, and a unique index would make that a permanently failing event. See the comment in Task 1 Step 5.
- **P4: an FK from `InvoiceReprints.InvoiceId` to `Invoices.Id`** backs up the guard at database level. It is not in the spec.
- **P5: the reprint's `ProcessedEvent` is keyed by the inbox `EventId`,** not the payload's. They are equal by construction (plan 1 sets `OutboxEvent.Id = eventId`), but the idempotency check reads the inbox's.
- **P6: the inbox tiebreak (Task 5)** is not in the spec. Review Focus 1.
- **P7: deferred retries (Task 3)** are not in the spec. Codex flagged (P1, PR #102) that Task 4's guard, which fails by design, could fill every batch and stop all sync. Pond chose to fix it here (2026-09-30), rather than in a separate spec.
- **P8: the reprint guard matches the store as well as the invoice id** (Codex P2, PR #102). Ingest does not yet check an event's `StoreId` against the token (see below).

**Placeholder scan:** none left. `<ts>` is generated by `dotnet ef`, and `<dir>` is any results folder. The expected counts are derived (22 → 24 → 26 → 33 → 41 → 42 cloud; 878 + 21 = 899), and Task 5 Step 4 replaces them with measured numbers.

**Type consistency:** `CloudInvoice.InvoiceNumber : long?` (Tasks 1, 2). `CloudInvoiceReprint { Id, InvoiceId, StoreId, CreatedByUserId, CreatedAtUtc, SyncedAtUtc }` and `CloudDbContext.InvoiceReprints` (Task 4, used in the `ReprintsOfAsync` helper). `Pipeline.AddInvoiceCompletedAsync(Guid? invoiceId = null, long? invoiceNumber = null)` (Task 1, used in Task 4). `Pipeline.AddInvoiceReprintedAsync(Guid invoiceId) : InvoiceReprintedEvent` and `ReprintsOfAsync(Guid)` (Task 4). `AddEventAsync(CloudDbContext, long id, DateTime receivedAtUtc)` (Task 5). `InvoiceReprintedEvent.CreatedUtc` → `CloudInvoiceReprint.CreatedAtUtc`: the cloud side follows the cloud's `…AtUtc` naming.

**Review Focus check:** all five lines have tests in their owning tasks, as named above.

**Found while planning, outside this plan's scope (flag, don't fix here):**
- **🚨 `POST /sync/events` trusts each event's `StoreId`** (`Program.cs:149-157`): it never compares it with the token's `store_id`, unlike `/users` (`Program.cs:299`). So any registered store can post events as another store. This is a security fix of its own, with a RED test, before Phase B.
- **An event that fails for good is retried hourly forever.** After Task 3 it no longer blocks anything, and from attempt 10 it logs as `Error` for manual review. There is still no dead-letter view to list such events; add one if they start to appear.
- `InvoiceCompleted` with a payload that deserializes to `null` is still logged and marked processed. The reprint handler throws instead (Task 4 Step 6).
