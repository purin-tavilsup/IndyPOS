namespace IndyPOS.Application.UseCases.StoreHub.Sales;

/// <summary>
/// Request to complete a sale in StoreHub.
/// </summary>
/// <param name="UserId">
/// Deprecated and ignored. StoreHub records the sale under the user in the caller's token; a body
/// value let any caller ring a sale up as someone else. Kept only so existing tills keep sending a
/// valid body.
/// </param>
public record CompleteSaleRequest(
    Guid UserId,
    IReadOnlyList<SaleLineRequest> Lines,
    IReadOnlyList<SalePaymentRequest> Payments);

public record SaleLineRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public record SalePaymentRequest(
    string Method,
    decimal Amount,
    string? Note = null);
