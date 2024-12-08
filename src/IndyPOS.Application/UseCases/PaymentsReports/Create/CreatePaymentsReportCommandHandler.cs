using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.Constants;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Application.UseCases.PaymentsReports.Create;

public class CreatePaymentsReportCommandHandler : ICommandHandler<CreatePaymentsReportCommand>
{
	private readonly IReportService _reportService;
	private readonly IReportRepository _repository;
	private readonly ILogger<CreatePaymentsReportCommandHandler> _logger;
	private readonly IJsonService _jsonService;

	public CreatePaymentsReportCommandHandler(IReportService reportService, 
											  IReportRepository repository, 
											  ILogger<CreatePaymentsReportCommandHandler> logger, IJsonService jsonService)
	{
		_reportService = reportService;
		_repository = repository;
		_logger = logger;
		_jsonService = jsonService;
	}

	public async Task Handle(CreatePaymentsReportCommand command, CancellationToken cancellationToken)
	{
		PaymentsReport? report = null;
		
		try
		{
			report = await _reportService.CreatePaymentsReportByInvoiceIdAsync(command.InvoiceId);
			
			await _repository.AddPaymentsReportAsync(report);
		}
		catch (Exception ex)
		{
			await HandleError(ex, report, command.InvoiceId);
		}
	}

	private async Task HandleError(Exception ex, PaymentsReport? report, int invoiceId)
	{
		_logger.LogWarning(ex, 
						   "Error occurred while creating payments report {@Report} for invoice ID {InvoiceId}",
						   report,
						   invoiceId);
			
		if (report is not null)
		{
			await BackupReportAsync(report);
		}
	}
	
	private async Task BackupReportAsync(PaymentsReport report)
	{
		try
		{
			var tasks = new List<Task>
			{
				BackupReportToFileAsync(report, ReportConstants.PrimaryPaymentsReportBackupDirectory),
				BackupReportToFileAsync(report, ReportConstants.SecondaryPaymentsReportBackupDirectory)
			};

			await Task.WhenAll(tasks);
		}
		catch (AggregateException ae)
		{
			foreach (var e in ae.InnerExceptions)
			{
				_logger.LogWarning(e, "Failed to backup payments report to file. {Message}", e.Message);
			}
		}
	}
	
	private async Task BackupReportToFileAsync(PaymentsReport report, string reportDirectory)
	{
		var fileName = $"report-{report.ReferenceId}";
		var filePath = $"{reportDirectory}{fileName}";
		
		await _jsonService.SaveToFileAsync(report, filePath);
	}
}