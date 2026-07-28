namespace IndyPOS.Bootstrapper.Installers;

/// <summary>
/// Polls StoreHub's readiness endpoint.
/// <para>USED BY BOTH INSTALL PATHS — fresh install and in-place upgrade. The upgrade
/// path also runs it AFTER a rollback: starting the service is not evidence it came
/// back (see the upgrade design spec, section 5).</para>
/// </summary>
public static class HealthProbe
{
    public static async Task<bool> IsReadyAsync(
        int port,
        int attempts = 5,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // /health/ready is StoreHub's DB-aware readiness probe. /health and /alive are
        // dev-only (Aspire's IsDevelopment() guard in ServiceDefaults).
        var healthUrl = $"http://localhost:{port}/health/ready";

        for (var i = 0; i < attempts; i++)
        {
            try
            {
                var response = await client.GetAsync(healthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Retry
            }

            await Task.Delay(2000, cancellationToken);
        }

        return false;
    }
}
