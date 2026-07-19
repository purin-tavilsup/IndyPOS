using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using IndyPOS.Mock;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.PaymentMethods;

public class PaymentMethodRepositoryTests
{
    private static StoreHubDbContext NewContext() =>
        new(new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyThisStoresMethods()
    {
        await using var ctx = NewContext();
        ctx.Set<PaymentMethod>().AddRange(
            new PaymentMethod { Code = "Cash", DisplayName = "Cash", Kind = PaymentMethodKind.Permanent, IsEnabled = true, StoreId = "store-A" },
            new PaymentMethod { Code = "Cash", DisplayName = "Cash", Kind = PaymentMethodKind.Permanent, IsEnabled = true, StoreId = "store-B" });
        await ctx.SaveChangesAsync();
        var identity = new MockStoreIdentityService { StoreId = "store-A" };
        var sut = new PaymentMethodRepository(ctx, identity);

        var result = await sut.GetAllAsync(default);

        result.Should().OnlyContain(m => m.StoreId == "store-A");
    }

    [Fact]
    public async Task UpdateAsync_ForCodeBelongingToAnotherStore_ShouldThrowAndNotModifyOtherStoresRow()
    {
        await using var ctx = NewContext();
        ctx.Set<PaymentMethod>().Add(
            new PaymentMethod { Code = "GCash", DisplayName = "GCash", Kind = PaymentMethodKind.Permanent, IsEnabled = true, StoreId = "store-B" });
        await ctx.SaveChangesAsync();
        var identity = new MockStoreIdentityService { StoreId = "store-A" };
        var sut = new PaymentMethodRepository(ctx, identity);
        var tamperedUpdate = new PaymentMethod { Code = "GCash", DisplayName = "Tampered", Kind = PaymentMethodKind.Permanent, IsEnabled = false, StoreId = "store-B" };

        var act = async () => await sut.UpdateAsync(tamperedUpdate, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var untouched = await ctx.Set<PaymentMethod>().AsNoTracking()
            .FirstAsync(m => m.StoreId == "store-B" && m.Code == "GCash");
        untouched.DisplayName.Should().Be("GCash");
        untouched.IsEnabled.Should().BeTrue();
    }
}
