namespace IndyPOS.Application.UseCases.StoreHub.Sales;

/// <summary>
/// Response after completing a sale. InvoiceNumber is the bill number the receipt prints.
/// </summary>
public record CompleteSaleResponse(
    Guid InvoiceId,
    decimal TotalAmount,
    DateTime CreatedUtc,
    long InvoiceNumber);
