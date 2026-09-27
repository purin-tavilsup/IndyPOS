using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class CashAuthorizationTests : IntegrationTestBase
{
    /// <summary>Matches <c>CashCapabilityTests.UnknownRoleId</c> — no role maps this id to <c>cash.manage</c>.</summary>
    private const int RoleWithoutCashManage = 99;

    public CashAuthorizationTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    /// <summary>
    /// A correctly signed cashier token that carries a role but NO user-id claim — the capability
    /// check passes, so only the user-id filter stands between it and a 500.
    /// </summary>
    private string TokenWithoutUserId()
    {
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var section = config.GetSection("LocalToken");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["SecretKey"]!));
        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: [new Claim("role_id", ((int)UserRole.Cashier).ToString()), new Claim("store_id", "test-store")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// A fully-formed token (user id present) whose role simply lacks the <c>cash.manage</c> capability.
    /// Bound through <see cref="LocalTokenOptions"/> rather than raw config indexing: the test host
    /// never sets Issuer/Audience explicitly, so a raw <c>section["Issuer"]</c> read (as
    /// <see cref="TokenWithoutUserId"/> above does) comes back null and the token fails validation
    /// before authorization is ever reached.
    /// </summary>
    private string TokenWithRole(int roleId)
    {
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var options = config.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>() ?? new LocalTokenOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim("role_id", roleId.ToString()),
                new Claim("store_id", "test-store")
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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
