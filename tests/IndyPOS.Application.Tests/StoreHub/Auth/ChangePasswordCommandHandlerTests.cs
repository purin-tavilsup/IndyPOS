using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IStoreUserRepository> _repo = new();
    private readonly IPasswordHasher _hasher = new BcryptPasswordHasher();
    private readonly ILocalTokenService _tokens;
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _tokens = new LocalTokenService(Options.Create(new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "IndyPOS.Test",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        }));
        _sut = new ChangePasswordCommandHandler(_repo.Object, _hasher, _tokens);
    }

    private StoreUser MakeUser(string currentPassword)
    {
        var user = new StoreUser
        {
            Id = Guid.NewGuid(), Username = "admin", StoreId = "s1",
            FirstName = "A", LastName = "B", RoleId = 3,
            PasswordHash = _hasher.Hash(currentPassword), PasswordHashVersion = 2,
            MustChangePassword = true
        };
        _repo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidChange_ClearsFlagAndReturnsFreshToken()
    {
        var user = MakeUser("oldPass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "oldPass123", "brandNew123"));

        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        _repo.Verify(r => r.SetPasswordAsync(user.Id, It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_FailsAndDoesNotWrite()
    {
        var user = MakeUser("oldPass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "WRONG", "brandNew123"));

        Assert.False(result.Success);
        _repo.Verify(r => r.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTooShortNewPassword_Fails()
    {
        var user = MakeUser("oldPass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "oldPass123", "short7!"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Handle_WhenNewEqualsCurrent_Fails()
    {
        var user = MakeUser("samePass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "samePass123", "samePass123"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_Fails()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((StoreUser?)null);
        var result = await _sut.HandleAsync(new ChangePasswordCommand(Guid.NewGuid(), "x", "brandNew123"));

        Assert.False(result.Success);
    }
}
