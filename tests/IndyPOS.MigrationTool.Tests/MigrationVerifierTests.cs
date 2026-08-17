using Dapper;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class MigrationVerifierTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private LegacyStoreDatabase _store = null!;

    public MigrationVerifierTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        _store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await _postgres.ResetDatabaseAsync();
    }

    public async Task DisposeAsync() => await _store.DisposeAsync();

    /// <summary>
    /// Seeds a lookup table, <paramref name="userCount"/> users, <paramref name="productCount"/>
    /// products, and <paramref name="invoiceCount"/> one-line cash invoices, cycling through the
    /// seeded users and products.
    /// </summary>
    private async Task SeedManyAsync(int userCount, int productCount, int invoiceCount)
    {
        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();

        for (var i = 1; i <= userCount; i++)
        {
            await builder.AddUserAsync(
                i, $"cashier{i}", $"First{i}", $"Last{i}", roleId: 1, dateCreated: "2024-03-15 09:00:00");
        }

        for (var i = 1; i <= productCount; i++)
        {
            await builder.AddProductAsync(
                productId: i, barcode: $"885000100{i:D4}", description: $"Product {i}",
                unitPrice: 10m * i, quantityInStock: 50, category: 50, isTrackable: true,
                dateCreated: "2024-03-15 09:00:00");
        }

        for (var i = 1; i <= invoiceCount; i++)
        {
            var userId = ((i - 1) % userCount) + 1;
            var productId = ((i - 1) % productCount) + 1;

            await builder.AddInvoiceAsync(i, userId: userId, total: 100m, dateCreated: "2024-03-15 14:30:00");
            await builder.AddInvoiceLineAsync(
                invoiceProductId: i, invoiceId: i, productId: productId,
                barcode: $"885000100{productId:D4}", description: "Product",
                quantity: 1, unitPrice: 100m, originalUnitPrice: 100m);
            await builder.AddPaymentAsync(
                paymentId: i, invoiceId: i, paymentTypeId: 1, amount: 100m,
                dateCreated: "2024-03-15 14:30:00");
        }
    }

    [Fact]
    public async Task VerifyAsync_AfterSuccessfulMigration_ReturnsValid()
    {
        // Arrange
        await SeedManyAsync(userCount: 3, productCount: 15, invoiceCount: 30);

        // Perform migration
        var migrationOptions = CreateOptions();
        var migrationService = new SqliteMigrationService(migrationOptions, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Act
        var verifier = new MigrationVerifier(migrationOptions, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        result.Checks.Should().Contain(c => c.EntityName == "Users" && c.IsValid);
        result.Checks.Should().Contain(c => c.EntityName == "Products" && c.IsValid);
        result.Checks.Should().Contain(c => c.EntityName == "Invoices" && c.IsValid);
        result.Checks.Should().Contain(c => c.EntityName == "Payments" && c.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_BeforeMigration_ReturnsInvalid()
    {
        // Arrange - seed SQLite but don't migrate
        await SeedManyAsync(userCount: 3, productCount: 15, invoiceCount: 30);

        var options = CreateOptions();
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();

        // All counts should show mismatch (SQLite > 0, PostgreSQL = 0)
        result.Checks.Where(c => c.EntityName == "Users").First().SqliteCount.Should().BeGreaterThan(0);
        result.Checks.Where(c => c.EntityName == "Users").First().PostgresCount.Should().Be(0);
    }

    [Fact]
    public async Task VerifyAsync_WithPartialMigration_ReportsDiscrepancies()
    {
        // Arrange - seed SQLite with initial data
        await SeedManyAsync(userCount: 5, productCount: 20, invoiceCount: 0);

        // Migrate
        var options = CreateOptions();
        var migrationService = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Now add more products to SQLite (simulating new data after migration)
        // Use products instead of users to avoid unique constraint collision on username
        var builder = new LegacyStoreDataBuilder(_store);
        for (var i = 21; i <= 23; i++)
        {
            await builder.AddProductAsync(
                productId: i, barcode: $"885000100{i:D4}", description: $"Product {i}",
                unitPrice: 10m * i, quantityInStock: 50, category: 50, isTrackable: true,
                dateCreated: "2024-03-15 09:00:00");
        }

        // Act
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert - Products should show mismatch (SQLite has more than PostgreSQL)
        var productCheck = result.Checks.First(c => c.EntityName == "Products");
        productCheck.SqliteCount.Should().Be(23); // 20 + 3
        productCheck.PostgresCount.Should().Be(20); // Only original 20
        productCheck.IsValid.Should().BeFalse(); // 23 <= 20 is false, so invalid
    }

    [Fact]
    public async Task VerifyAsync_TotalRevenue_MatchesWithinTolerance()
    {
        // Arrange
        await SeedManyAsync(userCount: 2, productCount: 10, invoiceCount: 50);

        var options = CreateOptions();
        var migrationService = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Act
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert
        var revenueCheck = result.Checks.First(c => c.EntityName == "Total Revenue");
        revenueCheck.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ReturnsCorrectCounts()
    {
        // Arrange
        await SeedManyAsync(userCount: 4, productCount: 12, invoiceCount: 25);

        var options = CreateOptions();
        var migrationService = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Act
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert
        result.Checks.First(c => c.EntityName == "Users").SqliteCount.Should().Be(4);
        result.Checks.First(c => c.EntityName == "Products").SqliteCount.Should().Be(12);
        result.Checks.First(c => c.EntityName == "Invoices").SqliteCount.Should().Be(25);

        result.Checks.First(c => c.EntityName == "Users").PostgresCount.Should().Be(4);
        result.Checks.First(c => c.EntityName == "Products").PostgresCount.Should().Be(12);
        result.Checks.First(c => c.EntityName == "Invoices").PostgresCount.Should().Be(25);
    }

    [Fact]
    public async Task VerifyAsync_Stock_MatchesLegacyQuantityPerProduct()
    {
        // Defect 14 went unnoticed because every existing check reconciles counts and revenue, and
        // all of them passed against a store netting -400,541 units. This is the check that would
        // have caught it: legacy QuantityInStock vs SUM(QuantityDelta) of what the migration wrote.
        await SeedManyAsync(userCount: 2, productCount: 10, invoiceCount: 20);

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        var stockCheck = result.Checks.First(c => c.EntityName == "Stock (units)");
        stockCheck.IsValid.Should().BeTrue();

        // 10 products at 50 each. The 20 invoices selling one unit apiece must NOT be deducted --
        // QuantityInStock already excludes them.
        stockCheck.SqliteCount.Should().Be(500);
        stockCheck.PostgresCount.Should().Be(500);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WhenMigratedStockIsWrong_ShouldFailAndNameTheProduct()
    {
        // Non-vacuity: proves the stock check can actually fail. Without this, a check that always
        // agreed with itself would look identical to a passing one -- the trap that let defect 1's
        // scrambled payment mapping reconcile perfectly.
        await SeedManyAsync(userCount: 1, productCount: 3, invoiceCount: 0);

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        // Skew exactly one product's stock, the way a replayed sale used to.
        await using (var db = _postgres.CreateDbContext())
        {
            var product = await db.Products.FirstAsync(p => p.Barcode == "8850001000002");
            db.InventoryMovements.Add(new IndyPOS.Domain.Entities.Core.InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = MigrationScenario.StoreId,
                ProductId = product.Id,
                QuantityDelta = -7,
                Reason = "Migration:Sale",
                CreatedUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        result.IsValid.Should().BeFalse();
        result.Checks.First(c => c.EntityName == "Stock (units)").IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("8850001000002: expected 50, migrated 43"),
            "the operator needs the barcode and both figures, not just a store-wide total");
    }

    [Fact]
    public async Task VerifyAsync_Category_MatchesTheCatalogueCodePerProduct()
    {
        await SeedManyAsync(userCount: 1, productCount: 3, invoiceCount: 0);

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        var categoryCheck = result.Checks.First(c => c.EntityName == "Categories");
        categoryCheck.SqliteCount.Should().Be(3, "all three legacy products carry category 50");
        categoryCheck.PostgresCount.Should().Be(3);
        categoryCheck.IsValid.Should().BeTrue();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WhenAMigratedCategoryIsTheRawLegacyId_ShouldFailAndNameTheProduct()
    {
        // Non-vacuity, and defect 5 exactly: the migration used to write Category = "50". Every
        // count and total reconciles perfectly in that state -- Products, Invoices, Payments, Stock
        // and Revenue are all green -- which is how defect 5 survived. This is the only check that
        // looks at the VALUE, so it is the only one that can catch it coming back.
        await SeedManyAsync(userCount: 1, productCount: 3, invoiceCount: 0);

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        await using (var db = _postgres.CreateDbContext())
        {
            var product = await db.Products.FirstAsync(p => p.Barcode == "8850001000002");
            product.Category = "50";
            await db.SaveChangesAsync();
        }

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        result.IsValid.Should().BeFalse();
        result.Checks.First(c => c.EntityName == "Categories").IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            e => e.Contains("8850001000002") && e.Contains("GeneralMaterials") && e.Contains("50"),
            "the operator needs the barcode, the expected code and what was actually written");
    }

    [Fact]
    public async Task VerifyAsync_WithACategoryTheCatalogueDoesNotHave_ExpectsNoCategory()
    {
        // The migrator refuses to guess a code for an unmapped legacy name and writes null. The
        // verifier must expect the SAME null, or every store that adds a category after
        // LegacyCategoryMap was measured reports a mismatch it cannot act on -- re-running is not
        // idempotent (defect 8), so a red here would have no safe remedy.
        await _store.Connection.ExecuteAsync(
            "INSERT INTO ProductCategory (Id, Category) VALUES (777, 'หมวดที่เพิ่งเพิ่ม')");

        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 1, barcode: "8850001000010", description: "Mystery item",
            unitPrice: 120m, quantityInStock: 5, category: 777, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        var categoryCheck = result.Checks.First(c => c.EntityName == "Categories");
        categoryCheck.SqliteCount.Should().Be(0, "an unmappable category is expected as none");
        categoryCheck.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// A real GeneralHardware barcode: a TISI certification QR code scanned into the barcode field,
    /// twice over, at 90 characters. Two such rows exist in GeneralHardware and one in MimyMart.
    /// </summary>
    private const string OverlongLegacyBarcode =
        "https://appdb.tisi.go.th/Q/i.php?d=3535221937https://appdb.tisi.go.th/Q/i.php?d=3535221937";

    [Fact]
    public async Task VerifyAsync_WithABarcodeLongerThanTheColumn_MatchesTheProductByItsStoredBarcode()
    {
        // Product.Barcode is capped at 50 characters, so the migrator truncates on write. Both sides
        // of this comparison must therefore be keyed on the barcode AS STORED -- keying the
        // expectation on the untruncated legacy value makes the join miss and reports a mismatch on
        // a product that migrated perfectly.
        await SeedOverlongBarcodeProductAsync();

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        var categoryCheck = result.Checks.First(c => c.EntityName == "Categories");
        categoryCheck.PostgresCount.Should().Be(1, "the truncated product still carries its code");
        categoryCheck.IsValid.Should().BeTrue(
            "the product's category migrated correctly; only the join key was wrong");
    }

    [Fact]
    public async Task VerifyAsync_WithABarcodeLongerThanTheColumn_MatchesItsStockByStoredBarcode()
    {
        // Same defect as the category check above, in the stock check. Recorded separately because
        // they are separate comparisons that must not drift apart again.
        await SeedOverlongBarcodeProductAsync();

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        var stockCheck = result.Checks.First(c => c.EntityName == "Stock (units)");
        stockCheck.PostgresCount.Should().Be(7, "the stock movement was written against the product");
        stockCheck.IsValid.Should().BeTrue();
    }

    /// <summary>One real TISI certification URL, 45 characters — the shape that gets scanned in.</summary>
    private const string TisiUrl = "https://appdb.tisi.go.th/Q/i.php?d=3535221937";

    [Fact]
    public async Task VerifyAsync_WhenTwoBarcodesCollideOnceTruncated_ReportsItInsteadOfThrowing()
    {
        // Legacy Barcode is TEXT NOT NULL UNIQUE, and that constraint is what made keying the
        // comparison on the raw barcode safe. Truncating to 50 discards the guarantee: two products
        // whose barcodes agree on their first 50 characters now share one key. The dictionary build
        // would throw ArgumentException and take the WHOLE verify report down with it -- including
        // the revenue check that runs after -- which is the defect 15 and 16 failure shape.
        //
        // Reachable on real data: the URL prefix is 45 chars, so any two products certified under
        // the same TISI certificate and double-scanned collide.
        var first = TisiUrl + TisiUrl;
        var second = TisiUrl + "https://appdb.tisi.go.th/Q/i.php?d=9999999999";

        LegacyBarcode.ToStored(first).Should().Be(LegacyBarcode.ToStored(second),
            "the fixture is only meaningful if these really do collide once truncated");

        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 1, barcode: first, description: "Certified fitting A",
            unitPrice: 120m, quantityInStock: 7, category: 10, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 2, barcode: second, description: "Certified fitting B",
            unitPrice: 130m, quantityInStock: 3, category: 10, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var options = CreateOptions();
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);

        var result = await verifier.VerifyAsync();

        result.Errors.Should().Contain(e => e.Contains(LegacyBarcode.ToStored(first)),
            "the operator needs the key that collided in order to fix the source data");
        result.Checks.Should().Contain(c => c.EntityName == "Total Revenue",
            "a collision must not stop the later checks from running");
    }

    [Fact]
    public async Task VerifyAsync_WithNoLegacyProductCategoryTable_ReportsItInsteadOfThrowing()
    {
        // VerifyAsync is a flat sequence with no per-check isolation, so an unguarded throw in the
        // category check loses the entire report. The migrator can afford to let this throw because
        // RunPhaseAsync records it as a phase failure; the verifier has no such net. Defect 15's
        // lesson was to probe sqlite_master first and report the absence as its own row, so that
        // "this store has no such table" cannot be confused with a check that silently vanished.
        await _store.Connection.ExecuteAsync("DROP TABLE ProductCategory");

        var options = CreateOptions();
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);

        var result = await verifier.VerifyAsync();

        result.Checks.Should().Contain(c => c.EntityName == "Categories (no legacy table)",
            "the absence is reported, not dropped");
        result.Checks.Should().Contain(c => c.EntityName == "Total Revenue",
            "every check after the category check must still run");
    }

    private async Task SeedOverlongBarcodeProductAsync()
    {
        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 1, barcode: OverlongLegacyBarcode, description: "Certified fitting",
            unitPrice: 120m, quantityInStock: 7, category: 10, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
    }

    [Fact]
    public async Task VerifyAsync_WithNegativeLegacyStock_ExpectsZeroNotTheNegative()
    {
        // The migrator clamps negative legacy stock to zero (defect 14b). The verifier must agree
        // with that decision, or every one of the 1,535 real clamped products reports a mismatch.
        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 1, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: -5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 2, barcode: "8850001000027", description: "Sand 25kg",
            unitPrice: 80m, quantityInStock: 12, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        var stockCheck = result.Checks.First(c => c.EntityName == "Stock (units)");
        stockCheck.SqliteCount.Should().Be(12, "the -5 is expected as 0, not as -5");
        stockCheck.PostgresCount.Should().Be(12);
        stockCheck.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_AgainstAStoreWithNoPayLaterTable_Completes()
    {
        // Defect 15. The verifier's PayLater count runs unconditionally -- the same shape as
        // defect 3, which was only ever fixed in SqliteMigrationService. So `verify` throws
        // "no such table: PayLater" on MimyShop and MimyMart, 2 of the 3 real stores, and the
        // per-product stock check added for defect 14 has never been runnable on either of them.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.MimyShop);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Malee", "Sooksan", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850002000020", description: "Instant noodles",
            unitPrice: 6m, quantityInStock: 100, category: 20, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 6m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850002000020",
            description: "Instant noodles", quantity: 1, unitPrice: 6m, originalUnitPrice: 6m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 6m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        var options = CreateOptions(store.Path);
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);

        var result = await verifier.VerifyAsync();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        // The absence is REPORTED, not silently dropped: a check that vanishes from the table looks
        // the same as one that was never written, which is how defect 14 survived every run.
        result.Checks.Should().Contain(c => c.EntityName == NoPayLaterTableCheckName && c.IsValid);

        // And the checks that matter still ran -- this is the whole point of the fix.
        result.Checks.Should().Contain(c => c.EntityName == "Stock (units)" && c.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_AgainstAStoreThatHasPayLater_ShouldStillCompareTheCounts()
    {
        // Coverage, not a new behaviour. Without this, a probe stuck at "absent" - reporting every
        // store as having no PayLater and never comparing anything - passes the entire suite. The
        // sibling test only proves the no-table path, so the two together are what constrain the
        // probe. GeneralHardware is the shape that HAS the table.
        await SeedManyAsync(userCount: 1, productCount: 2, invoiceCount: 2);

        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPayLaterAsync(
            paymentId: 1, invoiceId: 1, description: "Somchai", payLaterAmount: 700m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");

        var options = CreateOptions();
        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var result = await new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance)
            .VerifyAsync();

        var payLater = result.Checks.Should().ContainSingle(c => c.EntityName == "PayLater").Subject;
        payLater.SqliteCount.Should().Be(1);
        payLater.PostgresCount.Should().Be(1);
        payLater.IsValid.Should().BeTrue();

        result.Checks.Should().NotContain(c => c.EntityName == NoPayLaterTableCheckName,
            "this store HAS the table, so the skip row must not appear");
    }

    private const string NoPayLaterTableCheckName = "PayLater (no legacy table)";

    private MigrationOptions CreateOptions() => CreateOptions(_store.Path);

    private MigrationOptions CreateOptions(string sqlitePath)
    {
        return new MigrationOptions
        {
            SqlitePath = sqlitePath,
            PostgresConnectionString = _postgres.ConnectionString,
            StoreId = MigrationScenario.StoreId,
            DryRun = false
        };
    }
}
