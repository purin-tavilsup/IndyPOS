using Dapper;

namespace IndyPOS.MigrationTool.Tests.Fixtures;

/// <summary>
/// Inserts deterministic legacy rows with explicit ids and values, so assertions can state an
/// expected value rather than a count.
///
/// The old Bogus-based seeder could only be asserted with counts and BeGreaterThan, which is why
/// defects 4, 5, 7 and 8 went unnoticed for so long.
/// </summary>
public sealed class LegacyStoreDataBuilder
{
    private readonly LegacyStoreDatabase _store;

    public LegacyStoreDataBuilder(LegacyStoreDatabase store) => _store = store;

    /// <summary>The 8 rows every real store's PaymentType table holds, verbatim.</summary>
    public async Task AddPaymentTypeLookupAsync()
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO PaymentType (Id, Type) VALUES
                (1, 'เงินสด'),
                (2, 'ลงบัญชี'),
                (3, 'บัตรสวัสดิการแห่งรัฐ'),
                (4, 'ม.33'),
                (5, 'โอนเข้าบัญชี'),
                (6, 'ผ่อนชำระ'),
                (7, 'คนละครึ่ง'),
                (8, 'เราชนะ');
            """);
    }

    public async Task AddUserAsync(
        int userId, string username, string firstName, string lastName, int roleId, string dateCreated)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO User (UserId, FirstName, LastName, RoleId, DateCreated)
            VALUES (@userId, @firstName, @lastName, @roleId, @dateCreated);
            """, new { userId, firstName, lastName, roleId, dateCreated });

        await _store.Connection.ExecuteAsync("""
            INSERT INTO UserCredential (UserId, Username, Password, DateCreated)
            VALUES (@userId, @username, @password, @dateCreated);
            """, new { userId, username, password = $"legacy-3des-hash-{username}", dateCreated });
    }

    public async Task AddProductAsync(
        int productId, string barcode, string description, decimal unitPrice, int quantityInStock,
        int? category, bool isTrackable, string dateCreated,
        decimal groupPrice = 0m, int? groupPriceQuantity = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO InventoryProduct
                (InventoryProductId, Barcode, Description, Category, QuantityInStock,
                 GroupPriceQuantity, IsTrackable, DateCreated, UnitPrice, GroupPrice)
            VALUES
                (@productId, @barcode, @description, @category, @quantityInStock,
                 @groupPriceQuantity, @isTrackableFlag, @dateCreated, @unitPrice, @groupPrice);
            """, new
        {
            productId, barcode, description, category, quantityInStock, groupPriceQuantity,
            isTrackableFlag = isTrackable ? 1 : 0, dateCreated, unitPrice, groupPrice
        });
    }

    public async Task AddInvoiceAsync(int invoiceId, int userId, decimal total, string dateCreated)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO Invoice (InvoiceId, UserId, Total, DateCreated)
            VALUES (@invoiceId, @userId, @total, @dateCreated);
            """, new { invoiceId, userId, total, dateCreated });
    }

    /// <remarks>
    /// Deliberately does NOT insert <c>IsTrackable</c>. The legacy write path omits it, so SQLite
    /// applies DEFAULT 1 and the column is dead data in every real store. Reproducing that is the
    /// point -- see LegacyStoreDataBuilderTests.AddInvoiceLine_ShouldLeaveIsTrackableAtItsSqliteDefault_Defect7Addendum.
    /// </remarks>
    public async Task AddInvoiceLineAsync(
        int invoiceProductId, int invoiceId, int productId, string barcode, string description,
        int quantity, decimal unitPrice, decimal originalUnitPrice,
        decimal groupPrice = 0m, bool isGroupProduct = false, string? note = null,
        int? priority = null, int? category = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO InvoiceProduct
                (InvoiceProductId, Priority, InvoiceId, InventoryProductId, Barcode, Description,
                 Category, Quantity, Note, UnitPrice, GroupPrice, IsGroupProduct, OriginalUnitPrice)
            VALUES
                (@invoiceProductId, @priority, @invoiceId, @productId, @barcode, @description,
                 @category, @quantity, @note, @unitPrice, @groupPrice, @isGroupProductFlag,
                 @originalUnitPrice);
            """, new
        {
            invoiceProductId, priority, invoiceId, productId, barcode, description, category,
            quantity, note, unitPrice, groupPrice,
            isGroupProductFlag = isGroupProduct ? 1 : 0, originalUnitPrice
        });
    }

    public async Task AddPaymentAsync(
        int paymentId, int invoiceId, int paymentTypeId, decimal amount, string dateCreated,
        string? note = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO Payment (PaymentId, InvoiceId, PaymentTypeId, DateCreated, Note, Amount)
            VALUES (@paymentId, @invoiceId, @paymentTypeId, @dateCreated, @note, @amount);
            """, new { paymentId, invoiceId, paymentTypeId, dateCreated, note, amount });
    }

    /// <param name="paymentId">
    /// The id of an existing <c>Payment</c> row. PayLater is a 1:1 extension of Payment, so this
    /// is both its primary key and its foreign key -- it must reference a payment that exists.
    /// </param>
    public async Task AddPayLaterAsync(
        int paymentId, int invoiceId, string description, decimal payLaterAmount, decimal paidAmount,
        bool isCompleted, string dateCreated, string? dateUpdated = null)
    {
        await _store.Connection.ExecuteAsync("""
            INSERT INTO PayLater
                (PaymentId, Description, InvoiceId, IsCompleted, DateCreated, DateUpdated,
                 PayLaterAmount, PaidAmount)
            VALUES
                (@paymentId, @description, @invoiceId, @isCompletedFlag, @dateCreated, @dateUpdated,
                 @payLaterAmount, @paidAmount);
            """, new
        {
            paymentId, description, invoiceId, isCompletedFlag = isCompleted ? 1 : 0,
            dateCreated, dateUpdated, payLaterAmount, paidAmount
        });
    }
}
