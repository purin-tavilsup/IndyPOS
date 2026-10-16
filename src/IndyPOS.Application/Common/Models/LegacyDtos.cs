using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.Common.Models;

// Legacy DTOs - These are stubs for backward compatibility with WinForms report panels.
// They were originally in UseCases/Invoices/, UseCases/InvoiceProducts/, etc.
// The methods that return these DTOs now return empty collections.
// TODO: Migrate WinForms report panels to use StoreHub DTOs directly during MAUI migration.

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
