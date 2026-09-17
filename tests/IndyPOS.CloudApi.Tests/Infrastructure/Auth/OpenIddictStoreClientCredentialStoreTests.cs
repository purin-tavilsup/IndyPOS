using FluentAssertions;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Moq;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

public class OpenIddictStoreClientCredentialStoreTests
{
    [Fact]
    public async Task CreateAsync_BuildsConfidentialClientWithTokenEndpointGrantAndScopes()
    {
        OpenIddictApplicationDescriptor? captured = null;
        var managerMock = new Mock<IOpenIddictApplicationManager>();
        managerMock
            .Setup(m => m.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((d, _) => captured = d)
            .Returns(ValueTask.FromResult<object>(new object()));

        var sut = new OpenIddictStoreClientCredentialStore(managerMock.Object);

        await sut.CreateAsync("store_store1", "the-secret", "Test Store");

        captured.Should().NotBeNull();
        captured!.ClientId.Should().Be("store_store1");
        captured.ClientSecret.Should().Be("the-secret");
        captured.DisplayName.Should().Be("Test Store");
        captured.ClientType.Should().Be(ClientTypes.Confidential);
        captured.Permissions.Should().Contain(new[]
        {
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.ClientCredentials,
            Permissions.Prefixes.Scope + "sync.write",
            Permissions.Prefixes.Scope + "master.read"
        });
    }
}
