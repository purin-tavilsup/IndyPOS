using FluentAssertions;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.Cloud.Sync;

/// <summary>
/// The admin page's store status. Store PCs are switched off at night, so a store not seen is Offline,
/// shown neutrally, never an alarm; failed events are the one state that wins over everything else.
/// </summary>
public class StoreSyncHealthTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly StoreSyncHealthOptions Defaults = new();

    private static StoreSyncState Evaluate(
        DateTime? lastSeenUtc,
        int failedCount = 0,
        DateTime? oldestPendingUtc = null,
        DateTime? newestUnresolvedRejectionUtc = null,
        StoreSyncHealthOptions? options = null) =>
        StoreSyncHealth.Evaluate(lastSeenUtc, failedCount, oldestPendingUtc, newestUnresolvedRejectionUtc, Now, options ?? Defaults);

    [Fact]
    public void Evaluate_WhenSeenRecentlyWithNothingWaiting_ReturnsOk() =>
        Evaluate(Now.AddMinutes(-1)).Should()
                                    .Be(StoreSyncState.Ok);

    [Fact]
    public void Evaluate_WhenTheOldestPendingIsUnder30Minutes_ReturnsOk() =>
        Evaluate(Now.AddMinutes(-1), oldestPendingUtc: Now.AddMinutes(-29)).Should()
                                                                           .Be(StoreSyncState.Ok);

    [Fact]
    public void Evaluate_WhenTheOldestPendingIs30MinutesOrMore_ReturnsBehind() =>
        Evaluate(Now.AddMinutes(-1), oldestPendingUtc: Now.AddMinutes(-30)).Should()
                                                                           .Be(StoreSyncState.Behind);

    [Fact]
    public void Evaluate_WhenTheHeartbeatReportsFailedEvents_ReturnsFailedEvents() =>
        Evaluate(Now.AddMinutes(-1), failedCount: 1).Should()
                                                    .Be(StoreSyncState.FailedEvents);

    [Fact]
    public void Evaluate_WhenOfflineWithFailedEvents_ReturnsFailedEvents() =>
        Evaluate(Now.AddHours(-12), failedCount: 3).Should()
                                                   .Be(StoreSyncState.FailedEvents);

    [Fact]
    public void Evaluate_WhenARejectionIsNewerThanTheHeartbeat_ReturnsFailedEvents() =>
        Evaluate(Now.AddMinutes(-5), newestUnresolvedRejectionUtc: Now.AddMinutes(-1)).Should()
                                                                                      .Be(StoreSyncState.FailedEvents);

    [Fact]
    public void Evaluate_WhenTheHeartbeatIsNewerThanAnUnresolvedRejection_ReturnsOk() =>
        Evaluate(Now.AddMinutes(-1), newestUnresolvedRejectionUtc: Now.AddMinutes(-5)).Should()
                                                                                      .Be(StoreSyncState.Ok);

    [Fact]
    public void Evaluate_WhenARejectionExistsButTheStoreWasNeverSeen_ReturnsFailedEvents() =>
        Evaluate(lastSeenUtc: null, newestUnresolvedRejectionUtc: Now.AddMinutes(-1)).Should()
                                                                                     .Be(StoreSyncState.FailedEvents);

    [Fact]
    public void Evaluate_WhenNotSeenFor10Minutes_ReturnsOffline() =>
        Evaluate(Now.AddMinutes(-10)).Should()
                                     .Be(StoreSyncState.Offline);

    [Fact]
    public void Evaluate_WhenNeverSeen_ReturnsOffline() =>
        Evaluate(lastSeenUtc: null).Should()
                                   .Be(StoreSyncState.Offline);

    [Fact]
    public void Evaluate_WithCustomThresholds_UsesThem() =>
        Evaluate(Now.AddMinutes(-1), oldestPendingUtc: Now.AddMinutes(-5), options: new StoreSyncHealthOptions { BehindAfterMinutes = 5 })
            .Should()
            .Be(StoreSyncState.Behind);

    // The admin page and anything else reading the API see the state's name; a number would change meaning if
    // the enum were ever reordered.
    [Fact]
    public void StoreSyncState_WhenSerialized_IsItsName() =>
        System.Text.Json.JsonSerializer.Serialize(StoreSyncState.Offline).Should()
                                                                        .Be("\"Offline\"");
}
