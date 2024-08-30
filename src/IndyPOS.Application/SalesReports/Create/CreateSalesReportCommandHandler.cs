using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Constants;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Application.SalesReports.Create;

public class CreateSalesReportCommandHandler : ICommandHandler<CreateSalesReportCommand>
{
	private readonly IReportService _reportService;
	private readonly IReportRepository _repository;
	private readonly ILogger<CreateSalesReportCommandHandler> _logger;
	private readonly IJsonService _jsonService;

	public CreateSalesReportCommandHandler(IReportRepository repository, 
										   IReportService reportService, 
										   ILogger<CreateSalesReportCommandHandler> logger, 
										   IJsonService jsonService)
	{
		_repository = repository;
		_reportService = reportService;
		_logger = logger;
		_jsonService = jsonService;
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
			await HandleError(ex, report, command.InvoiceId);
		}
	}

	private async Task HandleError(Exception ex, SalesReport? report, int invoiceId)
	{
		_logger.LogWarning(ex, 
						   "Error occurred while creating sales report {@Report} for invoice ID {InvoiceId}",
						   report,
						   invoiceId);

		if (report is not null)
		{
			await BackupReportAsync(report);
		}
	}

	private async Task BackupReportAsync(SalesReport report)
	{
		try
		{
			var tasks = new List<Task>
			{
				BackupReportToFileAsync(report, ReportConstants.PrimarySalesReportBackupDirectory),
				BackupReportToFileAsync(report, ReportConstants.SecondarySalesReportBackupDirectory)
			};

			await Task.WhenAll(tasks);
		}
		catch (AggregateException ae)
		{
			foreach (var e in ae.InnerExceptions)
			{
				_logger.LogWarning(e, "Failed to backup sales report to file. {Message}", e.Message);
			}
		}
	}
	
	private async Task BackupReportToFileAsync(SalesReport report, string reportDirectory)
	{
		var fileName = $"report-{report.ReferenceId}";
		var filePath = $"{reportDirectory}{fileName}";
		
		await _jsonService.SaveToFileAsync(report, filePath);
	}
}