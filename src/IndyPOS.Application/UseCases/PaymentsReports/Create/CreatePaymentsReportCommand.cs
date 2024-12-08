using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.PaymentsReports.Create;

public record CreatePaymentsReportCommand(int InvoiceId) : ICommand;