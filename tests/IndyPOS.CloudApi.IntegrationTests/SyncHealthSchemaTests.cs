using FluentAssertions;
using Npgsql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The store sync health tables: one heartbeat row per store, one rejection row per store and event, and
/// resend requests. Like the mirrors, they are read through the schema alone, so every table and column
/// carries a comment.
/// </summary>
public class SyncHealthSchemaTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    public static readonly TheoryData<string> SyncHealthTables = new() { "StoreHeartbeats", "SyncRejections", "SyncResendRequests" };

    [Fact]
    public async Task StoreHeartbeats_AfterMigrating_HasOneRowPerStore()
    {
        await using var connection = await OpenAsync();
        await InsertHeartbeatAsync(connection, "store-a");

        var second = () => InsertHeartbeatAsync(connection, "store-a");

        await second.Should()
                    .ThrowAsync<PostgresException>();
    }

    [Fact]
    public async Task SyncRejections_AfterMigrating_AllowsOneRowPerStoreAndEvent()
    {
        await using var connection = await OpenAsync();
        var eventId = Guid.NewGuid();
        await InsertRejectionAsync(connection, "store-a", eventId);

        var second = () => InsertRejectionAsync(connection, "store-a", eventId);

        await second.Should()
                    .ThrowAsync<PostgresException>();
    }

    // A batch refusal names no event, and each one is its own row.
    [Fact]
    public async Task SyncRejections_AfterMigrating_AllowsSeveralBatchRejections()
    {
        await using var connection = await OpenAsync();
        await InsertRejectionAsync(connection, "store-a", eventId: null);

        var second = () => InsertRejectionAsync(connection, "store-a", eventId: null);

        await second.Should()
                    .NotThrowAsync();
    }

    [Theory]
    [MemberData(nameof(SyncHealthTables))]
    public async Task SyncHealthTables_ForEveryColumn_HaveAComment(string table)
    {
        await using var connection = await OpenAsync();

        // One row per column, so a missing table fails instead of passing on an empty list.
        var comments = await QueryAsync(connection,
            """
            select coalesce(col_description(format('%I', @t)::regclass, c.ordinal_position), '')
            from information_schema.columns c
            where c.table_name = @t
            """, table);

        comments.Should()
                .NotBeEmpty()
                .And.NotContain(string.Empty);
    }

    [Theory]
    [MemberData(nameof(SyncHealthTables))]
    public async Task SyncHealthTables_ForEveryTable_HaveAComment(string table)
    {
        await using var connection = await OpenAsync();

        var comment = await QueryAsync(connection, "select coalesce(obj_description(format('%I', @t)::regclass, 'pg_class'), '')", table);

        comment.Single().Should()
                        .NotBeEmpty();
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(await postgres.CreateDatabaseAsync());
        await connection.OpenAsync();
        return connection;
    }

    private static async Task InsertHeartbeatAsync(NpgsqlConnection connection, string storeId)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into "StoreHeartbeats" ("StoreId", "ReceivedAtUtc", "PendingCount", "FailedCount")
            values (@s, now(), 0, 0)
            """, connection);
        command.Parameters.AddWithValue("s", storeId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertRejectionAsync(NpgsqlConnection connection, string storeId, Guid? eventId)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into "SyncRejections" ("Id", "StoreId", "EventId", "StatusCode", "Reason", "FirstRejectedAtUtc", "LastRejectedAtUtc", "Count")
            values (gen_random_uuid(), @s, @e, 403, 'store-mismatch', now(), now(), 1)
            """, connection);
        command.Parameters.AddWithValue("s", storeId);
        command.Parameters.AddWithValue("e", eventId is { } id ? id : DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryAsync(NpgsqlConnection connection, string sql, string parameter)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("t", parameter);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows;
    }
}
