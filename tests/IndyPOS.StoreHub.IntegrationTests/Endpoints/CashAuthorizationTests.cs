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
    /// Signs a token through <see cref="LocalTokenOptions"/> (bound the same way <c>Program.cs</c>
    /// binds it: <c>config.GetSection(LocalTokenOptions.SectionName).Get&lt;LocalTokenOptions&gt;()
    /// ?? new LocalTokenOptions()</c>) rather than raw config indexing. The test host only sets
    /// <c>LocalToken:SecretKey</c> via <c>UseSetting</c> — a raw <c>section["Issuer"]</c> /
    /// <c>section["Audience"]</c> read comes back null, and a token minted with a null issuer/audience
    /// fails JWT bearer authentication before authorization or any endpoint filter ever runs. Binding
    /// through the options class picks up its Issuer/Audience defaults instead, so the token actually
    /// authenticates and the claims below are what determine the outcome.
    /// </summary>
    private string BuildToken(IEnumerable<Claim> claims)
    {
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var options = config.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>() ?? new LocalTokenOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// A token that AUTHENTICATES (cashier role, which has <c>cash.manage</c>) but carries NO
    /// sub/NameIdentifier claim — the capability check passes, so <see cref="RequireUserIdFilter"/> is
    /// the only thing standing between it and success. For a write (which calls
    /// <c>ClaimsPrincipal.GetRequiredUserId()</c>) that means a 500 if the filter is missing; for a
    /// read that never touches the user id it would otherwise succeed.
    /// </summary>
    private string TokenWithoutUserId() =>
        BuildToken([new Claim("role_id", ((int)UserRole.Cashier).ToString()), new Claim("store_id", "test-store")]);

    /// <summary>A fully-formed token (user id present) whose role simply lacks the <c>cash.manage</c> capability.</summary>
    private string TokenWithRole(int roleId) =>
        BuildToken(
        [
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role_id", roleId.ToString()),
            new Claim("store_id", "test-store")
        ]);

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
