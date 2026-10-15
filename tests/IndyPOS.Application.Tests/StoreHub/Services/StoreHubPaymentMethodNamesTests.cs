using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class StoreHubPaymentMethodNamesTests
{
    private static readonly PaymentMethodDto Cash = new("Cash", "เงินสด", PaymentMethodKind.Standard, true, 1);

    private readonly Mock<IStoreHubClient> _client = new();

    [Fact]
    public async Task GetAsync_WhenTheCatalogFailsToLoad_ReturnsAnEmptyMap()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubClientException("Cannot connect to StoreHub"));

        var names = await Names().GetAsync();

        names.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task GetAsync_AfterAFailedLoad_TriesAgain()
    {
        _client.SetupSequence(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new StoreHubClientException("Cannot connect to StoreHub"))
               .ReturnsAsync([Cash]);
        var sut = Names();
        await sut.GetAsync();

        var names = await sut.GetAsync();

        names.Should()
             .ContainKey("Cash");
    }

    // HttpClient.Timeout surfaces as a TaskCanceledException with no caller token cancelled. Rethrowing
    // it failed a sale StoreHub had already saved, and the cashier saved it again.
    [Fact]
    public async Task GetAsync_WhenTheRequestTimesOut_ReturnsAnEmptyMap()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new TaskCanceledException("timed out", new TimeoutException()));

        var names = await Names().GetAsync();

        names.Should()
             .BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WhenTheCallerCancels_Throws()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new OperationCanceledException(cancelled.Token));

        var act = () => Names().GetAsync(cancelled.Token);

        await act.Should()
                 .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetAsync_WithADifferentlyCasedCode_FindsTheName()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([Cash]);

        var names = await Names().GetAsync();

        names["CASH"].Should()
                     .Be("เงินสด");
    }

    [Fact]
    public async Task GetAsync_AfterASuccessfulLoad_DoesNotCallStoreHubAgain()
    {
        _client.Setup(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync([Cash]);
        var sut = Names();
        await sut.GetAsync();

        await sut.GetAsync();

        _client.Verify(c => c.GetOfferablePaymentMethodsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private StoreHubPaymentMethodNames Names() =>
        new(_client.Object, NullLogger<StoreHubPaymentMethodNames>.Instance);
}
