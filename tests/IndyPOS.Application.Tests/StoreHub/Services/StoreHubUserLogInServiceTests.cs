using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class StoreHubUserLogInServiceTests
{
    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<IProductCacheService> _cache = new();
    private readonly Mock<IEventAggregator> _events = new();
    private readonly Mock<UserLoggedInEvent> _loggedInEvent = new();
    private readonly StoreHubUserLogInService _sut;

    public StoreHubUserLogInServiceTests()
    {
        _events.Setup(e => e.GetEvent<UserLoggedInEvent>()).Returns(_loggedInEvent.Object);
        var options = Options.Create(new StoreHubOptions { AutoSyncProductsOnStartup = false });
        _sut = new StoreHubUserLogInService(_client.Object, _cache.Object, _events.Object, options,
            new Mock<ILogger<StoreHubUserLogInService>>().Object);
    }

    private static LoginResponse Ok(bool mustChange) =>
        new(true, "token", new StoreUserDto(Guid.NewGuid(), "admin", "A", "B", 3, "s1"), null, mustChange);

    [Fact]
    public async Task LogInAsync_NormalLogin_PublishesLoggedInEvent()
    {
        _client.Setup(c => c.LoginAsync("admin", "pw", It.IsAny<CancellationToken>())).ReturnsAsync(Ok(false));

        var result = await _sut.LogInAsync("admin", "pw");

        Assert.True(result.Success);
        Assert.False(result.MustChangePassword);
        _loggedInEvent.Verify(e => e.Publish(It.IsAny<ILoggedInUser>()), Times.Once);
    }

    [Fact]
    public async Task LogInAsync_MustChange_DoesNotPublishLoggedInEvent()
    {
        _client.Setup(c => c.LoginAsync("admin", "pw", It.IsAny<CancellationToken>())).ReturnsAsync(Ok(true));

        var result = await _sut.LogInAsync("admin", "pw");

        Assert.True(result.Success);
        Assert.True(result.MustChangePassword);
        _loggedInEvent.Verify(e => e.Publish(It.IsAny<ILoggedInUser>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_OnSuccess_SetsFreshToken()
    {
        _client.Setup(c => c.ChangePasswordAsync("old", "brandNew123", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ChangePasswordResponse(true, "fresh-token", null));

        var result = await _sut.ChangePasswordAsync("old", "brandNew123");

        Assert.True(result.Success);
        _client.Verify(c => c.SetAuthToken("fresh-token"), Times.Once);
    }
}
