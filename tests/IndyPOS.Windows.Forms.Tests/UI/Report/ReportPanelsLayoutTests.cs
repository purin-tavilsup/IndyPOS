using System.Windows.Forms;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.Services;
using IndyPOS.Windows.Forms.UI;
using IndyPOS.Windows.Forms.UI.Report;
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
