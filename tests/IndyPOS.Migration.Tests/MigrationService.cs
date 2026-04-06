using System.Data.SQLite;
using Dapper;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Migration.Tests;

/// <summary>
/// Service that migrates data from SQLite to PostgreSQL.
/// This is a simplified migration service for testing purposes.
/// </summary>
public class MigrationService
{
    private readonly ILogger<MigrationService>? _logger;

    public MigrationService(ILogger<MigrationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Migrates all products from SQLite to PostgreSQL.
    /// </summary>
    public async Task<MigrationResult> MigrateProductsAsync(
        SQLiteConnection sqliteConnection,
        StoreHubDbContext postgresContext,
        string storeId,
        CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();

        if (sqliteConnection.State != System.Data.ConnectionState.Open)
            sqliteConnection.Open();

        var legacyProducts = await sqliteConnection.QueryAsync<LegacyProduct>("""
            SELECT
                InventoryProductId,
                Barcode,
                Description,
                Manufacturer,
                Brand,
                Category,
                UnitPrice,
                QuantityInStock,
                GroupPrice,
                GroupPriceQuantity,
                IsTrackable,
                DateCreated,
                DateUpdated
            FROM InventoryProduct
            """);

        foreach (var legacy in legacyProducts)
        {
            try
            {
                // Check if product already exists (by barcode)
                var existing = await postgresContext.Products
                    .FirstOrDefaultAsync(p => p.Barcode == legacy.Barcode, cancellationToken);

                if (existing is not null)
                {
                    _logger?.LogDebug("Product {Barcode} already exists, skipping", legacy.Barcode);
                    result.Skipped++;
                    continue;
                }

                // Create new product
                var createdUtc = ParseSqliteDate(legacy.DateCreated) ?? DateTime.UtcNow;
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    Barcode = legacy.Barcode,
                    Name = legacy.Description, // SQLite uses Description as name
                    Description = legacy.Description,
                    Manufacturer = string.IsNullOrEmpty(legacy.Manufacturer) ? null : legacy.Manufacturer,
                    Brand = string.IsNullOrEmpty(legacy.Brand) ? null : legacy.Brand,
                    Category = legacy.Category?.ToString(), // Convert int to string
                    UnitPrice = (decimal)legacy.UnitPrice,
                    GroupPrice = legacy.GroupPrice == 0 ? null : (decimal)legacy.GroupPrice,
                    GroupPriceQuantity = legacy.GroupPriceQuantity.HasValue ? (int)legacy.GroupPriceQuantity.Value : null,
                    IsActive = true,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = ParseSqliteDate(legacy.DateUpdated) ?? createdUtc
                };

                postgresContext.Products.Add(product);

                // Create initial inventory movement for stock
                if (legacy.QuantityInStock > 0)
                {
                    var movement = new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        StoreId = storeId,
                        ProductId = product.Id,
                        QuantityDelta = (int)legacy.QuantityInStock,
                        Reason = "Migration",
                        CreatedUtc = product.CreatedUtc
                    };
                    postgresContext.InventoryMovements.Add(movement);
                }

                result.Migrated++;
                result.ProductIdMap[(int)legacy.InventoryProductId] = product.Id;

                _logger?.LogDebug("Migrated product: {Barcode} ({LegacyId} -> {NewId})",
                    legacy.Barcode, legacy.InventoryProductId, product.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate product {Barcode}", legacy.Barcode);
                result.Failed++;
                result.Errors.Add($"Product {legacy.Barcode}: {ex.Message}");
            }
        }

        await postgresContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Migrates all invoices from SQLite to PostgreSQL.
    /// Requires products to be migrated first.
    /// </summary>
    public async Task<MigrationResult> MigrateInvoicesAsync(
        SQLiteConnection sqliteConnection,
        StoreHubDbContext postgresContext,
        string storeId,
        Dictionary<int, Guid> productIdMap,
        Dictionary<int, Guid> userIdMap,
        CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();

        if (sqliteConnection.State != System.Data.ConnectionState.Open)
            sqliteConnection.Open();

        var legacyInvoices = await sqliteConnection.QueryAsync<LegacyInvoice>("""
            SELECT InvoiceId, UserId, Total, DateCreated
            FROM Invoice
            """);

        foreach (var legacy in legacyInvoices)
        {
            try
            {
                // Get invoice lines
                var lines = (await sqliteConnection.QueryAsync<LegacyInvoiceLine>("""
                    SELECT InvoiceProductId, InvoiceId, InventoryProductId, Barcode, Description, Quantity, UnitPrice, Priority
                    FROM InvoiceProduct
                    WHERE InvoiceId = @InvoiceId
                    """, new { InvoiceId = legacy.InvoiceId })).ToList();

                // Get payments
                var payments = (await sqliteConnection.QueryAsync<LegacyInvoicePayment>("""
                    SELECT InvoicePaymentId, InvoiceId, PaymentTypeId, Amount, Note
                    FROM InvoicePayment
                    WHERE InvoiceId = @InvoiceId
                    """, new { InvoiceId = legacy.InvoiceId })).ToList();

                // Map user ID
                if (!userIdMap.TryGetValue((int)legacy.UserId, out var userId))
                {
                    _logger?.LogWarning("User {UserId} not found in mapping, using default", legacy.UserId);
                    userId = userIdMap.Values.FirstOrDefault();
                }

                var createdUtc = ParseSqliteDate(legacy.DateCreated) ?? DateTime.UtcNow;

                // Create invoice
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    UserId = userId,
                    TotalAmount = (decimal)legacy.Total,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = createdUtc
                };

                postgresContext.Invoices.Add(invoice);

                // Create invoice lines
                foreach (var line in lines)
                {
                    if (!productIdMap.TryGetValue((int)line.InventoryProductId, out var productId))
                    {
                        _logger?.LogWarning("Product {ProductId} not found in mapping", line.InventoryProductId);
                        continue;
                    }

                    var invoiceLine = new InvoiceLine
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = invoice.Id,
                        ProductId = productId,
                        ProductName = line.Description,
                        Quantity = (int)line.Quantity,
                        UnitPrice = (decimal)line.UnitPrice,
                        CreatedUtc = createdUtc
                    };
                    postgresContext.InvoiceLines.Add(invoiceLine);

                    // Create inventory movement (stock deduction)
                    var movement = new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        StoreId = storeId,
                        ProductId = productId,
                        QuantityDelta = -(int)line.Quantity, // Negative for sales
                        Reason = $"Sale:Invoice:{invoice.Id}",
                        CreatedUtc = invoice.CreatedUtc
                    };
                    postgresContext.InventoryMovements.Add(movement);
                }

                // Create payments
                foreach (var legacyPayment in payments)
                {
                    var payment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = invoice.Id,
                        Method = MapPaymentType((int)legacyPayment.PaymentTypeId),
                        Amount = (decimal)legacyPayment.Amount,
                        Note = legacyPayment.Note,
                        CreatedUtc = createdUtc
                    };
                    postgresContext.Payments.Add(payment);
                }

