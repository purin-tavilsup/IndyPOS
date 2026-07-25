using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Mock;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.PaymentMethods;

public class PaymentMethodCatalogServiceTests
{
    private static PaymentMethod M(string code, bool enabled = true, int order = 0) => new()
    { Code = code, DisplayName = code, Kind = PaymentMethodKind.Standard, IsEnabled = enabled, DisplayOrder = order, StoreId = "s" };

    [Fact]
    public async Task GetOfferableAsync_OnMinimart_ShouldApplyPolicyAndExcludePayLater()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentMethod> { M("Cash", order: 1), M("PayLater", order: 2) });
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService { StoreType = StoreType.Minimart });

        var result = await sut.GetOfferableAsync(default);

        result.Select(m => m.Code).Should().Contain("Cash").And.NotContain("PayLater");
    }

    [Fact]
    public async Task AddCampaignAsync_ShouldPersistGovernmentCampaignEnabled()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetByCodeAsync("SomeNew2027", It.IsAny<CancellationToken>())).ReturnsAsync((PaymentMethod?)null);
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService());

        await sut.AddCampaignAsync("SomeNew2027", "New Campaign", 8, default);

        repo.Verify(r => r.AddAsync(It.Is<PaymentMethod>(m =>
            m.Code == "SomeNew2027" && m.Kind == PaymentMethodKind.GovernmentCampaign && m.IsEnabled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCampaignAsync_DuplicateCode_ShouldThrow()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetByCodeAsync("Cash", It.IsAny<CancellationToken>())).ReturnsAsync(M("Cash"));
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService());

        var act = () => sut.AddCampaignAsync("Cash", "dup", 9, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateDisplayAsync_WithNullOrder_ShouldPreserveExistingDisplayOrder()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetByCodeAsync("Cash", It.IsAny<CancellationToken>())).ReturnsAsync(M("Cash", order: 5));
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService());

        await sut.UpdateDisplayAsync("Cash", "New Name", null, default);

        repo.Verify(r => r.UpdateAsync(It.Is<PaymentMethod>(m =>
            m.DisplayName == "New Name" && m.DisplayOrder == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDisplayAsync_WithOrder_ShouldOverwriteDisplayOrder()
    {
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetByCodeAsync("Cash", It.IsAny<CancellationToken>())).ReturnsAsync(M("Cash", order: 5));
        var sut = new PaymentMethodCatalogService(repo.Object, new MockStoreIdentityService());

        await sut.UpdateDisplayAsync("Cash", "New Name", 9, default);

        repo.Verify(r => r.UpdateAsync(It.Is<PaymentMethod>(m =>
            m.DisplayName == "New Name" && m.DisplayOrder == 9), It.IsAny<CancellationToken>()), Times.Once);
    }
}
