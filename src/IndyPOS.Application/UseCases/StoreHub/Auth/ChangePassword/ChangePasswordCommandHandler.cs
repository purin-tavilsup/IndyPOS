using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private const int MinPasswordLength = 8;

    private readonly IStoreUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ILocalTokenService _tokens;

    public ChangePasswordCommandHandler(
        IStoreUserRepository users,
        IPasswordHasher hasher,
        ILocalTokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<ChangePasswordResponse> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var newPassword = command.NewPassword?.Trim() ?? string.Empty;
        var currentPassword = command.CurrentPassword ?? string.Empty;

        if (newPassword.Length < MinPasswordLength)
        {
            return new ChangePasswordResponse(false, null, $"New password must be at least {MinPasswordLength} characters.");
        }

        if (newPassword == currentPassword.Trim())
        {
            return new ChangePasswordResponse(false, null, "New password must be different from the current password.");
        }

        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return new ChangePasswordResponse(false, null, "User not found.");
        }

        if (!_hasher.Verify(currentPassword, user.PasswordHash))
        {
            return new ChangePasswordResponse(false, null, "Current password is incorrect.");
        }

        var newHash = _hasher.Hash(newPassword);
        await _users.SetPasswordAsync(user.Id, newHash, mustChangePassword: false, cancellationToken);

        // Mint a fresh token with the flag now cleared so the client gets a normal session.
        user.PasswordHash = newHash;
        user.MustChangePassword = false;
        var token = _tokens.GenerateToken(user);

        return new ChangePasswordResponse(true, token, null);
    }
}
