namespace IndyPOS.Infrastructure.Services.StoreHub;

using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Tells the cloud every two minutes that this StoreHub is up and how its outbox stands, and carries out the resend
/// requests in the reply. Runs on its own, so a slow or absent cloud never delays a sale or the sync worker.
/// </summary>
/// <remarks>
/// Kept light: the failed list and the completed resends are sent as null when empty, and each reason is cut to 200
/// characters (the full one stays in last_error). A completed resend is reported until a heartbeat carrying it gets
/// through. It is held in memory only: if StoreHub restarts before reporting it, the cloud hands the request out
/// again and it is recorded as having moved nothing. Making it durable is not worth a table.
/// </remarks>
public class HeartbeatWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<HeartbeatOptions> options,
    ILogger<HeartbeatWorker> logger) : BackgroundService
{
    private readonly HeartbeatOptions _options = options.Value;
    private readonly List<CompletedResend> _completedResends = [];
    private readonly string _version = AppVersion.GetVersionInfo().DisplayVersion;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BeatAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Heartbeat failed; the next one goes on schedule");
            }

            // At least a second: Task.Delay throws on a negative delay outside the catch, which would stop the host.
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds)), stoppingToken);
        }
    }

    private async Task BeatAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetService<ICloudHeartbeatClient>();
        if (client is null)
            return;

        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var reply = await client.SendAsync(await BuildAsync(outbox, cancellationToken), cancellationToken);
        if (reply is null)
            return;

        _completedResends.Clear();
        foreach (var resend in reply.Resends ?? [])
            _completedResends.Add(await ResendAsync(outbox, resend, cancellationToken));
    }

    private async Task<HeartbeatRequest> BuildAsync(IOutboxRepository outbox, CancellationToken cancellationToken)
    {
        var failed = await outbox.GetFailedEventsAsync(HeartbeatLimits.MaxFailedEventsPerHeartbeat, cancellationToken);

        return new HeartbeatRequest(
            _version,
            await outbox.GetPendingCountAsync(cancellationToken),
            await outbox.GetFailedCountAsync(cancellationToken),
            AgeInSeconds(await outbox.GetOldestPendingCreatedUtcAsync(cancellationToken)),
            failed.Count == 0 ? null : failed.Select(e => e with { Reason = e.Reason.CutTo(HeartbeatLimits.MaxReasonLength) }).ToList(),
            _completedResends.Count == 0 ? null : _completedResends.ToList());
    }

    private static int? AgeInSeconds(DateTime? createdUtc) =>
        createdUtc is { } created ? (int)Math.Max(0, (DateTime.UtcNow - created).TotalSeconds) : null;

    private async Task<CompletedResend> ResendAsync(IOutboxRepository outbox, ResendInstruction resend, CancellationToken cancellationToken)
    {
        var moved = await outbox.RequeueFailedAsync(resend.EventId, cancellationToken);
        IReadOnlyList<Guid> ignored = resend.EventId is { } id && !moved.Contains(id) ? [id] : [];

        logger.LogInformation("Resend {RequestId}: moved {Moved} failed event(s) back to Pending", resend.RequestId, moved.Count);
        return new CompletedResend(resend.RequestId, moved, ignored);
    }
}
