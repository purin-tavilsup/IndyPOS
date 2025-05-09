using Nokpirab;

namespace IndyPOS.Application.UseCases.PaymentsReports.Create;

public record CreatePaymentsReportCommand(int InvoiceId) : ICommand;