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

    private const string CategoryCheckName = "Categories";

    /// <summary>Shown in place of the PayLater row for a store that does not have that feature.</summary>
    private const string NoPayLaterTableCheckName = "PayLater (no legacy table)";

    /// <summary>A store can have thousands of products; the count stays exact, the listing does not.</summary>
    private const int MaxMismatchesReported = 10;

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

        // Verify the CATEGORY VALUE per product. Every check above is a count or a total, and all
        // of them are green while Category holds the raw legacy id -- which is how defect 5 lasted
        // as long as it did. This is the only check that looks at what was actually written.
        await VerifyCategoriesAsync(sqliteConnection, context, result, ct);

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
            if (check.EntityName is not ("Total Revenue" or StockCheckName or CategoryCheckName))
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

        foreach (var mismatch in mismatches.Take(MaxMismatchesReported))
        {
            result.Errors.Add($"  {mismatch}");
        }

        if (mismatches.Count > MaxMismatchesReported)
        {
            result.Errors.Add($"  ... and {mismatches.Count - MaxMismatchesReported} more");
        }
    }

    /// <summary>
    /// Compares the <c>Category</c> written against the code the legacy row resolves to, PER
    /// PRODUCT. The counts are of CATEGORISED products, so the row also reads as coverage.
    /// </summary>
    /// <remarks>
    /// Expectations come from <see cref="LegacyCategoryResolver"/> -- the same lookup and map the
    /// migration read, exactly as <see cref="VerifyPaymentsByMethodAsync"/> shares
    /// <see cref="LegacyPaymentTypeMap"/>. Re-deriving them independently would mean a second
    /// mapping to keep in step, and the two drifting is the failure this is meant to detect.
    ///
    /// A legacy category the catalogue does not know is expected as NULL, matching what the
    /// migrator writes. Expecting a code there would paint every such store red with no safe
    /// remedy: re-running the migration is not idempotent (defect 8).
    ///
    /// Like the stock check this is only meaningful straight after a migration -- once the shop is
    /// live, someone recategorising a product in the UI is a legitimate divergence.
    /// </remarks>
    private async Task VerifyCategoriesAsync(
        SQLiteConnection sqlite, StoreHubDbContext context, VerificationResult result, CancellationToken ct)
    {
        var resolver = await LegacyCategoryResolver.LoadAsync(sqlite);

        var expected = (await sqlite.QueryAsync<(string Barcode, long? Category)>(
                "SELECT Barcode, Category FROM InventoryProduct"))
            .ToDictionary(row => row.Barcode, row => resolver.Resolve(row.Category).Code);

        var actual = (await context.Products
                .Where(p => p.StoreId == _options.StoreId)
                .Select(p => new { p.Barcode, p.Category })
                .ToListAsync(ct))
            .ToDictionary(row => row.Barcode, row => row.Category);

        var mismatches = new List<string>();
        var expectedCategorised = 0;
        var actualCategorised = 0;

        foreach (var (barcode, want) in expected)
        {
            // A product missing from PostgreSQL resolves to null here, so a product that should
            // have been categorised and was not migrated at all counts as a mismatch rather than
            // quietly agreeing.
            var got = actual.GetValueOrDefault(barcode);

            if (want is not null) expectedCategorised++;
            if (want is not null && got == want) actualCategorised++;

            if (got != want)
            {
                mismatches.Add($"{barcode}: expected {want ?? "no category"}, migrated {got ?? "no category"}");
            }
        }

        result.Checks.Add(new VerificationCheck(
            CategoryCheckName, expectedCategorised, actualCategorised, mismatches.Count == 0));

        if (mismatches.Count == 0)
        {
            return;
        }

        result.Errors.Add(
            $"Category mismatch on {mismatches.Count} product(s). Product.Category must hold a v4 " +
            "catalogue code, never the raw legacy id.");

        foreach (var mismatch in mismatches.Take(MaxMismatchesReported))
        {
            result.Errors.Add($"  {mismatch}");
        }

        if (mismatches.Count > MaxMismatchesReported)
        {
            result.Errors.Add($"  ... and {mismatches.Count - MaxMismatchesReported} more");
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

        if (!await LegacySchemaProbe.HasTableAsync(sqlite, "PayLater"))
        {
            _logger.LogInformation(
                "No PayLater table in this store; skipping the count check. " +
                "PayLater is a GeneralHardware-only feature.");

            // Reported rather than skipped silently: a check that simply vanishes from the results
            // table is indistinguishable from one that was never written, which is the class of
            // blind spot that let defect 14 ship.
            // PostgreSQL holding PayLater rows this store has no legacy source for means a dirty
            // target or a second migration into it - the same thing VerifyPaymentsByMethodAsync
            // already treats as a failure. Hardcoding true here would make this the one row
            // that can never fail.
            result.Checks.Add(new VerificationCheck(NoPayLaterTableCheckName, 0, pgPayLater, pgPayLater == 0));
            return;
        }

        var sqlitePayLater = await sqlite.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PayLater");
        result.Checks.Add(new VerificationCheck("PayLater", sqlitePayLater, pgPayLater, sqlitePayLater <= pgPayLater));
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
            // CAST is load-bearing. Amount is declared NUMERIC, so SQLite stores each row in
            // whichever class fits and SUM() returns integer for some payment types and real
            // for others - on the real GeneralHardware store, integer for types 2/3/7/8 and
            // real for 1/5. Dapper binds the tuple accessor from the FIRST row and then throws
            // InvalidCastException on the first row of the other class, which took the whole
            // verify down before it ever reached the stock check. MimyMart and MimyShop escape
            // only because every one of their groups happens to land on integer.
            "SELECT PaymentTypeId, COUNT(*) AS Count, " +
            "       CAST(COALESCE(SUM(Amount), 0) AS REAL) AS Total " +
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
