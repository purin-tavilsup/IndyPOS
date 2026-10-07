using FluentAssertions;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.Application.Tests.Models;

public class TillLayoutTests
{
    [Fact]
    public void For_WithMimyMartsFeatures_ShowsNoExtraButtonOrMenu()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                         ServiceProductsEnabled: false));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: false, ShowServiceButtons: false, ShowAccountsReceivableMenu: false));
    }

    [Fact]
    public void For_WithMimyShopsFeatures_ShowsServiceButtonsOnly()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                         ServiceProductsEnabled: true));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: false, ShowServiceButtons: true, ShowAccountsReceivableMenu: false));
    }

    [Fact]
    public void For_WithGeneralHardwaresFeatures_ShowsTheHardwareButtonAndTheLedgerMenu()
    {
        var layout = TillLayout.For(new StoreFeaturesDto(PayLaterEnabled: true, MultipleProductTypesEnabled: true,
                                                         ServiceProductsEnabled: false));

        layout.Should()
              .Be(new TillLayout(ShowHardwareButton: true, ShowServiceButtons: false, ShowAccountsReceivableMenu: true));
    }
}
