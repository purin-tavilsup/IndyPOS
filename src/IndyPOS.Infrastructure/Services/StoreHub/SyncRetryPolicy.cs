namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>How long an outbox event waits before its next send after a failure that may pass.</summary>
public static class SyncRetryPolicy
{
    /// <summary>
    /// The base delay doubled per attempt so far, capped at <see cref="SyncWorkerOptions.MaxRetryDelaySeconds"/>.
    /// There is no attempt limit, so the doubling is capped in seconds before a TimeSpan is built:
    /// 2^attempts overflows long before a day-long outage ends. The exponent is capped too, because a
    /// base of 0 times 2^1024 (infinity) is NaN, which TimeSpan rejects.
    /// </summary>
    public static TimeSpan NextDelay(int attemptsSoFar, SyncWorkerOptions options)
    {
        const int DoublingsBeyondAnyCap = 30;
        var seconds = options.BaseRetryDelaySeconds * Math.Pow(2, Math.Min(attemptsSoFar, DoublingsBeyondAnyCap));

        return TimeSpan.FromSeconds(Math.Min(seconds, options.MaxRetryDelaySeconds));
    }
}
