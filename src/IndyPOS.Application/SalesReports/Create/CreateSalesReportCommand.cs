using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.SalesReports.Create;

public record CreateSalesReportCommand(int InvoiceId, bool HasPayLaterPayment) : ICommand;