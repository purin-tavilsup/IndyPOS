using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
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
		SalesReport? report = null;
		
		try
		{
			report = await _reportService.CreateSalesReportByInvoiceIdAsync(command.InvoiceId, 
																			command.HasPayLaterPayment);
			await _repository.AddSalesReportAsync(report);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, 
							   "Error occurred while creating sales report {@Report} for invoice ID {InvoiceId}",
							   report,
							   command.InvoiceId);
			throw;
		}
	}
}