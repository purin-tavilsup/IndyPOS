# PowerShell script to create test database from real Store.db
# Run from the project root: .\tests\IndyPOS.MigrationTool.Tests\create_test_db.ps1

$ErrorActionPreference = "Stop"

$sourceDb = "C:\personal\IndyPOS\.planning\indypos-overhaul\sqlite_database\Store.db"
$targetDb = "C:\personal\IndyPOS\tests\IndyPOS.MigrationTool.Tests\TestData\Store_test.db"

if (-not (Test-Path $sourceDb)) {
    Write-Error "Source database not found: $sourceDb"
    exit 1
}

# Ensure target directory exists
$targetDir = Split-Path $targetDb -Parent
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

# Remove existing test db
if (Test-Path $targetDb) {
    Remove-Item $targetDb -Force
}

# Build and run the generator
Push-Location "C:\personal\IndyPOS"

# Create a simple console app to generate the test db
$code = @"
using System.Data.SQLite;
using Dapper;

var sourceDb = @"$sourceDb";
var targetDb = @"$targetDb";

Console.WriteLine("Creating test database...");

using var testConn = new SQLiteConnection(`$"Data Source={targetDb};Version=3;");
testConn.Open();

using var sourceConn = new SQLiteConnection(`$"Data Source={sourceDb};Version=3;");
sourceConn.Open();

// Copy schema
var tables = sourceConn.Query<string>("SELECT sql FROM sqlite_master WHERE type='table' AND sql IS NOT NULL");
foreach (var sql in tables) {
    testConn.Execute(sql);
}

// Copy limited data
CopyTable("User", 10);
CopyTable("UserCredential", 10);
CopyTable("InventoryProduct", 50);

// Get recent invoices
var invoices = sourceConn.Query<int>("SELECT InvoiceId FROM Invoice ORDER BY InvoiceId DESC LIMIT 100");
var idList = string.Join(",", invoices);

if (!string.IsNullOrEmpty(idList)) {
    CopyWithFilter("Invoice", `$"InvoiceId IN ({idList})");
    CopyWithFilter("InvoiceProduct", `$"InvoiceId IN ({idList})");
    CopyWithFilter("Payment", `$"InvoiceId IN ({idList})");
    CopyWithFilter("PayLater", `$"InvoiceId IN ({idList})");
}

Console.WriteLine(`$"Test database created: {targetDb}");
Console.WriteLine(`$"Size: {new FileInfo(targetDb).Length / 1024.0:F1} KB");

void CopyTable(string table, int limit) {
    var data = sourceConn.Query(`$"SELECT * FROM [{table}] LIMIT {limit}");
    foreach (dynamic row in data) {
        var dict = (IDictionary<string, object>)row;
        var cols = string.Join(", ", dict.Keys.Select(k => `$"[{k}]"));
        var vals = string.Join(", ", dict.Keys.Select(k => `$"@{k}"));
        testConn.Execute(`$"INSERT INTO [{table}] ({cols}) VALUES ({vals})", row);
    }
    Console.WriteLine(`$"  {table}: {data.Count()} rows");
}

void CopyWithFilter(string table, string filter) {
    try {
        var data = sourceConn.Query(`$"SELECT * FROM [{table}] WHERE {filter}");
        foreach (dynamic row in data) {
            var dict = (IDictionary<string, object>)row;
            var cols = string.Join(", ", dict.Keys.Select(k => `$"[{k}]"));
            var vals = string.Join(", ", dict.Keys.Select(k => `$"@{k}"));
            testConn.Execute(`$"INSERT INTO [{table}] ({cols}) VALUES ({vals})", row);
        }
        Console.WriteLine(`$"  {table}: {data.Count()} rows");
    } catch { Console.WriteLine(`$"  {table}: skipped (table may not exist)"); }
}
"@

Write-Host "Use the C# test runner instead - run unskipped test manually"
Pop-Location
