using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.UI;

namespace IndyPOS.Windows.Forms.UI.Report;

public partial class SalesReportPanel : UserControl
{
    private readonly IReportService _reportService;
    private readonly MessageForm _messageForm;

    [ExcludeFromCodeCoverage]
    public SalesReportPanel(IReportService reportService, MessageForm messageForm)
    {
        _reportService = reportService;
        _messageForm = messageForm;

        InitializeComponent();
    }

    private void ShowSummary(SalesSummary salesSummary, PaymentsSummary paymentsSummary)
    {
        OverallSaleLabel.Text = $"{salesSummary.InvoiceTotal:N2}";

        OverallSaleExcluedIncompleteArLabel.Text = $"{salesSummary.InvoiceTotalWithoutPayLaterPayments:N2}";

        GeneralGoodsSaleLabel.Text = $"{salesSummary.GeneralProductsTotal:N2}";

        HardwareSaleLabel.Text = $"{salesSummary.HardwareProductsTotal:N2}";

        ArTotalForGeneralProductsLabel.Text = $"{salesSummary.PayLaterPaymentsTotalForGeneralProducts:N2}";

        ArTotalForHardwareProductsLabel.Text = $"{salesSummary.PayLaterPaymentsTotalForHardwareProducts:N2}";

        GeneralProductsTotalWithoutArLabel.Text = $"{salesSummary.GeneralProductsTotalWithoutPayLaterPayments:N2}";

        HardwareProductsTotalWithoutArLabel.Text = $"{salesSummary.HardwareProductsTotalWithoutPayLaterPayments:N2}";

        ArTotalLabel.Text = $"{salesSummary.PayLaterPaymentsTotal:N2}";

        CompletedArLabel.Text = $"{salesSummary.CompletedPayLaterPaymentsTotal:N2}";

        IncompleteArLabel.Text = $"{salesSummary.IncompletePayLaterPaymentsTotal:N2}";

        PaymentByTransferLabel.Text = $"{paymentsSummary.MoneyTransferTotal:N2}";

        PaymentByKlkLabel.Text = $"{paymentsSummary.FiftyFiftyTotal:N2}";

        PaymentByM33Label.Text = $"{paymentsSummary.M33WeLoveTotal:N2}";

        PaymentByWeWinLabel.Text = $"{paymentsSummary.WeWinTotal:N2}";

        PaymentByWelfareCardLabel.Text = $"{paymentsSummary.WelfareCardTotal:N2}";

        PaymentByArLabel.Text = $"{paymentsSummary.PayLaterTotal:N2}";
    }

    private async Task<SalesSummary> GetSalesReportByPeriodAsync(TimePeriod period)
    {
        return await _reportService.CreateSalesSummaryByPeriodAsync(period);
    }

	private async Task<SalesSummary> GetSalesReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _reportService.CreateSalesSummaryByDateRangeAsync(startDate, endDate);
	}

    private async Task<PaymentsSummary> GetPaymentsReportByPeriodAsync(TimePeriod period)
    {
        return await _reportService.CreatePaymentsSummaryByPeriodAsync(period);
    }

	private async Task<PaymentsSummary> GetPaymentsReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _reportService.CreatePaymentsSummaryByDateRangeAsync(startDate, endDate);
	}

    private async Task ShowReportByPeriodAsync(string periodText, TimePeriod period)
    {
        PeriodLabel.Text = periodText;

        try
        {
            var salesReport = await GetSalesReportByPeriodAsync(period);
            var paymentsReport = await GetPaymentsReportByPeriodAsync(period);

            ShowSummary(salesReport, paymentsReport);
        }
        catch (Exception ex)
        {
            ReportErrorHandler.Show(_messageForm, ex);
        }
    }

    private async Task ShowReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        PeriodLabel.Text = $"{startDate:yyyy MMMM dd} - {endDate:yyyy MMMM dd}";

        try
        {
            var salesReport = await GetSalesReportByDateRangeAsync(startDate, endDate);
            var paymentsReport = await GetPaymentsReportByDateRangeAsync(startDate, endDate);

            ShowSummary(salesReport, paymentsReport);
        }
        catch (Exception ex)
        {
            ReportErrorHandler.Show(_messageForm, ex);
        }
    }

    private async void ShowReportByTodayButton_Click(object sender, EventArgs e)
    {
        await ShowReportByPeriodAsync(ShowReportByTodayButton.Text, TimePeriod.Today);
    }

    private async void ShowReportByThisMonthButton_Click(object sender, EventArgs e)
    {
        await ShowReportByPeriodAsync(ShowReportByThisMonthButton.Text, TimePeriod.ThisMonth);
    }

    private async void ShowReportByThisYearButton_Click(object sender, EventArgs e)
    {
        await ShowReportByPeriodAsync(ShowReportByThisYearButton.Text, TimePeriod.ThisYear);
    }

    private void TestDataFeedButton_Click(object sender, EventArgs e)
    {
    }

    private async void ShowReportByDateRangeButton_Click(object sender, EventArgs e)
    {
		var startDate = StartDatePicker.Value.ToDateOnly();
		var endDate = EndDatePicker.Value.ToDateOnly();

        await ShowReportByDateRangeAsync(startDate, endDate);
	}
}
