using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class TillLayoutTests
{
    // The legacy summary only carries the ลงบัญชี and general/hardware tiles' figures; a store showing
    // neither needs one summary call, not two.
    [Fact]
    public void NeedsLegacySalesSummary_WithMimyShopsFeatures_IsFalse()
    {
        TillLayout.For(new StoreFeaturesDto(false, false, true)).NeedsLegacySalesSummary.Should()
                                                                                        .BeFalse();
    }

    [Fact]
    public void NeedsLegacySalesSummary_WithGeneralHardwaresFeatures_IsTrue()
    {
        TillLayout.For(new StoreFeaturesDto(true, true, false)).NeedsLegacySalesSummary.Should()
                                                                                       .BeTrue();
    }

    // StoreHub unreachable: keep the till as designed. The hardware button and the ledger stay (the
    // server still refuses what a store may not do), and the service buttons, which default off, stay off.
    [Fact]
    public void WhenFeaturesUnavailable_KeepsTheTillAsDesigned()
    {
        TillLayout.WhenFeaturesUnavailable.Should()
                                          .Be(new TillLayout(ShowHardwareButton: true, ShowServiceButtons: false,
                                                             ShowAccountsReceivableMenu: true, ShowPayLaterReports: true,
                                                             ShowProductTypeSplit: true));
    }

    [Fact]
    public void For_WithMimyMartsFeatures_ShowsNoExtraButtonOrMenu()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                         ServiceProductsEnabled: false));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: false, ShowServiceButtons: false, ShowAccountsReceivableMenu: false,
                                 ShowPayLaterReports: false, ShowProductTypeSplit: false));
    }

    [Fact]
    public void For_WithMimyShopsFeatures_ShowsServiceButtonsOnly()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                         ServiceProductsEnabled: true));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: false, ShowServiceButtons: true, ShowAccountsReceivableMenu: false,
                                 ShowPayLaterReports: false, ShowProductTypeSplit: false));
    }

    [Fact]
    public void For_WithGeneralHardwaresFeatures_ShowsTheHardwareButtonAndTheLedgerMenu()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: true, MultipleProductTypesEnabled: true,
                                                         ServiceProductsEnabled: false));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: true, ShowServiceButtons: false, ShowAccountsReceivableMenu: true,
                                 ShowPayLaterReports: true, ShowProductTypeSplit: true));
    }
}