                result.Migrated++;
                result.InvoiceIdMap[(int)legacy.InvoiceId] = invoice.Id;

                _logger?.LogDebug("Migrated invoice: {LegacyId} -> {NewId}", legacy.InvoiceId, invoice.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate invoice {InvoiceId}", legacy.InvoiceId);
                result.Failed++;
                result.Errors.Add($"Invoice {legacy.InvoiceId}: {ex.Message}");
            }
        }

        await postgresContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Migrates users from SQLite to PostgreSQL.
    /// </summary>
    public async Task<MigrationResult> MigrateUsersAsync(
        SQLiteConnection sqliteConnection,
        StoreHubDbContext postgresContext,
        string storeId,
        CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();

        if (sqliteConnection.State != System.Data.ConnectionState.Open)
            sqliteConnection.Open();

        // Query users and credentials separately, then join
        var users = (await sqliteConnection.QueryAsync<LegacyUser>("""
            SELECT UserId, FirstName, LastName, RoleId, DateCreated, DateUpdated
            FROM User
            """)).ToList();

        var credentials = (await sqliteConnection.QueryAsync<LegacyUserCredential>("""
            SELECT UserId, Username, Password, DateCreated, DateUpdated
            FROM UserCredential
            """)).ToDictionary(c => c.UserId);

        foreach (var user in users)
        {
            if (!credentials.TryGetValue(user.UserId, out var credential))
            {
                _logger?.LogWarning("No credential found for user {UserId}, skipping", user.UserId);
                continue;
            }

            try
            {
                // Check if user already exists
                var existing = await postgresContext.StoreUsers
                    .FirstOrDefaultAsync(u => u.Username == credential.Username, cancellationToken);

                if (existing is not null)
                {
                    _logger?.LogDebug("User {Username} already exists, skipping", credential.Username);
                    result.Skipped++;
                    result.UserIdMap[(int)user.UserId] = existing.Id;
                    continue;
                }

                var storeUser = new StoreUser
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    LegacyUserId = (int)user.UserId,
                    Username = credential.Username,
                    PasswordHash = credential.Password, // Keep legacy encrypted value
                    PasswordHashVersion = 1, // Legacy TripleDES
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleId = (int)user.RoleId,
                    IsActive = true,
                    CreatedAtUtc = ParseSqliteDate(user.DateCreated) ?? DateTime.UtcNow,
                    LastModifiedAtUtc = DateTime.UtcNow
                };

                postgresContext.StoreUsers.Add(storeUser);
                result.Migrated++;
                result.UserIdMap[(int)user.UserId] = storeUser.Id;

                _logger?.LogDebug("Migrated user: {Username} ({LegacyId} -> {NewId})",
                    credential.Username, user.UserId, storeUser.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to migrate user {UserId}", user.UserId);
                result.Failed++;
                result.Errors.Add($"User {user.UserId}: {ex.Message}");
            }
        }

        await postgresContext.SaveChangesAsync(cancellationToken);

        return result;
    }

    private static DateTime? ParseSqliteDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var result))
            return DateTime.SpecifyKind(result, DateTimeKind.Utc);

        return null;
    }

    private static string MapPaymentType(int paymentTypeId) => paymentTypeId switch
    {
        1 => "Cash",
        2 => "Card",
        3 => "Transfer",
        4 => "PayLater",
        5 => "WelfareCard",
        _ => "Other"
    };
}

