using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Drives one poll of the real EventProcessor against PostgreSQL with the retry strategy on. Its
/// InvoiceCompleted handler opened a bare BeginTransactionAsync, which that strategy rejects, so
/// no v4 sale ever reached cloud_invoice.
/// </summary>
public class EventProcessorTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const int BatchSize = 50;

    // A type the cloud cannot handle yet (stock adjustments send no event today).
    private const string EventTypeWithoutAHandler = "StockAdjusted";

    // A v4 number continuing a v3 store's history (spec §4).
    private const long KnownInvoiceNumber = 7008;

    // The harness's events belong to store "1" unless a test says otherwise.
    private const string OtherStoreId = "2";

    // Marking an unhandled event processed drops it for good, before its handler ships.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventTypeItCannotHandle_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddEventAsync(EventTypeWithoutAHandler);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
    }

    // Left unprocessed but still fetched, unhandled events would fill the batch and starve sales.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithABatchOfEventsItCannotHandle_StillStoresAnInvoiceBehindThem()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        for (var i = 0; i < BatchSize + 1; i++)
            await pipeline.AddEventAsync(EventTypeWithoutAHandler);
        var sale = await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.FindInvoiceAsync(sale.InvoiceId)).Should()
                                                         .NotBeNull(pipeline.LoggedErrors);
    }

    // One DbContext serves the whole batch. A failed save leaves its rows tracked as Added, so the
    // next event's save would try to insert them again and fail too.
    [Fact]
    public async Task ProcessPendingEventsAsync_WhenAnEventFails_StillStoresTheNextOne()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var alreadyInCloud = await pipeline.AddCloudInvoiceAsync();
        await pipeline.AddInvoiceCompletedAsync(alreadyInCloud);
        var next = await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.FindInvoiceAsync(next.InvoiceId)).Should()
                                                         .NotBeNull(pipeline.LoggedErrors);
    }

    // A fresh event beside it, so a pipeline that processes nothing at all cannot pass.
    [Fact]
    public async Task ProcessPendingEventsAsync_WhenAnEventFails_LeavesOnlyItUnprocessedToRetry()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var alreadyInCloud = await pipeline.AddCloudInvoiceAsync();
        await pipeline.AddInvoiceCompletedAsync(alreadyInCloud);
        await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1, pipeline.LoggedErrors);
    }

    // Codex P1 (PR #102): failed events stayed the oldest, so a full batch of them was fetched on
    // every poll and the events that would unblock them never were.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithABatchOfFailingEvents_StillStoresAnInvoiceBehindThem()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        for (var i = 0; i < BatchSize; i++)
            await pipeline.AddInvoiceCompletedAsync(await pipeline.AddCloudInvoiceAsync());
        var sale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.PollAsync();

        await pipeline.PollAsync();

        (await pipeline.FindInvoiceAsync(sale.InvoiceId)).Should()
                                                         .NotBeNull(pipeline.LoggedErrors);
    }

    [Fact]
    public void RetryDelay_WithNoEarlierFailures_ReturnsTheFirstRetryDelay()
    {
        EventProcessor.RetryDelay(earlierFailures: 0).Should()
                                                     .Be(EventProcessor.FirstRetryDelay);
    }

    [Fact]
    public void RetryDelay_WithManyEarlierFailures_ReturnsTheMaximum()
    {
        EventProcessor.RetryDelay(earlierFailures: 64).Should()
                                                      .Be(EventProcessor.MaxRetryDelay);
    }

    [Fact]
    public void RetryDelay_WithOneEarlierFailure_ReturnsTwiceTheFirstDelay()
    {
        EventProcessor.RetryDelay(earlierFailures: 1).Should()
                                                     .Be(EventProcessor.FirstRetryDelay * 2);
    }

    // A fresh event beside it, so a pipeline that stores nothing at all cannot pass.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventAlreadyInProcessedEvents_StoresOnlyTheFreshOne()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var alreadyProcessed = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddProcessedEventAsync(alreadyProcessed.EventId);
        await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.CountInvoicesAsync()).Should()
                                             .Be(1, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventAlreadyInProcessedEvents_MarksItProcessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddProcessedEventAsync(evt.EventId);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithMoreEventsThanOneBatch_ProcessesThemAllOverTwoPolls()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        for (var i = 0; i < BatchSize + 1; i++)
            await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();
        await pipeline.PollAsync();

        (await pipeline.CountInvoicesAsync()).Should()
                                             .Be(BatchSize + 1, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_StoresTheInvoice()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.FindInvoiceAsync(evt.InvoiceId)).Should()
                                                        .NotBeNull(pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_StoresItsLineAndPayment()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        (invoice?.Lines.Count, invoice?.Payments.Count).Should()
                                                       .Be((1, 1), pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedEvent_MarksTheEventProcessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddInvoiceCompletedAsync();

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }

    // Spec §9: a till queues InvoiceCompleted before it upgrades, so its payload has no number.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnEventFromBeforeBillNumbers_StoresANullNumber()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync(invoiceNumber: null);

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        invoice.Should()
               .NotBeNull(pipeline.LoggedErrors);
        invoice!.InvoiceNumber.Should()
                              .BeNull();
    }

    // Spec 2026-10-19 §3: the store decides the sale's day; a till from before this field sends none.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithoutABusinessDate_StoresNull()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync(businessDate: null);

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        invoice.Should()
               .NotBeNull(pipeline.LoggedErrors);
        invoice!.BusinessDate.Should()
                             .BeNull();
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithABusinessDate_StoresIt()
    {
        var bangkokDay = new DateOnly(2026, 10, 20);
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync(businessDate: bangkokDay);

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        invoice.Should()
               .NotBeNull(pipeline.LoggedErrors);
        invoice!.BusinessDate.Should()
                             .Be(bangkokDay);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithABillNumber_StoresIt()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var evt = await pipeline.AddInvoiceCompletedAsync(invoiceNumber: KnownInvoiceNumber);

        await pipeline.PollAsync();

        var invoice = await pipeline.FindInvoiceAsync(evt.InvoiceId);
        invoice.Should()
               .NotBeNull(pipeline.LoggedErrors);
        invoice!.InvoiceNumber.Should()
                              .Be(KnownInvoiceNumber);
    }

    // A literal-null payload was logged and then marked processed, so the sale never reached the cloud.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceCompletedWhosePayloadIsNull_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddEventAsync("InvoiceCompleted", payload: "null");

        await pipeline.PollAsync();

        // Unprocessed alone also holds with no handler; one failed attempt proves it was fetched and refused.
        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
        (await pipeline.FailedAttemptsAsync()).Should()
                                              .Be(1);
    }

    // The reprint handler already throws on a null payload; this pins it against becoming "log and return".
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAnInvoiceReprintedWhosePayloadIsNull_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddEventAsync("InvoiceReprinted", payload: "null");

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
        (await pipeline.FailedAttemptsAsync()).Should()
                                              .Be(1);
    }

    // Spec §9: a reprint that arrives before its InvoiceCompleted fails so it retries.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintBeforeItsInvoice_LeavesItUnprocessed()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        await pipeline.AddInvoiceReprintedAsync(Guid.NewGuid());

        await pipeline.PollAsync();

        // Unprocessed alone also holds with no handler; one failed attempt proves it was fetched and refused.
        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(1);
        (await pipeline.FailedAttemptsAsync()).Should()
                                              .Be(1);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintBeforeItsInvoice_StoresNoReprint()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
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
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
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

    // A payload can pair its own StoreId with another store's InvoiceId, so the guard matches the
    // invoice id AND the store. The fresh reprint beside it is stored, so a handler that stores
    // nothing at all cannot pass.
    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintOfAnotherStoresInvoice_StoresNoReprint()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var otherStoresSale = await pipeline.AddInvoiceCompletedAsync(storeId: OtherStoreId);
        var ownSale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddInvoiceReprintedAsync(otherStoresSale.InvoiceId);
        await pipeline.AddInvoiceReprintedAsync(ownSale.InvoiceId);

        await pipeline.PollAsync();

        var stored = (await pipeline.ReprintsOfAsync(ownSale.InvoiceId)).Count
                     + (await pipeline.ReprintsOfAsync(otherStoresSale.InvoiceId)).Count;
        stored.Should()
              .Be(1, pipeline.LoggedErrors);
        (await pipeline.ReprintsOfAsync(otherStoresSale.InvoiceId)).Should()
                                                                   .BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprintAlreadyInProcessedEvents_StoresOnlyTheFreshOne()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
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
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var sale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);

        await pipeline.PollAsync();

        (await pipeline.ReprintsOfAsync(sale.InvoiceId)).Should()
                                                        .HaveCount(2, pipeline.LoggedErrors);
    }

    [Fact]
    public async Task ProcessPendingEventsAsync_WithAReprint_CopiesTheReprintFields()
    {
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
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
        await using var pipeline = await EventPipeline.CreateAsync(postgres);
        var sale = await pipeline.AddInvoiceCompletedAsync();
        await pipeline.AddInvoiceReprintedAsync(sale.InvoiceId);

        await pipeline.PollAsync();

        (await pipeline.CountUnprocessedAsync()).Should()
                                                .Be(0, pipeline.LoggedErrors);
    }
}
