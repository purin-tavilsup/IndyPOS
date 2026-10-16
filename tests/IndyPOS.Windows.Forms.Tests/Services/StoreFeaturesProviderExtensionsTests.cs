using FluentAssertions;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.Services;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.Services;

public class StoreFeaturesProviderExtensionsTests
{
    [Fact]
    public async Task GetLayoutAsync_WhenTheFeaturesCannotLoad_ReturnsTheFallback()
    {
        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).ThrowsAsync(new HttpRequestException("StoreHub is down"));

        var layout = await features.Object.GetLayoutAsync("the test screen");

        layout.Should()
              .BeSameAs(TillLayout.WhenFeaturesUnavailable);
    }

    [Fact]
    public async Task GetLayoutAsync_AtAStoreWithOneProductType_HidesTheSplit()
    {
        var features = new Mock<IStoreFeaturesProvider>();
        features.Setup(f => f.GetAsync()).ReturnsAsync(new StoreFeaturesDto(PayLaterEnabled: false, MultipleProductTypesEnabled: false));

        var layout = await features.Object.GetLayoutAsync("the test screen");

        layout.ShowProductTypeSplit.Should()
                                   .BeFalse();
    }
}
