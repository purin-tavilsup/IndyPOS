using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Application.SalesReports.Create;

public class CreateSalesReportCommandHandler : ICommandHandler<CreateSalesReportCommand>
{
	private readonly IReportService _reportService;
	private readonly IReportRepository _repository;
	private readonly ILogger<CreateSalesReportCommandHandler> _logger;

	public CreateSalesReportCommandHandler(IReportRepository repository, 
										   IReportService reportService, 
										   ILogger<CreateSalesReportCommandHandler> logger)
	{
		_repository = repository;
		_reportService = reportService;
		_logger = logger;
	}
	
	public async Task Handle(CreateSalesReportCommand command, CancellationToken cancellationToken)
	{
		try
		{
			var report = await _reportService.CreateSalesReportByInvoiceIdAsync(command.InvoiceId);
			await _repository.AddSalesReportAsync(report);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, 
							   "Error occurred while creating sales report for invoice ID {InvoiceId}",
							   command.InvoiceId);
			throw;
		}
	}
}