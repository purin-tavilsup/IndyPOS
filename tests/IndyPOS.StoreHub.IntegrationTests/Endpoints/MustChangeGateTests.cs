using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

[Collection("Integration")]
public class MustChangeGateTests : IntegrationTestBase
{
    public MustChangeGateTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<string> LoginAsMustChangeAdminAsync()
    {
        // Unique username per call — the shared test DB is not reset between the
        // two test methods in this class (Respawn cleanup is intentionally not run
        // so the seeded payment-method catalog survives), so a fixed username would
        // violate IX_store_user_store_id_username on the second insert. Mirrors the
        // Guid-suffixed pattern used by AuthenticateAs*Async / SalesEndpointTests.
        var username = $"mc_admin_{Guid.NewGuid():N}";

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
            db.StoreUsers.Add(new StoreUser
            {
                Id = Guid.NewGuid(), StoreId = "test-store",
                LegacyUserId = Random.Shared.Next(1000, 9999),
                Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                PasswordHashVersion = 2, FirstName = "MC", LastName = "Admin",
                RoleId = (int)UserRole.SystemAdmin, IsActive = true,
                MustChangePassword = true,
                CreatedAtUtc = DateTime.UtcNow, LastModifiedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var login = await Client.PostAsJsonAsync("/auth/login", new { username, password = "Password123!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.True(body!.MustChangePassword);
        return body.Token!;
    }

    [Fact]
    public async Task MustChangeToken_IsBlockedOnProducts()
    {
        var token = await LoginAsMustChangeAdminAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await Client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task MustChangeToken_IsAllowedOnChangePassword()
    {
        var token = await LoginAsMustChangeAdminAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "Password123!", newPassword = "brandNew123" });

        Assert.True(resp.IsSuccessStatusCode);
    }
}
