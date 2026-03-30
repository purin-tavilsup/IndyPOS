using Nokpirab;

namespace IndyPOS.Application.UseCases.Cloud.Sync.BulkMigration;

/// <summary>
/// Command to bulk migrate data from SQLite to Cloud.
/// Used by the MigrationTool after loading data into StoreHub PostgreSQL.
/// </summary>
public record BulkMigrationCommand(
    string StoreId,
    IReadOnlyList<MigratedUser> Users,
    IReadOnlyList<MigratedProduct> Products,
    IReadOnlyList<MigratedInvoice> Invoices) : ICommand<BulkMigrationResponse>;

/// <summary>
/// User data for migration sync.
/// </summary>
public record MigratedUser(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    bool IsActive);

/// <summary>
/// Product data for migration sync.
/// </summary>
public record MigratedProduct(
    Guid Id,
    string Barcode,
    string Name,
    string? Description,
    string? Manufacturer,
    string? Brand,
    string? Category,
    decimal UnitPrice,
    decimal? GroupPrice,
    int? GroupPriceQuantity,
    bool IsActive);

/// <summary>
/// Invoice data for migration sync.
/// </summary>
public record MigratedInvoice(
    Guid Id,
    Guid UserId,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<MigratedInvoiceLine> Lines,
    IReadOnlyList<MigratedPayment> Payments);

/// <summary>
/// Invoice line data for migration sync.
/// </summary>
public record MigratedInvoiceLine(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Payment data for migration sync.
/// </summary>
public record MigratedPayment(
    Guid Id,
    string Method,
    decimal Amount,
    string? Note);

/// <summary>
/// Response from bulk migration.
/// </summary>
public record BulkMigrationResponse(
    bool Success,
    int UsersImported,
    int ProductsImported,
    int InvoicesImported,
    IReadOnlyList<string> Errors);

/// <summary>
/// Request DTO for bulk migration endpoint.
/// </summary>
public record BulkMigrationRequest(
    string StoreId,
    List<MigratedUser> Users,
    List<MigratedProduct> Products,
    List<MigratedInvoice> Invoices);
