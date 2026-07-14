namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>Request body for POST /auth/change-password. Identity is taken from the JWT.</summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
