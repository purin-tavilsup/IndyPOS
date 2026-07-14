using System.Data.SQLite;
using Dapper;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndyPOS.MigrationTool.Services;

public class MigrationVerifier
{
    private readonly MigrationOptions _options;
    private readonly ILogger<MigrationVerifier> _logger;

    public MigrationVerifier(MigrationOptions options, ILogger<MigrationVerifier> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyAsync(CancellationToken ct = default)
    {
        var result = new VerificationResult();

        await using var sqliteConnection = new SQLiteConnection($"Data Source={_options.SqlitePath};Version=3;");
        await sqliteConnection.OpenAsync(ct);

        var dbOptions = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(_options.PostgresConnectionString)
            .Options;

        await using var context = new StoreHubDbContext(dbOptions);

        // Verify Users
        var sqliteUsers = await sqliteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM User WHERE UserId IN (SELECT UserId FROM UserCredential)");
        var pgUsers = await context.StoreUsers.CountAsync(u => u.StoreId == _options.StoreId, ct);
        result.Checks.Add(new VerificationCheck("Users", sqliteUsers, pgUsers, sqliteUsers <= pgUsers));

        // Verify Products
        var sqliteProducts = await sqliteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM InventoryProduct");
        var pgProducts = await context.Products.CountAsync(ct);
        result.Checks.Add(new VerificationCheck("Products", sqliteProducts, pgProducts, sqliteProducts <= pgProducts));

        // Verify Invoices
        var sqliteInvoices = await sqliteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Invoice");
        var pgInvoices = await context.Invoices.CountAsync(i => i.StoreId == _options.StoreId, ct);
        result.Checks.Add(new VerificationCheck("Invoices", sqliteInvoices, pgInvoices, sqliteInvoices <= pgInvoices));

        // Verify Invoice Lines
        var sqliteLines = await sqliteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM InvoiceProduct");
        var pgLines = await context.InvoiceLines.CountAsync(ct);
        result.Checks.Add(new VerificationCheck("Invoice Lines", sqliteLines, pgLines, sqliteLines <= pgLines));

        // Verify Payments
        var sqlitePayments = await sqliteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Payment");
        var pgPayments = await context.Payments.CountAsync(ct);
        result.Checks.Add(new VerificationCheck("Payments", sqlitePayments, pgPayments, sqlitePayments <= pgPayments));

        // Verify PayLater
        var sqlitePayLater = await sqliteConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM PayLater");
        var pgPayLater = await context.PayLaters.CountAsync(ct);
        result.Checks.Add(new VerificationCheck("PayLater", sqlitePayLater, pgPayLater, sqlitePayLater <= pgPayLater));

        // Verify total amounts match (within tolerance)
        var sqliteTotalAmount = await sqliteConnection.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(Total), 0) FROM Invoice");
        var pgTotalAmount = await context.Invoices
            .Where(i => i.StoreId == _options.StoreId)
            .SumAsync(i => i.TotalAmount, ct);

        var amountDifference = Math.Abs(sqliteTotalAmount - pgTotalAmount);
        var isAmountValid = amountDifference < 1.00m; // Allow $1 tolerance for decimal precision differences
        result.Checks.Add(new VerificationCheck(
            "Total Revenue",
            (int)sqliteTotalAmount,
            (int)pgTotalAmount,
            isAmountValid));

        if (!isAmountValid)
        {
            result.Errors.Add($"Revenue mismatch: SQLite={sqliteTotalAmount:C}, PostgreSQL={pgTotalAmount:C}, Diff={amountDifference:C}");
        }

        // Add errors for failed checks
        foreach (var check in result.Checks.Where(c => !c.IsValid))
        {
            if (check.EntityName != "Total Revenue")
            {
                result.Errors.Add($"{check.EntityName}: SQLite has {check.SqliteCount}, PostgreSQL has {check.PostgresCount}");
            }
        }

        _logger.LogInformation("Verification completed. Valid: {IsValid}", result.IsValid);
        return result;
    }
}

public class VerificationResult
{
    public List<VerificationCheck> Checks { get; } = [];
    public List<string> Errors { get; } = [];
    public bool IsValid => Checks.All(c => c.IsValid) && Errors.Count == 0;
}

public record VerificationCheck(string EntityName, int SqliteCount, int PostgresCount, bool IsValid);
