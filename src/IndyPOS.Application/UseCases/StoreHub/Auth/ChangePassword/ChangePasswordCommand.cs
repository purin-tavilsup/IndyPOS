using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;

/// <summary>
/// Rotates the authenticated user's password. UserId comes from the JWT, never
/// the request body.
/// </summary>
public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : ICommand<ChangePasswordResponse>;
