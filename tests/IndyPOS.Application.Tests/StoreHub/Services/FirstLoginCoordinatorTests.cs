using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Infrastructure.Services.StoreHub;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class FirstLoginCoordinatorTests
{
    private readonly Mock<IUserLogInService> _login = new();
    private readonly Mock<IChangePasswordPrompt> _prompt = new();
    private readonly FirstLoginCoordinator _sut;

    public FirstLoginCoordinatorTests()
    {
        _sut = new FirstLoginCoordinator(_login.Object, _prompt.Object);
    }

    [Fact]
    public async Task LogInAsync_NormalLogin_ReturnsTrue_NoPrompt()
    {
        _login.Setup(l => l.LogInAsync("u", "p")).ReturnsAsync(new LogInResult(true, false));

        Assert.True(await _sut.LogInAsync("u", "p"));
        _prompt.Verify(p => p.RequestNewPasswordAsync(), Times.Never);
    }

    [Fact]
    public async Task LogInAsync_FailedLogin_ReturnsFalse()
    {
        _login.Setup(l => l.LogInAsync("u", "p")).ReturnsAsync(new LogInResult(false, false));
        Assert.False(await _sut.LogInAsync("u", "p"));
    }

    [Fact]
    public async Task LogInAsync_MustChange_ChangesThenRelogins()
    {
        _login.SetupSequence(l => l.LogInAsync("u", "boot"))
              .ReturnsAsync(new LogInResult(true, true));
        _login.Setup(l => l.LogInAsync("u", "newPass123")).ReturnsAsync(new LogInResult(true, false));
        _prompt.Setup(p => p.RequestNewPasswordAsync()).ReturnsAsync("newPass123");
        _login.Setup(l => l.ChangePasswordAsync("boot", "newPass123")).ReturnsAsync(new ChangePasswordResult(true, null));

        Assert.True(await _sut.LogInAsync("u", "boot"));
        _login.Verify(l => l.LogInAsync("u", "newPass123"), Times.Once);
    }

    [Fact]
    public async Task LogInAsync_MustChange_Cancelled_LogsOut_ReturnsFalse()
    {
        _login.Setup(l => l.LogInAsync("u", "boot")).ReturnsAsync(new LogInResult(true, true));
        _prompt.Setup(p => p.RequestNewPasswordAsync()).ReturnsAsync((string?)null);

        Assert.False(await _sut.LogInAsync("u", "boot"));
        _login.Verify(l => l.LogOut(), Times.Once);
    }

    [Fact]
    public async Task LogInAsync_MustChange_ChangeFails_LogsOut_ReturnsFalse()
    {
        _login.Setup(l => l.LogInAsync("u", "boot")).ReturnsAsync(new LogInResult(true, true));
        _prompt.Setup(p => p.RequestNewPasswordAsync()).ReturnsAsync("newPass123");
        _login.Setup(l => l.ChangePasswordAsync("boot", "newPass123")).ReturnsAsync(new ChangePasswordResult(false, "bad"));

        Assert.False(await _sut.LogInAsync("u", "boot"));
        _login.Verify(l => l.LogOut(), Times.Once);
    }
}
