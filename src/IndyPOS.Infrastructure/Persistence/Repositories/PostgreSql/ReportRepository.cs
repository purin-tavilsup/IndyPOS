using Dapper;
using IndyPOS.Application.Abstractions.Reports.Repositories;
using IndyPOS.Application.Common.Models;

namespace IndyPOS.Infrastructure.Persistence.Repositories.PostgreSql;

public class ReportRepository : IReportRepository
{
	private readonly IReportDbConnectionProvider _dbConnectionProvider;

	public ReportRepository(IReportDbConnectionProvider dbConnectionProvider)
	{
		_dbConnectionProvider = dbConnectionProvider;
	}

	public async Task AddSalesReportAsync(SalesReport report)
	{
		using var connection = _dbConnectionProvider.CreateConnection();
		connection.Open();
		
		const string sqlCommand = 
			"""
			INSERT INTO rungrat_report.sales_report
				(id,
				 created,
				 reference_id,
				 invoice_total,
				 general_total,
				 hardware_total,
				 paylater_total,
				 general_paylater_total,
				 hardware_paylater_total,
				 invoice_total_without_paylater,
				 general_total_without_paylater,
				 hardware_total_without_paylater,
				 completed_paylater_total,
				 incomplete_paylater_total)
			VALUES
				(@id,
				 @created,
				 @referenceId,
				 @invoiceTotal,
				 @generalTotal,
				 @hardwareTotal,
				 @payLaterTotal,
				 @generalPayLaterTotal,
				 @hardwarePayLaterTotal,
				 @invoiceTotalWithoutPayLater,
				 @generalTotalWithoutPayLater,
				 @hardwareTotalWithoutPayLater,
				 @completedPayLaterTotal,
				 @incompletePayLaterTotal);
			""";
		
		var sqlParameters = new
		{
			id = report.Id,
			created = report.Created,
			referenceId = report.ReferenceId,
			invoiceTotal = report.InvoiceTotal,
			generalTotal = report.GeneralProductsTotal,
			hardwareTotal = report.HardwareProductsTotal,
			payLaterTotal = report.PayLaterPaymentsTotal,
			generalPayLaterTotal = report.PayLaterPaymentsTotalForGeneralProducts,
			hardwarePayLaterTotal = report.PayLaterPaymentsTotalForHardwareProducts,
			invoiceTotalWithoutPayLater = report.InvoiceTotalWithoutPayLaterPayments,
			generalTotalWithoutPayLater = report.GeneralProductsTotalWithoutPayLaterPayments,
			hardwareTotalWithoutPayLater = report.HardwareProductsTotalWithoutPayLaterPayments,
			completedPayLaterTotal = report.CompletedPayLaterPaymentsTotal,
			incompletePayLaterTotal = report.IncompletePayLaterPaymentsTotal
		};

		await connection.ExecuteAsync(sqlCommand, sqlParameters);
	}

	public async Task AddPaymentsReportAsync(PaymentsReport report)
	{
		using var connection = _dbConnectionProvider.CreateConnection();
		connection.Open();
		
		const string sqlCommand = 
			"""
			INSERT INTO rungrat_report.payments_report
				(id,
				 created,
				 reference_id,
				 money_transfer_total,
				 fifty_fifty_total,
				 m33_we_love_total,
				 we_win_total,
				 welfare_card_total,
				 paylater_total)
			VALUES
				(@id,
				 @created,
				 @referenceId,
				 @moneyTransferTotal,
				 @fiftyFiftyTotal,
				 @m33WeLoveTotal,
				 @weWinTotal,
				 @welfareCardTotal,
				 @payLaterTotal);
			""";
		
		var sqlParameters = new
		{
			id = report.Id,
			created = report.Created,
			referenceId = report.ReferenceId,
			moneyTransferTotal = report.MoneyTransferTotal,
			fiftyFiftyTotal = report.FiftyFiftyTotal,
			m33WeLoveTotal = report.M33WeLoveTotal,
			weWinTotal = report.WeWinTotal,
			welfareCardTotal = report.WelfareCardTotal,
			payLaterTotal = report.PayLaterTotal
		};

		await connection.ExecuteAsync(sqlCommand, sqlParameters);
	}
}