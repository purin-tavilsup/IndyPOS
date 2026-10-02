namespace IndyPOS.Application.UseCases.StoreHub.Sales;

/// <summary>
/// Request to complete a sale in StoreHub. It carries no user: StoreHub records the sale under the
/// user in the caller's token. An older till that still sends "userId" is not refused, because
/// System.Text.Json ignores a member the record does not have.
/// </summary>
public record CompleteSaleRequest(
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
