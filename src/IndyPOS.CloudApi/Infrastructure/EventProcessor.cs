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

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
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

        // Use transaction for atomicity
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Create invoice
            var invoice = new CloudInvoice
            {
                Id = eventData.InvoiceId,
                StoreId = eventData.StoreId,
                UserId = eventData.UserId,
                TotalAmount = eventData.TotalAmount,
                CreatedAtUtc = eventData.CreatedAtUtc,
                SyncedAtUtc = now
            };
            dbContext.Invoices.Add(invoice);

            // Create invoice lines
            foreach (var lineSnapshot in eventData.Lines)
            {
                var line = new CloudInvoiceLine
                {
                    Id = lineSnapshot.LineId,
                    InvoiceId = eventData.InvoiceId,
                    ProductId = lineSnapshot.ProductId,
                    ProductName = lineSnapshot.ProductName,
                    Quantity = lineSnapshot.Quantity,
                    UnitPrice = lineSnapshot.UnitPrice
                };
                dbContext.InvoiceLines.Add(line);
            }

            // Create payments
            foreach (var paymentSnapshot in eventData.Payments)
            {
                var payment = new CloudPayment
                {
                    Id = paymentSnapshot.PaymentId,
                    InvoiceId = eventData.InvoiceId,
                    Method = paymentSnapshot.Method,
                    Amount = paymentSnapshot.Amount,
                    Note = paymentSnapshot.Note
                };
                dbContext.Payments.Add(payment);
            }

            // Create inventory movements
            foreach (var movementSnapshot in eventData.InventoryMovements)
            {
                var movement = new CloudInventoryMovement
                {
                    Id = movementSnapshot.MovementId,
                    StoreId = eventData.StoreId,
                    ProductId = movementSnapshot.ProductId,
                    QuantityDelta = movementSnapshot.QuantityDelta,
                    Reason = movementSnapshot.Reason,
                    ReferenceId = eventData.InvoiceId,
                    CreatedAtUtc = eventData.CreatedAtUtc,
                    SyncedAtUtc = now
                };
                dbContext.InventoryMovements.Add(movement);
            }

            // Record processed event for idempotency
            var processedEvent = new ProcessedEvent
            {
                EventId = eventData.EventId,
                EventType = "InvoiceCompleted",
                StoreId = eventData.StoreId,
                ProcessedAtUtc = now
            };
            dbContext.ProcessedEvents.Add(processedEvent);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
