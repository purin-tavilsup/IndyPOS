using System.Data.SQLite;
using Dapper;

namespace IndyPOS.MigrationTool.Tests.TestData;

/// <summary>
/// Creates a lite test database from the real Store.db with a subset of data.
/// </summary>
public static class CreateTestDatabase
{
    private const string SourceDbPath = @"C:\personal\IndyPOS\.planning\indypos-overhaul\sqlite_database\Store.db";
    private const string TestDbPath = @"C:\personal\IndyPOS\tests\IndyPOS.MigrationTool.Tests\TestData\Store_test.db";

    /// <summary>
    /// Run this manually to create a test database with sample data.
    /// </summary>
    public static async Task CreateAsync()
    {
        if (!File.Exists(SourceDbPath))
        {
            Console.WriteLine($"Source database not found: {SourceDbPath}");
            return;
        }

        // Delete existing test db
        if (File.Exists(TestDbPath))
        {
            File.Delete(TestDbPath);
        }

        // Create new test database
        await using var testConn = new SQLiteConnection($"Data Source={TestDbPath};Version=3;");
        await testConn.OpenAsync();

        await using var sourceConn = new SQLiteConnection($"Data Source={SourceDbPath};Version=3;");
        await sourceConn.OpenAsync();

        Console.WriteLine("Creating test database schema...");

        // Get schema from source
        var schema = await sourceConn.QueryAsync<TableSchema>("""
            SELECT name, sql FROM sqlite_master
            WHERE type='table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """);

        foreach (var table in schema)
        {
            if (!string.IsNullOrEmpty(table.Sql))
            {
                await testConn.ExecuteAsync(table.Sql);
                Console.WriteLine($"  Created table: {table.Name}");
            }
        }

        // Copy limited data
        Console.WriteLine("\nCopying sample data...");

        // Copy all users (usually small)
        var userCount = await CopyTableAsync(sourceConn, testConn, "User", limit: 10);
        Console.WriteLine($"  Users: {userCount}");

        // Copy user credentials
        var credCount = await CopyTableAsync(sourceConn, testConn, "UserCredential", limit: 10);
        Console.WriteLine($"  UserCredentials: {credCount}");

        // Copy sample products (50)
        var productCount = await CopyTableAsync(sourceConn, testConn, "InventoryProduct", limit: 50);
        Console.WriteLine($"  Products: {productCount}");

        // Copy recent invoices (100)
        var invoiceCount = await CopyTableLimitedAsync(sourceConn, testConn, "Invoice",
            "SELECT * FROM Invoice ORDER BY InvoiceId DESC LIMIT 100");
        Console.WriteLine($"  Invoices: {invoiceCount}");

        // Get invoice IDs we copied
        var invoiceIds = await testConn.QueryAsync<int>("SELECT InvoiceId FROM Invoice");
        var invoiceIdList = string.Join(",", invoiceIds);

        // Copy invoice products for those invoices
        if (!string.IsNullOrEmpty(invoiceIdList))
        {
            var lineCount = await CopyTableLimitedAsync(sourceConn, testConn, "InvoiceProduct",
                $"SELECT * FROM InvoiceProduct WHERE InvoiceId IN ({invoiceIdList})");
            Console.WriteLine($"  InvoiceProducts: {lineCount}");

            // Copy payments for those invoices
            var paymentCount = await CopyTableLimitedAsync(sourceConn, testConn, "Payment",
                $"SELECT * FROM Payment WHERE InvoiceId IN ({invoiceIdList})");
            Console.WriteLine($"  Payments: {paymentCount}");

            // Copy PayLater for those invoices
            var payLaterCount = await CopyTableLimitedAsync(sourceConn, testConn, "PayLater",
                $"SELECT * FROM PayLater WHERE InvoiceId IN ({invoiceIdList})");
            Console.WriteLine($"  PayLater: {payLaterCount}");
        }

        // Get file size
        var fileInfo = new FileInfo(TestDbPath);
        Console.WriteLine($"\nTest database created: {TestDbPath}");
        Console.WriteLine($"Size: {fileInfo.Length / 1024.0:F1} KB");
    }

    private static async Task<int> CopyTableAsync(SQLiteConnection source, SQLiteConnection target, string tableName, int limit)
    {
        var data = await source.QueryAsync($"SELECT * FROM [{tableName}] LIMIT {limit}");
        var count = 0;

        foreach (var row in data)
        {
            var dict = (IDictionary<string, object>)row;
            var columns = string.Join(", ", dict.Keys.Select(k => $"[{k}]"));
            var values = string.Join(", ", dict.Keys.Select(k => $"@{k}"));

            await target.ExecuteAsync($"INSERT INTO [{tableName}] ({columns}) VALUES ({values})", (object)row);
            count++;
        }

        return count;
    }

    private static async Task<int> CopyTableLimitedAsync(SQLiteConnection source, SQLiteConnection target, string tableName, string query)
    {
        var data = await source.QueryAsync(query);
        var count = 0;

        foreach (var row in data)
        {
            var dict = (IDictionary<string, object>)row;
            var columns = string.Join(", ", dict.Keys.Select(k => $"[{k}]"));
            var values = string.Join(", ", dict.Keys.Select(k => $"@{k}"));

            await target.ExecuteAsync($"INSERT INTO [{tableName}] ({columns}) VALUES ({values})", (object)row);
            count++;
        }

        return count;
    }

    private class TableSchema
    {
        public string Name { get; set; } = "";
        public string? Sql { get; set; }
    }
}
