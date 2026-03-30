using System.Data.SQLite;
using Bogus;
using Dapper;

namespace IndyPOS.MigrationTool.Tests.TestData;

/// <summary>
/// Seeds SQLite database with production-like test data using Bogus.
/// Uses hardcoded Thai names since Bogus doesn't have a Thai locale.
/// </summary>
public class SqliteTestDataSeeder
{
    private readonly SQLiteConnection _connection;
    private readonly Faker _faker = new("en"); // For random numbers, dates, etc.

    // Common Thai first names
    private static readonly string[] ThaiFirstNames =
    [
        "สมชาย", "สมหญิง", "วิชัย", "วิภา", "ประยุทธ์",
        "สุดา", "มานี", "มานะ", "ปิติ", "ปิยะ",
        "อรุณ", "อรทัย", "ชัยวัฒน์", "จิราภรณ์", "ธนาคาร",
        "กัญญา", "พิมพ์", "ภัทร", "ณัฐ", "ศิริ"
    ];

    // Common Thai last names
    private static readonly string[] ThaiLastNames =
    [
        "ใจดี", "สุขสันต์", "รักไทย", "ศรีสุข", "มั่งมี",
        "วงศ์ไทย", "พรหมมา", "จันทร์เพ็ญ", "แก้วมณี", "ทองดี",
        "สิทธิชัย", "เจริญสุข", "รุ่งเรือง", "สมบูรณ์", "พิทักษ์"
    ];

    // Common Thai customer names (full names for PayLater)
    private static readonly string[] ThaiCustomerNames =
    [
        "คุณสมชาย ใจดี",
        "คุณสมหญิง รักไทย",
        "นายวิชัย ศรีสุข",
        "นางสาวสุดา มั่งมี",
        "คุณมานี ทองดี",
        "คุณประยุทธ์ สิทธิชัย",
        "นายชัยวัฒน์ เจริญสุข",
        "นางจิราภรณ์ รุ่งเรือง",
        "คุณณัฐ สมบูรณ์",
        "คุณภัทร พิทักษ์"
    ];

    // Actual Thai product names from convenience stores
    private static readonly string[] ThaiProductNames =
    [
        "น้ำดื่มสิงห์ 600ml",
        "เบียร์ลีโอ 490ml",
        "โค้ก 325ml",
        "เป๊ปซี่ 325ml",
        "สไปรท์ 325ml",
        "แฟนต้าส้ม 325ml",
        "เรดบูล 250ml",
        "M-150 150ml",
        "กระทิงแดง 150ml",
        "มาม่าหมูสับ",
        "มาม่าต้มยำกุ้ง",
        "ไวไว 60g",
        "ยำยำจัมโบ้",
        "คอลเกตยาสีฟัน 150g",
        "ซันซิลแชมพู 180ml",
        "โดฟสบู่ก้อน",
        "ผ้าอ้อมผู้ใหญ่",
        "กระดาษทิชชู่ 100แผ่น",
        "ถุงขยะ 20ใบ",
        "น้ำยาล้างจาน 500ml",
        "ข้าวสาร 5kg",
        "น้ำมันพืช 1L",
        "น้ำปลา 700ml",
        "ซอสพริก 300ml",
        "นมสด 1L",
        "ไข่ไก่ 10ฟอง",
        "ขนมปังโฮลวีท",
        "คิทแคท 2F",
        "สนิกเกอร์",
        "เอ็มแอนด์เอ็ม 40g"
    ];

