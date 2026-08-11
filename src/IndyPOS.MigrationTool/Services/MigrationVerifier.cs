using System.Data.SQLite;
using Dapper;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndyPOS.MigrationTool.Services;

public class MigrationVerifier
{
    private const string StockCheckName = "Stock (units)";

    /// <summary>Shown in place of the PayLater row for a store that does not have that feature.</summary>
    private const string NoPayLaterTableCheckName = "PayLater (no legacy table)";

    /// <summary>A store can have thousands of products; the count stays exact, the listing does not.</summary>
    private const int MaxStockMismatchesReported = 10;

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
        await VerifyPayLaterAsync(sqliteConnection, context, result, ct);

        // Verify payments PER METHOD, not just in total. Counts and revenue both reconcile
        // perfectly when the method mapping is scrambled, which is exactly how a mapping that
        // filed PayLater as "Card" and bank transfers as "WelfareCard" survived: ~15% of
        // turnover attributed to the wrong method, with every aggregate check still green.
        await VerifyPaymentsByMethodAsync(sqliteConnection, context, result, ct);

        // Verify STOCK per product. Every other check here is a count or a total, and defect 14 --
        // which left GeneralHardware at -400,541 units, 66% of products negative -- passed all of
        // them. Stock was the one thing nothing looked at.
        await VerifyStockAsync(sqliteConnection, context, result, ct);

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

        // Add errors for failed checks. Total Revenue and Stock report their own richer errors --
        // a bare "SQLite has X, PostgreSQL has Y" would restate them less usefully.
        foreach (var check in result.Checks.Where(c => !c.IsValid))
        {
            if (check.EntityName is not ("Total Revenue" or StockCheckName))
            {
                result.Errors.Add($"{check.EntityName}: SQLite has {check.SqliteCount}, PostgreSQL has {check.PostgresCount}");
            }
        }

        _logger.LogInformation("Verification completed. Valid: {IsValid}", result.IsValid);
        return result;
    }

    /// <summary>
    /// Compares legacy <c>QuantityInStock</c> against <c>SUM(QuantityDelta)</c> of the movements the
    /// migration wrote, PER PRODUCT. Per product matters: defect 14 subtracted every sold unit a
    /// second time, and a store-wide total would have to be compared against a number nothing else
    /// computes, so the error would still hide.
    /// </summary>
    /// <remarks>
    /// It sums EVERY movement for the product, deliberately not just the migration's own
    /// <c>Migration:InitialStock</c> rows. Filtering by reason was tried and rejected: defect 14's
    /// harm was the EXTRA <c>Migration:Sale</c> rows, so a reason-filtered sum ignores exactly the
    /// rows that made stock wrong and reports green. Stock is <c>SUM(QuantityDelta)</c>
    /// (<c>InventoryMovement.cs:6</c>) and this must check the same number the till displays.
    ///
    /// The consequence is that this check is only meaningful immediately after a migration -- once
    /// the till starts selling, real movements legitimately move stock away from the legacy figure.
    ///
    /// Negative legacy stock is expected as ZERO, matching the migrator's clamp (defect 14b).
    /// </remarks>
    private async Task VerifyStockAsync(
        SQLiteConnection sqlite, StoreHubDbContext context, VerificationResult result, CancellationToken ct)
    {
        var expected = (await sqlite.QueryAsync<(string Barcode, long Quantity)>(
                "SELECT Barcode, MAX(QuantityInStock, 0) AS Quantity FROM InventoryProduct"))
            .ToDictionary(row => row.Barcode, row => (int)row.Quantity);

        var actual = (await context.Products
                .Where(p => p.StoreId == _options.StoreId)
                .Select(p => new
                {
                    p.Barcode,
                    Quantity = p.InventoryMovements.Sum(m => (int?)m.QuantityDelta) ?? 0
                })
                .ToListAsync(ct))
            .ToDictionary(row => row.Barcode, row => row.Quantity);

        var mismatches = new List<string>();
        var expectedUnits = 0;
        var actualUnits = 0;

        foreach (var (barcode, want) in expected)
        {
            // A product missing from PostgreSQL scores 0 rather than being skipped: "not migrated"
            // is a stock failure too, not an absence of evidence.
            var got = actual.GetValueOrDefault(barcode);
            expectedUnits += want;
            actualUnits += got;

            if (got != want)
            {
                mismatches.Add($"{barcode}: expected {want}, migrated {got}");
            }
        }

        result.Checks.Add(new VerificationCheck(
            StockCheckName, expectedUnits, actualUnits, mismatches.Count == 0));

        if (mismatches.Count == 0)
        {
            return;
        }

        result.Errors.Add(
            $"Stock mismatch on {mismatches.Count} product(s). Legacy QuantityInStock must equal " +
            "SUM(QuantityDelta) after migration, and negative legacy stock is expected as 0.");

        foreach (var mismatch in mismatches.Take(MaxStockMismatchesReported))
        {
            result.Errors.Add($"  {mismatch}");
        }

        if (mismatches.Count > MaxStockMismatchesReported)
        {
            result.Errors.Add($"  ... and {mismatches.Count - MaxStockMismatchesReported} more");
        }
    }

    /// <summary>
    /// Compares PayLater counts, tolerating stores that have no such table.
    ///
    /// Defect 15. PayLater is a GeneralHardware-only feature; MimyMart and MimyShop have no such
    /// table. Querying it unconditionally threw "no such table: PayLater" before any later check
    /// ran, so on 2 of the 3 real stores the per-product stock check added for defect 14 - the one
    /// thing that proves migrated stock is right - was never reachable. Same shape as defect 3,
    /// which was only ever fixed in the migrator.
    /// </summary>
    private async Task VerifyPayLaterAsync(
        SQLiteConnection sqlite, StoreHubDbContext context, VerificationResult result, CancellationToken ct)
    {
        var pgPayLater = await context.PayLaters.CountAsync(ct);

        if (!await HasTableAsync(sqlite, "PayLater"))
        {
            _logger.LogInformation(
                "No PayLater table in this store; skipping the count check. " +
                "PayLater is a GeneralHardware-only feature.");

            // Reported rather than skipped silently: a check that simply vanishes from the results
            // table is indistinguishable from one that was never written, which is the class of
            // blind spot that let defect 14 ship.
            result.Checks.Add(new VerificationCheck(NoPayLaterTableCheckName, 0, pgPayLater, true));
            return;
        }

        var sqlitePayLater = await sqlite.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PayLater");
        result.Checks.Add(new VerificationCheck("PayLater", sqlitePayLater, pgPayLater, sqlitePayLater <= pgPayLater));
    }

    private static async Task<bool> HasTableAsync(SQLiteConnection sqlite, string tableName)
    {
        return await sqlite.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName",
            new { tableName }) > 0;
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