// Legacy entity mappings for Dapper - using classes for compatibility
public class LegacyProduct
{
    public long InventoryProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Brand { get; set; }
    public long? Category { get; set; }
    public double UnitPrice { get; set; }
    public long QuantityInStock { get; set; }
    public double GroupPrice { get; set; }
    public long? GroupPriceQuantity { get; set; }
    public long IsTrackable { get; set; }
    public string DateCreated { get; set; } = string.Empty;
    public string? DateUpdated { get; set; }
}

public class LegacyInvoice
{
    public long InvoiceId { get; set; }
    public long UserId { get; set; }
    public double Total { get; set; }
    public string DateCreated { get; set; } = string.Empty;
}

public class LegacyInvoiceLine
{
    public long InvoiceProductId { get; set; }
    public long InvoiceId { get; set; }
    public long InventoryProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long Quantity { get; set; }
    public double UnitPrice { get; set; }
    public long Priority { get; set; }
}

public class LegacyInvoicePayment
{
    public long InvoicePaymentId { get; set; }
    public long InvoiceId { get; set; }
    public long PaymentTypeId { get; set; }
    public double Amount { get; set; }
    public string? Note { get; set; }
}

public class LegacyUser
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string DateCreated { get; set; } = string.Empty;
    public string? DateUpdated { get; set; }
}

public class LegacyUserCredential
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DateCreated { get; set; } = string.Empty;
    public string? DateUpdated { get; set; }
}

public class MigrationResult
{
    public int Migrated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; } = [];
    public Dictionary<int, Guid> ProductIdMap { get; } = [];
    public Dictionary<int, Guid> InvoiceIdMap { get; } = [];
    public Dictionary<int, Guid> UserIdMap { get; } = [];

    public int Total => Migrated + Skipped + Failed;
    public bool IsSuccess => Failed == 0;
}
