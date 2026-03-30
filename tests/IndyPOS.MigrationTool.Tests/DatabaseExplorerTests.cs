using System.Data.SQLite;
using Dapper;

namespace IndyPOS.MigrationTool.Tests;

public class DatabaseExplorerTests
{
    private const string RealDbPath = @"C:\personal\IndyPOS\.planning\indypos-overhaul\sqlite_database\Store.db";

    [Fact]
    public async Task Explore_RealDatabase_ShowsSchema()
    {
        if (!File.Exists(RealDbPath))
        {
            Assert.Fail($"Database not found at {RealDbPath}");
            return;
        }

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        // Get all tables
        var tables = (await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")).ToList();

        Console.WriteLine("=== TABLES ===");
        foreach (var table in tables)
        {
            Console.WriteLine($"  {table}");
        }

        Console.WriteLine("\n=== RECORD COUNTS ===");
        foreach (var table in tables)
        {
            try
            {
                var count = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM [{table}]");
                Console.WriteLine($"  {table}: {count:N0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {table}: ERROR - {ex.Message}");
            }
        }

        // Show sample data from key tables
        Console.WriteLine("\n=== SAMPLE USER ===");
        var user = await conn.QueryFirstOrDefaultAsync("SELECT * FROM User LIMIT 1");
        if (user != null)
        {
            foreach (var prop in (IDictionary<string, object>)user)
            {
                Console.WriteLine($"  {prop.Key}: {prop.Value}");
            }
        }

        Console.WriteLine("\n=== SAMPLE PRODUCT ===");
        var product = await conn.QueryFirstOrDefaultAsync("SELECT * FROM InventoryProduct LIMIT 1");
        if (product != null)
        {
            foreach (var prop in (IDictionary<string, object>)product)
            {
                Console.WriteLine($"  {prop.Key}: {prop.Value}");
            }
        }
    }

    [Fact]
    public void RealDatabase_Exists()
    {
        // This test just verifies the real database exists for reference
        var exists = File.Exists(RealDbPath);
        Assert.True(exists, $"Sample database should exist at {RealDbPath}");
    }
}
