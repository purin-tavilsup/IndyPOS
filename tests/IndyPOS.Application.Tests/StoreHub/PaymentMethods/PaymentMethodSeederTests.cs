using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.PaymentMethods;

public class PaymentMethodSeederTests
{
    private const string StoreId = "store-A";

    private static StoreHubDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static (PaymentMethodSeeder Sut, PaymentMethodRepository Repo) NewSut(StoreHubDbContext context)
    {
        var repo = new PaymentMethodRepository(context, new MockStoreIdentityService { StoreId = StoreId });
        var sut = new PaymentMethodSeeder(repo, new MockStoreIdentityService { StoreId = StoreId },
            NullLogger<PaymentMethodSeeder>.Instance);

        return (sut, repo);
    }

    [Fact]
    public async Task SeedAsync_OnEmptyCatalog_ShouldSeedSevenMethods()
    {
        await using var ctx = NewContext();
        var (sut, repo) = NewSut(ctx);

        await sut.SeedAsync(default);

        (await repo.GetAllAsync()).Should().HaveCount(7);
    }

    [Theory]
    [InlineData("Cash", PaymentMethodKind.Standard, true, 1)]
    [InlineData("MoneyTransfer", PaymentMethodKind.Standard, true, 2)]
    [InlineData("WelfareCard", PaymentMethodKind.GovernmentCampaign, true, 3)]
    [InlineData("PayLater", PaymentMethodKind.Special, true, 4)]
    [InlineData("M33WeLove", PaymentMethodKind.GovernmentCampaign, false, 5)]
    [InlineData("FiftyFifty", PaymentMethodKind.GovernmentCampaign, false, 6)]
    [InlineData("WeWin", PaymentMethodKind.GovernmentCampaign, false, 7)]
    public async Task SeedAsync_OnEmptyCatalog_ShouldSeedEachMethodAsClassified(
        string code, PaymentMethodKind expectedKind, bool expectedEnabled, int expectedOrder)
    {
        await using var ctx = NewContext();
        var (sut, repo) = NewSut(ctx);

        await sut.SeedAsync(default);

        var seeded = (await repo.GetAllAsync()).Single(m => m.Code == code);

        seeded.Kind.Should().Be(expectedKind);
        seeded.IsEnabled.Should().Be(expectedEnabled);
        seeded.DisplayOrder.Should().Be(expectedOrder);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_ShouldBeIdempotent()
    {
        await using var ctx = NewContext();
        var (sut, repo) = NewSut(ctx);

        await sut.SeedAsync(default);
        await sut.SeedAsync(default);

        (await repo.GetAllAsync()).Should().HaveCount(7);
    }
}
