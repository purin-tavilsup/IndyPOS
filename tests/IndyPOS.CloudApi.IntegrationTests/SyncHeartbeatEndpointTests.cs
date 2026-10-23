using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// A StoreHub reports every two minutes that it is up and how its outbox stands; the reply carries the resend
/// requests an admin made for it. One row per store, overwritten. Each test gets a fresh database.
/// </summary>
public class SyncHeartbeatEndpointTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";
    private const string OtherStore = "store-b";

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static string Heartbeat(int pending = 0, int failed = 0, IEnumerable<FailedEventSummary>? failedEvents = null,
        IEnumerable<CompletedResend>? completed = null, string version = "4.1.0") =>
        JsonSerializer.Serialize(new HeartbeatRequest(version, pending, failed, OldestPendingCreatedAtUtc: null,
            failedEvents?.ToList(), completed?.ToList()), Web);

    private async Task<(SyncTestHost Host, string ConnectionString)> StartAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        return (await SyncTestHost.StartAsync(connectionString), connectionString);
    }

    private static async Task<CloudStoreHeartbeat> HeartbeatRowAsync(string connectionString)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        return await db.StoreHeartbeats.AsNoTracking().SingleAsync();
    }

    private static async Task<Guid> AddResendRequestAsync(string connectionString, string storeId, Guid? eventId = null)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        var request = new CloudResendRequest { Id = Guid.NewGuid(), StoreId = storeId, EventId = eventId, RequestedAtUtc = DateTime.UtcNow };
        db.SyncResendRequests.Add(request);
        await db.SaveChangesAsync();
        return request.Id;
    }

    private static async Task<HeartbeatResponse> ReplyAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HeartbeatResponse>(Web))!;
    }

    [Fact]
    public async Task PostHeartbeat_FromAStore_StoresItsLatestState()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var failedEvent = new FailedEventSummary(Guid.NewGuid(), "InvoiceCompleted", DateTime.UtcNow, "403 store-mismatch");

        await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(pending: 3, failed: 1, failedEvents: [failedEvent])));

        var row = await HeartbeatRowAsync(connectionString);
        row.StoreId.Should().Be(ThisStore);
        row.PendingCount.Should().Be(3);
        row.FailedCount.Should().Be(1);
        row.StoreHubVersion.Should().Be("4.1.0");
        row.FailedEventsJson.Should().Contain(failedEvent.EventId.ToString());
    }

    [Fact]
    public async Task PostHeartbeat_Twice_KeepsOneRowWithTheNewest()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;

        await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(pending: 5)));
        await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(pending: 0)));

        (await HeartbeatRowAsync(connectionString)).PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task PostHeartbeat_WithMoreThan50FailedEvents_KeepsTheFirst50()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var failedEvents = Enumerable.Range(0, 60)
                                     .Select(_ => new FailedEventSummary(Guid.NewGuid(), "InvoiceCompleted", DateTime.UtcNow, "403"))
                                     .ToList();

        await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(failed: 60, failedEvents: failedEvents)));

        var stored = JsonSerializer.Deserialize<List<FailedEventSummary>>((await HeartbeatRowAsync(connectionString)).FailedEventsJson!, Web)!;
        stored.Select(e => e.EventId).Should().Equal(failedEvents.Take(HeartbeatLimits.MaxFailedEventsPerHeartbeat).Select(e => e.EventId));
    }

    [Fact]
    public async Task PostHeartbeat_WithAnOpenResendRequest_ReturnsIt()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var requestId = await AddResendRequestAsync(connectionString, ThisStore);

        var reply = await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat()));

        reply.Resends.Should().ContainSingle(r => r.RequestId == requestId && r.EventId == null);
    }

    [Fact]
    public async Task PostHeartbeat_ReportingACompletedResend_StopsReturningIt()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var requestId = await AddResendRequestAsync(connectionString, ThisStore);
        var completed = new CompletedResend(requestId, [Guid.NewGuid()], []);

        var reply = await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(completed: [completed])));

        reply.Resends.Should().BeEmpty();
    }

    [Fact]
    public async Task PostHeartbeat_ReportingACompletedResend_RecordsWhatMoved()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var requestId = await AddResendRequestAsync(connectionString, ThisStore);
        var moved = Guid.NewGuid();

        await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(completed: [new CompletedResend(requestId, [moved], [])])));

        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        var request = await db.SyncResendRequests.AsNoTracking().SingleAsync();
        request.CompletedAtUtc.Should().NotBeNull();
        request.MovedEventIdsJson.Should().Contain(moved.ToString());
    }

    [Fact]
    public async Task PostHeartbeat_ReportingAnotherStoresResend_LeavesItOpen()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var otherStoresRequest = await AddResendRequestAsync(connectionString, OtherStore);

        await ReplyAsync(await host.PostHeartbeatAsStoreAsync(ThisStore, Heartbeat(completed: [new CompletedResend(otherStoresRequest, [], [])])));

        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        (await db.SyncResendRequests.AsNoTracking().SingleAsync()).CompletedAtUtc.Should().BeNull();
    }

    // A newer StoreHub may send fields this CloudApi does not know; the heartbeat must still count.
    [Fact]
    public async Task PostHeartbeat_WithFieldsItDoesNotKnow_IsAccepted()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;

        var response = await host.PostHeartbeatAsStoreAsync(ThisStore, """{"pendingCount":2,"failedCount":0,"futureField":{"a":1}}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await HeartbeatRowAsync(connectionString)).PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task PostHeartbeat_WithoutAStoreClaim_ReturnsForbidden()
    {
        var (host, _) = await StartAsync();
        await using var __ = host;

        var response = await host.PostHeartbeatAsStoreAsync(SyncTestHost.WithoutAStore, Heartbeat());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
