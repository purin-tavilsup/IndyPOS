using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashCountEndpointsTests : IntegrationTestBase
{
    private static readonly AddCashCountRequest OneThousand = new(1, 0, 0, 0, 0, 0, 0, 0, 0);

    public CashCountEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task AddCount_WithNegativeDenomination_ReturnsBadRequest()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PostAsJsonAsync("/cash/counts", OneThousand with { Coin1Count = -1 });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditCount_WithAnyId_IsNotAllowed()
    {
        await AuthenticateAsCashierAsync();
        var created = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand)).Content
                                                                                       .ReadFromJsonAsync<CashCountDto>(JsonOptions);

        var response = await Client.PutAsJsonAsync($"/cash/counts/{created!.Id}", OneThousand);

        ((int)response.StatusCode).Should()
                                  .BeOneOf(404, 405);
    }

    [Fact]
    public async Task DeleteCount_WithAnyId_IsNotAllowed()
    {
        await AuthenticateAsCashierAsync();
        var created = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand)).Content
                                                                                       .ReadFromJsonAsync<CashCountDto>(JsonOptions);

        var response = await Client.DeleteAsync($"/cash/counts/{created!.Id}");

        ((int)response.StatusCode).Should()
                                  .BeOneOf(404, 405);
    }

    [Fact]
    public async Task AddCount_Twice_ListsBothNewestFirst()
    {
        await ResetDatabaseAsync();
        await AuthenticateAsCashierAsync();
        var first = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand)).Content.ReadFromJsonAsync<CashCountDto>(JsonOptions);
        var second = await (await Client.PostAsJsonAsync("/cash/counts", OneThousand with { BankNote1000Count = 2 })).Content
                                                                                                                     .ReadFromJsonAsync<CashCountDto>(JsonOptions);

        var list = await Client.GetFromJsonAsync<List<CashCountDto>>("/cash/counts", JsonOptions);

        list!.Select(c => c.Id).Should()
                               .Equal(second!.Id, first!.Id);
    }
}
