namespace IndyPOS.Infrastructure.Services.StoreHub;

using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class SyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<SyncWorkerOptions> options,
    ILogger<SyncWorker> logger) : BackgroundService
{
    private readonly SyncWorkerOptions _options = options.Value;
    private DateTime _lastUserSync = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SyncWorker is disabled");
            return;
        }

        logger.LogInformation(
            "SyncWorker started. Polling every {Interval}s, batch size {BatchSize}, user sync every {UserSyncInterval}s",
            _options.PollingIntervalSeconds,
            _options.BatchSize,
            _options.UserSyncIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);

                // User sync (Epic S2) - runs at separate interval
                if (ShouldSyncUsers())
                {
                    await SyncUsersAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in SyncWorker");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("SyncWorker stopped");
    }

    private bool ShouldSyncUsers()
    {
        var elapsed = (DateTime.UtcNow - _lastUserSync).TotalSeconds;
        return elapsed >= _options.UserSyncIntervalSeconds;
    }

    private async Task SyncUsersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        // IUserSyncService might not be registered if auth services aren't added
        var userSyncService = scope.ServiceProvider.GetService<IUserSyncService>();
        if (userSyncService is null)
        {
            return;
        }

        try
        {
            var synced = await userSyncService.SyncUsersFromCloudAsync(cancellationToken);
            _lastUserSync = DateTime.UtcNow;

            if (synced > 0)
            {
                logger.LogInformation("Synced {Count} users from cloud", synced);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "User sync failed");
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var cloudSyncClient = scope.ServiceProvider.GetRequiredService<ICloudSyncClient>();

        var events = await outboxRepository.GetPendingEventsAsync(_options.BatchSize, cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        logger.LogDebug("Processing {Count} outbox events", events.Count);

        foreach (var outboxEvent in events)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await SendAsync(outboxRepository, cloudSyncClient, outboxEvent, cancellationToken);
        }
    }

    private async Task SendAsync(
        IOutboxRepository outboxRepository,
        ICloudSyncClient cloudSyncClient,
        Domain.Entities.Core.OutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        SyncSendResult result;
        try
        {
            result = await cloudSyncClient.SendEventAsync(outboxEvent, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to send event {EventId}", outboxEvent.Id);
            result = SyncSendResult.RetryLater(ex.Message);
        }

        await RecordAsync(outboxRepository, outboxEvent, result, cancellationToken);
    }

    /// <remarks>
    /// Only the cloud's own refusal parks an event as Failed, which takes it out of the queue so it cannot
    /// hold back the events behind it. Anything else is retried with no attempt limit: an outage of any
    /// length must not turn a good sale into a failed one.
    /// </remarks>
    private async Task RecordAsync(
        IOutboxRepository outboxRepository,
        Domain.Entities.Core.OutboxEvent outboxEvent,
        SyncSendResult result,
        CancellationToken cancellationToken)
    {
        switch (result.Outcome)
        {
            case SyncSendOutcome.Sent:
                await outboxRepository.MarkAsSentAsync(outboxEvent.Id, cancellationToken);
                logger.LogDebug("Event {EventId} ({Type}) sent successfully", outboxEvent.Id, outboxEvent.Type);
                break;

            case SyncSendOutcome.Refused:
                await outboxRepository.MarkAsRefusedAsync(outboxEvent.Id, result.Reason ?? "Refused by the cloud", cancellationToken);
                logger.LogWarning(
                    "Event {EventId} ({Type}) was refused by the cloud and is kept as Failed: {Reason}",
                    outboxEvent.Id,
                    outboxEvent.Type,
                    result.Reason);
                break;

            default:
                var nextRetryUtc = DateTime.UtcNow + SyncRetryPolicy.NextDelay(outboxEvent.Attempts, _options);
                await outboxRepository.ScheduleRetryAsync(outboxEvent.Id, nextRetryUtc, result.Reason, cancellationToken);
                logger.LogDebug(
                    "Event {EventId} will retry at {NextRetry} (attempt {Attempt}): {Reason}",
                    outboxEvent.Id,
                    nextRetryUtc,
                    outboxEvent.Attempts + 1,
                    result.Reason);
                break;
        }
    }
}
