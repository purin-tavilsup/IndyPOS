using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.PaymentsReports.Create;

public record CreatePaymentsReportCommand(int InvoiceId) : ICommand;