using System.Data.SQLite;
using Dapper;
using IndyPOS.Application.Common.Constants;
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

        // Verify payments PER METHOD, not just in total. Counts and revenue both reconcile
        // perfectly when the method mapping is scrambled, which is exactly how a mapping that
        // filed PayLater as "Card" and bank transfers as "WelfareCard" survived: ~15% of
        // turnover attributed to the wrong method, with every aggregate check still green.
        await VerifyPaymentsByMethodAsync(sqliteConnection, context, result, ct);

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

    /// <summary>
    /// Compares payment count and amount per catalogue method. Expectations are derived from the
    /// legacy table through <see cref="LegacyPaymentTypeMap"/> - the same map the migration wrote
    /// with, so a mis-mapping shows up as a mismatch on both sides rather than cancelling out.
    /// </summary>
    private async Task VerifyPaymentsByMethodAsync(
        SQLiteConnection sqlite, StoreHubDbContext context, VerificationResult result, CancellationToken ct)
    {
        var legacyGroups = await sqlite.QueryAsync<(long PaymentTypeId, int Count, decimal Total)>(
            "SELECT PaymentTypeId, COUNT(*) AS Count, COALESCE(SUM(Amount), 0) AS Total " +
            "FROM Payment GROUP BY PaymentTypeId");

        var expected = new Dictionary<string, (int Count, decimal Total)>();

        foreach (var group in legacyGroups)
        {
            var code = LegacyPaymentTypeMap.ToCode((int)group.PaymentTypeId);
            if (code is null)
            {
                // Unmapped ids are a verification failure in their own right: the migration
                // refused those rows, so the money is in SQLite and nowhere else.
                result.Errors.Add(
                    $"Legacy PaymentTypeId {group.PaymentTypeId} has no payment-method code, so " +
                    $"{group.Count} payment(s) totalling {group.Total:N2} were not migrated.");
                result.Checks.Add(new VerificationCheck(
                    $"Payments [legacy type {group.PaymentTypeId}]", group.Count, 0, false));
                continue;
            }

            var current = expected.GetValueOrDefault(code);
            expected[code] = (current.Count + group.Count, current.Total + group.Total);
        }

        var actual = (await context.Payments
                .GroupBy(p => p.Method)
                .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .ToListAsync(ct))
            .ToDictionary(x => x.Method, x => (x.Count, x.Total));

        // PayLater is migrated from its own legacy table too, so PostgreSQL legitimately holds
        // MORE PayLater payments than the legacy Payment table alone accounts for.
        foreach (var (code, want) in expected)
        {
            var got = actual.GetValueOrDefault(code);
            var countOk = code == PaymentMethodCodes.PayLater ? got.Count >= want.Count : got.Count == want.Count;
            var totalOk = code == PaymentMethodCodes.PayLater
                ? got.Total >= want.Total - 1.00m
                : Math.Abs(got.Total - want.Total) < 1.00m;

            result.Checks.Add(new VerificationCheck($"Payments [{code}]", want.Count, got.Count, countOk && totalOk));

            if (!countOk || !totalOk)
            {
                result.Errors.Add(
                    $"Payment method {code}: SQLite has {want.Count} totalling {want.Total:N2}, " +
                    $"PostgreSQL has {got.Count} totalling {got.Total:N2}.");
            }
        }

        // A method PostgreSQL knows about but the legacy data never had means the migration
        // invented an attribution - the failure mode that produced "Card" and "Other".
        foreach (var (code, got) in actual.Where(a => !expected.ContainsKey(a.Key)))
        {
            result.Errors.Add(
                $"Payment method {code} exists in PostgreSQL ({got.Count} payment(s) totalling " +
                $"{got.Total:N2}) but no legacy payment type maps to it.");
            result.Checks.Add(new VerificationCheck($"Payments [{code}] unexpected", 0, got.Count, false));
        }
    }
}

public class VerificationResult
{
    public List<VerificationCheck> Checks { get; } = [];
    public List<string> Errors { get; } = [];
    public bool IsValid => Checks.All(c => c.IsValid) && Errors.Count == 0;
}

public record VerificationCheck(string EntityName, int SqliteCount, int PostgresCount, bool IsValid);
