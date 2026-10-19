using FluentAssertions;
using Npgsql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// The cash mirror is read by dashboards and AI agents through the schema alone (spec 2026-09-20 §7),
/// so every table and column must carry a Postgres comment.
/// </summary>
public class CashDrawerMirrorSchemaTests(CloudPostgresFixture postgres) : IClassFixture<CloudPostgresFixture>
{
    public static readonly TheoryData<string> CashTables = new() { "CashPayouts", "CashFloats", "DebtRepayments", "CashCounts" };

    [Theory]
    [MemberData(nameof(CashTables))]
    public async Task CashDrawerMirror_ForEveryColumn_HasAComment(string table)
    {
        // One row per column, so a missing table fails instead of passing on an empty list.
        var comments = await QueryAsync(
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
    [MemberData(nameof(CashTables))]
    public async Task CashDrawerMirror_ForEveryTable_HasAComment(string table)
    {
        var comment = await QueryAsync("select coalesce(obj_description(format('%I', @t)::regclass, 'pg_class'), '')", table);

        comment.Single().Should()
                        .NotBeEmpty();
    }

    [Fact]
    public async Task CashDrawerMirror_ForCashCounts_HasNoCountedTotalColumn()
    {
        var columns = await QueryAsync("select column_name from information_schema.columns where table_name = @t", "CashCounts");

        // Contain first, so a missing table fails instead of passing.
        columns.Should()
               .Contain("Coin1Count")
               .And.NotContain("CountedTotal");
    }

    [Fact]
    public async Task CashDrawerMirror_ForCashPayouts_StoresCategoryAsText()
    {
        var type = await QueryAsync(
            "select data_type from information_schema.columns where table_name = @t and column_name = 'Category'", "CashPayouts");

        type.Should()
            .Equal("character varying");
    }

    private async Task<List<string>> QueryAsync(string sql, string table)
    {
        await using var connection = new NpgsqlConnection(await postgres.CreateDatabaseAsync());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("t", table);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows;
    }
}
