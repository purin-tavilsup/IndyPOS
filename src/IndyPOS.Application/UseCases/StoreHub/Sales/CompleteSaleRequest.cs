namespace IndyPOS.Application.UseCases.StoreHub.Sales;

/// <summary>
/// Request to complete a sale in StoreHub.
/// </summary>
public record CompleteSaleRequest(
    long UserId,
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
