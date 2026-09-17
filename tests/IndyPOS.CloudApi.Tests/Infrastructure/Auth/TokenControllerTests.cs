using FluentAssertions;
using IndyPOS.CloudApi.Domain;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

public class TokenControllerTests
{
    private static CloudDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<CloudDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static TokenController CreateController(CloudDbContext db, string clientId)
    {
        var request = new OpenIddictRequest
        {
            GrantType = GrantTypes.ClientCredentials,
            ClientId = clientId,
            ClientSecret = "irrelevant-openiddict-already-validated-it"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set(new OpenIddictServerAspNetCoreFeature
        {
            Transaction = new OpenIddictServerTransaction { Request = request }
        });

        return new TokenController(db, NullLogger<TokenController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task Exchange_InactiveStore_IsForbidden()
    {
        await using var db = CreateInMemoryContext();
        db.StoreConfigs.Add(new CloudStoreConfig
        {
            StoreId = "store1",
            StoreName = "Test Store",
            ClientId = "store_store1",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, "store_store1").Exchange();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Exchange_UnknownClientId_IsForbidden()
    {
        await using var db = CreateInMemoryContext();

        var result = await CreateController(db, "store_does_not_exist").Exchange();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Exchange_ActiveStore_IssuesToken()
    {
        // OpenIddict owns the client secret and has already validated it before this controller
        // runs; the store persists no secret to re-check. An active, known store must therefore
        // issue a token rather than be forbidden.
        await using var db = CreateInMemoryContext();
        db.StoreConfigs.Add(new CloudStoreConfig
        {
            StoreId = "store1",
            StoreName = "Test Store",
            ClientId = "store_store1",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var result = await CreateController(db, "store_store1").Exchange();

        result.Should().BeOfType<SignInResult>();
    }
}
