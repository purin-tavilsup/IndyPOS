namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>How long an outbox event waits before its next send after a failure that may pass.</summary>
public static class SyncRetryPolicy
{
    /// <summary>
    /// The base delay doubled per attempt so far, capped at <see cref="SyncWorkerOptions.MaxRetryDelaySeconds"/>.
    /// There is no attempt limit, so the doubling is capped in seconds before a TimeSpan is built:
    /// 2^attempts overflows long before a day-long outage ends.
    /// </summary>
    public static TimeSpan NextDelay(int attemptsSoFar, SyncWorkerOptions options)
    {
        var seconds = options.BaseRetryDelaySeconds * Math.Pow(2, attemptsSoFar);

        return TimeSpan.FromSeconds(Math.Min(seconds, options.MaxRetryDelaySeconds));
    }
}
