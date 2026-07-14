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

            try
            {
                var success = await cloudSyncClient.SendEventAsync(outboxEvent, cancellationToken);

                if (success)
                {
                    await outboxRepository.MarkAsSentAsync(outboxEvent.Id, cancellationToken);
                    logger.LogDebug("Event {EventId} ({Type}) sent successfully", outboxEvent.Id, outboxEvent.Type);
                }
                else
                {
                    await HandleFailedEventAsync(outboxRepository, outboxEvent, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to send event {EventId}", outboxEvent.Id);
                await HandleFailedEventAsync(outboxRepository, outboxEvent, cancellationToken);
            }
        }
    }

    private async Task HandleFailedEventAsync(
        IOutboxRepository outboxRepository,
        Domain.Entities.Core.OutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        var nextAttempt = outboxEvent.Attempts + 1;

        if (nextAttempt >= _options.MaxRetries)
        {
            logger.LogError(
                "Event {EventId} ({Type}) exceeded max retries ({MaxRetries}). Leaving for manual review.",
                outboxEvent.Id,
                outboxEvent.Type,
                _options.MaxRetries);
            return;
        }

        // Exponential backoff: 30s, 60s, 120s, 240s...
        var delaySeconds = _options.BaseRetryDelaySeconds * Math.Pow(2, nextAttempt - 1);
        var nextRetryUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

        await outboxRepository.MarkAsFailedAsync(outboxEvent.Id, nextRetryUtc, cancellationToken);

        logger.LogDebug(
            "Event {EventId} will retry at {NextRetry} (attempt {Attempt}/{MaxRetries})",
            outboxEvent.Id,
            nextRetryUtc,
            nextAttempt,
            _options.MaxRetries);
    }
}
