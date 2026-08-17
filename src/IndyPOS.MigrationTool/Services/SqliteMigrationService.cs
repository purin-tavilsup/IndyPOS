using System.Data.SQLite;
using System.Globalization;
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
    /// <summary>The only movement reason the migration writes.</summary>
    private const string InitialStockReason = "Migration:InitialStock";

    private readonly MigrationOptions _options;
    private readonly ILogger<SqliteMigrationService> _logger;
    private MigrationResult _result = new();

    /// <summary>
    /// When this run started. Migration:InitialStock records stock as observed AT CUTOVER, so every
    /// such movement in a run carries this one timestamp rather than the product's creation date.
    /// Initialised here as well as in MigrateAllAsync so it is never default(DateTime).
    /// </summary>
    private DateTime _migrationStartedUtc = DateTime.UtcNow;

    /// <summary>
    /// Barcode -> migrated product id. A barcode identifies the physical article, so this is what
    /// lets an invoice line whose legacy product was deleted find the product the shopkeeper
    /// re-added in its place. See <see cref="ResolveLineProductId"/>.
    /// </summary>
    private readonly Dictionary<string, Guid> _productIdByBarcode = new();

    public SqliteMigrationService(MigrationOptions options, ILogger<SqliteMigrationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateAllAsync(CancellationToken ct = default)
    {
        _result = new MigrationResult();
        _productIdByBarcode.Clear();
        _migrationStartedUtc = DateTime.UtcNow;

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
        // Defect 12: each phase is isolated so ONE run reports every schema problem. Re-running
        // against a real shop costs a visit with the till switched off.
        await RunPhaseAsync("Users", () => MigrateUsersAsync(sqliteConnection, context, ct));
        await RunPhaseAsync("Products", () => MigrateProductsAsync(sqliteConnection, context, ct));
        await RunPhaseAsync("Invoices", () => MigrateInvoicesAsync(sqliteConnection, context, ct));
        await RunPhaseAsync("PayLater", () => MigratePayLaterAsync(sqliteConnection, context, ct));

        // Isolating the diagnosis must NOT isolate the transaction. If any phase failed the run is
        // not trustworthy, so nothing is written -- the same outcome as before, reached deliberately
        // instead of by an exception escaping past this line. A half-migrated store that reported
        // success would be far worse than the crash this replaces.
        if (!_options.DryRun && _result.PhaseFailures.Count == 0)
        {
            await context.SaveChangesAsync(ct);
        }

        // The log is what gets read during a support call, so it must not say "completed" for a run
        // that wrote nothing -- the same trap the console banner exists to avoid.
        if (_result.PhaseFailures.Count > 0)
        {
            _logger.LogError(
                "Migration ABORTED. Nothing was written. {PhaseCount} phase(s) failed: {Phases}. " +
                "{Count} row(s) were processed in memory and discarded.",
                _result.PhaseFailures.Count,
                string.Join(", ", _result.PhaseFailures.Select(f => f.Phase)),
                _result.TotalMigrated);
        }
        else
        {
            _logger.LogInformation("Migration completed. Migrated: {Count}, Errors: {Errors}",
                _result.TotalMigrated, _result.Errors.Count);
        }

        return _result;
    }

    /// <summary>
    /// Runs one migration phase, turning a phase-level throw into a recorded failure so the remaining
    /// phases still run and report their own state.
    /// </summary>
    /// <remarks>
    /// Nothing is swallowed: a recorded failure makes <see cref="MigrationResult.IsSuccess"/> false,
    /// which is the process exit code, prints an ABORTED banner, and suppresses the save.
    /// </remarks>
    private async Task RunPhaseAsync(string phase, Func<Task> migratePhase)
    {
        try
        {
            await migratePhase();
        }
        catch (OperationCanceledException)
        {
            // An operator cancelling is not a defect in the store's schema.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Migration phase {Phase} failed. Nothing will be persisted for this run.", phase);
            _result.AddPhaseFailure(phase, ex.Message);
        }
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
                _result.AddError("Users", $"User {user.UserId}: {ex.Message}");
            }
        }
    }

    private async Task MigrateProductsAsync(SQLiteConnection sqlite, StoreHubDbContext context, CancellationToken ct)
    {
        _logger.LogInformation("Migrating products...");

        var categories = await LegacyCategoryResolver.LoadAsync(sqlite);

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
                    _productIdByBarcode[existing.Barcode] = existing.Id;
                    continue;
                }

                var createdUtc = ParseDate(product.DateCreated) ?? DateTime.UtcNow;
                var newProduct = new Product
                {
                    Id = Guid.NewGuid(),
                    StoreId = _options.StoreId,
                    Barcode = LegacyBarcode.ToStored(product.Barcode),
                    Name = Truncate(product.Description, 50),
                    Description = Truncate(product.Description, 200),
                    Manufacturer = NullIfEmpty(product.Manufacturer) is { } m ? Truncate(m, 200) : null,
                    Brand = NullIfEmpty(product.Brand) is { } b ? Truncate(b, 200) : null,
                    Category = ResolveCategory(product, categories),
                    UnitPrice = (decimal)product.UnitPrice,
                    GroupPrice = product.GroupPrice > 0 ? (decimal)product.GroupPrice : null,
                    GroupPriceQuantity = product.GroupPriceQuantity > 0 ? (int)product.GroupPriceQuantity : null,
                    IsActive = true,
                    CreatedUtc = createdUtc,
                    LastModifiedUtc = ParseDate(product.DateUpdated) ?? createdUtc
                };

                // Recorded in BOTH modes on purpose: a dry run exists to preview what a real run
                // would do, and clamping stock is the one thing it does that the operator must
                // decide about beforehand.
                if (product.QuantityInStock < 0)
                {
                    _result.AddClampedStock(
                        newProduct.Barcode, newProduct.Name, (int)product.QuantityInStock);
                    _logger.LogWarning(
                        "Product {Barcode} has negative legacy stock {Quantity}; migrating as 0",
                        newProduct.Barcode, product.QuantityInStock);
                }

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
                            Reason = InitialStockReason,
                            CreatedUtc = _migrationStartedUtc
                        });
                    }
                }

                _result.Products.Migrated++;
                _result.ProductIdMap[(int)product.InventoryProductId] = newProduct.Id;
                _productIdByBarcode[newProduct.Barcode] = newProduct.Id;
                _logger.LogDebug("Migrated product: {Barcode}", product.Barcode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate product {Barcode}", product.Barcode);
                _result.Products.Failed++;
                _result.AddError("Products", $"Product {product.Barcode}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Resolves a product's category, reporting anything it could not resolve.
    /// </summary>
    /// <remarks>
    /// An unresolved category is recorded but does NOT fail the row: losing one product's category
    /// is no reason to refuse its price, stock and sales history as well.
    /// </remarks>
    private string? ResolveCategory(LegacyProduct product, LegacyCategoryResolver categories)
    {
        var (code, problem) = categories.Resolve(product.Category);

        if (problem is not null)
        {
            _result.AddError("Products",
                $"Product {product.Barcode}: {problem} Migrated with no category.");
        }

        return code;
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
                        var productId = ResolveLineProductId(line, context, createdUtc);

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

                        // Defect 14: NO inventory movement for a historical sale. The product's
                        // QuantityInStock is today's stock, already net of every sale, so replaying
                        // lines here subtracts each sold unit a second time. The sale itself is not
                        // lost -- it is the InvoiceLine written above.
                    }
                }

                foreach (var payment in payments)
                {
                    var method = LegacyPaymentTypeMap.ToCode((int)payment.PaymentTypeId);
                    if (method is null)
                    {
                        // Refused, never guessed. The previous fallback wrote "Other", which
                        // is not a catalogue code, so the amount became unresolvable while
                        // the row counts still reconciled.
                        _result.AddError("Invoices",
                            $"Invoice {invoice.InvoiceId} payment {payment.PaymentId}: legacy " +
                            $"PaymentTypeId {payment.PaymentTypeId} has no payment-method code. " +
                            $"Migrating it would misattribute {payment.Amount:N2}.");
                        _result.Payments.Failed++;
                        continue;
                    }

                    var newPayment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = newInvoice.Id,
                        Method = method,
                        Amount = (decimal)payment.Amount,
                        Note = payment.Note,
                        CreatedUtc = createdUtc
                    };

                    if (!_options.DryRun)
                    {
                        context.Payments.Add(newPayment);
                    }

                    // Built in dry-run too: MigratePayLaterAsync resolves against this map, and an
                    // empty map would make every PayLater row fail its lookup.
                    _result.PaymentIdMap[(int)payment.PaymentId] = newPayment.Id;
                    _result.Payments.Migrated++;
                }

                _result.Invoices.Migrated++;
                _result.InvoiceIdMap[(int)invoice.InvoiceId] = newInvoice.Id;
                _logger.LogDebug("Migrated invoice: {InvoiceId}", invoice.InvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate invoice {InvoiceId}", invoice.InvoiceId);
                _result.Invoices.Failed++;
                _result.AddError("Invoices", $"Invoice {invoice.InvoiceId}: {ex.Message}");
            }
        }
    }

    private async Task MigratePayLaterAsync(SQLiteConnection sqlite, StoreHubDbContext context, CancellationToken ct)
    {
        // Defect 3: PayLater is a GeneralHardware-only feature. Minimart and MimyShop have no such
        // table, so querying it unconditionally threw "no such table: PayLater" and -- because that
        // throw escaped before SaveChangesAsync -- discarded the entire migration.
        var hasPayLaterTable = await LegacySchemaProbe.HasTableAsync(sqlite, "PayLater");

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

                // Defect 10: the legacy Payment row IS this money. PayLater is its 1:1 extension,
                // keyed by the same id. Creating a payment here double-counted every credit sale
                // -- THB 836,013 across the 5,181 real rows.
                if (!_result.PaymentIdMap.TryGetValue((int)payLater.PaymentId, out var paymentId))
                {
                    _result.AddError("PayLater",
                        $"PayLater {payLater.PaymentId}: no migrated payment for legacy PaymentId " +
                        $"{payLater.PaymentId}, so the debt of {payLater.PayLaterAmount:N2} cannot be " +
                        $"attached. Refusing rather than inventing a payment.");
                    _result.PayLater.Failed++;
                    continue;
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
                _result.AddError("PayLater", $"PayLater {payLater.PaymentId}: {ex.Message}");
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

    /// <summary>
    /// Resolves the product a historical invoice line belongs to, in three tiers: the legacy id,
    /// then the barcode of a product still in the catalogue, then a synthesised inactive
    /// placeholder. It never fails to resolve — dropping the line is what defect 13 was, and it let
    /// an invoice's lines sum to less than the invoice's own total.
    /// </summary>
    private Guid ResolveLineProductId(LegacyInvoiceLine line, StoreHubDbContext context, DateTime createdUtc)
    {
        if (_result.ProductIdMap.TryGetValue((int)line.InventoryProductId, out var byLegacyId))
            return byLegacyId;

        var barcode = LegacyBarcode.ToStored(line.Barcode);

        // The product was deleted and re-added under a new legacy id. Same barcode means the same
        // physical article, so the line belongs with it — and one article keeps one sales history.
        if (_productIdByBarcode.TryGetValue(barcode, out var byBarcode))
        {
            _logger.LogDebug(
                "Invoice line for deleted product {LegacyProductId} relinked by barcode {Barcode}",
                line.InventoryProductId, barcode);
            return byBarcode;
        }

        var placeholder = new Product
        {
            Id = Guid.NewGuid(),
            StoreId = _options.StoreId,
            Barcode = barcode,
            Name = Truncate(line.Description, 50),
            Description = Truncate(line.Description, 200),
            UnitPrice = (decimal)line.UnitPrice,
            IsActive = false,
            CreatedUtc = createdUtc,
            LastModifiedUtc = createdUtc
        };

        context.Products.Add(placeholder);

        // Registering the barcode makes every later line for this article reuse the one placeholder,
        // which is also what keeps it from colliding on the (StoreId, Barcode) unique index.
        _productIdByBarcode[barcode] = placeholder.Id;

        _logger.LogInformation(
            "Deleted product {LegacyProductId} ({Barcode}) restored as an inactive placeholder so its invoice lines survive",
            line.InventoryProductId, barcode);

        return placeholder.Id;
    }

    /// <summary>
    /// Legacy timestamps are written by SQLite's datetime('now','localtime') on a till standing in
    /// Thailand, so every value is Bangkok wall-clock time with no offset recorded.
    /// </summary>
    private static readonly TimeZoneInfo StoreTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// Parses a legacy timestamp as Thai local time and returns it as UTC.
    /// InvariantCulture is required: the tool runs on a th-TH till, whose Buddhist calendar would
    /// otherwise read 2024 as a Buddhist-era year and land every row in 1481 AD.
    /// </summary>
    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;

        if (!DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return null;

        var storeLocal = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(storeLocal, StoreTimeZone);
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
