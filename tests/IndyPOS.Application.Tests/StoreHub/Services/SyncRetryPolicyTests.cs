using FluentAssertions;
using IndyPOS.Infrastructure.Services.StoreHub;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// There is no attempt limit any more, so an outage of a day pushes the attempt count far past where the
/// doubling would overflow. The delay must stay at the 15-minute cap however high it goes.
/// </summary>
public class SyncRetryPolicyTests
{
    private static readonly SyncWorkerOptions Defaults = new();

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 60)]
    [InlineData(2, 120)]
    [InlineData(3, 240)]
    [InlineData(4, 480)]
    [InlineData(5, 900)]
    [InlineData(6, 900)]
    [InlineData(100, 900)]
    [InlineData(int.MaxValue, 900)]
    public void NextDelay_WithAttempts_ReturnsTheDoublingDelayCappedAt15Minutes(int attemptsSoFar, int expectedSeconds)
    {
        SyncRetryPolicy.NextDelay(attemptsSoFar, Defaults).Should()
                                                          .Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    // Zero is a plausible way to ask for no delay. 0 * 2^1024 is 0 * infinity, which is NaN, and
    // TimeSpan.FromSeconds(NaN) throws, so the event would be picked again on every poll.
    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(int.MaxValue)]
    public void NextDelay_WithNoBaseDelay_ReturnsZero(int attemptsSoFar)
    {
        var noDelay = new SyncWorkerOptions { BaseRetryDelaySeconds = 0 };

        SyncRetryPolicy.NextDelay(attemptsSoFar, noDelay).Should()
                                                         .Be(TimeSpan.Zero);
    }
}
