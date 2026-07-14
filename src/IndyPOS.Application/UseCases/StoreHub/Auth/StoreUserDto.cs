namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>
/// DTO for authenticated store user.
/// </summary>
public record StoreUserDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    string StoreId);
