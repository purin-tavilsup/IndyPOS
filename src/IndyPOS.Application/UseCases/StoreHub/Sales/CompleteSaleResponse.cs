namespace IndyPOS.Application.UseCases.StoreHub.Sales;

/// <summary>
/// Response after completing a sale.
/// </summary>
public record CompleteSaleResponse(
    Guid InvoiceId,
    decimal TotalAmount,
    DateTime CreatedUtc);
