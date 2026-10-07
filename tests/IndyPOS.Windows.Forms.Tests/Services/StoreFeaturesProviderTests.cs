using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Models;
using IndyPOS.Windows.Forms.Services;
using Moq;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.Services;

public class StoreFeaturesProviderTests
{
    private static readonly StoreFeaturesDto MimyShop = new(PayLaterEnabled: false, MultipleProductTypesEnabled: false,
                                                            ServiceProductsEnabled: true);

    private readonly Mock<IStoreHubClient> _storeHub = new();

    [Fact]
    public async Task GetAsync_WhenTheFetchFails_Throws()
    {
        _storeHub.Setup(c => c.GetStoreFeaturesAsync(It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new HttpRequestException("StoreHub down"));

        var act = () => new StoreFeaturesProvider(_storeHub.Object).GetAsync();

        await act.Should()
                 .ThrowAsync<HttpRequestException>();
    }

    // A failure must not stick: the next login asks StoreHub again.
    [Fact]
    public async Task GetAsync_AfterAFailedFetch_FetchesAgain()
    {
        _storeHub.SetupSequence(c => c.GetStoreFeaturesAsync(It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new HttpRequestException("StoreHub down"))
                 .ReturnsAsync(MimyShop);
        var provider = new StoreFeaturesProvider(_storeHub.Object);
        await FluentActions.Awaiting(provider.GetAsync).Should().ThrowAsync<HttpRequestException>();

        var features = await provider.GetAsync();

        features.Should()
                .Be(MimyShop);
    }

    // The menu and the sale panel both ask on a login; the store type cannot change while the till runs.
    [Fact]
    public async Task GetAsync_CalledTwice_FetchesOnce()
    {
        _storeHub.Setup(c => c.GetStoreFeaturesAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MimyShop);
        var provider = new StoreFeaturesProvider(_storeHub.Object);
        await provider.GetAsync();

        await provider.GetAsync();

        _storeHub.Verify(c => c.GetStoreFeaturesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhileAFetchIsInFlight_SharesIt()
    {
        var pending = new TaskCompletionSource<StoreFeaturesDto>();
        _storeHub.Setup(c => c.GetStoreFeaturesAsync(It.IsAny<CancellationToken>()))
                 .Returns(pending.Task);
        var provider = new StoreFeaturesProvider(_storeHub.Object);
        var first = provider.GetAsync();

        var second = provider.GetAsync();
        pending.SetResult(MimyShop);
        await Task.WhenAll(first, second);

        _storeHub.Verify(c => c.GetStoreFeaturesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
