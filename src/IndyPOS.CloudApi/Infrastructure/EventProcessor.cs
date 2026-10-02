using System.Text.Json;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.Cloud.Sync.Events;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Background service that processes ingested events.
/// Implements idempotent processing - safe for duplicate delivery.
/// </summary>
public class EventProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventProcessor> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The event types this processor has a handler for. Only these are fetched from the inbox:
    /// stores already send types the cloud cannot handle yet (the cash-drawer events), and those
    /// must wait for their handler rather than be marked processed and lost, or fill the batch and
    /// starve the sales behind them. A new handler goes here and in the switch together.
    /// </summary>
    internal static readonly IReadOnlyCollection<string> HandledEventTypes = ["InvoiceCompleted", "InvoiceReprinted"];

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

    public EventProcessor(IServiceScopeFactory scopeFactory, ILogger<EventProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing events");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("EventProcessor stopped");
    }

    /// <summary>One poll: processes up to one batch of unprocessed events. Internal so tests can drive it.</summary>
    internal async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<ISyncedEventRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CloudDbContext>();

        var pendingEvents = await eventRepository.GetUnprocessedAsync(HandledEventTypes, limit: 50, cancellationToken);

        foreach (var syncedEvent in pendingEvents)
        {
            try
            {
                await ProcessEventAsync(syncedEvent, dbContext, eventRepository, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutting down is not a failed attempt; it must not count against the event.
                throw;
            }
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
            finally
            {
                // One DbContext serves the whole batch. Rows an event added stay tracked after its
                // save -- as Added if the save failed -- and the next event's save would insert them
                // again, failing an innocent event. Each event starts from a clean tracker.
                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private async Task ProcessEventAsync(
        SyncedEventEntity syncedEvent,
        CloudDbContext dbContext,
        ISyncedEventRepository eventRepository,
        CancellationToken cancellationToken)
    {
        // Idempotency check - skip if already processed
        var alreadyProcessed = await dbContext.ProcessedEvents
            .AnyAsync(e => e.EventId == syncedEvent.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogDebug("Event {EventId} already processed, skipping", syncedEvent.EventId);
            await eventRepository.MarkProcessedAsync(syncedEvent.EventId, cancellationToken);
            return;
        }

        // Process based on event type
        switch (syncedEvent.EventType)
        {
            case "InvoiceCompleted":
                await ProcessInvoiceCompletedAsync(syncedEvent, dbContext, cancellationToken);
                break;

            case "InvoiceReprinted":
                await ProcessInvoiceReprintedAsync(syncedEvent, dbContext, cancellationToken);
                break;

            // Only HandledEventTypes are fetched, so this means the list and the switch disagree.
            // Throwing leaves the event unprocessed to retry; marking it would drop it for good.
            default:
                throw new InvalidOperationException(
                    $"No handler for event type '{syncedEvent.EventType}'. Add it to {nameof(HandledEventTypes)} and this switch together.");
        }

        // A separate statement, after the handler's save. If the process dies in between, the
        // ProcessedEvents check above marks the event on the next poll instead of storing it twice.
        await eventRepository.MarkProcessedAsync(syncedEvent.EventId, cancellationToken);

        _logger.LogInformation("Processed event {EventId} of type {EventType}",
            syncedEvent.EventId, syncedEvent.EventType);
    }

    private async Task ProcessInvoiceCompletedAsync(
        SyncedEventEntity syncedEvent,
        CloudDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var eventData = JsonSerializer.Deserialize<InvoiceCompletedEvent>(syncedEvent.Payload);
        if (eventData is null)
        {
            _logger.LogError("Failed to deserialize InvoiceCompletedEvent for {EventId}", syncedEvent.EventId);
            return;
        }

        var now = DateTime.UtcNow;

        dbContext.Invoices.Add(new CloudInvoice
        {
            Id = eventData.InvoiceId,
            StoreId = eventData.StoreId,
            UserId = eventData.UserId,
            TotalAmount = eventData.TotalAmount,
            CreatedAtUtc = eventData.CreatedAtUtc,
            InvoiceNumber = eventData.InvoiceNumber,
            SyncedAtUtc = now
        });

        dbContext.InvoiceLines.AddRange(eventData.Lines.Select(line => new CloudInvoiceLine
        {
            Id = line.LineId,
            InvoiceId = eventData.InvoiceId,
            ProductId = line.ProductId,
            ProductName = line.ProductName,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice
        }));

        dbContext.Payments.AddRange(eventData.Payments.Select(payment => new CloudPayment
        {
            Id = payment.PaymentId,
            InvoiceId = eventData.InvoiceId,
            Method = payment.Method,
            Amount = payment.Amount,
            Note = payment.Note
        }));

        dbContext.InventoryMovements.AddRange(eventData.InventoryMovements.Select(movement => new CloudInventoryMovement
        {
            Id = movement.MovementId,
            StoreId = eventData.StoreId,
            ProductId = movement.ProductId,
            QuantityDelta = movement.QuantityDelta,
            Reason = movement.Reason,
            ReferenceId = eventData.InvoiceId,
            CreatedAtUtc = eventData.CreatedAtUtc,
            SyncedAtUtc = now
        }));

        // Recorded with the rows it guards, so a crash can never store the invoice without it.
        dbContext.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventData.EventId,
            EventType = "InvoiceCompleted",
            StoreId = eventData.StoreId,
            ProcessedAtUtc = now
        });

        // One SaveChangesAsync is already one database transaction, and the Npgsql retrying
        // strategy (on under Aspire's AddNpgsqlDbContext) can replay it safely. The explicit
        // BeginTransactionAsync that used to wrap it was redundant, and that strategy rejects a
        // user-initiated transaction outright -- so it threw on every event and no v4 sale ever
        // reached cloud_invoice.
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <remarks>
    /// Ordering guard (spec §9): a reprint can reach the inbox before its invoice. Throwing leaves
    /// the event unprocessed, so the retry backoff tries it again once InvoiceCompleted has landed;
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

        // Ingest checks the event's StoreId against the token, but a payload can still pair its own
        // StoreId with another store's InvoiceId. Matching the invoice id and the store keeps that
        // reprint out of both stores' audit trails.
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
}
