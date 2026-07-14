using IndyPOS.Application.UseCases.Cloud.Sync.BulkMigration;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Nokpirab;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Handles bulk migration of data from SQLite stores to Cloud PostgreSQL.
/// This is designed for one-time migration, not ongoing sync.
/// </summary>
public class BulkMigrationCommandHandler : ICommandHandler<BulkMigrationCommand, BulkMigrationResponse>
{
    private readonly CloudDbContext _dbContext;
    private readonly ILogger<BulkMigrationCommandHandler> _logger;

    public BulkMigrationCommandHandler(CloudDbContext dbContext, ILogger<BulkMigrationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BulkMigrationResponse> HandleAsync(BulkMigrationCommand command, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var usersImported = 0;
        var productsImported = 0;
        var invoicesImported = 0;
        var now = DateTime.UtcNow;

        _logger.LogInformation("Starting bulk migration for store {StoreId}: {Users} users, {Products} products, {Invoices} invoices",
            command.StoreId, command.Users.Count, command.Products.Count, command.Invoices.Count);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Import Users
            foreach (var user in command.Users)
            {
                try
                {
                    var exists = await _dbContext.Users.AnyAsync(u => u.Id == user.Id, cancellationToken);
                    if (exists)
                    {
                        _logger.LogDebug("User {UserId} already exists, skipping", user.Id);
                        continue;
                    }

                    var cloudUser = new CloudUser
                    {
                        Id = user.Id,
                        StoreId = command.StoreId,
                        Username = user.Username,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        RoleId = user.RoleId,
                        IsActive = user.IsActive,
                        Version = 1,
                        CreatedAtUtc = now,
                        LastModifiedAtUtc = now
                    };

                    _dbContext.Users.Add(cloudUser);
                    usersImported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User {user.Username}: {ex.Message}");
                    _logger.LogError(ex, "Failed to import user {Username}", user.Username);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Import Products
            foreach (var product in command.Products)
            {
                try
                {
                    var exists = await _dbContext.Products.AnyAsync(p => p.Id == product.Id, cancellationToken);
                    if (exists)
                    {
                        _logger.LogDebug("Product {ProductId} already exists, skipping", product.Id);
                        continue;
                    }

                    var cloudProduct = new CloudProduct
                    {
                        Id = product.Id,
                        Barcode = product.Barcode,
                        Name = product.Name,
                        Description = product.Description,
                        Manufacturer = product.Manufacturer,
                        Brand = product.Brand,
                        Category = product.Category,
                        UnitPrice = product.UnitPrice,
                        GroupPrice = product.GroupPrice,
                        GroupPriceQuantity = product.GroupPriceQuantity,
                        IsActive = product.IsActive,
                        LastModifiedAtUtc = now
                    };

                    _dbContext.Products.Add(cloudProduct);
                    productsImported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Product {product.Barcode}: {ex.Message}");
                    _logger.LogError(ex, "Failed to import product {Barcode}", product.Barcode);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 3. Import Invoices (with lines and payments)
            foreach (var invoice in command.Invoices)
            {
                try
                {
                    var exists = await _dbContext.Invoices.AnyAsync(i => i.Id == invoice.Id, cancellationToken);
                    if (exists)
                    {
                        _logger.LogDebug("Invoice {InvoiceId} already exists, skipping", invoice.Id);
                        continue;
                    }

                    var cloudInvoice = new CloudInvoice
                    {
                        Id = invoice.Id,
                        StoreId = command.StoreId,
                        UserId = invoice.UserId,
                        TotalAmount = invoice.TotalAmount,
                        CreatedAtUtc = invoice.CreatedAtUtc,
                        SyncedAtUtc = now
                    };

                    _dbContext.Invoices.Add(cloudInvoice);

                    // Add invoice lines
                    foreach (var line in invoice.Lines)
                    {
                        var cloudLine = new CloudInvoiceLine
                        {
                            Id = line.Id,
                            InvoiceId = invoice.Id,
                            ProductId = line.ProductId,
                            ProductName = line.ProductName,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice
                        };
                        _dbContext.InvoiceLines.Add(cloudLine);
                    }

                    // Add payments
                    foreach (var payment in invoice.Payments)
                    {
                        var cloudPayment = new CloudPayment
                        {
                            Id = payment.Id,
                            InvoiceId = invoice.Id,
                            Method = payment.Method,
                            Amount = payment.Amount,
                            Note = payment.Note
                        };
                        _dbContext.Payments.Add(cloudPayment);
                    }

                    invoicesImported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Invoice {invoice.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to import invoice {InvoiceId}", invoice.Id);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Bulk migration completed for store {StoreId}: {Users} users, {Products} products, {Invoices} invoices imported",
                command.StoreId, usersImported, productsImported, invoicesImported);

            return new BulkMigrationResponse(
                Success: errors.Count == 0,
                UsersImported: usersImported,
                ProductsImported: productsImported,
                InvoicesImported: invoicesImported,
                Errors: errors);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Bulk migration failed for store {StoreId}", command.StoreId);

            errors.Add($"Transaction failed: {ex.Message}");
            return new BulkMigrationResponse(
                Success: false,
                UsersImported: 0,
                ProductsImported: 0,
                InvoicesImported: 0,
                Errors: errors);
        }
    }
}
