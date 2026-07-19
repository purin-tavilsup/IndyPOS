using System.Net;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Models;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class StoreFeaturesEndpointTests : IntegrationTestBase
{
    public StoreFeaturesEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetStoreFeatures_ForGeneralHardwareHost_ReturnsBothFlagsTrue()
    {
        await AuthenticateAsCashierAsync();

        var resp = await Client.GetAsync("/store/features");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<StoreFeaturesDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.True(dto!.PayLaterEnabled);
        Assert.True(dto.MultipleProductTypesEnabled);
    }

    [Fact]
    public async Task GetStoreFeatures_Unauthenticated_IsRejected()
    {
        ClearAuthentication();

        var resp = await Client.GetAsync("/store/features");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
