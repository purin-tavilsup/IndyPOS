using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;
using IndyPOS.Domain.Entities.Core;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Products.Queries;

public class GenerateBarcodeQueryHandlerTests
{
    private static (GenerateBarcodeQueryHandler Sut, Mock<IStoreSettingRepository> Settings) CreateSut(
        int storeCode, int nextCounterValue)
    {
        var settings = new Mock<IStoreSettingRepository>();
        settings
            .Setup(x => x.IncrementAsync(StoreSettingKeys.BarcodeCounter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextCounterValue);

        var identity = new Mock<IStoreIdentityService>();
#pragma warning disable CS0618 // StoreCode is obsolete for identity but sanctioned for barcodes.
        identity.SetupGet(x => x.StoreCode).Returns(storeCode);
#pragma warning restore CS0618

        return (new GenerateBarcodeQueryHandler(settings.Object, identity.Object), settings);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnAScannableEan13Barcode()
    {
        var (sut, _) = CreateSut(storeCode: 1, nextCounterValue: 3);

        var barcode = await sut.HandleAsync(new GenerateBarcodeQuery());

        barcode.Should().Be("2000100000038");
    }

    [Fact]
    public async Task HandleAsync_WithASingleDigitStoreCode_ShouldStillReturnThirteenDigits()
    {
        // The regression this pins: the handler used to return {StoreCode}{Sequence:D8},
        // which is 9 digits for store code 1 - a length EAN-13 cannot encode, so the
        // add-product dialog threw "should be 12 or 13 digits long, but got 9". No test
        // covered the format, which is why it reached a live install.
        var (sut, _) = CreateSut(storeCode: 1, nextCounterValue: 3);

        var barcode = await sut.HandleAsync(new GenerateBarcodeQuery());

        barcode.Should().MatchRegex("^[0-9]{13}$");
        barcode.Should().NotBe("100000003");
    }

    [Fact]
    public async Task HandleAsync_ShouldIncrementTheSharedBarcodeCounter()
    {
        // Two tills must never mint the same barcode, so the sequence has to come from the
        // atomic counter rather than anything derived locally.
        var (sut, settings) = CreateSut(storeCode: 2, nextCounterValue: 1);

        await sut.HandleAsync(new GenerateBarcodeQuery());

        settings.Verify(
            x => x.IncrementAsync(StoreSettingKeys.BarcodeCounter, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ForDifferentStores_ShouldProduceDifferentBarcodes()
    {
        // Same sequence number, different store: the store code field must separate them.
        var (firstSut, _) = CreateSut(storeCode: 1, nextCounterValue: 7);
        var (secondSut, _) = CreateSut(storeCode: 2, nextCounterValue: 7);

        var first = await firstSut.HandleAsync(new GenerateBarcodeQuery());
        var second = await secondSut.HandleAsync(new GenerateBarcodeQuery());

        first.Should().NotBe(second);
    }
}
