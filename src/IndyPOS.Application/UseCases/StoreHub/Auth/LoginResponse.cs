namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>
/// Response DTO for login endpoint.
/// </summary>
public record LoginResponse(
    bool Success,
    string? Token,
    StoreUserDto? User,
    string? ErrorMessage);
