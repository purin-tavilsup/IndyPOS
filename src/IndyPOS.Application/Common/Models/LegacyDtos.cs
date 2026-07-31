using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Common.Models;

// Legacy DTOs - These are stubs for backward compatibility with WinForms report panels.
// They were originally in UseCases/Invoices/, UseCases/InvoiceProducts/, etc.
// The methods that return these DTOs now return empty collections.
// TODO: Migrate WinForms report panels to use StoreHub DTOs directly during MAUI migration.

/// <summary>
/// Legacy invoice DTO for report panels.
/// </summary>
public record InvoiceDto(
    int InvoiceId,
    Guid? StoreHubInvoiceId,
    int UserId,
    decimal Total,
    string DateCreated);

/// <summary>
/// Legacy invoice product DTO for report panels.
/// </summary>
public record InvoiceProductDto(
    int InvoiceProductId,
    int Priority,
    int InvoiceId,
    int InventoryProductId,
    string Barcode,
    string Description,
    string Manufacturer,
    string Brand,
    string Category,
    decimal UnitPrice,
    int Quantity,
    string DateCreated,
    string Note,
    decimal GroupPrice,
    bool IsGroupProduct)
{
    /// <summary>
    /// Calculate total for this line item.
    /// </summary>
    public decimal GetTotal() => IsGroupProduct ? GroupPrice : UnitPrice * Quantity;
}

/// <summary>
/// Legacy invoice payment DTO for report panels.
/// </summary>
public record InvoicePaymentDto(
    int InvoicePaymentId,
    int InvoiceId,
    int PaymentTypeId,
    decimal Amount,
    string Note,
    string DateCreated);

/// <summary>
/// Legacy pay-later payment DTO for report panels.
/// </summary>
public record PayLaterPaymentDto(
    int PayLaterPaymentId,
    int InvoiceId,
    string Description,
    decimal InvoiceTotal,
    decimal PaymentMade,
    bool IsCompleted,
    string DateCreated,
    string DateUpdated)
{
    /// <summary>
    /// Total amount receivable (same as InvoiceTotal).
    /// </summary>
    public decimal ReceivableAmount => InvoiceTotal;

    /// <summary>
    /// Amount paid so far.
    /// </summary>
    public decimal PaidAmount => PaymentMade;
}

/// <summary>
/// Implementation of IInvoiceInfo for completed sales.
/// Used by StoreHubSaleService and receipt printing.
/// </summary>
public class InvoiceInfo : IInvoiceInfo
{
    public int Id { get; init; }
    public Guid? StoreHubInvoiceId { get; init; }
    public IList<Product> Products { get; init; } = new List<Product>();
    public IList<Payment> Payments { get; init; } = new List<Payment>();
    public bool IsRefundInvoice { get; init; }
    public decimal InvoiceTotal { get; init; }
    public decimal PaymentTotal { get; init; }
    public decimal Changes { get; init; }
    public bool HasPayLaterPayment { get; init; }
}

/// <summary>
/// Legacy user DTO for user management panels.
/// Note: User management is disabled in StoreHub mode.
/// </summary>
public record UserDto(
    int UserId,
    string FirstName,
    string LastName,
    int RoleId,
    string DateCreated);

/// <summary>
/// Legacy user credential DTO for user management panels.
/// Note: User management is disabled in StoreHub mode.
/// </summary>
public record UserCredentialDto(
    int UserId,
    string Username,
    string Password,
    string DateCreated);
