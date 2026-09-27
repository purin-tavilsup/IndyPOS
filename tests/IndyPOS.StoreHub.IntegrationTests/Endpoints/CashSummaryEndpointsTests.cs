using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Summary;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashSummaryEndpointsTests : IntegrationTestBase
{
    public CashSummaryEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetSummary_WithFloatPayoutRepaymentAndTwoCounts_UsesLatestCountOnly()
    {
        await ResetDatabaseAsync();
        await AuthenticateAsCashierAsync();
        await Client.PostAsJsonAsync("/cash/floats", new { amount = 1000m });
        await Client.PostAsJsonAsync("/cash/payouts", new { amount = 200m });
        await Client.PostAsJsonAsync("/cash/debt-repayments", new { customerName = "ลุงสมชาย", amount = 300m });
        await Client.PostAsJsonAsync("/cash/counts", new AddCashCountRequest(9, 0, 0, 0, 0, 0, 0, 0, 0));
        await Client.PostAsJsonAsync("/cash/counts", new AddCashCountRequest(1, 0, 1, 0, 0, 0, 0, 0, 0));

        var summary = await Client.GetFromJsonAsync<CashDrawerSummaryDto>("/cash/summary", JsonOptions);

        summary!.ExpectedCash.Should()
                             .Be(1000m + 300m - 200m);
        summary.CountedCash.Should()
                           .Be(1100m);
        summary.CashDifference.Should()
                              .Be(1100m - 1100m);
    }
}