    public SqliteTestDataSeeder(SQLiteConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Creates the legacy SQLite schema matching the production database.
    /// </summary>
    public async Task CreateSchemaAsync()
    {
        await _connection.ExecuteAsync("""
            -- Users table
            CREATE TABLE IF NOT EXISTS User (
                UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                RoleId INTEGER NOT NULL DEFAULT 1,
                DateCreated TEXT,
                DateUpdated TEXT
            );

            -- User credentials (separate table in legacy system)
            CREATE TABLE IF NOT EXISTS UserCredential (
                UserCredentialId INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );

            -- Products table
            CREATE TABLE IF NOT EXISTS InventoryProduct (
                InventoryProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL UNIQUE,
                Description TEXT NOT NULL,
                Manufacturer TEXT,
                Brand TEXT,
                Category INTEGER,
                UnitPrice REAL NOT NULL,
                QuantityInStock INTEGER NOT NULL DEFAULT 0,
                GroupPrice REAL DEFAULT 0,
                GroupPriceQuantity INTEGER DEFAULT 0,
                IsTrackable INTEGER DEFAULT 1,
                DateCreated TEXT,
                DateUpdated TEXT
            );

            -- Invoices table
            CREATE TABLE IF NOT EXISTS Invoice (
                InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Total REAL NOT NULL,
                DateCreated TEXT,
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );

            -- Invoice products (line items)
            CREATE TABLE IF NOT EXISTS InvoiceProduct (
                InvoiceProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                InventoryProductId INTEGER NOT NULL,
                Barcode TEXT NOT NULL,
                Description TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId),
                FOREIGN KEY (InventoryProductId) REFERENCES InventoryProduct(InventoryProductId)
            );

            -- Payments table (named 'Payment' in production, maps to 'InvoicePayment' in some code)
            CREATE TABLE IF NOT EXISTS Payment (
                PaymentId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                PaymentTypeId INTEGER NOT NULL,
                Amount REAL NOT NULL,
                Note TEXT,
                FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId)
            );

            -- PayLater table
            CREATE TABLE IF NOT EXISTS PayLater (
                PayLaterId INTEGER PRIMARY KEY AUTOINCREMENT,
                InvoiceId INTEGER NOT NULL,
                UserId INTEGER NOT NULL,
                CustomerName TEXT NOT NULL,
                PaymentAmount REAL NOT NULL,
                IsCompleted INTEGER DEFAULT 0,
                DateCreated TEXT,
                DateUpdated TEXT,
                FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId),
                FOREIGN KEY (UserId) REFERENCES User(UserId)
            );
            """);
    }

    /// <summary>
    /// Seeds users with realistic Thai names and credentials.
    /// </summary>
    public async Task<List<SeededUser>> SeedUsersAsync(int count = 5)
    {
        var users = new List<SeededUser>();
        var roles = new[] { 1, 2, 3 }; // Admin, Manager, Cashier

        for (int i = 0; i < count; i++)
        {
            // Use Thai names, cycling through the arrays
            var firstName = ThaiFirstNames[i % ThaiFirstNames.Length];
            var lastName = ThaiLastNames[i % ThaiLastNames.Length];
            var username = $"user{i + 1}"; // Simple ASCII username for login
            var roleId = roles[i % roles.Length];
            var dateCreated = _faker.Date.Past(2).ToString("yyyy-MM-dd HH:mm:ss");

            // Insert user
            var userId = await _connection.ExecuteScalarAsync<long>("""
                INSERT INTO User (FirstName, LastName, RoleId, DateCreated, DateUpdated)
                VALUES (@FirstName, @LastName, @RoleId, @DateCreated, @DateCreated);
                SELECT last_insert_rowid();
                """, new { FirstName = firstName, LastName = lastName, RoleId = roleId, DateCreated = dateCreated });

            // Insert credentials (legacy TripleDES-style hash - just a placeholder)
            var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"legacy_{username}_hash"));
            await _connection.ExecuteAsync("""
                INSERT INTO UserCredential (UserId, Username, Password)
                VALUES (@UserId, @Username, @Password);
                """, new { UserId = userId, Username = username, Password = passwordHash });

            users.Add(new SeededUser(userId, username, firstName, lastName, roleId));
        }

