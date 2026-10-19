using FluentAssertions;
using Npgsql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The ลงบัญชี mirror is read by dashboards and AI agents through the schema alone (spec 2026-10-19 §3), so
/// every table and column carries a Postgres comment, and the figures an agent would otherwise compute
/// (what is still owed, whether a debt came from v3) are columns the database derives.
/// </summary>
public class PayLaterMirrorSchemaTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    public static readonly TheoryData<string> PayLaterTables = new() { "PayLaters", "PayLaterRepayments" };

    private const decimal DebtAmount = 700m;
    private const decimal PartPaid = 299m;
    private const int V3PaymentId = 4242;

    [Theory]
    [MemberData(nameof(PayLaterTables))]
    public async Task PayLaterMirror_ForEveryColumn_HasAComment(string table)
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
    [MemberData(nameof(PayLaterTables))]
    public async Task PayLaterMirror_ForEveryTable_HasAComment(string table)
    {
        await using var connection = await OpenAsync();

        var comment = await QueryAsync(connection, "select coalesce(obj_description(format('%I', @t)::regclass, 'pg_class'), '')", table);

        comment.Single().Should()
                        .NotBeEmpty();
    }

    [Fact]
    public async Task PayLaterMirror_WithAPartPaidDebt_ComputesOwedAmount()
    {
        await using var connection = await OpenAsync();
        var id = await InsertDebtAsync(connection, legacyPaymentId: null);

        var owed = await QueryAsync(connection, """select "OwedAmount"::text from "PayLaters" where "Id" = @t::uuid""", id.ToString());

        decimal.Parse(owed.Single()).Should()
                                    .Be(DebtAmount - PartPaid);
    }

    [Fact]
    public async Task PayLaterMirror_WithALegacyPaymentId_ComputesIsFromV3()
    {
        await using var connection = await OpenAsync();
        var id = await InsertDebtAsync(connection, V3PaymentId);

        var fromV3 = await QueryAsync(connection, """select "IsFromV3"::text from "PayLaters" where "Id" = @t::uuid""", id.ToString());

        fromV3.Should()
              .Equal("true");
    }

    [Fact]
    public async Task PayLaterMirror_WithoutALegacyPaymentId_ComputesIsFromV3False()
    {
        await using var connection = await OpenAsync();
        var id = await InsertDebtAsync(connection, legacyPaymentId: null);

        var fromV3 = await QueryAsync(connection, """select "IsFromV3"::text from "PayLaters" where "Id" = @t::uuid""", id.ToString());

        fromV3.Should()
              .Equal("false");
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(await postgres.CreateDatabaseAsync());
        await connection.OpenAsync();
        return connection;
    }

    // Names only the columns StoreHub sends; the derived ones are the database's to fill.
    private static async Task<Guid> InsertDebtAsync(NpgsqlConnection connection, int? legacyPaymentId)
    {
        var id = Guid.NewGuid();
        await using var command = new NpgsqlCommand(
            """
            insert into "PayLaters" ("Id", "StoreId", "InvoiceId", "PaymentId", "CustomerName", "PayLaterAmount", "PaidAmount",
                                     "IsCompleted", "LegacyPaymentId", "CreatedAtUtc", "LastModifiedAtUtc", "SyncedAtUtc")
            values (@id, '1', gen_random_uuid(), gen_random_uuid(), 'ลุงสมชาย', @amount, @paid, false, @legacy, now(), now(), now())
            """, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("amount", DebtAmount);
        command.Parameters.AddWithValue("paid", PartPaid);
        command.Parameters.AddWithValue("legacy", (object?)legacyPaymentId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
        return id;
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
