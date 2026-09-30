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

        var pendingEvents = await eventRepository.GetUnprocessedAsync(limit: 50, cancellationToken);

        foreach (var syncedEvent in pendingEvents)
        {
            try
            {
                await ProcessEventAsync(syncedEvent, dbContext, eventRepository, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {EventId} of type {EventType}",
                    syncedEvent.EventId, syncedEvent.EventType);
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

            default:
                _logger.LogWarning("Unknown event type: {EventType}", syncedEvent.EventType);
                break;
        }

        // Mark as processed in both tables (atomic)
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
}
