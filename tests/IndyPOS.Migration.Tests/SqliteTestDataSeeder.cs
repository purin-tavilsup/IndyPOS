using System.Data.SQLite;
using Dapper;

namespace IndyPOS.Migration.Tests;

/// <summary>
/// Seeds test data into SQLite database for migration testing.
/// </summary>
public static class SqliteTestDataSeeder
{
    /// <summary>
    /// Seeds a test user with credentials.
    /// </summary>
    public static int SeedUser(
        SQLiteConnection connection,
        string firstName = "Test",
        string lastName = "User",
        int roleId = 1,
        string username = "testuser",
        string password = "encrypted_password")
    {
        // Insert user
        var userId = connection.QuerySingle<int>("""
            INSERT INTO User (FirstName, LastName, RoleId, DateCreated)
            VALUES (@FirstName, @LastName, @RoleId, datetime('now','localtime'));
            SELECT last_insert_rowid();
            """, new { FirstName = firstName, LastName = lastName, RoleId = roleId });

        // Insert credential
        connection.Execute("""
            INSERT INTO UserCredential (UserId, Username, Password, DateCreated)
            VALUES (@UserId, @Username, @Password, datetime('now','localtime'));
            """, new { UserId = userId, Username = username, Password = password });

        return userId;
    }

    /// <summary>
    /// Seeds a test product.
    /// </summary>
    public static int SeedProduct(
        SQLiteConnection connection,
        string barcode,
        string description,
        decimal unitPrice,
        int quantityInStock = 10,
        string? manufacturer = null,
        string? brand = null,
        int? category = null,
        decimal? groupPrice = null,
        int? groupPriceQuantity = null,
        bool isTrackable = true)
    {
        return connection.QuerySingle<int>("""
            INSERT INTO InventoryProduct
                (Barcode, Description, Manufacturer, Brand, Category, UnitPrice,
                 QuantityInStock, GroupPrice, GroupPriceQuantity, IsTrackable, DateCreated)
            VALUES
                (@Barcode, @Description, @Manufacturer, @Brand, @Category, @UnitPrice,
                 @QuantityInStock, @GroupPrice, @GroupPriceQuantity, @IsTrackable, datetime('now','localtime'));
            SELECT last_insert_rowid();
            """, new
            {
                Barcode = barcode,
                Description = description,
                Manufacturer = manufacturer,
                Brand = brand,
                Category = category,
                UnitPrice = unitPrice,
                QuantityInStock = quantityInStock,
                GroupPrice = groupPrice,
                GroupPriceQuantity = groupPriceQuantity,
                IsTrackable = isTrackable ? 1 : 0
            });
    }

    /// <summary>
    /// Seeds a complete invoice with products and payments.
    /// </summary>
    public static int SeedInvoice(
        SQLiteConnection connection,
        int userId,
        decimal total,
        List<InvoiceLineData> lines,
        List<InvoicePaymentData> payments)
    {
        // Insert invoice
        var invoiceId = connection.QuerySingle<int>("""
            INSERT INTO Invoice (UserId, Total, DateCreated)
            VALUES (@UserId, @Total, datetime('now','localtime'));
            SELECT last_insert_rowid();
            """, new { UserId = userId, Total = total });

        // Insert invoice products
        foreach (var line in lines)
        {
            connection.Execute("""
                INSERT INTO InvoiceProduct
                    (InvoiceId, InventoryProductId, Barcode, Description, Quantity, UnitPrice, Priority, DateCreated)
                VALUES
                    (@InvoiceId, @InventoryProductId, @Barcode, @Description, @Quantity, @UnitPrice, @Priority, datetime('now','localtime'));
                """, new
                {
                    InvoiceId = invoiceId,
                    InventoryProductId = line.InventoryProductId,
                    Barcode = line.Barcode,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Priority = line.Priority
                });
        }

        // Insert payments
        foreach (var payment in payments)
        {
            connection.Execute("""
                INSERT INTO InvoicePayment (InvoiceId, PaymentTypeId, Amount, Note, DateCreated)
                VALUES (@InvoiceId, @PaymentTypeId, @Amount, @Note, datetime('now','localtime'));
                """, new
                {
                    InvoiceId = invoiceId,
                    PaymentTypeId = payment.PaymentTypeId,
                    Amount = payment.Amount,
                    Note = payment.Note
                });
        }

        return invoiceId;
    }

    /// <summary>
    /// Seeds an accounts receivable (PayLater) record.
    /// </summary>
    public static int SeedAccountsReceivable(
        SQLiteConnection connection,
        int invoiceId,
        string description,
        decimal amount,
        bool isCompleted = false)
    {
        return connection.QuerySingle<int>("""
            INSERT INTO AccountsReceivable
                (InvoiceId, ReceivableDescription, ReceivableAmount, IsCompleted, DateCreated)
            VALUES
                (@InvoiceId, @ReceivableDescription, @ReceivableAmount, @IsCompleted, datetime('now','localtime'));
            SELECT last_insert_rowid();
            """, new
            {
                InvoiceId = invoiceId,
                ReceivableDescription = description,
                ReceivableAmount = amount,
                IsCompleted = isCompleted ? 1 : 0
            });
    }

