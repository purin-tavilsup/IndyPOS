using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashAuthorizationTests : IntegrationTestBase
{
    /// <summary>Matches <c>CashCapabilityTests.UnknownRoleId</c> — no role maps this id to <c>cash.manage</c>.</summary>
    private const int RoleWithoutCashManage = 99;

    public CashAuthorizationTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetSummary_WithoutAuth_ReturnsUnauthorized()
    {
        ClearAuthentication();

        var response = await Client.GetAsync("/cash/summary");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddPayout_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutUserId());

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_WithTokenMissingUserId_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutUserId());

        var response = await Client.GetAsync("/cash/summary");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_AsCashier_ReturnsOk()
    {
        await AuthenticateAsCashierAsync();

        var response = await Client.GetAsync("/cash/summary");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSummary_WithRoleLackingCashManage_ReturnsForbidden()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithRole(RoleWithoutCashManage));

        var response = await Client.GetAsync("/cash/summary");

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPayout_WithRoleLackingCashManage_ReturnsForbidden()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithRole(RoleWithoutCashManage));

        var response = await Client.PostAsJsonAsync("/cash/payouts", new { amount = 10m });

        response.StatusCode.Should()
                           .Be(HttpStatusCode.Forbidden);
    }
}
