using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub.Repositories;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class InventoryMovementRepositoryTests : IntegrationTestBase
{
    public InventoryMovementRepositoryTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private InventoryMovementRepository CreateRepository() => new(GetDbContext());

    private async Task AddMovementAsync(string storeId, Guid productId, int delta)
    {
        var repository = CreateRepository();

        await repository.AddAsync(new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = productId,
            QuantityDelta = delta,
            Reason = "Adjustment",
            CreatedUtc = DateTime.UtcNow
        });
    }

    [Fact]
    public async Task GetBalancesAsync_WithSeveralMovements_ShouldSumThemPerProduct()
    {
        // CreateTestProductAsync seeds one InitialStock movement of 10 on "test-store".
        var product = await CreateTestProductAsync(initialStock: 10);
        await AddMovementAsync("test-store", product.Id, -3);
        await AddMovementAsync("test-store", product.Id, 5);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances[product.Id].Should()
                            .Be(12);
    }

    [Fact]
    public async Task GetBalancesAsync_WithTwoProducts_ShouldKeepThemIndependent()
    {
        var first = await CreateTestProductAsync(initialStock: 10);
        var second = await CreateTestProductAsync(initialStock: 4);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances[first.Id].Should()
                          .Be(10);
        balances[second.Id].Should()
                           .Be(4);
    }

    [Fact]
    public async Task GetBalancesAsync_WithNoMovements_ShouldOmitTheProduct()
    {
        // A product with no movements is absent, so callers read it as zero rather than
        // needing a row per product. initialStock: 0 skips the seeded movement.
        var product = await CreateTestProductAsync(initialStock: 0);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances.ContainsKey(product.Id).Should()
                                        .BeFalse();
    }

    [Fact]
    public async Task GetBalancesAsync_WithAnotherStoresMovements_ShouldExcludeThem()
    {
        var product = await CreateTestProductAsync(initialStock: 10);
        await AddMovementAsync("some-other-store", product.Id, 999);

        var balances = await CreateRepository().GetBalancesAsync("test-store");

        balances[product.Id].Should()
                            .Be(10);
    }
}
