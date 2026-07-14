using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for /auth/* endpoints.
/// </summary>
[Collection("Integration")]
public class AuthEndpointTests : IntegrationTestBase
{
    public AuthEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var username = $"testuser_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Password123!", UserRole.Cashier);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be(username);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var username = $"testuser_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Password123!", UserRole.Cashier);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "WrongPassword!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username = $"nonexistent_{Guid.NewGuid():N}",
            password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var username = $"inactive_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Password123!", UserRole.Cashier, isActive: false);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "Password123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var username = $"meuser_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Password123!", UserRole.Cashier);

        // Login to get token
        var loginResponse = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "Password123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);

        // Act
        var response = await Client.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AsCashier_HasCorrectRoleInToken()
    {
        // Arrange
        var username = $"cashier_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Cashier123!", UserRole.Cashier);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "Cashier123!"
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        result!.User!.RoleId.Should().Be((int)UserRole.Cashier);
    }

    [Fact]
    public async Task Login_AsStoreManager_HasCorrectRoleInToken()
    {
        // Arrange
        var username = $"manager_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Manager123!", UserRole.StoreManager);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "Manager123!"
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        result!.User!.RoleId.Should().Be((int)UserRole.StoreManager);
    }

    [Fact]
    public async Task Login_AsSystemAdmin_HasCorrectRoleInToken()
    {
        // Arrange
        var username = $"admin_{Guid.NewGuid():N}";
        await CreateTestUserAsync(username, "Admin123!", UserRole.SystemAdmin);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password = "Admin123!"
        });

        // Assert
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        result!.User!.RoleId.Should().Be((int)UserRole.SystemAdmin);
    }
}