        return users;
    }

    /// <summary>
    /// Seeds products with realistic Thai retail data (convenience store items).
    /// Uses actual Thai product names to test Unicode handling.
    /// </summary>
    public async Task<List<SeededProduct>> SeedProductsAsync(int count = 50)
    {
        var products = new List<SeededProduct>();
        var categories = new[] { 1, 2, 3, 4, 5 }; // Beverages, Snacks, Groceries, Personal Care, Household

        var brands = new[] { "สิงห์", "ลีโอ", "เนสท์เล่", "ยูนิลีเวอร์", "พีแอนด์จี", "แบรนด์ท้องถิ่น", null };
        var manufacturers = new[] { "ไทยเบฟเวอเรจ", "เนสท์เล่ไทย", "ยูนิลีเวอร์ไทย", "โรงงานท้องถิ่น", null };

        for (int i = 0; i < count; i++)
        {
            var barcode = $"88{_faker.Random.Long(10000000000, 99999999999)}";
            // Use Thai product names, cycle through if count exceeds array length
            var rawDesc = ThaiProductNames[i % ThaiProductNames.Length];
            var description = rawDesc[..Math.Min(50, rawDesc.Length)]; // Truncate to fit varchar(100)
            var unitPrice = _faker.Random.Decimal(10, 500);
            var quantity = _faker.Random.Int(0, 100);
            var category = _faker.PickRandom(categories);
            var brand = _faker.PickRandom(brands);
            var manufacturer = _faker.PickRandom(manufacturers);
            var groupPrice = _faker.Random.Bool(0.3f) ? unitPrice * 0.9m * _faker.Random.Int(3, 6) : 0;
            var groupPriceQty = groupPrice > 0 ? _faker.Random.Int(3, 6) : 0;
            var dateCreated = _faker.Date.Past(1).ToString("yyyy-MM-dd HH:mm:ss");

            var productId = await _connection.ExecuteScalarAsync<long>("""
                INSERT INTO InventoryProduct (Barcode, Description, Manufacturer, Brand, Category,
                    UnitPrice, QuantityInStock, GroupPrice, GroupPriceQuantity, IsTrackable, DateCreated, DateUpdated)
                VALUES (@Barcode, @Description, @Manufacturer, @Brand, @Category,
                    @UnitPrice, @QuantityInStock, @GroupPrice, @GroupPriceQuantity, 1, @DateCreated, @DateCreated);
                SELECT last_insert_rowid();
                """, new
            {
                Barcode = barcode,
                Description = description,
                Manufacturer = manufacturer,
                Brand = brand,
                Category = category,
                UnitPrice = unitPrice,
                QuantityInStock = quantity,
                GroupPrice = groupPrice,
                GroupPriceQuantity = groupPriceQty,
                DateCreated = dateCreated
            });

            products.Add(new SeededProduct(productId, barcode, description, unitPrice, quantity));
        }

        return products;
    }

    /// <summary>
    /// Seeds invoices with realistic sales patterns.
    /// </summary>
    public async Task<List<SeededInvoice>> SeedInvoicesAsync(
        List<SeededUser> users,
        List<SeededProduct> products,
        int count = 100)
    {
        var invoices = new List<SeededInvoice>();

        for (int i = 0; i < count; i++)
        {
            var user = _faker.PickRandom(users);
            var dateCreated = _faker.Date.Past(1).ToString("yyyy-MM-dd HH:mm:ss");
            var lineCount = _faker.Random.Int(1, 5);
            var lines = new List<SeededInvoiceLine>();
            decimal total = 0;

            // Create invoice
            var invoiceId = await _connection.ExecuteScalarAsync<long>("""
                INSERT INTO Invoice (UserId, Total, DateCreated)
                VALUES (@UserId, 0, @DateCreated);
                SELECT last_insert_rowid();
                """, new { UserId = user.Id, DateCreated = dateCreated });

            // Add line items
            var usedProducts = new HashSet<long>();
            for (int j = 0; j < lineCount; j++)
            {
                var product = _faker.PickRandom(products.Where(p => !usedProducts.Contains(p.Id)).ToList());
                if (product == null) break;

                usedProducts.Add(product.Id);
                var quantity = _faker.Random.Int(1, 3);
                var lineTotal = product.UnitPrice * quantity;
                total += lineTotal;

                await _connection.ExecuteAsync("""
                    INSERT INTO InvoiceProduct (InvoiceId, InventoryProductId, Barcode, Description, Quantity, UnitPrice)
                    VALUES (@InvoiceId, @ProductId, @Barcode, @Description, @Quantity, @UnitPrice);
                    """, new
                {
                    InvoiceId = invoiceId,
                    ProductId = product.Id,
                    Barcode = product.Barcode,
                    Description = product.Description,
                    Quantity = quantity,
                    UnitPrice = product.UnitPrice
                });

                lines.Add(new SeededInvoiceLine(product.Id, product.Barcode, quantity, product.UnitPrice));
            }

            // Update invoice total
            await _connection.ExecuteAsync("""
                UPDATE Invoice SET Total = @Total WHERE InvoiceId = @InvoiceId;
                """, new { Total = total, InvoiceId = invoiceId });

            // Add payment(s)
            var paymentTypeId = _faker.Random.WeightedRandom(
                new[] { 1, 2, 3, 4 }, // Cash, Card, Transfer, PayLater
                new[] { 0.6f, 0.2f, 0.1f, 0.1f });

            if (paymentTypeId == 4) // PayLater - split payment
            {
                var paidNow = Math.Round(total * 0.5m, 2);
                var payLaterAmount = total - paidNow;

                // Cash portion
                await _connection.ExecuteAsync("""
                    INSERT INTO Payment (InvoiceId, PaymentTypeId, Amount, Note)
                    VALUES (@InvoiceId, 1, @Amount, NULL);
                    """, new { InvoiceId = invoiceId, Amount = paidNow });

                // PayLater portion
                await _connection.ExecuteAsync("""
                    INSERT INTO Payment (InvoiceId, PaymentTypeId, Amount, Note)
                    VALUES (@InvoiceId, 4, @Amount, @Note);
                    """, new { InvoiceId = invoiceId, Amount = payLaterAmount, Note = "Pay later" });

                // PayLater record - use Thai customer names
                var customerName = _faker.PickRandom(ThaiCustomerNames);
                var isCompleted = _faker.Random.Bool(0.3f) ? 1 : 0;
                await _connection.ExecuteAsync("""
                    INSERT INTO PayLater (InvoiceId, UserId, CustomerName, PaymentAmount, IsCompleted, DateCreated, DateUpdated)
                    VALUES (@InvoiceId, @UserId, @CustomerName, @Amount, @IsCompleted, @DateCreated, @DateCreated);
                    """, new
                {
                    InvoiceId = invoiceId,
                    UserId = user.Id,
                    CustomerName = customerName,
                    Amount = payLaterAmount,
                    IsCompleted = isCompleted,
                    DateCreated = dateCreated
                });
            }
            else
            {
                await _connection.ExecuteAsync("""
                    INSERT INTO Payment (InvoiceId, PaymentTypeId, Amount, Note)
                    VALUES (@InvoiceId, @PaymentTypeId, @Amount, NULL);
                    """, new { InvoiceId = invoiceId, PaymentTypeId = paymentTypeId, Amount = total });
            }

            invoices.Add(new SeededInvoice(invoiceId, user.Id, total, lines));
        }

        return invoices;
    }

    /// <summary>
    /// Seeds a complete test dataset with realistic proportions.
    /// </summary>
    public async Task<TestDataSet> SeedCompleteDataSetAsync(
        int userCount = 5,
        int productCount = 50,
        int invoiceCount = 100)
    {
        await CreateSchemaAsync();

        var users = await SeedUsersAsync(userCount);
        var products = await SeedProductsAsync(productCount);
        var invoices = await SeedInvoicesAsync(users, products, invoiceCount);

        // Count PayLater records
        var payLaterCount = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PayLater");

        return new TestDataSet(users, products, invoices, payLaterCount);
    }
}

// Record types for seeded data
public record SeededUser(long Id, string Username, string FirstName, string LastName, int RoleId);
public record SeededProduct(long Id, string Barcode, string Description, decimal UnitPrice, int Quantity);
public record SeededInvoiceLine(long ProductId, string Barcode, int Quantity, decimal UnitPrice);
public record SeededInvoice(long Id, long UserId, decimal Total, List<SeededInvoiceLine> Lines);
public record TestDataSet(
    List<SeededUser> Users,
    List<SeededProduct> Products,
    List<SeededInvoice> Invoices,
    int PayLaterCount);
