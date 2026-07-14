using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for POST /auth/change-password.
/// </summary>
[Collection("Integration")]
public class ChangePasswordEndpointTests : IntegrationTestBase
{
    public ChangePasswordEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        ClearAuthentication();
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "x", newPassword = "brandNew123" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsError_AndDoesNotChange()
    {
        await AuthenticateAsAsync("changepw_wrong", "Password123!", UserRole.SystemAdmin);
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "WRONG", newPassword = "brandNew123" });

        Assert.False(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ChangePassword_TooShort_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("changepw_short", "Password123!", UserRole.SystemAdmin);
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "Password123!", newPassword = "short7!" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Valid_PersistsNewHash_AndClearsFlag()
    {
        await AuthenticateAsAsync("changepw_ok", "Password123!", UserRole.SystemAdmin);
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "Password123!", newPassword = "brandNew123" });

        Assert.True(resp.IsSuccessStatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var user = await db.StoreUsers.AsNoTracking().FirstAsync(u => u.Username == "changepw_ok");
        Assert.False(user.MustChangePassword);
        Assert.True(BCrypt.Net.BCrypt.Verify("brandNew123", user.PasswordHash));
    }
}
