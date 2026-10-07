using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI;
using IndyPOS.Windows.Forms.UI.Report;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Report;

/// <summary>
/// Each report panel follows the store as soon as it is shown, before any data is fetched: a MimyShop
/// manager must never see GeneralHardware's tiles or filter, even on an empty first view.
/// </summary>
public class ReportPanelsLayoutTests
{
    private static readonly StoreFeaturesDto MimyShop = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                            ServiceProductsEnabled: true);
    private static readonly StoreFeaturesDto GeneralHardware = new(PayLaterEnabled: true, MultipleProductTypesEnabled: true);

    [Fact]
    public void SalesOverview_WhenShownAtMimyShop_HidesTheLedgerTiles()
    {
        using var panel = new SalesReportPanel(Mock.Of<IReportService>(), Mock.Of<IStoreHubClient>(), Features(MimyShop),
                                               new MessageForm());

        Show(panel);

        Find(panel, "panel14").Visible.Should()
                                      .BeFalse();
    }

    [Fact]
    public void SalesOverview_WhenShownAtMimyShop_HidesTheProductTypeTiles()
    {
        using var panel = new SalesReportPanel(Mock.Of<IReportService>(), Mock.Of<IStoreHubClient>(), Features(MimyShop),
                                               new MessageForm());

        Show(panel);

        Find(panel, "panel5").Visible.Should()
                                     .BeFalse();
    }

    [Fact]
    public void SalesOverview_WhenShownAtGeneralHardware_KeepsTheLedgerTiles()
    {
        using var panel = new SalesReportPanel(Mock.Of<IReportService>(), Mock.Of<IStoreHubClient>(),
                                               Features(GeneralHardware), new MessageForm());

        Show(panel);

        Find(panel, "panel14").Visible.Should()
                                      .BeTrue();
    }

    [Fact]
    public void ProductsSold_WhenShownAtMimyShop_HidesTheProductTypeFilter()
    {
        using var panel = new InvoiceProductsReportPanel(Mock.Of<IStoreHubClient>(), Features(MimyShop), new MessageForm());

        Show(panel);

        Find(panel, "groupBox1").Visible.Should()
                                        .BeFalse();
    }

    // The tabs wait on the store's features; the overview must not. Otherwise a tab the manager clicks while
    // the features load is replaced by the overview when they arrive.
    [Fact]
    public void ReportsPanel_WhenShownWhileFeaturesLoad_ShowsTheOverviewAtOnce()
    {
        var features = new TaskCompletionSource<StoreFeaturesDto>();
        var pending = new Mock<IStoreFeaturesProvider>();
        pending.Setup(p => p.GetAsync())
               .Returns(features.Task);
        using var messageForm = new MessageForm();
        var overview = new SalesReportPanel(Mock.Of<IReportService>(), Mock.Of<IStoreHubClient>(), pending.Object, messageForm);
        using var reports = new ReportsPanel(
            overview,
            new InvoiceProductsReportPanel(Mock.Of<IStoreHubClient>(), pending.Object, messageForm),
            new SalesHistoryReportPanel(Mock.Of<IReportService>(), Mock.Of<IStoreConstants>(), Mock.Of<IReceiptPrinterService>(), messageForm),
            new PayLaterPaymentsReportPanel(Mock.Of<IStoreHubClient>(), messageForm),
            new CashFlowCalculatorPanel(Mock.Of<IReportService>(), Mock.Of<IJsonService>(), Mock.Of<ICsvService>(),
                                        NullLogger<MainForm>.Instance),
            pending.Object);

        Show(reports);
        var shownWhileLoading = Find(reports, "ActivePanel").Controls.Contains(overview);
        features.SetResult(GeneralHardware); // xUnit waits for every async void a test starts

        shownWhileLoading.Should()
                         .BeTrue();
    }

    [Fact]
    public void SalesOverview_WhenShowingTodayAtMimyShop_MakesNoLegacySummaryCall()
    {
        var reports = LegacyReports();
        using var panel = new SalesReportPanel(reports.Object, SummaryClient(linesTotal: 45m), Features(MimyShop),
                                               new MessageForm());

        ((Button)Find(panel, "ShowReportByTodayButton")).PerformClick();

        reports.Verify(r => r.CreateSalesSummaryByDateRangeAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public void SalesOverview_WhenShowingTodayAtMimyShop_ShowsTheLinesTotal()
    {
        using var panel = new SalesReportPanel(LegacyReports().Object, SummaryClient(linesTotal: 45m), Features(MimyShop),
                                               new MessageForm());

        ((Button)Find(panel, "ShowReportByTodayButton")).PerformClick();

        Find(panel, "OverallSaleLabel").Text.Should()
                                            .Be($"{45m:N2}");
    }

    // Answers the legacy call with an empty summary: a null would throw inside the panel, which then opens
    // its modal error dialog and blocks the test run.
    private static Mock<IReportService> LegacyReports()
    {
        var reports = new Mock<IReportService>();
        reports.Setup(r => r.CreateSalesSummaryByDateRangeAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
               .ReturnsAsync(new SalesSummary());
        return reports;
    }

    private static IStoreHubClient SummaryClient(decimal linesTotal)
    {
        var client = new Mock<IStoreHubClient>();
        client.Setup(c => c.GetSalesSummaryAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new SalesSummaryDto(default, default, 0, 0m, new PaymentBreakdownDto(0, 0, 0, 0, 0, 0), [])
              {
                  LinesTotal = linesTotal
              });
        return client.Object;
    }

    private static IStoreFeaturesProvider Features(StoreFeaturesDto features)
    {
        var provider = new Mock<IStoreFeaturesProvider>();
        provider.Setup(p => p.GetAsync())
                .ReturnsAsync(features);
        return provider.Object;
    }

    // ReportsPanel shows a sub-panel by setting Visible; toggling it raises VisibleChanged the same way.
    private static void Show(Control panel)
    {
        panel.Visible = false;
        panel.Visible = true;
    }

    private static Control Find(Control panel, string name) => panel.Controls.Find(name, searchAllChildren: true).Single();
}
