namespace IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;

/// <summary>
/// On success, Token is a fresh JWT WITHOUT the must_change claim so the caller
/// can proceed with a normal session.
/// </summary>
public record ChangePasswordResponse(bool Success, string? Token, string? ErrorMessage);
