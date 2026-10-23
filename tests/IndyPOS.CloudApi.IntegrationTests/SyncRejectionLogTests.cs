using System.Net;
using System.Text.Json;
using FluentAssertions;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Every refusal the cloud gives on /sync/events is logged per store and event, so the admin page can show it even
/// when the store's own heartbeat cannot get through. It resolves once the same event is accepted.
/// </summary>
public class SyncRejectionLogTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    private const string ThisStore = "store-a";

    private async Task<(SyncTestHost Host, string ConnectionString)> StartAsync()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        return (await SyncTestHost.StartAsync(connectionString), connectionString);
    }

    private static string Batch(params (Guid Id, string PayloadStore)[] events) =>
        JsonSerializer.Serialize(new
        {
            events = events.Select(e => new
            {
                eventId = e.Id, storeId = 0, eventType = "InvoiceCompleted",
                payload = $$"""{"StoreId":"{{e.PayloadStore}}"}""", createdAtUtc = DateTime.UtcNow
            })
        });

    private static async Task<List<CloudSyncRejection>> RejectionsAsync(string connectionString)
    {
        await using var db = CloudPostgresFixture.CreateContext(connectionString);
        return await db.SyncRejections.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task SyncEvents_ForAnotherStore_LogsARejectionPerEvent()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;

        await host.PostEventsAsStoreAsync(ThisStore, Batch((Guid.NewGuid(), "store-b"), (Guid.NewGuid(), "store-b")));

        var rejections = await RejectionsAsync(connectionString);
        rejections.Should().HaveCount(2)
                  .And.OnlyContain(r => r.StoreId == ThisStore && r.StatusCode == 403 && r.Reason.Contains("store-mismatch"));
    }

    [Fact]
    public async Task SyncEvents_RefusedTwice_CountsTwiceInOneRow()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var eventId = Guid.NewGuid();

        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, "store-b")));
        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, "store-b")));

        (await RejectionsAsync(connectionString)).Should().ContainSingle()
                                                 .Which.Count.Should().Be(2);
    }

    [Fact]
    public async Task SyncEvents_WithoutAnEventsList_LogsOneBatchRejection()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;

        await host.PostEventsAsStoreAsync(ThisStore, "{}");

        var rejection = (await RejectionsAsync(connectionString)).Should().ContainSingle().Subject;
        rejection.EventId.Should().BeNull();
        rejection.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SyncEvents_AcceptingARejectedEvent_ResolvesItsRejection()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var eventId = Guid.NewGuid();
        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, "store-b")));

        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, ThisStore)));

        (await RejectionsAsync(connectionString)).Single().ResolvedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncEvents_AcceptingADuplicate_ResolvesItsRejection()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var eventId = Guid.NewGuid();
        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, ThisStore)));
        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, "store-b")));

        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, ThisStore)));

        (await RejectionsAsync(connectionString)).Single().ResolvedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncEvents_WhenTheLogCannotBeWritten_StillReturnsTheRefusal()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand("""drop table "SyncRejections" """, connection);
            await drop.ExecuteNonQueryAsync();
        }

        var response = await host.PostEventsAsStoreAsync(ThisStore, Batch((Guid.NewGuid(), "store-b")));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().Contain("store-mismatch");
    }

    // Checked against the database before anything was saved, the second copy added a second row, the unique
    // index refused the save, and the whole batch went unlogged.
    [Fact]
    public async Task SyncEvents_WithTheSameEventTwiceInARefusedBatch_LogsItOnce()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        var eventId = Guid.NewGuid();

        await host.PostEventsAsStoreAsync(ThisStore, Batch((eventId, "store-b"), (eventId, "store-b")));

        (await RejectionsAsync(connectionString)).Should().ContainSingle(r => r.EventId == eventId);
    }

    [Fact]
    public async Task SyncEvents_WhenTheLogCannotBeWritten_StillAcceptsTheEvent()
    {
        var (host, connectionString) = await StartAsync();
        await using var _ = host;
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand("""drop table "SyncRejections" """, connection);
            await drop.ExecuteNonQueryAsync();
        }

        var response = await host.PostEventsAsStoreAsync(ThisStore, Batch((Guid.NewGuid(), ThisStore)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"accepted\":true");
    }
}
