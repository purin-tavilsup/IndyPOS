using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.Reprints;

/// <param name="UserId">From the token, never the body.</param>
/// <param name="CanViewAnyDay">reports.view: managers reprint any day, cashiers only today.</param>
public record CreateInvoiceReprintCommand(Guid InvoiceId, Guid UserId, bool CanViewAnyDay) : ICommand<InvoiceReprintResultDto>;
