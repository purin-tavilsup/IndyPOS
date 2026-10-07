namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Polls StoreHub's readiness endpoint.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. The upgrade
/// path also runs it AFTER a rollback: starting the service is not evidence it came
/// back (see the upgrade design spec, section 5).</para>
/// </summary>
public static class HealthProbe
{
    // Long enough for PostgreSQL to come up after a reboot. Bounded by time, not attempts: StoreHub's
    // readiness check fails within about 3 seconds, so a fixed number of attempts would end the wait
    // too soon and roll an upgrade back while the database is still starting.
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RetryGap = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);

    public static Task<bool> IsReadyAsync(int port, CancellationToken cancellationToken = default) =>
        // /health/ready is StoreHub's readiness probe (its own database). /health is mapped in
        // Development only, so an installed StoreHub answers it with 404.
        IsReadyAsync(new Uri($"http://localhost:{port}/health/ready"), new HttpClientHandler(),
                     TimeProvider.System, (gap, ct) => Task.Delay(gap, ct), cancellationToken);

    internal static async Task<bool> IsReadyAsync(
        Uri readyUrl,
        HttpMessageHandler handler,
        TimeProvider clock,
        Func<TimeSpan, CancellationToken, Task> delay,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient(handler) { Timeout = AttemptTimeout };
        var giveUpAt = clock.GetUtcNow() + Deadline;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await AnswersReadyAsync(client, readyUrl, cancellationToken))
                return true;

            if (clock.GetUtcNow() + RetryGap > giveUpAt)
                return false;

            await delay(RetryGap, cancellationToken);
        }
    }

    private static async Task<bool> AnswersReadyAsync(HttpClient client, Uri readyUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(readyUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}
