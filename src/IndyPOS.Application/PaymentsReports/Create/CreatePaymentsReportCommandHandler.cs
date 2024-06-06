using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Application.PaymentsReports.Create;

public class CreatePaymentsReportCommandHandler : ICommandHandler<CreatePaymentsReportCommand>
{
	private readonly IReportService _reportService;
	private readonly IReportRepository _repository;
	private readonly ILogger<CreatePaymentsReportCommandHandler> _logger;

	public CreatePaymentsReportCommandHandler(IReportService reportService, 
											  IReportRepository repository, 
											  ILogger<CreatePaymentsReportCommandHandler> logger)
	{
		_reportService = reportService;
		_repository = repository;
		_logger = logger;
	}

	public async Task Handle(CreatePaymentsReportCommand command, CancellationToken cancellationToken)
	{
		try
		{
			var report = await _reportService.CreatePaymentsReportByInvoiceIdAsync(command.InvoiceId);
			await _repository.AddPaymentsReportAsync(report);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, 
							   "Error occurred while creating payments report for invoice ID {InvoiceId}",
							   command.InvoiceId);
			throw;
		}
	}
}