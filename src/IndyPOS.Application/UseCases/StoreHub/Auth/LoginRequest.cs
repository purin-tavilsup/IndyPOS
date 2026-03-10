namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>
/// Request DTO for login endpoint.
/// </summary>
public record LoginRequest(string Username, string Password);
