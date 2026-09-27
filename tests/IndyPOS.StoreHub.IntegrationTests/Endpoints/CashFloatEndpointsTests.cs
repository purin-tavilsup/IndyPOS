using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashFloatEndpointsTests : IntegrationTestBase
{
    public CashFloatEndpointsTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<CashFloatDto> AddCashFloatAsync(decimal amount = 1000m)
    {
        var response = await Client.PostAsJsonAsync("/cash/floats", new { amount });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CashFloatDto>(JsonOptions))!;
    }

    [Fact]
    public async Task EditCashFloat_WithUnknownId_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.PutAsJsonAsync($"/cash/floats/{Guid.NewGuid()}", new { amount = 500m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditCashFloat_AfterDelete_ReturnsNotFound()
    {
        await AuthenticateAsCashierAsync();
        var cashFloat = await AddCashFloatAsync();
        await Client.DeleteAsync($"/cash/floats/{cashFloat.Id}");

        var response = await Client.PutAsJsonAsync($"/cash/floats/{cashFloat.Id}", new { amount = 500m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCashFloat_WhenDeletedTwice_ReturnsNoContentTheSecondTime()
    {
        await AuthenticateAsCashierAsync();
        var cashFloat = await AddCashFloatAsync();
        await Client.DeleteAsync($"/cash/floats/{cashFloat.Id}");

        var second = await Client.DeleteAsync($"/cash/floats/{cashFloat.Id}");

        second.StatusCode.Should()
                         .Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCashFloat_ThenList_ExcludesIt()
    {
        await AuthenticateAsCashierAsync();
        var cashFloat = await AddCashFloatAsync();

        await Client.DeleteAsync($"/cash/floats/{cashFloat.Id}");
        var list = await Client.GetFromJsonAsync<List<CashFloatDto>>("/cash/floats", JsonOptions);

        list.Should()
            .NotContain(f => f.Id == cashFloat.Id);
    }
}
