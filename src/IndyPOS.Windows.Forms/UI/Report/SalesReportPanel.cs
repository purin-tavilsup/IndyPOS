﻿using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Extensions;

namespace IndyPOS.Windows.Forms.UI.Report;

public partial class SalesReportPanel : UserControl
{
    private readonly IReportService _reportService;

    [ExcludeFromCodeCoverage]
    public SalesReportPanel(IReportService reportService)
    {
        _reportService = reportService;

        InitializeComponent();
    }

    private void ShowSummary(ISalesReport salesReport, IPaymentsReport paymentsReport)
    {
        OverallSaleLabel.Text = $"{salesReport.InvoiceTotal:N2}";

        OverallSaleExcluedIncompleteArLabel.Text = $"{salesReport.InvoiceTotalWithoutPayLaterPayments:N2}";

        GeneralGoodsSaleLabel.Text = $"{salesReport.GeneralProductsTotal:N2}";

        HardwareSaleLabel.Text = $"{salesReport.HardwareProductsTotal:N2}";

        ArTotalForGeneralProductsLabel.Text = $"{salesReport.PayLaterPaymentsTotalForGeneralProducts:N2}";

        ArTotalForHardwareProductsLabel.Text = $"{salesReport.PayLaterPaymentsTotalForHardwareProducts:N2}";

        GeneralProductsTotalWithoutArLabel.Text = $"{salesReport.GeneralProductsTotalWithoutPayLaterPayments:N2}";

        HardwareProductsTotalWithoutArLabel.Text = $"{salesReport.HardwareProductsTotalWithoutPayLaterPayments:N2}";

        ArTotalLabel.Text = $"{salesReport.PayLaterPaymentsTotal:N2}";

        CompletedArLabel.Text = $"{salesReport.CompletedPayLaterPaymentsTotal:N2}";

        IncompleteArLabel.Text = $"{salesReport.IncompletePayLaterPaymentsTotal:N2}";

        PaymentByTransferLabel.Text = $"{paymentsReport.MoneyTransferTotal:N2}";

        PaymentByKlkLabel.Text = $"{paymentsReport.FiftyFiftyTotal:N2}";

        PaymentByM33Label.Text = $"{paymentsReport.M33WeLoveTotal:N2}";

        PaymentByWeWinLabel.Text = $"{paymentsReport.WeWinTotal:N2}";

        PaymentByWelfareCardLabel.Text = $"{paymentsReport.WelfareCardTotal:N2}";

        PaymentByArLabel.Text = $"{paymentsReport.PayLaterTotal:N2}";
    }

    private async Task<ISalesReport> GetSalesReportByPeriodAsync(TimePeriod period)
    {
        return await _reportService.CreateSalesReportByPeriodAsync(period);
    }

	private async Task<ISalesReport> GetSalesReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _reportService.CreateSalesReportByDateRangeAsync(startDate, endDate);
	}

    private async Task<IPaymentsReport> GetPaymentsReportByPeriodAsync(TimePeriod period)
    {
        return await _reportService.CreatePaymentsReportByPeriodAsync(period);
    }

	private async Task<IPaymentsReport> GetPaymentsReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _reportService.CreatePaymentsReportByDateRangeAsync(startDate, endDate);
	}

    private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
    {
        PeriodLabel.Text = ShowReportByTodayButton.Text;

        var salesReport = await GetSalesReportByPeriodAsync(TimePeriod.Today);
        var paymentsReport = await GetPaymentsReportByPeriodAsync(TimePeriod.Today);

        ShowSummary(salesReport, paymentsReport);
    }

    private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
    {
        PeriodLabel.Text = ShowReportByThisMonthButton.Text;

        var salesReport = await GetSalesReportByPeriodAsync(TimePeriod.ThisMonth);
        var paymentsReport = await GetPaymentsReportByPeriodAsync(TimePeriod.ThisMonth);

        ShowSummary(salesReport, paymentsReport);
    }

    private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
    {
        PeriodLabel.Text = ShowReportByThisYearButton.Text;

        var salesReport = await GetSalesReportByPeriodAsync(TimePeriod.ThisYear);
        var paymentsReport = await GetPaymentsReportByPeriodAsync(TimePeriod.ThisYear);

        ShowSummary(salesReport, paymentsReport);
    }

    private void TestDataFeedButton_Click(object sender, EventArgs e)
    {
    }

    private async void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
    {
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

		PeriodLabel.Text = $"{startDate:yyyy MMMM dd} - {endDate:yyyy MMMM dd}";

		var salesReport = await GetSalesReportByDateRangeAsync(startDate, endDate);
		var paymentsReport = await GetPaymentsReportByDateRangeAsync(startDate, endDate);

        ShowSummary(salesReport, paymentsReport);
	}
}