using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Complete;

/// <summary>
/// Command to complete a sale - creates invoice, lines, payments, and inventory movements.
/// </summary>
public record CompleteSaleCommand(
    string StoreId,
    long UserId,
    IReadOnlyList<SaleLineRequest> Lines,
    IReadOnlyList<SalePaymentRequest> Payments) : ICommand<CompleteSaleResponse>;
