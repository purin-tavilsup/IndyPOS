using System.Data.SQLite;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.UseCases.Cloud.Sync.BulkMigration;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndyPOS.MigrationTool.Services;

public class SqliteMigrationService
{
    private readonly MigrationOptions _options;
    private readonly ILogger<SqliteMigrationService> _logger;
    private MigrationResult _result = new();

    public SqliteMigrationService(MigrationOptions options, ILogger<SqliteMigrationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateAllAsync(CancellationToken ct = default)
    {
        _result = new MigrationResult();

        await using var sqliteConnection = new SQLiteConnection($"Data Source={_options.SqlitePath};Version=3;");
        await sqliteConnection.OpenAsync(ct);

        var dbOptions = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(_options.PostgresConnectionString)
            .Options;

        await using var context = new StoreHubDbContext(dbOptions);

        // Ensure database exists and migrations applied
        if (!_options.DryRun)
        {
            await context.Database.MigrateAsync(ct);
        }

        _logger.LogInformation("Starting migration from {SqlitePath} to PostgreSQL", _options.SqlitePath);

        // Order matters: Users → Products → Invoices (with lines/payments) → PayLater
        await MigrateUsersAsync(sqliteConnection, context, ct);
        await MigrateProductsAsync(sqliteConnection, context, ct);
        await MigrateInvoicesAsync(sqliteConnection, context, ct);
        await MigratePayLaterAsync(sqliteConnection, context, ct);

        if (!_options.DryRun)
        {
            await context.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Migration completed. Migrated: {Count}, Errors: {Errors}",
            _result.TotalMigrated, _result.Errors.Count);

        return _result;
    }

    private async Task MigrateUsersAsync(SQLiteConnection sqlite, StoreHubDbContext context, CancellationToken ct)
    {
        _logger.LogInformation("Migrating users...");

        var users = (await sqlite.QueryAsync<LegacyUser>("""
            SELECT u.UserId, u.FirstName, u.LastName, u.RoleId, u.DateCreated, u.DateUpdated,
                   c.Username, c.Password
            FROM User u
            LEFT JOIN UserCredential c ON u.UserId = c.UserId
            """)).ToList();

        foreach (var user in users)
        {
            try
            {
                if (string.IsNullOrEmpty(user.Username))
                {
                    _logger.LogWarning("User {UserId} has no credentials, skipping", user.UserId);
                    _result.Users.Skipped++;
                    continue;
                }

                // Check if already exists
                var existing = await context.StoreUsers
                    .FirstOrDefaultAsync(u => u.Username == user.Username, ct);

                if (existing is not null)
                {
                    _logger.LogDebug("User {Username} already exists", user.Username);
                    _result.Users.Skipped++;
                    _result.UserIdMap[(int)user.UserId] = existing.Id;
                    continue;
                }

                var newUser = new StoreUser
                {
                    Id = Guid.NewGuid(),
                    StoreId = _options.StoreId,
                    LegacyUserId = (int)user.UserId,
                    Username = user.Username,
                    PasswordHash = user.Password, // Keep legacy hash, will be upgraded on first login
                    PasswordHashVersion = 1, // Legacy TripleDES
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleId = (int)user.RoleId,
                    IsActive = true,
                    CreatedAtUtc = ParseDate(user.DateCreated) ?? DateTime.UtcNow,
                    LastModifiedAtUtc = DateTime.UtcNow
                };

                if (!_options.DryRun)
                {
                    context.StoreUsers.Add(newUser);
                }

                _result.Users.Migrated++;
                _result.UserIdMap[(int)user.UserId] = newUser.Id;
                _logger.LogDebug("Migrated user: {Username}", user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate user {UserId}", user.UserId);
                _result.Users.Failed++;
                _result.Errors.Add($"User {user.UserId}: {ex.Message}");
            }
        }
    }

    private async Task MigrateProductsAsync(SQLiteConnection sqlite, StoreHubDbContext context, CancellationToken ct)
    {
        _logger.LogInformation("Migrating products...");

        var products = (await sqlite.QueryAsync<LegacyProduct>("""
            SELECT InventoryProductId, Barcode, Description, Manufacturer, Brand, Category,
                   UnitPrice, QuantityInStock, GroupPrice, GroupPriceQuantity, IsTrackable,
                   DateCreated, DateUpdated
            FROM InventoryProduct
            """)).ToList();

        foreach (var product in products)
        {
            try
            {
                // Check if already exists by barcode
                var existing = await context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == product.Barcode, ct);

                if (existing is not null)
                {
                    _logger.LogDebug("Product {Barcode} already exists", product.Barcode);
                    _result.Products.Skipped++;
                    _result.ProductIdMap[(int)product.InventoryProductId] = existing.Id;
                    continue;
                }

                var createdUtc = ParseDate(product.DateCreated) ?? DateTime.UtcNow;
                var newProduct = new Product
                {
                    Id = Guid.NewGuid(),
                    StoreId = _options.StoreId,
                    Barcode = Truncate(product.Barcode, 50),
                    Name = Truncate(product.Description, 50),
                    Description = Truncate(product.Description, 200),
                    Manufacturer = NullIfEmpty(product.Manufacturer) is { } m ? Truncate(m, 200) : null,
                    Brand = NullIfEmpty(product.Brand) is { } b ? Truncate(b, 200) : null,
                    Category = product.Category?.ToString() is { } c ? Truncate(c, 100) : null,
                    UnitPrice = (decimal)product.UnitPrice,
                    GroupPrice = product.GroupPrice > 0 ? (decimal)product.GroupPrice : null,
                    GroupPriceQuantity = product.GroupPriceQuantity > 0 ? (int)product.GroupPriceQuantity : null,
                    IsActive = true,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = ParseDate(product.DateUpdated) ?? createdUtc
                };

                if (!_options.DryRun)
                {
                    context.Products.Add(newProduct);

                    // Create initial inventory movement for current stock
                    if (product.QuantityInStock > 0)
                    {
                        context.InventoryMovements.Add(new InventoryMovement
                        {
                            Id = Guid.NewGuid(),
                            StoreId = _options.StoreId,
                            ProductId = newProduct.Id,
                            QuantityDelta = (int)product.QuantityInStock,
                            Reason = "Migration:InitialStock",
                            CreatedUtc = createdUtc
                        });
                    }
                }

                _result.Products.Migrated++;
                _result.ProductIdMap[(int)product.InventoryProductId] = newProduct.Id;
                _logger.LogDebug("Migrated product: {Barcode}", product.Barcode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate product {Barcode}", product.Barcode);
                _result.Products.Failed++;
                _result.Errors.Add($"Product {product.Barcode}: {ex.Message}");
            }
        }
    }

    private async Task MigrateInvoicesAsync(SQLiteConnection sqlite, StoreHubDbContext context, CancellationToken ct)
    {
        _logger.LogInformation("Migrating invoices...");

        var invoices = (await sqlite.QueryAsync<LegacyInvoice>("""
            SELECT InvoiceId, UserId, Total, DateCreated
            FROM Invoice
            """)).ToList();

        foreach (var invoice in invoices)
        {
            try
            {
                // Map user ID
                if (!_result.UserIdMap.TryGetValue((int)invoice.UserId, out var userId))
                {
                    _logger.LogWarning("User {UserId} not found in mapping for invoice {InvoiceId}",
                        invoice.UserId, invoice.InvoiceId);
                    userId = _result.UserIdMap.Values.FirstOrDefault();
                }

                var createdUtc = ParseDate(invoice.DateCreated) ?? DateTime.UtcNow;
                var newInvoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    StoreId = _options.StoreId,
                    UserId = userId,
                    TotalAmount = (decimal)invoice.Total,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = createdUtc
                };

                // Get and migrate invoice lines
                var lines = await sqlite.QueryAsync<LegacyInvoiceLine>("""
                    SELECT InvoiceProductId, InvoiceId, InventoryProductId, Barcode, Description, Quantity, UnitPrice
                    FROM InvoiceProduct
                    WHERE InvoiceId = @InvoiceId
                    """, new { invoice.InvoiceId });

                // Get and migrate payments
                var payments = await sqlite.QueryAsync<LegacyPayment>("""
                    SELECT PaymentId, InvoiceId, PaymentTypeId, Amount, Note
                    FROM Payment
                    WHERE InvoiceId = @InvoiceId
                    """, new { invoice.InvoiceId });

                if (!_options.DryRun)
                {
                    context.Invoices.Add(newInvoice);

                    foreach (var line in lines)
                    {
                        if (!_result.ProductIdMap.TryGetValue((int)line.InventoryProductId, out var productId))
                        {
                            _logger.LogWarning("Product {ProductId} not found for invoice line", line.InventoryProductId);
                            continue;
                        }

                        context.InvoiceLines.Add(new InvoiceLine
                        {
                            Id = Guid.NewGuid(),
                            InvoiceId = newInvoice.Id,
                            ProductId = productId,
                            ProductName = Truncate(line.Description, 200),
                            Quantity = (int)line.Quantity,
                            UnitPrice = (decimal)line.UnitPrice,
                            CreatedUtc = createdUtc
                        });

                        // Create inventory movement for sale
                        context.InventoryMovements.Add(new InventoryMovement
                        {
                            Id = Guid.NewGuid(),
                            StoreId = _options.StoreId,
                            ProductId = productId,
                            QuantityDelta = -(int)line.Quantity,
                            Reason = "Migration:Sale",
                            ReferenceId = newInvoice.Id,
                            CreatedUtc = createdUtc
                        });
                    }

                    foreach (var payment in payments)
                    {
                        var method = LegacyPaymentTypeMap.ToCode((int)payment.PaymentTypeId);
                        if (method is null)
                        {
                            // Refused, never guessed. The previous fallback wrote "Other", which
                            // is not a catalogue code, so the amount became unresolvable while
                            // the row counts still reconciled.
                            _result.Errors.Add(
                                $"Invoice {invoice.InvoiceId} payment {payment.PaymentId}: legacy " +
                                $"PaymentTypeId {payment.PaymentTypeId} has no payment-method code. " +
                                $"Migrating it would misattribute {payment.Amount:N2}.");
                            _result.Payments.Failed++;
                            continue;
                        }

                        context.Payments.Add(new Payment
                        {
                            Id = Guid.NewGuid(),
                            InvoiceId = newInvoice.Id,
                            Method = method,
                            Amount = (decimal)payment.Amount,
                            Note = payment.Note,
                            CreatedUtc = createdUtc
                        });
                        _result.Payments.Migrated++;
                    }
                }

                _result.Invoices.Migrated++;
                _result.InvoiceIdMap[(int)invoice.InvoiceId] = newInvoice.Id;
                _logger.LogDebug("Migrated invoice: {InvoiceId}", invoice.InvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate invoice {InvoiceId}", invoice.InvoiceId);
                _result.Invoices.Failed++;
                _result.Errors.Add($"Invoice {invoice.InvoiceId}: {ex.Message}");
            }
        }
    }

    private async Task MigratePayLaterAsync(SQLiteConnection sqlite, StoreHubDbContext context, CancellationToken ct)
    {
        // Defect 3: PayLater is a GeneralHardware-only feature. Minimart and MimyShop have no such
        // table, so querying it unconditionally threw "no such table: PayLater" and -- because that
        // throw escaped before SaveChangesAsync -- discarded the entire migration.
        var hasPayLaterTable = await sqlite.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'PayLater'") > 0;

        if (!hasPayLaterTable)
        {
            _logger.LogInformation(
                "No PayLater table in this store; skipping. PayLater is a GeneralHardware-only feature.");
            return;
        }

        _logger.LogInformation("Migrating PayLater records...");

        // Defect 2: the previous SELECT named PayLaterId, UserId, CustomerName and PaymentAmount.
        // None exist. PayLater is a 1:1 extension of Payment (its PK IS the payment's id), so it
        // needs no id of its own, no user (the payment's invoice has one) and no amount column
        // beyond the debt it is tracking.
        var payLaters = (await sqlite.QueryAsync<LegacyPayLater>("""
            SELECT PaymentId, Description, InvoiceId, IsCompleted, DateCreated, DateUpdated,
                   PayLaterAmount, PaidAmount
            FROM PayLater
            """)).ToList();

        foreach (var payLater in payLaters)
        {
            try
            {
                // Map invoice ID
                if (!_result.InvoiceIdMap.TryGetValue((int)payLater.InvoiceId, out var invoiceId))
                {
                    _logger.LogWarning("Invoice {InvoiceId} not found for PayLater {PaymentId}",
                        payLater.InvoiceId, payLater.PaymentId);
                    _result.PayLater.Failed++;
                    continue;
                }

                var createdUtc = ParseDate(payLater.DateCreated) ?? DateTime.UtcNow;

                // First, we need to find or create a payment for this PayLater
                var paymentId = Guid.NewGuid();
                if (!_options.DryRun)
                {
                    // Create the payment record for PayLater
                    context.Payments.Add(new Payment
                    {
                        Id = paymentId,
                        InvoiceId = invoiceId,
                        Method = PaymentMethodCodes.PayLater,
                        Amount = (decimal)payLater.PayLaterAmount,
                        Note = payLater.Description,
                        CreatedUtc = createdUtc
                    });
                }

                var newPayLater = new PayLater
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentId,
                    InvoiceId = invoiceId,
                    Description = Truncate(payLater.Description ?? string.Empty, 500),
                    PayLaterAmount = (decimal)payLater.PayLaterAmount,
                    // Read, never derived. The customer repays in instalments, so this column is
                    // the only record of that progress -- Installments is empty in every store.
                    PaidAmount = (decimal)payLater.PaidAmount,
                    IsCompleted = payLater.IsCompleted == 1,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = ParseDate(payLater.DateUpdated) ?? createdUtc
                };

                if (!_options.DryRun)
                {
                    context.PayLaters.Add(newPayLater);
                }

                _result.PayLater.Migrated++;
                _logger.LogDebug("Migrated PayLater: {PaymentId}", payLater.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate PayLater {PaymentId}", payLater.PaymentId);
                _result.PayLater.Failed++;
                _result.Errors.Add($"PayLater {payLater.PaymentId}: {ex.Message}");
            }
        }
    }

    public async Task<CloudSyncResult> SyncToCloudAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.CloudApiUrl))
        {
            return new CloudSyncResult { IsSuccess = false, Error = "Cloud API URL not configured" };
        }

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(_options.CloudApiUrl) };

            // Get OAuth token
            var tokenResponse = await httpClient.PostAsJsonAsync("/connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.CloudClientId!,
                    ["client_secret"] = _options.CloudClientSecret!,
                    ["scope"] = "sync.write"
                }), ct);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                return new CloudSyncResult { IsSuccess = false, Error = "Failed to get OAuth token" };
            }

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult?.AccessToken);

            // Build bulk migration request with actual migrated data
            var bulkRequest = await BuildBulkMigrationRequestAsync(ct);

            _logger.LogInformation("Sending bulk migration to cloud: {Users} users, {Products} products, {Invoices} invoices",
                bulkRequest.Users.Count, bulkRequest.Products.Count, bulkRequest.Invoices.Count);

            var syncResponse = await httpClient.PostAsJsonAsync("/sync/bulk-migration", bulkRequest, ct);

            if (syncResponse.IsSuccessStatusCode)
            {
                var result = await syncResponse.Content.ReadFromJsonAsync<BulkMigrationResponse>(ct);
                return new CloudSyncResult
                {
                    IsSuccess = result?.Success ?? false,
                    EventsSent = (result?.UsersImported ?? 0) + (result?.ProductsImported ?? 0) + (result?.InvoicesImported ?? 0)
                };
            }

            var errorContent = await syncResponse.Content.ReadAsStringAsync(ct);
            return new CloudSyncResult
            {
                IsSuccess = false,
                Error = $"Sync failed: {syncResponse.StatusCode} - {errorContent}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud sync failed");
            return new CloudSyncResult { IsSuccess = false, Error = ex.Message };
        }
    }

    private async Task<BulkMigrationRequest> BuildBulkMigrationRequestAsync(CancellationToken ct)
    {
        var dbOptions = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(_options.PostgresConnectionString)
            .Options;

        await using var context = new StoreHubDbContext(dbOptions);

        // Load migrated data from PostgreSQL
        var users = await context.StoreUsers
            .Where(u => u.StoreId == _options.StoreId)
            .Select(u => new MigratedUser(u.Id, u.Username, u.FirstName, u.LastName, u.RoleId, u.IsActive))
            .ToListAsync(ct);

        var products = await context.Products
            .Select(p => new MigratedProduct(
                p.Id, p.Barcode, p.Name, p.Description, p.Manufacturer, p.Brand, p.Category,
                p.UnitPrice, p.GroupPrice, p.GroupPriceQuantity, p.IsActive))
            .ToListAsync(ct);

        var invoices = await context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.StoreId == _options.StoreId)
            .Select(i => new MigratedInvoice(
                i.Id,
                i.UserId,
                i.TotalAmount,
                i.CreatedUtc,
                i.Lines.Select(l => new MigratedInvoiceLine(l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList(),
                i.Payments.Select(p => new MigratedPayment(p.Id, p.Method, p.Amount, p.Note)).ToList()))
            .ToListAsync(ct);

        return new BulkMigrationRequest(_options.StoreId, users, products, invoices);
    }

    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        return DateTime.TryParse(dateString, out var result)
            ? DateTime.SpecifyKind(result, DateTimeKind.Utc)
            : null;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    // Legacy entity classes for Dapper
    private class LegacyUser
    {
        public long UserId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public long RoleId { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? DateCreated { get; set; }
        public string? DateUpdated { get; set; }
    }

    private class LegacyProduct
    {
        public long InventoryProductId { get; set; }
        public string Barcode { get; set; } = "";
        public string Description { get; set; } = "";
        public string? Manufacturer { get; set; }
        public string? Brand { get; set; }
        public long? Category { get; set; }
        public double UnitPrice { get; set; }
        public long QuantityInStock { get; set; }
        public double GroupPrice { get; set; }
        public long GroupPriceQuantity { get; set; }
        public long IsTrackable { get; set; }
        public string? DateCreated { get; set; }
        public string? DateUpdated { get; set; }
    }

    private class LegacyInvoice
    {
        public long InvoiceId { get; set; }
        public long UserId { get; set; }
        public double Total { get; set; }
        public string? DateCreated { get; set; }
    }

    private class LegacyInvoiceLine
    {
        public long InvoiceProductId { get; set; }
        public long InvoiceId { get; set; }
        public long InventoryProductId { get; set; }
        public string Barcode { get; set; } = "";
        public string Description { get; set; } = "";
        public long Quantity { get; set; }
        public double UnitPrice { get; set; }
    }

    private class LegacyPayment
    {
        public long PaymentId { get; set; }
        public long InvoiceId { get; set; }
        public long PaymentTypeId { get; set; }
        public double Amount { get; set; }
        public string? Note { get; set; }
    }

    private class LegacyPayLater
    {
        /// <summary>Both the primary key and the FK to <c>Payment.PaymentId</c>.</summary>
        public long PaymentId { get; set; }
        public long InvoiceId { get; set; }

        /// <summary>The customer who owes the debt. Mirrored in <c>Payment.Note</c>.</summary>
        public string? Description { get; set; }
        public double PayLaterAmount { get; set; }
        public double PaidAmount { get; set; }
        public long IsCompleted { get; set; }
        public string? DateCreated { get; set; }
        public string? DateUpdated { get; set; }
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
    }
}
