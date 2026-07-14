namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>
/// Response DTO for login endpoint. MustChangePassword lives on this envelope
/// (not StoreUserDto), so it does not leak into the session-long ILoggedInUser.
/// </summary>
public record LoginResponse(
    bool Success,
    string? Token,
    StoreUserDto? User,
    string? ErrorMessage,
    bool MustChangePassword = false);
