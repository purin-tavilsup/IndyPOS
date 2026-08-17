using FluentAssertions;
using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// A placeholder product should keep the category its own invoice line records.
/// </summary>
/// <remarks>
/// Defect 13 synthesises an inactive placeholder for a line whose product was deleted, so the sale
/// survives. It never set <c>Category</c> — yet <c>InvoiceProduct</c> carries its own
/// <c>Category</c> column, so the information was sitting in the very row being read.
/// <para>
/// Measured on the real stores: GeneralHardware synthesises **30** placeholders from 244 orphaned
/// lines, and for all 30 the lines agree on a category that its <c>ProductCategory</c> table holds.
/// MimyMart and MimyShop synthesise none. So every placeholder that can be categorised, is.
/// </para>
/// <para>
/// "Never guessed" was never meant to mean "never looked".
/// </para>
/// </remarks>
[Collection("Postgres")]
public class PlaceholderCategoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public PlaceholderCategoryTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// One invoice whose only line points at a product that no longer exists, with no live product
    /// sharing its barcode — the tier-3 path that synthesises a placeholder.
    /// </summary>
    private static async Task SeedDeletedProductLineAsync(LegacyStoreDatabase store, int? lineCategory)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 80m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 777, barcode: "8850009999999",
            description: "Product deleted years ago", quantity: 1, unitPrice: 80m,
            originalUnitPrice: 80m, category: lineCategory);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 80m,
            dateCreated: "2024-03-15 14:30:00");
    }

    [Fact]
    public async Task MigrateInvoiceLines_APlaceholderProduct_TakesTheCategoryFromItsOwnLine()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedDeletedProductLineAsync(store, lineCategory: 50);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var placeholder = await db.Products.SingleAsync();

        placeholder.Category.Should().Be(ProductCategoryCodes.GeneralMaterials,
            "legacy id 50 is วัสดุและอุปกรณ์ทั่วไป in this store, and the line records it");
        placeholder.IsActive.Should().BeFalse("categorising it must not put it back in the catalogue");
    }

    [Fact]
    public async Task MigrateInvoiceLines_APlaceholderWhoseLineHasNoCategory_IsUncategorisedWithoutAnError()
    {
        // InvoiceProduct.Category is nullable and an uncategorised line is not a data problem.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedDeletedProductLineAsync(store, lineCategory: null);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Products.SingleAsync()).Category.Should().BeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateInvoiceLines_APlaceholderWhoseLineCategoryIsUnknown_IsUncategorisedAndReported()
    {
        // Same treatment as a live product with an unresolvable category: never guess a code, and
        // never write the raw legacy id, but say so rather than dropping it in silence.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedDeletedProductLineAsync(store, lineCategory: 999);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Products.SingleAsync()).Category.Should().BeNull(
            "the raw legacy id is never written as a fallback");
        result.Errors.Should().ContainSingle()
              .Which.Should().Contain("999").And.Contain("8850009999999");
    }
}