    /// <summary>
    /// Seeds a payment against an accounts receivable.
    /// </summary>
    public static int SeedAccountsReceivablePayment(
        SQLiteConnection connection,
        int accountsReceivableId,
        decimal amount)
    {
        return connection.QuerySingle<int>("""
            INSERT INTO AccountsReceivablePayment (AccountsReceivableId, PaymentAmount, DateCreated)
            VALUES (@AccountsReceivableId, @PaymentAmount, datetime('now','localtime'));
            SELECT last_insert_rowid();
            """, new { AccountsReceivableId = accountsReceivableId, PaymentAmount = amount });
    }

    /// <summary>
    /// Seeds a realistic set of test data for a complete store scenario.
    /// </summary>
    public static TestDataSet SeedRealisticStoreData(SQLiteConnection connection)
    {
        var result = new TestDataSet();

        // Users
        result.AdminUserId = SeedUser(connection, "Admin", "User", roleId: 4, username: "admin", password: "admin_pass");
        result.ManagerUserId = SeedUser(connection, "Store", "Manager", roleId: 3, username: "manager", password: "mgr_pass");
        result.CashierUserId = SeedUser(connection, "John", "Cashier", roleId: 1, username: "cashier1", password: "cash_pass");

        // Products with various attributes
        result.Products.Add(SeedProduct(connection, "8850999111001", "Coca-Cola 330ml", 25.00m, 100, "Coca-Cola", "Coca-Cola", 1));
        result.Products.Add(SeedProduct(connection, "8850999111002", "Pepsi 330ml", 23.00m, 80, "PepsiCo", "Pepsi", 1));
        result.Products.Add(SeedProduct(connection, "8851234567890", "Thai Rice 5kg", 180.00m, 50, "CP", "Royal Umbrella", 2));
        result.Products.Add(SeedProduct(connection, "8859876543210", "Cooking Oil 1L", 65.00m, 30, "Yok", "Yok", 2, 120.00m, 2)); // With group price
        result.Products.Add(SeedProduct(connection, "TEST000000001", "Test Product Unicode: ไทย 日本語 中文", 100.00m, 10)); // Unicode test
        result.Products.Add(SeedProduct(connection, "TEST000000002", "Zero Stock Product", 50.00m, 0)); // Zero stock

        // Invoices with different payment types
        // Invoice 1: Cash payment
        result.Invoices.Add(SeedInvoice(connection, result.CashierUserId, 125.00m,
        [
            new(result.Products[0], "8850999111001", "Coca-Cola 330ml", 3, 25.00m),
            new(result.Products[1], "8850999111002", "Pepsi 330ml", 2, 23.00m)
        ],
        [
            new(1, 125.00m, null) // Cash
        ]));

        // Invoice 2: Card payment
        result.Invoices.Add(SeedInvoice(connection, result.CashierUserId, 180.00m,
        [
            new(result.Products[2], "8851234567890", "Thai Rice 5kg", 1, 180.00m)
        ],
        [
            new(2, 180.00m, "Visa *1234") // Card
        ]));

        // Invoice 3: Mixed payment
        result.Invoices.Add(SeedInvoice(connection, result.ManagerUserId, 245.00m,
        [
            new(result.Products[3], "8859876543210", "Cooking Oil 1L", 2, 65.00m),
            new(result.Products[0], "8850999111001", "Coca-Cola 330ml", 3, 25.00m),
            new(result.Products[4], "TEST000000001", "Test Product Unicode: ไทย 日本語 中文", 1, 100.00m)
        ],
        [
            new(1, 200.00m, null), // Cash
            new(2, 45.00m, "SCB *5678") // Card
        ]));

        // Invoice 4: PayLater
        var payLaterInvoiceId = SeedInvoice(connection, result.CashierUserId, 300.00m,
        [
            new(result.Products[4], "TEST000000001", "Test Product Unicode: ไทย 日本語 中文", 3, 100.00m)
        ],
        [
            new(4, 300.00m, "Customer: นายสมชาย") // PayLater (type 4)
        ]);
        result.Invoices.Add(payLaterInvoiceId);

        // Create accounts receivable for PayLater invoice
        var arId = SeedAccountsReceivable(connection, payLaterInvoiceId, "นายสมชาย - ยอดค้างชำระ", 300.00m);
        result.AccountsReceivables.Add(arId);

        // Partial payment on AR
        SeedAccountsReceivablePayment(connection, arId, 100.00m);

        return result;
    }
}

public record InvoiceLineData(int InventoryProductId, string Barcode, string Description, int Quantity, decimal UnitPrice, int Priority = 0);
public record InvoicePaymentData(int PaymentTypeId, decimal Amount, string? Note);

public class TestDataSet
{
    public int AdminUserId { get; set; }
    public int ManagerUserId { get; set; }
    public int CashierUserId { get; set; }
    public List<int> Products { get; } = [];
    public List<int> Invoices { get; } = [];
    public List<int> AccountsReceivables { get; } = [];
}
