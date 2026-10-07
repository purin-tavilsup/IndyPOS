using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Every renamed route was a hard rename: the old path is gone, with no alias. Signed in as admin,
/// who holds every capability, so a 403 can never stand in for a missing route.
/// </summary>
[Collection("Integration")]
public class RenamedRouteTests : IntegrationTestBase
{
    private const string AnyId = "6f1c2a4e-8d3b-4c7a-9e21-5b0d7f3a1c88";

    public RenamedRouteTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Theory]
    [InlineData("GET", "/admin/payment-methods")]
    [InlineData("POST", "/admin/payment-methods")]
    [InlineData("PATCH", "/admin/payment-methods/Cash")]
    public async Task RenamedRoute_OnTheOldPath_ReturnsNotFound(string method, string path)
    {
        await AuthenticateAsAdminAsync();

        var response = await SendAsync(method, path);

        response.StatusCode.Should()
                           .Be(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> SendAsync(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", AnyId));
        if (method != "GET")
        {
            request.Content = JsonContent.Create(new { });
        }

        return Client.SendAsync(request);
    }
}
