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
    private static StoreHubDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task SeedAsync_OnEmptyCatalog_ShouldSeedSevenMethodsWithDeadCampaignsDisabled()
    {
        await using var ctx = NewContext();
        var repo = new PaymentMethodRepository(ctx, new MockStoreIdentityService { StoreId = "store-A" });
        var sut = new PaymentMethodSeeder(repo, new MockStoreIdentityService { StoreId = "store-A" },
            NullLogger<PaymentMethodSeeder>.Instance);

        await sut.SeedAsync(default);

        var all = await repo.GetAllAsync();
        all.Should().HaveCount(7);
        all.Single(m => m.Code == "PayLater").IsEnabled.Should().BeTrue();
        all.Single(m => m.Code == "M33WeLove").IsEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData("Cash", PaymentMethodKind.Standard)]
    [InlineData("MoneyTransfer", PaymentMethodKind.Standard)]
    [InlineData("PayLater", PaymentMethodKind.Special)]
    [InlineData("WelfareCard", PaymentMethodKind.GovernmentCampaign)]
    [InlineData("M33WeLove", PaymentMethodKind.GovernmentCampaign)]
    [InlineData("FiftyFifty", PaymentMethodKind.GovernmentCampaign)]
    [InlineData("WeWin", PaymentMethodKind.GovernmentCampaign)]
    public async Task SeedAsync_ForEachSeededCode_ShouldAssignExpectedKind(string code, PaymentMethodKind expectedKind)
    {
        await using var ctx = NewContext();
        var repo = new PaymentMethodRepository(ctx, new MockStoreIdentityService { StoreId = "store-A" });
        var sut = new PaymentMethodSeeder(repo, new MockStoreIdentityService { StoreId = "store-A" },
            NullLogger<PaymentMethodSeeder>.Instance);

        await sut.SeedAsync(default);

        var all = await repo.GetAllAsync();

        all.Single(m => m.Code == code).Kind.Should().Be(expectedKind);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_ShouldBeIdempotent()
    {
        await using var ctx = NewContext();
        var repo = new PaymentMethodRepository(ctx, new MockStoreIdentityService { StoreId = "store-A" });
        var sut = new PaymentMethodSeeder(repo, new MockStoreIdentityService { StoreId = "store-A" },
            NullLogger<PaymentMethodSeeder>.Instance);

        await sut.SeedAsync(default);
        await sut.SeedAsync(default);

        (await repo.GetAllAsync()).Should().HaveCount(7);
    }
}
