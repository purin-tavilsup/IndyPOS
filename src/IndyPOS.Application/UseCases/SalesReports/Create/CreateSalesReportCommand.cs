using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.SalesReports.Create;

public record CreateSalesReportCommand(int InvoiceId, bool HasPayLaterPayment) : ICommand;